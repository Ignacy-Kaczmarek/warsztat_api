using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warsztat.Models;
using System.Linq;
using System.Security.Claims;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Warsztat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationController : ControllerBase
    {
        private readonly WarsztatdbContext _context;

        public ReservationController(WarsztatdbContext context)
        {
            _context = context;
        }

        [HttpGet("occupied-slots")]
        public IActionResult GetOccupiedSlots(DateTime startDate, DateTime endDate)
        {
            var occupiedSlots = _context.Orders
                .Select(o => new
                {
                    o.StartDate,
                    EstimatedEndDate = o.StartDate.AddHours(o.Services.Sum(s => s.RepairTime))
                })
                .Where(slot => slot.StartDate < endDate && slot.EstimatedEndDate > startDate)
                .ToList();

            return Ok(occupiedSlots);
        }


        [HttpPost("finalize")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> FinalizeReservation([FromBody] ReservationFinalizeDto finalizeDto)
        {
            // Pobranie ID klienta z tokena JWT
            int clientId = int.Parse(User.FindFirst("id").Value);

            // Sprawdzenie, czy pojazd należy do klienta
            var car = _context.Cars.FirstOrDefault(c => c.Id == finalizeDto.CarId && c.ClientId == clientId);
            if (car == null)
            {
                return Unauthorized("Nie masz uprawnień do tego pojazdu.");
            }

            // Pobranie usług z bazy danych
            var services = _context.Services
                .Where(s => finalizeDto.ServiceIds.Contains(s.Id))
                .ToList();

            if (services.Count != finalizeDto.ServiceIds.Count)
            {
                return BadRequest("Niektóre usługi są nieprawidłowe.");
            }

            // Obliczenie kosztów, czasu oraz przewidywanej daty zakończenia
            var totalCost = services.Sum(s => s.Price);
            var totalTimeInMinutes = services.Sum(s => s.RepairTime) + 15; // Dodajemy 15 minut na protokół
            var estimatedEndDate = finalizeDto.PreferredStartDate.AddMinutes(totalTimeInMinutes);

            // Sprawdzenie dostępności stanowisk w podanym czasie
            var overlappingOrdersCount = _context.Orders
                .Where(o => o.StartDate < estimatedEndDate &&
                            o.StartDate.AddMinutes(o.Services.Sum(s => s.RepairTime) + 15) > finalizeDto.PreferredStartDate)
                .Count();

            // Zmienna określająca maksymalną liczbę stanowisk
            const int MaxWorkstations = 3;

            if (overlappingOrdersCount >= MaxWorkstations)
            {
                return Conflict("Brak wolnych stanowisk w wybranym terminie.");
            }

            // Utworzenie nowego zlecenia
            var order = new Order
            {
                ClientId = clientId,
                StartDate = finalizeDto.PreferredStartDate,
                Status = "Oczekuje",
                PaymentStatus = 0,
                EmployeeId = null, // Pracownik będzie przypisany później
                InvoiceLink = null,
                Services = services
            };

            // Dodanie zamówienia do bazy danych
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Zwrócenie odpowiedzi z podsumowaniem
            return Ok(new
            {
                Message = "Rezerwacja została pomyślnie utworzona.",
                TotalCost = totalCost,
                TotalTimeInMinutes = totalTimeInMinutes,
                EstimatedEndDate = estimatedEndDate
            });
        }







        [HttpGet("init")]
        [Authorize(Roles = "Client")]
        
        public IActionResult GetReservationData()
        {
            // Domyślne wartości dla `startDate` i `endDate`
            DateTime startDate = DateTime.Now.Date; // Dzisiejsza data bez czasu
            DateTime endDate = DateTime.Now.Date.AddYears(1); // Rok od dzisiaj

            // Pobranie zajętych terminów w domyślnym lub podanym zakresie
            var occupiedSlots = _context.Orders
                .Where(o => o.StartDate < endDate && o.StartDate.AddMinutes(o.Services.Sum(s => s.RepairTime) + 15) > startDate)
                .Select(o => new
                {
                    o.StartDate,
                    EstimatedEndDate = o.StartDate.AddMinutes(o.Services.Sum(s => s.RepairTime) + 15)
                })
                .ToList();

            // Pobranie listy usług
            var services = _context.Services
                .Select(service => new
                {
                    service.Id,
                    service.Name,
                    service.Price,
                    RepairTimeInMinutes = service.RepairTime
                })
                .ToList();

            return Ok(new
            {
                OccupiedSlots = occupiedSlots,
                Services = services
            });
        }


        [HttpGet("all")]
        [Authorize(Policy = "RequireEmployeeRole")]

        public IActionResult GetAllReservations()
        {
            var reservations = _context.Orders
                .Select(o => new
                {
                    o.Id,
                    o.StartDate,
                    EstimatedEndDate = o.StartDate.AddHours(o.Services.Sum(s => s.RepairTime)),
                    o.Status,
                    o.ClientId,
                    TotalCost = o.Services.Sum(s => s.Price),
                    Services = o.Services.Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Price,
                        s.RepairTime
                    }).ToList()
                })
                .ToList();
            return Ok(reservations);
        }

        [HttpGet("client")]
        [Authorize(Policy = "RequireClientRole")]
        public IActionResult GetClientReservations()
        {
            int clientId = int.Parse(User.FindFirst("id").Value);

            var reservations = _context.Orders
                .Where(o => o.ClientId == clientId)
                .Select(o => new
                {
                    o.Id,
                    o.StartDate,
                    EstimatedEndDate = o.StartDate.AddHours(o.Services.Sum(s => s.RepairTime)),
                    o.Status,
                    TotalCost = o.Services.Sum(s => s.Price),
                    Services = o.Services.Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Price,
                        s.RepairTime
                    }).ToList()
                })
                .ToList();

            return Ok(reservations);
        }

        [HttpGet("{id}")]
        public IActionResult GetOrderById(int id)
        {
            var userId = int.Parse(User.FindFirst("id").Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;


            // Pobranie zgłoszenia z bazy
            var order = _context.Orders
                .Where(o => o.Id == id)
                .Select(o => new
                {
                    o.Id,
                    o.StartDate,
                    o.Status,
                    o.Client.FirstName,
                    o.Client.LastName,
                    ClientId = o.Client.Id,
                    Services = o.Services.Select(s => new { s.Name, s.Price, s.RepairTime }),
                    TotalCost = o.Services.Sum(s => s.Price),
                    TotalTimeInMinutes = o.Services.Sum(s => s.RepairTime) + 15,
                    EstimatedEndDate = o.StartDate.AddMinutes(o.Services.Sum(s => s.RepairTime) + 15)
                })
                .FirstOrDefault();

            // Jeśli zlecenie nie istnieje, zwróć 404
            if (order == null)
            {
                return NotFound($"Zlecenie o ID {id} nie istnieje.");
            }

            // Logika autoryzacji dostępu
            if (userRole == "Employee" || userRole == "Manager")
            {
                // Pracownik i kierownik mają dostęp bez dodatkowej weryfikacji
                return Ok(order);
            }
            else if (userRole == "Client" && order.ClientId == userId)
            {
                // Klient ma dostęp tylko do swoich zleceń
                return Ok(order);
            }

            // W innych przypadkach brak dostępu
            return Unauthorized("Nie masz uprawnień do tego zlecenia.");
        }

        [HttpPatch("{id}/update")]
        [Authorize(Policy = "RequireEmployeeRole")]
        public async Task<IActionResult> UpdateReservation(int id, [FromBody] UpdateReservationDto updateDto)
        {
            // Znajdź rezerwację w bazie danych
            var reservation = await _context.Orders.FindAsync(id);
            if (reservation == null)
            {
                return NotFound($"Rezerwacja o ID {id} nie istnieje.");
            }

            // Aktualizuj pola, jeśli zostały przekazane w DTO
            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                reservation.Status = updateDto.Status;
            }

            if (updateDto.EmployeeId.HasValue)
            {
                var employee = await _context.Employees.FindAsync(updateDto.EmployeeId.Value);
                if (employee == null)
                {
                    return BadRequest("Pracownik o podanym ID nie istnieje.");
                }
                reservation.EmployeeId = updateDto.EmployeeId;
            }

            if (!string.IsNullOrEmpty(updateDto.Comment))
            {
                reservation.Comment = updateDto.Comment;
            }

            if (updateDto.PaymentStatus.HasValue)
            {
                if (updateDto.PaymentStatus < 0 || updateDto.PaymentStatus > 2) // Zakładam, że status płatności ma wartości 0, 1, 2
                {
                    return BadRequest("Nieprawidłowy status płatności.");
                }
                reservation.PaymentStatus = updateDto.PaymentStatus.Value;
            }

            if (!string.IsNullOrEmpty(updateDto.InvoiceLink))
            {
                reservation.InvoiceLink = updateDto.InvoiceLink;
            }

            // Zapisz zmiany w bazie danych
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Rezerwacja została zaktualizowana." });
        }


        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            // Pobranie zlecenia wraz z usługami
            var reservation = await _context.Orders
                .Include(o => o.Services) // Dołączenie usług powiązanych z zamówieniem
                .Include(o => o.Client) // Dołączenie klienta dla weryfikacji
                .FirstOrDefaultAsync(o => o.Id == id);

            if (reservation == null)
            {
                return NotFound($"Rezerwacja o ID {id} nie istnieje.");
            }

            // Pobranie danych użytkownika z tokena JWT
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst("id").Value);

            // Sprawdzenie uprawnień
            if (userRole == "Client" && reservation.ClientId != userId)
            {
                return StatusCode(403, new { Message = "Nie masz uprawnień do usunięcia tej rezerwacji." });
            }

            // Usunięcie powiązań między zleceniem a usługami
            reservation.Services.Clear();

            // Usunięcie samego zlecenia
            _context.Orders.Remove(reservation);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Rezerwacja została usunięta." });
        }


    }




}

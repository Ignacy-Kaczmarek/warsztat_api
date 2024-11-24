using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warsztat.Models;

namespace Warsztat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly WarsztatdbContext _context;

        public EmployeeController(WarsztatdbContext context)
        {
            _context = context;
        }

        // 1. Pobieranie zleceń przypisanych do pracownika, które nie są ukończone
        [HttpGet("reservations")]
        public async Task<IActionResult> GetEmployeeReservations()
        {
            int employeeId = int.Parse(User.FindFirst("id").Value);

            var reservations = await _context.Orders
                .Where(o => o.EmployeeId == employeeId && o.Status != "Ukończone")
                .OrderBy(o => o.StartDate)
                .Select(o => new
                {
                    o.Id,
                    StartDate = o.StartDate,
                    EstimatedEndDate = o.StartDate.AddMinutes(o.Services.Sum(s => s.RepairTime) + 15),
                    o.Status,
                    Vehicle = _context.Cars
                        .Where(car => car.ClientId == o.ClientId)
                        .Select(car => new
                        {
                            car.Id,
                            car.Brand,
                            car.Model,
                            car.ProductionYear,
                            car.Vin,
                            car.RegistrationNumber
                        })
                        .FirstOrDefault(),
                    Client = new
                    {
                        o.Client.Id,
                        o.Client.FirstName,
                        o.Client.LastName
                    },
                    Tasks = o.Services.Select(s => new
                    {
                        s.Name
                    }).ToList()
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // 2. Oznaczenie zlecenia jako "ukończone"
        [HttpPatch("reservations/{id}/complete")]
        public async Task<IActionResult> MarkReservationAsCompleted(int id)
        {
            int employeeId = int.Parse(User.FindFirst("id").Value);

            var reservation = await _context.Orders.FindAsync(id);
            if (reservation == null)
            {
                return NotFound("Zlecenie o podanym ID nie istnieje.");
            }

            if (reservation.EmployeeId != employeeId)
            {
                return Unauthorized("Nie masz uprawnień do ukończenia tego zlecenia.");
            }

            reservation.Status = "Ukończone";

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Zlecenie zostało oznaczone jako ukończone." });
        }

        // 3. Pobranie komentarza do zlecenia
        [HttpGet("reservations/{id}/comment")]
        public async Task<IActionResult> GetReservationComment(int id)
        {
            int employeeId = int.Parse(User.FindFirst("id").Value);

            var reservation = await _context.Orders
                .Where(o => o.Id == id && o.EmployeeId == employeeId)
                .Select(o => new { o.Comment })
                .FirstOrDefaultAsync();

            if (reservation == null)
            {
                return NotFound("Zlecenie o podanym ID nie istnieje lub nie masz do niego dostępu.");
            }

            return Ok(new { Comment = reservation.Comment ?? string.Empty });
        }

        // 4. Dodanie/aktualizacja komentarza do zlecenia
        [HttpPost("reservations/{id}/comment")]
        public async Task<IActionResult> UpdateReservationComment(int id, [FromBody] string comment)
        {
            int employeeId = int.Parse(User.FindFirst("id").Value);

            var reservation = await _context.Orders.FindAsync(id);
            if (reservation == null)
            {
                return NotFound("Zlecenie o podanym ID nie istnieje.");
            }

            if (reservation.EmployeeId != employeeId)
            {
                return Unauthorized("Nie masz uprawnień do edytowania tego zlecenia.");
            }

            reservation.Comment = comment;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Komentarz został zaktualizowany." });
        }
    }
}

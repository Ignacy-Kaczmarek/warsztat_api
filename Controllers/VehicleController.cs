using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warsztat.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Warsztat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Upewniamy się, że tylko zalogowani użytkownicy mają dostęp
    public class VehicleController : Controller
    {
        private readonly WarsztatdbContext _context;

        public VehicleController(WarsztatdbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddVehicle([FromBody] VehicleDto vehicleDto)
        {
            // Znajdź zalogowanego klienta na podstawie ID (pozyskanego z tokena JWT)
            int clientId = int.Parse(User.FindFirst("id").Value); // Wyciąganie ID z tokena JWT

            // Utwórz nowy pojazd i przypisz go do klienta
            var vehicle = new Car
            {
                Brand = vehicleDto.Brand,
                Model = vehicleDto.Model,
                ProductionYear = vehicleDto.ProductionYear,
                Vin = vehicleDto.Vin,
                RegistrationNumber = vehicleDto.RegistrationNumber,
                ClientId = clientId
            };

            _context.Cars.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Pojazd został dodany pomyślnie." });
        }

        [HttpPut("update/{vehicleId}")]
        public async Task<IActionResult> UpdateVehicle(int vehicleId, [FromBody] VehicleUpdateDto vehicleDto)
        {
            // Wyciągnięcie ID klienta z tokena JWT
            int clientId = int.Parse(User.FindFirst("id").Value);

            // Znajdź pojazd w bazie danych
            var vehicle = await _context.Cars.FindAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound("Pojazd o podanym ID nie istnieje.");
            }

            // Sprawdzenie, czy pojazd należy do zalogowanego klienta
            if (vehicle.ClientId != clientId)
            {
                return Unauthorized("Nie masz uprawnień do edytowania tego pojazdu.");
            }

            // Aktualizacja tylko tych pól, które zostały podane w żądaniu
            vehicle.Brand = vehicleDto.Brand ?? vehicle.Brand;
            vehicle.Model = vehicleDto.Model ?? vehicle.Model;
            vehicle.ProductionYear = vehicleDto.ProductionYear ?? vehicle.ProductionYear;
            vehicle.Vin = vehicleDto.Vin ?? vehicle.Vin;
            vehicle.RegistrationNumber = vehicleDto.RegistrationNumber ?? vehicle.RegistrationNumber;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Dane pojazdu zostały zaktualizowane." });
        }


        [HttpDelete("delete/{vehicleId}")]
        public async Task<IActionResult> DeleteVehicle(int vehicleId)
        {
            // Wyciągnięcie ID klienta z tokena JWT
            int clientId = int.Parse(User.FindFirst("id").Value);

            // Znajdź pojazd w bazie danych
            var vehicle = await _context.Cars.FindAsync(vehicleId);
            if (vehicle == null)
            {
                return NotFound("Pojazd o podanym ID nie istnieje.");
            }

            // Sprawdzenie, czy pojazd należy do zalogowanego klienta
            if (vehicle.ClientId != clientId)
            {
                return Unauthorized("Nie masz uprawnień do usunięcia tego pojazdu.");
            }

            _context.Cars.Remove(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Pojazd został usunięty pomyślnie." });
        }

        [HttpGet("my-vehicles")]
        [Authorize(Roles = "Client")]
        public IActionResult GetMyVehicles()
        {
            int clientId = int.Parse(User.FindFirst("id").Value);

            var vehicles = _context.Cars
                .Where(car => car.ClientId == clientId)
                .Select(car => new
                {
                    car.Id,
                    car.Brand,
                    car.Model,
                    car.ProductionYear,
                    car.Vin,
                    car.RegistrationNumber
                })
                .ToList();

            return Ok(vehicles);
        }

        [HttpGet("{vehicleId}")]
        public IActionResult GetVehicleById(int vehicleId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst("id").Value);

            var vehicle = _context.Cars
                .Where(car => car.Id == vehicleId)
                .Select(car => new
                {
                    car.Id,
                    car.Brand,
                    car.Model,
                    car.ProductionYear,
                    car.Vin,
                    car.RegistrationNumber,
                    Client = new { car.Client.Id, car.Client.FirstName, car.Client.LastName }
                })
                .FirstOrDefault();

            if (vehicle == null)
            {
                return NotFound("Pojazd o podanym ID nie istnieje.");
            }

            // Klient może zobaczyć tylko swoje pojazdy
            if (userRole == "Client" && vehicle.Client.Id != userId)
            {
                return Unauthorized("Nie masz uprawnień do tego pojazdu.");
            }
                return Ok(vehicle);
        }


    }
}

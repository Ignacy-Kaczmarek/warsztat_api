using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warsztat.Models;


namespace Warsztat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ManagerController : Controller
    {
        private readonly WarsztatdbContext _context;
        public ManagerController(WarsztatdbContext context)
        {
            _context = context;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableEmployees(DateTime start, DateTime end)
        {
            var availableEmployees = await _context.Employees
                .Where(e => e.IsManager == 0 && // Wyklucza kierowników
                            !_context.Orders
                                .Where(o => o.EmployeeId == e.Id &&
                                            o.StartDate < end &&
                                            o.StartDate.AddMinutes(o.Services.Sum(s => s.RepairTime)) > start)
                                .Any()) // Wyklucza zajętych pracowników
                .Select(e => new
                {
                    e.Id,
                    FullName = e.FirstName + " " + e.LastName
                })
                .ToListAsync();


            return Ok(availableEmployees);
        }


        [HttpGet("{employeeId}/reservations")]
        [Authorize(Policy = "RequireManagerRole")]
        public async Task<IActionResult> GetEmployeeReservations(int employeeId)
        {
            var employeeReservations = await _context.Orders
                .Where(o => o.EmployeeId == employeeId && o.Status != "Ukończone")
                .OrderBy(o => o.StartDate)
                .Select(o => new
                {
                    o.Id,
                    o.StartDate,
                    o.Status,
                    ClientName = o.Client.FirstName + " " + o.Client.LastName
                })
                .ToListAsync();

            return Ok(employeeReservations);
        }

        [HttpPatch("{orderId}/mark-as-paid")]
        [Authorize(Policy = "RequireManagerRole")]
        public async Task<IActionResult> MarkOrderAsPaid(int orderId)
        {
            // Znajdź zlecenie na podstawie ID
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            // Jeśli zlecenie nie istnieje, zwróć 404
            if (order == null)
            {
                return NotFound($"Zlecenie o ID {orderId} nie istnieje.");
            }

            // Sprawdź, czy zlecenie jest już oznaczone jako opłacone
            if (order.PaymentStatus == 1)
            {
                return BadRequest($"Zlecenie o ID {orderId} jest już oznaczone jako opłacone.");
            }

            // Ustaw status płatności na "Opłacone"
            order.PaymentStatus = 1;

            // Zapisz zmiany w bazie danych
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Zlecenie o ID {orderId} zostało oznaczone jako opłacone."
            });
        }



    }
}

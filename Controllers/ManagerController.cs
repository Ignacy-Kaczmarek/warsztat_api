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


    }
}

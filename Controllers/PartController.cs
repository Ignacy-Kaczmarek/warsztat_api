using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Warsztat.Models;

namespace Warsztat.Controllers
{
    public class PartController : Controller
    {
        private readonly WarsztatdbContext _context;

        public PartController(WarsztatdbContext context)
        {
            _context = context;
        }

        // 1. GET all parts for an order
        [HttpGet("{orderId}")]
        [Authorize(Roles = "Client,Employee,Manager")]
        public async Task<IActionResult> GetPartsForOrder(int orderId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst("id").Value);

            var order = await _context.Orders
                .Include(o => o.Client)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound("Zlecenie o podanym ID nie istnieje.");
            }

            // Klient może pobierać części tylko do swoich zleceń
            if (userRole == "Client" && order.ClientId != userId)
            {
                return Unauthorized("Nie masz uprawnień do przeglądania części tego zlecenia.");
            }

            var parts = await _context.Parts
                .Where(p => p.OrderId == orderId)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SerialNumber,
                    p.Quantity,
                    p.Price
                })
                .ToListAsync();

            if (!parts.Any())
            {
                return NotFound("Brak części przypisanych do tego zlecenia.");
            }

            return Ok(parts);
        }



        // 2. POST add a part to an order
        [HttpPost("{orderId}")]
        [Authorize(Policy = "RequireEmployeeRole")]
        public async Task<IActionResult> AddPartToOrder(int orderId, [FromBody] PartDto partDto)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound($"Zlecenie o ID {orderId} nie istnieje.");
            }

            var part = new Part
            {
                OrderId = orderId,
                Name = partDto.Name,
                SerialNumber = partDto.SerialNumber,
                Quantity = partDto.Quantity,
                Price = partDto.Price
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Część została dodana.", PartId = part.Id });
        }

        // 3. PUT update a part
        [HttpPut("{orderId}/{partId}")]
        [Authorize(Policy = "RequireEmployeeRole")]
        public async Task<IActionResult> UpdatePartInOrder(int orderId, int partId, [FromBody] PartDto partDto)
        {
            var part = await _context.Parts.FindAsync(partId);
            if (part == null || part.OrderId != orderId)
            {
                return NotFound($"Część o ID {partId} dla zlecenia {orderId} nie istnieje.");
            }

            part.Name = partDto.Name ?? part.Name;
            part.SerialNumber = partDto.SerialNumber ?? part.SerialNumber;
            part.Quantity = partDto.Quantity > 0 ? partDto.Quantity : part.Quantity;
            part.Price = partDto.Price > 0 ? partDto.Price : part.Price;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Część została zaktualizowana." });
        }

        // 4. DELETE remove a part from an order
        [HttpDelete("{orderId}/{partId}")]
        [Authorize(Policy = "RequireEmployeeRole")]
        public async Task<IActionResult> DeletePartFromOrder(int orderId, int partId)
        {
            var part = await _context.Parts.FindAsync(partId);
            if (part == null || part.OrderId != orderId)
            {
                return NotFound($"Część o ID {partId} dla zlecenia {orderId} nie istnieje.");
            }

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Część została usunięta." });
        }
    }
}

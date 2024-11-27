using iText.IO.Font;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
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

            var reservation = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Services)
                .Include(o => o.Parts)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (reservation == null)
            {
                return NotFound("Zlecenie o podanym ID nie istnieje.");
            }

            if (reservation.EmployeeId != employeeId)
            {
                return Unauthorized("Nie masz uprawnień do ukończenia tego zlecenia.");
            }

            reservation.Status = "Ukończone";

            // Wywołanie funkcji generującej fakturę
            try
            {
                string invoiceLink = await GenerateInvoice(reservation);
                reservation.InvoiceLink = invoiceLink;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Błąd podczas generowania faktury.", Error = ex.Message });
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Zlecenie zostało oznaczone jako ukończone.", InvoiceLink = reservation.InvoiceLink });
        }

        private async Task<string> GenerateInvoice(Order reservation)
        {
            string invoiceDirectory = Path.Combine("wwwroot", "invoices", reservation.Id.ToString());
            Directory.CreateDirectory(invoiceDirectory);

            string pdfPath = Path.Combine(invoiceDirectory, $"invoice_{reservation.Id}.pdf");
            string fontPathRegular = Path.Combine("wwwroot", "fonts", "ARIAL.ttf");
            string fontPathBold = Path.Combine("wwwroot", "fonts", "ARIALBD.ttf");

            if (System.IO.File.Exists(pdfPath))
            {
                System.IO.File.Delete(pdfPath);
            }

            var writer = new PdfWriter(pdfPath);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            var regularFont = PdfFontFactory.CreateFont(fontPathRegular, PdfEncodings.IDENTITY_H);
            var boldFont = PdfFontFactory.CreateFont(fontPathBold, PdfEncodings.IDENTITY_H);

            document.SetFont(regularFont);
            document.SetFontSize(12);

            // Nagłówek faktury
            document.Add(new Paragraph("Faktura VAT")
                .SetFont(boldFont)
                .SetFontSize(18)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Dane sprzedawcy
            document.Add(new Paragraph("Sprzedawca:").SetFont(boldFont).SetFontSize(14));
            document.Add(new Paragraph("Nazwa firmy: Warsztat Samochodowy XYZ")
                .SetFont(regularFont).SetMarginBottom(5));
            document.Add(new Paragraph("Adres: ul. Przykładowa 12, 00-000 Miasto")
                .SetFont(regularFont).SetMarginBottom(5));
            document.Add(new Paragraph("NIP: 123-456-78-90")
                .SetFont(regularFont).SetMarginBottom(10));

            // Dane nabywcy
            document.Add(new Paragraph("Nabywca:").SetFont(boldFont).SetFontSize(14));
            document.Add(new Paragraph($"{reservation.Client.FirstName} {reservation.Client.LastName}")
                .SetFont(regularFont).SetMarginBottom(5));
            document.Add(new Paragraph($"Adres: {reservation.Client.Address}")
                .SetFont(regularFont).SetMarginBottom(5));
            //document.Add(new Paragraph($"NIP: {reservation.Client.Nip}")
            //    .SetFont(regularFont).SetMarginBottom(10));

            // Szczegóły zlecenia (data i numer faktury)
            AddDetail(document, "Data wystawienia:", DateTime.Now.ToString("yyyy-MM-dd"), boldFont, regularFont);
            AddDetail(document, "Numer faktury:", $"{reservation.Id}/{DateTime.Now.Year}", boldFont, regularFont);

            // Tabela z usługami i częściami
            document.Add(new Paragraph("Szczegóły faktury:").SetFont(boldFont).SetFontSize(14).SetMarginTop(20));

            var table = new Table(5, false);
            table.AddHeaderCell(new Cell().Add(new Paragraph("Nazwa").SetFont(boldFont)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Typ").SetFont(boldFont)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Ilość").SetFont(boldFont)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Cena za szt.").SetFont(boldFont)));
            table.AddHeaderCell(new Cell().Add(new Paragraph("Cena").SetFont(boldFont)));

            foreach (var service in reservation.Services)
            {
                table.AddCell(new Cell().Add(new Paragraph(service.Name).SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph("Usługa").SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph("1").SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph(service.Price.ToString("C")).SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph(service.Price.ToString("C")).SetFont(regularFont)));
            }

            foreach (var part in reservation.Parts)
            {
                table.AddCell(new Cell().Add(new Paragraph(part.Name).SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph("Część").SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph(part.Quantity.ToString()).SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph(part.Price.ToString("C")).SetFont(regularFont)));
                table.AddCell(new Cell().Add(new Paragraph((part.Price * part.Quantity).ToString("C")).SetFont(regularFont)));
            }

            document.Add(table.SetMarginTop(10).SetMarginBottom(10));

            // Podsumowanie
            decimal totalServices = reservation.Services.Sum(s => s.Price);
            decimal totalParts = reservation.Parts.Sum(p => p.Price * p.Quantity);
            decimal totalCost = totalServices + totalParts;

            AddDetail(document, "Razem do zapłaty :", $"{totalCost * 1m:C}", boldFont, regularFont);

            document.Close();

            // Zwróć ścieżkę do pliku
            return $"/invoices/{reservation.Id}/invoice_{reservation.Id}.pdf";
        }

        private void AddDetail(Document document, string label, string value, PdfFont boldFont, PdfFont regularFont)
        {
            document.Add(new Paragraph()
                .Add(new Text(label).SetFont(boldFont))
                .Add(new Text(" " + value).SetFont(regularFont))
                .SetMarginBottom(5));
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warsztat.Models;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.Layout.Properties;


namespace Warsztat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProtocolController : ControllerBase
    {
        private readonly WarsztatdbContext _context;

        public ProtocolController(WarsztatdbContext context)
        {
            _context = context;
        }

        [HttpPost("{orderId}/description")]
        [Authorize(Policy = "RequireEmployeeRole")]
        public async Task<IActionResult> AddProtocolDescription(int orderId, [FromBody] ProtocolDescriptionDto request)
        {
            var order = await _context.Orders.Include(o => o.Handoverprotocol).FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound("Zlecenie o podanym ID nie istnieje.");
            }

            if (order.Handoverprotocol == null)
            {
                order.Handoverprotocol = new Handoverprotocol { OrderId = orderId };
            }

            order.Handoverprotocol.Description = request.Description;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Opis protokołu został dodany." });
        }


        // 2. Przesyłanie zdjęć do folderu
        [HttpPost("{orderId}/upload-photo")]
        [Authorize(Policy = "RequireEmployeeRole")]
        public async Task<IActionResult> UploadPhoto(int orderId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                return BadRequest("Nie przesłano zdjęcia.");
            }

            // Ścieżka do folderu dla tego zlecenia
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "protocols", orderId.ToString());

            // Jeśli folder nie istnieje, twórz go
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Pobierz numer ostatniego zdjęcia w folderze i zwiększ go o 1
            var existingFiles = Directory.GetFiles(folderPath, "zdj_*")
                   .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            int nextNumber = existingFiles.Length + 1; // Liczba plików + 1 jako kolejny numer

            // Nazwa pliku
            var fileName = $"zdj_{nextNumber}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            // Zapis zdjęcia na serwerze
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // Aktualizacja linku do folderu w bazie danych
            var protocol = await _context.Handoverprotocols.FirstOrDefaultAsync(p => p.OrderId == orderId);
            if (protocol == null)
            {
                protocol = new Handoverprotocol { OrderId = orderId, PictureLink = $"/protocols/{orderId}/" };
                _context.Handoverprotocols.Add(protocol);
            }
            else if (protocol.PictureLink == null)
            {
                protocol.PictureLink = $"/protocols/{orderId}/";
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Zdjęcie zostało przesłane.", FileName = fileName });
        }


        // 3. Pobieranie danych protokołu
        [HttpGet("{orderId}")]
        [Authorize(Roles = "Client,Employee,Manager")]
        public async Task<IActionResult> GetProtocolDetails(int orderId)
        {
            var protocol = await _context.Handoverprotocols
                .Include(p => p.Order)
                .ThenInclude(o => o.Client)
                .Include(p => p.Order.Services)
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (protocol == null)
            {
                return NotFound("Protokół dla tego zlecenia nie istnieje.");
            }

            return Ok(new
            {
                protocol.OrderId,
                Description = protocol.Description,
                PictureLink = protocol.PictureLink,
                protocol.ProtocolLink,
                Client = new
                {
                    protocol.Order.Client.FirstName,
                    protocol.Order.Client.LastName,
                    protocol.Order.Client.PhoneNumber
                },
                Vehicle = await _context.Cars
                    .Where(car => car.ClientId == protocol.Order.ClientId)
                    .Select(car => new
                    {
                        car.Id,
                        car.Brand,
                        car.Model,
                        car.ProductionYear,
                        car.Vin,
                        car.RegistrationNumber
                    })
                    .FirstOrDefaultAsync(),
                Tasks = protocol.Order.Services.Select(s => s.Name).ToList()
            });
        }

        //[HttpPost("{orderId}/generate-pdf")]
        //[Authorize(Policy = "RequireEmployeeRole")]
        //public async Task<IActionResult> GeneratePdf(int orderId)
        //{
        //    var order = await _context.Orders
        //        .Include(o => o.Client)
        //        .Include(o => o.Employee)
        //        .Include(o => o.Services)
        //        .Include(o => o.Handoverprotocol)
        //        .FirstOrDefaultAsync(o => o.Id == orderId);

        //    if (order == null)
        //    {
        //        return NotFound("Zlecenie nie istnieje.");
        //    }

        //    var car = await _context.Cars
        //        .FirstOrDefaultAsync(c => c.ClientId == order.ClientId);

        //    string protocolDirectory = Path.Combine("wwwroot", "protocols", orderId.ToString());
        //    Directory.CreateDirectory(protocolDirectory);

        //    string pdfPath = Path.Combine(protocolDirectory, $"protocol_{orderId}.pdf");
        //    string fontPathRegular = Path.Combine("wwwroot", "fonts", "ARIAL.ttf");
        //    string fontPathBold = Path.Combine("wwwroot", "fonts", "ARIALBD.ttf");

        //    if (System.IO.File.Exists(pdfPath))
        //    {
        //        System.IO.File.Delete(pdfPath);
        //    }

        //    var writer = new PdfWriter(pdfPath);
        //    var pdf = new PdfDocument(writer);
        //    var document = new Document(pdf);

        //    var regularFont = PdfFontFactory.CreateFont(fontPathRegular, PdfEncodings.IDENTITY_H);
        //    var boldFont = PdfFontFactory.CreateFont(fontPathBold, PdfEncodings.IDENTITY_H);

        //    document.SetFont(regularFont);
        //    document.SetFontSize(12);

        //    // Tytuł dokumentu
        //    document.Add(new Paragraph("Protokół zdania pojazdu")
        //        .SetFont(boldFont)
        //        .SetFontSize(16)
        //        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
        //        .SetMarginBottom(20));

        //    // Szczegóły zlecenia
        //    AddDetail(document, "Zlecenie ID:", order.Id.ToString(), boldFont, regularFont);
        //    AddDetail(document, "Klient:", $"{order.Client.FirstName} {order.Client.LastName}", boldFont, regularFont);

        //    if (car != null)
        //    {
        //        AddDetail(document, "Samochód:", $"{car.Brand} {car.Model}, {car.ProductionYear}", boldFont, regularFont);
        //        AddDetail(document, "VIN:", car.Vin, boldFont, regularFont);
        //        AddDetail(document, "Numer rejestracyjny:", car.RegistrationNumber, boldFont, regularFont);
        //    }
        //    else
        //    {
        //        AddDetail(document, "Samochód:", "Brak danych", boldFont, regularFont);
        //        AddDetail(document, "VIN:", "Brak", boldFont, regularFont);
        //        AddDetail(document, "Numer rejestracyjny:", "Brak", boldFont, regularFont);
        //    }

        //    AddDetail(document, "Opis stanu pojazdu:", order.Handoverprotocol?.Description ?? "Brak opisu", boldFont, regularFont);
        //    AddDetail(document, "Pracownik odpowiedzialny:", $"{order.Employee?.FirstName} {order.Employee?.LastName}", boldFont, regularFont);

        //    document.Add(new Paragraph("Lista usług:").SetFont(boldFont).SetFontSize(14).SetMarginTop(20));
        //    foreach (var service in order.Services)
        //    {
        //        document.Add(new Paragraph($"- {service.Name}").SetMarginLeft(10));
        //    }

        //    // Dodawanie zdjęć pojazdu
        //    document.Add(new Paragraph("Zdjęcia pojazdu:").SetFont(boldFont).SetFontSize(14).SetMarginTop(20));

        //    string imageFolderPath = Path.Combine("wwwroot", "protocols", orderId.ToString());
        //    if (Directory.Exists(imageFolderPath))
        //    {
        //        var imageFiles = Directory.GetFiles(imageFolderPath)
        //            .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        //                           file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
        //                           file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        //            .ToArray();

        //        if (imageFiles.Length == 0)
        //        {
        //            document.Add(new Paragraph("Brak zdjęć pojazdu."));
        //        }
        //        else
        //        {
        //            foreach (var imageFile in imageFiles)
        //            {
        //                ImageData imageData = ImageDataFactory.Create(imageFile);
        //                Image img = new Image(imageData);

        //                // Skalowanie zdjęć, jeśli rozmiar przekracza 500 px
        //                if (img.GetImageWidth() > 500 || img.GetImageHeight() > 500)
        //                {
        //                    img = img.ScaleToFit(500, 500);
        //                }

        //                document.Add(img.SetMarginBottom(10));
        //            }
        //        }
        //    }
        //    else
        //    {
        //        document.Add(new Paragraph("Brak zdjęć pojazdu."));
        //    }

        //    document.Close();

        //    // Zapisanie ścieżki do bazy
        //    order.Handoverprotocol.ProtocolLink = $"/protocols/{orderId}/protocol_{orderId}.pdf";
        //    await _context.SaveChangesAsync();

        //    return Ok(new { Message = "Protokół został wygenerowany.", ProtocolLink = order.Handoverprotocol.ProtocolLink });
        //}

        //private void AddDetail(Document document, string label, string value, PdfFont boldFont, PdfFont regularFont)
        //{
        //    document.Add(new Paragraph()
        //        .Add(new Text(label).SetFont(boldFont))
        //        .Add(new Text(" " + value).SetFont(regularFont))
        //        .SetMarginBottom(5));
        //}

        [HttpPost("{orderId}/generate-pdf")]
        [Authorize(Policy = "RequireEmployeeRole")]
        public async Task<IActionResult> GeneratePdf(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Client)
                .Include(o => o.Employee)
                .Include(o => o.Services)
                .Include(o => o.Handoverprotocol)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound("Zlecenie nie istnieje.");
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.ClientId == order.ClientId);

            string protocolDirectory = Path.Combine("wwwroot", "protocols", orderId.ToString());
            Directory.CreateDirectory(protocolDirectory);

            string pdfPath = Path.Combine(protocolDirectory, $"protocol_{orderId}.pdf");
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

            // Tytuł dokumentu
            document.Add(new Paragraph("Protokół zdania pojazdu")
                .SetFont(boldFont)
                .SetFontSize(16)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Szczegóły zlecenia
            AddDetail(document, "Zlecenie ID:", order.Id.ToString(), boldFont, regularFont);
            AddDetail(document, "Klient:", $"{order.Client.FirstName} {order.Client.LastName}", boldFont, regularFont);

            if (car != null)
            {
                AddDetail(document, "Samochód:", $"{car.Brand} {car.Model}, {car.ProductionYear}", boldFont, regularFont);
                AddDetail(document, "VIN:", car.Vin, boldFont, regularFont);
                AddDetail(document, "Numer rejestracyjny:", car.RegistrationNumber, boldFont, regularFont);
            }
            else
            {
                AddDetail(document, "Samochód:", "Brak danych", boldFont, regularFont);
                AddDetail(document, "VIN:", "Brak", boldFont, regularFont);
                AddDetail(document, "Numer rejestracyjny:", "Brak", boldFont, regularFont);
            }

            AddDetail(document, "Opis stanu pojazdu:", order.Handoverprotocol?.Description ?? "Brak opisu", boldFont, regularFont);
            AddDetail(document, "Pracownik odpowiedzialny:", $"{order.Employee?.FirstName} {order.Employee?.LastName}", boldFont, regularFont);

            document.Add(new Paragraph("Lista usług:").SetFont(boldFont).SetFontSize(14).SetMarginTop(20));
            foreach (var service in order.Services)
            {
                document.Add(new Paragraph($"- {service.Name}").SetMarginLeft(10));
            }

            // Dodawanie zdjęć pojazdu w formie tabeli
            document.Add(new Paragraph("Zdjęcia pojazdu:").SetFont(boldFont).SetFontSize(14).SetMarginTop(20));

            string imageFolderPath = Path.Combine("wwwroot", "protocols", orderId.ToString());
            if (Directory.Exists(imageFolderPath))
            {
                var imageFiles = Directory.GetFiles(imageFolderPath)
                    .Where(file => file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                   file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                   file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (imageFiles.Length == 0)
                {
                    document.Add(new Paragraph("Brak zdjęć pojazdu."));
                }
                else
                {
                    // Tworzenie tabeli
                    var table = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();
                    foreach (var imageFile in imageFiles)
                    {
                        ImageData imageData = ImageDataFactory.Create(imageFile);
                        Image img = new Image(imageData);

                        // Skalowanie zdjęć, jeśli rozmiar przekracza 500 px
                        if (img.GetImageWidth() > 500 || img.GetImageHeight() > 500)
                        {
                            img = img.ScaleToFit(250, 250); // Dopasowanie do mniejszej komórki tabeli
                        }

                        // Dodanie zdjęcia do tabeli
                        Cell imageCell = new Cell().Add(img.SetAutoScale(true)).SetPadding(5);
                        table.AddCell(imageCell);

                        //// Dodanie podpisu pod zdjęciem
                        //Cell textCell = new Cell().Add(new Paragraph($"Zdjęcie {Array.IndexOf(imageFiles, imageFile) + 1}"))
                        //                          .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                        //table.AddCell(textCell);
                    }
                    document.Add(table);
                }
            }
            else
            {
                document.Add(new Paragraph("Brak zdjęć pojazdu."));
            }

            document.Close();

            // Zapisanie ścieżki do bazy
            order.Handoverprotocol.ProtocolLink = $"/protocols/{orderId}/protocol_{orderId}.pdf";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Protokół został wygenerowany.", ProtocolLink = order.Handoverprotocol.ProtocolLink });
        }

        private void AddDetail(Document document, string label, string value, PdfFont boldFont, PdfFont regularFont)
        {
            document.Add(new Paragraph()
                .Add(new Text(label).SetFont(boldFont))
                .Add(new Text(" " + value).SetFont(regularFont))
                .SetMarginBottom(5));
        }










    }
}

using Microsoft.AspNetCore.Mvc;
using Warsztat.Models;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using MySqlConnector;

namespace Warsztat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly WarsztatdbContext _context;
        private readonly IConfiguration _configuration;
        public AuthController(WarsztatdbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ClientRegisterDto clientDto)
        {
            // Sprawdź, czy email jest już używany
            if (_context.Clients.Any(c => c.Email == clientDto.Email))
            {
                return BadRequest("Użytkownik o podanym adresie email już istnieje.");
            }

            // Zaszyfruj hasło metodą BCrypt
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(clientDto.Password);

            // Utwórz nowy obiekt Client i przypisz zaszyfrowane hasło
            var client = new Client
            {
                FirstName = clientDto.FirstName,
                LastName = clientDto.LastName,
                Address = clientDto.Address,
                PhoneNumber = clientDto.PhoneNumber,
                Email = clientDto.Email,
                Password = hashedPassword
            };

            // Zapisz nowego klienta w bazie
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Rejestracja zakończona sukcesem" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] ClientLoginDto loginDto)
        {
            // Weryfikacja użytkownika
            var client = _context.Clients.FirstOrDefault(c => c.Email == loginDto.Email);
            if (client == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, client.Password))
            {
                return Unauthorized("Nieprawidłowy email lub hasło.");
            }

            // Generowanie tokena JWT
            var token = GenerateJwtToken(client);

            return Ok(new { Token = token });
        }

        // Metoda do generowania tokena JWT
        private string GenerateJwtToken(Client client)
        {
            var claims = new[]
            {
        new Claim("id", client.Id.ToString()),
        new Claim("email", client.Email),
        new Claim("role", "Client") // Dodajemy rolę Klienta
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddHours(10),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        //aktualizacja danych
        [Authorize]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] ClientUpdateDto updateDto)
        {
            
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound("Klient o podanym ID nie istnieje.");
            }
            int userIdFromToken = int.Parse(User.FindFirst("id").Value);

            if (userIdFromToken != id)
            {
                return Unauthorized("Nie masz uprawnień do tej operacji.");
            }

            // Aktualizacja pól (z wyłączeniem Email)
            client.FirstName = updateDto.FirstName ?? client.FirstName;
            client.LastName = updateDto.LastName ?? client.LastName;
            client.Address = updateDto.Address ?? client.Address;
            client.PhoneNumber = updateDto.PhoneNumber ?? client.PhoneNumber;

            // Zaktualizuj hasło, jeśli podano nowe
            if (!string.IsNullOrEmpty(updateDto.Password))
            {
                client.Password = BCrypt.Net.BCrypt.HashPassword(updateDto.Password);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Dane klienta zostały zaktualizowane." });
        }

        //usuwanie konta
        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound("Klient o podanym ID nie istnieje.");
            }
            int userIdFromToken = int.Parse(User.FindFirst("id").Value);

            if (userIdFromToken != id)
            {
                return Unauthorized("Nie masz uprawnień do tej operacji.");
            }


            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Konto klienta zostało pomyślnie usunięte." });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientById(int id)
        {
            var client = await _context.Clients
                .Select(c => new
                {
                    c.Id,
                    c.FirstName,
                    c.LastName,
                    c.Address,
                    c.PhoneNumber,
                    c.Email,
                    Password = c.Password // Wyświetlamy hasło w formie zaszyfrowanej
                })
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return NotFound("Klient o podanym ID nie istnieje.");
            }

            return Ok(client);
        }

        [HttpPost("verify-password")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> VerifyPassword([FromBody] VerifyPasswordDto verifyPasswordDto)
        {
            // Pobranie ID klienta z tokena JWT
            int userIdFromToken = int.Parse(User.FindFirst("id").Value);

            // Pobranie klienta z bazy danych
            var client = await _context.Clients.FindAsync(userIdFromToken);
            if (client == null)
            {
                return NotFound("Klient o podanym ID nie istnieje.");
            }

            // Weryfikacja hasła
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(verifyPasswordDto.Password, client.Password);
            if (!isPasswordValid)
            {
                return Unauthorized(new { Message = "Nieprawidłowe hasło." });
            }

            return Ok(new { Message = "Hasło jest prawidłowe." });
        }





    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Warsztat.Models;

namespace Warsztat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeAuthController : ControllerBase
    {
        private readonly WarsztatdbContext _context;
        private readonly IConfiguration _configuration;

        public EmployeeAuthController(WarsztatdbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] EmployeeLoginDto loginDto)
        {
            // Weryfikacja danych logowania
            var employee = _context.Employees.FirstOrDefault(e => e.Email == loginDto.Email);
            if (employee == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, employee.Password))
            {
                return Unauthorized("Nieprawidłowy email lub hasło.");
            }

            // Generowanie tokena JWT
            var token = GenerateJwtToken(employee);

            return Ok(new { Token = token });
        }

        // Generowanie tokena JWT
        private string GenerateJwtToken(Employee employee)
        {
            var claims = new[]
            {
        new Claim("id", employee.Id.ToString()),
        new Claim("email", employee.Email),
        new Claim("role", employee.IsManager == 1 ? "Manager" : "Employee")
            };
            


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(10),
                signingCredentials: creds);
            foreach (var role in User.Claims.Where(c => c.Type.Contains("role")))
            {
                Console.WriteLine($"Claim Type: {role.Type}, Value: {role.Value}");
            }



            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}

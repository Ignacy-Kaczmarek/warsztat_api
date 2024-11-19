using Microsoft.EntityFrameworkCore;
using Warsztat.Data;
using Warsztat.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);



var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, // Sprawdzanie wydawcy
        ValidateAudience = true, // Sprawdzanie odbiorcy
        ValidateLifetime = true, // Sprawdzanie wa�no�ci tokenu
        ValidateIssuerSigningKey = true, // Weryfikacja podpisu
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Ustawienia wydawcy
        ValidAudience = builder.Configuration["Jwt:Audience"], // Ustawienia odbiorcy
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"

    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireClientRole", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var roleClaim = context.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            Console.WriteLine($"RequireClientRole - Found Role: {roleClaim}");
            return roleClaim == "Client";
        });
    });

    options.AddPolicy("RequireEmployeeRole", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var roleClaim = context.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            Console.WriteLine($"RequireEmployeeRole - Found Role: {roleClaim}");
            return roleClaim == "Employee" || roleClaim == "Manager";
        });
    });

    options.AddPolicy("RequireManagerRole", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var roleClaim = context.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            Console.WriteLine($"RequireManagerRole - Found Role: {roleClaim}");
            return roleClaim == "Manager";
        });
    });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dodaj konfiguracj� DbContext, wskazuj�c connection string z sekret�w
builder.Services.AddDbContext<WarsztatdbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 4, 3))
    ));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });

    // Konfiguracja JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wprowad� token JWT w formacie 'Bearer {token}'"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


var app = builder.Build();
app.UseCors("AllowAllOrigins");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()|| app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

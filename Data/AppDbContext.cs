using Microsoft.EntityFrameworkCore;

namespace Warsztat.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Definicje DbSet dla każdej tabeli w bazie danych
        //public DbSet<Client> Clients { get; set; }
        //public DbSet<Car> Cars { get; set; }
        //public DbSet<Employee> Employees { get; set; }
        //public DbSet<Order> Orders { get; set; }
        //public DbSet<HandoverProtocol> HandoverProtocols { get; set; }
        //public DbSet<Service> Services { get; set; }
        //public DbSet<Part> Parts { get; set; }
        //public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}

namespace Warsztat.Models
{
    public class VehicleDto
    {
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int ProductionYear { get; set; }
        public string Vin { get; set; } = null!;
        public string RegistrationNumber { get; set; } = null!;
    }
}

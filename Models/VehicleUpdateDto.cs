namespace Warsztat.Models
{
    public class VehicleUpdateDto
    {
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int? ProductionYear { get; set; }
        public string? Vin { get; set; }
        public string? RegistrationNumber { get; set; }
    }
}

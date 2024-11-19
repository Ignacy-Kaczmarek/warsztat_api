namespace Warsztat.Models
{
    public class ReservationRequestDto
    {
        public int CarId { get; set; }
        public List<int> ServiceIds { get; set; } = new List<int>();
        public DateTime PreferredStartDate { get; set; }
    }
}

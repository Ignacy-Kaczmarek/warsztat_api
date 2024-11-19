namespace Warsztat.Models
{
    public class UpdateReservationDto
    {
        
            public string? Status { get; set; }
            public int? EmployeeId { get; set; }
            public string? Comment { get; set; }
            public sbyte? PaymentStatus { get; set; }
            public string? InvoiceLink { get; set; }
        

    }

}

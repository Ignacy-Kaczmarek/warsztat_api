using System;
using System.Collections.Generic;

namespace Warsztat.Models;

public partial class Order
{
    public int Id { get; set; }

    public DateTime StartDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Comment { get; set; }

    public sbyte PaymentStatus { get; set; }

    public int? EmployeeId { get; set; }

    public string? InvoiceLink { get; set; }

    public int ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Employee? Employee { get; set; }

    public virtual Handoverprotocol? Handoverprotocol { get; set; }

    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}

using System;
using System.Collections.Generic;

namespace Warsztat.Models;

public partial class Car
{
    public int Id { get; set; }

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int ProductionYear { get; set; }

    public string Vin { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    public int ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

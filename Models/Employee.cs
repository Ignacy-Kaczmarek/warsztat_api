﻿using System;
using System.Collections.Generic;

namespace Warsztat.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public sbyte IsManager { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

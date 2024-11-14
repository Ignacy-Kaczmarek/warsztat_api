using System;
using System.Collections.Generic;

namespace Warsztat.Models;

public partial class Handoverprotocol
{
    public int OrderId { get; set; }

    public string? Description { get; set; }

    public string? PictureLink { get; set; }

    public virtual Order Order { get; set; } = null!;
}

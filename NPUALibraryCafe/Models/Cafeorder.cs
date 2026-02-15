using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Cafeorder
{
    public int Orderid { get; set; }

    public int Userid { get; set; }

    public DateTime Orderdate { get; set; }

    public decimal Totalamount { get; set; }

    public string Ordertype { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual ICollection<Cafeorderitem> Cafeorderitems { get; set; } = new List<Cafeorderitem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Payment
{
    public int Paymentid { get; set; }

    public int Orderid { get; set; }

    public int Userid { get; set; }

    public decimal Amount { get; set; }

    public string? Paymentmethod { get; set; }

    public DateTime Paymentdate { get; set; }

    public virtual Cafeorder Order { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

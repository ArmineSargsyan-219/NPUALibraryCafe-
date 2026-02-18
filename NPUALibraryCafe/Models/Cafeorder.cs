using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

public partial class Cafeorder
{
    public int Orderid { get; set; }

    public int Userid { get; set; }

    public DateTime Orderdate { get; set; }

    public decimal Totalamount { get; set; }

    public string Ordertype { get; set; } = null!;

    public string Status { get; set; } = null!;

    [Column("notifiedat")]
    public DateTime? Notifiedat { get; set; }

    [Column("confirmedat")]
    public DateTime? Confirmedat { get; set; }

    [Column("completedat")]
    public DateTime? Completedat { get; set; }

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    public virtual ICollection<Cafeorderitem> Cafeorderitems { get; set; } = new List<Cafeorderitem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User User { get; set; } = null!;
}

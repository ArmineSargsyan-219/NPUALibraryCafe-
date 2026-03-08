using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

[Table("orders")]
public partial class Cafeorder
{
    [Key]
    [Column("id")]
    public int Orderid { get; set; }

    [Column("user_id")]
    public int Userid { get; set; }

    [Column("items")]
    public string? Items { get; set; }

    [Column("total_price")]
    public decimal Totalamount { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("order_time")]
    public DateTime? Orderdate { get; set; }

    [Column("ready_time")]
    public DateTime? Readytime { get; set; }

    [Column("completed_time")]
    public DateTime? Completedat { get; set; }

    [Column("history_time")]
    public DateTime? Historytime { get; set; }

    [Column("created_at")]
    public DateTime? Createdat { get; set; }

    [Column("updated_at")]
    public DateTime? Updatedat { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual User User { get; set; } = null!;
}
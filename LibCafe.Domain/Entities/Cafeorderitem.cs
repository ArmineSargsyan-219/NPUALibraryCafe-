using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace  LibCafe.Domain.Entities;

[Table("cart_items")]
public partial class Cafeorderitem
{
    [Key]
    [Column("id")]
    public int Orderitemid { get; set; }

    [Column("user_id")]
    public string? Userid { get; set; }

    [Column("menu_item_id")]
    public string? Itemid { get; set; }

    [Column("size")]
    public string? Size { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at")]
    public DateTime? Createdat { get; set; }
}
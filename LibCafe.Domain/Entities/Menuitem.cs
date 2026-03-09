using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace  LibCafe.Domain.Entities;

[Table("menu_items")]
public partial class Menuitem
{
    [Key]
    [Column("id")]
    public string Itemid { get; set; } = null!;

    [Column("name")]
    public string Itemname { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category_id")]
    public string? CategoryId { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("image")]
    public string? Imagepath { get; set; }

    [Column("available")]
    public bool Available { get; set; } = true;

    [Column("rating")]
    public decimal? Rating { get; set; }
}
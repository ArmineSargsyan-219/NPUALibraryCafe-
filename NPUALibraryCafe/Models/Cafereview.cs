using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

[Table("cafereviews")]
public partial class Cafereview
{
    [Key]
    [Column("reviewid")]
    public int Reviewid { get; set; }

    [Column("userid")]
    public int Userid { get; set; }

    [Column("itemid")]
    public string? Itemid { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    public virtual User User { get; set; } = null!;
}
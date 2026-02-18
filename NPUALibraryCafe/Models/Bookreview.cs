using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

public partial class Bookreview
{
    [Key]
    [Column("reviewid")]
    public int Reviewid { get; set; }

    [Column("userid")]
    public int Userid { get; set; }

    [Column("bookid")]
    public int Bookid { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    // ✨ ADD THIS - the missing property causing the error:
    [Column("createdat")]
    public DateTime Createdat { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
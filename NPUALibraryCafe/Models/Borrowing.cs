using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

public partial class Borrowing
{
    [Key]
    [Column("borrowingid")]
    public int Borrowingid { get; set; }

    [Column("userid")]
    public int Userid { get; set; }

    [Column("bookid")]
    public int Bookid { get; set; }

    [Column("borrowdate")]
    public DateTime Borrowdate { get; set; }

    [Column("duedate")]
    public DateTime Duedate { get; set; }

    [Column("returndate")]
    public DateTime? Returndate { get; set; }

    // Navigation properties
    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
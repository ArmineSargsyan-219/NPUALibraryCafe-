using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

public partial class Book
{
    public int Bookid { get; set; }

    public string Title { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string? Category { get; set; }

    public string? Isbn { get; set; }

    public string Bookshelf { get; set; } = null!;

    // ✨ NEW PROPERTIES - Add these:
    [Column("shelfnumber")]
    public string? Shelfnumber { get; set; }

    [Column("physicalcopies")]
    public int Physicalcopies { get; set; } = 1;

    [Column("availablecopies")]
    public int Availablecopies { get; set; } = 1;

    [Column("pdfurl")]
    public string? Pdfurl { get; set; }

    [Column("pdfavailable")]
    public bool Pdfavailable { get; set; } = false;

    [Column("imagepath")]
    public string? Imagepath { get; set; }

    // Navigation properties stay at the end
    public virtual ICollection<Bookreview> Bookreviews { get; set; } = new List<Bookreview>();

    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

[Table("books")]
public partial class Book
{
    [Key]
    [Column("bookid")]
    public int Bookid { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("author")]
    public string Author { get; set; } = null!;

    [Column("category")]
    public string? Category { get; set; }

    [Column("isbn")]
    public string? Isbn { get; set; }

    [Column("bookshelf")]
    public string? Bookshelf { get; set; }

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

    public virtual ICollection<Bookreview> Bookreviews { get; set; } = new List<Bookreview>();
    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
}
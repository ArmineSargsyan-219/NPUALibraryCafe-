using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Book
{
    public int Bookid { get; set; }

    public string Title { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string? Category { get; set; }

    public string? Isbn { get; set; }

    public string Bookshelf { get; set; } = null!;

    public virtual ICollection<Bookreview> Bookreviews { get; set; } = new List<Bookreview>();

    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
}

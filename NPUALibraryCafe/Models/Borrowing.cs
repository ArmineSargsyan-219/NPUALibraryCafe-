using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

[Table("borrowed_books")]
public partial class Borrowing
{
    [Key]
    [Column("id")]
    public int Borrowingid { get; set; }

    [Column("user_id")]
    public int Userid { get; set; }

    [Column("book_id")]
    public int Bookid { get; set; }

    [Column("book_title")]
    public string? BookTitle { get; set; }

    [Column("book_author")]
    public string? BookAuthor { get; set; }

    [Column("borrowed_at")]
    public DateTime? Borrowdate { get; set; }

    [Column("due_date")]
    public DateTime? Duedate { get; set; }

    [Column("returned_at")]
    public DateTime? Returndate { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    public virtual Book Book { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
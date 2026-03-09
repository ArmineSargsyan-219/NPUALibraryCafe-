using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace  LibCafe.Domain.Entities;

[Table("users")]
public partial class User
{
    [Key]
    [Column("id")]
    public int Userid { get; set; }

    [Column("name")]
    public string Fullname { get; set; } = null!;

    [Column("email")]
    public string Email { get; set; } = null!;

    [Column("password")]
    public string Passwordhash { get; set; } = null!;

    [Column("role")]
    public string Role { get; set; } = null!;

    [Column("push_token")]
    public string? Pushtoken { get; set; }

    [Column("platform")]
    public string? Platform { get; set; }

    public virtual ICollection<Bookreview> Bookreviews { get; set; } = new List<Bookreview>();
    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
    public virtual ICollection<Cafeorder> Cafeorders { get; set; } = new List<Cafeorder>();
    public virtual ICollection<Cafereview> Cafereviews { get; set; } = new List<Cafereview>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
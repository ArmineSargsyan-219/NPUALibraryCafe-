using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class User
{
    public int Userid { get; set; }

    public string Fullname { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<Bookreview> Bookreviews { get; set; } = new List<Bookreview>();

    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

    public virtual ICollection<Cafeorder> Cafeorders { get; set; } = new List<Cafeorder>();

    public virtual ICollection<Cafereview> Cafereviews { get; set; } = new List<Cafereview>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

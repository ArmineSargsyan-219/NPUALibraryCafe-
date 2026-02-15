using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Borrowing
{
    public int Borrowid { get; set; }

    public int Userid { get; set; }

    public int Bookid { get; set; }

    public DateOnly Borrowdate { get; set; }

    public DateOnly? Returndate { get; set; }

    public DateOnly Duedate { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

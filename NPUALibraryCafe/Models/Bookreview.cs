using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Bookreview
{
    public int Reviewid { get; set; }

    public int Userid { get; set; }

    public int Bookid { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

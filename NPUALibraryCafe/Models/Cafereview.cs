using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Cafereview
{
    public int Reviewid { get; set; }

    public int Userid { get; set; }

    public int Itemid { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public virtual Menuitem Item { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

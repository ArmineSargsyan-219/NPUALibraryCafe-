using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPUALibraryCafe.Models;

public partial class Menuitem
{
    public int Itemid { get; set; }

    public string Itemname { get; set; } = null!;

    public string Category { get; set; } = null!;

    public decimal Price { get; set; }

    // ✨ NEW PROPERTY - Add this:
    [Column("imagepath")]
    public string? Imagepath { get; set; }

    // Navigation properties stay at the end
    public virtual ICollection<Cafeorderitem> Cafeorderitems { get; set; } = new List<Cafeorderitem>();

    public virtual ICollection<Cafereview> Cafereviews { get; set; } = new List<Cafereview>();
}
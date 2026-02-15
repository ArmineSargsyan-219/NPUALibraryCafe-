using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Cafeorderitem
{
    public int Orderitemid { get; set; }

    public int Orderid { get; set; }

    public int Itemid { get; set; }

    public int Quantity { get; set; }

    public virtual Menuitem Item { get; set; } = null!;

    public virtual Cafeorder Order { get; set; } = null!;
}

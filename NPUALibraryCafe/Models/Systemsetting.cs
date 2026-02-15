using System;
using System.Collections.Generic;

namespace NPUALibraryCafe.Models;

public partial class Systemsetting
{
    public int Settingid { get; set; }

    public string Settingname { get; set; } = null!;

    public string? Settingvalue { get; set; }
}

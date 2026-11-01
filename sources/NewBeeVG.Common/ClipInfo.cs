using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG;

public class ClipInfo
{
    public bool ContainsVisualContent { get; set; }
    public int Frames { get; set; }
    public string Name { get; set; } = String.Empty;
}

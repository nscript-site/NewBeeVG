using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Internal;

internal class ClipInfo
{
    public bool ContainsVisualContent { get; set; }
    public int Frames { get; set; }
    public string Name { get; set; } = String.Empty;
}

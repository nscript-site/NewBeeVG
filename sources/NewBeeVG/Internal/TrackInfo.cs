using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Internal;

internal class TrackInfo
{
    public int Frames { get; set; }
    public string Name { get; set; } = String.Empty;
    public List<ClipInfo> Clips { get; set; } = new();
}

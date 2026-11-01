using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewBeeVG;

[JsonSerializable(typeof(TrackInfo))]
public partial class TrackInfoJsonContext : JsonSerializerContext
{
}

public class TrackInfo
{
    public int Frames { get; set; }
    public string Name { get; set; } = String.Empty;
    public List<ClipInfo> Clips { get; set; } = new();

    public override string ToString()
    {
        // 序列化为 json
        return JsonSerializer.Serialize(this, TrackInfoJsonContext.Default.TrackInfo);
    }

    public List<ClipInfo> GetVisualClips()
    {
        var list = new List<ClipInfo>();
        foreach (var item in Clips)
            if (item.ContainsVisualContent == true) list.Add(item);
        return list;
    }
}

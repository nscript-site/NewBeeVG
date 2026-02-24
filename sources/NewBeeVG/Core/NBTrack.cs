namespace NewBeeVG;

public class NBTrack
{
    public string Name { get; set; } = "Track";
    public List<NBClip> Clips { get; set; } = new List<NBClip>();
    public bool IsVisible { get; set; } = true;

    public void Measure()
    {

    }
}

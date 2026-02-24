namespace NewBeeVG.Core;

public class NBWork
{
    public string Name { get; set; } = "Work";
    public NBStage Stage { get; set; } = new NBStage();
    public List<NBTrack> Tracks { get; set; } = new List<NBTrack>();
}

namespace NewBeeVG;

public class NBWorkspace
{
    public static NBWorkspace? Current { get; set; } = null;

    public string Name { get; set; } = "workspace";
    public List<NBWork> Works { get; set; } = new List<NBWork>();

    public static NBWorkspace Create(NBStage stage, NBClip[] clips)
    {
        var track = new NBTrack();
        track.Clips.AddRange(clips);

        return Create(stage, [track]);
    }

    public static NBWorkspace Create(NBStage stage, NBTrack[] tracks)
    {
        var work = new NBWork()
        {
            Stage = stage
        };
        work.Tracks.AddRange(tracks);

        var ws = new NBWorkspace { Name = "Workspace" };
        ws.Works.Add(work);
        return ws;
    }

    public static NBWorkspace Create(NBWork[] works)
    {
        var ws = new NBWorkspace { Name = "Workspace" };
        ws.Works.AddRange(works);
        return ws;
    }
}

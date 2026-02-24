namespace NewBeeVG.Viewer.Widgets;

public class WorkNodeView : BaseView
{
    public NBWork WorkNode { get; set; } = default!;

    public Action<IPlayable, NBWork>? OnPlayableClicked { get; set; }

    protected override void Build(out Control content)
    {
        var arrs = new List<Control>();
        
        foreach(var track in WorkNode.Tracks)
        {
            var trackView = new TrackNodeView
            {
                TrackNode = track,
                OnTrackClicked = n => { OnPlayableClicked?.Invoke(n, WorkNode); }
            };
            arrs.Add(trackView);
            foreach(var clip in track.Clips)
            {
                var clipView = new ClipNodeView
                {
                    ClipNode = clip,
                    OnClipClicked = n => { OnPlayableClicked?.Invoke(n, WorkNode); }
                };
                arrs.Add(clipView);
            }
        }

        VStack(arrs.ToArray())
            .Align(-1,-1)
            .Return(out content);
    }
}

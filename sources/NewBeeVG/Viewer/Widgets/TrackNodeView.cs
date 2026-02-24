namespace NewBeeVG.Viewer.Widgets;

public class TrackNodeView : BaseView
{
    public NBTrack TrackNode { get; set; } = default!;

    public Action<NBTrack>? OnTrackClicked { get; set; }

    protected override void Build(out Control content)
    {
        TextButton($"{TrackNode.Name}").Align(null)
            .OnClick(() => OnTrackClicked?.Invoke(TrackNode))
            .Return(out content);
    }
}

namespace NewBeeVG.Viewer.Widgets;

public class ClipNodeView : BaseView
{
    public NBClip ClipNode { get; set; } = default!;

    public Action<NBClip>? OnClipClicked { get; set; }

    protected override void Build(out Control content)
    {
        TextButton($"{ClipNode.Name}").Align(null)
            .OnClick(() => OnClipClicked?.Invoke(ClipNode))
            .Return(out content);
    }
}
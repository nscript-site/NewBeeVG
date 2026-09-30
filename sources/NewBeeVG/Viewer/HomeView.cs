using Avalonia.Interactivity;

namespace NewBeeVG.Viewer;

public class HomeView : BaseView
{
    private PlayerView? Player = default;

    private NBWork? Work;

    protected override void Build(out Control content)
    {
        Work = NBWorkspace.Current?.Works.FirstOrDefault() ?? new NBWork();
        Player = new PlayerView();

        HGrid("140,10,*", [
                new WorkNodeView
                {
                    WorkNode = Work,
                    OnPlayableClicked = LoadPlayable,
                }.Scrollable(),
                null,
                Player,
            ]).Return(out content);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var playable = Work?.Tracks.FirstOrDefault();
        if(playable != null && Work != null)
        {
            LoadPlayable(playable, Work);
        }
    }

    protected void LoadPlayable(IPlayable playable, NBWork work)
    {
        playable.Reset();
        Player?.Stop();
        Player?.Load(playable, work);
    }
}
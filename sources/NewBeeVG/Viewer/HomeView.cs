namespace NewBeeVG.Viewer;

public class HomeView : BaseView
{
    private PlayerView? Player = default;

    protected override void Build(out Control content)
    {
        Player = new PlayerView();

        HGrid("100,*", [
                new WorkNodeView
                {
                    WorkNode = NBWorkspace.Current?.Works.FirstOrDefault()??new NBWork(),
                    OnPlayableClicked = LoadPlayable,
                },
                Player,
            ]).Return(out content);
    }

    protected void LoadPlayable(IPlayable playable, NBWork work)
    {
        Player?.Load(playable, work);
    }
}
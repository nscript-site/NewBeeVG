namespace NewBeeVG.Viewer;

public class HomeView : BaseView
{
    protected override void Build(out Control content)
    {
        VStack([
               TextBlock("Hello, NewBee VG")
            ]).Align(0,0).Spacing(32).Return(out content);
    }
}
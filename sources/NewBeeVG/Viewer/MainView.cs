namespace NewBeeVG.Viewer;

public class MainView : BaseView, IWindowView
{
    TextBlock? SubtitleTextBlock;

    #region IWindowView

    public WindowInfo WindowInfo { get; }

    protected List<RoutedViewBuilder>? RoutedViewBuilders = null;

    protected WindowInfo CreateWindowInfo()
    {
        return new NWindowInfo()
        {
            WindowTitle = "NewBee VG",
            CanResize = true,
            CanMinimize = true,
            CanClose = true,
            WindowMinWidth = 400,
            WindowMinHeight = 500,
            WindowWidth = 1200,
            WindowHeight = 800,
            IsWindowAnimationEnable = true,
            Subtitle = BuildSubtitle(),
            RightWindowsBar = HStack([
                this.CreateWindowIcon(CogOutlineIcon.Instance).OnClick(_=>{ new SettingView().ShowDialog("设置"); }),
            ]).Ref(out var rightBar)!,
        };
    }

    protected Control BuildSubtitle()
    {
        var tb = new TextBlock().Ref(out SubtitleTextBlock)!.TextTrimming(TextTrimming.CharacterEllipsis)
                .FontSize(12).Align(null, 0);
        var grid = HGrid("*", [tb]).ClipToBounds(true);
        return grid;
    }

    #endregion

    public MainView() : base()
    {
        this.WindowInfo = CreateWindowInfo();
    }

    #region build

    protected override void Build(out Control content)
    {
        HGrid("*", [new HomeView().Ref(out var homeView)!]).Margin(10)
            .Return(out content);
    }

    #endregion
}
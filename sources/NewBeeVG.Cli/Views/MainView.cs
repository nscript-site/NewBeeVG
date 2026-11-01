using Avalonia.Interactivity;
using NewBeeVG.Cli.Tools;
using NewBeeVG.Cli.Tools.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Cli.Views;

public class MainView : BaseView, IWindowView
{
    TextBlock? SubtitleTextBlock;

    public static string ProgramSubName { get; set; } = "";

    internal static FileWatcher Watcher { get; set; } = default!;

    #region IWindowView

    public WindowInfo WindowInfo { get; }

    private string? ClipName = "clip1";

    protected List<RoutedViewBuilder>? RoutedViewBuilders = null;

    protected WindowInfo CreateWindowInfo()
    {
        return new NWindowInfo()
        {
            WindowTitle = $"nbvg watch{ProgramSubName}",
            CanResize = true,
            CanMinimize = true,
            CanClose = true,
            WindowMinWidth = 400,
            WindowMinHeight = 500,
            WindowWidth = 1200,
            WindowHeight = 800, StartupLocation = WindowStartupLocation.Manual,
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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Watcher.OnChanged = _ => OnReload();
    }

    #endregion

    public MainView() : base()
    {
        this.WindowInfo = CreateWindowInfo();
    }

    #region build

    protected override void Build(out Control content)
    {
        VGrid("50,*", [
            HStack([
                IconButton(FlashIcon.Instance, "重新加载当前片段").OnClick(OnReload),
                IconButton(ReloadIcon.Instance, "重新加载当前视频").OnClick(OnReloadVideo),
                IconButton(MotionPlayIcon.Instance, "播放当前片段").OnClick(PlayClip),
                IconButton(VideoVintageIcon.Instance, "播放当前视频").OnClick(PlayVideo),
                ]).Background(Brushes.LightGray).Spacing(0).Align(0,0),
            Panel().Background(Brushes.LightGray)
            ]).Margin(2)
            .Return(out content);
    }

    #endregion

    private void PlayClip()
    {
        if (String.IsNullOrEmpty(ClipName)) return;

        var fileInfo = new FileInfo(Watcher.FilePath);
        var dir = fileInfo.Directory;
        if (dir == null) return;

        Utils.RunCommandDetached("dotnet", $"\"{fileInfo.Name}\" -- \"{ClipName}\"", dir.FullName);
    }

    private void OnReload()
    {
        var png = WatchTool.DumpPNG(Watcher.FilePath, "clip1", 0);
        Console.WriteLine(png);
    }

    private void OnReloadVideo()
    {
        Watcher.Track = WatchTool.DumpTrackInfo(Watcher.FilePath);
    }

    private void PlayVideo()
    {
        var fileInfo = new FileInfo(Watcher.FilePath);
        var dir = fileInfo.Directory;
        if (dir == null) return;

        Utils.RunCommandDetached("dotnet", $"\"{fileInfo.Name}\"", dir.FullName);
    }
}
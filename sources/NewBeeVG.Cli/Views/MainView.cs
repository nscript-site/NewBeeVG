using Avalonia.Interactivity;
using NewBeeVG.Cli.Tools;
using NewBeeVG.Cli.Tools.Internal;

namespace NewBeeVG.Cli.Views;

public class MainView : BaseView, IWindowView
{
    TextBlock? SubtitleTextBlock;

    public static string ProgramSubName { get; set; } = "";

    internal static FileWatcher Watcher { get; set; } = default!;

    internal TrackInfo? Track => Watcher.Track;

    #region IWindowView

    public WindowInfo WindowInfo { get; }

    private string? ClipName = null;
    private int CurrentFrame = 0;
    private int Frames = 100;

    private string ErrorMessage = String.Empty;

    private readonly SingleFlight _reloadFlight = new();
    private readonly SingleFlight _reloadVideoFlight = new();
    private readonly SingleFlight _playClipFlight = new();
    private readonly SingleFlight _playVideoFlight = new();

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
        Watcher.OnChanged = _ => OnReloadAtBackground();
        UpdateClips();
        this.UpdateStateByUIThread();
        OnReloadAtBackground();
    }

    #endregion

    public MainView() : base()
    {
        this.WindowInfo = CreateWindowInfo();
    }

    #region build
    private NBSkiaBitmapView FrameImage = default!;

    private string LoadingText = "加载中 ...";

    private bool IsErrorMsgVisible()
    {
        return String.IsNullOrEmpty(ErrorMessage) == false;
    }

    protected override void Build(out Control content)
    {
        FrameImage = new NBSkiaBitmapView();
        VGrid("50,*, 50", [
            HStack([
                FilterButton(()=>$"{ClipName ?? "请选择预览的片段"}",BuildClipsFlyout()),
                IconButton(FlashIcon.Instance, "重新加载当前片段").OnClick(OnReloadAtBackground),
                IconButton(ReloadIcon.Instance, "重新加载当前视频").OnClick(OnReloadVideoBackground),
                IconButton(MotionPlayIcon.Instance, "播放当前片段").OnClick(PlayClipAtBackground),
                IconButton(VideoVintageIcon.Instance, "播放当前视频").OnClick(PlayVideoAtBackground),
                ]).Background(Brushes.LightGray).Spacing(0).Align(0,0),
            Panel([
                Border(FrameImage)
                    .Background(Brushes.Gray)
                    .BorderBrush(Brushes.LightGray)
                    .BorderThickness(1).IsVisible(()=>!IsErrorMsgVisible()),
                Border(TextBlock(()=>ErrorMessage).Foreground(Brushes.Red).Align(0,0))
                    .Background(Brushes.Gray)
                    .BorderBrush(Brushes.LightGray)
                    .BorderThickness(1).Align(null,null)
                    .IsVisible(()=>IsErrorMsgVisible())
                ]),
            HGrid("Auto,*,Auto",[
                HStack([
                    IconButton(SkipPreviousIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(0)),
                    IconButton(ArrowLeftBoldBoxIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(CurrentFrame - 1)),
                    IconButton(ArrowRightBoldBoxIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(CurrentFrame + 1)),
                    IconButton(SkipNextIcon.Instance,"", scale: 1, iconSize:20).Size(26,26)
                        .OnClick(_=>GotoFrame(Frames)),
                    ]).Margin(10,0).Spacing(0),
                new ProgressBar().Value(()=>CurrentFrame).Maximum(()=>Math.Max(0,Frames-1)).Ref(out var progress)
                    .Align(null,0).Cursor(new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand))
                    .OnPointerPressed(e=>{
                        if(Frames <= 0) return;
                        var p = e.GetPosition(progress);
                        var w = progress.Bounds.Width;
                        var f = (int)Math.Round(p.X / w * Frames);
                        GotoFrame(f);
                    }),
                TextBlock(() =>$"{CurrentFrame+1}/{Frames}").Align(1,0).Margin(10,0)
                ])
            ]).Margin(2)
            .Return(out content);
    }

    private Button? FlyoutButton;

    protected Control FilterButton(Func<String> labelFunc, Control? flyoutContent = null, Func<bool>? isClearIconVisibleFunc = null, Action? onClear = null)
    {
        var labelButton = new Button()
                .Content(
                    HStack([
                        new TextBlock().Text(labelFunc).Foreground(R_TextColor),
                        PathIcon(ChevronDownIcon.Instance)
                            .Scale(0.3).Align(-1,0).Margin(-14,0,5,0)
                        ])
                )
                .Flyout(flyoutContent).BasicStyle();
        FlyoutButton = labelButton;
        return Panel([labelButton]).Height(32);
    }

    protected Border ActiveLine(string? cols, Control?[] controls)
    {
        return new Border()
            .Classes("active").CornerRadius(20)
            .Padding(10, 0)
            .Child(HGrid(cols, controls));
    }

    protected Control BuildClipsFlyout()
    {
        var clips = Watcher.Track?.GetVisualClips() ?? new List<ClipInfo>();
        if (clips.Count == 0)
        {
            return VStack([TextBlock("没有可选片段")]).Margin(20);
        }

        Control[] BuildRadioButtons()
        {
            var grids = new Control[clips.Count];
            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                var button = new RadioButton()
                        .GroupName("clipsGroup")
                        .IsChecked(() => ClipName == clip.Name)
                        .Content(new TextBlock().Text(clip.Name).FontSize(14));
                var g = ActiveLine("*", [button])
                    .OnTapped(e => { e.Handled = true; this.Switch(clip); });
                grids[i] = g;
            }
            return grids;
        }
        return new ScrollViewer().MaxHeight(500)
                .Content(WrapPanel(BuildRadioButtons()).Margin(20));
    }
    #endregion

    private void Switch(ClipInfo clip)
    {
        FlyoutButton?.Flyout?.Hide();

        if (clip.Name == ClipName) return;

        this.UpdateStateByUIThread();
        ClipName = clip.Name;
        Frames = clip.Frames;
        CurrentFrame = 0;
        this.OnReloadAtBackground();
    }

    private void ShowLoadingText()
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ErrorMessage = "加载中";
            FrameImage.Bitmap = null;
            FrameImage.InvalidateVisual();
            this.UpdateState();
        });
    }

    private void ShowErrorMessage(string msg)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ErrorMessage = msg;
            FrameImage.Bitmap = null;
            FrameImage.InvalidateVisual();
            this.UpdateState();
        });
    }

    private void OnReloadAtBackground()
    {
        ShowLoadingText();
        _reloadFlight.Run("重新加载", OnReload);
    }

    private void OnReload()
    {
        if(ClipName != null)
        {
            try
            {
                var bmp = WatchTool.DumpPNG(Watcher.FilePath, ClipName, Clamp(CurrentFrame));

                if (bmp != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ErrorMessage = String.Empty;
                        FrameImage.Bitmap = bmp;
                        FrameImage.InvalidateVisual();
                        this.UpdateState();
                    });
                }
                else
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ErrorMessage = "加载失败: 无法获取对应画面，请检查代码是否存在问题或指定片段是否存在";
                        FrameImage.Bitmap = null;
                        FrameImage.InvalidateVisual();
                        this.UpdateState();
                    });
                }
            }
            catch(Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }
        else
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ErrorMessage = "加载失败: 请选择预览的片段";
                FrameImage.Bitmap = null;
                FrameImage.InvalidateVisual();
                this.UpdateState();
            });
        }
    }

    private void OnReloadVideoBackground()
    {
        ShowLoadingText();
        _reloadVideoFlight.Run("重新加载视频", OnReloadVideo);
    }

    private void OnReloadVideo()
    {
        try
        {
            var track = WatchTool.DumpTrackInfo(Watcher.FilePath);
            ClipInfo? match = null;
            if (track != null)
            {
                var clips = track.GetVisualClips();
                foreach (var item in clips)
                {
                    if (item.Name == ClipName)
                    {
                        match = item;
                        break;
                    }
                }
            }
            if (match != null)
            {
                Frames = match.Frames;
                CurrentFrame = Clamp(CurrentFrame);
            }
            else
            {
                var first = track?.Clips.First();
                if (first == null)
                {
                    ClipName = null;
                    Frames = 0;
                    CurrentFrame = 0;
                }
                else
                {
                    ClipName = first.Name;
                    Frames = first.Frames;
                    CurrentFrame = 0;
                }
            }
            Watcher.Track = track;
            this.OnReloadAtBackground();
        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }

    private void UpdateClips()
    {
        var clips = Track?.GetVisualClips();
        if(clips?.Count > 0)
        {
            var first = clips.First();
            ClipName = first.Name;
            Frames = first.Frames;
            CurrentFrame = 0;
        }
        else
        {
            ClipName = null;
            Frames = 0;
            CurrentFrame = 0;
        }
    }

    private void PlayClipAtBackground()
    {
        _playClipFlight.Run("播放片段", PlayClip,1);
    }

    private void PlayClip()
    {
        if (String.IsNullOrEmpty(ClipName)) return;

        var fileInfo = new FileInfo(Watcher.FilePath);
        var dir = fileInfo.Directory;
        if (dir == null) return;

        Utils.RunCommandDetached("dotnet", $"\"{fileInfo.Name}\" -- \"{ClipName}\"", dir.FullName);
    }

    private void PlayVideoAtBackground()
    {
        _playVideoFlight.Run("播放视频", PlayVideo,1);
    }

    private void PlayVideo()
    {
        var fileInfo = new FileInfo(Watcher.FilePath);
        var dir = fileInfo.Directory;
        if (dir == null) return;

        Utils.RunCommandDetached("dotnet", $"\"{fileInfo.Name}\"", dir.FullName);
    }

    private int Clamp(int frame)
    {
        frame = Math.Max(0, frame);
        frame = Math.Min(Frames - 1, frame);
        return frame;
    }

    public void GotoFrame(int frame)
    {
        CurrentFrame = Clamp(frame);
        OnReloadAtBackground();
    }

    /// <summary>
    /// 同一个实例的 action 不会并发执行；执行期间的新请求只保留一次尾随执行。
    /// </summary>
    private sealed class SingleFlight
    {
        private readonly object _gate = new();
        private bool _running;
        private int _version;

        private DateTime _last = new DateTime(2000, 1, 1);
        public void Run(string name, Action action, double minClickInterval = 0)
        {
            var now = DateTime.Now;
            var ts = now - _last;
            if (Math.Abs(ts.TotalSeconds) < minClickInterval)
                return;

            _last = now;

            var version = Interlocked.Increment(ref _version);

            lock (_gate)
            {
                if (_running) return;
                _running = true;
            }

            _ = Task.Run(() => Loop(name, action, version));
        }

        private void Loop(string name, Action action, int version)
        {
            try
            {
                while (true)
                {
                    action();

                    lock (_gate)
                    {
                        var latest = Volatile.Read(ref _version);
                        if (latest == version)
                        {
                            _running = false;
                            return;
                        }

                        version = latest;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{name}失败: {ex.Message}");
                lock (_gate)
                {
                    _running = false;
                }
            }
        }
    }
}
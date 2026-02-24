using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NewBeeVG.Viewer;

namespace NewBeeVG;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        //this.Styles.AddRange(GlobalStyles.BuildStyles());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        //this.Styles.Add(new NStyles.NTheme());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
#if DEBUG
            this.AttachDevTools();
#endif
            new MainView().ShowDialog();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static void Start(string[]? args = null)
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();

        if(args == null) args = new string[0];
        builder.StartWithClassicDesktopLifetime(args);
    }
}

using NewBeeVG.Core;

namespace NewBeeVG;

public static class Methods
{
    public static NBStage stage(int width = 1080, int height = 1920, int dpi = 96)
    {
        return new NBStage
        {
            Width = width,
            Height = height,
            Dpi = dpi
        };
    }

    public static NBWorkspace workspace()
    {
        return new NBWorkspace();
    }

    public static NBWork project()
    {
        return new NBWork();
    }

    public static NBClip clip(string name = "clip", Func<NBBuildContext, NBClip, Control?>? builder = null, int frames = 1, int? start = null)
    {
        return new NBClip(name, builder, frames, start);
    }

    public static NBTrack track()
    {
        return new NBTrack();
    }

    internal static void start(string[]? args = null)
    {
        App.Start(args);
    }

    public static void run()
    {
        run(stage(), []);
    }

    public static void run(NBStage stage, NBClip[] clips)
    {
        start();
    }
}

using SkiaSharp;

namespace NewBeeVG;

public static class Methods
{
    public static NBStage stage(int width = 1080, int height = 1920, int dpi = 96, IBrush? bg = null)
    {
        return new NBStage
        {
            Width = width,
            Height = height,
            Dpi = dpi,
            Background = bg,
        };
    }

    public static NBWorkspace workspace()
    {
        return new NBWorkspace();
    }

    public static NBWork work()
    {
        return new NBWork();
    }

    public static NBClip clip(string name = "clip", Func<NBDrawContext, NBClip, Control?>? builder = null, int frames = 1, int? start = null)
    {
        return new NBClip(name, builder, frames, start);
    }

    public static NBSkiaClip skclip(string name = "skclip", Action<NBDrawContext, NBClip, SKCanvas>? builder = null, int frames = 1, int? start = null)
    {
        return new NBSkiaClip(name, builder, frames, start);
    }

    public static NBMaskedSkiaClip skclip_withmask(string name = "skclip_withmask", 
        Action<NBDrawContext, NBClip, SKCanvas>? builder = null,
        Action<NBDrawContext, NBClip, SKCanvas>? maskBuilder = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int frames = 1, int? start = null)
    {
        return new NBMaskedSkiaClip(name, builder, maskBuilder, blend, frames, start);
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

    public static void run(NBStage stage, IList<NBClip> clips)
    {
        NBWorkspace.Current = NBWorkspace.Create(stage, clips);
        start();
    }
}

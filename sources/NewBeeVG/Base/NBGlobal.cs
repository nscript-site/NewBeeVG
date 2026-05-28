using NewBeeMedia;

namespace NewBeeVG;

public class NBGlobal
{
    public static bool IsFFmpegLoaded { get; internal set; } = false;

    public static void LoadFFmpegLibrary(string? ffmpegLibraryPath = null)
    {
        if (IsFFmpegLoaded)
            return;

        IsFFmpegLoaded = FFmpegUtils.Initialize(ffmpegLibraryPath);
    }

    public static void Initialize(string? ffmpegLibraryPath = null)
    {
        LoadFFmpegLibrary(ffmpegLibraryPath);
    }
}

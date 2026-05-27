namespace NewBeeMedia;

internal class FFLibrary
{
    private static readonly object LoadLock = new object();

    public static string BasePath { get; private set; } = String.Empty;

    public static bool IsLoaded { get; private set; }

    public static bool Load(string basePath)
    {
        lock (LoadLock)
        {
            if (IsLoaded == true)
                return true;

            DynamicallyLoadedBindings.LibrariesPath = basePath;
            DynamicallyLoadedBindings.Initialize();
            IsLoaded = true;
            return true;
        }
    }
}

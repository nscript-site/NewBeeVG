namespace NewBeeVG.Demo.Samples;

internal class Utils
{
    public static void EmbedPython(string filePath = "")
    {
        var fileInfo = new System.IO.FileInfo(filePath);
        var dirInfo = fileInfo.Directory ?? new System.IO.DirectoryInfo("./");
        var embedPython312_win32 = System.IO.Path.Combine(dirInfo.FullName, "../../apps/_lib/python-3.12.8-embed-amd64/");
        embed_python312_win32(embedPython312_win32);
    }
}

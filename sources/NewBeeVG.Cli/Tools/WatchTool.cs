using NewBeeVG.Cli.Tools.Internal;
using SkiaSharp;
using System.Text;
using System.Text.Json;

namespace NewBeeVG.Cli.Tools;

internal class WatchTool : BaseTool
{
    public override void Run(string[] args)
    {
        if(args.Length != 1)
        {
            PrintHelp();
            return;
        }

        var filePath = args[0].Trim();
        var fileInfo = new FileInfo(filePath);
        if(fileInfo.Exists == false)
        {
            Console.WriteLine($"文件不存在: {fileInfo.Name}");
            return;
        }
        else if(fileInfo.Name.EndsWith(".cs") == false)
        {
            Console.WriteLine($"只能 watch .cs 后缀的文件");
            return;
        }

        var lines = File.ReadAllLines(filePath);
        int count = Math.Min(10, lines.Length);
        bool match = false;
        for(int i = 0; i < count; i++)
        {
            var line = lines[i].Trim();
            if(line.StartsWith("#!/usr/bin/env") && line.EndsWith("dotnet"))
            {
                match = true;
                break;
            }
        }

        if(match == false)
        {
            Console.WriteLine($"文件内容必须以 #!/usr/bin/env dotnet 开头");
            return;
        }

        Console.WriteLine($"获取视频基本信息中 .......");
        var track = WatchTool.DumpTrackInfo(filePath);
        Console.WriteLine($"获取视频基本信息完成，启动 watch 窗口");
        var w = new FileWatcher(fileInfo, track);
        App.Start(fileInfo.Name, w);
    }

    public override void PrintHelp()
    {
        base.PrintHelp();
    }

    private static (string msg,string? err) RunCmd(string filePath, string[] args)
    {
        var fileInfo = new FileInfo(filePath);
        var dirName = fileInfo.DirectoryName;
        var fileName = fileInfo.Name;
        List<String> list = new();
        list.Add(fileName);
        list.AddRange(args);
        var sb = new StringBuilder();
        foreach (var item in list)
        {
            if (sb.Length > 0) sb.Append(" ");
            sb.Append(item);
        }
        try
        {
            var arg = sb.ToString();
            (int ExitCode, string StdOut, string StdErr) = Utils.RunCommand("dotnet", arg, dirName, 60*1000);
            return (StdOut, null);
        }
        catch(Exception ex)
        {
            return ("", ex.Message);
        }
    }

    internal static SKBitmap? DumpPNG(string filePath, string clipName, int frame)
    {
        var rtn = RunCmd(filePath, ["--", clipName, frame.ToString(), "@dump"]);
        if (rtn.err != null) return null;
        var str = GetDumpContent(rtn.msg);
        const string prefix = "data:image/png;base64,";
        if (str.StartsWith(prefix) == false) return null;
        str = str.Substring(prefix.Length);
        try
        {
            var data = Convert.FromBase64String(str);
            using var stream = new MemoryStream(data);
            return SKBitmap.Decode(stream);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    internal static TrackInfo? DumpTrackInfo(string filePath)
    {
        var rtn = RunCmd(filePath, ["--", "@dump"]);
        if (rtn.err != null) return null;
        var str = GetDumpContent(rtn.msg);
        if (string.IsNullOrWhiteSpace(str)) return null;

        try
        {
            Console.WriteLine($"Dump Track: {str}");
            return JsonSerializer.Deserialize(str, TrackInfoJsonContext.Default.TrackInfo);
        }
        catch (JsonException)
        {
            Console.WriteLine(str);
            return null;
        }
    }

    private static string GetDumpContent(string txt)
    {
        const string flag = "@@@@DUMPS@@@@";
        int idx = txt.IndexOf(flag);
        if (idx < 0) return string.Empty;
        txt = txt.Substring(idx + flag.Length);
        return txt.Trim();
    }
}

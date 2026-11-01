using NewBeeVG.Cli.Tools;

namespace NewBeeVG.Cli;

internal class Program
{
    const string Version = "1.0.1.1";

    static void Main(string[] args)
    {
        //Run(args);
        Run(["watch", "D:\\Projects\\21_Private_NewBee\\NewBeeVG\\apps\\files\\demo.cs"]);
    }

    static void Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }

        var cmd = args[0].Trim();

        args = ShiftArgs(args);

        if (cmd == "watch")
        {
            new WatchTool().Run(args);
        }
        else if(cmd == "--version")
        {
            Console.WriteLine($"nbvg version: {Version}");
        }
        else
        {
            PrintHelp();
        }
    }

    static string[] ShiftArgs(string[] args)
    {
        var list = new List<String>();
        for (int i = 1; i < args.Length; i++)
            list.Add(args[i]);
        return list.ToArray();
    }

    static void PrintHelp()
    {
        Console.WriteLine($"""
            NewBeeVG CLI {Version}

            用法:
              nbvg watch <file.cs>    监视并预览 .cs 视频脚本
              nbvg --version          显示版本
              nbvg help               显示帮助

            示例:
              nbvg watch .\demo.cs
              nbvg watch "C:\videos\demo.cs"

            说明:
              - 仅支持 .cs 后缀的文件
              - 文件必须以 #!/usr/bin/env dotnet 开头
              - 修改脚本后会自动重新加载预览
            """);
    }
}

using NewBeeVG.Cli.Tools;

namespace NewBeeVG.Cli;

internal class Program
{
    const string Version = "1.0.1.2";

    static void Main(string[] args)
    {
#if DEBUG
        Run(args);
        //Run(["watch", "d:\\test\\test4.cs"]);
        //Run(["watch", "D:\\Projects\\21_Private_NewBee\\NewBeeVG\\apps\\files\\demo.cs"]);
#else
        Run(args);
#endif
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
        else if(cmd == "create")
        {
            new CreateTool().Run(args);
        }
        else if(cmd == "init")
        {
            new InitTool().Run(args);
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
              nbvg init <ProjectName>  创建完整项目目录结构
              nbvg watch <file.cs>     监视并预览 .cs 视频脚本
              nbvg create <file.cs>    创建新的视频脚本模板
              nbvg --version           显示版本
              nbvg help                显示帮助

            示例:
              nbvg init MyVideo
              nbvg create .\new-video.cs
              nbvg watch .\demo.cs
              nbvg watch "C:\videos\demo.cs"

            说明:
              - init 不会覆盖已存在的项目目录
              - create 不会覆盖已存在的文件
              - watch 仅支持 .cs 后缀的文件
              - watch 的文件必须以 #!/usr/bin/env dotnet 开头
              - watch 期间修改脚本会自动重新加载预览
            """);
    }
}

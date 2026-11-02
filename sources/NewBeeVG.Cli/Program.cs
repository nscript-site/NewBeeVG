using NewBeeVG.Cli.Tools;

namespace NewBeeVG.Cli;

internal class Program
{
    const string Version = "1.0.2.6";

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
        else if(cmd == "new")
        {
            new NewTool().Run(args);
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
              nbvg init <ProjectName>        创建完整项目目录结构
              nbvg new <file.cs>             创建新的视频脚本文件
              nbvg new <ScriptProjectName>   创建单个视频项目
              nbvg watch <file.cs>           监视并预览 .cs 视频脚本
              nbvg --version                 显示版本
              nbvg help                      显示帮助

            示例:
              nbvg init MyWorkspace
              nbvg new .\new-video.cs
              nbvg new MyVideo
              nbvg watch .\demo.cs
              nbvg watch "C:\videos\demo.cs"

            说明:
              - init 不会覆盖已存在的项目目录
              - new 的参数以 .cs 结尾时创建文件，否则创建视频项目目录
              - new 不会覆盖已存在的文件或目录
              - watch 仅支持 .cs 后缀的文件
              - watch 的文件必须以 #!/usr/bin/env dotnet 开头
              - watch 期间修改脚本会自动重新加载预览

            """);
    }
}

using NewBeeVG.Cli.Tools.Internal;
using System;
using System.Collections.Generic;
using System.Text;

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

        var w = new FileWatcher(fileInfo);
        App.Start(fileInfo.Name, w);
    }

    public override void PrintHelp()
    {
        base.PrintHelp();
    }
}

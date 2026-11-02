using System;

namespace NewBeeVG.Cli.Tools;

internal class NewTool : BaseTool
{
    public override void Run(string[] args)
    {
        if (args.Length != 1)
        {
            PrintHelp();
            return;
        }

        var target = args[0].Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            PrintHelp();
            return;
        }

        if (target.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            new CreateTool().Run([target]);
        else
            new ScriptTool().Run([target]);
    }

    public override void PrintHelp()
    {
        Console.WriteLine("用法: nbvg new <file.cs> | nbvg new <ScriptProjectName>");
        Console.WriteLine("说明: 参数以 .cs 结尾时创建视频脚本文件，否则创建单个视频项目目录。");
    }
}

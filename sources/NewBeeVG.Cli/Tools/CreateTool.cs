using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NewBeeVG.Cli.Tools;

internal class CreateTool : BaseTool
{
    private const string DefaultTemplateSuffix = "Templates.demo.cs";
    private const string BuildPropsTemplateSuffix = "Templates.demo2.cs";

    public override void Run(string[] args)
    {
        if (args.Length != 1)
        {
            PrintHelp();
            return;
        }

        var filePath = args[0].Trim();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            PrintHelp();
            return;
        }

        var fullPath = Path.GetFullPath(filePath);
        if (File.Exists(fullPath))
        {
            Console.WriteLine($"文件已存在，未创建: {fullPath}");
            return;
        }

        if (Directory.Exists(fullPath))
        {
            Console.WriteLine($"目标路径是目录，无法创建文件: {fullPath}");
            return;
        }

        var targetDirectory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        var hasBuildProps = HasDirectoryBuildProps(targetDirectory);
        var templateFile = hasBuildProps ? "demo2.cs" : "demo.cs";
        var templateSuffix = hasBuildProps ? BuildPropsTemplateSuffix : DefaultTemplateSuffix;
        var template = ReadTemplate(templateSuffix);
        if (template == null)
        {
            Console.WriteLine($"未找到内置模板 Templates/{templateFile}");
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory) == false)
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, template, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"已创建: {fullPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建失败: {ex.Message}");
        }
    }

    public override void PrintHelp()
    {
        Console.WriteLine("用法: nbvg create <file.cs>");
        Console.WriteLine("说明: 创建 NewBeeVG 视频脚本模板；目标文件已存在时不会覆盖。");
    }

    private static bool HasDirectoryBuildProps(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        // 依次检查 ./、../、../../、../../../、../../../../（共 5 层）
        for (var level = 0; level <= 4 && directory != null; level++, directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
                return true;
        }

        return false;
    }

    private static string? ReadTemplate(string templateSuffix)
    {
        var assembly = typeof(CreateTool).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(templateSuffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null) return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}

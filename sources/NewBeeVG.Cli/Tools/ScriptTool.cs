using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NewBeeVG.Cli.Tools;

internal class ScriptTool : BaseTool
{
    private const string TemplatePrefix = "Templates.Project.scripts.demo.";

    private static readonly string[] TemplateFiles =
    [
        "README.md",
        "script.md",
        "storyboard.md",
        "script.cs",
        "modules/GlintTextA.cs",
        "modules/Test/GlintTextA_Test.cs",
        "gen/images/prompts.md.template",
    ];

    private static readonly string[] TemplateDirectories =
    [
        "assets",
        "modules",
        "modules/Test",
        "gen",
        "gen/images",
        "gen/apngs",
        "gen/audios",
        "gen/videos",
        "gen/typsts",
        "gen/svgs",
        "tools",
        "output",
    ];

    public override void Run(string[] args)
    {
        if (args.Length != 1)
        {
            PrintHelp();
            return;
        }

        var projectName = args[0].Trim();
        if (string.IsNullOrWhiteSpace(projectName))
        {
            PrintHelp();
            return;
        }

        var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), projectName));
        if (Directory.Exists(projectDir))
        {
            Console.WriteLine($"目录已经存在，创建失败: {projectDir}");
            return;
        }

        if (File.Exists(projectDir))
        {
            Console.WriteLine($"同名文件已经存在，创建失败: {projectDir}");
            return;
        }

        // 先读取全部模板，避免创建到一半才发现资源缺失。
        var templates = new Dictionary<string, string>();
        foreach (var relativePath in TemplateFiles)
        {
            var content = ReadTemplate(relativePath);
            if (content == null)
            {
                Console.WriteLine($"未找到内置模板: Templates/Project/scripts/demo/{relativePath}");
                return;
            }

            templates[relativePath] = content;
        }

        try
        {
            Directory.CreateDirectory(projectDir);

            foreach (var relativeDir in TemplateDirectories)
                Directory.CreateDirectory(ToLocalPath(projectDir, relativeDir));

            foreach (var (relativePath, content) in templates)
            {
                var targetPath = ToLocalPath(projectDir, relativePath);
                var targetDir = Path.GetDirectoryName(targetPath);
                if (string.IsNullOrEmpty(targetDir) == false)
                    Directory.CreateDirectory(targetDir);

                File.WriteAllText(targetPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            Console.WriteLine($"视频项目已创建: {projectDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"创建失败: {ex.Message}");
        }
    }

    public override void PrintHelp()
    {
        Console.WriteLine("用法: nbvg new <ScriptProjectName>");
        Console.WriteLine("说明: 在当前目录创建单个视频项目目录结构；同名目录已存在时不会创建。");
    }

    private static string ToLocalPath(string root, string relativePath)
    {
        return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string? ReadTemplate(string relativePath)
    {
        var assembly = typeof(ScriptTool).Assembly;
        var suffix = TemplatePrefix + relativePath.Replace('/', '.');
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (resourceName == null) return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}

#!/usr/bin/env dotnet
// 遍历 modules 下每个子模块的 Test 目录，在该目录里生成（覆盖）@run.ps1：
// 每个测试入口一条执行指令，且脚本自行回到自己所在目录，入口路径写成相对文件名。
// 文件名前缀 @ 是为了在目录里排在最前面。
//
// 用法：cd modules;  dotnet GenRun.cs
//      新增 / 删除测试文件后重跑一次，各 Test 目录下的 @run.ps1 即同步刷新。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

var modulesDir = Directory.GetCurrentDirectory();

// 自称 Crash-safe：即使在别的目录下误跑，也只是找不到东西
var moduleDirs = Directory.GetDirectories(modulesDir)
                          .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)
                          .ToList();

int written = 0, skipped = 0;

foreach (var moduleDir in moduleDirs)
{
    var module = Path.GetFileName(moduleDir);

    // Test / test 都算（Windows 下目录名大小写不敏感，历史上 Common 用的是小写 test）
    var testDir = Directory.GetDirectories(moduleDir)
                           .FirstOrDefault(p => string.Equals(Path.GetFileName(p), "Test", StringComparison.OrdinalIgnoreCase));

    if (testDir is null)
    {
        Console.WriteLine($"[skip] {module}/：没有 Test 目录");
        skipped++;
        continue;
    }

    var tests = Directory.GetFiles(testDir, "*_Test.cs")
                         .Select(Path.GetFileName)
                         .Where(n => !string.IsNullOrEmpty(n))
                         .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                         .ToList()!;

    if (tests.Count == 0)
    {
        Console.WriteLine($"[skip] {module}/Test：没有 *_Test.cs");
        skipped++;
        continue;
    }

    // 脚本内容：回到脚本所在目录 -> 说明 -> Mode 开关 -> 每条指令一行
    var lines = new List<string>
    {
        "Set-Location $PSScriptRoot   # 统一以本脚本所在目录为工作目录",
        "",
        $"# {module}/{Path.GetFileName(testDir)} · @run.ps1 由 modules/GenRun.cs 自动生成：增删 *_Test.cs 后重跑 dotnet GenRun.cs 即可刷新",
        "# Mode 就是传给每个入口的第一个参数 args[0]（对应测试里的 debug 开关）：",
        "#   \"run\"   => debug = false => 按 SkiaAnim 注册的片段正式出片",
        "#   \"debug\" => debug = true  => 导出测试静帧 / 测试 mp4（不带参数运行时也是这个模式）",
        "$Mode = \"run\"",
        "# $Mode = \"debug\"",
        "",
    };
    lines.AddRange(tests.Select(n => $"dotnet {n} -- $Mode"));

    // 写出（带 BOM 的 UTF-8：PowerShell 5.1 读无 BOM 脚本会按 ANSI 解中文，会乱码）
    var target = Path.Combine(testDir, "@run.ps1");
    File.WriteAllLines(target, lines, new System.Text.UTF8Encoding(true));

    Console.WriteLine($"[gen]  {module}/{Path.GetFileName(testDir)}/@run.ps1  <-  {tests.Count} 条");
    written++;
}

Console.WriteLine($"[gen] 完成：写入 {written} 个，跳过 {skipped} 个");
return 0;

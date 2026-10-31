using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NewBeeVG.Cli.Tools.Internal;

internal class Utils
{
    /// <summary>
    /// 运行外部命令，等待执行结束并返回结果。
    /// </summary>
    /// <param name="fileName">可执行文件，如 "cmd.exe"、"git"、"dotnet"</param>
    /// <param name="arguments">参数，如 "/c dir"</param>
    /// <param name="workingDirectory">工作目录，可为 null</param>
    /// <param name="timeoutMs">超时毫秒数，0 或负数表示不限制</param>
    /// <returns>(退出码, 标准输出, 标准错误)</returns>
    public static (int ExitCode, string StdOut, string StdErr) RunCommand(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int timeoutMs = 0)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        bool exited = timeoutMs > 0
            ? process.WaitForExit(timeoutMs)
            : WaitForExitNoTimeout(process);

        if (!exited)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* 忽略 */ }
            throw new TimeoutException($"命令执行超时（{timeoutMs} ms）：{fileName} {arguments}");
        }

        // 确保异步输出读取完成
        process.WaitForExit();

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static bool WaitForExitNoTimeout(Process process)
    {
        process.WaitForExit();
        return true;
    }

    /// <summary>
    /// 启动外部命令后立即返回，不等待其结束（fire-and-forget）。
    /// </summary>
    /// <param name="fileName">可执行文件，如 "cmd.exe"、"notepad.exe"</param>
    /// <param name="arguments">参数，如 "/c start http://xxx"</param>
    /// <param name="workingDirectory">工作目录，可为 null</param>
    /// <param name="onOutput">每行标准输出的回调，可为 null</param>
    /// <param name="onError">每行标准错误的回调，可为 null</param>
    public static void RunCommandDetached(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        Action<string>? onOutput = null,
        Action<string>? onError = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? string.Empty
        };

        var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (s, e) => { if (e.Data != null) onOutput?.Invoke(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) onError?.Invoke(e.Data); };

        // 进程退出时释放资源，避免句柄泄漏
        process.EnableRaisingEvents = true;
        process.Exited += (s, e) => (s as Process)?.Dispose();

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // 不调用 WaitForExit，直接返回
    }
}

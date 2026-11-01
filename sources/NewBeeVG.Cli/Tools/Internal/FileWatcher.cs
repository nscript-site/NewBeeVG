using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NewBeeVG.Cli.Tools.Internal;

internal class FileWatcher
{
    private const int DebounceMilliseconds = 200;

    private List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();
    private readonly object _changeGate = new();
    private readonly string _watchedPath;
    private string? _pendingPath;
    private Timer? _debounceTimer;

    internal string FilePath;

    internal Action<String>? OnChanged;

    internal TrackInfo? Track { get; set; }

    public FileWatcher(FileInfo fileInfo, TrackInfo? track)
    {
        Track = track;
        _watchedPath = fileInfo.FullName;
        FilePath = _watchedPath;
        var w = new FileSystemWatcher(fileInfo.DirectoryName??String.Empty, fileInfo.Name);
        w.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
        w.Changed += OnFileChanged;
        w.Created += OnFileChanged; // 如果是文件被覆盖或重新创建，通常会触发 Created
        w.Renamed += OnFileRenamed;

        // FileSystemWatcher 默认不会触发事件，必须显式启用。
        w.EnableRaisingEvents = true;

        Watchers.Add(w);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        ScheduleChanged(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // 编辑器常用“写临时文件再重命名”的方式保存，可能触发多次事件；
        // 只把与目标文件相关的重命名并入一次防抖刷新。
        if (StringComparer.OrdinalIgnoreCase.Equals(e.FullPath, _watchedPath) ||
            StringComparer.OrdinalIgnoreCase.Equals(e.OldFullPath, _watchedPath))
            ScheduleChanged(_watchedPath);
    }

    private void ScheduleChanged(string path)
    {
        lock (_changeGate)
        {
            _pendingPath = path;
            _debounceTimer ??= new Timer(OnDebounceElapsed);
            _debounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        string? path;

        lock (_changeGate)
        {
            path = _pendingPath;
            _pendingPath = null;
        }

        if (path == null) return;

        Console.WriteLine($"[FileWatcher] 检测到文件改动: {path}");
        FilePath = path;
        OnChanged?.Invoke(path);
    }
}

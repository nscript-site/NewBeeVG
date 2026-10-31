using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG.Cli.Tools.Internal;

public class FileWatcher
{
    private List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();

    internal string FilePath;

    internal Action<String>? OnChanged;

    public FileWatcher(FileInfo fileInfo)
    {
        FilePath = fileInfo.FullName;
        var w = new FileSystemWatcher(fileInfo.DirectoryName??String.Empty, fileInfo.Name);
        w.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
        w.Changed += OnFileChanged;
        w.Created += OnFileChanged; // 如果是文件被覆盖或重新创建，通常会触发 Created
        w.Renamed += OnFileRenamed;

        Watchers.Add(w);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        FilePath = e.FullPath;
        OnChanged?.Invoke(FilePath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        FilePath = e.FullPath;
        OnChanged?.Invoke(FilePath);
    }
}

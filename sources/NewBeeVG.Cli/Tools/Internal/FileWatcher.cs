namespace NewBeeVG.Cli.Tools.Internal;

internal class FileWatcher
{
    private const int DebounceMilliseconds = 200;
    private const string IncludeDirective = "#:include";

    private readonly List<FileSystemWatcher> Watchers = new();
    private readonly object _changeGate = new();
    private readonly string _watchedPath;
    private string? _pendingPath;
    private string? _pendingFilePath;
    private Timer? _debounceTimer;

    internal string FilePath;

    internal Action<String>? OnChanged;

    internal TrackInfo? Track { get; set; }

    public FileWatcher(FileInfo fileInfo, TrackInfo? track)
    {
        Track = track;
        _watchedPath = fileInfo.FullName;
        FilePath = _watchedPath;

        AddWatcher(fileInfo.DirectoryName ?? String.Empty, fileInfo.Name, isMainFile: true);

        // 只解析主脚本直接引用的文件，不递归解析这些文件自身的 #:include。
        var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            _watchedPath,
        };

        foreach (var includePath in ParseIncludes(_watchedPath))
        {
            if (addedPaths.Add(includePath) == false) continue;
            if (File.Exists(includePath) == false) continue;

            var directory = Path.GetDirectoryName(includePath);
            if (string.IsNullOrEmpty(directory)) continue;

            AddWatcher(directory, Path.GetFileName(includePath), isMainFile: false);
        }
    }

    private void AddWatcher(string directory, string fileName, bool isMainFile)
    {
        var w = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = isMainFile
                ? NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                : NotifyFilters.LastWrite | NotifyFilters.Size,
        };

        if (isMainFile)
        {
            w.Changed += OnFileChanged;
            w.Created += OnFileChanged; // 如果是文件被覆盖或重新创建，通常会触发 Created
            w.Renamed += OnFileRenamed;
        }
        else
        {
            // 被引用的文件只监测 Changed。
            w.Changed += OnFileChanged;
        }

        // FileSystemWatcher 默认不会触发事件，必须显式启用。
        w.EnableRaisingEvents = true;

        Watchers.Add(w);
    }

    private static IEnumerable<string> ParseIncludes(string mainFilePath)
    {
        var mainDirectory = Path.GetDirectoryName(mainFilePath) ?? String.Empty;
        var inBlockComment = false;

        foreach (var rawLine in File.ReadLines(mainFilePath))
        {
            var line = rawLine.Trim();
            if (TryStripLeadingComments(ref line, ref inBlockComment) == false) continue;
            if (line.StartsWith(IncludeDirective, StringComparison.Ordinal) == false) continue;

            var value = line.Substring(IncludeDirective.Length);
            if (value.Length == 0 || Char.IsWhiteSpace(value[0]) == false) continue;

            value = value.Trim();
            if (value.Length == 0) continue;

            // 支持带引号的路径，例如 #:include "../shared/My File.cs"
            if (value[0] is '"' or '\'')
            {
                var quote = value[0];
                var end = value.IndexOf(quote, 1);
                if (end < 0) continue;
                value = value.Substring(1, end - 1);
            }
            else
            {
                // 未加引号时，路径到第一个空白为止；支持行尾注释。
                var end = value.IndexOfAny([' ', '\t']);
                if (end >= 0) value = value.Substring(0, end);
            }

            value = value.Trim();
            if (value.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == false) continue;

            var fullPath = Path.IsPathRooted(value)
                ? Path.GetFullPath(value)
                : Path.GetFullPath(Path.Combine(mainDirectory, value));

            yield return fullPath;
        }
    }

    private static bool TryStripLeadingComments(ref string line, ref bool inBlockComment)
    {
        line = line.TrimStart();

        while (true)
        {
            if (inBlockComment)
            {
                var blockEnd = line.IndexOf("*/", StringComparison.Ordinal);
                if (blockEnd < 0)
                {
                    line = String.Empty;
                    return false;
                }

                line = line.Substring(blockEnd + 2).TrimStart();
                inBlockComment = false;
                continue;
            }

            if (line.StartsWith("//", StringComparison.Ordinal))
            {
                line = String.Empty;
                return false;
            }

            if (line.StartsWith("/*", StringComparison.Ordinal))
            {
                var blockEnd = line.IndexOf("*/", 2, StringComparison.Ordinal);
                if (blockEnd < 0)
                {
                    inBlockComment = true;
                    line = String.Empty;
                    return false;
                }

                line = line.Substring(blockEnd + 2).TrimStart();
                continue;
            }

            return true;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(e.FullPath, _watchedPath))
        {
            ScheduleChanged(e.FullPath);
            return;
        }

        // 被引用文件变化时仍重载主脚本，不把 FilePath 指向依赖文件。
        ScheduleChanged(e.FullPath, _watchedPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // 编辑器常用“写临时文件再重命名”的方式保存，可能触发多次事件；
        // 只把与目标文件相关的重命名并入一次防抖刷新。
        if (StringComparer.OrdinalIgnoreCase.Equals(e.FullPath, _watchedPath) ||
            StringComparer.OrdinalIgnoreCase.Equals(e.OldFullPath, _watchedPath))
            ScheduleChanged(_watchedPath);
    }

    private void ScheduleChanged(string path, string? filePath = null)
    {
        lock (_changeGate)
        {
            _pendingPath = path;
            _pendingFilePath = filePath ?? path;
            _debounceTimer ??= new Timer(OnDebounceElapsed);
            _debounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
        }
    }

    private void OnDebounceElapsed(object? state)
    {
        string? path;
        string? filePath;

        lock (_changeGate)
        {
            path = _pendingPath;
            filePath = _pendingFilePath;
            _pendingPath = null;
            _pendingFilePath = null;
        }

        if (path == null) return;

        Console.WriteLine($"[FileWatcher] 检测到文件改动: {path}");
        FilePath = filePath ?? _watchedPath;
        OnChanged?.Invoke(path);
    }
}

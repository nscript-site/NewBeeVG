namespace NewBeeMedia;

public interface IMediaStorage
{
    public bool Match(Uri uri);

    public Uri Fetch(Uri uri);
}

public class MediaStorageManager
{
    public static MediaStorageManager Instance = new MediaStorageManager();
    private MediaStorageManager() { }

    public void Add(IMediaStorage storage)
    {
        _mediaStorageFetchers.Add(storage);
    }

    public void Remove(IMediaStorage storage)
    {
        _mediaStorageFetchers.Remove(storage);
    }

    public void RemoveAllMediaStorages()
    {
        _mediaStorageFetchers.Clear();
    }

    private List<IMediaStorage> _mediaStorageFetchers = new List<IMediaStorage>();

    public int NumOfFetchers
    {
        get { return _mediaStorageFetchers.Count; }
    }

    public Uri Fetch(Uri uri)
    {
        foreach (var item in _mediaStorageFetchers)
        {
            if (item.Match(uri))
                return item.Fetch(uri);
        }

        return uri;
    }
}

/// <summary>
/// HTTP 媒体存储管理器
/// </summary>
public class HttpMediaStorage : IMediaStorage
{
    public Action<Uri> OnFetchOk { get; set; }
    public Action<Uri> OnFetchBegin { get; set; }
    public Action<String> OnMessage { get; set; }

    public int MaxTryTimes { get; private set; }

    private String _localCacheDir;

    public HttpMediaStorage(String localCacheDir = "./cache", int maxTryTimes = 6)
    {
        _localCacheDir = localCacheDir;
        MaxTryTimes = Math.Max(1, maxTryTimes);
        DirectoryInfo dirInfo = new DirectoryInfo(localCacheDir);
        if (dirInfo.Exists == false) dirInfo.Create();
    }

    public Uri Fetch(Uri uri)
    {
        if (OnFetchBegin != null) OnFetchBegin(uri);

        String ext = String.Empty;
        String path = uri.OriginalString;
        int idx = path.LastIndexOf(".");
        if (idx > 0) ext = path.Substring(idx + 1);
        if (String.IsNullOrEmpty(ext)) ext = "mp4";

        // 缓存文件名称
        String fileName = "t_" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString() + "_" + DateTime.Now.ToFileTimeUtc() + "." + ext;

        // 目录若不存在，则创建。缓存文件若存在则删除。
        String cacheFilePath = Path.Combine(_localCacheDir, fileName);
        DirectoryInfo dirCache = new FileInfo(cacheFilePath).Directory;
        if (dirCache.Exists == false) dirCache.Create();
        if (File.Exists(cacheFilePath)) File.Delete(cacheFilePath);  // 删除文件

        int failCount = 0;
        while (failCount < MaxTryTimes)
        {
            try
            {
                if (File.Exists(cacheFilePath)) File.Delete(cacheFilePath);  // 删除文件
                System.Net.WebClient client = new System.Net.WebClient();
                client.DownloadFile(path, cacheFilePath);
                OnMessage?.Invoke("Download Ok: " + path);
                break;
            }
            catch (Exception ex)
            {
                OnMessage?.Invoke("Download Fail: " + path);
                OnMessage?.Invoke(ex.Message);
            }
            System.Threading.Thread.Sleep(3000);
            failCount++;
        }

        Uri localUri = new Uri(new FileInfo(cacheFilePath).FullName);
        if (OnFetchOk != null) OnFetchOk(localUri);

        return localUri;
    }

    public bool Match(Uri uri)
    {
        String url = uri.OriginalString;
        if (url.StartsWith("sftp://")) return true;
        else if (url.StartsWith("http://") || url.StartsWith("https://")) return true;
        else return false;
    }
}

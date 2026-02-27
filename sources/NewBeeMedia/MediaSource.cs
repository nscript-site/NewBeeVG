namespace NewBeeMedia;

/// <summary>
/// 媒体源
/// </summary>
public class MediaSource
{
    public String FilePath { get; private set; }

    public String FileName { get; private set; }

    public MediaSource(FileInfo file)
    {
        FilePath = file.FullName;
        UriSource = new Uri(file.FullName);
        FileName = file.Name;
    }

    public MediaSource(String filePath, String fileName = null)
    {
        FilePath = filePath;
        FileName = fileName;
        if (filePath.StartsWith("sftp://"))
        {
            UriSource = new Uri(filePath);
        }
        else if (filePath.StartsWith("http://"))
        {
            UriSource = new Uri(filePath);
        }
        else if (filePath.StartsWith("https://")){
            UriSource = new Uri(filePath);
        } 
        else
            UriSource = new Uri(new System.IO.FileInfo(filePath).FullName);
    }

    public Uri UriSource { get; protected set; }
    public String AbsolutePath { get { return UriSource.AbsolutePath; } }

    public Uri Fetch()
    {
        Uri localUri = MediaStorageManager.Instance.Fetch(UriSource);
        return localUri;
    }
}

/// <summary>
/// 媒体存储服务
/// </summary>
public class MediaStorageService
{
}

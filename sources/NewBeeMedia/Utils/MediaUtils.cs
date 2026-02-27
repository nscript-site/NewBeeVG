namespace NewBeeMedia;

public class MediaUtils
{
    public static DirectoryInfo GetTempCache()
    {
        DirectoryInfo dir = new DirectoryInfo("./tmp");
        if (dir.Exists == false) dir.Create();
        return dir;
    }

    public static void ExportPcmSr10File(String mediaFilePath, String pcmSr10FilePath)
    {
        DirectoryInfo dirTmp = GetTempCache();
        String tmpFile = Path.Combine(dirTmp.FullName, $"{System.Threading.Thread.CurrentThread.ManagedThreadId}_tmp.pcm");
        if (File.Exists(tmpFile)) File.Delete(tmpFile);
        FFmpegCmd.ExportPcm16File(mediaFilePath, tmpFile,null);
        Pcm.PcmSR10.Export(tmpFile, pcmSr10FilePath);
    }

    public static void ExportPcmFiles(String mediaFilePath, String pcmS16FilePath,  String pcmSr10FilePath)
    {
        FFmpegCmd.ExportPcm16File(mediaFilePath, pcmS16FilePath, null);
        Pcm.PcmSR10.Export(pcmS16FilePath, pcmSr10FilePath);
    }

    public static string ExportWav16File(String filePath)
    {
        DirectoryInfo dirTmp = GetTempCache();
        String tmpFile = Path.Combine(dirTmp.FullName, $"{System.Threading.Thread.CurrentThread.ManagedThreadId}_tmp.wav");
        FFmpegCmd.ExportWav16File(filePath, tmpFile, null);
        return tmpFile;
    }

    public static double GetDuration(String filePath)
    {
        using(MediaReader mr = new MediaReader(filePath))
        {
            return mr.Duration.TotalSeconds;
        }   
    }
}

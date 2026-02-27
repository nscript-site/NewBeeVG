namespace NewBeeMedia;

public class FFmpegCmd
{
    public static void ExportPcm16File(String filePath, String outAudioFilePath, Action<String> onMessage)
    {
        FileInfo outfileInfo = new FileInfo(outAudioFilePath);
        DirectoryInfo outDirInfo = outfileInfo.Directory;
        if (outDirInfo.Exists == false) outDirInfo.Create();
        if (outfileInfo.Exists == true) outfileInfo.Delete();

        using Command cmd = new Command();
        cmd.ExecuteCmd("ffmpeg", $"-y -i {filePath} -acodec pcm_s16le -f s16le -ac 1 -ar 16000 {outAudioFilePath}", onMessage);
    }

    public static void ExportPcm8File(String filePath, String outAudioFilePath, Action<String> onMessage)
    {
        FileInfo outfileInfo = new FileInfo(outAudioFilePath);
        DirectoryInfo outDirInfo = outfileInfo.Directory;
        if (outDirInfo.Exists == false) outDirInfo.Create();
        if (outfileInfo.Exists == true) outfileInfo.Delete();

        using Command cmd = new Command();
        cmd.ExecuteCmd("ffmpeg", $"-y -i {filePath} -acodec pcm_s16le -f s16le -ac 1 -ar 8000 {outAudioFilePath}", onMessage);
    }

    public static void ExportWav16File(String filePath, String outAudioFilePath, Action<String> onMessage)
    {
        FileInfo outfileInfo = new FileInfo(outAudioFilePath);
        DirectoryInfo outDirInfo = outfileInfo.Directory;
        if (outDirInfo.Exists == false) outDirInfo.Create();
        if (outfileInfo.Exists == true) outfileInfo.Delete();

        using Command cmd = new Command();
        cmd.ExecuteCmd("ffmpeg", $"-y -i {filePath} -acodec pcm_s16le -ac 1 -ar 16000 {outAudioFilePath}", onMessage);
    }

    public static void Run(string cmd)
    {
        using Command command = new Command();
        command.ExecuteCmd("ffmpeg", cmd, null);
    }

    /// <summary>
    /// 转码
    /// </summary>
    /// <param name="filePath">输入路径</param>
    /// <param name="outFilePath">输出路径</param>
    /// <param name="onMessage">接收消息</param>
    public static void Transcode(String filePath, String outFilePath, Action<String> onMessage)
    {
        StringBuilder sb = new StringBuilder();
        String cmds = " -i \"" + filePath + "\" \"" + outFilePath + "\"";
        Command command = new Command();
        command.ExecuteCmd("ffmpeg", cmds, (String txt) => { sb.AppendLine(txt); }, null);
        if (onMessage != null) onMessage(sb.ToString());
    }
}

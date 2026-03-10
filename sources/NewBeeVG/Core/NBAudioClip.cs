namespace NewBeeVG;

public class NBAudioClip : NBClip
{
    private string? _audioFilePath;

    public NBAudioClip(string name = "clip", string? audioFilePath = null, int duration = 1, int? start = null)
    {
        Name = name;
        ControlBuilder = null;
        _audioFilePath = audioFilePath;
        DurationFrames = duration;
        StartFrame = start;
    }

    public override void Prepare()
    {
            // 这里可以添加加载音频文件的逻辑，例如使用 NAudio 库加载音频数据
            // 例如：
            // if (!string.IsNullOrEmpty(_audioFilePath))
            // {
            //     var audioData = LoadAudio(_audioFilePath);
            //     // 存储或处理 audioData 以供播放使用
            // }
    }
}

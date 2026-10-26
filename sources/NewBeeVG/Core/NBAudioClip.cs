using NAudio.Wave;

namespace NewBeeVG;

public class NBAudioClip : NBClip
{
    private string? _audioFilePath;

    public NBAudioClip(string name = "audio", string? audioFilePath = null, int duration = 1, int? start = null)
    {
        Name = name;
        ControlBuilder = null;
        _audioFilePath = audioFilePath;
        DurationFrames = duration;
        StartFrame = start;
    }

    public override void Prepare(NBStage stage)
    {
        double fps = stage.FrameRate;
        // 检查 _audioFilePath, 存在且是 mp3 或 wav 文件, 计算文件的时长，转换成帧数
        //this.DurationFrames = ...
    }
}

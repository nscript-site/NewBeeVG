using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG;

public enum NBAnimateLoopMode
{
    Loop,
    AutoReverse,
    Stop
}

public class NBBaseLoopAnimation : NBBaseImage
{
    public int TotalFrames { get; protected set; }

    public NBAnimateLoopMode LoopMode { get; set; } = NBAnimateLoopMode.Stop;

    /// <summary>
    /// 播放速度
    /// </summary>
    public double Speed { get; set; } = 1.0;

    protected int NextFrame(int totleFrames, NBAnimateLoopMode loopMode, double speed = 1.0)
    {
        var frame = NBDrawContext.Current?.frame ?? 0;
        frame = (int)(frame * speed);

        if (loopMode == NBAnimateLoopMode.Loop)
        {
            frame = frame % totleFrames;
        }
        else if (loopMode == NBAnimateLoopMode.AutoReverse)
        {
            var cycleFrames = totleFrames * 2;
            frame = frame % cycleFrames;
            if (frame >= totleFrames)
            {
                frame = cycleFrames - frame - 1;
            }
        }
        else if (loopMode == NBAnimateLoopMode.Stop)
        {
            if (frame >= totleFrames)
                frame = totleFrames - 1;
            if (frame < 0) frame = 0;
        }

        return frame;
    }
}

public static partial class NBExtentions
{
    public static T LoopMode<T>(this T self, NBAnimateLoopMode loopMode) where T : NBBaseLoopAnimation
    {
        self.LoopMode = loopMode;
        return self;
    }

    public static T Speed<T>(this T self, double speed) where T : NBBaseLoopAnimation
    {
        self.Speed = speed;
        return self;
    }
}
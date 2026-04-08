using NAudio.Wave;

namespace NewBeeVG;

internal class AudioHelpers
{
    /// <summary>
    /// Returns the duration of a WAV file using NAudio.
    /// Works for common PCM / WAV files (NAudio supports many formats).
    /// </summary>
    public static TimeSpan GetWavDuration(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        using var reader = new WaveFileReader(path);
        return reader.TotalTime;
    }

    /// <summary>
    /// Returns sample rate and channels of the WAV if you need them.
    /// </summary>
    public static (int sampleRate, int channels) GetWavFormat(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
        using var reader = new WaveFileReader(path);
        var wf = reader.WaveFormat;
        return (wf.SampleRate, wf.Channels);
    }
}

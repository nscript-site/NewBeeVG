using NAudio.Wave;

namespace NewBeeVG.Internal;

/// <summary>
/// Simple audio player that supports playing WAV data from a byte[] or file.
/// Uses NAudio and disposes resources when playback stops.
/// </summary>
public class AudioPlayer : IDisposable
{
    private IWavePlayer? _output;
    private WaveStream? _waveStream;

    public PlaybackState PlaybackState => _output?.PlaybackState ?? PlaybackState.Stopped;

    public void PlayFromFile(string path)
    {
        Stop();
        _waveStream = new AudioFileReader(path);
        _output = new WaveOutEvent();
        _output.Init(_waveStream);
        _output.PlaybackStopped += OnPlaybackStopped;
        _output.Play();
    }

    public void PlayFromBytes(byte[] data)
    {
        if (data == null || data.Length == 0) return;
        Stop();

        // MemoryStream must stay alive while WaveFileReader reads from it.
        var ms = new MemoryStream(data, writable: false);

        // Use WaveFileReader for WAV byte data
        _waveStream = new WaveFileReader(ms);
        _output = new WaveOutEvent();
        _output.Init(_waveStream);
        _output.PlaybackStopped += OnPlaybackStopped;
        _output.Play();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // cleanup
        try
        {
            _output!.PlaybackStopped -= OnPlaybackStopped;
        }
        catch { }

        _waveStream?.Dispose();
        _waveStream = null;

        _output?.Dispose();
        _output = null;
    }

    public void Stop()
    {
        try
        {
            _output?.Stop();
        }
        catch { }
    }

    public async Task PlayAndWaitAsync(byte[] data, CancellationToken ct = default)
    {
        PlayFromBytes(data);
        while (PlaybackState == PlaybackState.Playing)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
        }
    }

    public void Dispose()
    {
        try
        {
            Stop();
        }
        catch { }

        _waveStream?.Dispose();
        _waveStream = null;
        _output?.Dispose();
        _output = null;
    }
}
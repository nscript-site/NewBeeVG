namespace NewBeeMedia;

public class PcmSFileReader : IDisposable
{
    private string _pcmPath;
    private FileStream _stream;
    private BinaryReader _reader;
    private int _audioRate;

    public double duration { get; set; }

    public FileStream Stream { get { return _stream; } }
    public BinaryReader Reader { get { return _reader; } }
    public int AudioRate { get { return _audioRate; } }

    public PcmSFileReader(String pcmPath)
        : this(pcmPath, 16000)
    {
    }

    public PcmSFileReader(String pcmPath, int rate)
    {
        _pcmPath = pcmPath;
        _stream = new FileStream(pcmPath, FileMode.Open);
        _reader = new BinaryReader(_stream);
        _audioRate = rate;
        long length = _stream.Length;
        duration = length / 2.0f / (double)_audioRate;
    }

    public byte[] GetPcm(double start, double end)
    {
        byte[] data = null;
        int length = Convert.ToInt32(_stream.Length);
        int idxStart = (int)(start * _audioRate) * 2;
        int idxEnd = (int)(end * _audioRate) * 2;
        idxStart = Math.Min(idxStart, length);
        idxStart = Math.Max(idxStart, 0);
        idxEnd = Math.Min(idxEnd, length);
        idxEnd = Math.Max(idxEnd, 0);
        if (idxEnd > idxStart)
        {
            _stream.Position = idxStart;
            data = new byte[idxEnd - idxStart];
            _stream.Read(data, 0, data.Length);
        }

        return data;
    }

    public void Dispose()
    {
        if (_reader != null)
        {
            _reader.Close();
            _reader = null;
        }

        if (_stream != null)
        {
            _stream.Close();
            _stream = null;
        }
    }

    ~PcmSFileReader()
    {
        this.Dispose();
    }
}

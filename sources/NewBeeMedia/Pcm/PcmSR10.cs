namespace NewBeeMedia.Pcm;

public class PcmSR10
{
    private byte[] _values;

    public Byte[] Values { get { return _values; } }

    public unsafe PcmSR10(FileStream stream, int sr = 16000)
    {
        this.Init(stream, sr);
    }

    public PcmSR10(String pcmFilePath, int sr = 16000)
    {
        using(FileStream fs = new FileStream(pcmFilePath, FileMode.Open))
        {
            this.Init(fs, sr);
        }
    }

    //http://localhost:5000/format?filepath=20210223073546_415ff0cc-f0a0-4046-bf9a-0058d42184db.mp4
    private unsafe void Init(FileStream stream, int sr = 16000)
    {
        stream.Position = 0;
        int step = sr / 10;
        int length = (int)(stream.Length / (2 * step));
        if (length * step * 2 < stream.Length) length++;
        _values = new byte[length * 4];
        byte[] buff = new byte[step * 4 * 1000];
        fixed (Byte* pVal0 = _values)
        fixed (Byte* pBuff0 = buff)
        {
            float* pValF0 = (float*)pVal0;
            Int16* pBuffF0 = (Int16*)pBuff0;
            float scale = 1.0f / Int16.MaxValue;
            int offset = 0;
            while (true)
            {
                int count = stream.Read(buff);
                if (count <= 0) break;
                count = count / 2;  // byte size -> int16 size
                for (int i = 0; i < count; i ++)
                {
                    pValF0[offset + i / step] += Math.Abs(pBuffF0[i] * scale);
                }
                offset += count % step;
            }

            // 缩放信号
            float max = 0;
            for (int i = 0; i < length; i++)
            {
                max = Math.Max(max, pValF0[i]);
            }
            if(max > 0)
            {
                scale = 1 / max;
                for (int i = 0; i < length; i++)
                {
                    pValF0[i] = pValF0[i] * scale;
                }
            }
        }
    }

    public void Write(String filePath)
    {
        File.WriteAllBytes(filePath, _values);
    }

    public static void Export(String rawPcmFilePath, String pcmSr10FilePath, int sr = 16000)
    {
        PcmSR10 pcmSR10 = new PcmSR10(rawPcmFilePath, sr);
        pcmSR10.Write(pcmSr10FilePath);
    }
}

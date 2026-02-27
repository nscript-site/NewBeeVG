namespace NewBeeMedia.Pcm;

public class PcmS8FileReader : PcmSFileReader
{
    public PcmS8FileReader(String pcmPath)
        : base(pcmPath, 8000)
    {
    }
}

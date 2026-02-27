namespace NewBeeMedia;

public class PcmS16FileReader : PcmSFileReader
{
    public PcmS16FileReader(String pcmPath)
        : base(pcmPath, 16000)
    {
    }
}

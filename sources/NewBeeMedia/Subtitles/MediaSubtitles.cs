namespace NewBeeMedia.Subtitles;

/// <summary>
/// 媒体字幕
/// </summary>
public class MediaSubtitles : List<MediaSubtitleItem>
{
    /// <summary>
    /// 解码内部的内容。从数据库出来的数据，有时会将 \' 符号编码为 & #39;, 这里进行解码
    /// </summary>
    public void DecodeDbContents()
    {
        foreach (var item in this)
            item.DecodeDbContents();
    }

    public List<MediaClip> ToMediaClips()
    {
        List<MediaClip> list = new List<MediaClip>();
        foreach (var item in this)
            list.Add(item.ToMediaClip());
        return list;
    }

    public String ToSrt(ContentFlag flag, params ContentFlag[] otherFlags)
    {
        return ToSrt(true, flag, otherFlags);
    }

    public String ToSrt(bool removeNewlineInContent, ContentFlag flag, params ContentFlag[] otherFlags)
    {
        List<ContentFlag> list = new List<ContentFlag>();
        list.Add(flag);
        if (otherFlags != null) list.AddRange(otherFlags);
        ContentFlag[] flags = list.ToArray();
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < this.Count; i++)
        {
            MediaSubtitleItem item = this[i];
            SerializeToSrt(sb, item, i + 1, removeNewlineInContent, flags);
        }
        return sb.ToString();
    }

    public String ToAss(SubtitleStyle style, int frameWidth, int frameHeight, ContentFlag[] flags)
    {
        String ass = new SubExporter("AssCaption", this, style, frameWidth, frameHeight, flags).ExportAss();
        return ass;
    }

    const String NewLine = "\r\n";

    private void SerializeToSrt(StringBuilder sb, MediaSubtitleItem item, int num, bool removeNewlineInContent, ContentFlag[] flags)
    {
        sb.Append(num.ToString()).Append(NewLine);
        sb.Append(FormatTime(item.Start));
        sb.Append(" --> ");
        sb.Append(FormatTime(item.End)).Append(NewLine);

        foreach (var flag in flags)
        {
            String content = item[flag];
            if (content == null) content = String.Empty;
            sb.Append(removeNewlineInContent ? FormatSrtOutput(content) :content).Append(NewLine);
        }

        sb.Append(NewLine);
    }

    private String FormatSrtOutput(String txt)
    {
        if (txt == null) return null;
        return txt.Replace("\r", "").Replace("\n", "");
    }

    private String FormatTime(double time)
    {
        int hours = (int)(time / 3600);
        time -= hours * 3600;
        int mins = (int)(time / 60);
        time -= mins * 60;
        int seconds = (int)(time);
        time -= seconds;
        int ms = (int)(time * 1000);
        return String.Format("{0}:{1}:{2},{3}",
            hours.ToString().PadLeft(2, '0'),
            mins.ToString().PadLeft(2, '0'),
            seconds.ToString().PadLeft(2, '0'),
            ms.ToString().PadLeft(3, '0'));
    }
}

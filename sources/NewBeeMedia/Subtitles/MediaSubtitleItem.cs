namespace NewBeeMedia.Subtitles;

public class MediaSubtitleItem
{
    #region 内部类

    public class SubtitleItemContent
    {
        public ContentFlag Lang { get; set; }
        public String Text { get; set; }

        public void DecodeDbContents()
        {
            if (Text != null) Text = Text.Replace("& #39;", "'").Replace("&#39;", "'").Replace("_", " ");
        }
    }

    public class SubtitleItemContents : List<SubtitleItemContent>
    {
        public String this[ContentFlag flag]
        {
            get
            {
                foreach (var item in this)
                {
                    if (item.Lang == flag) return item.Text;
                }
                return null;
            }
            set
            {
                foreach (var sub in this)
                {
                    if (sub.Lang == flag)
                    {
                        sub.Text = value;
                        return;
                    }
                }

                SubtitleItemContent item = new SubtitleItemContent { Lang = flag, Text = value };
                this.Add(item);
            }
        }
    }

    #endregion

    public String Id { get; set; }

    public Double Start { get; set; }

    public Double End { get; set; }

    public RectF Bound { get; set; }

    /// <summary>
    /// 置信度
    /// </summary>
    public double Score { get; set; }

    public void Shift(double deltaTime)
    {
        this.Start += deltaTime;
        this.End += deltaTime;
    }

    public Double Duration
    {
        get { return End - Start; }
    }

    public void DecodeDbContents()
    {
        if(this.Contents != null)
        {
            foreach(var item in Contents)
            {
                item.DecodeDbContents();
            }
        }
    }

    public SubtitleItemContents Contents { get; private set; } = new SubtitleItemContents();

    public String this[ContentFlag flag]
    {
        get { return Contents == null ? null : Contents[flag]; }
        set {
            if (Contents == null) Contents = new SubtitleItemContents();
            Contents[flag] = value;
        }
    }

    public void Append(ContentFlag flag, String val)
    {
        if (val == null) return;
        String str = this[flag];
        if (str == null) str = String.Empty;
        this[flag] = str + val;
    }

    public String ToText(ContentFlag[] flags)
    {
        if (flags == null) return String.Empty;

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < flags.Length; i++)
        {
            String txt = this[flags[i]];
            if (String.IsNullOrEmpty(txt) == false)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(txt);
            }
        }
        return sb.ToString();
    }

    public MediaClip ToMediaClip()
    {
        return new MediaClip { Start = Start, End = End };
    }

    public void Clear() { Contents?.Clear(); }
}

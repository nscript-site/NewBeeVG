namespace NewBeeMedia.Subtitles;

public class SrtParser
{
    public class SrtItem
    {
        public MediaSubtitleItem Data { get; set; }
        public List<String> Lines { get; set; }

        public SrtItem()
        {
            Lines = new List<string>();
        }

        public void Fill(ContentFlag[] langs)
        {
            if (Data == null) return;

            if(langs == null || langs.Length == 0)
            {
                for (int i = 0; i < Lines.Count; i++)
                {
                    if (String.IsNullOrEmpty(Lines[i]) == false)
                    {
                        Data.Append(ContentFlag.None, Lines[i]);
                    }
                }
            }
            else if (langs.Length == 2)
            {
                if (Lines.Count > 0)
                {
                    if (String.IsNullOrEmpty(Lines[0]) == false)
                    {
                        Data.Append(langs[0], Lines[0]);
                    }
                }

                if (Lines.Count > 1)
                {
                    for (int i = 1; i < Lines.Count; i++)
                    {
                        if (String.IsNullOrEmpty(Lines[i]) == false)
                        {
                            Data.Append(langs[1], Lines[i]);
                        }
                    }
                }
            }
            else if (langs.Length == 1)
            {
                for (int i = 0; i < Lines.Count; i++)
                {
                    if (String.IsNullOrEmpty(Lines[i]) == false)
                    {
                        Data.Append(langs[0], Lines[i]);
                    }
                }
            }
        }
    }

    public MediaSubtitles Subtitles = new ();

    private Boolean IsId(String line)
    {
        if (String.IsNullOrEmpty(line)) return false;
        line = line.Trim();
        int id = -1;
        return int.TryParse(line, out id);
    }

    private Boolean IsTimeline(String line)
    {
        int count = 0;
        foreach (Char c in line)
        {
            if (c == ':') count++;
        }

        return (count >= 4);
    }

    public void Parse(String[] lines, params ContentFlag[] langs)
    {
        if (lines == null || lines.Length == 0) return;

        List<SrtItem> list = new List<SrtItem>();

        /* SRT 字幕格式
            45
            00:02:52,184 --> 00:02:53,617
            慢慢来
         */

        try
        {
            // 第一遍，逆向扫描，查找时间行，同时，将时间行之前的ID行给置空
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (IsTimeline(lines[i]) == true)
                {
                    for (int k = i - 1; k >= 0; k--)
                    {
                        if (IsId(lines[k]) == true)
                        {
                            lines[k] = String.Empty;
                            break;
                        }
                    }
                }
            }

            // 第二遍，正向扫描
            SrtItem srt = new SrtItem();
            list.Add(srt);

            for (int i = 0; i < lines.Length; i++)
            {
                String line = lines[i];
                if (IsTimeline(line) == true)
                {
                    if (srt.Data != null)
                    {
                        srt = new SrtItem();
                        list.Add(srt);
                    }

                    srt.Data = ParseSubtitle(line);
                }
                else if (srt.Data != null)
                {
                    String content = line.Trim();
                    srt.Lines.Add(content);
                }
            }

            // 解析文字
            foreach (SrtItem item in list)
            {
                item.Fill(langs);
                if (item.Data != null) Subtitles.Add(item.Data);
            }
        }
        catch (Exception ex)
        {
        }
    }

    private Boolean IsValidContent(String content)
    {
        return !content.StartsWith("{");
    }

    private MediaSubtitleItem ParseSubtitle(String line)
    {
        /* 
            时间轴格式
            00:02:52,184 --> 00:02:53,617
         */

        line = line.Replace("--", "");
        String[] terms = line.Split('>');
        if (terms.Length == 2)
        {
            String str1 = terms[0].Trim();
            String str2 = terms[1].Trim();
            MediaSubtitleItem subtitle = new MediaSubtitleItem();
            subtitle.Start = ParseTime(str1);
            subtitle.End = ParseTime(str2);
            return subtitle;
        }
        return null;
    }

    /// <summary>
    /// 将SRT 时间字符串解析为以秒为单位的时间
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private float ParseTime(String str)
    {
        String[] terms = str.Split(',');
        float time = 0;
        if (terms.Length > 0)
        {
            String s = terms[0];
            String[] timeStrs = s.Split(':');
            float[] times = new float[timeStrs.Length];
            for (int i = 0; i < timeStrs.Length; i++)
            {
                float t = 0;
                float.TryParse(timeStrs[i], out t);
                times[i] = t;
            }

            int scale = 1;
            for (int i = 0; i < times.Length; i++)
            {
                time += scale * times[times.Length - i - 1];
                scale *= 60;
            }
        }

        if (terms.Length == 2)
        {
            String msStr = terms[1].Trim();
            int ms = 0;
            int.TryParse(msStr, out ms);
            time += ms * 0.001f;
        }

        return time;
    }
}

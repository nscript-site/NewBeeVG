namespace NewBeeMedia;

public unsafe class MediaFilter : IDisposable
{
    AVFilterGraph* pFilter;

    /// <summary>
    /// 有字符串来初始化 filters。例如： InitFilters("movie=my_logo.png[wm];[in][wm]overlay=5:5[out]")
    /// </summary>
    /// <param name="filtersDescr"></param>
    /// <returns></returns>
    public unsafe int InitFilters(String filtersDescr)
    {
        int ret = 0;
        AVFilter* buffersrc = ffmpeg.avfilter_get_by_name("buffer");
        AVFilter* buffersink = ffmpeg.avfilter_get_by_name("ffbuffersink");
        AVFilterInOut* outputs = ffmpeg.avfilter_inout_alloc();
        AVFilterInOut* inputs = ffmpeg.avfilter_inout_alloc();
        AVBufferSinkParams* buffersink_params;
        pFilter = ffmpeg.avfilter_graph_alloc();
        return ret;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

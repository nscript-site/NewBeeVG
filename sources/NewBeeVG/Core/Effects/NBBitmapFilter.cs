using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// Bitmap Filter 的基类。
/// </summary>
public abstract class NBBitmapFilter
{
    /// <summary>
    /// 对给定的位图进行滤镜处理。如果生成的图的大小和原图不一致，则返回的偏移点表示新图相对于
    /// 原图的偏移量。如果一致，则偏移点为 (0, 0)。
    /// </summary>
    /// <param name="ctx">绘图上下文。</param>
    /// <param name="rect">要处理的矩形区域。</param>
    /// <param name="bitmap">要处理的位图。</param>
    /// <returns>返回处理后的位图和偏移点。</returns>
    public abstract (SKBitmap?, SKPoint) Filter(NBDrawContext ctx, SKRect rect, SKBitmap? bitmap);
}

public class NBBitmapFilterCollection : NBBitmapFilter
{
    private List<NBBitmapFilter> Filters { get; } = new List<NBBitmapFilter>();

    public void AddFilter(NBBitmapFilter filter)
    {
        if (filter != null)
        {
            Filters.Add(filter);
        }
    }

    public void RemoveFilter(NBBitmapFilter filter)
    {
        if (filter != null)
        {
            Filters.Remove(filter);
        }
    }

    public void ClearFilters()
    {
        Filters.Clear();
    }

    public int Count => Filters.Count;

    public bool IsEmpty => Filters.Count == 0;

    public override (SKBitmap?, SKPoint) Filter(NBDrawContext ctx, SKRect rect, SKBitmap? bitmap)
    {
        if (bitmap == null) return (null, new SKPoint());
        SKBitmap? currentBitmap = bitmap;
        SKPoint offset = new SKPoint();
        foreach (var filter in Filters)
        {
            var input = currentBitmap;
            try
            {
                var (newBitmap, newOffset) = filter.Filter(ctx, rect, input);
                currentBitmap = newBitmap;
                offset += newOffset;
            }
            catch(Exception ex)
            {
            }
            finally
            {
                // 中间产物的位图需要释放，避免内存泄漏
                if (input != bitmap && input != currentBitmap)
                {
                    input.Dispose();
                }
            }
        }
        return (currentBitmap, offset);
    }
}
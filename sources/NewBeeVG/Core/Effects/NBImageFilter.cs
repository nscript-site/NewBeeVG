using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// ImageFilter 的基类 NBImageFilter
/// </summary>
public abstract class NBImageFilter
{
    public abstract SKImageFilter? CreateFilter();
}

public class NBSimpleImageFilter : NBImageFilter
{
    private readonly SKImageFilter _filter;
    public NBSimpleImageFilter(SKImageFilter filter)
    {
        _filter = filter;
    }
    public override SKImageFilter? CreateFilter()
    {
        return _filter;
    }
}

/// <summary>
/// Represents a collection of shaders that can be composed together to create a single shader.
/// </summary>
public class NBImageFilterCollection
{
    public List<NBImageFilter> Filters { get; private set; } = new List<NBImageFilter>();

    public void AddFilter(NBImageFilter filter)
    {
        Filters.Add(filter);
    }

    public void ClearFilters()
    {
        Filters.Clear();
    }

    public bool IsEmpty()
    {
        return Filters.Count == 0;
    }

    public SKImageFilter? GetComposeFilter()
    {
        SKImageFilter? composed = null;
        if (Filters.Count == 0)
        {
            composed = null;
        }
        else if (Filters.Count == 1)
        {
            composed = Filters[0].CreateFilter();
        }
        else
        {
            var filters = new List<SKImageFilter>();

            foreach (var filterFunc in Filters)
            {
                var filter = filterFunc.CreateFilter();
                if (filter != null)
                {
                    filters.Add(filter);
                }
            }
            if (filters.Count == 1)
            {
                composed = filters[0];
            }
            else if (filters.Count > 1)
            {
                var f0 = filters[0];
                for (int i = 1; i < filters.Count; i++)
                {
                    f0 = SKImageFilter.CreateCompose(f0, filters[i]);
                }
                composed = f0;
            }
            else
            {
                composed = null;
            }
        }

        return composed;
    }
}


using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// ColorFilter 的基类 NBColorFilter
/// </summary>
public abstract class NBColorFilter
{
    public abstract SKColorFilter? CreateFilter();
}

public class NBSimpleColorFilter : NBColorFilter
{
    private readonly SKColorFilter _filter;
    public NBSimpleColorFilter(SKColorFilter filter)
    {
        _filter = filter;
    }
    public override SKColorFilter? CreateFilter()
    {
        return _filter;
    }
}

/// <summary>
/// Represents a collection of shaders that can be composed together to create a single shader.
/// </summary>
public class NBColorFilterCollection
{
    public List<NBColorFilter> Filters { get; private set; } = new List<NBColorFilter>();

    public void AddFilter(NBColorFilter shader)
    {
        Filters.Add(shader);
    }

    public void ClearFilters()
    {
        Filters.Clear();
    }

    public bool IsEmpty()
    {
        return Filters.Count == 0;
    }

    public SKColorFilter? GetComposeFilter()
    {
        SKColorFilter? composed = null;
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
            var filters = new List<SKColorFilter>();

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
                    f0 = SKColorFilter.CreateCompose(f0, filters[i]);
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


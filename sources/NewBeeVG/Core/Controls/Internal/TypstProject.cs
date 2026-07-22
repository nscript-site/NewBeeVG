namespace NewBeeVG.Internal;

/// <summary>
/// 封装 Typst 项目
/// </summary>
internal class TypstProject
{
    static Queue<TypstProject> Cache = new Queue<TypstProject>();

    static int CacheSize = 32;

    private TypstProject() { }

    public static TypstProject Create(string typstCode, Dictionary<string, string>? typstInputs = null)
    {
        TypstProject project;
        if (Cache.Count > 0)
        {
            project = Cache.Dequeue();
        }
        else
        {
            project = new TypstProject();
        }
        project.TypstCode = typstCode;
        project.TypstInputs = typstInputs;
        return project;
    }

    public string TypstCode { get; set; } = String.Empty;

    public Dictionary<string, string>? TypstInputs { get; set; }

    public string? SvgResult { get; set; }

    public override int GetHashCode()
    {
        if(TypstInputs != null && TypstInputs.Count > 0)
        {
            int hash = TypstCode.GetHashCode();
            foreach (var kvp in TypstInputs)
            {
                hash ^= kvp.Key.GetHashCode();
                hash ^= kvp.Value.GetHashCode();
            }
            return hash;
        }
        else
            return TypstCode.GetHashCode();
    }
}
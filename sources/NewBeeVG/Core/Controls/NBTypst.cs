using SkiaSharp;
using Typst.NET;

namespace NewBeeVG;

public class NBTypst : NBSvg
{
    public String Workspace { get; set; } = String.Empty;

    public string? TypstCode {
        get {
            if (RawTypeContent == null) return null;
            {
                var content = OnLoadConent(RawTypeContent);
                return content;
            }
        } 
    }

    public string? RawTypeContent { get; set; }

    public string? TypstFile { get; 
        set
        {
            field = value;
            if(String.IsNullOrEmpty(value) == false && File.Exists(value))
            {
                // Load the file or perform some action
                RawTypeContent = File.ReadAllText(value);
                var fileInfo = new FileInfo(value);
                Workspace = fileInfo.DirectoryName ?? new DirectoryInfo("./").FullName;
            }
        } 
    }

    public string? TypstContent
    {
        get;
        set
        {
            field = value;
            if (String.IsNullOrEmpty(value) == false)
            {
                RawTypeContent = field;
                Workspace = new DirectoryInfo("./").FullName;
            }
        }
    }

    protected virtual string OnLoadConent(string content)
    {
        return content;
    }

    protected internal Dictionary<string,string>? TypstInputs { get; set; }

    public Action<SKBitmap>? OnMeasureBitmap { get; set; }

    public String? SvgResult { get; private set; }

    public void UpdateInputs(IList<(string, object)> inputs)
    {
        if (TypstInputs == null) TypstInputs = new Dictionary<string, string>();

        foreach(var item in inputs)
        {
            TypstInputs[item.Item1] = item.Item2.ToString();
        }
        
        IsTypstLoaded = false;
        IsSvgLoaded = false;
    }

    protected override SKSize? GetImageSize()
    {
        TryLoadTypst();
        if (Source != null)
        {
            return new SKSize(Source.CullRect.Width, Source.CullRect.Height);
        }
        return null;
    }

    private bool IsTypstLoaded;
    private void TryLoadTypst()
    {
        if (IsTypstLoaded == true) return;

        IsTypstLoaded = true;

        if (String.IsNullOrEmpty(TypstCode) == false)
        {
            try
            {
                // 获取 LocalAppData 路径
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                // 拼接 typst packages 缓存目录
                string typstPackageCache = Path.Combine(localAppData, "typst", "packages");

                var options = new TypstCompilerOptions
                {
                    WorkspaceRoot = String.IsNullOrEmpty(Workspace) ? "." : Workspace,
                    Inputs = TypstInputs ?? new Dictionary<string, string>(),
                    CustomFontPaths = ["./fonts", "./assets/typography"],
                    IncludeSystemFonts = true,
                    PackagePath = typstPackageCache
                };

                using var compiler = new TypstCompiler(options);
                using var result = compiler.Compile(TypstCode);
                if (result.Success)
                {
                    var svg = result?.Document?.RenderPageToSvg(0);
                    SvgResult = svg;
                }
                else
                {
                    var err = result.Errors.FirstOrDefault();
                    if (err != null) 
                    {
                        DecodeException = new Exception($"Typst compilation error: {err.Message} at {err.Location}");
                    }
                }

                if(SvgResult != null)
                {
                    SvgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SvgResult));
                    TryLoadSvg();
                }
            }
            catch (Exception ex)
            {
                DecodeException = ex;
            }
        }
    }

    private bool IsBitmapMeasured;
    private void TryMeasureBitmap()
    {
        if (IsBitmapMeasured == true) return;
        IsBitmapMeasured = true;
        if (OnMeasureBitmap != null)
        {
        }
    }
}

public static partial class NBExtentions
{
    public static T TypstInputs<T>(this T self, Action<Dictionary<string, string>> onInputs) where T : NBTypst
    {
        var dic = new Dictionary<string, string>();
        onInputs(dic);
        self.TypstInputs = dic;
        return self;
    }

    public static T TypstInputs<T>(this T self, IList<(string, object)> inputs) where T : NBTypst
    {
        var dic = new Dictionary<string, string>();
        foreach (var item in inputs)
            dic[item.Item1] = item.Item2.ToString();
        self.TypstInputs = dic;
        return self;
    }

    public static T TypstInputs<T>(this T self, params (string, object)[] inputs) where T : NBTypst
    {
        var dic = new Dictionary<string, string>();
        foreach (var item in inputs)
            dic[item.Item1] = item.Item2.ToString();
        self.TypstInputs = dic;
        return self;
    }

    public static T TypstInputs<T>(this T self, string key, object val) where T : NBTypst
    {
        var dic = new Dictionary<string, string>();
        dic[key] = val.ToString();
        self.TypstInputs = dic;
        return self;
    }

    public static T UpdateInputs<T>(this T self, string key, object val) where T : NBTypst
    {
        var list = new List<(string, object)>();
        list.Add((key, val));
        self.UpdateInputs(list);
        return self;
    }

    public static T UpdateInputs<T>(this T self, params (string, object)[] inputs) where T : NBTypst
    {
        var list = new List<(string, object)>();
        list.AddRange(inputs);
        self.UpdateInputs(list);
        return self;
    }

    public static T UpdateInputs<T>(this T self, IList<(string, object)> inputs) where T : NBTypst
    {
        var list = new List<(string, object)>();
        list.AddRange(inputs);
        self.UpdateInputs(list);
        return self;
    }
}
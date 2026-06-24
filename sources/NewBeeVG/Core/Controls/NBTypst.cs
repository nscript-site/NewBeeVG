using SkiaSharp;
using Typst.NET;

namespace NewBeeVG;

public class NBTypst : NBSvg
{
    public String Workspace { get; set; } = String.Empty;

    public string? TypstCode { get; set; }

    public string? TypstFile { get; 
        set
        {
            field = value;
            if(String.IsNullOrEmpty(value) == false && File.Exists(value))
            {
                // Load the file or perform some action
                TypstCode = File.ReadAllText(value);
                var fileInfo = new FileInfo(value);
                Workspace = fileInfo.DirectoryName ?? new DirectoryInfo("./").FullName;
            }
        } 
    }

    public Dictionary<string,string>? TypstInputs { get; set; }

    public String? SvgResult { get; private set; }

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

                if(SvgResult != null)
                {
                    SvgStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SvgResult));
                    TryLoadSvg();
                }

            }
            catch (Exception ex)
            {
                SvgLoadException = ex;
            }
        }
    }
}

public static partial class NBExtentions
{
    public static T TypstInputs<T>(this T widget, Action<Dictionary<string,string>> onInputs) where T : NBTypst
    {
        var dic = new Dictionary<string, string>();
        onInputs(dic);
        widget.TypstInputs = dic;
        return widget;
    }
}
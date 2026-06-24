using SkiaSharp;
using Svg.Skia;
using Typst.NET;

namespace NewBeeVG;

public class NBTypst : NBLayoutable, INBImage
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

    public SKPicture? Source { get; private set; }

    private SKSvg? Svg { get; set; }

    public Exception? SvgLoadException { get; private set; }

    private bool IsSvgLoaded;

    public Stretch Stretch { get; set; } = Stretch.Uniform;

    public StretchDirection StretchDirection { get; set; } = StretchDirection.Both;

    public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;

    private void TryLoadSvg()
    {
        if (IsSvgLoaded == true) return;

        IsSvgLoaded = true;

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
                    using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(SvgResult));
                    Svg = new SKSvg();
                    Source = Svg.Load(stream);
                    IsSvgLoaded = true;
                }

            }
            catch (Exception ex)
            {
                SvgLoadException = ex;
            }
        }
    }

    /// <summary>
    /// Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        TryLoadSvg();

        var source = Source;
        var result = new Size();

        if (source != null)
        {
            result = Stretch.CalculateSize(availableSize, new Size(source.CullRect.Width, source.CullRect.Height), StretchDirection);
        }

        return result;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var source = Source;

        if (source != null)
        {
            var sourceSize = new Size(source.CullRect.Width, source.CullRect.Height);
            var result = Stretch.CalculateSize(finalSize, sourceSize, StretchDirection);
            return result;
        }
        else
        {
            return new Size();
        }
    }

    protected override void RenderCore(SKCanvas context)
    {
        var source = Source;

        if (source != null && Bounds.Width > 0 && Bounds.Height > 0)
        {
            var viewPort = new SKRect(Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Bottom);
            var sourceSize = new SKSize(source.CullRect.Width, source.CullRect.Height);

            Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
            SKSize scaledSize = new SKSize((float)(sourceSize.Width * scale.X), (float)(sourceSize.Height * scale.Y));
            SKRect dest = viewPort
                .CenterRect(new SKRect(0, 0, scaledSize.Width, scaledSize.Height))
                .IntersectRect(viewPort);
            SKRect src = new SKRect(0, 0, sourceSize.Width, sourceSize.Height)
                .CenterRect(new SKRect(0, 0, (float)(dest.Width / scale.X), (float)(dest.Height / scale.Y)));

            // 计算绘制矩阵


            // 绘制图像
            using var paint = new SKPaint { BlendMode = BlendMode };

            context.Save();

            // 计算缩放和平移
            float scaleX = dest.Width / src.Width;
            float scaleY = dest.Height / src.Height;
            // 平移到目标区域
            context.Translate(dest.Left, dest.Top);
            // 缩放到目标大小
            context.Scale(scaleX, scaleY);
            // 平移到原点（如果 cullRect 不从 0,0 开始）
            context.Translate(-src.Left, -src.Top);

            context.DrawPicture(source, paint);

            context.Restore();
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
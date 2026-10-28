using NewBeeVG.Internal;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.Text;
using Typst.NET;

namespace NewBeeVG;

public class NBTypstAnimation : NBBaseLoopAnimation
{
    public String Workspace { get; protected set; } = String.Empty;

    public string? RawTypeContent { get; protected set; }

    public List<SKPicture> TypstFrames { get; protected set; } = new List<SKPicture>();

    public string? TypstFile
    {
        get;
        set
        {
            field = value;
            if (String.IsNullOrEmpty(value) == false && File.Exists(value))
            {
                // Load the file or perform some action
                RawTypeContent = File.ReadAllText(value);
                var fileInfo = new FileInfo(value);
                Workspace = fileInfo.DirectoryName ?? new DirectoryInfo("./").FullName;
                TryLoadResource();
            }
            else
            {
                DecodeException = new IOException($"文件不存在: {value}");
            }
        }
    }

    private bool IsResourceLoaded;
    private void TryLoadResource()
    {
        if (IsResourceLoaded == true) return;

        IsResourceLoaded = true;
        TypstFrames.Clear();
        if (String.IsNullOrEmpty(RawTypeContent) == false)
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
                    CustomFontPaths = ["./fonts", "./assets/typography"],
                    IncludeSystemFonts = true,
                    PackagePath = typstPackageCache
                };

                using var compiler = new TypstCompiler(options);
                using var result = compiler.Compile(RawTypeContent);
                if (result.Success)
                {
                    var doc = result?.Document;
                    if (doc != null && doc.PageCount > 0)
                    {
                        for (int i = 0; i < doc.PageCount; i++)
                        {
                            var svgContent = doc.RenderPageToSvg(i);
                            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent));
                            var svg = new SKSvg();
                            svg.Load(ms);
                            if (svg.Picture != null)
                            {
                                TypstFrames.Add(svg.Picture);
                            }
                        }
                    }
                }
                else
                {
                    var err = result.Errors.FirstOrDefault();
                    if (err != null)
                    {
                        DecodeException = new Exception($"Typst compilation error: {err.Message} at {err.Location}");
                    }
                }
                TotalFrames = TypstFrames.Count;
            }
            catch (Exception ex)
            {
                DecodeException = ex;
            }
        }
    }

    protected override SKSize? GetImageSize()
    {
        TryLoadResource();
        if (TypstFrames != null && TypstFrames.Count > 0)
        {
            return TypstFrames[0].CullRect.Size;
        }
        return null;
    }


    protected override void Draw(SKCanvas context, SKRect src, SKRect dest, SKPaint paint)
    {
        if (TypstFrames == null) return;

        int frame = NextFrame(TotalFrames, LoopMode, Speed);
        if (frame < 0 || frame >= TypstFrames.Count) return;
        var pic = TypstFrames[frame];
        if (pic == null) return;

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

        context.DrawPicture(pic, paint);

        context.Restore();
    }
}

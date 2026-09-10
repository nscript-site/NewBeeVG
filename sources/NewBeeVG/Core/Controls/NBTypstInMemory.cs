using System;
using System.Collections.Generic;
using System.Text;

namespace NewBeeVG;

public class NBTypstInMemory : NBTypst
{
    public int? PageWidth { get; set; }
    public int? PageHeight { get; set; }
    public Thickness? PageMargin { get; set; }
    public float? FontSize { get; set; }

    protected override string OnLoadConent(string content)
    {
        var body = base.OnLoadConent(content);
        var header = LoadHeader();
        var footer = LoadFooter();
        var sbPage = new StringBuilder();
        if (header != null) sbPage.Append(header);
        sbPage.AppendLine(body);
        if (footer != null) sbPage.AppendLine(footer);
        return sbPage.ToString();
    }

    protected virtual string? LoadHeader()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"#set page(fill: none)");

        var width = PageWidth == null ? "auto" : $"{PageWidth}pt";
        var height = PageHeight == null ? "auto" : $"{PageHeight}pt";

        sb.AppendLine($"#set page(  width: {width}, height: {height})");

        //#set page(
        //    margin: (
        //    top: 40px,
        //    bottom: 30px,
        //    left: 35px,
        //    right: 35px,
        //  )
        //)
        if(PageMargin != null)
        {
            var m = PageMargin.Value;
            sb.AppendLine($"""
                #set page(
                    margin: (
                    top: {m.Top}pt,
                    bottom: {m.Bottom}pt,
                    left: {m.Left}pt,
                    right: {m.Right}pt,
                    )
                )
                """);
        }

        if(FontSize != null)
        {
            sb.AppendLine($"#set text(size: {FontSize}pt)");
        }

        return sb.ToString();
    }

    protected virtual string? LoadFooter()
    {
        return null;
    }
}

public class NBTypstMath : NBTypstInMemory
{
}

public class NBTypstCode : NBTypstInMemory
{
    public string? Lang { get; set; }
    public bool ShowLang { get; set; } = true;
    protected override string? LoadHeader()
    {
        var showLang = ShowLang ? "true" : "false";
        var sb = new StringBuilder();
        sb.AppendLine(@"#import ""@preview/zebraw:0.6.3"": *");
        sb.AppendLine(@"#show: zebraw");
        sb.AppendLine(base.LoadHeader());
        sb.AppendLine($"""
            #zebraw(
            lang: {showLang},
            ```{Lang}
            """);
        return sb.ToString();
    }

    protected override string? LoadFooter()
    {
        return """
            ```
            )
            """;
    }
}

public static partial class NBExtentions
{
    public static T PageMargin<T>(this T self, int left, int top, int right, int bottom) where T : NBTypstInMemory
    {
        self.PageMargin = new Thickness(left, top, right, bottom);
        return self;
    }

    public static T PageMargin<T>(this T self, int uniform) where T : NBTypstInMemory
    {
        self.PageMargin = new Thickness(uniform);
        return self;
    }

    public static T PageMargin<T>(this T self, int horizontal, int vertical) where T : NBTypstInMemory
    {
        self.PageMargin = new Thickness(horizontal, vertical);
        return self;
    }

    public static T PageSize<T>(this T self, int? width, int? height) where T : NBTypstInMemory
    {
        self.PageWidth = width;
        self.PageHeight = height;
        return self;
    }

    public static T FontSize<T>(this T self, float? fontSize) where T : NBTypstInMemory
    {
        self.FontSize = fontSize;
        return self;
    }

    public static T Lang<T>(this T self, string? lang, bool showLang = true) where T : NBTypstCode
    {
        self.Lang = lang;
        self.ShowLang = showLang;
        return self;
    }
}

# 显示文字

## 普通文本

`NBText` 为普通文本组件，每个 `NBText`  组件拥有独立的文字属性。

`TextBlock` 或 `Text` 扩展方法可以创建普通文字组件 `NBText`。原型如下:

```csharp
public static NBText Text(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null, bool wrap = true, int textAlign = -1)
{
    return TextBlock(text, fontSize, color, fontFamily, wrap, textAlign);
}

public static NBText TextBlock(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null, bool wrap = true, int textAlign = -1)
{
    var tb = new NBText { Text = text, FontFamily = fontFamily ?? GetDefaultFontFamily(), FontSize = fontSize, Foreground = color ?? SKColors.Black };
    tb.IsWrapText = wrap;
    tb.TextAlign = textAlign.ToNBTextAlign();
    return tb;
}
```

[texts.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/texts.cs) 示例是普通文本的使用演示。

## 富文本

`NBRichText` 为富文本组件，每个 `NBRichText` 由多个 `NBTextRun` 组成，每个 `NBTextRun` 拥有独立的文字属性。

可以通过 `TextRun` 和 `RichText` 扩展方法，创建 `NBTextRun` 和 `NBRichText`。原型如下:

```csharp
public static NBTextRun TextRun(string text = "", float fontSize = 40, SKColor? color = null, string? fontFamily = null)
{
    var tb = new NBTextRun { Text = text, FontFamily = fontFamily ?? GetDefaultFontFamily(), FontSize = fontSize, Foreground = color ?? SKColors.Black };
    return tb;
}

public static NBRichText RichText(NBTextRun?[]? runs = null, bool wrap = true, int textAlign = -1)
{
    var tb = new NBRichText();
    tb.IsWrapText = wrap;
    tb.TextAlign = textAlign.ToNBTextAlign();
    if(runs != null)
    {
        foreach(var run in runs)
        {
            if (run != null)
            {
                tb.Add(run);
            }
        }
    }
    return tb;
}
```

[richtexts.cs](https://github.com/nscript-site/NewBeeVG/blob/main/apps/files/richtexts.cs) 示例是富文本的使用演示。

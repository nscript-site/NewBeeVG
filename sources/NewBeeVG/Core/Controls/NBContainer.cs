/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

namespace NewBeeVG;

public class NBContainer : NBLayoutable
{
    public NBContainerSizing Sizing { get; set; } = NBContainerSizing.Normal;

    private Lazy<NBVisualQueryProvider> LazyQueryProvider => new(() => new NBVisualQueryProvider(this));

    internal NBVisualQueryProvider QueryProvider => LazyQueryProvider.Value;
}

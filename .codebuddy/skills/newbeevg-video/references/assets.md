# 资源目录、素材生成与组合

## 1. 目录结构

`assets` 是资源根目录，固定按类型分目录：

```text
assets/
├── audios/      # mp3 / wav
├── images/      # png / jpg / webp；PngAnim 的 APNG 内容也用 .png 后缀
├── videos/      # mp4 / webm 等
├── svgs/        # 静态或带动画的 svg
├── lottie/      # Lottie JSON（LottieAnim）
├── typst/       # .typ 及依赖（TypstAnim）
└── README.md    # 资源清单与说明
```

## 2. 查找顺序

视频脚本运行时，按以下顺序找 `assets`：

1. 脚本当前目录：`./assets`；
2. 当前目录的父目录：`../assets`；
3. 继续向上：`../../assets`；
4. 直到项目根目录的 `assets`。

使用**第一个存在**的 `assets` 目录。都没有时，在脚本当前目录创建 `assets`。

项目根判定：从脚本目录向上，最近的包含 `.git`、`Directory.Build.props` 或 `.codebuddy` 的目录。

```csharp
static string FindAssetsDir(string startDir)
{
    var start = new DirectoryInfo(startDir);
    DirectoryInfo? root = null;

    // 项目根：包含 .git / Directory.Build.props / .codebuddy 的最近目录
    for (var d = start; d != null; d = d.Parent)
    {
        if (Directory.Exists(Path.Combine(d.FullName, ".git")) ||
            File.Exists(Path.Combine(d.FullName, "Directory.Build.props")) ||
            Directory.Exists(Path.Combine(d.FullName, ".codebuddy")))
        {
            root = d;
            break;
        }
    }

    // 从脚本目录向上找 assets，最多找到项目根
    for (var d = start; d != null; d = d.Parent)
    {
        var assets = Path.Combine(d.FullName, "assets");
        if (Directory.Exists(assets)) return assets;
        if (d.FullName == root?.FullName) break;
    }

    var fallback = Path.Combine(startDir, "assets");
    Directory.CreateDirectory(fallback);
    return fallback;
}
```

创建子目录：

```csharp
void EnsureAssetDirs(string assets)
{
    foreach (var sub in new[] { "audios", "images", "videos", "svgs", "lottie", "typst" })
        Directory.CreateDirectory(Path.Combine(assets, sub));
}
```

## 3. 优先读取 `README.md`

找到 `assets` 后，**先读 `README.md`**，再决定是否存在可用素材。README 通常记录：

- 文件名与路径；
- 用途（背景、图标、数据图、音效…）；
- 尺寸、时长、格式；
- 来源 / 授权；
- 使用注意事项。

建议格式：

```markdown
# Assets

| 文件 | 类型 | 用途 | 尺寸/时长 | 备注 |
| --- | --- | --- | --- | --- |
| images/bg-night.png | 图片 | 夜景背景 | 1920×1080 | 可裁切 |
| svgs/icon-chart.svg | SVG | 图表图标 | 64×64 | 单色 |
| audios/intro.mp3 | 音频 | 片头音乐 | 12s | 淡入淡出 |
```

新增素材后同步更新 README。

## 4. 素材盘点流程

写代码前：

1. **分析稿件**：按镜头/clip 拆分，列出每个 clip 需要的视觉与音频元素；
2. **读取 README 与目录**：标记已有素材，确认尺寸/时长/风格是否匹配；
3. **列出缺失项**：背景、图标、插画、图表、照片、音效、视频等；
4. **生成缺失素材**：优先 SVG；简单动画转 APNG，数学/科学排版用 Typst；复杂特效先查 `modules/` 现有 Skia 模板，最后才新建 SkiaAnim；
5. **保存并登记**：放入对应子目录，更新 README。

## 5. 用 SVG 生成缺失素材

优先让大模型生成 SVG：可以是静态图，也可以带简单动画。

保存位置：

- 优先：视频脚本当前目录的 `assets/svgs/`；
- 该目录不存在时创建；
- 文件名体现用途，例如 `svgs/flow-arrow.svg`、`svgs/chart-bars.svg`。

生成要求：

- 使用明确的 `width` / `height` / `viewBox`；
- 自包含：不引用外部图片、字体、脚本；
- 颜色尽量使用主题色，或使用 `currentColor` 便于替换；
- 图标/插画保持分层与语义化命名（`<g id="...">`）；
- 动画优先使用简单 SMIL（`animate` / `animateTransform`）或内联 CSS；
- 动画 SVG 需要实际渲染验证；若 Svg.Skia 不支持某个特性，可转成 APNG（`PngAnim`）或改用 `SkiaAnim` 实现动效。

静态 SVG 示例：

```svg
<svg xmlns="http://www.w3.org/2000/svg" width="320" height="320" viewBox="0 0 320 320">
  <g id="bars" fill="#F0B429">
    <rect x="40"  y="180" width="48" height="100" rx="8"/>
    <rect x="110" y="120" width="48" height="160" rx="8"/>
    <rect x="180" y="70"  width="48" height="210" rx="8"/>
    <rect x="250" y="30"  width="48" height="250" rx="8"/>
  </g>
</svg>
```

动画 SVG 示例（SMIL 旋转）：

```svg
<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200">
  <g id="gear" fill="#4FA3D1">
    <circle cx="100" cy="100" r="60"/>
    <rect x="92" y="20" width="16" height="40" rx="4"/>
    <animateTransform attributeName="transform" type="rotate"
                      from="0 100 100" to="360 100 100"
                      dur="4s" repeatCount="indefinite"/>
  </g>
</svg>
```

如果 SVG 动画在预览中不生效，改用 `SkiaAnim` 绘制同一图形，或导出成帧序列/视频。

### 转成 APNG（`PngAnim`）

画面简单或纯矢量的动画，建议把 SVG 转成 APNG 后嵌入，避免依赖 Svg.Skia 对 SMIL/CSS 的支持：

```bash
npx --yes svg-animation-convert --input .\your.svg --output your.apng
```

转换器只接受 `.apng` 中间输出，**转换完成后必须重命名为 `.png`**：

```powershell
Move-Item .\your.apng .\assets\images\your.png -Force
```

```bash
mv ./your.apng ./assets/images/your.png
```

无限循环的 SVG 必须指定时长，否则转换可能无法结束或帧数不确定：

```bash
npx --yes svg-animation-convert --input .\your.svg --output your.apng --duration 3
mv ./your.apng ./assets/images/your.png
```

- `--duration` 按秒计，覆盖一个完整循环或镜头所需时长；
- 最终交付后缀必须是 `.png`，不能是 `.apng`；`assets/images/` 下不要残留 `.apng`；
- 用 `PngAnim("./assets/images/your.png")` 引入；
- 转换后检查循环衔接、透明区域、分辨率和文件体积；可用 `inspect_apng.py --require-png` 校验后缀。

## 6. 引用素材

路径相对于运行时目录（通常为脚本所在目录）：

```csharp
// 当前脚本目录下的 assets
Image("./assets/images/bg-night.png")
    .Size(1920, 1080)
    .Stretch(Stretch.UniformToFill);

SVG("./assets/svgs/icon-chart.svg")
    .Size(320, 320);

AudioFile("./assets/audios/intro.mp3")
    .AsClip("audio").LinkTo("start");
```

引用父目录的 assets：

```csharp
Image("../assets/images/bg-night.png");
```

## 7. 组合原则：布局 + 动画 + 素材

每个 clip 尽量有两类以上元素，不要只用一个全屏动画：

- **布局组件**：分栏、留白、层级、标题区；
- **动画组件**：优先 APNG（`PngAnim`）、Typst（`TypstAnim`）或现有 `modules/` 中的 Skia 模板；只有它们无法实现或效果不好时，才新建 `SkiaAnim`；`LottieAnim` 仅作兜底；
- **素材**：背景图/视频、SVG 图标/插画、音频。

**生成动画素材的优先级**：优先在 APNG、Typst、现有 Skia 模板中选择；只有它们都无法实现或效果不好时，才新建 SkiaAnim。不要一上来就写新的 Skia 动画。

同一画面可通过 `HGrid` / `HStack` / `Panel` 嵌入多个动画节点；它们共享当前 clip 的帧号，需要独立时间轴时拆成多个 clip 并用 `start` 叠加。

推荐结构：

```csharp
Panel([
    Image("./assets/images/bg.png").Size(1920, 1080).Stretch(Stretch.UniformToFill),

    HGrid("480,*", [
        SVG("./assets/svgs/icon-chart.svg").Size(320, 320).Align(0, 0),

        HStack([
            SkiaAnim(new ChartAnimation(600, 800, 90, content)),
            VStack([
                TextBlock("季度增长").Font(72, SKColors.White),
                TextBlock("+128%").Font(160, SKColors.Orange),
            ]).Spacing(20),
        ]).Spacing(40),
    ]).Margin(120),

    TextBlock("数据来源：内部统计").Font(32, SKColors.LightGray)
        .Align(-1, 1).Margin(80),
])
.AsClip(90, name: "chart");
```

不推荐：

```csharp
// 整个 clip 只有一个全屏动画，缺少信息层级与静态装饰
SkiaAnim(new FullScreenAnimation(1920, 1080, 90, content))
    .AsClip(90, name: "chart");
```

例外：片头/片尾若确实只需要一句标题或一个视觉主体，可以用纯文字或单个动画。

# 视频工程目录、素材生成与组合

## 1. 目录结构

项目根以 `Templates/Project/README.md` 为准：

```text
<project_root>/
├── Directory.Build.props
├── README.md
├── modules/                     # 跨工程公共 SkiaAnim 模板
│   ├── README.md
│   └── GenRun.cs
├── assets/                      # 跨工程共享的非 AIGC 素材
├── todo/
└── scripts/
    └── <ScriptProjectName>/     # 一个子目录 = 一部片
        ├── README.md
        ├── script.md            # 剧本/口播稿
        ├── storyboard.md        # 分镜
        ├── script.cs            # C# 视频脚本
        ├── modules/             # 本片自用 SkiaAnim
        ├── assets/              # 本片非 AIGC 素材（可再分子目录）
        ├── gen/                 # 全部生成产物
        │   ├── images/          # AIGC 图像与 prompts.md
        │   ├── apngs/           # APNG，后缀统一为 .png
        │   ├── audios/          # TTS 语音 + audio_timeline.md
        │   ├── videos/          # AIGC/后处理视频
        │   ├── typsts/          # TypstAnim 的 .typ 及依赖
        │   └── svgs/            # 生成或编辑的 SVG
        ├── tools/               # png_frames_to_apng.py 等本片工具
        └── output/              # mp4、校对帧
```

创建视频工程：

```bash
# 项目根
nbvg new scripts/<ScriptProjectName>

# 已在 scripts/ 下
nbvg new <ScriptProjectName>
```

规则：

- `script.md`、`storyboard.md`、`script.cs` 固定放在视频工程根目录；
- `assets/` 只放**非 AIGC** 素材，保留用户原有目录和文件名；
- 所有 AIGC/生成结果写入 `gen/`，不得写入任何 `assets/`；
- 本片 `modules/` 放自创动画；可复用的公共模板放项目根 `modules/`；
- 最终 MP4 和校对帧放本片 `output/`。

## 2. 查找与引用

从 `scripts/<ScriptProjectName>/` 运行 `script.cs`，路径相对于该目录：

| 用途 | 路径 |
| --- | --- |
| 本片非 AIGC 素材 | `./assets/...` |
| 本片 AIGC 图片 | `./gen/images/...` |
| 本片 APNG | `./gen/apngs/...` |
| 本片语音/音乐 | `./gen/audios/...` |
| 本片 AIGC 视频 | `./gen/videos/...` |
| 本片 Typst | `./gen/typsts/...` |
| 本片生成 SVG | `./gen/svgs/...` |
| 本片动画模板 | `./modules/Xxx.cs` |
| 项目根共享素材 | `../../assets/...` |
| 项目根公共模板 | `../../modules/Xxx.cs` |
| 渲染产物 | `./output/...` |

查找顺序：

1. 先递归扫描本片 `./assets/`，按目录名、文件名、README 和实际内容匹配；
2. 再查本片 `./gen/` 是否已有可复用生成物；
3. 再查项目根 `../../assets/` 的共享素材；
4. 最后才生成缺失素材。

不要用“最近一个 assets”代替全部查找：本片 `assets/` 和根 `assets/` 角色不同，二者都可直接引用。

## 3. 优先读取 `README.md`

先读本片 `README.md`；共享 `assets/` 或 `modules/` 若有 README 也先读，再决定是否存在可用素材。README 通常记录：

- 文件名与路径；
- 用途（背景、图标、数据图、音效…）；
- 尺寸、时长、格式；
- 来源 / 授权；
- 使用注意事项。

本片 README 建议格式：

```markdown
# <ScriptProjectName>

## 素材

| 文件 | 类型 | 用途 | 尺寸/时长 | 来源 |
| --- | --- | --- | --- | --- |
| assets/bg-night.jpg | 图片 | 夜景背景 | 1920×1080 | 非 AIGC |
| assets/brand/logo.svg | SVG | 品牌 Logo | 64×64 | 非 AIGC |
| gen/images/bg-ai.png | 图片 | AIGC 生成背景 | 1920×1080 | Seedream |
| gen/svgs/icon-chart.svg | SVG | 图表图标 | 64×64 | 生成 |
| gen/apngs/pulse.png | APNG | 脉冲动画 | 320×320 / 3s | 生成 |
| gen/audios/shot01.mp3 | 音频 | 第一镜口播 | 3.40s | TTS |
```

新增本片素材后同步更新该 README；新增公共素材或公共模板时更新根 `assets/README.md`、根 `modules/README.md`。

## 4. 非 AIGC 素材规则

本片 `assets/` 与根 `assets/` 都是**非 AIGC 素材库**，可能包含图片、视频、音频、SVG、序列帧等，也可能按来源、主题或项目分成多层子目录。

使用规则：

- **优先复用**：素材盘点时先递归扫描，按目录名、文件名、README 描述和实际内容匹配；
- **保留原结构**：不要移动、改名、重排或覆盖原文件；代码直接引用原路径；
- **只读**：AIGC 结果、裁剪/调色后的中间产物不要写回 `assets/`，应写入 `gen/images`、`gen/videos`、`gen/svgs` 等目录；
- **先读索引**：先读本片 `README.md`，再读共享目录的 README（若存在）；
- 若素材需要裁切、调色或转码，派生文件另存到 `gen/`，并在 README 记录与原始素材的对应关系。

## 5. 素材盘点流程

写代码前：

1. **读取 `script.md` 并产出分镜**：在同目录生成或补全 `storyboard.md`，按镜头/clip 列出 `shotNN`、口播、画面和所需视觉/音频元素；
2. **先查非 AIGC 素材**：递归读取本片 `assets/` 与根 `../../assets/`，记录可复用路径、尺寸、时长和风格；
3. **再查生成物与模板**：检查本片 `gen/`、本片 `modules/README.md`、根 `modules/README.md`，列出真正缺失项；
4. **生成缺失素材**：优先 SVG；简单动画转 APNG，数学/科学排版用 Typst；复杂特效先查现有 Skia 模板，最后才新建 SkiaAnim；
5. **生成并登记语音**：按分镜合成语音到 `gen/audios/shotNN.mp3`，并把入点/出点写入 `gen/audios/audio_timeline.md`；
6. **保存并登记素材**：AIGC 结果放入本片 `gen/` 对应子目录，更新本片 `README.md`；不要写入任何 `assets/`。

## 6. 用 SVG 生成缺失素材

优先让大模型生成 SVG：可以是静态图，也可以带简单动画。

保存位置：

- AIGC/编辑后的 SVG：本片 `gen/svgs/`；
- 目录不存在时创建；
- 文件名体现用途，例如 `gen/svgs/flow-arrow.svg`、`gen/svgs/chart-bars.svg`。

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
npx --yes svg-animation-convert --input ./gen/svgs/your.svg --output your.apng
```

转换器只接受 `.apng` 中间输出，**转换完成后必须重命名为 `.png`**：

```powershell
Move-Item .\your.apng .\gen\apngs\your.png -Force
```

```bash
mv ./your.apng ./gen/apngs/your.png
```

无限循环的 SVG 必须指定时长，否则转换可能无法结束或帧数不确定：

```bash
npx --yes svg-animation-convert --input ./gen/svgs/your.svg --output your.apng --duration 3
mv ./your.apng ./gen/apngs/your.png
```

- `--duration` 按秒计，覆盖一个完整循环或镜头所需时长；
- 最终交付后缀必须是 `.png`，不能是 `.apng`；`gen/apngs/` 下不要残留 `.apng`；
- 用 `PngAnim("./gen/apngs/your.png")` 引入；
- 转换后检查循环衔接、透明区域、分辨率和文件体积；可用 `inspect_apng.py --require-png` 校验后缀；
- 视频工程自带的逐帧工具位于 `tools/png_frames_to_apng.py`，用于把逐帧 PNG 合成全帧 APNG。

## 7. 引用素材

路径相对于 `script.cs` 所在目录：

```csharp
// 本片非 AIGC 素材
Image("./gen/images/bg-night.png")
    .Size(1920, 1080)
    .Stretch(Stretch.UniformToFill);

// 本片生成 SVG
SVG("./gen/svgs/icon-chart.svg")
    .Size(320, 320);

// 本片语音
AudioFile("./gen/audios/shot01.mp3")
    .AsClip("audio_01").LinkTo("shot01");
```

引用项目根共享素材：

```csharp
Image("../../assets/brand/logo.png");
```

引入模板：

```csharp
#:include modules/MyAnimation.cs
#:include ../../modules/GlintTextA.cs
```

## 8. 组合原则：布局 + 动画 + 素材

每个 clip 尽量有两类以上元素，不要只用一个全屏动画：

- **布局组件**：分栏、留白、层级、标题区；
- **动画组件**：优先 APNG（`PngAnim`）、Typst（`TypstAnim`）或现有 `modules/` 中的 Skia 模板；只有它们无法实现或效果不好时，才新建 `SkiaAnim`；`LottieAnim` 仅作兜底；
- **素材**：背景图/视频、SVG 图标/插画、音频。

**生成动画素材的优先级**：优先在 APNG、Typst、现有 Skia 模板中选择；只有它们都无法实现或效果不好时，才新建 SkiaAnim。不要一上来就写新的 Skia 动画。

同一画面可通过 `HGrid` / `HStack` / `Panel` 嵌入多个动画节点；它们共享当前 clip 的帧号，需要独立时间轴时拆成多个 clip 并用 `start` 叠加。

推荐结构（示例假设文件头部已声明 `var theme = NBVideoThemes.XXX;` 与 `font(fontName)`）：

```csharp
Panel([
    Image("./assets/bg.png").Size(1920, 1080).Stretch(Stretch.UniformToFill),

    HGrid("480,*", [
        SVG("./gen/svgs/icon-chart.svg").Size(320, 320).Align(0, 0),

        HStack([
            SkiaAnim(new ChartAnimation(600, 800, 90, content)),
            VStack([
                TextBlock("季度增长").Font(72, theme.TextPrimary),
                TextBlock("+128%").Font(160, theme.Primary),
            ]).Spacing(20),
        ]).Spacing(40),
    ]).Margin(120),

    TextBlock("数据来源：内部统计").Font(32, theme.TextSecondary)
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

---
name: newbeevg-apng-animation
description: This skill should be used when generating, converting, validating, or embedding APNG (animated PNG) assets, especially for NewBeeVG PngAnim in dotnet file-based video projects. It covers authoring self-contained animated SVGs with SMIL/CSS keyframes, converting them with svg-animation-convert (duration, fps, loop, transparency, viewBox), renaming the final APNG-content file to a .png extension for preview compatibility, checking APNG frame count/size/duration/alpha, saving under the video script's assets/apngs, and wiring PngAnim with LoopMode/Speed into layouts.
---

# APNG 动画生成（最终后缀必须为 `.png`）

## 用途

把矢量动画做成透明、可循环的 APNG，作为 NewBeeVG 视频中的轻量动画素材，保存到视频脚本目录的 `assets/apngs/`，用 `PngAnim(path)` 嵌入布局。

**最终交付文件必须使用 `.png` 后缀。** 很多图片查看器/预览器不能预览 `.apng` 后缀；APNG 内容放在 `.png` 文件中不影响 NewBeeVG 解码。`.apng` 只作为 `svg-animation-convert` 的中间输出，转换完成后立即重命名为 `.png`。

适用：扁平插画、图标、进度环、加载动画、路径生长、简单形变等**画面简单、时间短、需要透明背景**的动画。

不适用：

| 需求 | 选择 |
| --- | --- |
| 复杂数据图形、粒子、逐帧精确控制 | `SkiaAnim`（`newbeevg-skia-animation` 技能） |
| 数学公式、科学示意图、复杂排版 | `TypstAnim` |
| 已有可靠 Lottie 素材 | `LottieAnim`（不推荐从零生成） |
| 长视频、实拍、无透明需求 | MP4 / WebM |
| 只追求兼容性的动图 | GIF（画质与 Alpha 较差） |

## 前置检查

```bash
node --version                 # 需要 >= 18
ffmpeg -version                # 必须在 PATH 中
npx --yes svg-animation-convert --help
```

首次运行会下载 Puppeteer/Chromium，耗时较长；失败时在项目内安装后重试：

```bash
npm i -D svg-animation-convert
npx svg-animation-convert --input ./assets/svgs/demo.svg --output ./assets/apngs/demo.apng
mv ./assets/apngs/demo.apng ./assets/apngs/demo.png
```

## 工作流

### 1. 先定参数

写 SVG 前确定：

- **画布尺寸**：由目标布局区域决定，例如 320×320、512×512、960×540；转换器从 SVG 的 `width` / `height` / `viewBox` 自动取分辨率，**没有 `--width` / `--height` 参数**；
- **时长**：一个完整循环的秒数，例如 2s、3s、6s；
- **帧率**：默认 24；简单动画 12–15 足够，平滑运动用 30；
- **循环**：APNG 文件内循环次数用 `--loop` 控制；NewBeeVG 节点是否循环由 `LoopMode` 单独控制；
- **透明**：需要叠加在背景上时，不画整张不透明底色。

### 2. 编写动画 SVG

保存到视频脚本目录的 `assets/svgs/`，目录不存在时创建。

硬性要求：

- 根节点同时写 `width`、`height`、`viewBox`，三者比例一致；
- **自包含**：不引用外部图片、字体、脚本；文字尽量转成路径，或只使用系统字体；
- 使用 SMIL（`<animate>`、`<animateTransform>`、`<animateMotion>`、`<set>`）或 CSS `@keyframes`；
- 所有动画的 `dur` 明确，循环动画写 `repeatCount="indefinite"`；
- 动画总周期要可确定：多个元素周期不同时，取它们的最小公倍数作为整体时长；
- 透明背景：不要放铺满画布的 `<rect fill="#fff">`；需要底色时由 NewBeeVG 的背景或下层素材提供；
- 运动元素不要超出 `viewBox`，避免被裁切。

SMIL 旋转围绕中心要显式写中心点：

```svg
<animateTransform attributeName="transform" type="rotate"
                  from="0 256 256" to="360 256 256"
                  dur="6s" repeatCount="indefinite"/>
```

CSS 旋转需要修正变换原点：

```css
#gear {
  transform-box: fill-box;
  transform-origin: center;
  animation: spin 6s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
```

可以直接复制 `assets/svg-animation-template.svg` 作为起点，替换颜色、尺寸与动画参数。

### 3. 转成 APNG

Windows PowerShell：

```powershell
npx --yes svg-animation-convert --input .\assets\svgs\pulse.svg --output .\assets\apngs\pulse.apng --fps 24 --loop 0 --duration 3s
Move-Item .\assets\apngs\pulse.apng .\assets\apngs\pulse.png -Force
```

bash / zsh 把反斜杠换成 `/`：

```bash
npx --yes svg-animation-convert --input ./assets/svgs/pulse.svg --output ./assets/apngs/pulse.apng --fps 24 --loop 0 --duration 3s
mv ./assets/apngs/pulse.apng ./assets/apngs/pulse.png
```

转换器只接受 `.apng` / `.gif` / `.mov` / `.webm` 作为 `--output`，所以先输出 `.apng`；**转换完成后必须重命名为 `.png`**。最终 `assets/apngs/` 下不应残留 `.apng` 文件：

```powershell
Get-ChildItem .\assets\apngs\*.apng
# 应无输出
```

参数：

| 参数 | 说明 | 默认 |
| --- | --- | --- |
| `--input` | 动画 SVG 路径 | 必填 |
| `--output` | 转换器中间输出，扩展名必须是 `.apng` / `.gif` / `.mov` / `.webm`；APNG 随后重命名为 `.png` | 必填 |
| `--duration` | 捕获时长：`4000`、`4000ms`、`4s`、`4.5` | 从 SMIL/GSAP 推断 |
| `--fps` | 帧率 | `24` |
| `--loop` | `0` 无限循环，`-1` 播放一次，`N` 循环 N 次 | `0` |
| `--frames` | 临时帧目录 | `frames` |
| `--keep-frames` | 保留中间 PNG 帧，便于排查 | 关闭 |

关键点：

- **转换器输出用 `.apng`，最终交付用 `.png`**；不要直接交付 `.apng` 文件。
- 重命名后仍可用 `inspect_apng.py` 校验，因为脚本按 PNG 块识别 APNG，与扩展名无关。
- 无限循环（`repeatCount="indefinite"`）以及 JS/GSAP 动画，**显式传 `--duration`**，值等于一个完整循环；不要依赖推断。
- `--duration` 只决定捕获多长，不改变 SVG 内部速度；时长不是周期的整数倍时，首尾会接不上。
- 需要输出后只播放一次，用 `--loop -1`；需要文件内循环，用 `--loop 0`（默认）。

### 4. 校验 APNG

先做结构校验：

```bash
python scripts/inspect_apng.py .\assets\apngs\pulse.png --require-png
```

在技能根目录执行；在其他目录运行时改用脚本的绝对路径。输出包含帧数、画布尺寸、Alpha、总时长、循环次数。`--require-png` 会检查最终后缀必须是 `.png`，否则返回退出码 `3`。需要机器可读结果时加 `--json`。

校验标准：

- 帧数 ≥ 2，否则它只是静态 PNG；
- 尺寸与 SVG 设计尺寸一致；
- 总时长与目标循环时长一致；
- 循环次数符合预期：`0` 为无限；
- 需要透明时确认有 Alpha 通道。

再做视觉校验：

- 第一帧不是空白或半成品；
- 最后一帧能自然接回第一帧，没有跳变；
- 透明区域没有黑底/白底；
- 边缘没有被 `viewBox` 裁切；
- 用 `--keep-frames` 抽查中间帧，确认动画确实在推进。

需要快速看元数据时也可用：

```bash
ffprobe -v error -select_streams v:0 -show_entries stream=width,height,nb_frames,duration -of default=noprint_wrappers=1 .\assets\apngs\pulse.png
```

### 5. 接入 NewBeeVG

把 APNG 放到视频脚本目录的 `assets/apngs/`，用 `PngAnim` 引入：

```csharp
HStack([
    PngAnim("./assets/apngs/pulse.png")
        .Size(320, 320)
        .LoopMode(NBAnimateLoopMode.Loop)
        .Speed(1.0)
        .Align(0, 0),

    TextBlock("实时同步").Font(72, SKColors.White),
]).AsClip(90, name: "pulse");
```

- 路径相对于运行目录，通常是视频脚本所在目录；
- `PngAnim` 默认 `LoopMode = Stop`，播放到最后一帧后停住；**APNG 文件自身的循环设置不会自动改变节点行为**，需要循环必须显式 `.LoopMode(NBAnimateLoopMode.Loop)`；
- 需要往复播放用 `.LoopMode(NBAnimateLoopMode.AutoReverse)`；调速用 `.Speed(1.5)`；
- 节点尺寸默认取 APNG 第一帧，`.Size(...)` 可覆盖；
- clip 比 APNG 长且设置了 `Loop` 时会重复播放；clip 更短时只显示前段；
- 文件不存在或解码失败时不会抛异常，错误记录在 `DecodeException`，节点不绘制内容，需要检查路径与 APNG 结构。

### 6. 登记素材

在视频脚本目录的 `assets/README.md` 中补充：

- 文件名与路径；
- 用途（图标、加载、进度、装饰…）；
- 画布尺寸、帧率、循环时长；
- 来源（生成的 SVG / 使用的提示词）；
- 是否需要透明、是否可循环。

## 目录约定

```text
<视频脚本目录>/
└── assets/
    ├── svgs/      # 动画 SVG 源文件
    ├── apngs/      # APNG 内容，文件名后缀必须为 .png
    ├── images/    # 普通静态图片
    └── README.md
```

目录不存在时创建；不要写到系统临时目录后忘记复制。

## 常见问题

| 现象 | 原因与处理 |
| --- | --- |
| 转换报输出格式错误 | `--output` 必须先用 `.apng` 中间文件；转换完成后再重命名为 `.png`，不要直接让转换器输出 `.png` |
| 最终文件是 `.apng` | 立即重命名为 `.png`；用 `inspect_apng.py --require-png` 检查，交付目录不要残留 `.apng` |
| 转换卡住或时长不对 | 无限循环 / JS 动画没有可推断终点；传 `--duration 3s`（等于一个完整循环） |
| `ffmpeg not found` | 安装 FFmpeg 并加入 `PATH`，重开终端 |
| 首次 `npx` 失败或 Chromium 下载失败 | 检查 Node ≥ 18 与网络；改为项目内 `npm i -D svg-animation-convert` 后运行 |
| 首帧空白、动画不完整 | 外部字体/图片未加载，或 JS 未就绪；改为自包含 SVG、文字转路径，并显式传 `--duration` |
| 背景是黑色/白色 | SVG 里有铺满画布的不透明 `<rect>`；删除它或改用 `fill="none"` |
| 循环处跳帧 | `--duration` 不是动画周期的整数倍；让所有 `dur` 共享同一周期，或裁剪到完整循环 |
| CSS 旋转中心偏移 | 加 `transform-box: fill-box; transform-origin: center;` |
| NewBeeVG 只显示一帧 | 先确认 APNG 帧数 ≥ 2；再确认节点设置了 `.LoopMode(NBAnimateLoopMode.Loop)` |
| APNG 文件过大 | 降低分辨率/帧率/时长；只使用支持 APNG 的优化工具，普通 PNG 优化器可能破坏动画块 |
| 画面模糊 | SVG 的 `viewBox` 与目标尺寸不一致，或输出分辨率过低；调整 SVG 画布尺寸后重新转换 |

## 资源

- `references/svg-animation-patterns.md`：SMIL/CSS 动画模式、透明度、时间轴与自包含检查。
- `references/apng-validation-and-optimization.md`：APNG 结构、循环语义、校验命令、体积优化与故障排查。
- `scripts/inspect_apng.py`：无第三方依赖的 APNG 检查脚本，输出后缀、帧数、尺寸、Alpha、时长、循环次数，支持 `--json` 与 `--require-png`（强制最终后缀为 `.png`）。
- `assets/svg-animation-template.svg`：可直接修改的透明循环动画模板（旋转圆环 + 脉冲圆点）。

# NewBeeVG 长期记忆

## 视频制作流程

- 输入：Markdown 稿件 `xxxx.md`，记录视频内容与要求。
- 分镜：同目录生成 `xxxx_storyboard.md`，按 `shotNN` 记录口播、画面、素材/动画、时长、音频。
- 语音：逐分镜生成 `assets/audios/shotNN.mp3`，并在 `assets/audios/audio_timeline.md` 记录入点/出点/时长。
- 脚本：同目录生成 `xxxx.cs`（dotnet file-based app），按分镜与音频时间表创建 clip 和音频。

## 视频脚本约定

- 生成 NewBeeVG 视频脚本时，主题与字体必须作为全局变量写在文件头部，只声明一次：
  ```csharp
  var theme = NBVideoThemes.Nature;
  var fontName = "阿里巴巴普惠体 2.0";
  font(fontName);
  ```
- 后续 clip 只引用 `theme.Primary` / `theme.Background` / `theme.TextPrimary` / `theme.TextSecondary` / `theme.Divider` / `theme.Positive` / `theme.Negative`，不要在片段内重复 `font(...)` 或硬编码主题色。
- `SkiaAnim` 需要主题时，通过 `ContentTheme.From(theme)` 等主题参数传入全局主题。
- 视频素材目录：APNG → `assets/apngs/`（最终后缀 `.png`）；Lottie → `assets/lottie/`；Typst → `assets/typsts/`；自创 SkiaAnim → `modules/`。
- `assets/materials/` 是事先整理的非 AIGC 素材库，优先递归查找复用，保留原目录/文件名，不写入 AIGC 生成结果。

## Seedream 图片生成

- 用户提供 `--- style: ... ---` + `!(场景简介)[./scene-01.jpg]` + 提示词的 Markdown 场景文档时，使用 `generate_image.py --doc <文档>`。
- 已存在且非空的文件跳过，只生成缺失项；文档模式不传 `--ratio`，不用 `宽x高`，由模型自动判断比例；`.jpg` 输出 JPEG，`.png` 输出 PNG。


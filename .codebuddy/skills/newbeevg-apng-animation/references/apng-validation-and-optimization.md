# APNG 校验、优化与故障排查

## 1. APNG 结构速览

APNG 是 PNG 的超集，关键块：

| 块 | 作用 |
| --- | --- |
| `IHDR` | 画布宽高、位深、颜色类型 |
| `acTL` | 动画控制：总帧数、循环次数 |
| `fcTL` | 单帧控制：帧尺寸、偏移、延迟、dispose/blend |
| `IDAT` | 第一帧（默认图像）的像素数据 |
| `fdAT` | 后续帧的像素数据 |
| `IEND` | 文件结束 |

判断是否真的动图：

- 有 `acTL` 且 `num_frames >= 2`；
- 有足够的 `fcTL` 延迟信息；
- 不是只有一张 `IDAT` 的静态 PNG。

普通 PNG 优化器（例如只处理 `IDAT` 的工具）可能删除或破坏 `acTL`/`fcTL`/`fdAT`，导致动画变成静态图。优化后必须重新检查帧数。

## 2. 用脚本校验

```bash
python scripts/inspect_apng.py .\assets\images\demo.apng
```

输出示例：

```text
File:     .\assets\images\demo.apng
APNG:     yes
Canvas:   512 x 512
Frames:   72 (fcTL: 72, fdAT: 71)
Duration: 3.000 s
Loop:     infinite
Alpha:    yes (RGBA, 8 bit)
Size:     184.2 KB
```

机器可读：

```bash
python scripts/inspect_apng.py .\assets\images\demo.apng --json
```

退出码：

| 退出码 | 含义 |
| --- | --- |
| `0` | 有效 APNG，至少 2 帧 |
| `2` | 是 PNG，但不是动画（缺少 `acTL` 或只有 1 帧） |
| `1` | 文件不存在、签名错误或结构损坏 |

## 3. 用 ffprobe 辅助检查

```bash
ffprobe -v error -select_streams v:0 \
  -show_entries stream=width,height,nb_frames,duration \
  -of default=noprint_wrappers=1 .\assets\images\demo.apng
```

ffprobe 对 APNG 的 `nb_frames` 支持因版本而异；以 `inspect_apng.py` 的 `acTL`/`fcTL` 结果为准。

## 4. 视觉验收

- 首帧：不是空白、半透明或未加载状态；
- 中间帧：动画确实在推进，不是静止画面；
- 末帧：能接回首帧；需要停顿的动画在末尾有明确停留；
- 透明：叠加到深色/浅色背景上都正常，没有黑边或白底；
- 边缘：旋转、缩放、位移没有被裁切；
- 速度：与目标镜头时长匹配，不突兀。

保留帧序列：

```bash
npx --yes svg-animation-convert --input .\assets\svgs\demo.svg --output .\assets\images\demo.apng --duration 3s --fps 24 --keep-frames --frames .\tmp\demo-frames
```

## 5. 体积控制

从以下顺序优化：

1. **降分辨率**：512×512 通常够用；全屏背景才需要 1280/1920；
2. **降帧率**：简单图标 12–15fps，普通运动 24fps，快速运动 30fps；
3. **缩时长**：只保留一个完整循环，用 `LoopMode` 重复播放；
4. **减颜色/细节**：扁平色块、少渐变、少大面积半透明；
5. **复用素材**：同一动画在多个镜头复用，不要重复生成。

经验预算（不是硬限制）：

| 用途 | 建议尺寸 | 帧率 | 时长 | 目标体积 |
| --- | --- | --- | --- | --- |
| 图标 / 角标 | 128–320 | 12–24 | 1–3s | < 500 KB |
| 中等插画 / 数据卡 | 320–640 | 24 | 2–5s | < 2 MB |
| 全屏背景 | 1280–1920 | 24–30 | 3–8s | < 8 MB |

超过预算时优先降尺寸和时长，而不是继续降帧率。

## 6. NewBeeVG 集成检查

```csharp
PngAnim("./assets/images/demo.apng")
    .Size(320, 320)
    .LoopMode(NBAnimateLoopMode.Loop)
    .Speed(1.0);
```

- `PngAnim` 默认 `LoopMode = Stop`，APNG 文件内的循环次数不会自动驱动节点；
- 要循环必须显式 `.LoopMode(NBAnimateLoopMode.Loop)`；
- 要往复用 `.LoopMode(NBAnimateLoopMode.AutoReverse)`；
- `Speed` 是播放倍速，不是帧率；
- 路径相对于运行目录；素材放在视频脚本目录的 `assets/images/`；
- 解码失败记录在 `DecodeException`，节点不绘制内容；先检查路径，再用 `inspect_apng.py` 检查文件。

## 7. 故障排查

| 现象 | 排查 |
| --- | --- |
| 转换器报输出扩展名错误 | 改成 `.apng`；不要用 `.png` |
| 转换卡住 | 无限循环或 JS 动画没有终点；传 `--duration` |
| 时长比预期长/短 | `--duration` 与 SVG 周期不一致；按周期整数倍设置 |
| 首帧空白 | 字体/图片未加载或 JS 未执行；改为自包含 SVG，文字转路径，显式传时长 |
| 画面黑底/白底 | 删除铺满画布的 `rect`，或设置 `fill="none"` |
| 循环处跳变 | 首尾值不一致，或 `--duration` 不是周期整数倍 |
| 旋转位置不对 | SMIL 补 `cx cy`；CSS 补 `transform-box: fill-box; transform-origin: center;` |
| APNG 只有 1 帧 | SVG 实际没有动画，或 `dur`/`repeatCount` 写错；检查浏览器预览 |
| NewBeeVG 只显示第一帧 | 先确认帧数 ≥ 2，再确认 `.LoopMode(Loop)` |
| 文件过大 | 降尺寸/帧率/时长；用 APNG 感知的工具优化，优化后重新校验 |
| 优化后不动了 | 普通 PNG 优化器破坏了 `acTL`/`fcTL`/`fdAT`；回退原文件 |

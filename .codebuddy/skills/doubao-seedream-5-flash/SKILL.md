---
name: doubao-seedream-5-flash
description: 本技能用于通过火山方舟 REST API 调用 Doubao Seedream 5.0 Flash 生成或编辑图片，适用于 NewBeeVG 视频素材制作。涵盖 Markdown 场景文档批量生成（检查文件、跳过已存在、仅生成缺失、模型自动判断比例）、Python 调用、文生图、单图/多图参考、base64 data URI、1K/1.5K/2K 尺寸、url/b64_json 返回、水印与透明背景、下载保存、提示词结构与错误处理。
---

# Doubao Seedream 5.0 Flash 图像生成

## 用途

通过火山方舟图片生成接口调用：

```text
模型：doubao-seedream-5-0-flash-260915
接口：POST https://ark.cn-beijing.volces.com/api/v3/images/generations
鉴权：Authorization: Bearer $ARK_API_KEY
```

支持：

- **文生图**：只传 `prompt`；
- **单图生图**：`image` 传一张参考图；
- **多图参考**：`image` 传 2–10 张参考图，生成一张融合/编辑结果；
- **Markdown 场景文档**：按文档条目检查文件，已存在则跳过，仅生成缺失图片；
- 输出 `1K` / `1.5K` / `2K` 或指定 `宽x高`；
- 画面比例：`1:1`、`4:3`、`3:4`、`16:9`、`9:16`、`21:9`（文档模式不设置比例）；
- `url` 或 `b64_json` 两种返回；
- PNG/JPEG、透明背景、水印开关。

本技能用 Python 调用，不直接使用 curl。

## 快速开始

安装依赖：

```bash
python -m pip install requests
```

API Key 放在技能目录的 `.env`：

```text
doubao-seedream-5-flash/
├── .env
├── .env.example
└── scripts/generate_image.py
```

`.env` 示例：

```dotenv
# 在此填入火山方舟 API Key
ARK_API_KEY=your_api_key_here
```

脚本会自动读取技能目录下的 `.env`；进程环境变量优先级更高，也可以用 `--env-file` 指定其他文件。不要把真实 Key 写进脚本、提示词或输出文件，也不要提交 `.env`。

以下命令在技能目录执行；在视频项目目录运行时，把 `scripts/generate_image.py` 换成该脚本的绝对路径。

### 文档模式（批量场景图，推荐）

当用户提供以下格式的 Markdown 文档时，直接使用 `--doc`，不要逐条手写命令：

```markdown
---
style: 视频口播风格插图
---

!(场景简介)[./scene-01.jpg]

时间、地点、主体、动作、构图、环境细节、光线色彩、媒介风格、质量与限制，写成一段具体的中文提示词。

!(场景简介)[./scene-02.jpg]

第二张图的时间、地点、主体、构图、细节、光线与风格描述。
```

```bash
python scripts/generate_image.py --doc assets/images/scenes.md
```

先预检、不调用 API：

```bash
python scripts/generate_image.py --doc assets/images/scenes.md --dry-run
```

处理规则：

- **路径相对文档所在目录**解析，例如 `./scene-01.jpg` 与 `scenes.md` 同级；
- 逐个检查目标文件：存在且非空则**跳过**，不存在或为空才生成；
- frontmatter 的 `style` 作为全片统一风格，自动拼接到每条提示词前；
- 每条图片引用后面的段落就是该图提示词；提示词为空时退回使用 `场景简介`；
- 输出格式由文件后缀决定：`.jpg` / `.jpeg` → JPEG，`.png` → PNG；
- **不要传 `--ratio`**，也不要在提示词里写 `16:9`、`9:16` 等画幅要求，由 AIGC 模型自动判断构图比例；
- 仍可用 `--size 2K` 控制清晰度，但不要用 `宽x高` 指定比例；`size` 不等于长宽比；
- `--output-format` 在文档模式下忽略，按图片后缀决定；
- 单条失败会继续处理后续条目，最后输出“已存在 / 生成 / 失败”的汇总；
- `--doc` 不能与 `--prompt`、`--output`、`--image`、`--ratio`、`--background` 同时使用。

### 文生图

```bash
python scripts/generate_image.py \
  --prompt "星际穿越，黑洞，复古列车冲出黑洞，电影大片，强对比色，广角透视，体积光" \
  --output assets/images/black-hole.png \
  --size 2K \
  --ratio 16:9
```

PowerShell 用反引号续行，或写成一行：

```powershell
python scripts/generate_image.py --prompt "星际穿越，黑洞，复古列车冲出黑洞，电影大片，强对比色，广角透视，体积光" --output assets/images/black-hole.png --size 2K --ratio 16:9
```

### 单图生图

```bash
python scripts/generate_image.py \
  --prompt "生成狗狗趴在草地上的近景画面，保留原图毛色和项圈" \
  --image assets/images/dog.png \
  --output assets/images/dog-grass.png \
  --size 2K \
  --ratio 4:3
```

### 多张参考图生成单张

```bash
python scripts/generate_image.py \
  --prompt "将图1的服装换为图2的服装，保留图1的人物姿势与背景" \
  --image assets/images/person.png \
  --image assets/images/cloth.png \
  --output assets/images/person-new-outfit.png \
  --size 2K \
  --ratio 3:4
```

`--image` 可重复传入；脚本会把本地图片转成 `data:image/<格式>;base64,...`，URL 或已有 data URI 原样传递。

## 脚本参数

`scripts/generate_image.py`：

| 参数 | 说明 | 默认 |
| --- | --- | --- |
| `--doc` | Markdown 场景文档；解析条目、跳过已存在图片、仅生成缺失项 | 空 |
| `--dry-run` | 只预检 `--doc` 文档，不调用 API | 关闭 |
| `--prompt` | 提示词；单图模式必填，`--doc` 模式不可用 | — |
| `--image` | 参考图路径/URL/data URI，可重复，最多 10 张 | 空（文生图） |
| `--output` | 输出文件路径；单图模式必填，`--doc` 模式不可用 | — |
| `--size` | `1K` / `1.5K` / `2K` 或 `宽x高` | `2K` |
| `--ratio` | `1:1` / `4:3` / `3:4` / `16:9` / `9:16` / `21:9`；换算成具体像素 `宽x高`；`--doc` 模式不可用 | 不传（由模型判断） |
| `--output-format` | `png` / `jpeg` | `png` |
| `--response-format` | `url` / `b64_json` | `url` |
| `--background` | `opaque` / `transparent` | 不传（API 默认 opaque） |
| `--watermark` | 加「AI 生成」水印 | 关闭 |
| `--model` | 模型 ID | `doubao-seedream-5-0-flash-260915` |
| `--timeout` | 请求超时秒数 | `180` |
| `--env-file` | 含 `ARK_API_KEY` 的 `.env` 路径 | 技能目录下的 `.env` |

脚本会：

1. `--doc` 模式解析 frontmatter 与每个 `!(场景简介)[路径]` 条目，检查文件是否存在；
2. 跳过非空已存在文件，只对缺失文件生成；
3. 校验参考图数量、格式与 30MB 上限；
4. 本地图片转 Base64 data URI；
5. 调用接口；
6. 对 `url` 立即下载，对 `b64_json` 解码；
7. 保存前创建输出目录；
8. 打印保存路径、尺寸、格式与 `usage`。

## 请求字段

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `model` | string | 固定 `doubao-seedream-5-0-flash-260915` |
| `prompt` | string | 生成/编辑提示词 |
| `image` | string \| string[] | 参考图；单张传 string，多张传数组 |
| `size` | string | `1K`、`1.5K`、`2K`，或 `2048x1152` 这类像素值；脚本可用 `--ratio` 从档位换算 |
| `response_format` | string | `url` 或 `b64_json` |
| `output_format` | string | `png` 或 `jpeg`；默认 `jpeg`，脚本默认 `png` |
| `background` | string | `opaque` 或 `transparent`；透明仅支持单张参考图的图生图 |
| `watermark` | boolean | 是否加 AI 生成水印 |
| `stream` | boolean | flash 不支持流式，固定 `false` |

`image` 使用 base64 时格式必须是：

```text
data:image/png;base64,<...>
data:image/jpeg;base64,<...>
data:image/webp;base64,<...>
```

格式名用小写；脚本会自动处理本地图片的 MIME。

## 尺寸与参考图限制

### 尺寸与画面比例

支持以下比例：`1:1`、`4:3`、`3:4`、`16:9`、`9:16`、`21:9`。

传 `--ratio` 时，脚本按 `--size` 档位换算成具体的 `宽x高`，接口只收到像素尺寸，不会多传比例字段：

| 比例 | 1K | 1.5K | 2K |
| --- | --- | --- | --- |
| `1:1` | `1024x1024` | `1536x1536` | `2048x2048` |
| `4:3` | `1152x864` | `1792x1344` | `2368x1776` |
| `3:4` | `864x1152` | `1344x1792` | `1776x2368` |
| `16:9` | `1424x800` | `2048x1152` | `2816x1584` |
| `9:16` | `800x1424` | `1152x2048` | `1584x2816` |
| `21:9` | `1568x672` | `2352x1008` | `3136x1344` |

- `--ratio` 只能配合 `1K` / `1.5K` / `2K` 档位使用；
- 已经用 `--size 2816x1584` 指定像素时，不要再传 `--ratio`，否则脚本会报错；
- 不传 `--ratio` 时，模型会根据场景自行决定构图比例；`--doc` 文档模式始终不传 `--ratio`，也不在提示词里写画幅，由 AIGC 模型自动判断；手工单图可按需显式传 `--ratio`；
- 5.0 flash 的像素范围约为 `1280×720` 到 `2048×2048×1.1025`，宽高比范围 `[1/16, 16]`；
- 太小的尺寸（如 `512x512`）会被拒绝。

### 参考图

- 5.0 flash 最多 **10 张**；
- 格式：jpeg、png、webp、bmp、tiff、gif、heic、heif；
- 单张 ≤ **30MB**；
- 宽高比 `[1/16, 16]`，宽高均 > 14 像素；
- 总像素范围约 `196` 到 `6000×6000`；
- 多图时在提示词中用「图1」「图2」明确引用顺序；
- 脚本按 `--image` 出现顺序映射图1、图2。

## 返回与保存

`response_format=url`：

```json
{
  "data": [
    { "url": "https://...", "size": "2048x2048", "output_format": "png" }
  ],
  "usage": { "generated_images": 1, "output_tokens": 16384, "total_tokens": 16384 }
}
```

- URL 生成后 **24 小时有效**，必须立即下载；
- 脚本默认立即下载并保存。

`response_format=b64_json`：

- 图片以 Base64 放在 `data[].b64_json`，脚本直接解码；
- 适合不希望依赖临时 URL 的场景，但响应体更大。

如果响应顶层有 `error`，或 `data[]` 中某张图带 `error`，脚本会打印错误并跳过失败项。

## 提示词写法

建议按「主体 + 构图 + 风格 + 光影/色彩 + 镜头 + 细节 + 限制」组织：

```text
主体：一辆锈迹斑斑的复古列车冲出黑洞
构图：低机位广角，列车占画面右侧三分之二，黑洞在左上方
风格：电影概念设计，超现实主义，暗黑科幻
光影：强引力透镜、体积光、边缘耀光、深蓝与橙红对比
镜头：24mm 广角，动态模糊，浅景深
细节：金属碎片、尘埃、火花、破碎车窗
限制：不要文字，不要水印，16:9 横版
```

要点：

- 中文建议不超过 300 字，英文不超过 600 词；
- 多图参考时明确「保留什么、改变什么」，例如「保留图1的人物姿势，服装换成图2」；
- 画面文字用引号写出准确内容，并说明位置与字体风格；生成后检查文字是否正确；
- 画幅：手工单图可写清 `16:9 横版`、`9:16 竖版`、`1:1 方图` 并配合 `--ratio`；文档模式不要写画幅，由模型自动判断；
- 透明背景仅在单张参考图的图生图中使用，并设置 `--background transparent --output-format png`；
- 不要把多个互相冲突的风格堆在一起，优先保证主体和构图。

## 保存到视频素材目录

NewBeeVG 项目的图片素材放在视频脚本目录的 `assets/images/`：

```bash
python scripts/generate_image.py \
  --prompt "..." \
  --output assets/images/scene-01.png \
  --size 2K
```

批量场景图优先用文档模式，文档和图片可放在同一目录：

```bash
python scripts/generate_image.py --doc assets/images/scenes.md
```

- 目录不存在时脚本会创建；
- 命名体现用途：`scene-01.png`、`character-hero.png`、`bg-night.png`；
- 生成后在 `assets/README.md` 记录文件名、用途、尺寸、提示词摘要；
- 下载 URL 返回的图片后不要只保留链接，必须落地成本地文件；
- 需要透明背景的素材用 PNG，不要用 JPEG。

## 常见错误

| 现象 | 原因与处理 |
| --- | --- |
| `401 Unauthorized` | `.env` 中 `ARK_API_KEY` 未替换、无效或过期；检查技能目录 `.env` 或同名环境变量 |
| `429 Too Many Requests` | 触发限流；降低并发，稍后重试 |
| `400` 尺寸错误 | 档位与 `宽x高` 混用，或总像素/宽高比越界；同时传 `--ratio` 和 `--size 宽x高` 也会报错 |
| `400` 参考图错误 | 超过 10 张、格式不支持、单张超过 30MB |
| 透明背景报错 | 必须是单张参考图的图生图，且 `--output-format png` |
| 图片 URL 打不开 | 链接 24 小时过期；重新生成并立即下载 |
| 生成内容被拒绝 | 提示词触发审核；调整描述，避免违规或敏感内容 |
| 文字乱码 | 重新生成，明确文字内容/位置/字体，或改用后期排版 |

## 安全与合规

- 不生成违法、暴力、仇恨、色情或侵犯他人隐私的内容；
- 不上传未授权的他人肖像、证件、私密图片；
- 生成人物/品牌素材时确认授权与商用范围；
- 不把 API Key、参考图或生成结果泄露到公共日志；
- `.env` 只在本机使用；分享或打包技能前删除 `.env`，只保留 `.env.example`；
- 生成结果需要人工检查后再用于正式视频。

## 资源

- `.env.example`：API Key 配置示例；复制为 `.env` 后填真实 Key。
- `scripts/generate_image.py`：Python 调用脚本，自动读取技能目录 `.env`，支持文生图、单图/多图参考、URL/Base64 下载、透明背景与错误处理。
- `references/api-and-prompting.md`：完整请求/响应字段、尺寸映射、Python `requests` 直连示例、提示词模板与限制。

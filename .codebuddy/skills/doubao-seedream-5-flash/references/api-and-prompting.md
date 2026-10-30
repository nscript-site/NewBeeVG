# Seedream 5.0 Flash API 与提示词参考

## 1. 接口

```text
POST https://ark.cn-beijing.volces.com/api/v3/images/generations
Authorization: Bearer $ARK_API_KEY
Content-Type: application/json
```

模型：

```text
doubao-seedream-5-0-flash-260915
```

能力：

- 文生图；
- 单图生图；
- 多图参考生图（最多 10 张）；
- 精准编辑 / 图层拆分（本技能主要覆盖普通生成与编辑）。

5.0 flash 不支持组图生成、联网搜索、流式输出；`stream` 固定 `false`。

## 2. 请求字段

| 字段 | 类型 | 必填 | 说明 |
| --- | --- | --- | --- |
| `model` | string | 是 | `doubao-seedream-5-0-flash-260915` |
| `prompt` | string | 是 | 生成/编辑提示词 |
| `image` | string \| string[] | 否 | 参考图 URL、Base64 data URI；多张传数组 |
| `size` | string | 否 | `1K` / `1.5K` / `2K`，或 `宽x高` |
| `response_format` | string | 否 | `url`（默认）或 `b64_json` |
| `output_format` | string | 否 | `png` / `jpeg`，默认 `jpeg` |
| `background` | string | 否 | `opaque` / `transparent`；透明仅图生图 |
| `watermark` | boolean | 否 | 默认 `true`；脚本默认关闭 |
| `stream` | boolean | 否 | 固定 `false` |
| `layer_decomposition` | boolean | 否 | 图层拆分；普通生成不要传 |

Base64 格式：

```text
data:image/png;base64,<base64>
data:image/jpeg;base64,<base64>
data:image/webp;base64,<base64>
```

格式名必须小写；本地文件由 Python 脚本读取并编码。

## 3. `size` 与画面比例

5.0 flash 支持：

- 档位：`1K`、`1.5K`、`2K`；
- 像素：`宽x高`，例如 `2048x1152`；
- 两种方式不能混用；
- 总像素约 `1280×720` 到 `2048×2048×1.1025`；
- 宽高比 `[1/16, 16]`。

脚本支持固定比例：`1:1`、`4:3`、`3:4`、`16:9`、`9:16`、`21:9`。传 `--ratio` 时，按 `--size` 档位换算成下面的像素尺寸：

| 比例 | 1K | 1.5K | 2K |
| --- | --- | --- | --- |
| `1:1` | `1024x1024` | `1536x1536` | `2048x2048` |
| `4:3` | `1152x864` | `1792x1344` | `2368x1776` |
| `3:4` | `864x1152` | `1344x1792` | `1776x2368` |
| `16:9` | `1424x800` | `2048x1152` | `2816x1584` |
| `9:16` | `800x1424` | `1152x2048` | `1584x2816` |
| `21:9` | `1568x672` | `2352x1008` | `3136x1344` |

注意：

- 接口本身只有 `size` 字段，没有独立的 `aspect_ratio` 字段；
- `--ratio` 是脚本参数，最终请求仍发送 `宽x高`；
- 已经传 `--size 2816x1584` 时不要再传 `--ratio`；
- 不传 `--ratio` 时，模型按场景自行决定构图比例；
- `--doc` 文档模式始终不传 `--ratio`，也不要求提示词写画幅，由模型自动判断。

## 3.5 Markdown 场景文档模式

文档格式：

```markdown
---
style: 视频口播风格插图
---

![场景简介](./scene-01.jpg)

第一张图的中文提示词。

![场景简介](./scene-02.jpg)

第二张图的中文提示词。
```

命令：

```bash
python scripts/generate_image.py --doc assets/images/scenes.md
python scripts/generate_image.py --doc assets/images/scenes.md --dry-run
```

处理规则：

- 图片路径相对文档所在目录解析；
- 文件存在且非空时跳过，不存在或为空时生成；
- frontmatter 的 `style` 自动拼接到每条提示词前；
- 图片引用后的段落作为提示词，为空时使用 `场景简介`；
- `.jpg` / `.jpeg` 输出 JPEG，`.png` 输出 PNG；
- 文档模式禁止 `--ratio`，由模型自动判断长宽比；
- `--size` 只使用 `1K` / `1.5K` / `2K` 档位，不要用 `宽x高`，默认 `2K`；
- 单条失败不中断批次，最后汇总结果。

## 4. 参考图限制

普通生成/编辑：

- 5.0 flash 最多 **10 张**；
- 格式：jpeg、png、webp、bmp、tiff、gif、heic、heif；
- 单张 ≤ 30MB；
- 宽高比 `[1/16, 16]`；
- 宽高均 > 14 像素；
- 总像素约 `196` 到 `6000×6000`。

透明背景：

- 仅支持图生图；
- 仅 1 张参考图；
- 输入图需要带透明通道；
- `output_format` 必须是 `png`；
- 透明输入不能是 jpeg 等无 Alpha 格式。

## 5. 响应

非流式：

```json
{
  "model": "doubao-seedream-5-0-flash-260915",
  "created": 1784696685,
  "data": [
    {
      "url": "https://...",
      "size": "2048x2048",
      "output_format": "png"
    }
  ],
  "usage": {
    "generated_images": 1,
    "output_tokens": 16384,
    "total_tokens": 16384
  }
}
```

关键字段：

| 字段 | 说明 |
| --- | --- |
| `data[].url` | `url` 模式返回，24 小时有效，需立即下载 |
| `data[].b64_json` | `b64_json` 模式返回 |
| `data[].size` | 实际宽高 |
| `data[].output_format` | `png` / `jpeg` |
| `data[].error` | 单张失败时的 `{code, message}` |
| `error` | 整个请求失败时的顶层错误 |
| `usage.generated_images` | 成功生成张数 |

## 6. Python 直连示例

不依赖技能脚本时，可以直接用 `requests`：

```python
import base64
import os
from pathlib import Path

import requests

api_key = os.environ["ARK_API_KEY"]
endpoint = "https://ark.cn-beijing.volces.com/api/v3/images/generations"

payload = {
    "model": "doubao-seedream-5-0-flash-260915",
    "prompt": "星际穿越，黑洞，复古列车冲出黑洞，电影大片，16:9 横版",
    "size": "2K",
    "response_format": "url",
    "output_format": "png",
    "watermark": False,
    "stream": False,
}

# 图生图时追加 image：
# payload["image"] = "data:image/png;base64," + base64.b64encode(
#     Path("assets/images/ref.png").read_bytes()
# ).decode("ascii")

response = requests.post(
    endpoint,
    headers={
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    },
    json=payload,
    timeout=180,
)
response.raise_for_status()
result = response.json()

if result.get("error"):
    raise RuntimeError(result["error"])

item = result["data"][0]
if item.get("error"):
    raise RuntimeError(item["error"])

output = Path("assets/images/result.png")
output.parent.mkdir(parents=True, exist_ok=True)

if item.get("b64_json"):
    output.write_bytes(base64.b64decode(item["b64_json"]))
else:
    image = requests.get(item["url"], timeout=180)
    image.raise_for_status()
    output.write_bytes(image.content)

print("saved:", output, item.get("size"), result.get("usage"))
```

多图：

```python
images = []
for path in ("assets/images/person.png", "assets/images/cloth.png"):
    raw = Path(path).read_bytes()
    images.append("data:image/png;base64," + base64.b64encode(raw).decode("ascii"))

payload["image"] = images
payload["prompt"] = "将图1的服装换为图2的服装，保留图1的人物姿势与背景"
```

## 7. 提示词模板

### 电影感场景

```text
主体：...
构图：低机位广角，主体占右侧三分之二，...
风格：电影概念设计，超现实主义，暗黑科幻
光影：体积光、边缘耀光、深蓝与橙红对比
镜头：24mm 广角，动态模糊，浅景深
细节：金属碎片、尘埃、火花、破碎车窗
限制：不要文字，不要水印，16:9 横版
```

### 产品/素材图

```text
主体：白色陶瓷咖啡杯，杯口有细微高光
构图：正面平视，居中，留白充足
背景：纯白无缝背景
光影：柔和棚拍光，右侧主光，左侧补光
风格：极简产品摄影，真实材质
限制：不要文字，不要手，不要logo，1:1 方图
```

### 多图编辑

```text
将图1的服装替换为图2的服装；
保留图1的人物身份、姿势、发型、背景和光线；
服装的版型、颜色、褶皱与图2一致；
不要改变人物脸型和手部；
输出写实摄影风格，16:9 横版。
```

### 信息图/图表底图

```text
主体：深色科技感数据可视化背景
构图：中心留出大面积空白，四周有细线网格与光点
风格：简洁、低对比、可叠加文字
限制：不要生成具体数字、文字或logo，16:9 横版
```

## 8. 常见错误

| 错误 | 处理 |
| --- | --- |
| `401` | 检查 `ARK_API_KEY`；确认环境变量已传入当前进程 |
| `403` | 模型未开通/无权限；在方舟控制台确认模型与 Key 权限 |
| `429` | 限流；降低并发、增加重试间隔 |
| `400` size | 检查档位/像素混用、总像素与宽高比 |
| `400` image | 检查数量 ≤10、格式、30MB、宽高比 |
| 透明背景失败 | 单张图生图、输入有 Alpha、输出 png |
| URL 失效 | 24 小时后过期；生成后立即下载 |
| 审核失败 | 调整提示词，避免敏感/违规内容 |

## 9. 素材落地

- 保存到视频脚本目录的 `assets/images/`；
- 文件命名体现用途与镜头：`scene-01.png`、`character-hero.png`；
- 在 `assets/README.md` 记录用途、尺寸、提示词摘要；
- 需要透明背景时使用 PNG；
- 生成结果先人工检查，再放入视频。

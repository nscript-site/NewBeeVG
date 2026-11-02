---
name: doubao-voice
display_name: 豆包语音合成
display_name_en: Doubao Voice
description: 使用火山引擎豆包语音能力生成音频。当用户需要文字转语音、语音合成、TTS、配音、朗读文本、生成语音播报、音频生成、音效生成、广播剧、有声书、音乐+对话成片音频，或查询/挑选发音人（音色）时使用。支持 5 种模式：音频生成 HTTP（seed-audio-1.0，参考音频/图片生成成片级音频）/ 标准 HTTP（默认）/ 单线流式（WebSocket）/ 双向流式（实时交互、语音指令）/ 异步长文本（≤10 万字符）。内置 444 个官方发音人音色库（离线可查），50+ 常用音色、语速调节，输出 mp3。
description_zh: 豆包语音合成：文字转语音 TTS、音频生成（音效/广播剧/有声书）、声音复刻与音色设计、播客、同声传译，内置 444 个官方发音人可离线查询
description_en: Doubao text-to-speech and audio generation — TTS, sound-effect/multi-role audio creation, voice cloning and design, podcasts, and real-time speech translation; ships an offline catalog of 444 official voices
version: 1.1.0
agent_created: true
---

# doubao-voice - 豆包语音合成 / 音频生成

通过豆包语音控制台（openspeech）5 种模式生成音频，**按场景选模式**。

> **运行时依赖**：HTTP 类接口（标准/音频生成/复刻/查询等）零第三方依赖（纯标准库）；WebSocket 类接口（`speech_stream.py` / `speech_bidirection.py` / `podcast.py`，依赖 `tts_protocols.py` 共享模块）需 `pip install websockets`；`translate.py`（同声传译）另需 `protobuf==6.33.0` + `ffmpeg`；发音人查询 `list_voices.py` 离线可用（读内置 `data/voices.json`，不需要 Key）。无本地路径依赖，可发布市场。

## 环境变量

- `VOLCENGINE_SPEECH_API_KEY` - 豆包语音 API Key（UUID 格式，必填），从豆包语音控制台获取
  - 获取：<https://console.volcengine.com/speech/new/> → 右上「API 调用」→ 创建 API Key
  - ⚠️ 注意：方舟 ARK 的 key（ark- 开头）**不能**用于本接口，必须是豆包语音控制台的 UUID Key
  - 与 doubao-asr（语音识别）共用同一个 Key
  - 配置方式：优先用环境变量；也可复制 `.env.example` 为技能目录下的 `.env` 填入（脚本自动读取，不覆盖已有环境变量）。仅需 legacy 鉴权 / OpenAPI 签名（`voice_manage.py`）时才要填其余变量

## 5 种模式 · 按场景选

| 场景 | 用哪个 | 脚本 | 特点 |
|------|--------|------|------|
| **音频生成**（音效/广播剧/有声书/多角色对话+音乐） | 音频生成 HTTP（seed-audio-1.0） | `speech_create.py` | 一段提示词生成成片级音频；支持参考音频/图片、时间轴控制、最长 120s |
| **默认日常用** | 标准（单向流式 HTTP） | `speech.py` | 一次输入文本，流式返回音频，纯标准库 |
| **边合成边接收** | 单线流式（单向 WebSocket） | `speech_stream.py` | 比 HTTP 更实时；需 websockets |
| **实时交互**（流式输入/语音指令） | 双向流式（WebSocket） | `speech_bidirection.py` | 支持逐字输入、context_texts 语音指令；需 websockets |
| **长文本/批量**（≤10 万字符） | 异步长文本（submit/query） | `speech_longtext.py` | 支持整本小说/长报告；音频存 7 天 |

### 资源 ID（X-Api-Resource-Id）

- `seed-tts-2.0`：豆包语音合成大模型 2.0（默认，推荐）
- `seed-icl-2.0`：豆包声音复刻大模型 2.0（复刻音色，需先克隆）

## 使用方式

```
配音：大家好，欢迎收看本期节目              # 默认走标准 HTTP
生成一段广播剧音频：<场景描述+人物对话>      # 走音频生成（seed-audio-1.0）
流式合成这个：大家好                       # 走单线流式
双向流式，用痛心的语气说：我很难过          # 走双向流式（含语音指令）
长文本合成：<小说/报告文本>                # 走异步长文本
查一下有哪些客服音色                       # 走发音人查询（list_voices.py）
```

## 脚本

### 音频生成（seed-audio-1.0，纯标准库）

一段 text_prompt 直接生成「对话 + 背景音乐 + 音效」的成片级音频，适合有声书、配音、广播剧、游戏。参考音频/图片可指定音色与氛围。

```bash
# 纯文本生成（提示词描述场景、人物、音效）
python3 scripts/speech_create.py -t "深夜便利店，一名男子用低沉的语气说：欢迎光临，需要什么吗？" -o scene.mp3

# 参考音频生成（最多 3 条，提示词里用 @音频N 引用，音色相似）
python3 scripts/speech_create.py -t "@音频1 用同样的声音说：把门关上" --ref-audio voice.wav -o out.mp3

# 参考图片生成（1 张，按图片氛围/角色生成）
python3 scripts/speech_create.py -t "用图片中角色的声音说：你好" --ref-image char.png -o out.mp3

# 指定音色（TTS 2.0 音色或声音复刻音色）
python3 scripts/speech_create.py -t "…" --speaker zh_female_shuangkuaisisi_uranus_bigtts -o out.mp3

# 用声音复刻的音色生成（S_ 开头的复刻音色同样适用！）
python3 scripts/speech_create.py -t "用我克隆的声音说这句台词" --speaker S_xxxxx -o out.mp3

# 开启字幕（输出同名 .json 字幕文件）
python3 scripts/speech_create.py -t "…" --subtitle -o out.mp3
```

| 参数 | 简写 | 说明 | 默认值 |
|------|------|------|--------|
| `--text` | `-t` | text_prompt（必填，≤3000 字符；参考音频用 `@音频N` 引用） | — |
| `--output` | `-o` | 输出音频路径 | `speech_create.mp3` |
| `--format` | — | 输出格式 wav/mp3/pcm/ogg_opus | `mp3` |
| `--sample-rate` | — | 采样率 8000-48000 | `24000` |
| `--ref-audio` | — | 参考音频文件（可多次，最多 3 条，每条 ≤30s/10MB） | — |
| `--ref-audio-url` | — | 参考音频 URL（可多次） | — |
| `--ref-image` | — | 参考图片文件（1 张，≤10MB，不能与音频参考混用） | — |
| `--ref-image-url` | — | 参考图片 URL | — |
| `--speaker` | — | 音色 ID（TTS 2.0 音色，或 S_ 开头的**声音复刻音色**，与音频/图片参考互斥） | — |
| `--subtitle` | — | 输出字幕 JSON（含句子/词粒度时间戳） | off |

**提示词写法要点**（官方推荐）：把四样东西写清楚——谁在说（年龄性别+音色）、什么情绪、什么场景、前后该有什么声响。支持时间轴控制（描述总时长、人声说话时间）。

**音乐+人声同时生成的经验**（实测）：
- 音乐描述写弱（如"搭配轻柔弦乐"）时，模型会优先人声，BGM 可能听不见
- 要让 BGM 明显，必须用强约束词：`背景音乐贯穿全片 / 从开头一直播放到结尾 / 旋律清晰饱满 / 音量与人声相当`
- 纯 BGM（无旁白）单独生成没问题，实测稳定
- 若仍不满意 → 分开生成（纯 BGM + TTS 人声）再 ffmpeg 混音（BGM 音量 ~0.3）

### 音色设计 / 声音复刻（voice_design.py，纯标准库）

用文字描述或图片设计一个专属音色，训练成功后得到 speaker_id，可用 `seed-icl-1.0/2.0` 资源 ID 合成。

```bash
# 文字描述音色（训练成功后返回试听音频）
python3 scripts/voice_design.py -s S_xxxxxxxx -p "温柔知性的中年女声，语速舒缓，适合有声书旁白"

# 带参考图片设计
python3 scripts/voice_design.py -s S_xxxxxxxx -p "清亮少年音" --image char.png

# 英文音色
python3 scripts/voice_design.py -s S_xxxxxxxx -p "a deep, calm male voice" -l 1
```

| 参数 | 简写 | 说明 |
|------|------|------|
| `--speaker-id` | `-s` | 复刻音色 ID（S_ 开头，控制台「音色库→我的音色」获取，每槽位 15 次免费训练） |
| `--prompt` | `-p` | 音色文字描述（≤200 字） |
| `--text` | `-t` | 试听文本（≤300 字，训练成功返回该文本的试听音频） |
| `--image` / `--image-url` | — | 参考图片（可选，与文字提示二选一或并用） |
| `-l` | — | 语种 0=中文 1=英文 |

- 训练状态：`2=Success` / `4=Active` 后可用该音色合成
- 合成时资源 ID：`seed-icl-1.0` / `seed-icl-2.0`（声音复刻模型），speaker 填 S_ ID
- 前提：控制台需开通「声音复刻」服务（开通管理 → 声音复刻模型 2.0），并获取复刻音色 ID
- 注意：错误码 `55000000 resource ID is mismatched` = speaker_id 不是有效的复刻音色 ID

### 音色管理（voice_manage.py，火山 OpenAPI AKSK 签名）

查询账号下的复刻音色列表/状态/剩余训练次数/试听音频。**注意：此接口走火山 OpenAPI（AKSK 签名），不是 openspeech 的 UUID Key**。

```bash
# 查全部复刻音色
python3 scripts/voice_manage.py --app-id <AppID>

# 查指定音色
python3 scripts/voice_manage.py --app-id <AppID> --speaker-ids S_xxx S_yyy

# 按状态过滤
python3 scripts/voice_manage.py --app-id <AppID> --state Success
```

| 参数 | 说明 |
|------|------|
| `--app-id` | AppID（控制台「概览页→API接入」可查，数字） |
| `--speaker-ids` | 指定音色 ID（不传查全部） |
| `--state` | 状态过滤：Unknown/Training/Success/Active/Expired/Reclaimed |

- 环境变量：`VOLCENGINE_AK` + `VOLCENGINE_SK`（console.volcengine.com/iam/keymanage 创建）
- 返回：speaker_id / state / 剩余训练次数 / demo_audio / 对应资源 ID（seed-icl-1.0 等）
- 状态含义：`Success` 可合成；`Active` 已激活不可再训练；`Training` 训练中
- 付费接口（OrderAccessResourcePacks 下单 / RenewAccessResourcePacks 续费）未接入，需要时再开

### 声音复刻全家桶（4 个脚本，纯标准库）

声音复刻 = 4 个环节：**训练（录音克隆）→ 查询（状态）→ 设计（文字/图片造音色）→ 升级（V1→V3）**，全部 X-Api-Key 同 Key 鉴权。

**① 音色训练（voice_clone.py）**——上传录音克隆声音（10-30 秒单人干净录音，≤10MB）：
```bash
python3 scripts/voice_clone.py -s S_xxx -a voice.wav -t "参考文本" --demo-text "试听文本"
```
| 参数 | 说明 |
|------|------|
| `-s --speaker-id` | 复刻音色 ID（S_ 开头，控制台音色库获取） |
| `-a --audio` | 录音文件（wav/mp3/ogg/m4a/aac/pcm；pcm/m4a 需 `--format` 指定） |
| `-t --text` | 参考文本（让用户按此念诵，差异过大会 45001109 失败） |
| `--demo-text` | 试听文本（4-300 字，成功返回试听音频） |
| `--denoise` | 开启降噪（录音噪声大时用） |
| `--no-volume-norm` | 关闭音量归一化（音色更接近样本） |

**② 音色查询（voice_query.py）**——查音色训练状态/剩余次数/试听音频：
```bash
python3 scripts/voice_query.py -s S_xxx
```

**③ 音色设计（voice_design.py）**——文字/图片造音色（无需录音）：
```bash
python3 scripts/voice_design.py -s S_xxx -p "温柔知性的中年女声，语速舒缓"
```

**④ 音色升级（voice_upgrade.py）**——V1 克隆音色升级到 V3（跨 1.0/2.0 通用）：
```bash
python3 scripts/voice_upgrade.py -s S_xxx
```

- 训练状态：`2=Success` / `4=Active` 后可用；合成资源 ID：`seed-icl-1.0` / `seed-icl-2.0`
- 复刻音色可直接用于 `speech_create.py --speaker S_xxx`（音频生成支持复刻音色）
- 命名规范（custom_speaker_id 后付费）：8-256 字符、字母开头、不可含 `S_`/`ICL_`/`MIX_` 等官方前缀

### 双人播客生成（podcast.py，WebSocket）

一键把内容变成双人对话播客音频。3 种玩法：

```bash
# action=0 长文本 → 双人播客
python3 scripts/podcast.py --input-text "长文章内容..." -o podcast.mp3

# action=0 网页链接/PDF → 双人播客
python3 scripts/podcast.py --input-url "https://example.com/article.html" -o podcast.mp3

# action=3 对话脚本直接合成（--line "说话人|台词" 可多次）
python3 scripts/podcast.py --action 3 --line "咪仔|大家好，欢迎收听本期播客" --line "大壹先生|今天我们聊聊AI" -o podcast.mp3

# action=4 联网搜索话题 → 播客
python3 scripts/podcast.py --action 4 --prompt-text "2026年AI视频生成趋势" -o podcast.mp3
```

| 参数 | 说明 |
|------|------|
| `--action` | 0=长文本/URL总结（默认） 3=对话脚本 4=联网搜索 |
| `--input-text` / `--input-url` | action=0 的输入（≤32k，建议 ≤12000） |
| `--line` | action=3 对话轮次，"说话人\|文本" |
| `--prompt-text` | action=4 联网话题 |
| `--only-nlp-text` | action=0：只生成对话脚本（不生成音频），先看脚本再合成 |
| `--return-audio-url` | action=0：返回完整音频 URL（1h 有效）而非流式 |
| `--preset` | 双人音色：黑猫侦探社（默认男女）/ 刘飞和潇磊（双男） |
| `--speakers` | 自定义两个发音人 ID |
| `--head-music` / `--tail-music` | 开头/结尾音效 |
| `--format` / `--sample-rate` / `--speech-rate` | 音频参数 |
| `--auth` | 鉴权：apikey=UUID Key（默认，新版可用）/ legacy=旧版 APP ID |

- **鉴权（实测）**：新版 UUID Key（`VOLCENGINE_SPEECH_API_KEY`）**完全可用**（默认，文档未更新但接口已支持）；legacy（`X-Api-App-Id`+`X-Api-Access-Key`，.env 配 `VOLCENGINE_APP_ID`/`VOLCENGINE_ACCESS_KEY`）也可用，作为备用
- **⚠️ action=3 的 speaker 必须用音色 ID**（不是中文名！）：如 `zh_female_mizaitongxue_v2_saturn_bigtts`。脚本内置中文名映射：咪仔/大壹先生/刘飞/潇磊 → 自动转音色 ID；传中文名报 `55000000 resource mismatched`
- **⚠️ 前置**：需在控制台**开启播客试用**：console.volcengine.com/speech/service/10028；**开通后有时延（实测约 10 分钟），期间报 55000000 属正常，稍等再试**
- 音频流式返回 + 完整音频 URL（PodcastEnd 事件，1h 有效），支持断点续传（retry_info）
- 需要 websockets + tts_protocols.py；事件：150 开始 / 360-362 轮次 / 363 结束 / 152 完成

### 同声传译 2.0（translate.py，WebSocket + Protobuf）

实时语音翻译：**S2S**（语音→语音，默认克隆说话人音色）/ **S2T**（语音→文本）。输入本地音频文件自动转 16k PCM 流式发送。

```bash
# S2S：中文语音 → 英文语音（克隆说话人音色）
python3 scripts/translate.py -f input.mp3 -m s2s -s zh -t en -o output/audio/en_version

# S2S 指定公版音色（目标为中/英时）
python3 scripts/translate.py -f input.mp3 -m s2s -s zh -t en --speaker vv

# S2T：中文语音 → 英文文本
python3 scripts/translate.py -f input.mp3 -m s2t -s zh -t en -o output/audio/en_text
```

| 参数 | 说明 |
|------|------|
| `-f --file` | 输入音频（wav/mp3/m4a 等，自动 ffmpeg 转 16k PCM） |
| `-m --mode` | s2s=语音→语音（默认）/ s2t=语音→文本 |
| `-s --source` / `-t --target` | 源/目标语种（S2S 8 种：中英德法西印尼日葡；S2T 20 外语+粤语沪语方言） |
| `--speaker` | 公版音色：vv / 京腔爷（不传则克隆说话人音色） |

- **鉴权**：`X-Api-Key`（UUID Key）+ Resource `volc.service_type.10053`（实测可用）
- **⚠️ 输出格式必须用 ogg_opus**（默认已设）：`pcm` 裸流输出实测**有明显杂音**，ogg_opus 干净（与官方 demo 一致）；不要开 denoise（官方没开，开了反而可能引入处理痕迹）
- **S2S 克隆音色**：不传 speaker 时自动复刻输入音频的说话人音色——中文口播直接翻成英文版还是你的声音
- 中英反转互译：source 和 target 都传 `zhen` 免切换
- **依赖**：websockets + protobuf==6.33.0（需 pip install）+ `ast_service_pb2` 等（官方 Protobuf 定义，已随技能提供）+ ffmpeg
- 输出：s2s 出 mp3 / s2t 出 txt；实测中译英两个模式均通过

### 标准（默认，纯标准库）

```bash
python3 scripts/speech.py -t "要合成的文本" -o output.mp3
```

| 参数 | 简写 | 说明 | 默认值 |
|------|------|------|--------|
| `--text` | `-t` | 要合成的文本（必填，除非用 --list-voices） | — |
| `--model` | `-m` | 模型 | `doubao-seed-tts-2-0` |
| `--voice` | `-v` | 音色 ID | 爽快思思 2.0 |
| `--output` | `-o` | 输出音频路径 | `speech.mp3` |
| `--speed` | — | 语速 0.5-2.0 | `1.0` |
| `--list-voices` | — | 列出内置音色（可独立使用） | off |

### 单线流式（WebSocket）

```bash
python3 scripts/speech_stream.py -t "文本" -o out.mp3
```

| 参数 | 说明 |
|------|------|
| `--voice` `-v` | 音色 ID |
| `--sample-rate` | 采样率（默认 24000） |

### 双向流式（WebSocket，实时交互）

```bash
python3 scripts/speech_bidirection.py -t "文本" -o out.mp3
python3 scripts/speech_bidirection.py -t "我很难过" --context-texts "你可以用特别痛心的语气说话吗"
```

| 参数 | 说明 |
|------|------|
| `--context-texts` | 语音指令（逗号分隔多条），仅 2.0 音色支持 |
| `--sample-rate` | 采样率（默认 24000） |

### 异步长文本（≤10 万字符）

```bash
python3 scripts/speech_longtext.py -t "长文本..." -o long.mp3
python3 scripts/speech_longtext.py -f article.txt -o long.mp3   # 从文件读
```

| 参数 | 说明 |
|------|------|
| `--file` `-f` | 从文件读取文本（与 -t 二选一） |
| `--max-wait` | 最大等待秒数（默认 300） |

## 发音人（音色）

### 查发音人：list_voices.py（离线、零依赖、不需要 Key）

内置官方音色库快照 `data/voices.json`（444 个 `seed-tts-2.0` 在售音色，含名称/性别/年龄/分类/语言/标签/描述/热度/试听），本地搜索即可拿到 `voice_type`：

```bash
python3 scripts/list_voices.py                              # 热门音色前 20（JSON）
python3 scripts/list_voices.py -q 思思                       # 关键词搜（名称/描述/ID/分类/标签/情感）
python3 scripts/list_voices.py -c 客服场景 -g 女 -a 青年      # 按场景/性别/年龄筛
python3 scripts/list_voices.py -l en --limit 30             # 英语音色
python3 scripts/list_voices.py --label 豆包同款              # 按特殊标签（豆包/抖音/剪映同款）
python3 scripts/list_voices.py --id zh_female_vv            # 按 voice_type 精确/前缀查
python3 scripts/list_voices.py --table                      # 人读表格（默认输出 JSON）
python3 scripts/list_voices.py --stats                      # 看分类/语言/标签分布
```

| 参数 | 说明 |
|------|------|
| `-q --search` | 关键词，可重复；匹配名称/描述/voice_type/分类/标签/情感，多关键词取交集 |
| `-c --category` | 分类模糊匹配：通用场景 / 视频配音 / 有声阅读 / 角色扮演 / 客服场景 / 教学场景 / 外语音色 |
| `-g --gender` / `-a --age` | 男 / 女；青年 / 中年 / 少年少女 / 儿童 / 老年 |
| `-l --lang` | 语言前缀：zh（含 zh-cn）/ en / ja / ko / pt-br … |
| `--id` | 按 voice_type 精确或前缀查询 |
| `--sort` / `--limit` | 排序：heat（默认）/ name / id；条数默认 20，`0` = 全部 |
| `--table` / `--stats` | 人读表格 / 只输出音色库统计 |

返回 JSON 里的 `voice_type` 直接用于 `speech.py`、`speech_create.py`、`podcast.py` 的 `--speaker`。

### 刷新音色库：refresh_voices.py（需 AKSK，可选）

官方新增音色后想更新快照，走火山引擎 OpenAPI `ListSpeakers`（Version 2025-05-20）：

```bash
python3 scripts/refresh_voices.py --dry-run          # 只打印请求，便于排查凭据
python3 scripts/refresh_voices.py                    # 拉取并覆盖 data/voices.json
```

- 鉴权用 `VOLCENGINE_AK` / `VOLCENGINE_SK`（火山引擎「访问控制 → API 访问密钥」），**不是** openspeech 的 UUID Key，也不是 legacy 的 AppID/AccessToken
- 接口不返回控制台热度，脚本按 `voice_type` 继承旧 `heat`，新音色为 0
- 覆盖前建议先 `--out /tmp/voices.json` 对比差异

### 常用音色速查（按热度）

| 音色 ID | 音色 | 场景 |
|---------|------|------|
| `zh_female_shuangkuaisisi_uranus_bigtts` | 爽快思思 2.0（默认） | 通用 |
| `zh_female_vv_uranus_bigtts` | Vivi 2.0（热度第一） | 通用 |
| `zh_female_xiaohe_uranus_bigtts` | 小何 2.0 | 有声阅读（豆包同款） |
| `zh_female_tianmeixiaoyuan_uranus_bigtts` | 甜美小源 2.0 | 通用 |
| `zh_female_kailangjiejie_uranus_bigtts` | 开朗姐姐 2.0 | 通用 |
| `zh_female_mizai_uranus_bigtts` | 黑猫侦探社咪仔 2.0 | 视频配音（播客 preset） |
| `zh_male_liufei_uranus_bigtts` | 刘飞 2.0 | 通用（播客 preset） |
| `zh_male_taocheng_uranus_bigtts` | 小天 2.0 | 通用 |
| `zh_female_kefunvsheng_uranus_bigtts` | 暖阳女声 2.0 | 客服场景 |
| `zh_male_shenyeboke_uranus_bigtts` | 深夜播客 2.0 | 有声阅读 |
| `en_male_tim_uranus_bigtts` | Tim | 美式英语 |

> **规则**：音色库快照内全部是 `seed-tts-2.0`（`uranus` 系）音色，请求头 `X-Api-Resource-Id` 要与音色自带的 `ResourceID` 一致。`mars` 结尾的是 1.0 音色，要换用 1.0 的资源 ID 才能合成（见各脚本 `--resource-id`）；推荐统一用 2.0。
> `speech.py --list-voices` 是脚本内置的 50 个精选子集；要全量 444 个用 `list_voices.py`。
> 官方音色文档：<https://www.volcengine.com/docs/6561/1257544>

## 依赖

- 标准 / 异步长文本：纯标准库
- 单线流式 / 双向流式：`pip install websockets`（一次性）+ `tts_protocols.py`（官方协议库，已随技能提供）
- 发音人查询 `list_voices.py`：纯标准库 + 内置 `data/voices.json`（离线，不需要 Key）

## 输出

默认输出 JSON：`{"success": true, "file": "output.mp3"}`，mp3 可直接用于视频配音、语音播报、有声内容。

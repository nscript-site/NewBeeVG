"""
豆包音频生成（doubao-voice / seed-audio-1.0）脚本
通过豆包语音控制台 openspeech 接口（HTTP 非流式音频生成）生成成片级音频：
一段 text_prompt 即可生成「对话 + 背景音乐 + 音效」合成音频，支持：
  - 纯文本生成（提示词描述场景/人物/音效）
  - 参考音频生成（最多 3 条，提示词里用 @音频N 引用，音色相似）
  - 参考图片生成（1 张，按图片氛围生成）
  - 自然语言时间轴控制（描述总时长、人声说话时间）
接口：https://openspeech.bytedance.com/api/v3/tts/create
鉴权：X-Api-Key（UUID，与 TTS 同一个 Key）
模型：seed-audio-1.0（最长 120s 输出，17 种语言）
纯标准库实现，无第三方依赖。
"""

import os
import sys
import json
import time
import base64
import argparse
import urllib.request
import urllib.error

API_URL = "https://openspeech.bytedance.com/api/v3/tts/create"
DEFAULT_MODEL = "seed-audio-1.0"
SUPPORTED_FORMATS = ["wav", "mp3", "pcm", "ogg_opus"]
SUPPORTED_RATES = [8000, 16000, 24000, 32000, 44100, 48000]


def _load_dotenv(path: str) -> None:
    """加载 .env 文件到环境变量（已存在则不覆盖）。"""
    if not os.path.isfile(path):
        return
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, v = line.split("=", 1)
            k = k.strip()
            v = v.strip().strip('"').strip("'")
            if k and k not in os.environ:
                os.environ[k] = v


def get_api_key() -> str:
    """读取豆包语音 API Key（UUID，openspeech 专用）。"""
    for name in ("VOLCENGINE_SPEECH_API_KEY", "ARK_API_KEY", "VOLCENGINE_API_KEY"):
        if os.environ.get(name):
            return os.environ[name]
    raise ValueError(
        "未找到 API Key。请在 .env 中配置 VOLCENGINE_SPEECH_API_KEY "
        "（豆包语音控制台获取，UUID 格式）"
    )


def _to_base64(path: str) -> str:
    """读取文件转 base64。"""
    with open(path, "rb") as f:
        return base64.b64encode(f.read()).decode("ascii")


def build_references(ref_audios: list, ref_audio_urls: list,
                     ref_images: list, ref_image_urls: list, speakers: list) -> list:
    """构造 references 数组。每个元素只含一个键（音频/图片/音色互斥）。"""
    refs = []
    for p in ref_audios:
        refs.append({"audio_data": _to_base64(p)})
    for u in ref_audio_urls:
        refs.append({"audio_url": u})
    for p in ref_images:
        refs.append({"image_data": _to_base64(p)})
    for u in ref_image_urls:
        refs.append({"image_url": u})
    for s in speakers:
        refs.append({"speaker": s})
    return refs


def create_audio(text: str, output: str, audio_format: str = "mp3",
                 sample_rate: int = 24000, ref_audios: list = None,
                 ref_audio_urls: list = None, ref_images: list = None,
                 ref_image_urls: list = None, speakers: list = None,
                 subtitle: bool = False, model: str = DEFAULT_MODEL,
                 timeout: int = 300) -> dict:
    """调用音频生成接口，音频保存到 output，返回结果信息。"""
    api_key = get_api_key()
    references = build_references(ref_audios or [], ref_audio_urls or [],
                                  ref_images or [], ref_image_urls or [],
                                  speakers or [])
    payload = {
        "model": model,
        "text_prompt": text,
        "audio_config": {
            "format": audio_format,
            "sample_rate": sample_rate,
        },
    }
    if references:
        payload["references"] = references
    if subtitle:
        payload["audio_config"]["enable_subtitle"] = True

    req = urllib.request.Request(
        API_URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Content-Type": "application/json",
            "X-Api-Key": api_key,
            "X-Api-Request-Id": str(time.time()).replace(".", ""),
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"请求失败 (HTTP {e.code}): {body[:500]}") from e

    code = data.get("code", 0)  # 成功时无 code 字段（仅出错时返回）
    if code != 0:
        raise RuntimeError(f"生成失败 (code={code}): {data.get('message', '')}")

    audio_b64 = data.get("audio") or ""
    url = data.get("url") or ""
    duration = data.get("duration") or data.get("original_duration") or 0
    sub = data.get("subtitle")

    if audio_b64:
        audio_bytes = base64.b64decode(audio_b64)
        with open(output, "wb") as f:
            f.write(audio_bytes)
    elif url:
        with urllib.request.urlopen(url, timeout=timeout) as resp:
            audio_bytes = resp.read()
        with open(output, "wb") as f:
            f.write(audio_bytes)
    else:
        raise RuntimeError(f"响应中没有音频数据: {json.dumps(data, ensure_ascii=False)[:300]}")

    result = {
        "success": True,
        "file": output,
        "duration": duration,
        "original_duration": data.get("original_duration", 0),
        "url": url,
    }
    if sub and subtitle:
        sub_path = os.path.splitext(output)[0] + ".json"
        with open(sub_path, "w", encoding="utf-8") as f:
            json.dump(sub, f, ensure_ascii=False, indent=2)
        result["subtitle"] = sub_path
    return result


def main():
    parser = argparse.ArgumentParser(
        description="豆包音频生成（seed-audio-1.0）：一段提示词生成对话+音乐+音效成片级音频。"
    )
    parser.add_argument("-t", "--text", required=True, help="text_prompt（必选，≤3000 字符；参考音频用 @音频N 引用）")
    parser.add_argument("-o", "--output", default="speech_create.mp3", help="输出音频路径（默认 speech_create.mp3）")
    parser.add_argument("--format", default="mp3", choices=SUPPORTED_FORMATS, help="输出格式（默认 mp3）")
    parser.add_argument("--sample-rate", type=int, default=24000, choices=SUPPORTED_RATES, help="采样率（默认 24000）")
    parser.add_argument("--ref-audio", action="append", default=None, help="参考音频文件（可多次，最多 3 条，提示词里用 @音频N 引用）")
    parser.add_argument("--ref-audio-url", action="append", default=None, help="参考音频 URL（可多次）")
    parser.add_argument("--ref-image", action="append", default=None, help="参考图片文件（1 张，不能与音频参考混用）")
    parser.add_argument("--ref-image-url", action="append", default=None, help="参考图片 URL")
    parser.add_argument("--speaker", action="append", default=None, help="音色 ID（TTS 2.0 音色或声音复刻音色，与音频参考互斥）")
    parser.add_argument("--subtitle", action="store_true", help="输出字幕（JSON）")
    parser.add_argument("--model", default=DEFAULT_MODEL, help=f"模型（默认 {DEFAULT_MODEL}）")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不调用 API")
    args = parser.parse_args()

    _load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    _load_dotenv(".env")

    if args.dry_run:
        refs_plan = []
        for p in (args.ref_audio or []):
            refs_plan.append({"audio_data": f"<本地文件 base64: {p}>"})
        for u in (args.ref_audio_url or []):
            refs_plan.append({"audio_url": u})
        for p in (args.ref_image or []):
            refs_plan.append({"image_data": f"<本地文件 base64: {p}>"})
        for u in (args.ref_image_url or []):
            refs_plan.append({"image_url": u})
        for s in (args.speaker or []):
            refs_plan.append({"speaker": s})
        print(json.dumps({
            "api_url": API_URL,
            "model": args.model,
            "text_prompt": args.text[:200],
            "references": refs_plan,
            "audio_config": {"format": args.format, "sample_rate": args.sample_rate,
                             "enable_subtitle": args.subtitle},
            "output": args.output,
        }, ensure_ascii=False, indent=2))
        return

    try:
        result = create_audio(
            args.text, args.output, args.format, args.sample_rate,
            args.ref_audio, args.ref_audio_url, args.ref_image,
            args.ref_image_url, args.speaker, args.subtitle, args.model,
        )
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

"""
豆包音色训练（声音复刻）脚本
上传语音样本（10-30 秒单人干净录音），训练一个自定义音色，用于语音合成。
接口：POST https://openspeech.bytedance.com/api/v3/tts/voice_clone
鉴权：X-Api-Key（UUID，与 TTS 同一个 Key）
纯标准库实现。
"""

import os
import sys
import json
import base64
import argparse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from voice_common import load_dotenv, get_api_key, post_json, parse_voice_response  # noqa: E402

API_URL = "https://openspeech.bytedance.com/api/v3/tts/voice_clone"

# 扩展名 → format
EXT_FORMAT = {".wav": "wav", ".mp3": "mp3", ".ogg": "ogg", ".m4a": "m4a",
              ".aac": "aac", ".pcm": "pcm"}


def main():
    parser = argparse.ArgumentParser(
        description="豆包音色训练（声音复刻）：上传录音训练自定义音色。"
    )
    parser.add_argument("-s", "--speaker-id", required=True, help="复刻音色 ID（S_ 开头，控制台「音色库→我的音色」获取）")
    parser.add_argument("-a", "--audio", required=True, help="录音文件路径（10-30 秒单人干净录音，≤10MB，wav/mp3/ogg/m4a/aac/pcm）")
    parser.add_argument("--format", default="", help="音频格式（pcm/m4a 必传；mp3/ogg/aac 可不指定，默认按扩展名推断）")
    parser.add_argument("-t", "--text", default="", help="参考文本（可选：让用户按此文本念诵，差异过大将复刻失败）")
    parser.add_argument("-l", "--language", type=int, default=0, choices=sorted([0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,19,20,21]),
                        help="语种（默认 0=中文，1=英文）")
    parser.add_argument("--demo-text", default="", help="试听文本（4-300 字，训练成功会返回这段的试听音频）")
    parser.add_argument("--denoise", action="store_true", help="开启降噪（录音噪声大时建议开）")
    parser.add_argument("--no-volume-norm", action="store_true", help="关闭音量归一化（音色与样本更接近）")
    parser.add_argument("-o", "--output", default="", help="试听音频保存路径")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不调用 API")
    args = parser.parse_args()

    load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    load_dotenv(".env")

    if not os.path.isfile(args.audio):
        print(json.dumps({"success": False, "error": f"录音文件不存在: {args.audio}"}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)

    fmt = args.format or EXT_FORMAT.get(os.path.splitext(args.audio)[1].lower(), "")
    if not fmt:
        print(json.dumps({"success": False, "error": "无法推断音频格式，请用 --format 指定"}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)

    payload: dict = {
        "speaker_id": args.speaker_id,
        "audio": {"data": "", "format": fmt},
        "language": args.language,
    }
    extra: dict = {}
    if args.text:
        payload["text"] = args.text
    if args.demo_text:
        extra["demo_text"] = args.demo_text
    if args.denoise:
        extra["enable_audio_denoise"] = True
    if args.no_volume_norm:
        extra["disable_volume_normalization"] = True
    if extra:
        payload["extra_params"] = extra

    if args.dry_run:
        with open(args.audio, "rb") as f:
            audio_b64 = base64.b64encode(f.read()).decode()
        print(json.dumps({
            "api_url": API_URL,
            "speaker_id": args.speaker_id,
            "audio": {"data": f"<base64 {len(audio_b64)//1024}KB>", "format": fmt},
            "text": args.text or "(未填)",
            "language": args.language,
            "extra_params": extra,
        }, ensure_ascii=False, indent=2))
        return

    try:
        with open(args.audio, "rb") as f:
            audio_b64 = base64.b64encode(f.read()).decode("ascii")
        payload["audio"]["data"] = audio_b64
        data = post_json(API_URL, payload)
        result = parse_voice_response(data, args.output)
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

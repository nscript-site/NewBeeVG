"""
豆包音色查询脚本
查询已训练音色的状态（训练中/成功/失败、剩余训练次数、试听音频）。
接口：POST https://openspeech.bytedance.com/api/v3/tts/get_voice
鉴权：X-Api-Key（UUID，与 TTS 同一个 Key）
纯标准库实现。
"""

import os
import sys
import json
import argparse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from voice_common import load_dotenv, post_json, parse_voice_response  # noqa: E402

API_URL = "https://openspeech.bytedance.com/api/v3/tts/get_voice"


def main():
    parser = argparse.ArgumentParser(description="豆包音色查询：查询已训练音色的状态。")
    parser.add_argument("-s", "--speaker-id", required=True, help="复刻音色 ID（S_ 开头）")
    parser.add_argument("-l", "--language", type=int, default=0, help="语种（默认 0=中文，1=英文）")
    parser.add_argument("-o", "--output", default="", help="试听音频保存路径（成功时自动下载）")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不调用 API")
    args = parser.parse_args()

    load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    load_dotenv(".env")

    payload = {"speaker_id": args.speaker_id, "language": args.language}

    if args.dry_run:
        print(json.dumps({"api_url": API_URL, "body": payload}, ensure_ascii=False, indent=2))
        return

    try:
        data = post_json(API_URL, payload)
        result = parse_voice_response(data, args.output)
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

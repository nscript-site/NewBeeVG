"""
豆包音色升级脚本
把 V1 训练接口生成的克隆音色升级到 V3，升级后可在声音复刻 1.0/2.0 等多个产品通用。
（若已用 V3 训练接口则无需升级）
接口：POST https://openspeech.bytedance.com/api/v3/tts/upgrade_voice
鉴权：X-Api-Key（UUID，与 TTS 同一个 Key）
纯标准库实现。
"""

import os
import sys
import json
import argparse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from voice_common import load_dotenv, post_json, parse_voice_response  # noqa: E402

API_URL = "https://openspeech.bytedance.com/api/v3/tts/upgrade_voice"


def main():
    parser = argparse.ArgumentParser(description="豆包音色升级：V1 克隆音色升级到 V3（跨模型通用）。")
    parser.add_argument("-s", "--speaker-id", required=True, help="要升级的复刻音色 ID")
    parser.add_argument("--custom-speaker-id", default="", help="自定义音色代号（仅后付费音色：8-256 字符，字母开头，不可含 S_ 等官方前缀）")
    parser.add_argument("-o", "--output", default="", help="试听音频保存路径")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不调用 API")
    args = parser.parse_args()

    load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    load_dotenv(".env")

    payload: dict = {"speaker_id": args.speaker_id}
    if args.custom_speaker_id:
        payload["custom_speaker_id"] = args.custom_speaker_id

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

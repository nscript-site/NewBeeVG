"""
豆包双人播客生成（doubao-voice / 播客 API v3）
通过 WebSocket 流式合成双人对话播客音频：
  - action=0：长文本 / 网页链接总结生成播客
  - action=3：对话脚本直接合成（指定谁说什么）
  - action=4：联网搜索话题后生成播客
接口：wss://openspeech.bytedance.com/api/v3/sami/podcasttts
鉴权：X-Api-App-Id + X-Api-Access-Key（旧版控制台）+ X-Api-App-Key（固定 aGjiRDfUWi）
      + X-Api-Resource-Id（volc.service_type.10050）
依赖：websockets + 官方协议库 tts_protocols.py（同目录）
注意：此接口鉴权是旧版控制台 APP ID/Access Token 体系，不是 UUID Key
"""

import os
import sys
import json
import uuid
import argparse
import asyncio

import websockets

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import tts_protocols as proto  # noqa: E402
from voice_common import load_dotenv  # noqa: E402

WS_URL = "wss://openspeech.bytedance.com/api/v3/sami/podcasttts"
RESOURCE_ID = "volc.service_type.10050"
APP_KEY = "aGjiRDfUWi"  # 固定值

# 默认双人发音人：黑猫侦探社咪仔（男女对谈）
DEFAULT_SPEAKERS = [
    "zh_female_mizaitongxue_v2_saturn_bigtts",
    "zh_male_dayixiansheng_v2_saturn_bigtts",
]
# 刘飞和潇磊（双男声）
SPEAKER_PRESETS = {
    "黑猫侦探社": DEFAULT_SPEAKERS,
    "刘飞和潇磊": ["zh_male_liufei_v2_saturn_bigtts", "zh_male_xiaolei_v2_saturn_bigtts"],
}
# 口语名 → 音色 ID（action=3 的 nlp_texts.speaker 必须用音色 ID）
SPEAKER_ALIASES = {
    "咪仔": "zh_female_mizaitongxue_v2_saturn_bigtts",
    "咪仔同学": "zh_female_mizaitongxue_v2_saturn_bigtts",
    "大壹先生": "zh_male_dayixiansheng_v2_saturn_bigtts",
    "大壹": "zh_male_dayixiansheng_v2_saturn_bigtts",
    "刘飞": "zh_male_liufei_v2_saturn_bigtts",
    "潇磊": "zh_male_xiaolei_v2_saturn_bigtts",
}


def resolve_speaker(name: str) -> str:
    """把口语名映射为音色 ID；已是音色 ID 则原样返回。"""
    name = name.strip()
    if name in SPEAKER_ALIASES:
        return SPEAKER_ALIASES[name]
    return name


def get_legacy_credentials() -> tuple:
    """读取旧版控制台鉴权信息。"""
    app_id = os.environ.get("VOLCENGINE_APP_ID") or os.environ.get("DOUBAO_APP_ID")
    access_key = os.environ.get("VOLCENGINE_ACCESS_KEY") or os.environ.get("DOUBAO_ACCESS_KEY")
    if not app_id or not access_key:
        raise ValueError(
            "未找到旧版控制台鉴权信息。请在 .env 配置 VOLCENGINE_APP_ID + VOLCENGINE_ACCESS_KEY\n"
            "获取：旧版控制台 speech/app 创建应用 → 左侧 API 服务中心选对应模型 → 复制 APP ID 和 Access Token"
        )
    return app_id, access_key


def build_headers(auth: str = "apikey") -> dict:
    """构造鉴权 headers。
    auth=apikey：新版控制台 X-Api-Key（UUID，优先，文档可能未更新）
    auth=legacy：旧版控制台 X-Api-App-Id + X-Api-Access-Key
    """
    base = {
        "X-Api-Resource-Id": RESOURCE_ID,
        "X-Api-Request-Id": str(uuid.uuid4()),
    }
    if auth == "legacy":
        app_id, access_key = get_legacy_credentials()
        base["X-Api-App-Id"] = app_id
        base["X-Api-Access-Key"] = access_key
        base["X-Api-App-Key"] = APP_KEY
    else:
        from voice_common import get_api_key
        base["X-Api-Key"] = get_api_key()
    return base


def build_payload(args) -> dict:
    """构造播客请求体。"""
    payload: dict = {"action": args.action}

    if args.action == 0:
        if args.input_text:
            payload["input_text"] = args.input_text
        if args.input_url:
            payload["input_info"] = {"input_url": args.input_url, "strict_audit": False}
        if args.only_nlp_text:
            payload.setdefault("input_info", {})["only_nlp_text"] = True
        if args.return_audio_url:
            payload.setdefault("input_info", {})["return_audio_url"] = True
    elif args.action == 3:
        nlp_texts = []
        for line in args.line or []:
            if "|" in line:
                speaker, text = line.split("|", 1)
                nlp_texts.append({"speaker": resolve_speaker(speaker), "text": text.strip()})
            else:
                nlp_texts.append({"speaker": "", "text": line.strip()})
        if not nlp_texts:
            raise ValueError("action=3 必须用 --line \"说话人|文本\" 至少传一轮对话")
        payload["nlp_texts"] = nlp_texts
    elif args.action == 4:
        if not args.prompt_text:
            raise ValueError("action=4 必须用 --prompt-text 传联网搜索话题")
        payload["prompt_text"] = args.prompt_text
    else:
        raise ValueError("action 只支持 0（长文本/URL）/ 3（对话脚本）/ 4（联网搜索）")

    speakers = args.speakers
    if speakers:
        # 用户显式指定才传 speakers（官方默认不传，服务端自动分配）
        payload["speaker_info"] = {"random_order": False, "speakers": speakers[:2]}
    else:
        payload["speaker_info"] = {"random_order": False}
    payload["audio_config"] = {
        "format": args.format,
        "sample_rate": args.sample_rate,
        "speech_rate": args.speech_rate,
    }
    payload["use_head_music"] = args.head_music
    payload["use_tail_music"] = args.tail_music
    payload["aigc_watermark"] = False
    return payload


async def generate(payload: dict, output: str, timeout: int = 600, auth: str = "apikey") -> dict:
    """连接播客接口并合成，返回结果。"""
    headers = build_headers(auth)

    session_id = str(uuid.uuid4())
    audio_data = bytearray()
    audio_url = ""
    events_seen = []
    round_texts = []

    async with websockets.connect(WS_URL, additional_headers=headers,
                                  max_size=50 * 1024 * 1024) as ws:
        # 1. 建立连接（事件 1）→ 等服务端确认（事件 50）
        await proto.start_connection(ws)
        # 2. 发送播客请求（StartSession 事件 100，payload 含 action 和参数）
        await proto.start_session(ws, json.dumps(payload).encode("utf-8"), session_id)
        # 3. 会话确认（事件 150）后立即发 finish_session（官方流程）
        await proto.finish_session(ws, session_id)

        # 3. 接收流式响应
        while True:
            try:
                msg = await asyncio.wait_for(proto.receive_message(ws), timeout=timeout)
            except asyncio.TimeoutError:
                raise TimeoutError(f"等待播客生成超时（{timeout}s）")
            except Exception as e:
                # 服务端关闭连接
                break

            ev = msg.event if isinstance(msg.event, int) else msg.event.value
            events_seen.append(ev)

            if msg.type == proto.MsgType.Error:
                raise RuntimeError(f"服务端错误: {msg.payload.decode('utf-8', 'ignore')[:300]}")

            if ev == 150:  # SessionStarted
                try:
                    d = json.loads(msg.payload.decode("utf-8"))
                    if d.get("session_id"):
                        session_id = d["session_id"]
                except Exception:
                    pass
            elif ev == 360:  # PodcastRoundStart
                try:
                    d = json.loads(msg.payload.decode("utf-8"))
                    t = d.get("text", "")
                    if t:
                        round_texts.append(t)
                except Exception:
                    pass
            elif ev == 361:  # PodcastRoundResponse（音频）
                if msg.payload:
                    audio_data.extend(msg.payload)
            elif ev == 362:  # PodcastRoundEnd
                pass
            elif ev == 363:  # PodcastEnd（完整音频 URL）
                try:
                    d = json.loads(msg.payload.decode("utf-8"))
                    audio_url = (d.get("meta_info") or {}).get("audio_url", "") or d.get("audio_url", "")
                except Exception:
                    pass
            elif ev in (152, 2, 52):  # SessionFinished / FinishConnection
                break

    # only_nlp_text：只输出对话脚本，不生成音频
    if (payload.get("input_info") or {}).get("only_nlp_text"):
        if not round_texts:
            raise RuntimeError("未收到对话脚本文本，请检查参数")
        json_path = os.path.splitext(output)[0] + ".json"
        os.makedirs(os.path.dirname(json_path) or ".", exist_ok=True)
        with open(json_path, "w", encoding="utf-8") as f:
            json.dump(round_texts, f, ensure_ascii=False, indent=2)
        return {
            "success": True,
            "file": json_path,
            "texts": round_texts,
            "rounds": len(round_texts),
        }

    # 保存音频：优先流式拼装的；为空则下载 audio_url
    if not audio_data and audio_url:
        import urllib.request
        with urllib.request.urlopen(audio_url, timeout=120) as resp:
            audio_data.extend(resp.read())

    if not audio_data:
        raise RuntimeError("未收到音频数据，请检查鉴权或参数")

    os.makedirs(os.path.dirname(output) or ".", exist_ok=True)
    with open(output, "wb") as f:
        f.write(bytes(audio_data))

    return {
        "success": True,
        "file": output,
        "size": len(audio_data),
        "rounds": len(round_texts),
        "audio_url": audio_url,
        "events": events_seen,
    }


def main():
    parser = argparse.ArgumentParser(
        description="豆包双人播客生成：长文本/URL 总结、对话脚本、联网话题，一键合成双人播客音频。"
    )
    parser.add_argument("--action", type=int, default=0, choices=[0, 3, 4],
                        help="0=长文本/URL总结 3=对话脚本直接合成 4=联网搜索话题（默认 0）")
    parser.add_argument("--input-text", default="", help="action=0：待总结的长文本（≤32k，建议 ≤12000）")
    parser.add_argument("--input-url", default="", help="action=0：网页链接或可下载文件链接（pdf/doc/txt）")
    parser.add_argument("--only-nlp-text", action="store_true", help="action=0：只生成对话脚本文本，不生成音频（先看脚本再合成）")
    parser.add_argument("--return-audio-url", action="store_true", help="action=0：返回完整音频 URL 而非流式（1h 有效）")
    parser.add_argument("--prompt-text", default="", help="action=4：联网搜索的话题")
    parser.add_argument("--line", action="append", default=None, help='action=3：对话轮次，格式 "说话人|文本"（可多次）')
    parser.add_argument("--speakers", nargs=2, default=None, help="自定义两个发音人 ID")
    parser.add_argument("--preset", default="黑猫侦探社", choices=list(SPEAKER_PRESETS.keys()),
                        help="双人音色预设：黑猫侦探社（默认）/ 刘飞和潇磊")
    parser.add_argument("--format", default="mp3", choices=["mp3", "ogg_opus", "pcm", "aac"], help="音频格式（默认 mp3）")
    parser.add_argument("--sample-rate", type=int, default=24000, choices=[16000, 24000, 48000], help="采样率（默认 24000）")
    parser.add_argument("--speech-rate", type=int, default=0, help="语速 -50~100（默认 0）")
    parser.add_argument("--head-music", action="store_true", help="使用开头音效")
    parser.add_argument("--tail-music", action="store_true", help="使用结尾音效")
    parser.add_argument("-o", "--output", default="output/audio/podcast.mp3", help="输出音频路径（默认 output/audio/podcast.mp3）")
    parser.add_argument("--auth", default="apikey", choices=["apikey", "legacy"],
                        help="鉴权方式：apikey=新版 UUID Key（默认）/ legacy=旧版 APP ID+Access Key")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不连接")
    args = parser.parse_args()

    load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    load_dotenv(".env")

    try:
        payload = build_payload(args)
    except ValueError as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)

    if args.dry_run:
        headers = build_headers(args.auth)
        print(json.dumps({
            "ws_url": WS_URL,
            "resource_id": RESOURCE_ID,
            "auth": args.auth,
            "headers": {k: (v[:20] + "..." if len(v) > 20 else v) for k, v in headers.items()},
            "payload": payload,
            "output": args.output,
        }, ensure_ascii=False, indent=2))
        return

    try:
        result = asyncio.run(generate(payload, args.output, auth=args.auth))
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

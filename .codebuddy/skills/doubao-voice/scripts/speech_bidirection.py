"""
豆包语音合成 - 双向流式 WebSocket（实时交互，支持流式输入、语音指令）
接口：wss://openspeech.bytedance.com/api/v3/tts/bidirection
资源：seed-tts-2.0（默认）/ seed-icl-2.0（复刻音色）
场景：实时对话、边输入边合成、逐字流式、语音指令（context_texts）
依赖：websockets（pip install websockets）+ 官方协议库 tts_protocols.py（已随技能提供）

流程（官方 protocols）：
start_connection(1) → start_session(100) → task_request(200) → finish_session(102) → finish_connection(2)
"""

import os
import sys
import json
import uuid
import argparse
import asyncio

try:
    import websockets
except ImportError:
    print(json.dumps({"success": False,
                      "error": "缺少依赖 websockets，请先安装：pip install websockets"},
                     ensure_ascii=False), file=sys.stderr)
    sys.exit(1)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from tts_protocols import (
    MsgType, EventType, Message, MsgTypeFlagBits,
    receive_message, wait_for_event, start_connection, start_session,
    finish_session, task_request, finish_connection,
)

WS_URL = "wss://openspeech.bytedance.com/api/v3/tts/bidirection"
RESOURCE_ID = "seed-tts-2.0"

# 下行事件
EV_CONNECTION_STARTED = 50
EV_SESSION_STARTED = 150
EV_SESSION_FINISHED = 152
EV_TTS_RESPONSE = 352  # 音频数据
EV_TTS_SENTENCE_START = 350
EV_TTS_SENTENCE_END = 351


def _load_dotenv(path: str) -> None:
    if not os.path.isfile(path):
        return
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, v = line.split("=", 1)
            k, v = k.strip(), v.strip().strip('"').strip("'")
            if k and k not in os.environ:
                os.environ[k] = v


def get_api_key() -> str:
    for name in ("VOLCENGINE_SPEECH_API_KEY", "DOUBAO_SPEECH_API_KEY"):
        if os.environ.get(name):
            return os.environ[name]
    raise ValueError("未找到豆包语音 API Key，请在 .env 配置 VOLCENGINE_SPEECH_API_KEY（UUID）")


async def synthesize_bidirectional(text: str, voice: str, output: str,
                                   context_texts: list[str] | None = None,
                                   sample_rate: int = 24000) -> str:
    api_key = get_api_key()
    headers = {
        "X-Api-Key": api_key,
        "X-Api-Resource-Id": RESOURCE_ID,
        "X-Api-Connect-Id": str(uuid.uuid4()),
    }

    base_params = {
        "req_params": {
            "speaker": voice,
            "audio_params": {"format": "mp3", "sample_rate": sample_rate},
        }
    }
    if context_texts:
        base_params["req_params"]["context_texts"] = context_texts

    session_id = str(uuid.uuid4())
    audio_data = bytearray()

    async with websockets.connect(WS_URL, additional_headers=headers,
                                  max_size=10 * 1024 * 1024) as ws:
        # 1. 建立连接
        await start_connection(ws)
        await wait_for_event(ws, MsgType.FullServerResponse, EventType.ConnectionStarted)

        # 2. 创建会话
        await start_session(ws, json.dumps(base_params).encode("utf-8"), session_id)
        await wait_for_event(ws, MsgType.FullServerResponse, EventType.SessionStarted)

        # 3. 发送文本任务（一次全量；逐字流式可拆开发多次 task_request）
        task_req = dict(base_params)
        task_req["req_params"]["text"] = text
        await task_request(ws, json.dumps(task_req).encode("utf-8"), session_id)

        # 4. 结束会话（服务端此时把已合成的音频全部下发）
        await finish_session(ws, session_id)

        # 5. 收音频直到 SessionFinished
        while True:
            msg = await receive_message(ws)
            if msg.type == MsgType.Error:
                raise RuntimeError(f"服务端错误: {msg}")
            if msg.type == MsgType.AudioOnlyServer and msg.payload:
                audio_data.extend(msg.payload)
            elif msg.type == MsgType.FullServerResponse and msg.event == EventType.SessionFinished:
                break

        # 6. 结束连接
        await finish_connection(ws)

    if not audio_data:
        raise RuntimeError("未收到音频数据")
    with open(output, "wb") as f:
        f.write(audio_data)
    return output


def main():
    p = argparse.ArgumentParser(description="豆包语音合成 - 双向流式（WebSocket 实时）")
    p.add_argument("-t", "--text", required=True, help="要合成的文本")
    p.add_argument("-v", "--voice", default="zh_female_shuangkuaisisi_uranus_bigtts",
                   help="音色 ID（默认爽快思思 2.0）")
    p.add_argument("-o", "--output", default="speech_bidirection.mp3", help="输出音频路径")
    p.add_argument("--context-texts", default="", help="语音指令（逗号分隔，如：你可以用痛心的语气说话吗）")
    p.add_argument("--sample-rate", type=int, default=24000, help="采样率（默认 24000）")
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()

    _load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    _load_dotenv(".env")

    context_texts = [s.strip() for s in args.context_texts.split(",") if s.strip()] if args.context_texts else None

    if args.dry_run:
        print(json.dumps({"ws_url": WS_URL, "resource_id": RESOURCE_ID,
                          "voice": args.voice, "context_texts": context_texts},
                         ensure_ascii=False, indent=2))
        return

    try:
        path = asyncio.run(synthesize_bidirectional(args.text, args.voice, args.output,
                                                    context_texts, args.sample_rate))
        print(json.dumps({"success": True, "file": path}, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

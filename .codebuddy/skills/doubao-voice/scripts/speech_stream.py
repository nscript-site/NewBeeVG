"""
豆包语音合成 - 单线流式（单向流式 WebSocket，一次输入文本，流式返回音频）
接口：wss://openspeech.bytedance.com/api/v3/tts/unidirectional/stream
资源：seed-tts-2.0（默认）/ seed-icl-2.0（复刻音色）
场景：需要边合成边接收（如边生成边播放），比 HTTP 版更实时
依赖：websockets（pip install websockets）
协议：豆包自研二进制帧 —— header(4) + [sequence/event 可选] + payload_size(4) + payload
"""

import os
import sys
import json
import uuid
import struct
import argparse
import asyncio

try:
    import websockets
except ImportError:
    print(json.dumps({"success": False,
                      "error": "缺少依赖 websockets，请先安装：pip install websockets"},
                     ensure_ascii=False), file=sys.stderr)
    sys.exit(1)

WS_URL = "wss://openspeech.bytedance.com/api/v3/tts/unidirectional/stream"
RESOURCE_ID = "seed-tts-2.0"

# 消息类型
MSG_FULL_CLIENT = 0b0001        # 客户端完整请求（文本）
MSG_AUDIO_SERVER = 0b1011       # 服务端音频数据
MSG_FULL_SERVER = 0b1001        # 服务端文本响应
MSG_ERROR = 0b1111              # 错误
FLAG_NO_SEQ = 0b0000
FLAG_WITH_EVENT = 0b0100


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


def build_frame(message_type: int, flags: int, payload_bytes: bytes | None,
                serial: int = 0x10, event: int = 0) -> bytes:
    """构造豆包二进制帧：header(4) + [event(4)] + [payload_size(4) + payload]。"""
    header = bytes([0x11, (message_type << 4) | flags, serial, 0x00])
    frame = bytearray(header)
    if flags & FLAG_WITH_EVENT:
        frame.extend(struct.pack(">I", event))
    if payload_bytes is not None:
        frame.extend(struct.pack(">I", len(payload_bytes)))
        frame.extend(payload_bytes)
    return bytes(frame)


def parse_frame(raw: bytes) -> dict:
    """解析服务端帧。
    结构：header(4) + [event(4)] + [session_id_len(4) + session_id] + payload_size(4) + payload
    实测服务端（TTS 单线流式）event 后总是带 session_id。
    """
    if len(raw) < 4:
        return {"type": 0, "event": 0, "payload": b""}
    msg_type = (raw[1] >> 4) & 0x0F
    flags = raw[1] & 0x0F
    pos = 4
    event = 0
    if flags & FLAG_WITH_EVENT:
        if len(raw) < pos + 4:
            return {"type": msg_type, "event": 0, "payload": b""}
        event = struct.unpack(">I", raw[pos:pos + 4])[0]
        pos += 4
        # session_id：4 字节长度 + 内容（TTS 服务端总是携带）
        if len(raw) >= pos + 4:
            sid_len = struct.unpack(">I", raw[pos:pos + 4])[0]
            pos += 4 + sid_len
    payload = b""
    if len(raw) >= pos + 4:
        size = struct.unpack(">I", raw[pos:pos + 4])[0]
        pos += 4
        payload = raw[pos:pos + size]
    return {"type": msg_type, "event": event, "payload": payload}


async def synthesize_stream(text: str, voice: str, output: str,
                            sample_rate: int = 24000) -> str:
    api_key = get_api_key()
    headers = {
        "X-Api-Key": api_key,
        "X-Api-Resource-Id": RESOURCE_ID,
        "X-Api-Connect-Id": str(uuid.uuid4()),
    }

    body = {
        "req_params": {
            "speaker": voice,
            "text": text,
            "audio_params": {"format": "mp3", "sample_rate": sample_rate},
        }
    }

    audio_data = bytearray()
    async with websockets.connect(WS_URL, additional_headers=headers,
                                  max_size=10 * 1024 * 1024) as ws:
        # 发送完整请求（全量文本）
        await ws.send(build_frame(MSG_FULL_CLIENT, FLAG_NO_SEQ,
                                  json.dumps(body).encode("utf-8")))

        while True:
            raw = await ws.recv()
            msg = parse_frame(raw)
            if msg["type"] == MSG_ERROR:
                raise RuntimeError(f"服务端错误: {msg['payload'][:200]}")
            if msg["type"] == MSG_AUDIO_SERVER and msg["payload"]:
                audio_data.extend(msg["payload"])
            elif msg["type"] == MSG_FULL_SERVER:
                # event 350=SessionStarted 351=SessionFinished 352=TTSResponse(音频)
                # 153=TTSSentenceEnd 等；收到 SessionFinished 即结束
                if msg["event"] in (351, 152, 50):
                    break

    if not audio_data:
        raise RuntimeError("未收到音频数据")

    with open(output, "wb") as f:
        f.write(audio_data)
    return output


def main():
    p = argparse.ArgumentParser(description="豆包语音合成 - 单线流式（WebSocket）")
    p.add_argument("-t", "--text", required=True, help="要合成的文本")
    p.add_argument("-v", "--voice", default="zh_female_shuangkuaisisi_uranus_bigtts",
                   help="音色 ID（默认爽快思思 2.0）")
    p.add_argument("-o", "--output", default="speech_stream.mp3", help="输出音频路径")
    p.add_argument("--sample-rate", type=int, default=24000, help="采样率（默认 24000）")
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()

    _load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    _load_dotenv(".env")

    if args.dry_run:
        print(json.dumps({"ws_url": WS_URL, "resource_id": RESOURCE_ID,
                          "voice": args.voice, "output": args.output},
                         ensure_ascii=False, indent=2))
        return

    try:
        path = asyncio.run(synthesize_stream(args.text, args.voice, args.output, args.sample_rate))
        print(json.dumps({"success": True, "file": path}, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

"""
豆包同声传译 2.0（doubao-voice / AST）脚本
实时语音翻译：S2S（语音→语音，可克隆说话人音色）和 S2T（语音→文本）。
输入本地音频文件，自动转 16k PCM 流式发送，输出翻译音频/译文文本。
接口：wss://openspeech.bytedance.com/api/v4/ast/v2/translate
鉴权：X-Api-Key（UUID，与 TTS 同一个 Key）+ X-Api-Resource-Id: volc.service_type.10053
依赖：websockets + protobuf==6.33.0 + ast_service_pb2 等（官方 Protobuf 定义，已随技能提供）+ ffmpeg（音频转码）
"""

import os
import sys
import json
import uuid
import argparse
import asyncio
import subprocess
import tempfile

import websockets

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ast_service_pb2 import (  # noqa: E402
    TranslateRequest, TranslateResponse,
)
from events_pb2 import Type  # noqa: E402
from voice_common import load_dotenv, get_api_key  # noqa: E402

WS_URL = "wss://openspeech.bytedance.com/api/v4/ast/v2/translate"
RESOURCE_ID = "volc.service_type.10053"
SAMPLE_RATE = 16000  # 输入采样率（固定 16k）
CHUNK_MS = 80  # 每包 80ms
CHUNK_BYTES = SAMPLE_RATE * 2 * 1 * CHUNK_MS // 1000  # 2560 字节/包

# S2S 可指定公版音色（仅目标语种为中/英时）
PUBLIC_SPEAKERS = {
    "vv": "zh_female_vv_uranus_bigtts",
    "京腔爷": "zh_male_jingqiangkanye_emo_mars_bigtts",
}


def to_pcm16k(input_path: str) -> str:
    """用 ffmpeg 把任意音频转成 16k 16bit 单声道 raw PCM，返回临时文件路径。"""
    tmp = tempfile.mktemp(suffix=".pcm")
    cmd = [
        "ffmpeg", "-y", "-i", input_path,
        "-ar", str(SAMPLE_RATE), "-ac", "1", "-f", "s16le", tmp,
    ]
    r = subprocess.run(cmd, capture_output=True)
    if r.returncode != 0:
        raise RuntimeError(f"ffmpeg 转码失败: {r.stderr.decode()[:300]}")
    return tmp


async def translate(audio_path: str, mode: str, source_language: str,
                    target_language: str, output: str, speaker_id: str = "",
                    timeout: int = 300, target_format: str = "ogg_opus") -> dict:
    """执行翻译，保存结果。"""
    api_key = get_api_key()
    headers = {
        "X-Api-Key": api_key,
        "X-Api-Resource-Id": RESOURCE_ID,
        "X-Api-Connect-Id": str(uuid.uuid4()),
    }
    session_id = str(uuid.uuid4())

    pcm_path = to_pcm16k(audio_path)
    try:
        with open(pcm_path, "rb") as f:
            audio = f.read()
    finally:
        os.remove(pcm_path)

    req_params = {
        "mode": mode,
        "source_language": source_language,
        "target_language": target_language,
    }
    if speaker_id:
        req_params["speaker_id"] = speaker_id

    translated_text = ""
    source_text = ""
    audio_data = bytearray()

    async with websockets.connect(WS_URL, additional_headers=headers,
                                  max_size=50 * 1024 * 1024) as ws:
        # 1. StartSession
        start_req = TranslateRequest(
            event=Type.StartSession,
            request_meta={"SessionID": session_id},
            user={"uid": "doubao-voice"},
            source_audio={"format": "wav", "codec": "raw", "rate": 16000, "bits": 16, "channel": 1},
            target_audio={"format": target_format, "rate": 24000},
            request=req_params,
        )
        await ws.send(start_req.SerializeToString())

        # 2. 等 SessionStarted
        first = TranslateResponse()
        first.ParseFromString(await asyncio.wait_for(ws.recv(), timeout=15))
        if first.event != Type.SessionStarted:
            code = getattr(first.response_meta, "Code", "UNKNOWN")
            msg = getattr(first.response_meta, "Message", "会话启动失败")
            raise RuntimeError(f"会话启动失败: {msg} (code={code})")

        # 3. 流式发送音频（80ms/包）
        chunks = [audio[i:i + CHUNK_BYTES] for i in range(0, len(audio), CHUNK_BYTES)]
        if not chunks:
            chunks = [audio]

        async def sender():
            for chunk in chunks:
                task_req = TranslateRequest(
                    event=Type.TaskRequest,
                    request_meta={"SessionID": session_id},
                    source_audio={"binary_data": chunk},
                )
                await ws.send(task_req.SerializeToString())
                await asyncio.sleep(0.02)  # 控制发送节奏
            finish = TranslateRequest(event=Type.FinishSession, request_meta={"SessionID": session_id})
            await ws.send(finish.SerializeToString())

        # 4. 接收结果
        async def receiver():
            nonlocal translated_text, source_text
            while True:
                try:
                    msg = await asyncio.wait_for(ws.recv(), timeout=timeout)
                except asyncio.TimeoutError:
                    raise TimeoutError(f"等待翻译结果超时（{timeout}s）")
                except Exception:
                    break
                resp = TranslateResponse()
                resp.ParseFromString(msg)
                ev = resp.event
                if ev == Type.SessionFinished:
                    break
                if ev == Type.SessionFailed:
                    raise RuntimeError(f"会话失败: {getattr(resp.response_meta, 'Message', '')}")
                if ev in (Type.SourceSubtitleResponse, Type.SourceSubtitleEnd) and resp.text:
                    source_text = resp.text
                elif ev in (Type.TranslationSubtitleResponse, Type.TranslationSubtitleEnd) and resp.text:
                    translated_text = resp.text
                elif ev == Type.TTSResponse and resp.data:
                    audio_data.extend(resp.data)
                elif ev == Type.UsageResponse:
                    pass

        await asyncio.gather(sender(), receiver())

    result = {
        "success": True,
        "mode": mode,
        "source_language": source_language,
        "target_language": target_language,
        "source_text": source_text,
        "translated_text": translated_text,
        "audio_bytes": len(audio_data),
    }

    if mode == "s2s" and audio_data:
        # ogg_opus 流 → 转 mp3；pcm 裸流直接转 mp3
        pcm_tmp = tempfile.mktemp(suffix="." + ("opus" if target_format == "ogg_opus" else "pcm"))
        with open(pcm_tmp, "wb") as f:
            f.write(bytes(audio_data))
        out_mp3 = output if output.endswith(".mp3") else output + ".mp3"
        os.makedirs(os.path.dirname(out_mp3) or ".", exist_ok=True)
        if target_format == "ogg_opus":
            r = subprocess.run(
                ["ffmpeg", "-y", "-i", pcm_tmp, "-codec:a", "libmp3lame", "-qscale:a", "2", out_mp3],
                capture_output=True,
            )
        else:
            r = subprocess.run(
                ["ffmpeg", "-y", "-f", "s16le", "-ar", "24000", "-ac", "1", "-i", pcm_tmp,
                 "-codec:a", "libmp3lame", "-qscale:a", "2", out_mp3],
                capture_output=True,
            )
        os.remove(pcm_tmp)
        if r.returncode != 0:
            raise RuntimeError(f"音频转 mp3 失败: {r.stderr.decode()[:200]}")
        result["file"] = out_mp3

    if mode == "s2t" and translated_text:
        txt_path = output if output.endswith(".txt") else output + ".txt"
        os.makedirs(os.path.dirname(txt_path) or ".", exist_ok=True)
        with open(txt_path, "w", encoding="utf-8") as f:
            f.write(translated_text)
        result["file"] = txt_path

    return result


def main():
    parser = argparse.ArgumentParser(
        description="豆包同声传译 2.0：语音翻译（S2S 语音→语音可克隆音色 / S2T 语音→文本）。"
    )
    parser.add_argument("-f", "--file", required=True, help="输入音频文件（wav/mp3/m4a 等，自动转 16k PCM）")
    parser.add_argument("-m", "--mode", default="s2s", choices=["s2s", "s2t"],
                        help="s2s=语音→语音（默认）/ s2t=语音→文本")
    parser.add_argument("-s", "--source", default="zh", help="源语种（默认 zh 中文）")
    parser.add_argument("-t", "--target", default="en", help="目标语种（默认 en 英文）")
    parser.add_argument("--speaker", default="", help="公版音色（S2S 且目标为中/英时可用；不传则克隆说话人音色）")
    parser.add_argument("--target-format", default="ogg_opus", choices=["ogg_opus", "pcm"],
                        help="输出音频编码（默认 ogg_opus，与官方一致，质量更好；pcm 为裸流）")
    parser.add_argument("-o", "--output", default="output/audio/translate_out", help="输出路径（s2s 出 mp3 / s2t 出 txt）")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不连接")
    args = parser.parse_args()

    load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    load_dotenv(".env")

    if args.dry_run:
        print(json.dumps({
            "ws_url": WS_URL,
            "resource_id": RESOURCE_ID,
            "headers": {"X-Api-Key": "<.env 配置>", "X-Api-Resource-Id": RESOURCE_ID},
            "audio_file": args.file,
            "mode": args.mode,
            "source_language": args.source,
            "target_language": args.target,
            "speaker_id": args.speaker or "(克隆说话人音色)",
            "chunk_bytes": CHUNK_BYTES,
            "output": args.output,
        }, ensure_ascii=False, indent=2))
        return

    try:
        result = asyncio.run(translate(
            args.file, args.mode, args.source, args.target,
            args.output, args.speaker, target_format=args.target_format,
        ))
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

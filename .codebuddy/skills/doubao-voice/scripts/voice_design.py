"""
豆包音色设计 / 声音复刻（doubao-voice）脚本
通过豆包语音控制台 openspeech 接口，用文本描述或图片设计一个自定义音色，
训练成功后返回 speaker_id 和试听音频，之后可用 seed-icl-1.0/2.0 资源 ID 合成。
接口：https://openspeech.bytedance.com/api/v3/tts/voice_design
鉴权：X-Api-Key（UUID，与 TTS 同一个 Key）
注意：speaker_id 需要在豆包语音控制台开通「声音复刻」服务后获得（有设计次数配额）
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

API_URL = "https://openspeech.bytedance.com/api/v3/tts/voice_design"

# 训练状态
STATUS = {0: "NotFound", 1: "Training", 2: "Success", 3: "Failed", 4: "Active"}


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


def design_voice(speaker_id: str, text: str, text_prompt: str,
                 image_path: str = "", image_url: str = "",
                 language: int = 0, timeout: int = 120) -> dict:
    """提交音色设计任务，返回响应（含 speaker_id/status/demo_audio）。"""
    api_key = get_api_key()
    prompt: dict = {}
    if text_prompt:
        prompt["text_prompt"] = text_prompt
    if image_path:
        with open(image_path, "rb") as f:
            prompt["image_prompt"] = {"image_bytes": base64.b64encode(f.read()).decode("ascii")}
    elif image_url:
        prompt["image_prompt"] = {"image_url": image_url}

    if not prompt:
        raise ValueError("text_prompt 和 image_prompt 不能同时为空，至少填一个")

    payload: dict = {
        "speaker_id": speaker_id,
        "text": text,
        "prompt": prompt,
        "language": language,
    }

    req = urllib.request.Request(
        API_URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Content-Type": "application/json",
            "X-Api-Key": api_key,
            "X-Api-Request-Id": str(uuid4()),
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"请求失败 (HTTP {e.code}): {body[:500]}") from e

    if data.get("code"):
        raise RuntimeError(f"音色设计失败 (code={data['code']}): {data.get('message', '')}")

    return data


def uuid4() -> str:
    """生成 UUID v4 字符串（纯标准库，避免依赖 uuid 的随机源差异）。"""
    import uuid
    return str(uuid.uuid4())


def main():
    parser = argparse.ArgumentParser(
        description="豆包音色设计（声音复刻）：用文字描述或图片设计一个专属音色。"
    )
    parser.add_argument("-s", "--speaker-id", default="", help="speaker_id（需控制台开通声音复刻服务后获得）")
    parser.add_argument("-p", "--prompt", default="", help="音色文字描述，如：女性，语速中等偏快，语调低沉有力（≤200字）")
    parser.add_argument("-t", "--text", default="夜色渐浓，城市的灯火次第亮起，每个人都在为自己的生活奔波，从未停歇。",
                        help="试听文本（≤300字，训练成功后会返回这段的试听音频）")
    parser.add_argument("--image", default="", help="参考图片文件路径（可选，与文字提示二选一或并用）")
    parser.add_argument("--image-url", default="", help="参考图片 URL（可选）")
    parser.add_argument("-l", "--language", type=int, default=0, choices=[0, 1], help="语种：0=中文（默认），1=英文")
    parser.add_argument("-o", "--output", default="", help="试听音频保存路径（默认保存到 output/audio/voice_design_demo.mp3）")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不调用 API")
    args = parser.parse_args()

    _load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    _load_dotenv(".env")

    if args.dry_run:
        print(json.dumps({
            "api_url": API_URL,
            "speaker_id": args.speaker_id or "(必填，需控制台开通声音复刻)",
            "text": args.text[:100],
            "prompt": {"text_prompt": args.prompt or "(未填)", "image_prompt": args.image or args.image_url or "(未填)"},
            "language": args.language,
        }, ensure_ascii=False, indent=2))
        return

    try:
        data = design_voice(args.speaker_id, args.text, args.prompt,
                            args.image, args.image_url, args.language)
        status = data.get("status", -1)
        result = {
            "success": True,
            "speaker_id": data.get("speaker_id", ""),
            "status": f"{status}={STATUS.get(status, 'Unknown')}",
            "available_training_times": data.get("available_training_times"),
            "create_time": data.get("create_time"),
            "demo_audio": data.get("demo_audio", ""),
        }
        # 尝试下载试听音频
        demo_url = data.get("demo_audio", "")
        if demo_url and status in (2, 4):
            out = args.output or os.path.join(
                os.getcwd(), "output", "audio", "voice_design_demo.mp3")
            os.makedirs(os.path.dirname(out), exist_ok=True)
            try:
                with urllib.request.urlopen(demo_url, timeout=60) as resp:
                    with open(out, "wb") as f:
                        f.write(resp.read())
                result["demo_saved"] = out
            except Exception as e:
                result["demo_download_error"] = str(e)
        print(json.dumps(result, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

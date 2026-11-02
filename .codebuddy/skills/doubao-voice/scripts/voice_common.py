"""
豆包声音复刻公共模块（doubao-voice）
三个复刻接口共用：音色训练 voice_clone / 音色查询 voice_query / 音色升级 voice_upgrade
鉴权：X-Api-Key（UUID，与 TTS 同一个 Key）
纯标准库实现。
"""

import os
import json
import uuid
import urllib.request
import urllib.error

STATUS = {0: "NotFound", 1: "Training", 2: "Success", 3: "Failed", 4: "Active"}

LANGUAGES = {
    0: "中文", 1: "英文", 2: "日语", 3: "西班牙语", 4: "印尼语", 5: "葡萄牙语",
    6: "德语", 7: "法语", 8: "韩语", 9: "意大利语", 10: "泰语", 11: "越南语",
    12: "俄语", 13: "菲律宾语", 14: "马来语", 15: "阿拉伯语", 16: "墨西哥西语",
    17: "巴西葡语", 19: "波兰语", 20: "土耳其语", 21: "瑞典语",
}


def load_dotenv(path: str) -> None:
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


def post_json(url: str, payload: dict, timeout: int = 120) -> dict:
    """POST JSON 到 openspeech 接口，返回解析后的 dict。"""
    api_key = get_api_key()
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Content-Type": "application/json",
            "X-Api-Key": api_key,
            "X-Api-Request-Id": str(uuid.uuid4()),
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"请求失败 (HTTP {e.code}): {body[:500]}") from e


def parse_voice_response(data: dict, output: str = "") -> dict:
    """解析复刻接口通用响应，可下载试听音频。"""
    if data.get("code"):
        raise RuntimeError(f"接口错误 (code={data['code']}): {data.get('message', '')}")

    status = data.get("status", -1)
    result = {
        "success": True,
        "speaker_id": data.get("speaker_id", ""),
        "status": f"{status}={STATUS.get(status, 'Unknown')}",
        "available_training_times": data.get("available_training_times"),
        "language": data.get("language"),
        "demo_audio": data.get("demo_audio", ""),
    }

    # speaker_status 数组（复刻 2.0: model_type=5）
    ss = data.get("speaker_status")
    if ss and isinstance(ss, list) and ss:
        result["models"] = [
            {"model_type": m.get("model_type"), "demo_audio": m.get("demo_audio", "")}
            for m in ss
        ]
        # 取第一个有试听音频的
        for m in ss:
            if m.get("demo_audio") and not result["demo_audio"]:
                result["demo_audio"] = m["demo_audio"]

    demo_url = result["demo_audio"]
    if demo_url and status in (2, 4):
        out = output or os.path.join(os.getcwd(), "output", "audio", f"voice_demo_{result['speaker_id'] or 'x'}.mp3")
        os.makedirs(os.path.dirname(out), exist_ok=True)
        try:
            with urllib.request.urlopen(demo_url, timeout=60) as resp:
                with open(out, "wb") as f:
                    f.write(resp.read())
            result["demo_saved"] = out
        except Exception as e:
            result["demo_download_error"] = str(e)
    return result

"""
豆包语音合成 - 异步长文本（submit/query，最大 10 万字符）
提交：POST https://openspeech.bytedance.com/api/v3/tts/submit
查询：POST https://openspeech.bytedance.com/api/v3/tts/query
资源：seed-tts-2.0（默认）/ seed-icl-2.0（复刻音色）
场景：长文本（小说、长报告）、批量离线合成；音频保存 7 天，下载链接 1 小时有效
"""

import os
import sys
import json
import time
import uuid
import argparse
import urllib.request
import urllib.error

SUBMIT_URL = "https://openspeech.bytedance.com/api/v3/tts/submit"
QUERY_URL = "https://openspeech.bytedance.com/api/v3/tts/query"
RESOURCE_ID = "seed-tts-2.0"


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


def _post(url: str, payload: dict, api_key: str, request_id: str) -> tuple[int, dict]:
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Content-Type": "application/json",
            "X-Api-Key": api_key,
            "X-Api-Resource-Id": RESOURCE_ID,
            "X-Api-Request-Id": request_id,
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            code = resp.status
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        code = e.code
    try:
        data = json.loads(body) if body.strip() else {}
    except json.JSONDecodeError:
        data = {"raw": body}
    return code, data


def synthesize_longtext(text: str, voice: str, output: str,
                        max_wait: int = 300, poll_interval: int = 5,
                        sample_rate: int = 24000) -> str:
    """异步长文本合成：submit 拿 task_id → 轮询 query → 下载音频。"""
    api_key = get_api_key()
    if len(text) > 100000:
        raise ValueError(f"文本过长（{len(text)} 字符），上限 10 万字符")

    request_id = str(uuid.uuid4())
    submit_payload = {
        "user": {"uid": "workbuddy"},
        "unique_id": request_id,
        "req_params": {
            "text": text,
            "speaker": voice,
            "audio_params": {"format": "mp3", "sample_rate": sample_rate},
        },
    }

    code, data = _post(SUBMIT_URL, submit_payload, api_key, request_id)
    if code != 200:
        raise RuntimeError(f"提交任务失败 (HTTP {code}): {json.dumps(data, ensure_ascii=False)[:300]}")

    task_id = (data.get("data") or {}).get("task_id") or request_id
    status = (data.get("data") or {}).get("task_status", 1)

    deadline = time.time() + max_wait
    while status == 1 and time.time() < deadline:
        time.sleep(poll_interval)
        qcode, qdata = _post(QUERY_URL, {"task_id": task_id}, api_key, request_id)
        if qcode != 200:
            continue
        body = qdata.get("data") or qdata
        status = body.get("task_status", 1)
        if status == 2:
            audio_url = body.get("audio_url")
            if not audio_url:
                raise RuntimeError("任务完成但未返回 audio_url")
            urllib.request.urlretrieve(audio_url, output)
            return output
        if status == 3:
            raise RuntimeError(f"合成失败，响应: {json.dumps(qdata, ensure_ascii=False)[:300]}")

    if status != 2:
        raise TimeoutError(f"合成超时（{max_wait}s），task_id={task_id}")


def main():
    p = argparse.ArgumentParser(description="豆包语音合成 - 异步长文本（≤10 万字符）")
    p.add_argument("-t", "--text", required=True, help="要合成的长文本（≤10 万字符）")
    p.add_argument("-f", "--file", default="", help="从文件读取文本（与 -t 二选一）")
    p.add_argument("-v", "--voice", default="zh_female_shuangkuaisisi_uranus_bigtts",
                   help="音色 ID（默认爽快思思 2.0）")
    p.add_argument("-o", "--output", default="speech_longtext.mp3", help="输出音频路径")
    p.add_argument("--max-wait", type=int, default=300, help="最大等待秒数（默认 300）")
    p.add_argument("--dry-run", action="store_true")
    args = p.parse_args()

    _load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    _load_dotenv(".env")

    text = args.text
    if args.file:
        if not os.path.isfile(args.file):
            p.error(f"文件不存在: {args.file}")
        with open(args.file, encoding="utf-8") as f:
            text = f.read()
    if not text or not text.strip():
        p.error("文本不能为空")

    if args.dry_run:
        print(json.dumps({"submit_url": SUBMIT_URL, "query_url": QUERY_URL,
                          "resource_id": RESOURCE_ID, "text_len": len(text),
                          "voice": args.voice}, ensure_ascii=False, indent=2))
        return

    try:
        path = synthesize_longtext(text, args.voice, args.output, args.max_wait)
        print(json.dumps({"success": True, "file": path}, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

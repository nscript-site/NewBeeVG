"""
豆包发音人（音色）库刷新（doubao-voice）脚本

走火山引擎 OpenAPI 的 ListSpeakers 接口（Action=ListSpeakers, Version=2025-05-20）
拉取官方大模型音色列表，重写本地音色库 data/voices.json（供 list_voices.py 查询）。

- 鉴权：AKSK 签名（与 openspeech 的 UUID Key 不是一套），需环境变量
  VOLCENGINE_AK / VOLCENGINE_SK（火山引擎控制台「访问控制 → API 访问密钥」创建）
- 纯标准库，不引入第三方依赖
- ListSpeakers 不返回控制台的 Heat/描述热度字段：刷新时按 voice_type 继承旧 Heat，
  新音色 Heat=0；其余字段以接口返回为准

用法：
    python3 scripts/refresh_voices.py --dry-run          # 只打印请求，不发出去
    python3 scripts/refresh_voices.py                    # 拉取并覆盖 data/voices.json
    python3 scripts/refresh_voices.py --out /tmp/v.json   # 写到别处对比
    python3 scripts/refresh_voices.py --resource seed-tts-1.0
"""

import os
import sys
import json
import hmac
import hashlib
import argparse
import urllib.request
import urllib.error
from datetime import datetime, timezone

HOST = "open.volcengineapi.com"
SERVICE = "speech_saas_prod"
VERSION = "2025-05-20"
ACTION = "ListSpeakers"

HERE = os.path.dirname(os.path.abspath(__file__))
CATALOG = os.path.join(HERE, "..", "data", "voices.json")


def load_dotenv(path: str) -> None:
    if not os.path.isfile(path):
        return
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, v = line.split("=", 1)
            k, v = k.strip(), v.strip().strip('"').strip("'")
            if k and k not in os.environ:
                os.environ[k] = v


def get_credentials() -> tuple:
    ak = os.environ.get("VOLCENGINE_AK") or os.environ.get("VOLCENGINE_ACCESS_KEY_ID")
    sk = os.environ.get("VOLCENGINE_SK") or os.environ.get("VOLCENGINE_SECRET_ACCESS_KEY")
    if not ak or not sk:
        raise ValueError(
            "未找到 AK/SK。请配置环境变量 VOLCENGINE_AK / VOLCENGINE_SK"
            "（火山引擎控制台「访问控制 → API 访问密钥」创建，"
            "不是 openspeech 的 UUID Key，也不是 legacy 的 AppID/AccessToken）"
        )
    return ak, sk


def _hmac(key: bytes, msg: str) -> bytes:
    return hmac.new(key, msg.encode("utf-8"), hashlib.sha256).digest()


def sign_headers(ak: str, sk: str, body_bytes: bytes, region: str) -> dict:
    """构造火山 OpenAPI V4 签名请求头。"""
    now = datetime.now(timezone.utc)
    x_date = now.strftime("%Y%m%dT%H%M%SZ")
    short_date = now.strftime("%Y%m%d")

    query = f"Action={ACTION}&Version={VERSION}"
    x_content_sha256 = hashlib.sha256(body_bytes).hexdigest()
    canonical_headers = (
        f"content-type:application/json; charset=utf-8\n"
        f"host:{HOST}\n"
        f"x-content-sha256:{x_content_sha256}\n"
        f"x-date:{x_date}\n"
    )
    signed_headers = "content-type;host;x-content-sha256;x-date"
    canonical_request = "\n".join(["POST", "/", query, canonical_headers, signed_headers, x_content_sha256])

    scope = f"{short_date}/{region}/{SERVICE}/request"
    string_to_sign = "\n".join([
        "HMAC-SHA256", x_date, scope, hashlib.sha256(canonical_request.encode("utf-8")).hexdigest(),
    ])
    k = _hmac(sk.encode("utf-8"), short_date)
    k = _hmac(k, region)
    k = _hmac(k, SERVICE)
    k = _hmac(k, "request")
    signature = hmac.new(k, string_to_sign.encode("utf-8"), hashlib.sha256).hexdigest()

    return {
        "Host": HOST,
        "Content-Type": "application/json; charset=utf-8",
        "X-Date": x_date,
        "X-Content-Sha256": x_content_sha256,
        "Authorization": (
            f"HMAC-SHA256 Credential={ak}/{scope}, "
            f"SignedHeaders={signed_headers}, Signature={signature}"
        ),
    }


def call_list_speakers(ak: str, sk: str, region: str, body: dict, timeout: int = 60) -> dict:
    body_bytes = json.dumps(body, ensure_ascii=False).encode("utf-8")
    headers = sign_headers(ak, sk, body_bytes, region)
    url = f"https://{HOST}/?Action={ACTION}&Version={VERSION}"
    req = urllib.request.Request(url, data=body_bytes, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", errors="replace")
        try:
            data = json.loads(raw)
        except json.JSONDecodeError:
            raise RuntimeError(f"请求失败 (HTTP {e.code}): {raw[:300]}")
    meta = data.get("ResponseMetadata", {}) or {}
    err = meta.get("Error")
    if err:
        hint = ""
        if err.get("Code") == "InvalidAccessKey":
            hint = "（AK/SK 不是火山引擎访问密钥，或密钥已失效）"
        elif "not granted" in str(err.get("Message", "")):
            hint = "（该账号未开通豆包语音服务，或密钥无 speech_saas_prod 权限）"
        raise RuntimeError(f"接口错误 [{err.get('Code')}]: {err.get('Message', '')}{hint}")
    return data.get("Result", {}) or {}


def trim(speaker: dict, heat: int) -> dict:
    """把接口条目裁剪成与本地音色库一致的字段。"""
    cats, langs, emotions = [], [], []
    for c in speaker.get("Categories") or []:
        for n in c.get("Categories") or []:
            if n and n not in cats:
                cats.append(n)
    for l in speaker.get("Languages") or []:
        code = l.get("Language")
        if code and code not in langs:
            langs.append(code)
    for m in speaker.get("Emotions") or []:
        val = m.get("Value") or m.get("Label")
        if val and val not in emotions:
            emotions.append(val)
    return {
        "voice_type": speaker.get("VoiceType"),
        "name": speaker.get("Name"),
        "gender": speaker.get("Gender"),
        "age": speaker.get("Age"),
        "resource": speaker.get("ResourceID"),
        "categories": cats,
        "languages": langs,
        "labels": speaker.get("SpecialLabels") or [],
        "emotions": emotions,
        "description": speaker.get("Description") or "",
        "heat": heat,
        "trial": speaker.get("TrialURL") or "",
    }


def load_old_heat(path: str) -> dict:
    """读旧快照里的 heat，刷新时继承（接口不返回热度）。"""
    if not os.path.isfile(path):
        return {}
    try:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
    except (OSError, json.JSONDecodeError):
        return {}
    voices = data.get("voices") if isinstance(data, dict) else data
    if not isinstance(voices, list):
        return {}
    return {v.get("voice_type"): (v.get("heat") or 0) for v in voices if isinstance(v, dict)}


def main() -> None:
    p = argparse.ArgumentParser(description="拉取火山引擎官方音色列表（ListSpeakers），刷新本地音色库。")
    p.add_argument("--resource", action="append", default=None, help="模型资源 ID，可重复（默认 seed-tts-2.0）")
    p.add_argument("--region", default="cn-beijing", help="签名 Region（默认 cn-beijing，与官方文档示例一致）")
    p.add_argument("--page-size", type=int, default=100, help="每页条数（默认 100）")
    p.add_argument("--max-pages", type=int, default=50, help="最多翻页数（默认 50）")
    p.add_argument("--out", default=CATALOG, help="输出文件（默认 data/voices.json）")
    p.add_argument("--keep-heat", action="store_true", default=True, help="按 voice_type 继承旧热度（默认开启）")
    p.add_argument("--no-keep-heat", dest="keep_heat", action="store_false", help="不继承旧热度，全部置 0")
    p.add_argument("--dry-run", action="store_true", help="只打印签名后的请求，不实际调用")
    p.add_argument("--timeout", type=int, default=60, help="单次请求超时秒数")
    args = p.parse_args()

    load_dotenv(os.path.join(HERE, "..", ".env"))
    load_dotenv(".env")

    resources = args.resource or ["seed-tts-2.0"]

    try:
        if args.dry_run:
            ak, _ = get_credentials()
            body = {"ResourceIDs": resources, "Page": 1, "Limit": args.page_size}
            print(json.dumps({
                "url": f"https://{HOST}/?Action={ACTION}&Version={VERSION}",
                "region": args.region,
                "service": SERVICE,
                "ak_prefix": ak[:6] + "…",
                "body": body,
            }, ensure_ascii=False, indent=2))
            return

        ak, sk = get_credentials()
        collected, page, total = [], 1, None
        while page <= args.max_pages:
            result = call_list_speakers(
                ak, sk, args.region,
                {"ResourceIDs": resources, "Page": page, "Limit": args.page_size},
                args.timeout,
            )
            total = result.get("Total", total)
            speakers = result.get("Speakers") or []
            if not speakers:
                break
            collected.extend(speakers)
            if total is not None and len(collected) >= int(total):
                break
            page += 1

        old_heat = load_old_heat(args.out) if args.keep_heat else {}
        voices, seen = [], set()
        for s in collected:
            vt = s.get("VoiceType")
            if not vt or vt in seen:
                continue
            seen.add(vt)
            voices.append(trim(s, old_heat.get(vt, 0)))
        voices.sort(key=lambda v: (-(v.get("heat") or 0), v.get("name") or ""))

        data = {
            "_meta": {
                "source": f"火山引擎 OpenAPI {ACTION}（Version {VERSION}）",
                "snapshot": datetime.now().strftime("%Y-%m-%d"),
                "generated": datetime.now().strftime("%Y-%m-%d"),
                "count": len(voices),
                "resource": sorted({v.get("resource") or "" for v in voices if v.get("resource")}),
                "note": "接口不返回控制台热度，heat 由上一版快照按 voice_type 继承，新音色为 0",
            },
            "voices": voices,
        }
        out = os.path.abspath(args.out)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with open(out, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, separators=(",", ":"))

        print(json.dumps({
            "success": True,
            "api_total": total,
            "collected": len(collected),
            "unique": len(voices),
            "new_voices": sorted(set(v["voice_type"] for v in voices) - set(old_heat))[:20],
            "heat_inherited": sum(1 for v in voices if v["heat"]),
            "out": out,
        }, ensure_ascii=False, indent=2))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

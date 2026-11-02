"""
豆包音色管理（doubao-voice）脚本
通过火山引擎 OpenAPI（AKSK 签名）查询/管理声音复刻音色。
核心用途：查账号下有哪些复刻音色（SpeakerID）、状态、剩余训练次数、试听音频。
接口体系：open.volcengineapi.com（火山 OpenAPI，AKSK 签名，与 openspeech 的 UUID Key 不同体系）
Region = cn-north-1, Service = speech_saas_prod, Version = 2023-11-07
纯标准库实现，无第三方依赖。
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
REGION = "cn-north-1"
SERVICE = "speech_saas_prod"
VERSION = "2023-11-07"

# 音色状态枚举
STATE = {
    "Unknown": "尚未训练", "Training": "训练中", "Success": "训练成功，可合成",
    "Active": "已激活（不可再训练）", "Expired": "已过期/欠费", "Reclaimed": "已回收",
}


def _load_dotenv(path: str) -> None:
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


def get_credentials() -> tuple:
    """读取 AKSK。"""
    ak = os.environ.get("VOLCENGINE_AK") or os.environ.get("VOLCENGINE_ACCESS_KEY")
    sk = os.environ.get("VOLCENGINE_SK") or os.environ.get("VOLCENGINE_SECRET_KEY")
    if not ak or not sk:
        raise ValueError(
            "未找到 AK/SK。请在 .env 中配置 VOLCENGINE_AK / VOLCENGINE_SK "
            "（火山引擎控制台「访问控制」创建，console.volcengine.com/iam/keymanage）"
        )
    return ak, sk


def _sha256_hex(s: bytes) -> str:
    return hashlib.sha256(s).hexdigest()


def _hmac(key: bytes, msg: str) -> bytes:
    return hmac.new(key, msg.encode("utf-8"), hashlib.sha256).digest()


def sign_request(ak: str, sk: str, action: str, body_bytes: bytes,
                 region: str = REGION, service: str = SERVICE) -> dict:
    """构造火山 OpenAPI 签名请求头。"""
    now = datetime.now(timezone.utc)
    x_date = now.strftime("%Y%m%dT%H%M%SZ")
    short_date = now.strftime("%Y%m%d")

    query = f"Action={action}&Version={VERSION}"
    uri = "/"

    # CanonicalHeaders（排序、小写）
    x_content_sha256 = _sha256_hex(body_bytes)
    canonical_headers = (
        f"content-type:application/json; charset=utf-8\n"
        f"host:{HOST}\n"
        f"x-content-sha256:{x_content_sha256}\n"
        f"x-date:{x_date}\n"
    )
    signed_headers = "content-type;host;x-content-sha256;x-date"

    canonical_request = "\n".join([
        "POST", uri, query, canonical_headers, signed_headers, x_content_sha256,
    ])

    credential_scope = f"{short_date}/{region}/{service}/request"
    string_to_sign = "\n".join([
        "HMAC-SHA256", x_date, credential_scope, _sha256_hex(canonical_request.encode("utf-8")),
    ])

    k_date = _hmac(sk.encode("utf-8"), short_date)
    k_region = _hmac(k_date, region)
    k_service = _hmac(k_region, service)
    k_signing = _hmac(k_service, "request")
    signature = hmac.new(k_signing, string_to_sign.encode("utf-8"), hashlib.sha256).hexdigest()

    authorization = (
        f"HMAC-SHA256 Credential={ak}/{credential_scope}, "
        f"SignedHeaders={signed_headers}, Signature={signature}"
    )

    return {
        "Host": HOST,
        "Content-Type": "application/json; charset=utf-8",
        "X-Date": x_date,
        "X-Content-Sha256": x_content_sha256,
        "Authorization": authorization,
    }


def call_api(action: str, app_id: str, body_extra: dict = None, timeout: int = 60) -> dict:
    """调用火山 OpenAPI。"""
    ak, sk = get_credentials()
    body: dict = {"AppID": int(app_id)}
    if body_extra:
        body.update(body_extra)
    body_bytes = json.dumps(body).encode("utf-8")
    headers = sign_request(ak, sk, action, body_bytes)
    url = f"https://{HOST}/?Action={action}&Version={VERSION}"

    req = urllib.request.Request(url, data=body_bytes, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        data = json.loads(e.read().decode("utf-8", errors="replace"))
        raise RuntimeError(f"请求失败 (HTTP {e.code}): {json.dumps(data, ensure_ascii=False)[:400]}")

    meta = data.get("ResponseMetadata", {})
    err = meta.get("Error")
    if err:
        raise RuntimeError(f"接口错误 [{err.get('Code')}]: {err.get('Message', '')}")
    return data.get("Result", {})


def main():
    parser = argparse.ArgumentParser(
        description="豆包音色管理（查询复刻音色状态）。"
    )
    parser.add_argument("--app-id", required=True, help="AppID（控制台「概览页→API接入」可查，数字）")
    parser.add_argument("--speaker-ids", nargs="*", default=[], help="指定 S_ 开头的音色 ID 查询（不传则查全部）")
    parser.add_argument("--state", default="", help="按状态过滤：Unknown/Training/Success/Active/Expired/Reclaimed")
    parser.add_argument("--page-size", type=int, default=50, help="每页条数（1-100，默认 50）")
    parser.add_argument("--dry-run", action="store_true", help="仅打印签名请求参数，不调用 API")
    args = parser.parse_args()

    _load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    _load_dotenv(".env")

    if args.dry_run:
        ak, sk = get_credentials()
        body = {"AppID": int(args.app_id)}
        if args.speaker_ids:
            body["SpeakerIDs"] = args.speaker_ids
        if args.state:
            body["State"] = args.state
        body["PageSize"] = args.page_size
        headers = sign_request(ak, sk, "BatchListMegaTTSTrainStatus", json.dumps(body).encode())
        print(json.dumps({
            "url": f"https://{HOST}/?Action=BatchListMegaTTSTrainStatus&Version={VERSION}",
            "headers": {k: (v[:80] + "..." if len(v) > 80 else v) for k, v in headers.items()},
            "body": body,
        }, ensure_ascii=False, indent=2))
        return

    try:
        body_extra: dict = {}
        if args.speaker_ids:
            body_extra["SpeakerIDs"] = args.speaker_ids
        if args.state:
            body_extra["State"] = args.state
        body_extra["PageSize"] = args.page_size

        result = call_api("BatchListMegaTTSTrainStatus", args.app_id, body_extra)
        statuses = result.get("Statuses", [])
        print(json.dumps({
            "success": True,
            "total": result.get("TotalCount", len(statuses)),
            "next_token": result.get("NextToken", ""),
            "voices": [
                {
                    "speaker_id": s.get("SpeakerID"),
                    "state": f"{s.get('State')}={STATE.get(s.get('State'), '')}",
                    "version": s.get("Version"),
                    "available_training_times": s.get("AvailableTrainingTimes"),
                    "demo_audio": s.get("DemoAudio"),
                    "alias": s.get("Alias"),
                    "models": [
                        {"resource_id": m.get("ResourceID"), "icl_speaker_id": m.get("IclSpeakerId")}
                        for m in s.get("ModelTypeDetails", [])
                    ],
                }
                for s in statuses
            ],
        }, ensure_ascii=False, indent=2))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

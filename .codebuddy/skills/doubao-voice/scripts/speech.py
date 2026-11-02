"""
豆包语音合成（doubao-voice）脚本
通过豆包语音控制台 openspeech 接口（HTTP Chunked 单向流式）将文本合成为语音。
接口：https://openspeech.bytedance.com/api/v3/tts/unidirectional
鉴权：X-Api-Key（UUID，从豆包语音控制台获取）+ X-Api-Resource-Id（seed-tts-1.0 / seed-tts-2.0）
注意：此接口与方舟 ARK（/api/v3/audio/speech）是两套体系，Key 不能混用。
纯标准库实现，无第三方依赖。
"""

import os
import sys
import json
import argparse
import urllib.request
import urllib.error

API_URL = "https://openspeech.bytedance.com/api/v3/tts/unidirectional"
DEFAULT_MODEL = "doubao-seed-tts-2-0"
DEFAULT_VOICE = "zh_female_shuangkuaisisi_uranus_bigtts"

# 常用音色速查（官方音色列表精选，来源：volcengine.com/docs/6561/1257544）
# 规则：mars 结尾 = 1.0 音色（配 seed-tts-1.0）；uranus 结尾 = 2.0 音色（配 seed-tts-2.0）
VOICES = {
    # ===== 豆包语音合成 1.0（seed-tts-1.0，mars 音色）=====
    "zh_female_shuangkuaisisi_mars_bigtts": "爽快思思（1.0 默认）",
    "zh_female_cancan_mars_bigtts": "知性灿灿",
    "zh_female_vv_mars_bigtts": "Vivi（活泼灵动女声）",
    "zh_female_xiaohe_mars_bigtts": "小何（甜美女声）",
    "zh_male_yunzhou_mars_bigtts": "云舟（清爽沉稳男声）",
    "zh_male_xiaotian_mars_bigtts": "小天（清爽磁性男声）",
    "zh_male_jingqiang_dylan_bigtts": "阳光青年",
    "zh_male_yuanxiaoshuo_mars_bigtts": "解说男声",
    "zh_female_shaoergushi_mars_bigtts": "少儿故事（儿童故事）",
    # ===== 豆包语音合成 2.0（seed-tts-2.0，uranus 音色）=====
    # 通用场景
    "zh_female_shuangkuaisisi_uranus_bigtts": "爽快思思（2.0）",
    "zh_female_cancan_uranus_bigtts": "知性灿灿（2.0）",
    "zh_female_tianmeixiaoyuan_uranus_bigtts": "甜美小源（2.0）",
    "zh_male_m191_uranus_bigtts": "云舟（2.0）",
    "zh_male_taocheng_uranus_bigtts": "小天（2.0）",
    "zh_female_xiaohe_uranus_bigtts": "小何（2.0）",
    "zh_female_vv_uranus_bigtts": "Vivi（2.0）",
    "zh_female_tianmeitaozi_uranus_bigtts": "甜美桃子（2.0）",
    "zh_female_qingxinnvsheng_uranus_bigtts": "清新女声（2.0）",
    "zh_female_sophie_uranus_bigtts": "魅力苏菲（2.0）",
    "zh_male_liufei_uranus_bigtts": "刘飞（2.0）",
    "zh_female_wenroumama_uranus_bigtts": "温柔妈妈（2.0）",
    "zh_female_tiexinnvsheng_uranus_bigtts": "贴心女声（2.0）",
    "zh_male_yangguangqingnian_uranus_bigtts": "阳光青年（2.0）",
    "zh_female_wenrouxiaoya_uranus_bigtts": "温柔小雅（2.0）",
    "zh_female_roumeinvyou_uranus_bigtts": "柔美女友（2.0）",
    # 视频配音
    "zh_female_kefunvsheng_uranus_bigtts": "暖阳女声（2.0）",
    "zh_male_jieshuoxiaoming_uranus_bigtts": "解说小明（2.0）",
    "zh_male_cixingjieshuonan_uranus_bigtts": "磁性解说男声（2.0）",
    "zh_male_guanggaojieshuo_uranus_bigtts": "广告解说（2.0）",
    "zh_male_dayi_uranus_bigtts": "大壹（2.0，纪录片感）",
    "zh_male_ruyayichen_uranus_bigtts": "儒雅逸辰（2.0）",
    # 有声阅读
    "zh_female_shaoergushi_uranus_bigtts": "少儿故事（2.0）",
    "zh_female_xiaoxue_uranus_bigtts": "儿童绘本（2.0）",
    "zh_male_shenyeboke_uranus_bigtts": "深夜播客（2.0）",
    "zh_male_baqiqingshu_uranus_bigtts": "霸气青叔（2.0）",
    # 角色扮演 / IP 同款
    "zh_female_meilinvyou_uranus_bigtts": "魅力女友（2.0）",
    "zh_female_sajiaoxuemei_uranus_bigtts": "撒娇学妹（2.0）",
    "zh_male_aojiaobazong_uranus_bigtts": "傲娇霸总（2.0）",
    "zh_female_gaolengyujie_uranus_bigtts": "高冷御姐（2.0）",
    "zh_male_sunwukong_uranus_bigtts": "猴哥（2.0，悟空）",
    "zh_male_xionger_uranus_bigtts": "熊二（2.0）",
    "zh_male_zhubajie_uranus_bigtts": "猪八戒（2.0）",
    "zh_female_yingtaowanzi_uranus_bigtts": "樱桃丸子（2.0）",
    "zh_male_lubanqihao_uranus_bigtts": "鲁班七号（2.0，游戏）",
    "zh_male_tangseng_uranus_bigtts": "唐僧（2.0）",
    "zh_male_qingcang_uranus_bigtts": "擎苍（2.0，小说）",
    # 教育
    "zh_female_yingyujiaoxue_uranus_bigtts": "Tina老师（2.0，英语教学）",
    "zh_male_tiancaitongsheng_uranus_bigtts": "天才童声（2.0）",
    # 多语种
    "en_male_tim_uranus_bigtts": "Tim（2.0，美式英语）",
    "en_female_dacey_uranus_bigtts": "Dacey（2.0，美式英语）",
    "en_female_stokie_uranus_bigtts": "Stokie（2.0，美式英语）",
}


def _load_dotenv(path: str) -> None:
    """简易 .env 解析，不覆盖已有环境变量。"""
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
    """读取豆包语音控制台的 UUID Key（openspeech 接口专用，与方舟 Key 不通用）。"""
    for name in ("VOLCENGINE_SPEECH_API_KEY", "DOUBAO_SPEECH_API_KEY"):
        if os.environ.get(name):
            return os.environ[name]
    raise ValueError(
        "未找到豆包语音 API Key。请在 .env 中配置 VOLCENGINE_SPEECH_API_KEY（UUID 格式，"
        "从豆包语音控制台 console.volcengine.com/speech/new/ 获取）。"
        "注意：方舟 ARK 的 key（ark- 开头）不能用于本接口。"
    )


def synthesize(
    text: str,
    model: str = DEFAULT_MODEL,
    voice: str = DEFAULT_VOICE,
    output: str = "speech.mp3",
    speed: float = 1.0,
) -> str:
    """调用豆包 TTS 接口，将文本合成为音频文件，返回本地文件路径。"""
    api_key = get_api_key()

    if not text.strip():
        raise ValueError("合成文本不能为空")

    # 映射模型到 resource_id
    if "2-0" in model or "2.0" in model:
        resource_id = "seed-tts-2.0"
    else:
        resource_id = "seed-tts-1.0"

    import uuid

    payload = {
        "user": {"uid": "workbuddy"},
        "namespace": "BidirectionalTTS",
        "req_params": {
            "text": text,
            "speaker": voice,
            "audio_params": {"format": "mp3"},
        },
    }
    # speed(0.5-2.0) → audio_params.speech_rate(-50~100)；1.0 时不下发，保持接口默认韵律
    if abs(speed - 1.0) > 1e-6:
        payload["req_params"]["audio_params"]["speech_rate"] = max(-50, min(100, int(round((speed - 1.0) * 100))))

    req = urllib.request.Request(
        API_URL,
        data=json.dumps(payload).encode("utf-8"),
        headers={
            "Content-Type": "application/json",
            "X-Api-Key": api_key,
            "X-Api-Resource-Id": resource_id,
            "X-Api-Request-Id": str(uuid.uuid4()),
        },
        method="POST",
    )

    try:
        with urllib.request.urlopen(req, timeout=180) as resp:
            raw = resp.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as e:
        err_body = e.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"API 请求失败 (HTTP {e.code}): {err_body}") from e
    except Exception as e:
        raise RuntimeError(f"网络请求失败: {e}") from e

    # 响应是 NDJSON 流：每行一个 JSON 对象，data 字段为 base64 音频片段
    audio = bytearray()
    for line in raw.strip().split("\n"):
        line = line.strip()
        if not line:
            continue
        try:
            chunk = json.loads(line)
        except json.JSONDecodeError:
            continue
        if chunk.get("code") not in (0, 20000000):
            raise RuntimeError(f"API 错误: {json.dumps(chunk, ensure_ascii=False)}")
        if chunk.get("data"):
            import base64
            audio.extend(base64.b64decode(chunk["data"]))

    if not audio:
        raise RuntimeError("未收到音频数据")

    os.makedirs(os.path.dirname(os.path.abspath(output)) if os.path.dirname(output) else ".", exist_ok=True)
    with open(output, "wb") as f:
        f.write(audio)
    return output


def main():
    parser = argparse.ArgumentParser(description="豆包语音合成（doubao-voice）")
    parser.add_argument("-t", "--text", help="要合成的文本")
    parser.add_argument("-m", "--model", default=DEFAULT_MODEL, help=f"模型（默认 {DEFAULT_MODEL}）")
    parser.add_argument("-v", "--voice", default=DEFAULT_VOICE, help=f"音色（默认 {DEFAULT_VOICE}）")
    parser.add_argument("-o", "--output", default="speech.mp3", help="输出音频文件路径（默认 speech.mp3）")
    parser.add_argument("--speed", type=float, default=1.0, help="语速 0.5-2.0（默认 1.0）")
    parser.add_argument("--list-voices", action="store_true", help="列出常用音色")
    parser.add_argument("--dry-run", action="store_true", help="仅打印请求参数，不调用 API")
    args = parser.parse_args()

    # 优先读取脚本同级 .env，再读当前工作目录 .env
    _load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".env"))
    _load_dotenv(".env")

    if args.list_voices:
        for vid, desc in VOICES.items():
            print(f"{vid}  # {desc}")
        return

    if not args.text:
        parser.error("-t/--text 是必填参数（除非使用 --list-voices）")

    if args.dry_run:
        print(json.dumps({
            "api_url": API_URL,
            "model": args.model,
            "voice": args.voice,
            "speed": args.speed,
            "text": args.text[:50] + ("..." if len(args.text) > 50 else ""),
            "output": args.output,
        }, ensure_ascii=False, indent=2))
        return

    try:
        path = synthesize(args.text, args.model, args.voice, args.output, args.speed)
        print(json.dumps({"success": True, "file": path}, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

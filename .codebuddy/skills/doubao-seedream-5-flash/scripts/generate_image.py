#!/usr/bin/env python3
"""Generate or edit images with Doubao Seedream 5.0 Flash.

Text-to-image:
    python generate_image.py --prompt "..." --output assets/images/cover.png

Image-to-image:
    python generate_image.py --prompt "..." --image assets/images/ref.png \
        --output assets/images/edited.png

Multi-reference:
    python generate_image.py --prompt "把图1的服装换成图2的服装" \
        --image assets/images/person.png --image assets/images/cloth.png \
        --output assets/images/result.png

Markdown scene document (skip existing files, generate missing ones):
    python generate_image.py --doc assets/images/scenes.md
"""

from __future__ import annotations

import argparse
import base64
import json
import mimetypes
import os
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import NoReturn

try:
    import requests
except ImportError:
    print("缺少 requests，请先运行: python -m pip install requests", file=sys.stderr)
    raise SystemExit(1)

DEFAULT_MODEL = "doubao-seedream-5-0-flash-260915"
DEFAULT_ENDPOINT = "https://ark.cn-beijing.volces.com/api/v3/images/generations"
SKILL_DIR = Path(__file__).resolve().parent.parent
DEFAULT_ENV_FILE = SKILL_DIR / ".env"
PLACEHOLDER_KEYS = {"", "your_api_key_here", "<your_api_key>", "YOUR_API_KEY"}

ALLOWED_IMAGE_MIME = {
    "image/jpeg",
    "image/png",
    "image/webp",
    "image/bmp",
    "image/tiff",
    "image/gif",
    "image/heic",
    "image/heif",
}
MAX_INPUT_IMAGES = 10
MAX_INPUT_BYTES = 30 * 1024 * 1024

SUPPORTED_RATIOS = ("1:1", "4:3", "3:4", "16:9", "9:16", "21:9")
SCENE_PATTERNS = (
    # 标准 Markdown：![场景简介](./scene-01.jpg)
    re.compile(r"^!\s*\[(?P<alt>[^\]]*)\]\s*\((?P<path>[^)]+)\)\s*$"),
    # 兼容旧格式：!(场景简介)[./scene-01.jpg]
    re.compile(r"^!\s*\((?P<alt>[^)]*)\)\s*\[(?P<path>[^\]]+)\]\s*$"),
)

RATIO_SIZES = {
    "1K": {
        "1:1": (1024, 1024),
        "4:3": (1152, 864),
        "3:4": (864, 1152),
        "16:9": (1424, 800),
        "9:16": (800, 1424),
        "21:9": (1568, 672),
    },
    "1.5K": {
        "1:1": (1536, 1536),
        "4:3": (1792, 1344),
        "3:4": (1344, 1792),
        "16:9": (2048, 1152),
        "9:16": (1152, 2048),
        "21:9": (2352, 1008),
    },
    "2K": {
        "1:1": (2048, 2048),
        "4:3": (2368, 1776),
        "3:4": (1776, 2368),
        "16:9": (2816, 1584),
        "9:16": (1584, 2816),
        "21:9": (3136, 1344),
    },
}


def die(message: str) -> NoReturn:
    print(f"错误: {message}", file=sys.stderr)
    raise SystemExit(1)


def load_env_file(path: Path) -> None:
    """Load KEY=VALUE pairs without overriding existing environment variables."""
    if not path.is_file():
        return

    for raw_line in path.read_text(encoding="utf-8-sig").splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("export "):
            line = line[7:].strip()
        if "=" not in line:
            continue

        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        if key:
            os.environ.setdefault(key, value)


def to_image_value(value: str) -> str:
    """Convert a local image path to a data URI; keep URL/data URI unchanged."""
    if value.startswith(("http://", "https://")):
        return value
    if value.startswith("data:image/"):
        return value

    path = Path(value).expanduser()
    if not path.is_file():
        die(f"参考图不存在: {value}")

    mime, _ = mimetypes.guess_type(path.name)
    mime = (mime or "").lower()
    if mime == "image/jpg":
        mime = "image/jpeg"
    if mime not in ALLOWED_IMAGE_MIME:
        die(f"不支持的参考图格式: {value} ({mime or 'unknown'})")

    raw = path.read_bytes()
    if len(raw) > MAX_INPUT_BYTES:
        die(f"参考图超过 30MB: {value}")
    encoded = base64.b64encode(raw).decode("ascii")
    return f"data:{mime};base64,{encoded}"


@dataclass
class SceneItem:
    """One image entry in a Markdown scene document."""

    path: Path
    alt: str
    prompt: str
    line_no: int


def split_frontmatter(lines: list[str]) -> tuple[dict[str, str], list[str]]:
    """Parse a simple YAML frontmatter block without adding a PyYAML dependency."""
    if not lines or lines[0].strip() != "---":
        return {}, lines

    for index in range(1, len(lines)):
        if lines[index].strip() != "---":
            continue

        meta: dict[str, str] = {}
        for raw in lines[1:index]:
            if ":" not in raw:
                continue
            key, value = raw.split(":", 1)
            meta[key.strip().lower()] = value.strip().strip("'\"")
        return meta, lines[index + 1 :]

    return {}, lines


def resolve_scene_path(doc_path: Path, raw_path: str) -> Path:
    value = raw_path.strip().strip("<>").strip("'\"")
    if value.startswith(("http://", "https://", "data:")):
        die(f"文档中的图片路径必须是本地路径: {raw_path}")
    path = Path(value).expanduser()
    if not path.is_absolute():
        path = doc_path.parent / path
    return path.resolve()


def infer_output_format(path: Path) -> str:
    suffix = path.suffix.lower()
    if suffix in {".jpg", ".jpeg"}:
        return "jpeg"
    if suffix == ".png":
        return "png"
    die(f"文档图片只支持 .png/.jpg/.jpeg 输出: {path}")


def compose_prompt(style: str, prompt: str) -> str:
    style = style.strip()
    prompt = prompt.strip()
    if not style or prompt.startswith(style):
        return prompt
    return f"{style}。\n{prompt}"


def parse_scene_doc(doc_path: Path) -> tuple[str, list[SceneItem]]:
    text = doc_path.read_text(encoding="utf-8-sig")
    lines = text.splitlines()
    meta, body = split_frontmatter(lines)
    style = meta.get("style", "").strip()

    items: list[SceneItem] = []
    current: SceneItem | None = None
    prompt_lines: list[str] = []

    for line_no, raw_line in enumerate(body, start=1):
        line = raw_line.strip()
        match = next((pattern.match(line) for pattern in SCENE_PATTERNS if pattern.match(line)), None)
        if match:
            if current is not None:
                current.prompt = "\n".join(prompt_lines).strip()
            current = SceneItem(
                path=resolve_scene_path(doc_path, match.group("path")),
                alt=match.group("alt").strip(),
                prompt="",
                line_no=line_no,
            )
            items.append(current)
            prompt_lines = []
        elif current is not None:
            prompt_lines.append(raw_line)

    if current is not None:
        current.prompt = "\n".join(prompt_lines).strip()

    return style, items


def resolve_size(size: str, ratio: str | None) -> str:
    value = size.strip()
    if not ratio:
        return value

    key = value.upper()
    if "x" in key:
        die("--size 已经是宽x高时，不要同时指定 --ratio")
    if key not in RATIO_SIZES:
        die(f"--ratio 只能配合 1K / 1.5K / 2K 使用，当前 size={value}")

    width, height = RATIO_SIZES[key][ratio]
    return f"{width}x{height}"


def build_payload(
    args: argparse.Namespace,
    prompt: str | None = None,
    output_format: str | None = None,
    ratio: str | None = None,
) -> dict:
    images = [to_image_value(value) for value in args.image]
    if len(images) > MAX_INPUT_IMAGES:
        die(f"Seedream 5.0 flash 最多支持 {MAX_INPUT_IMAGES} 张参考图")

    effective_prompt = args.prompt if prompt is None else prompt
    if not effective_prompt:
        die("缺少 --prompt")
    effective_format = args.output_format if output_format is None else output_format
    effective_ratio = args.ratio if ratio is None else ratio

    payload: dict = {
        "model": args.model,
        "prompt": effective_prompt,
        "size": resolve_size(args.size, effective_ratio),
        "response_format": args.response_format,
        "output_format": effective_format,
        "stream": False,
        "watermark": args.watermark,
    }

    if images:
        payload["image"] = images[0] if len(images) == 1 else images
    if args.background:
        payload["background"] = args.background

    if args.background == "transparent" and effective_format != "png":
        die("background=transparent 时必须使用 --output-format png")
    if args.background == "transparent" and len(images) != 1:
        die("background=transparent 只支持单张参考图的图生图")

    return payload


def output_paths(output: Path, count: int) -> list[Path]:
    if output.suffix == "":
        output = output.with_suffix(".png")

    if count <= 1:
        return [output]

    return [
        output.with_name(f"{output.stem}_{index}{output.suffix}")
        for index in range(1, count + 1)
    ]


def save_item(item: dict, target: Path, response_format: str, timeout: int) -> bool:
    if item.get("error"):
        error = item["error"]
        print(f"跳过失败图片: {error.get('code')} {error.get('message')}", file=sys.stderr)
        return False

    target.parent.mkdir(parents=True, exist_ok=True)

    try:
        if response_format == "b64_json":
            encoded = item.get("b64_json")
            if not encoded:
                print("响应缺少 b64_json", file=sys.stderr)
                return False
            target.write_bytes(base64.b64decode(encoded))
        else:
            url = item.get("url")
            if not url:
                print("响应缺少 url", file=sys.stderr)
                return False
            response = requests.get(url, timeout=timeout)
            response.raise_for_status()
            target.write_bytes(response.content)
    except Exception as exc:  # noqa: BLE001 - report the failing item and continue
        print(f"保存失败 {target}: {exc}", file=sys.stderr)
        return False

    size = item.get("size", "?")
    fmt = item.get("output_format", "?")
    print(f"已保存: {target} ({size}, {fmt})")
    return True


def require_api_key(env_file: Path) -> str:
    load_env_file(env_file)
    api_key = (os.environ.get("ARK_API_KEY") or "").strip()
    if api_key in PLACEHOLDER_KEYS:
        die(
            f"未配置 ARK_API_KEY。请编辑 {env_file} 填入真实 Key，"
            "或设置同名环境变量"
        )
    return api_key


def post_generation(args: argparse.Namespace, api_key: str, payload: dict) -> dict:
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }

    try:
        response = requests.post(args.base_url, headers=headers, json=payload, timeout=args.timeout)
    except requests.RequestException as exc:
        raise RuntimeError(f"请求失败: {exc}") from exc

    if response.status_code >= 400:
        detail = response.text[:2000]
        if response.status_code == 401:
            hint = "检查 ARK_API_KEY 是否有效"
        elif response.status_code == 429:
            hint = "触发限流，稍后重试或降低并发"
        elif response.status_code == 400:
            hint = "检查 size、参考图数量/格式、透明背景等参数"
        else:
            hint = "查看响应体中的错误码与 message"
        raise RuntimeError(f"HTTP {response.status_code}: {detail}\n提示: {hint}")

    try:
        result = response.json()
    except ValueError as exc:
        raise RuntimeError(f"响应不是 JSON: {response.text[:2000]}") from exc

    if result.get("error"):
        error = result["error"]
        raise RuntimeError(f"API 错误: {error.get('code')} {error.get('message')}")
    if not result.get("data"):
        raise RuntimeError("响应中没有 data 图片")

    return result


def run_doc(args: argparse.Namespace) -> int:
    if "x" in args.size.lower():
        die("--doc 模式不要用 宽x高 指定比例；请使用 1K / 1.5K / 2K 档位")

    doc_path = args.doc.expanduser().resolve()
    if not doc_path.is_file():
        die(f"文档不存在: {doc_path}")

    style, items = parse_scene_doc(doc_path)
    if not items:
        die(f"文档中没有找到 ![场景简介](./scene-01.jpg) 格式的图片条目: {doc_path}")

    print(f"文档: {doc_path}")
    print(f"全局风格: {style or '（未设置）'}")
    print(f"图片条目: {len(items)}")

    existing = 0
    missing = 0
    invalid = 0
    pending: list[tuple[SceneItem, str]] = []
    for item in items:
        relative = item.path.relative_to(doc_path.parent) if item.path.is_relative_to(doc_path.parent) else item.path
        if item.path.is_file() and item.path.stat().st_size > 0:
            print(f"[跳过] {relative}（已存在）")
            existing += 1
            continue

        missing += 1
        prompt = item.prompt.strip() or item.alt.strip()
        if not prompt:
            print(f"[失败] {relative}（缺少提示词）", file=sys.stderr)
            invalid += 1
            continue

        print(f"[待生成] {relative}（{infer_output_format(item.path)}）")
        pending.append((item, compose_prompt(style, prompt)))

    if args.dry_run:
        print(f"预检完成：已存在 {existing}，待生成 {missing}，缺少提示词 {invalid}")
        return 0

    if not pending:
        if invalid:
            print(f"没有可生成的条目：缺少提示词 {invalid}。", file=sys.stderr)
            return 1
        print("全部图片已存在，无需生成。")
        return 0

    api_key = require_api_key(args.env_file)
    generated = 0
    failed = invalid
    for item, prompt in pending:
        relative = item.path.relative_to(doc_path.parent) if item.path.is_relative_to(doc_path.parent) else item.path

        try:
            payload = build_payload(
                args,
                prompt=prompt,
                output_format=infer_output_format(item.path),
                ratio=None,
            )
            result = post_generation(args, api_key, payload)
            data = result.get("data") or []
            if len(data) > 1:
                print(f"[提示] {relative} 返回了 {len(data)} 张，只保存第一张", file=sys.stderr)
            if save_item(data[0], item.path, args.response_format, args.timeout):
                generated += 1
            else:
                failed += 1
        except Exception as exc:  # noqa: BLE001 - one failed scene should not stop the batch
            print(f"[失败] {relative}: {exc}", file=sys.stderr)
            failed += 1

    print(f"完成：已存在 {existing}，生成 {generated}，失败 {failed}。")
    return 1 if failed else 0


def main() -> int:
    parser = argparse.ArgumentParser(
        description="调用火山方舟 Doubao Seedream 5.0 Flash 生成/编辑图片"
    )
    parser.add_argument("--doc", type=Path, help="Markdown 场景文档：解析条目，跳过已存在图片，仅生成缺失项")
    parser.add_argument("--prompt", help="生成或编辑提示词（单图模式必填）")
    parser.add_argument(
        "--image",
        action="append",
        default=[],
        help="参考图路径、URL 或 data:image/...；可重复，最多 10 张",
    )
    parser.add_argument("--output", type=Path, help="输出文件路径（单图模式必填）")
    parser.add_argument("--size", default="2K", help="1K / 1.5K / 2K 或 宽x高，默认 2K")
    parser.add_argument(
        "--ratio",
        choices=SUPPORTED_RATIOS,
        default=None,
        help="画面比例：1:1 / 4:3 / 3:4 / 16:9 / 9:16 / 21:9；--doc 模式不要使用",
    )
    parser.add_argument(
        "--output-format",
        default="png",
        choices=["png", "jpeg"],
        help="输出格式，默认 png",
    )
    parser.add_argument(
        "--response-format",
        default="url",
        choices=["url", "b64_json"],
        help="url 返回 24 小时链接；b64_json 直接返回 Base64",
    )
    parser.add_argument(
        "--background",
        choices=["opaque", "transparent"],
        default=None,
        help="透明背景仅支持单张参考图的图生图，且输出需为 png",
    )
    parser.add_argument("--watermark", action="store_true", help="添加 AI 生成水印")
    parser.add_argument("--dry-run", action="store_true", help="只预检 --doc 文档，不调用 API")
    parser.add_argument("--model", default=DEFAULT_MODEL, help="模型 ID")
    parser.add_argument("--base-url", default=DEFAULT_ENDPOINT, help="API 地址")
    parser.add_argument("--timeout", type=int, default=180, help="请求超时秒数")
    parser.add_argument(
        "--env-file",
        type=Path,
        default=DEFAULT_ENV_FILE,
        help="含 ARK_API_KEY 的 .env 文件，默认使用技能目录下的 .env",
    )
    args = parser.parse_args()

    if args.doc:
        if args.prompt or args.output or args.image or args.ratio:
            die("--doc 不能与 --prompt / --output / --image / --ratio 同时使用")
        if args.background:
            die("--doc 模式不支持 --background")
        return run_doc(args)

    if args.dry_run:
        die("--dry-run 只能配合 --doc 使用")
    if not args.prompt or not args.output:
        die("单图模式需要 --prompt 和 --output；批量模式请使用 --doc")

    api_key = require_api_key(args.env_file)
    payload = build_payload(args)
    try:
        result = post_generation(args, api_key, payload)
    except RuntimeError as exc:
        die(str(exc))

    items = result.get("data") or []
    paths = output_paths(args.output, len(items))
    saved = 0
    for item, target in zip(items, paths):
        if save_item(item, target, args.response_format, args.timeout):
            saved += 1

    usage = result.get("usage")
    if usage:
        print("usage:", json.dumps(usage, ensure_ascii=False))

    if saved == 0:
        die("没有任何图片保存成功")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

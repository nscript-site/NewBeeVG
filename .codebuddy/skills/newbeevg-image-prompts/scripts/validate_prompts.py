#!/usr/bin/env python3
"""Validate a generated prompts.md for NewBeeVG image assets.

Expected format:

    ---
    style: 金融史口播风格插图
    ---

    ![古代华尔街的木墙](./wall.jpg)

    1653年新阿姆斯特丹，华尔街原始防御木栅栏...

Exit codes: 0 = valid, 1 = validation error.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

ENTRY_RE = re.compile(r"^!\[(?P<alt>[^\]]+)\]\((?P<path>[^)]+)\)\s*$")
ALLOWED_SUFFIXES = {".jpg", ".jpeg", ".png"}


def fail(message: str) -> None:
    print(f"错误: {message}", file=sys.stderr)
    raise SystemExit(1)


def parse_frontmatter(lines: list[str]) -> tuple[dict[str, str], int]:
    if not lines or lines[0].strip() != "---":
        fail("文件开头缺少 YAML frontmatter（---）")

    meta: dict[str, str] = {}
    end = None
    for index in range(1, len(lines)):
        line = lines[index].strip()
        if line == "---":
            end = index
            break
        if ":" in line:
            key, value = line.split(":", 1)
            meta[key.strip()] = value.strip().strip("\"'")

    if end is None:
        fail("frontmatter 没有结束标记 ---")
    return meta, end + 1


def parse_entries(lines: list[str], start: int) -> list[dict]:
    entries: list[dict] = []
    current: dict | None = None
    prompt_lines: list[str] = []

    def flush() -> None:
        nonlocal current, prompt_lines
        if current is None:
            return
        prompt = " ".join(line.strip() for line in prompt_lines if line.strip())
        current["prompt"] = prompt
        entries.append(current)
        current = None
        prompt_lines = []

    for line in lines[start:]:
        match = ENTRY_RE.match(line.strip())
        if match:
            flush()
            current = {
                "alt": match.group("alt").strip(),
                "path": match.group("path").strip(),
                "prompt": "",
            }
        elif current is not None:
            prompt_lines.append(line)

    flush()
    return entries


def validate(entries: list[dict], meta: dict[str, str], check_images: bool, base_dir: Path) -> list[str]:
    errors: list[str] = []
    style = meta.get("style", "").strip()
    if not style:
        errors.append("frontmatter 缺少非空 style")

    if not entries:
        errors.append("没有找到任何标准 Markdown 图片条目 ![简介](./文件名.jpg)；不要使用 !(简介)[./文件名]")
        return errors

    seen: set[str] = set()
    for index, entry in enumerate(entries, start=1):
        label = f"第 {index} 个条目"
        if not entry["alt"]:
            errors.append(f"{label}: 简介为空")
        if not entry["prompt"]:
            errors.append(f"{label}: 缺少提示词")
        elif len(entry["prompt"]) < 20:
            errors.append(f"{label}: 提示词过短（{len(entry['prompt'])} 字）")

        value = entry["path"]
        if not value.startswith("./"):
            errors.append(f"{label}: 路径必须以 ./ 开头，当前为 {value}")
            continue

        relative = value[2:]
        if "/" in relative or "\\" in relative:
            errors.append(f"{label}: 文件必须位于 prompts.md 同目录，当前为 {value}")
            continue

        suffix = Path(relative).suffix.lower()
        if suffix not in ALLOWED_SUFFIXES:
            errors.append(f"{label}: 后缀必须是 .jpg/.jpeg/.png，当前为 {value}")

        key = relative.lower()
        if key in seen:
            errors.append(f"{label}: 文件名重复 {value}")
        seen.add(key)

        if check_images and not (base_dir / relative).is_file():
            errors.append(f"{label}: 图片文件尚不存在 {value}")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description="校验 NewBeeVG prompts.md 格式")
    parser.add_argument("file", type=Path, help="prompts.md 路径")
    parser.add_argument("--json", action="store_true", help="输出 JSON")
    parser.add_argument(
        "--check-images",
        action="store_true",
        help="同时检查 ![简介](./xxx.jpg) 引用的图片是否存在",
    )
    args = parser.parse_args()

    if not args.file.is_file():
        fail(f"文件不存在: {args.file}")

    lines = args.file.read_text(encoding="utf-8-sig").splitlines()
    meta, start = parse_frontmatter(lines)
    entries = parse_entries(lines, start)
    errors = validate(entries, meta, args.check_images, args.file.parent)

    result = {
        "file": str(args.file),
        "style": meta.get("style", ""),
        "entries": len(entries),
        "files": [entry["path"] for entry in entries],
        "errors": errors,
        "valid": not errors,
    }

    if args.json:
        print(json.dumps(result, ensure_ascii=False, indent=2))
    else:
        print(f"File:  {args.file}")
        print(f"Style: {result['style'] or '(missing)'}")
        print(f"Items: {len(entries)}")
        for entry in entries:
            print(f"  {entry['path']}  {entry['alt']}")
        for error in errors:
            print(f"Error: {error}")

    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""Inspect APNG metadata without third-party dependencies.

Reports extension, canvas size, frame count, alpha, total duration, loop
count and structural warnings. Exit codes: 0 = animated APNG, 2 = static PNG,
1 = invalid/read error, 3 = --require-png failed.
"""

from __future__ import annotations

import argparse
import json
import struct
import sys
from pathlib import Path

PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"

COLOR_TYPES = {
    0: "grayscale",
    2: "RGB",
    3: "palette",
    4: "grayscale + alpha",
    6: "RGBA",
}


def read_chunks(data: bytes):
    if not data.startswith(PNG_SIGNATURE):
        raise ValueError("not a PNG file (bad signature)")

    pos = 8
    while pos + 8 <= len(data):
        length = struct.unpack(">I", data[pos : pos + 4])[0]
        chunk_type = data[pos + 4 : pos + 8].decode("latin-1")
        start = pos + 8
        end = start + length
        if end + 4 > len(data):
            raise ValueError(f"truncated chunk {chunk_type!r}")
        yield chunk_type, data[start:end]
        pos = end + 4


def analyze(path: Path) -> dict:
    data = path.read_bytes()
    result = {
        "file": str(path),
        "extension": path.suffix.lower(),
        "png_extension": path.suffix.lower() == ".png",
        "size_bytes": len(data),
        "valid_png": False,
        "is_apng": False,
        "animated": False,
        "width": None,
        "height": None,
        "bit_depth": None,
        "color_type": None,
        "color_type_name": None,
        "alpha": False,
        "frames": 0,
        "declared_frames": 0,
        "fctl_count": 0,
        "fdat_count": 0,
        "duration_seconds": 0.0,
        "loop_count": None,
        "loop": None,
        "warnings": [],
    }

    seen_iend = False
    seen_idat = False
    seen_trns = False
    delays = []

    for chunk_type, payload in read_chunks(data):
        if chunk_type == "IHDR":
            if len(payload) < 13:
                raise ValueError("invalid IHDR")
            width, height, bit_depth, color_type = struct.unpack(">IIBB", payload[:10])
            result.update(
                width=width,
                height=height,
                bit_depth=bit_depth,
                color_type=color_type,
                color_type_name=COLOR_TYPES.get(color_type, f"unknown ({color_type})"),
            )
        elif chunk_type == "acTL":
            if len(payload) < 8:
                raise ValueError("invalid acTL")
            declared_frames, loop_count = struct.unpack(">II", payload[:8])
            result.update(
                is_apng=True,
                declared_frames=declared_frames,
                frames=declared_frames,
                loop_count=loop_count,
                loop=(
                    "infinite"
                    if loop_count == 0
                    else "once"
                    if loop_count == 1
                    else f"{loop_count} times"
                ),
            )
        elif chunk_type == "fcTL":
            if len(payload) < 26:
                raise ValueError("invalid fcTL")
            _, _, _, _, _, delay_num, delay_den, _, _ = struct.unpack(">IIIIIHHBB", payload[:26])
            result["fctl_count"] += 1
            delays.append(delay_num / (delay_den or 100))
        elif chunk_type == "fdAT":
            result["fdat_count"] += 1
        elif chunk_type == "IDAT":
            seen_idat = True
        elif chunk_type == "tRNS":
            seen_trns = True
        elif chunk_type == "IEND":
            seen_iend = True
            break

    if result["width"] is None:
        raise ValueError("missing IHDR")

    result["valid_png"] = seen_iend and seen_idat
    if not seen_iend:
        result["warnings"].append("missing IEND (file may be truncated)")
    if not seen_idat:
        result["warnings"].append("missing IDAT (no image data)")

    color_type = result["color_type"]
    result["alpha"] = color_type in (4, 6) or (color_type in (0, 2, 3) and seen_trns)

    if not result["png_extension"]:
        result["warnings"].append(
            f"file extension is {result['extension']!r}; "
            "rename the final asset to .png for preview compatibility"
        )

    if result["is_apng"]:
        result["duration_seconds"] = round(sum(delays), 6)
        result["animated"] = result["declared_frames"] >= 2
        if result["fctl_count"] != result["declared_frames"]:
            result["warnings"].append(
                f"declared {result['declared_frames']} frames but found "
                f"{result['fctl_count']} fcTL chunks"
            )
        if not delays:
            result["warnings"].append("no fcTL delays; duration is unknown")
        if result["declared_frames"] < 2:
            result["warnings"].append("APNG has fewer than 2 frames")
    else:
        result["frames"] = 1
        result["warnings"].append("not an APNG (missing acTL); it is a static PNG")

    return result


def print_human(result: dict) -> None:
    size_kb = result["size_bytes"] / 1024
    print(f"File:     {result['file']}")
    print(f"Ext:      {result['extension'] or '(none)'}")
    print(f"APNG:     {'yes' if result['is_apng'] else 'no'}")
    print(f"Canvas:   {result['width']} x {result['height']}")
    print(f"Frames:   {result['frames']} (fcTL: {result['fctl_count']}, fdAT: {result['fdat_count']})")
    print(f"Duration: {result['duration_seconds']:.3f} s")
    print(f"Loop:     {result['loop'] if result['loop'] is not None else 'n/a'}")
    print(
        "Alpha:    "
        f"{'yes' if result['alpha'] else 'no'} "
        f"({result['color_type_name']}, {result['bit_depth']} bit)"
    )
    print(f"Size:     {size_kb:.1f} KB")
    for warning in result["warnings"]:
        print(f"Warning:  {warning}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspect APNG/PNG metadata.")
    parser.add_argument("file", type=Path, help="APNG/PNG file; final assets should use .png")
    parser.add_argument("--json", action="store_true", help="print JSON instead of text")
    parser.add_argument(
        "--require-png",
        action="store_true",
        help="fail (exit 3) when the final file does not use the .png extension",
    )
    args = parser.parse_args()

    try:
        result = analyze(args.file)
    except Exception as exc:  # noqa: BLE001 - CLI should report any read/parse failure
        if args.json:
            print(json.dumps({"file": str(args.file), "error": str(exc)}, ensure_ascii=False))
        else:
            print(f"Error: {exc}", file=sys.stderr)
        return 1

    if args.json:
        print(json.dumps(result, ensure_ascii=False, indent=2))
    else:
        print_human(result)

    if args.require_png and not result["png_extension"]:
        if not args.json:
            print(
                "Error: final APNG asset must use the .png extension "
                f"(got {result['extension'] or '(none)'})",
                file=sys.stderr,
            )
        return 3
    if not result["valid_png"]:
        return 1
    if not result["is_apng"]:
        return 2
    return 0 if result["animated"] else 2


if __name__ == "__main__":
    raise SystemExit(main())

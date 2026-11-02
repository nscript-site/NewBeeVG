"""
豆包发音人（音色）查询（doubao-voice）脚本

在本地音色库快照（data/voices.json，444 个官方音色）里搜索/筛选发音人，
拿到可直接用于 speech.py / speech_create.py / podcast.py 的 voice_type。

纯标准库，无需 API Key，不联网。
音色库刷新见 refresh_voices.py（走官方 ListSpeakers 接口，需要 AKSK）。

用法：
    python3 scripts/list_voices.py                          # 热门音色前 20
    python3 scripts/list_voices.py -q 思思                   # 按关键词搜（名称/描述/ID/标签/分类）
    python3 scripts/list_voices.py -c 客服场景 -g 女
    python3 scripts/list_voices.py -l en --limit 30
    python3 scripts/list_voices.py --id zh_female_vv_uranus_bigtts
    python3 scripts/list_voices.py --stats                  # 看分类/语言分布
    python3 scripts/list_voices.py -c 有声阅读 --table       # 人读的表格
"""

import os
import sys
import json
import argparse

HERE = os.path.dirname(os.path.abspath(__file__))
CATALOG = os.path.join(HERE, "..", "data", "voices.json")

# 主分类顺序（用于 --stats 与主分类判定）
MAIN_CATEGORIES = ["通用场景", "视频配音", "有声阅读", "角色扮演", "客服场景", "教学场景", "外语音色"]


def load_catalog(path: str = CATALOG) -> dict:
    if not os.path.isfile(path):
        raise FileNotFoundError(
            f"未找到音色库文件 {path}；可用 scripts/refresh_voices.py 重新拉取，"
            "或从技能包中恢复 data/voices.json"
        )
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    if isinstance(data, list):  # 兼容裸数组格式
        return {"_meta": {"count": len(data)}, "voices": data}
    return data


def main_category(v: dict) -> str:
    cats = v.get("categories") or []
    for c in MAIN_CATEGORIES:
        if c in cats:
            return c
    return cats[0] if cats else "未分类"


def match(v: dict, args) -> bool:
    if args.gender and (v.get("gender") or "") != args.gender:
        return False
    if args.age and (v.get("age") or "") != args.age:
        return False
    if args.resource and (v.get("resource") or "") != args.resource:
        return False
    if args.category:
        cats = v.get("categories") or []
        if not any(args.category in c for c in cats):
            return False
    if args.lang:
        langs = v.get("languages") or []
        if not any(l.lower().startswith(args.lang.lower()) for l in langs):
            return False
    if args.label:
        labels = v.get("labels") or []
        if not any(args.label in l for l in labels):
            return False
    for kw in args.search or []:
        blob = " ".join([
            v.get("name") or "", v.get("voice_type") or "", v.get("description") or "",
            " ".join(v.get("categories") or []), " ".join(v.get("labels") or []),
            " ".join(v.get("emotions") or []),
        ])
        if kw.lower() not in blob.lower():
            return False
    return True


def pick(v: dict) -> dict:
    return {
        "voice_type": v.get("voice_type"),
        "name": v.get("name"),
        "gender": v.get("gender"),
        "age": v.get("age"),
        "resource": v.get("resource"),
        "category": main_category(v),
        "categories": v.get("categories") or [],
        "languages": v.get("languages") or [],
        "labels": v.get("labels") or [],
        "emotions": v.get("emotions") or [],
        "description": v.get("description") or "",
        "heat": v.get("heat") or 0,
        "trial": v.get("trial") or "",
    }


def cjk_width(s: str) -> int:
    """按终端宽度估算字符串长度（中文算 2）。"""
    return sum(2 if ord(ch) > 0x2E7F else 1 for ch in s)


def pad(s: str, width: int) -> str:
    s = s or ""
    w = cjk_width(s)
    if w > width:
        out = ""
        for ch in s:
            if cjk_width(out + ch) > width - 1:
                break
            out += ch
        return out + "…"
    return s + " " * (width - w)


def print_table(rows: list, total: int, matched: int) -> None:
    print(f"音色库共 {total} 个，命中 {matched} 个，显示 {len(rows)} 个：\n")
    header = f"{'热度':>4}  {pad('名称', 22)}  {pad('voice_type', 40)}  {pad('性别/年龄', 10)}  {pad('主分类', 10)}  标签"
    print(header)
    print("-" * cjk_width(header))
    for v in rows:
        tag = "、".join(v["labels"])
        who = "%s/%s" % (v["gender"], v["age"])
        print(
            f"{v['heat']:>4}  {pad(v['name'], 22)}  {pad(v['voice_type'], 40)}  "
            f"{pad(who, 10)}  {pad(v['category'], 10)}  {tag}"
        )
    if matched > len(rows):
        print(f"\n（只显示前 {len(rows)} 个，用 --limit 0 或调大 --limit 看全部）")


def print_stats(catalog: dict) -> None:
    voices = catalog.get("voices", [])
    cats, langs, genders, ages, labels = {}, {}, {}, {}, {}
    for v in voices:
        for c in v.get("categories") or []:
            cats[c] = cats.get(c, 0) + 1
        for l in v.get("languages") or []:
            langs[l] = langs.get(l, 0) + 1
        for l in v.get("labels") or []:
            labels[l] = labels.get(l, 0) + 1
        genders[v.get("gender")] = genders.get(v.get("gender"), 0) + 1
        ages[v.get("age")] = ages.get(v.get("age"), 0) + 1

    def top(d, n=12):
        return [[k, v] for k, v in sorted(d.items(), key=lambda kv: -kv[1])[:n]]

    print(json.dumps({
        "success": True,
        "total": len(voices),
        "meta": catalog.get("_meta", {}),
        "by_category": dict(top(cats, 20)),
        "by_language": dict(top(langs, 15)),
        "by_gender": genders,
        "by_age": ages,
        "by_label": dict(top(labels, 10)),
    }, ensure_ascii=False, indent=2))


def main() -> None:
    p = argparse.ArgumentParser(
        description="查询本地豆包发音人库（无需 Key）：搜索、按场景/性别/语言筛选，拿到 voice_type。"
    )
    p.add_argument("-q", "--search", action="append", help="关键词（可重复，多个关键词取交集）；匹配名称/描述/ID/分类/标签/情感")
    p.add_argument("-c", "--category", help="分类（模糊匹配，如 客服 / 有声阅读 / 角色扮演 / 外语音色）")
    p.add_argument("-g", "--gender", help="性别：男 / 女")
    p.add_argument("-a", "--age", help="年龄：青年 / 中年 / 少年少女 / 儿童 / 老年")
    p.add_argument("-l", "--lang", help="语言前缀：zh / en / ja / ko / pt-br …（zh 可匹配 zh-cn）")
    p.add_argument("--label", help="特殊标签：豆包同款 / 抖音同款 / 剪映同款 / 猫箱同款 / 番茄小说同款")
    p.add_argument("-r", "--resource", help="模型资源 ID（快照内均为 seed-tts-2.0）")
    p.add_argument("--id", help="按 voice_type 精确/前缀查询（配合 --limit 0 可看多个）")
    p.add_argument("--sort", choices=["heat", "name", "id"], default="heat", help="排序，默认按热度降序")
    p.add_argument("--limit", type=int, default=20, help="返回条数，0=全部（默认 20）")
    p.add_argument("--table", action="store_true", help="输出人读表格（默认输出 JSON）")
    p.add_argument("--stats", action="store_true", help="只输出音色库统计概览")
    p.add_argument("--catalog", default=CATALOG, help="音色库文件路径（默认 data/voices.json）")
    args = p.parse_args()

    try:
        catalog = load_catalog(args.catalog)
        voices = [v for v in catalog.get("voices", []) if isinstance(v, dict)]

        if args.stats:
            print_stats(catalog)
            return

        if args.id:
            kw = args.id.lower()
            rows = [v for v in voices if kw in (v.get("voice_type") or "").lower()]
        else:
            rows = [v for v in voices if match(v, args)]

        if args.sort == "heat":
            rows.sort(key=lambda v: (-(v.get("heat") or 0), v.get("name") or ""))
        elif args.sort == "name":
            rows.sort(key=lambda v: (v.get("name") or ""))
        else:
            rows.sort(key=lambda v: (v.get("voice_type") or ""))

        matched = len(rows)
        limit = matched if args.limit == 0 else max(0, args.limit)
        shown = [pick(v) for v in rows[:limit]]

        if args.table:
            print_table(shown, len(voices), matched)
            return

        print(json.dumps({
            "success": True,
            "catalog_total": len(voices),
            "matched": matched,
            "returned": len(shown),
            "snapshot": (catalog.get("_meta") or {}).get("snapshot", ""),
            "voices": shown,
        }, ensure_ascii=False, indent=2))
    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()

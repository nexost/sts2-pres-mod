"""
Render docs/design/cards.json into readable tables and check it against the base-game benchmarks.

  python scripts/render_design.py

Writes docs/design/card_list.md and prints the validation report (also appended to that file).
"""
import json
import os
import re
from collections import Counter, defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "design", "cards.json")
OUT = os.path.join(ROOT, "docs", "design", "card_list.md")
RARITIES = ["Basic", "Common", "Uncommon", "Rare", "Ancient", "Token"]
TYPES = ["Attack", "Skill", "Power"]
ARCHES = ["Wall", "Deport", "Deals", "Tweets", "General"]
# Base game averages per character (build/analysis/benchmarks.md)
BASE_RARITY = {"Basic": 4, "Common": 20, "Uncommon": 36, "Rare": 26, "Ancient": 2}
BASE_TYPE_BY_RARITY = {"Common": (11.0, 9.0, 0.0), "Uncommon": (11.0, 16.4, 8.6), "Rare": (7.4, 9.0, 9.6)}


def show(text, upgraded=False):
    return re.sub(r"\{([^|}]*)\|([^}]*)\}", lambda m: m.group(2 if upgraded else 1), text)


def both(text):
    return re.sub(r"\{([^|}]*)\|([^}]*)\}", lambda m: m.group(1) if m.group(1) == m.group(2) else f"{m.group(1)} ({m.group(2)})", text)


def cost_str(c):
    cost = str(c["cost"])
    return f"{cost} ({c['cost_up']})" if "cost_up" in c else cost


def upgrade_str(c):
    parts = []
    if "cost_up" in c:
        parts.append(f"cost {c['cost']}→{c['cost_up']}")
    for a, b in re.findall(r"\{([^|}]*)\|([^}]*)\}", c["text"]):
        if a != b:
            parts.append(f"{a}→{b}")
    if c.get("up"):
        parts.append(c["up"])
    return ", ".join(parts)


def main():
    data = json.load(open(SRC, encoding="utf-8"))
    cards = data["cards"]
    lines = ["# The Donald: full card list", "",
             "Generated from `cards.json` by `scripts/render_design.py`. Numbers in parentheses are the upgraded values.", ""]
    for rarity in RARITIES:
        group = [c for c in cards if c["rarity"] == rarity]
        if not group:
            continue
        lines += [f"## {rarity} ({len(group)})", "", "| # | Card | Cost | Type | Style | Text | Upgrade |", "|---|---|---|---|---|---|---|"]
        order = {t: i for i, t in enumerate(TYPES)}
        for i, c in enumerate(sorted(group, key=lambda c: (order[c["type"]], ARCHES.index(c["arch"]), str(c["cost"]))), 1):
            kw = (" " + " ".join(f"*{k}.*" for k in c.get("kw", []))) if c.get("kw") else ""
            mp = " *(co-op only)*" if c.get("mp") else ""
            lines.append(f"| {i} | **{c['name']}** | {cost_str(c)} | {c['type']} | {c['arch']} | {both(c['text'])}{kw}{mp} | {upgrade_str(c)} |")
        lines.append("")

    lines += ["## By play style", ""]
    for arch in ARCHES:
        group = [c for c in cards if c["arch"] == arch and c["rarity"] != "Token"]
        by_r = Counter(c["rarity"] for c in group)
        names = ", ".join(c["name"] for c in sorted(group, key=lambda c: RARITIES.index(c["rarity"])))
        lines.append(f"- **{arch}** ({len(group)}: " + ", ".join(f"{by_r[r]} {r}" for r in RARITIES if by_r[r]) + f"): {names}")
    lines.append("")

    report = validate(cards)
    lines += ["## Validation", "", "```", *report, "```", ""]
    with open(OUT, "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines))
    print("\n".join(report))
    print("wrote", OUT)


def validate(cards):
    r = []
    pool = [c for c in cards if c["rarity"] != "Token"]
    rc = Counter(c["rarity"] for c in pool)
    r.append(f"Total pool cards: {len(pool)} (base game: 87-88)")
    for rarity, want in BASE_RARITY.items():
        r.append(f"  {rarity:9} {rc[rarity]:3}  base {want:3}  {'OK' if rc[rarity] == want else 'DIFF'}")
    tc = Counter(c["type"] for c in pool)
    r.append(f"Types: Attack {tc['Attack']}, Skill {tc['Skill']}, Power {tc['Power']}  (base avg 32 / 37 / 19)")
    for rarity, base in BASE_TYPE_BY_RARITY.items():
        t = Counter(c["type"] for c in pool if c["rarity"] == rarity)
        r.append(f"  {rarity:9} A/S/P {t['Attack']:2}/{t['Skill']:2}/{t['Power']:2}   base {base[0]:.0f}/{base[1]:.0f}/{base[2]:.0f}")

    solo = [c for c in pool if not c.get("mp")]
    sr = Counter(c["rarity"] for c in solo)
    r.append("Reward/shop rules (single-player pool, co-op-only cards excluded):")
    r.append(f"  >= 8 Commons (Room Full of Cheese): {sr['Common']}  {'OK' if sr['Common'] >= 8 else 'FAIL'}")
    for rarity in ("Common", "Uncommon", "Rare"):
        r.append(f"  >= 5 {rarity} (Sea Glass): {sr[rarity]}  {'OK' if sr[rarity] >= 5 else 'FAIL'}")
    r.append(f"  >= 3 Rares (boss rewards): {sr['Rare']}  {'OK' if sr['Rare'] >= 3 else 'FAIL'}")
    for t in TYPES:
        n = sum(1 for c in solo if c['type'] == t and c['rarity'] in ('Common', 'Uncommon', 'Rare'))
        need = 2 if t != "Power" else 1
        r.append(f"  shop needs {need}+ {t}s: {n}  {'OK' if n >= need else 'FAIL'}")

    r.append("Damage per energy, simple single-target attacks (base common median 9, p75 9):")
    for c in pool:
        if c["type"] != "Attack" or c["cost"] in ("X", 0) or c["target"] != "Enemy":
            continue
        m = re.match(r"Deal \{(\d+)\|(\d+)\} damage", c["text"])
        if m:
            dpe = int(m.group(1)) / c["cost"]
            flag = "  <-- high" if dpe > 10 and c["rarity"] in ("Common", "Basic") else ""
            r.append(f"  {c['rarity']:9} {c['name']:22} {dpe:4.1f}/E{flag}")
    r.append("Build per energy (Block benchmark: common median 6, p75 8; Wall persists so ~5-6/E is par):")
    for c in pool:
        m = re.search(r"Build \{(\d+)\|(\d+)\}", c["text"])
        if m and c["cost"] not in ("X", 0) and c["type"] == "Skill" and "Pay" not in c["text"]:
            r.append(f"  {c['rarity']:9} {c['name']:22} {int(m.group(1)) / c['cost']:4.1f}/E")
    return r


if __name__ == "__main__":
    main()

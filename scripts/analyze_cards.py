"""
Mine the base game's character card pools for balance benchmarks.

  python scripts/analyze_cards.py

Reads the decompiled code (re/code) and English text (re/pck/localization) and writes to build/analysis/
(git-ignored, since it's derived from the game's data):
  base_cards.json      every character card with cost/type/rarity/target/keywords/vars/upgrades/text
  cards_<char>.md      readable per-character card lists
  benchmarks.md        rarity/type/cost distributions and damage/block per energy by rarity
"""
import json
import os
import re
import statistics
from collections import Counter, defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODELS = os.path.join(ROOT, "re", "code", "MegaCrit", "sts2", "Core", "Models")
LOC = os.path.join(ROOT, "re", "pck", "localization", "eng")
OUT = os.path.join(ROOT, "build", "analysis")
CHARACTERS = ["Ironclad", "Silent", "Defect", "Regent", "Necrobinder"]
RARITIES = ["Basic", "Common", "Uncommon", "Rare", "Ancient"]
TYPES = ["Attack", "Skill", "Power"]


def slug(name):
    return re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", name).upper()


def read(path):
    with open(path, encoding="utf-8") as fh:
        return fh.read()


def parse_card(name, cards_loc):
    text = read(os.path.join(MODELS, "Cards", name + ".cs"))
    card = {"class": name, "id": slug(name)}
    m = re.search(r"base\(\s*(-?\d+)\s*,\s*CardType\.(\w+)\s*,\s*CardRarity\.(\w+)\s*,\s*TargetType\.(\w+)", text)
    if m:
        card.update(cost=int(m.group(1)), type=m.group(2), rarity=m.group(3), target=m.group(4))
    card["x_cost"] = "HasEnergyCostX => true" in text
    kw_block = re.search(r"CanonicalKeywords\s*=>(.*?);", text, re.S)
    card["keywords"] = sorted(set(re.findall(r"CardKeyword\.(\w+)", kw_block.group(1)))) if kw_block else []

    vars_ = {}
    for vm in re.finditer(r"new (\w+?)Var(?:<(\w+)>)?\(\s*(?:\"(\w+)\"\s*,\s*)?(-?[\d.]+)m?", text):
        kind, generic, custom, value = vm.groups()
        key = custom or (generic if kind == "Power" else kind)
        vars_[key] = float(value)
    card["vars"] = vars_

    upgrades = {}
    up = re.search(r"void OnUpgrade\(\)\s*\{(.*?)\n\t\}", text, re.S)
    if up:
        body = up.group(1)
        for um in re.finditer(r"DynamicVars(?:\.(\w+)|\[\"?(\w+)\"?\]|\[nameof\((\w+)\)\])\.UpgradeValueBy\((-?[\d.]+)m?\)", body):
            key = um.group(1) or um.group(2) or um.group(3)
            upgrades[key] = float(um.group(4))
        for um in re.finditer(r"EnergyCost\.UpgradeBy\((-?\d+)\)", body):
            upgrades["cost"] = int(um.group(1))
        upgrades.update({f"+{k}": True for k in re.findall(r"AddKeyword\(CardKeyword\.(\w+)\)", body)})
        upgrades.update({f"-{k}": True for k in re.findall(r"RemoveKeyword\(CardKeyword\.(\w+)\)", body)})
    card["upgrades"] = upgrades

    card["title"] = cards_loc.get(card["id"] + ".title", name)
    desc = cards_loc.get(card["id"] + ".description", "")
    card["text"] = render(desc, vars_)
    card["hits"] = vars_.get("Repeat", 1)
    return card


def render(desc, vars_):
    """Rough substitution of SmartFormat placeholders so the text is readable."""
    def sub(m):
        name = m.group(1)
        for k, v in vars_.items():
            if k.lower() == name.lower() or k.lower() == name.lower().replace("power", ""):
                return str(int(v)) if v == int(v) else str(v)
        return "{" + name + "}"
    desc = re.sub(r"\{(\w+)(?::[^{}]*(?:\{[^{}]*\}[^{}]*)*)?\}", sub, desc)
    desc = re.sub(r"\[/?\w+(?:=[^\]]*)?\]", "", desc)
    return desc.replace("\n", " ")


def pool_cards(character):
    text = read(os.path.join(MODELS, "CardPools", character + "CardPool.cs"))
    return re.findall(r"ModelDb\.Card<(\w+)>", text)


def main():
    os.makedirs(OUT, exist_ok=True)
    cards_loc = json.load(open(os.path.join(LOC, "cards.json"), encoding="utf-8"))
    data = {}
    for ch in CHARACTERS:
        data[ch] = [parse_card(n, cards_loc) for n in pool_cards(ch)]
    with open(os.path.join(OUT, "base_cards.json"), "w", encoding="utf-8") as fh:
        json.dump(data, fh, indent=1)

    for ch, cards in data.items():
        lines = [f"# {ch} ({len(cards)} cards)\n", "| Card | Cost | Type | Rarity | Kw | Text | Upgrade |", "|---|---|---|---|---|---|---|"]
        order = {r: i for i, r in enumerate(RARITIES)}
        for c in sorted(cards, key=lambda c: (order.get(c.get("rarity"), 9), c.get("type", ""), c.get("cost", 0))):
            cost = "X" if c["x_cost"] else c.get("cost")
            up = ", ".join(f"{k} {v:+g}" if not isinstance(v, bool) else k for k, v in c["upgrades"].items())
            lines.append(f"| {c['title']} | {cost} | {c.get('type')} | {c.get('rarity')} | {' '.join(c['keywords'])} | {c['text']} | {up} |")
        with open(os.path.join(OUT, f"cards_{ch.lower()}.md"), "w", encoding="utf-8") as fh:
            fh.write("\n".join(lines) + "\n")

    write_benchmarks(data)
    print("wrote", OUT)


def write_benchmarks(data):
    out = ["# Base game benchmarks (character pools)\n"]
    out.append("## Rarity x type per character\n")
    out.append("| Character | Total | " + " | ".join(RARITIES) + " | Attack | Skill | Power |")
    out.append("|---|---|" + "---|" * (len(RARITIES) + 3))
    for ch, cards in data.items():
        r = Counter(c.get("rarity") for c in cards)
        t = Counter(c.get("type") for c in cards)
        out.append(f"| {ch} | {len(cards)} | " + " | ".join(str(r[x]) for x in RARITIES) + f" | {t['Attack']} | {t['Skill']} | {t['Power']} |")

    allc = [c for cards in data.values() for c in cards]
    out.append("\n## Type split per rarity (all 5 characters, average per character)\n")
    out.append("| Rarity | Attack | Skill | Power |")
    out.append("|---|---|---|---|")
    for r in RARITIES:
        t = Counter(c.get("type") for c in allc if c.get("rarity") == r)
        out.append(f"| {r} | " + " | ".join(f"{t[x] / 5:.1f}" for x in TYPES) + " |")

    out.append("\n## Cost curve per rarity (average per character)\n")
    out.append("| Rarity | X | 0 | 1 | 2 | 3 | 4+ |")
    out.append("|---|---|---|---|---|---|---|")
    for r in RARITIES:
        cs = [c for c in allc if c.get("rarity") == r]
        x = sum(c["x_cost"] for c in cs)
        counts = Counter(min(c.get("cost", 0), 4) for c in cs if not c["x_cost"])
        out.append(f"| {r} | {x / 5:.1f} | " + " | ".join(f"{counts[i] / 5:.1f}" for i in range(5)) + " |")

    out.append("\n## Damage per energy, attacks with a Damage var (single target / AoE), cost >= 1\n")
    out.append("| Rarity | n | median dmg/E | p75 dmg/E | AoE n | AoE median dmg/E |")
    out.append("|---|---|---|---|---|---|")
    for r in RARITIES:
        st, aoe = [], []
        for c in allc:
            if c.get("rarity") != r or c.get("type") != "Attack" or "Damage" not in c["vars"] or c["x_cost"] or c.get("cost", 0) < 1:
                continue
            dpe = c["vars"]["Damage"] * c["hits"] / c["cost"]
            (aoe if c.get("target") == "AllEnemies" else st).append(dpe)
        fmt = lambda xs, f: f"{f(xs):.1f}" if xs else "-"
        p75 = lambda xs: sorted(xs)[int(len(xs) * 0.75)]
        out.append(f"| {r} | {len(st)} | {fmt(st, statistics.median)} | {fmt(st, p75)} | {len(aoe)} | {fmt(aoe, statistics.median)} |")

    out.append("\n## Block per energy, cards with a Block var, cost >= 1\n")
    out.append("| Rarity | n | median block/E | p75 block/E |")
    out.append("|---|---|---|---|")
    for r in RARITIES:
        xs = [c["vars"]["Block"] / c["cost"] for c in allc if c.get("rarity") == r and "Block" in c["vars"] and not c["x_cost"] and c.get("cost", 0) >= 1]
        if xs:
            out.append(f"| {r} | {len(xs)} | {statistics.median(xs):.1f} | {sorted(xs)[int(len(xs) * 0.75)]:.1f} |")

    out.append("\n## Upgrade patterns (share of cards)\n")
    ups = Counter()
    for c in allc:
        u = c["upgrades"]
        if "cost" in u:
            ups["cost -1"] += 1
        if any(k in u for k in ("Damage", "Block")):
            ups["damage/block up"] += 1
        for k in u:
            if k.startswith("+") or k.startswith("-"):
                ups[k] += 1
    for k, v in ups.most_common():
        out.append(f"- {k}: {v} ({v / len(allc):.0%})")

    out.append("\n## Keywords (cards per character, average)\n")
    kw = Counter(k for c in allc for k in c["keywords"])
    for k, v in kw.most_common():
        out.append(f"- {k}: {v / 5:.1f}")
    out.append("\n## Other vars used (cards per character, average)\n")
    vs = Counter(k for c in allc for k in c["vars"])
    for k, v in vs.most_common(30):
        out.append(f"- {k}: {v / 5:.1f}")
    with open(os.path.join(OUT, "benchmarks.md"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(out) + "\n")


if __name__ == "__main__":
    main()

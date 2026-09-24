"""
After a game update: does everything the mod relies on in the game still exist? Reads re/ (make it with
scripts/extract_game.py first) and the mod's own sources. docs/GAME_UPDATES.md explains what to do with each finding.

  python scripts/game_update_check.py

Checks:
  1. versions   the installed game, the game re/ was extracted from, and mod.json's min_game_version
  2. patches    every [HarmonyPatch(typeof(X), nameof(X.Y))] target: class X and member Y still exist
  3. by name    private fields, properties and methods the mod reaches by string (Traverse, AccessTools)
  4. ancients   the Ancients with character dialogue (AncientDialoguePatch) against the game's Ancients
  5. game files scenes and images the tools copy or use as art references (make_placeholders, art_recipes,
                every character's card_refs)
Exit code 1 if anything is missing.
"""
import glob
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

RE = os.path.join(presmod.REPO, "re")
CODE = os.path.join(RE, "code")
PCK = os.path.join(RE, "pck")
problems = []


def say(ok, text):
    print(("  ok    " if ok else "  MISSING ") + text)
    if not ok:
        problems.append(text)


def read(path):
    with open(path, encoding="utf-8", errors="replace") as f:
        return f.read()


def game_sources():
    """Class name -> file text for the decompiled game (the first file named after the class)."""
    classes = {}
    for path in glob.glob(os.path.join(CODE, "**", "*.cs"), recursive=True):
        classes.setdefault(os.path.basename(path)[:-3], path)
    return classes


def declared(text, member):
    """A member declaration (method, property, field or event) in a class body."""
    return re.search(rf"\b{re.escape(member)}\b\s*(\(|=>|\{{|;|=|<)", text) is not None


def main():
    if not os.path.isdir(CODE):
        sys.exit("re/code is missing: run python scripts/extract_game.py first")
    mod_sources = {p: read(p) for p in glob.glob(os.path.join(presmod.SRC_DIR, "**", "*.cs"), recursive=True)}
    mod_text = "\n".join(mod_sources.values())

    print("== Versions")
    installed = json.load(open(os.path.join(presmod.GAME_DIR, "release_info.json"), encoding="utf-8"))["version"]
    version_file = os.path.join(RE, "game_version.json")
    extracted = json.load(open(version_file, encoding="utf-8"))["version"] if os.path.exists(version_file) else "unknown"
    print(f"  installed game {installed}, re/ extracted from {extracted}, mod.json min_game_version {presmod.MOD['min_game_version']}")
    say(extracted == installed, f"re/ matches the installed game (else: python scripts/extract_game.py)")

    classes = game_sources()
    all_code = None

    print("\n== Harmony patch targets")
    targets = set(re.findall(r'\[HarmonyPatch\(typeof\((\w+)\),\s*(?:nameof\(\w+\.(\w+)\)|"(\w+)")', mod_text))
    for cls, member_nameof, member_str in sorted(targets):
        member = member_nameof or member_str
        path = classes.get(cls)
        if path is None:
            say(False, f"{cls}.{member}: class {cls} not found")
            continue
        say(declared(read(path), member), f"{cls}.{member}")

    print("\n== Members reached by name (Traverse / AccessTools)")
    names = set(re.findall(r'Traverse\.Create\([^)]*\)\.(?:Field|Property|Method)(?:<[^>]+>)?\("(\w+)"', mod_text))
    names |= set(re.findall(r'AccessTools\.\w+\([^,()]+,\s*"(\w+)"', mod_text))
    for name in sorted(names):
        if all_code is None:
            all_code = "\n".join(read(p) for p in classes.values())
        say(declared(all_code, name), name)

    print("\n== Ancients with character dialogue")
    patch = read(os.path.join(presmod.SRC_DIR, "Framework", "Patches", "AncientDialoguePatch.cs"))
    ours = set(re.findall(r'\["([A-Z_]+)"\]\s*=\s*new', patch))
    game = set()
    for path in classes.values():
        for cls in re.findall(r"class (\w+) : AncientEventModel", read(path)):
            if cls not in ("TheArchitect", "DeprecatedAncientEvent"):
                game.add(presmod.slug(cls).upper())
    for name in sorted(game | ours):
        if name in ours and name not in game:
            say(False, f"{name}: in AncientDialoguePatch but no longer in the game (remove it)")
        elif name in game and name not in ours:
            say(False, f"{name}: new Ancient without our dialogue pattern (add it, plus ancients.json lines per character)")
        else:
            say(True, name)

    print("\n== Game files the tools use")
    wanted = set()
    for script in ("make_placeholders.py", "art_recipes.py"):
        text = read(os.path.join(presmod.REPO, "scripts", script))
        wanted.update(re.findall(r'copy_scene\("([^"]+)"', text))
        # Game files only: the f-string templates ({cid}, {id}, ...) are our own output paths.
        wanted.update(p.replace("{n}", "1") for p in re.findall(r'"((?:images|animations)/[^"]+\.png)"', text)
                      if "{" not in p.replace("{n}", ""))
        wanted.update(f"images/packed/card_portraits/{r}.png" for r in re.findall(r'_card_ref\("([^"]+)"\)', text))
    for ch in presmod.characters():
        for refs in ch["art"].get("card_refs", {}).values():
            wanted.update(f"images/packed/card_portraits/{r}.png" for r in refs)
    for rel in sorted(wanted):
        say(os.path.exists(os.path.join(PCK, rel.replace("/", os.sep))), rel)

    print(f"\n{'All present.' if not problems else f'{len(problems)} problem(s):'}")
    for p in problems:
        print("  - " + p)
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()

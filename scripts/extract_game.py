"""
Step 1, repeatable: decompile the game's code into re/code and unpack its resource pack into re/pck. Both folders are
git-ignored because they are Mega Crit's; every PC makes its own. Run it once after cloning (docs/SETUP.md) and again
after every game update (docs/GAME_UPDATES.md).

  python scripts/extract_game.py            decompile + unpack (~1 min, ~3 GB); an older re/ is kept as re_<version>/
  python scripts/extract_game.py --code     only the C# (10 s): enough for the build's ID check and reading the code
  python scripts/extract_game.py --force    redo it even if re/ already matches the installed game

What uses re/:
  re/code  the game's C# (ilspycmd): read to write patches; build.py and new_character.py check class names against it
  re/pck   the game's assets as a Godot project (GDRE Tools): style references for the art, scenes copied by
           make_placeholders.py, localization and card data for analyze_cards.py

Needs ilspycmd (a dotnet tool) and GDRE Tools in tools/gdre/ (docs/SETUP.md). Writes re/game_version.json, a copy of the
game's release_info.json, so scripts/game_update_check.py can tell when re/ is out of date.
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

RE = os.path.join(presmod.REPO, "re")
GDRE = os.path.join(presmod.REPO, "tools", "gdre", "gdre_tools.exe")
VERSION_FILE = os.path.join(RE, "game_version.json")


def game_version():
    path = os.path.join(presmod.GAME_DIR, "release_info.json")
    if not os.path.exists(path):
        sys.exit(f"No release_info.json in {presmod.GAME_DIR}: set sts2_game_dir (docs/SETUP.md)")
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def extracted_version():
    if not os.path.exists(VERSION_FILE):
        return None
    with open(VERSION_FILE, encoding="utf-8") as f:
        return json.load(f).get("version")


def run(args, log):
    print("  $", " ".join(f'"{a}"' if " " in a else a for a in args), flush=True)
    with open(log, "w", encoding="utf-8") as out:
        code = subprocess.run(args, stdout=out, stderr=subprocess.STDOUT).returncode
    if code != 0:
        sys.exit(f"FAILED ({code}); see {log}")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--code", action="store_true", help="only decompile the C# (skip the 3 GB asset unpack)")
    ap.add_argument("--force", action="store_true", help="redo even if re/ matches the installed game")
    a = ap.parse_args()

    info = game_version()
    current, old = info["version"], extracted_version()
    print(f"Installed game: {current}   re/: {old or 'none'}")
    if old == current and not a.force:
        print("re/ already matches the installed game (use --force to redo).")
        return
    if os.path.isdir(RE) and os.listdir(RE):
        keep = os.path.join(presmod.REPO, f"re_{old or 'unknown'}")
        if os.path.exists(keep):
            keep += time.strftime("_%Y%m%d_%H%M%S")
        os.rename(RE, keep)
        print(f"Kept the previous extraction as {os.path.basename(keep)}/ (compare with it, then delete it)")
    os.makedirs(RE)

    data = os.path.join(presmod.GAME_DIR, "data_sts2_windows_x86_64")
    print("\n== Decompiling sts2.dll -> re/code")
    if shutil.which("ilspycmd") is None:
        sys.exit("ilspycmd not found: dotnet tool install -g ilspycmd (docs/SETUP.md)")
    run(["ilspycmd", "-p", "--nested-directories", "-o", os.path.join(RE, "code"), os.path.join(data, "sts2.dll")],
        os.path.join(RE, "ilspy.log"))
    if not a.code:
        print("\n== Unpacking SlayTheSpire2.pck -> re/pck")
        if not os.path.exists(GDRE):
            sys.exit(f"GDRE Tools not found at {GDRE} (docs/SETUP.md)")
        run([GDRE, "--headless", f"--recover={os.path.join(presmod.GAME_DIR, 'SlayTheSpire2.pck')}",
             f"--output={os.path.join(RE, 'pck')}"], os.path.join(RE, "gdre.log"))
    with open(VERSION_FILE, "w", encoding="utf-8") as f:
        json.dump(info, f, indent=2)
    print(f"\nDone: re/ is game {current}. Next: python scripts/game_update_check.py")


if __name__ == "__main__":
    main()

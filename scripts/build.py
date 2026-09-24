"""
One-command build of the whole mod (every character in characters/).

  python scripts/build.py              build into build/dist/
  python scripts/build.py --install    build, then install into the game (runs dist/install.ps1)
  python scripts/build.py --zip        build, then pack build/dist/ into build/release/sts2-pres-mod-v<version>.zip

Steps: placeholders -> model ID check -> C# build -> Godot import -> merge localization -> pack PCK ->
assemble dist/ (mod + installer). The mod's id, name and version come from mod.json.
"""
import argparse
import glob
import json
import os
import re
import shutil
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

ROOT = presmod.REPO
MOD = presmod.MOD_DIR
SRC = presmod.SRC_DIR
BUILD = os.path.join(ROOT, "build")
DIST = os.path.join(BUILD, "dist")
GAME_CODE = os.path.join(ROOT, "re", "code", "MegaCrit", "sts2", "Core", "Models")
GODOT = os.path.join(ROOT, "tools", "godot", "Godot_v4.5.1-stable_mono_win64", "Godot_v4.5.1-stable_mono_win64_console.exe")

MOD_ID = presmod.MOD_ID
MANIFEST = {
    "id": MOD_ID,
    "name": presmod.MOD["name"],
    "author": presmod.MOD["author"],
    "description": presmod.MOD["description"],
    "version": presmod.MOD["version"],
    "has_dll": True,
    "has_pck": True,
    "affects_gameplay": True,
    "min_game_version": presmod.MOD["min_game_version"],
    "dependencies": [],
}

# Files in mod/ that are never packed.
SKIP_DIRS = {".godot", "obj", "bin"}
SKIP_FILES = {"project.godot", "PresMod.csproj", "icon.svg"}
# Imported source assets: the PCK gets their .import remap + the imported file, not the source.
IMPORTED_EXT = {".png", ".jpg", ".jpeg", ".webp", ".svg", ".wav", ".ogg", ".mp3", ".ttf", ".otf"}
# Folders named "Nodes" hold Godot Node scripts (Framework/Nodes, Characters/<Name>/Nodes); the PCK needs a stub at
# each of their res:// paths (like the base game).
NODE_SCRIPT_DIR_NAME = "Nodes"


def step(name):
    print(f"\n== {name}", flush=True)


def run(cmd, **kw):
    print("  $", " ".join(f'"{c}"' if " " in c else c for c in cmd), flush=True)
    result = subprocess.run(cmd, **kw)
    if result.returncode != 0:
        sys.exit(f"FAILED ({result.returncode}): {cmd[0]}")
    return result


# ---------- model IDs ----------

def slugify(name):
    """Mirror of MegaCrit StringHelper.Slugify for plain class names."""
    s = re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", name.strip())
    s = re.sub(r"\s+", "_", s.upper())
    return re.sub(r"[^A-Z0-9_]", "", s)


def category_of(base):
    s = slugify(base)
    return s[: -len("_MODEL")] if s.endswith("_MODEL") else s


def our_models():
    classes = {}
    for dp, _, files in os.walk(SRC):
        for f in files:
            if f.endswith(".cs"):
                text = open(os.path.join(dp, f), encoding="utf-8").read()
                for m in re.finditer(r"^\s*public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)(?:\s*\([^)]*\))?\s*:\s*(\w+)", text, re.M):
                    abstract = "abstract" in m.group(0)
                    classes[m.group(1)] = (m.group(2), abstract)
    models = []
    for name, (base, abstract) in classes.items():
        if abstract:
            continue
        root = base
        while root in classes:
            root = classes[root][0]
        if root.endswith("Model") and root != "AbstractModel":
            models.append((name, f"{category_of(root)}.{slugify(name)}"))
    return sorted(models, key=lambda m: m[1])


def check_ids():
    step("Model ID check")
    models = our_models()
    # Model IDs come from class names alone: two characters can't both have a "Strike" (name it StrikeBiden).
    seen = {}
    for n, i in models:
        seen.setdefault(i, []).append(n)
    dupes = {i: ns for i, ns in seen.items() if len(ns) > 1}
    if dupes:
        sys.exit("Two classes with the same model ID: " + ", ".join(f"{i} ({', '.join(ns)})" for i, ns in dupes.items()))
    game_entries = set()
    if os.path.isdir(GAME_CODE):
        for dp, _, files in os.walk(GAME_CODE):
            game_entries.update(slugify(f[:-3]) for f in files if f.endswith(".cs"))
    else:
        print("  (re/code missing, skipping collision check; the mod still checks at runtime)")
    clashes = [(n, i) for n, i in models if i.split(".", 1)[1] in game_entries]
    for n, i in models:
        print(f"  {i:<40} {n}")
    if clashes:
        sys.exit("ID collision with base game: " + ", ".join(f"{n} ({i})" for n, i in clashes))
    return [i for _, i in models]


# ---------- build ----------

def build_dll():
    step("C# build")
    run(["dotnet", "build", os.path.join(MOD, "PresMod.csproj"), "-c", "ExportRelease", "-nologo", "-v", "q"])
    return os.path.join(MOD, ".godot", "mono", "temp", "bin", "ExportRelease", f"{MOD_ID}.dll")


# Texture import settings that differ from Godot's default (lossless). Big paintings are stored as lossy WebP at 0.9:
# no visible difference, and the card portraits shrink from ~60 MB to ~8 MB. Figures keep lossless: lossy colour
# under their transparent pixels bleeds into the edges when scaled. Icons are small and need crisp edges.
LOSSY = {"compress/mode": "1", "compress/lossy_quality": "0.9"}
IMPORT_OVERRIDES = [
    ("images/packed/card_portraits/*/*.png", LOSSY),
    ("images/*/char_select_bg.png", LOSSY),
    ("images/ui/transitions/*_transition.png", LOSSY),
]


def apply_import_overrides():
    """Set IMPORT_OVERRIDES in the .import files; returns how many changed (they then need a re-import)."""
    changed = 0
    for pattern, params in IMPORT_OVERRIDES:
        for src in glob.glob(os.path.join(MOD, pattern)):
            path = src + ".import"
            if not os.path.exists(path):
                continue
            with open(path, encoding="utf-8") as f:
                text = f.read()
            new = text
            for key, value in params.items():
                new = re.sub(rf"^{re.escape(key)}=.*$", f"{key}={value}", new, flags=re.M)
            if new != text:
                with open(path, "w", encoding="utf-8", newline="\n") as f:
                    f.write(new)
                changed += 1
    return changed


def godot_import():
    step("Godot import")
    run([GODOT, "--headless", "--path", MOD, "--import"], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    # New images get Godot's default settings on their first import: set ours, then import those again.
    changed = apply_import_overrides()
    if changed:
        print(f"  import settings changed for {changed} image(s), re-importing")
        run([GODOT, "--headless", "--path", MOD, "--import"], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)


def pack_list():
    entries = []
    for dp, dirs, files in os.walk(MOD):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        rel_dir = os.path.relpath(dp, MOD).replace("\\", "/")
        rel_dir = "" if rel_dir == "." else rel_dir + "/"
        in_src = os.path.abspath(dp).startswith(os.path.abspath(SRC))
        for f in files:
            full = os.path.join(dp, f)
            rel = rel_dir + f
            ext = os.path.splitext(f)[1].lower()
            if f in SKIP_FILES or ext in IMPORTED_EXT:
                continue
            if in_src:
                if ext == ".cs" and os.path.basename(dp) == NODE_SCRIPT_DIR_NAME:
                    entries.append((rel, "STUB"))
                continue
            if ext == ".import":
                entries.append((rel, full))
                text = open(full, encoding="utf-8").read()
                for imported in set(re.findall(r'"res://(\.godot/imported/[^"]+)"', text)):
                    entries.append((imported, os.path.join(MOD, imported.replace("/", os.sep))))
                continue
            entries.append((rel, full))
    entries += merge_localization()
    return sorted(set(entries))


def merge_localization():
    """The game reads a mod's text from res://<mod id>/localization/<lang>/<table>.json, one file per table for the
    whole mod. Each character keeps its own in characters/<id>/localization/<lang>/; merge them here (a key defined
    twice is an error) and pack the merged files."""
    merged = {}
    for ch in presmod.characters():
        loc = ch.path("localization")
        if not os.path.isdir(loc):
            continue
        for lang in os.listdir(loc):
            for f in os.listdir(os.path.join(loc, lang)):
                if not f.endswith(".json"):
                    continue
                with open(os.path.join(loc, lang, f), encoding="utf-8") as fh:
                    table = json.load(fh)
                target = merged.setdefault((lang, f), {})
                clash = sorted(set(target) & set(table))
                if clash:
                    sys.exit(f"Localization key defined twice ({ch.id}, {lang}/{f}): {', '.join(clash[:5])}")
                target.update(table)
    out_dir = os.path.join(BUILD, "loc")
    shutil.rmtree(out_dir, ignore_errors=True)
    entries = []
    for (lang, f), table in sorted(merged.items()):
        path = os.path.join(out_dir, lang, f)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(table, fh, ensure_ascii=False, indent=1)
        entries.append((f"{MOD_ID}/localization/{lang}/{f}", path))
    print(f"  localization: {len(entries)} tables from {len(presmod.character_ids())} character(s)")
    return entries


def pack(pck_path):
    step("Pack PCK")
    entries = pack_list()
    stub = os.path.join(BUILD, "stub.cs")
    with open(stub, "w") as fh:
        fh.write("\n")
    list_path = os.path.join(BUILD, "pack_list.txt")
    with open(list_path, "w", encoding="utf-8") as fh:
        for rel, src in entries:
            if src != "STUB" and not os.path.exists(src):
                sys.exit(f"Missing file for PCK: {src}")
            fh.write(f"{rel}|{stub if src == 'STUB' else src}\n")
    print(f"  {len(entries)} files")
    run([GODOT, "--headless", "--path", MOD, "--script", os.path.join(ROOT, "scripts", "pack.gd"), "--",
         f"--list={list_path}", f"--out={pck_path}"], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    if not os.path.exists(pck_path):
        sys.exit("PCK was not written")


def assemble(dll, ids):
    step("Assemble dist/")
    mod_dir = os.path.join(DIST, MOD_ID)
    shutil.copy2(dll, os.path.join(mod_dir, f"{MOD_ID}.dll"))
    # The game treats every .json in a mod folder as a manifest, so this must stay the only one.
    with open(os.path.join(mod_dir, "manifest.json"), "w", encoding="utf-8") as fh:
        json.dump(MANIFEST, fh, indent=2)
    # Used by the uninstaller to find saves that reference this mod's content.
    with open(os.path.join(mod_dir, "mod_ids.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(ids) + "\n")
    # Earlier ids of this mod (mod.json legacy_ids): the installer removes those folders, or both copies would load.
    with open(os.path.join(DIST, "legacy_ids.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(presmod.MOD.get("legacy_ids", [])) + "\n")
    for f in ("install.ps1", "install.cmd", "uninstall.ps1", "uninstall.cmd"):
        shutil.copy2(os.path.join(ROOT, "scripts", "dist", f), os.path.join(DIST, f))
    # The player README gets the version stamped in, with Windows line endings for Notepad.
    with open(os.path.join(ROOT, "scripts", "dist", "README.txt"), encoding="utf-8") as fh:
        readme = fh.read().replace("{VERSION}", MANIFEST["version"]).replace("{GAME_VERSION}", MANIFEST["min_game_version"])
    with open(os.path.join(DIST, "README.txt"), "w", encoding="utf-8", newline="\r\n") as fh:
        fh.write(readme.replace("\r\n", "\n"))
    for f in sorted(os.listdir(mod_dir)):
        print(f"  {MOD_ID}/{f:<28} {os.path.getsize(os.path.join(mod_dir, f)):>10,} bytes")


def check_characters():
    """Every character has the scenes, localization tables and class the game loads by name."""
    step("Characters")
    problems = []
    for ch in presmod.characters():
        missing = presmod.missing_files(ch)
        problems += [f"{ch.id}: missing {m}" for m in missing]
        if presmod.slug(ch["class"]) != ch.id:
            problems.append(f"{ch.id}: the game will call class {ch['class']} '{presmod.slug(ch['class'])}', so its assets must use that id")
        todo = sum(json.dumps(json.load(open(ch.path("localization", "eng", t + ".json"), encoding="utf-8"))).count("TODO")
                   for t in presmod.LOC_TABLES if os.path.exists(ch.path("localization", "eng", t + ".json")))
        print(f"  {ch.id}: {ch['name']}" + (f"  ({todo} TODO texts left in localization)" if todo else ""))
    if problems:
        sys.exit("\n".join(problems) + "\n(scripts/new_character.py creates all of these for a new character)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--install", action="store_true", help="install into the game after building")
    ap.add_argument("--zip", action="store_true", help="also pack build/dist/ into build/release/sts2-pres-mod-v<version>.zip")
    args = ap.parse_args()

    shutil.rmtree(DIST, ignore_errors=True)
    os.makedirs(os.path.join(DIST, MOD_ID))

    step("Placeholders (only missing files)")
    run([sys.executable, os.path.join(ROOT, "scripts", "make_placeholders.py")])
    check_characters()
    ids = check_ids()
    dll = build_dll()
    godot_import()
    pack(os.path.join(DIST, MOD_ID, f"{MOD_ID}.pck"))
    assemble(dll, ids)
    print(f"\nBuild OK -> {DIST}")

    if args.zip:
        step("Release zip")
        release = os.path.join(BUILD, "release")
        os.makedirs(release, exist_ok=True)
        name = f"sts2-pres-mod-v{MANIFEST['version']}"
        # One folder inside the zip (named like the zip), so unzipping never scatters files.
        staging = os.path.join(release, name)
        shutil.rmtree(staging, ignore_errors=True)
        shutil.copytree(DIST, staging)
        archive = shutil.make_archive(staging, "zip", root_dir=release, base_dir=name)
        shutil.rmtree(staging)
        print(f"  {archive}  ({os.path.getsize(archive):,} bytes)")
        # Steam Workshop preview (docs/PUBLISHING.md): the first character's select painting, 16:9, under 1 MB.
        from PIL import Image
        painting = os.path.join(MOD, "images", presmod.character().id, "char_select_bg.png")
        if os.path.exists(painting):
            src = Image.open(painting).convert("RGB")
            w, h = src.size
            src.crop((w - int(h * 16 / 9) - 80, 0, w - 80, h)).resize((1280, 720), Image.LANCZOS).save(
                os.path.join(release, "workshop_preview.png"), optimize=True)
            print(f"  {os.path.join(release, 'workshop_preview.png')}")

    if args.install:
        step("Install")
        run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", os.path.join(DIST, "install.ps1"), "-Quiet"])


if __name__ == "__main__":
    main()

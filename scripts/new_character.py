"""
Create a new playable character: every file the game, the build and the tools need, so the character boots and plays
(Strike, Defend and a stub starter relic, placeholder art) before any of its design exists.

  python scripts/new_character.py biden --class Biden --name "Uncle Joe" --full-name "Joe Biden" --primary 2F6BD8
  python scripts/new_character.py biden ... --dry-run      list what would be written, write nothing

Options (only the id, --class, --name and --primary are needed; the rest have defaults):
  --full-name       used in art prompts ("Joe Biden"); default: the name
  --secondary       dark UI colour (default: a dark shade of primary); --accent (default B22234); --text (default FAF0D7)
  --color-word      colour in art prompts, e.g. "royal blue" (default: guessed from primary)
  --gender          masculine | feminine | neutral (default masculine): pronouns in texts, the game's gendered lines
  --hp, --gold      starting HP (default 75) and Gold (default 99)
  --starter CLASS   starter relic class (default <Class>Keepsake), --starter-name "Its Name"
  --sfx             base character whose sounds are borrowed (default ironclad)
  --from ID         existing character whose scenes are copied and recoloured (default: the first one)

What it writes (docs/ADDING_A_CHARACTER.md explains each):
  characters/<id>/            character.json, design/ (cards.json, design.md, compendium template), art/art_assets.json,
                              localization/eng/*.json (all 9 tables; ancients.json with every line the patches ask for)
  mod/pres_mod/src/Characters/<Class>/
                              the character class, card/relic/potion pools, Strike<Class>, Defend<Class>, the starter
                              relic, Dev/DevHarness.<Class>.cs (empty test kit)
  mod/scenes, mod/materials   combat body, shop, rest site, character select, energy counter + vfx, card trail,
                              transition: copied from --from with names replaced and warm colours re-hued to primary
  mod/images                  placeholder art (make_placeholders.py), replaced later by the art review tool

Templates: characters/_template/ ({{Token}} placeholders, listed in its README.md). Nothing existing is overwritten.
"""
import argparse
import colorsys
import datetime
import json
import os
import re
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

TEMPLATE = os.path.join(presmod.CHARACTERS_DIR, "_template")
GAME_MODELS = os.path.join(presmod.REPO, "re", "code", "MegaCrit", "sts2", "Core", "Models")
BASE_CHARACTERS = ["ironclad", "silent", "defect", "necrobinder", "regent"]

PRONOUNS = {
    "masculine": ("Masculine", "he", "him", "his", "his"),
    "feminine": ("Feminine", "she", "her", "hers", "her"),
    "neutral": ("Neutral", "they", "them", "theirs", "their"),
}

# Hue (0-1) -> a word for art prompts and the Colorful Philosophers option.
HUE_WORDS = [(0.03, "red"), (0.09, "orange"), (0.15, "gold"), (0.19, "yellow"), (0.45, "green"), (0.53, "teal"),
             (0.70, "blue"), (0.83, "purple"), (0.95, "pink"), (1.01, "red")]


# ---------------------------------------------------------------------------------------------------------------
# Colours

def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def rgb_hex(rgb):
    return "".join(f"{round(max(0.0, min(1.0, c)) * 255):02X}" for c in rgb)


def shade(h, v_scale, s_scale=1.0):
    hh, s, v = colorsys.rgb_to_hsv(*hex_rgb(h))
    return rgb_hex(colorsys.hsv_to_rgb(hh, min(1.0, s * s_scale), v * v_scale))


def hue_word(h):
    hh, s, v = colorsys.rgb_to_hsv(*hex_rgb(h))
    if s < 0.2:
        return "silver" if v > 0.5 else "black"
    return next(word for limit, word in HUE_WORDS if hh < limit)


def vfx_color(h):
    """Closest VfxColor for the speech bubble."""
    word = hue_word(h)
    return {"red": "Red", "orange": "Orange", "gold": "Gold", "yellow": "Gold", "green": "Green", "teal": "Cyan",
            "blue": "Blue", "purple": "Purple", "pink": "Purple", "silver": "White", "black": "DarkGray"}[word]


class Recolor:
    """Moves the saturated colours of copied scenes from the source character's hue to the new one, keeping their
    brightness (white sparks and dark shadows stay as they are)."""

    def __init__(self, src_primary, dst_primary):
        sh, ss, _ = colorsys.rgb_to_hsv(*hex_rgb(src_primary))
        dh, ds, _ = colorsys.rgb_to_hsv(*hex_rgb(dst_primary))
        self.hue = dh
        self.sat_scale = min(1.5, ds / ss) if ss > 0 else 1.0

    def color(self, m):
        r, g, b = (float(m.group(i)) for i in (1, 2, 3))
        h, s, v = colorsys.rgb_to_hsv(r, g, b)
        if s < 0.25:
            return m.group(0)
        r, g, b = colorsys.hsv_to_rgb(self.hue, min(1.0, s * self.sat_scale), v)
        return f"Color({r:.3g}, {g:.3g}, {b:.3g}, {m.group(4)})"

    def __call__(self, text):
        return re.sub(r"Color\(([\d.]+), ([\d.]+), ([\d.]+), ([\d.]+)\)", self.color, text)


# ---------------------------------------------------------------------------------------------------------------

def tokens(a):
    primary = a.primary.upper().lstrip("#")
    gender, he, him, his_pronoun, his = PRONOUNS[a.gender]
    starter = a.starter or f"{a.cls}Keepsake"
    first = a.full_name.split()[0]
    hh = colorsys.rgb_to_hsv(*hex_rgb(primary))[0]
    word = a.color_word or hue_word(primary)
    return {
        "id": a.id, "ID": a.id.upper(), "Class": a.cls, "Name": a.name, "FullName": a.full_name, "FirstName": first,
        "Initial": a.full_name.split()[-1][0].upper(),
        "Starter": starter, "STARTER_ID": presmod.slug(starter).upper(), "starter_id": presmod.slug(starter),
        "StarterName": a.starter_name or re.sub(r"(?<=[a-z0-9])([A-Z])", r" \1", starter),
        "primary": primary, "secondary": (a.secondary or shade(primary, 0.28, 0.9)).upper().lstrip("#"),
        "accent": a.accent.upper().lstrip("#"), "text": a.text.upper().lstrip("#"),
        "dark": shade(primary, 0.5), "deck": shade(primary, 0.9), "map": shade(primary, 0.8), "dialogue": shade(primary, 0.24),
        "tint_dark": shade(primary, 0.19), "tint_mid": shade(primary, 0.85), "tint_light": shade(primary, 1.1, 0.35),
        "frame_h": f"{(hh + 0.02) % 1:.3f}",
        "color_word": word, "ColorWordTitle": word.title(), "VfxColor": vfx_color(primary),
        "Gender": gender, "he": he, "him": him, "his_pronoun": his_pronoun, "his": his,
        "hp": str(a.hp), "gold": str(a.gold), "sfx": a.sfx, "date": datetime.date.today().isoformat(),
    }


def fill(text, tok):
    def sub(m):
        if m.group(1) not in tok:
            raise KeyError(f"unknown template token {{{{{m.group(1)}}}}}")
        return tok[m.group(1)]
    return re.sub(r"\{\{(\w+)\}\}", sub, text)


class Writer:
    def __init__(self, dry_run):
        self.dry_run = dry_run
        self.written = []

    def write(self, path, text):
        if os.path.exists(path):
            raise FileExistsError(path)
        self.written.append(os.path.relpath(path, presmod.REPO).replace(os.sep, "/"))
        if self.dry_run:
            return
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)


def check_names(tok):
    """Model IDs come from class names alone: refuse names another character or the base game already uses."""
    names = [fill(f[:-3], tok) for _, _, files in os.walk(os.path.join(TEMPLATE, "src")) for f in files
             if f.endswith(".cs") and not f.startswith("DevHarness.")]
    taken = set()
    for root, _, files in os.walk(presmod.SRC_DIR):
        for f in files:
            if f.endswith(".cs"):
                with open(os.path.join(root, f), encoding="utf-8") as fh:
                    taken.update(re.findall(r"\bclass (\w+)", fh.read()))
    if os.path.isdir(GAME_MODELS):
        for root, _, files in os.walk(GAME_MODELS):
            taken.update(f[:-3] for f in files if f.endswith(".cs"))
    clashes = [n for n in names if n in taken]
    if clashes:
        sys.exit(f"Class names already used by the mod or the game: {', '.join(clashes)} (pick another --class or --starter)")


def copy_template(w, tok, ch_dir, src_dir):
    for root, _, files in os.walk(TEMPLATE):
        rel_root = os.path.relpath(root, TEMPLATE)
        for f in files:
            rel = os.path.normpath(os.path.join(rel_root, f))
            if rel == "README.md":
                continue
            with open(os.path.join(root, f), encoding="utf-8") as fh:
                text = fill(fh.read(), tok)
            if rel.startswith("src" + os.sep):
                dst = os.path.join(src_dir, fill(rel[4:], tok))
            else:
                dst = os.path.join(ch_dir, fill(rel, tok))
            if dst.endswith(".json"):
                json.loads(text)  # a token must not break the JSON
            w.write(dst, text)


def ancients(w, tok, source, ch_dir):
    """Every Ancient line the dialogue patches read, keyed like the source character's, with placeholder text."""
    src = json.load(open(source.path("localization", "eng", "ancients.json"), encoding="utf-8"))
    out = {}
    for key in src:
        new_key = key.replace(f".{source.entry}.", f".{tok['ID']}.")
        if key.endswith(".next"):
            out[new_key] = "Continue"
        elif key.endswith(".char"):
            out[new_key] = f"TODO {tok['Name']} answers."
        else:
            out[new_key] = "TODO the Ancient speaks."
    w.write(os.path.join(ch_dir, "localization", "eng", "ancients.json"), json.dumps(out, indent=2, ensure_ascii=False) + "\n")


def compendium(w, source, ch_dir):
    """The review page's template carries the source character's prose: a starting point, rewritten in the design step."""
    with open(source.path("design", "compendium_template.html"), encoding="utf-8") as f:
        w.write(os.path.join(ch_dir, "design", "compendium_template.html"), f.read())


def scenes(w, tok, source):
    recolor = Recolor(source["colors"]["primary"], tok["primary"])
    sid, scls = source.id, source["class"]
    for pattern in presmod.CHARACTER_SCENES:
        src = os.path.join(presmod.MOD_DIR, pattern.format(id=sid))
        dst = os.path.join(presmod.MOD_DIR, pattern.format(id=tok["id"]))
        if "character_icons" in pattern or "card_frame" in pattern:
            continue  # made by make_placeholders.py from the game's own
        with open(src, encoding="utf-8") as f:
            text = f.read()
        text = re.sub(r'^(\[gd_(?:scene|resource)[^\]]*?) uid="uid://[a-z0-9]+"', r"\1", text, count=1, flags=re.M)
        text = text.replace(sid, tok["id"]).replace(scls, tok["Class"]).replace(source.entry, tok["ID"])
        w.write(dst, recolor(text))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("id", help="lowercase id = the class name in snake_case, e.g. biden for class Biden (model ID BIDEN)")
    ap.add_argument("--class", dest="cls", required=True, help="C# class name, e.g. Biden")
    ap.add_argument("--name", required=True, help='display name, e.g. "Uncle Joe"')
    ap.add_argument("--full-name")
    ap.add_argument("--primary", required=True, help="card and UI colour, hex")
    ap.add_argument("--secondary")
    ap.add_argument("--accent", default="B22234")
    ap.add_argument("--text", default="FAF0D7")
    ap.add_argument("--color-word")
    ap.add_argument("--gender", choices=sorted(PRONOUNS), default="masculine")
    ap.add_argument("--hp", type=int, default=75)
    ap.add_argument("--gold", type=int, default=99)
    ap.add_argument("--starter")
    ap.add_argument("--starter-name")
    ap.add_argument("--sfx", choices=BASE_CHARACTERS, default="ironclad")
    ap.add_argument("--from", dest="source", help="character whose scenes are copied (default: the first)")
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()
    a.full_name = a.full_name or a.name

    if not re.fullmatch(r"[a-z][a-z0-9_]*", a.id):
        sys.exit("id: lowercase letters, digits and _ only")
    if not re.fullmatch(r"[A-Z][A-Za-z0-9]*", a.cls):
        sys.exit("--class: a C# class name in PascalCase")
    if presmod.slug(a.cls) != a.id:
        # The game names the model after the class (SmokeTest -> SMOKE_TEST) and every asset path after the model.
        sys.exit(f"The id must be the class name in snake_case: --class {a.cls} needs id '{presmod.slug(a.cls)}' "
                 f"(or pick --class {a.id.title().replace('_', '')} for id '{a.id}')")
    if not re.fullmatch(r"#?[0-9A-Fa-f]{6}", a.primary):
        sys.exit("--primary: a hex colour like 2F6BD8")
    if a.id in presmod.character_ids() or a.id in BASE_CHARACTERS:
        sys.exit(f"'{a.id}' is already a character")
    ch_dir = os.path.join(presmod.CHARACTERS_DIR, a.id)
    src_dir = os.path.join(presmod.SRC_DIR, "Characters", a.cls)
    for d in (ch_dir, src_dir):
        if os.path.exists(d):
            sys.exit(f"{d} already exists")

    tok = tokens(a)
    check_names(tok)
    source = presmod.character(a.source)
    w = Writer(a.dry_run)
    copy_template(w, tok, ch_dir, src_dir)
    ancients(w, tok, source, ch_dir)
    compendium(w, source, ch_dir)
    scenes(w, tok, source)

    print(("Would write" if a.dry_run else "Wrote") + f" {len(w.written)} files:")
    for p in w.written:
        print("  " + p)
    if a.dry_run:
        return
    for script in ("make_placeholders.py", "render_design.py", "render_gallery.py"):
        subprocess.run([sys.executable, os.path.join(presmod.REPO, "scripts", script), "-c", a.id], check=True)
    print(f"""
{a.name} ({tok['ID']}) is ready to build: python scripts/build.py --install, then python scripts/test.py ui -c {a.id}
Next (docs/ADDING_A_CHARACTER.md): design (characters/{a.id}/design/), code, texts (TODO lines),
art (fill character.json "art" and art/art_assets.json, then python scripts/art_review.py), balance.""")


if __name__ == "__main__":
    main()

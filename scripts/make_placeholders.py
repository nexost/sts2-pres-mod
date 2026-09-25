"""
Placeholders for every character: simple generated images at the exact sizes and paths the game expects, so a character
is fully playable before its art exists. The art review tool (scripts/art_review.py) replaces them with the real art on
Keep. Existing files are never overwritten unless --force. Run by build.py before every build.

  python scripts/make_placeholders.py [--force] [--character trump]

What gets a placeholder, per character (colours from characters/<id>/character.json "colors"):
  - every card, relic, power and potion class in mod/pres_mod/src/Characters/<Folder>/ (found by their base class);
  - the character's UI: top-bar icon, character select button, map marker, energy icon;
  - every other art item in characters/<id>/art/art_assets.json (poses, select screen, orb, hands, props, ...);
  - hover outlines for all relics and potions (derived from the icons, rebuilt when an icon changes);
  - the two scenes copied from Ironclad: the top-bar icon scene (if missing) and the card frame colour (rewritten
    whenever character.json "frame_hsv" changes).
"""
import argparse
import os
import re
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

MOD = presmod.MOD_DIR
GAME = presmod.GAME_PCK
FORCE = False


def rgb(hex_color):
    h = hex_color.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def out(rel):
    path = os.path.join(MOD, rel.replace("/", os.sep))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    return path


def font(size):
    for name in ("arialbd.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def centered(draw, box, text, size, fill):
    f = font(size)
    l, t, r, b = draw.textbbox((0, 0), text, font=f)
    x = box[0] + (box[2] - box[0] - (r - l)) / 2 - l
    y = box[1] + (box[3] - box[1] - (b - t)) / 2 - t
    draw.text((x, y), text, font=f, fill=fill)


def save(img, rel):
    path = out(rel)
    if os.path.exists(path) and not FORCE:
        return
    img.save(path)
    print("wrote", rel)


class Palette:
    def __init__(self, ch):
        c = ch.get("colors", {})
        self.primary = rgb(c.get("primary", "F2B92E"))
        self.secondary = rgb(c.get("secondary", "162242"))
        self.accent = rgb(c.get("accent", "B22234"))
        self.text = rgb(c.get("text", "FAF0D7"))


def badge(size, text, bg, fg, circle=True, outline_only=False):
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pad = max(2, w // 16)
    shape = [pad, pad, w - pad, h - pad]
    if outline_only:
        (d.ellipse if circle else d.rounded_rectangle)(shape, outline=(255, 255, 255, 255), width=max(2, w // 14), **({} if circle else {"radius": w // 6}))
        return img
    (d.ellipse if circle else d.rounded_rectangle)(shape, fill=bg + (255,), outline=fg + (255,), width=max(2, w // 18), **({} if circle else {"radius": w // 6}))
    centered(d, shape, text, int(min(w, h) * 0.55), fg + (255,))
    return img


def card_portrait(title, kind, pal, size=(1000, 760)):
    w, h = size
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    top = {"attack": (120, 30, 40), "skill": (30, 60, 110), "power": (90, 60, 20)}.get(kind, (60, 60, 60))
    for y in range(h):
        t = y / h
        d.line([(0, y), (w, y)], fill=tuple(int(top[i] * (1 - t) + pal.secondary[i] * t) for i in range(3)) + (255,))
    for i in range(0, w + h, 60):
        d.line([(i, 0), (i - h, h)], fill=(255, 255, 255, 18), width=14)
    centered(d, (0, h * 0.24, w, h * 0.68), title, int(w * 0.11), pal.primary + (255,))
    centered(d, (0, h * 0.74, w, h * 0.9), "PLACEHOLDER ART", int(w * 0.044), pal.text + (200,))
    return img


def figure(size, pal, eyes=None):
    """A stand-in figure: body in the secondary colour, a tie in the accent colour, on transparent.
    eyes: optional hex colour for a band across the eyes (an item's "placeholder_eyes", e.g. Biden's red Dark Brandon poses)."""
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([w * 0.25, h * 0.3, w * 0.75, h * 0.97], radius=int(w * 0.08), fill=pal.secondary + (255,))
    d.polygon([(w * 0.47, h * 0.32), (w * 0.53, h * 0.32), (w * 0.55, h * 0.75), (w * 0.5, h * 0.8), (w * 0.45, h * 0.75)], fill=pal.accent + (255,))
    d.ellipse([w * 0.33, h * 0.08, w * 0.67, h * 0.33], fill=(235, 160, 110, 255))
    d.ellipse([w * 0.28, h * 0.03, w * 0.74, h * 0.16], fill=pal.primary + (255,))
    if eyes:
        rgb = tuple(int(eyes.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
        d.rounded_rectangle([w * 0.35, h * 0.17, w * 0.65, h * 0.22], radius=int(w * 0.02), fill=rgb + (255,))
        d.polygon([(w * 0.65, h * 0.18), (w * 0.98, h * 0.12), (w * 0.98, h * 0.16), (w * 0.65, h * 0.21)], fill=rgb + (200,))
    centered(d, (0, int(h * 0.82), w, h), "PLACEHOLDER", max(12, w // 14), pal.text + (230,))
    return img


def initials(class_name):
    return "".join(re.findall(r"[A-Z0-9]", class_name))[:3] or class_name[:2].upper()


# ---------------------------------------------------------------------------------------------------------------
# The character's model classes, read from its C# source

def all_classes():
    """{class name: (base class, file path, source text)} for every class in mod/pres_mod/src."""
    classes = {}
    for dp, _, files in os.walk(presmod.SRC_DIR):
        for f in files:
            if f.endswith(".cs"):
                path = os.path.join(dp, f)
                text = open(path, encoding="utf-8").read()
                for m in re.finditer(r"public\s+(?:sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)(\([^)]*\))?\s*:\s*(\w+)\s*(\([^;{]*\))?", text):
                    classes[m.group(1)] = (m.group(3), path, text, m)
    return classes


def models_of(ch, root_bases):
    """(class name, match, text) of concrete classes in the character's folder whose base chain reaches one of root_bases."""
    classes = all_classes()
    found = []
    for name, (base, path, text, m) in classes.items():
        if not os.path.abspath(path).startswith(os.path.abspath(ch.src)) or "abstract" in m.group(0):
            continue
        root = base
        while root in classes and root not in root_bases:
            root = classes[root][0]
        if root in root_bases:
            found.append((name, m, text))
    return sorted(found)


def card_kind(m, text):
    args = m.group(4) or ""
    if not args:  # classic constructor: base(cost, CardType.X, ...)
        ctor = re.search(r"base\(\s*-?\w+\s*,\s*CardType\.(\w+)", text[m.end():])
        args = ctor.group(0) if ctor else ""
    kind = re.search(r"CardType\.(\w+)", args)
    return (kind.group(1) if kind else "Skill").lower()


# ---------------------------------------------------------------------------------------------------------------

def make_images(ch):
    pal = Palette(ch)
    cid = ch.id
    letter = ch["art"].get("placeholder_initial", ch["name"][:1])
    ui = presmod.KIND_OUTPUTS
    save(badge((88, 88), letter, pal.secondary, pal.primary), ui["top_icon"]["icon"].format(id=cid))
    save(badge((88, 88), "", pal.secondary, pal.primary, outline_only=True), ui["top_icon"]["outline"].format(id=cid))
    for locked in (False, True):
        img = Image.new("RGBA", (132, 195), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        d.rounded_rectangle([4, 4, 128, 191], radius=18, fill=((60, 60, 60) if locked else pal.secondary) + (255,),
                            outline=((120, 120, 120) if locked else pal.primary) + (255,), width=5)
        centered(d, (4, 20, 128, 140), "?" if locked else letter, 90, ((150, 150, 150) if locked else pal.primary) + (255,))
        centered(d, (4, 140, 128, 185), ch["name"].split()[-1].upper()[:8], 22, pal.text + (255,))
        save(img, ui["char_button"]["locked" if locked else "button"].format(id=cid))
    marker = Image.new("RGBA", (49, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(marker)
    d.polygon([(24, 63), (4, 30), (44, 30)], fill=pal.primary + (255,))
    d.ellipse([2, 2, 46, 46], fill=pal.secondary + (255,), outline=pal.primary + (255,), width=3)
    centered(d, (2, 2, 46, 46), letter, 28, pal.primary + (255,))
    save(marker, ui["map_marker"]["marker"].format(id=cid))
    # Energy icon inside card text ([img] tag) and on the card cost gem (ui_atlas, served by our atlas fallback).
    save(badge((24, 24), "", pal.primary, pal.secondary), ui["energy_icon"]["text"].format(id=cid))
    save(badge((74, 74), "", pal.primary, pal.secondary), ui["energy_icon"]["gem"].format(id=cid))

    for name, m, text in models_of(ch, {"CardModel"}):
        kind = card_kind(m, text)
        title = re.sub(r"(?<=[a-z0-9])([A-Z])", r" \1", name).replace(" " + ch["class"], "").upper()
        save(card_portrait(title, kind, pal), f"images/packed/card_portraits/{cid}/{presmod.slug(name)}.png")
    # Relics, powers and potions: loose PNGs are picked up by the game's own atlas fallback (images/<kind>/<id>.png).
    for name, _, _ in models_of(ch, {"RelicModel"}):
        save(badge((256, 256), initials(name), pal.accent, pal.text), f"images/relics/{presmod.slug(name)}.png")
    for name, _, _ in models_of(ch, {"PowerModel", "TemporaryStrengthPower"}):
        save(badge((256, 256), initials(name.removesuffix("Power")), pal.secondary, pal.primary), f"images/powers/{presmod.slug(name)}.png")
    for name, _, _ in models_of(ch, {"PotionModel"}):
        save(badge((256, 256), initials(name), (40, 110, 90), pal.text), f"images/potions/{presmod.slug(name)}.png")


def make_art_standins(ch):
    """Every other art item of art_assets.json that has no file yet gets a stand-in of the right size."""
    pal = Palette(ch)
    assets = ch.assets
    for spec in assets.get("character", []) + assets.get("ui", []):
        kind = spec["kind"]
        if kind in ("char_button", "top_icon", "map_marker", "energy_icon"):
            continue  # made above
        outputs = presmod.art_outputs(ch, spec["id"], kind, spec)
        for key, rel in outputs.items():
            if kind in ("figure", "figure_sheet"):
                img = figure((600, 900), pal, spec.get("placeholder_eyes"))
            elif kind == "fullscreen":
                img = Image.new("RGBA", (2560, 1200))
                d = ImageDraw.Draw(img)
                for y in range(1200):
                    t = y / 1200
                    d.line([(0, y), (2560, y)], fill=tuple(int(pal.secondary[i] * (1 - t * 0.5)) for i in range(3)) + (255,))
                centered(d, (1300, 300, 2500, 900), ch["name"].upper(), 160, pal.primary + (255,))
            elif kind == "hand":
                img = Image.new("RGBA", (422, 1200), (0, 0, 0, 0))
                d = ImageDraw.Draw(img)
                d.rectangle([130, 500, 292, 1199], fill=pal.secondary + (255,))
                d.ellipse([110, 300, 312, 540], fill=(235, 160, 110, 255))
                centered(d, (0, 560, 422, 700), spec["id"].split("_", 1)[-1].upper(), 40, pal.text + (255,))
            elif kind == "transition":
                img = Image.radial_gradient("L").resize((2560, 1200)).convert("RGBA")
            elif kind == "orb":
                n = int(key.removeprefix("layer"))
                img = badge((256, 256), "", pal.primary, pal.secondary) if n == 1 else Image.new("RGBA", (256, 256), (0, 0, 0, 0))
            elif kind in ("prop", "decal"):
                w, h = spec.get("fit", [640, 400] if kind == "prop" else [512, 300])
                img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
                d = ImageDraw.Draw(img)
                d.rectangle([10, int(h * 0.15), w - 10, h - 1], fill=(pal.primary if kind == "prop" else (0, 0, 0)) + ((255,) if kind == "prop" else (0,)),
                            outline=pal.accent + (255,), width=6)
                centered(d, (10, int(h * 0.15), w - 10, h - 1), spec["name"].split(":")[-1].strip().upper()[:14], max(18, h // 7), pal.text + (255,))
            elif kind == "small_icon":
                size = spec.get("sizes", {}).get(key, 64)
                img = badge((size, size), "", pal.primary, pal.secondary)
            else:
                continue
            save(img, rel)


def derive_outlines():
    """Hover outlines for relics and potions, like the game's relic/potion_outline_atlas: a white silhouette a little
    larger than the icon. Rebuilt whenever the icon is newer (the art review tool replaces icons on Keep)."""
    for kind in ("relics", "potions"):
        src_dir = os.path.join(MOD, "images", kind)
        if not os.path.isdir(src_dir):
            continue
        for f in sorted(os.listdir(src_dir)):
            if not f.endswith(".png"):
                continue
            src = os.path.join(src_dir, f)
            rel = f"{presmod.MOD_ID}/atlas_fallback/{kind[:-1]}_outline_atlas/{f}"
            dst = out(rel)
            if os.path.exists(dst) and os.path.getmtime(dst) >= os.path.getmtime(src) and not FORCE:
                continue
            alpha = Image.open(src).convert("RGBA").getchannel("A").point(lambda v: 255 if v > 24 else 0)
            alpha = alpha.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(1.2))
            outline = Image.new("RGBA", alpha.size, (255, 255, 255, 0))
            outline.putalpha(alpha)
            outline.save(dst)
            print("wrote", rel)


def copy_scene(src_rel, dst_rel, renames=(), replace=(), derived=False):
    """Copy a game scene or resource into the mod. derived: it only holds values from character.json, so rewrite it
    whenever they change (otherwise an existing file is kept)."""
    dst = out(dst_rel)
    if os.path.exists(dst) and not FORCE and not derived:
        return
    with open(os.path.join(GAME, src_rel.replace("/", os.sep)), encoding="utf-8") as fh:
        text = fh.read()
    # Drop the copied resource's own uid so it never clashes with the original in the game's UID cache.
    text = re.sub(r'^(\[gd_(?:scene|resource)[^\]]*?) uid="uid://[a-z0-9]+"', r"\1", text, count=1, flags=re.M)
    for old, new in renames:
        text = text.replace(f'[node name="{old}"', f'[node name="{new}"', 1)
    for old, new in replace:
        text = text.replace(old, new)
    if os.path.exists(dst):
        with open(dst, encoding="utf-8") as fh:
            if fh.read() == text:
                return
    with open(dst, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)
    print("wrote", dst_rel)


def make_scenes(ch):
    """The two resources every character copies from Ironclad. The rest of its scenes (combat body, shop, rest site,
    character select, energy counter, card trail, transition) are created by scripts/new_character.py."""
    cid, cls = ch.id, ch["class"]
    copy_scene("scenes/ui/character_icons/ironclad_icon.tscn", f"scenes/ui/character_icons/{cid}_icon.tscn", [("IroncladIcon", f"{cls}Icon")],
               [('uid="uid://x2neryjvbtwy" path="res://images/ui/top_panel/character_icon_ironclad.png"', f'path="res://images/ui/top_panel/character_icon_{cid}.png"')])
    # Card frame: the game's HSV shader on the orange frame, tinted per character (character.json "frame_hsv").
    hsv = ch.get("frame_hsv", {"h": 0.15, "s": 1.1, "v": 1.3})
    copy_scene("materials/cards/frames/card_frame_orange_mat.tres", f"materials/cards/frames/card_frame_{cid}_mat.tres",
               replace=[("shader_parameter/h = 0.12", f"shader_parameter/h = {hsv['h']}"), ("shader_parameter/s = 1.5", f"shader_parameter/s = {hsv['s']}"),
                        ("shader_parameter/v = 1.2", f"shader_parameter/v = {hsv['v']}")], derived=True)


def main():
    global FORCE
    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true", help="overwrite existing files (replaces real art!)")
    ap.add_argument("--character", "-c", help="only this character (default: all)")
    a = ap.parse_args()
    FORCE = a.force
    chars = [presmod.character(a.character)] if a.character else presmod.characters()
    for ch in chars:
        make_images(ch)
        make_art_standins(ch)
        make_scenes(ch)
    derive_outlines()


if __name__ == "__main__":
    main()

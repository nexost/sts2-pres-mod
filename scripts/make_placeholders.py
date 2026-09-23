"""
Step 2 placeholders: simple generated images at the exact sizes/paths the game expects, plus scene/material
copies of the Ironclad ones (still pointing at Ironclad's animations) so the character is fully wired.
Every file written here gets replaced by real art in Step 8. Existing files are left alone unless --force.

Usage: python scripts/make_placeholders.py [--force]
"""
import os
import re
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(ROOT, "mod")
GAME = os.path.join(ROOT, "re", "pck")
FORCE = "--force" in sys.argv

GOLD = (242, 185, 46)
NAVY = (22, 34, 66)
CREAM = (250, 240, 215)
RED = (178, 34, 52)


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


def badge(size, text, bg=NAVY, fg=GOLD, circle=True, outline_only=False):
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


def card_portrait(title, kind):
    w, h = 1000, 760
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    top = {"attack": (120, 30, 40), "skill": (30, 60, 110), "power": (90, 60, 20)}[kind]
    for y in range(h):
        t = y / h
        d.line([(0, y), (w, y)], fill=tuple(int(top[i] * (1 - t) + NAVY[i] * t) for i in range(3)) + (255,))
    for i in range(0, w + h, 60):
        d.line([(i, 0), (i - h, h)], fill=(255, 255, 255, 18), width=14)
    centered(d, (0, 180, w, 520), title, 110, GOLD + (255,))
    centered(d, (0, 560, w, 680), "PLACEHOLDER ART", 44, CREAM + (200,))
    return img


def find_models(bases):
    """Class names of every concrete model in the mod source deriving directly from one of the given base classes."""
    names = []
    src = os.path.join(MOD, "trump_character", "src")
    for dp, _, files in os.walk(src):
        for f in files:
            if f.endswith(".cs"):
                text = open(os.path.join(dp, f), encoding="utf-8").read()
                names += [m.group(1) for m in re.finditer(r"public\s+sealed\s+class\s+(\w+)\s*:\s*(\w+)", text) if m.group(2) in bases]
    return sorted(set(names))


def slug(class_name):
    """The game's model ID entry, lowercased: GoldenShovel -> golden_shovel."""
    return re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", class_name).lower()


def initials(class_name):
    return "".join(re.findall(r"[A-Z0-9]", class_name))[:3] or class_name[:2].upper()


def find_cards():
    """(class name, 'attack'|'skill'|'power') for every concrete card class in the mod source."""
    cards = []
    src = os.path.join(MOD, "trump_character", "src")
    for dp, _, files in os.walk(src):
        for f in files:
            if not f.endswith(".cs"):
                continue
            text = open(os.path.join(dp, f), encoding="utf-8").read()
            for m in re.finditer(r"public\s+sealed\s+class\s+(\w+)(\([^)]*\))?\s*:\s*(\w+)\s*(\([^;{]*\))?", text):
                name, base = m.group(1), m.group(3)
                if base not in ("CardModel", "PayGoldCard"):
                    continue
                args = m.group(4) or ""
                if not args:  # classic constructor: base(cost, CardType.X, ...)
                    ctor = re.search(r"base\(\s*-?\w+\s*,\s*CardType\.(\w+)", text[m.end():])
                    args = ctor.group(0) if ctor else ""
                kind = re.search(r"CardType\.(\w+)", args)
                cards.append((name, (kind.group(1) if kind else "Skill").lower()))
    return cards


def make_images():
    save(badge((88, 88), "T"), "images/ui/top_panel/character_icon_trump.png")
    save(badge((88, 88), "", outline_only=True), "images/ui/top_panel/character_icon_trump_outline.png")

    for locked in (False, True):
        img = Image.new("RGBA", (132, 195), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        d.rounded_rectangle([4, 4, 128, 191], radius=18, fill=((60, 60, 60) if locked else NAVY) + (255,), outline=((120, 120, 120) if locked else GOLD) + (255,), width=5)
        centered(d, (4, 20, 128, 140), "?" if locked else "T", 90, ((150, 150, 150) if locked else GOLD) + (255,))
        centered(d, (4, 140, 128, 185), "TRUMP", 22, CREAM + (255,))
        save(img, f"images/packed/character_select/char_select_trump{'_locked' if locked else ''}.png")

    marker = Image.new("RGBA", (49, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(marker)
    d.polygon([(24, 63), (4, 30), (44, 30)], fill=GOLD + (255,))
    d.ellipse([2, 2, 46, 46], fill=NAVY + (255,), outline=GOLD + (255,), width=3)
    centered(d, (2, 2, 46, 46), "T", 28, GOLD + (255,))
    save(marker, "images/packed/map/icons/map_marker_trump.png")

    for class_name, card_type in find_cards():
        title = re.sub(r"(?<=[a-z0-9])([A-Z])", r" \1", class_name).replace(" Trump", "").upper()
        save(card_portrait(title, card_type), f"images/packed/card_portraits/trump/{slug(class_name)}.png")

    # Relics, powers and potions: loose PNGs are picked up by the game's own atlas fallback (images/<kind>/<id>.png).
    for name in find_models(("RelicModel",)):
        save(badge((256, 256), initials(name), bg=RED, fg=CREAM), f"images/relics/{slug(name)}.png")
    for name in find_models(("PowerModel", "TemporaryStrengthPower")):
        save(badge((256, 256), initials(name.removesuffix("Power"))), f"images/powers/{slug(name)}.png")
    for name in find_models(("PotionModel",)):
        save(badge((256, 256), initials(name), bg=(40, 110, 90), fg=CREAM), f"images/potions/{slug(name)}.png")

    # Gold coin that replaces the star-cost badge on Pay-Gold cards (PayGoldBadgePatch).
    save(badge((64, 64), "$", bg=GOLD, fg=(110, 70, 10)), "trump_character/ui/gold_cost_icon.png")

    # Energy icon inside card text ([img] tag) and on the card cost gem (ui_atlas, served by our atlas fallback).
    save(badge((24, 24), "", bg=GOLD, fg=NAVY), "images/packed/sprite_fonts/trump_energy_icon.png")
    save(badge((74, 74), "", bg=GOLD, fg=NAVY), "trump_character/atlas_fallback/ui_atlas/card/energy_trump.png")


def copy_scene(src_rel, dst_rel, renames=(), replace=()):
    dst = out(dst_rel)
    if os.path.exists(dst) and not FORCE:
        return
    with open(os.path.join(GAME, src_rel.replace("/", os.sep)), encoding="utf-8") as fh:
        text = fh.read()
    # Drop the copied resource's own uid so it never clashes with the original in the game's UID cache.
    text = re.sub(r'^(\[gd_(?:scene|resource)[^\]]*?) uid="uid://[a-z0-9]+"', r"\1", text, count=1, flags=re.M)
    for old, new in renames:
        text = text.replace(f'[node name="{old}"', f'[node name="{new}"', 1)
    for old, new in replace:
        text = text.replace(old, new)
    with open(dst, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)
    print("wrote", dst_rel)


def make_scenes():
    copy_scene("scenes/creature_visuals/ironclad.tscn", "scenes/creature_visuals/trump.tscn", [("Ironclad", "Trump")])
    copy_scene("scenes/ui/character_icons/ironclad_icon.tscn", "scenes/ui/character_icons/trump_icon.tscn", [("IroncladIcon", "TrumpIcon")],
               [('uid="uid://x2neryjvbtwy" path="res://images/ui/top_panel/character_icon_ironclad.png"', 'path="res://images/ui/top_panel/character_icon_trump.png"')])
    copy_scene("scenes/combat/energy_counters/ironclad_energy_counter.tscn", "scenes/combat/energy_counters/trump_energy_counter.tscn", [("IroncladEnergyCounter", "TrumpEnergyCounter")])
    copy_scene("scenes/merchant/characters/ironclad_merchant.tscn", "scenes/merchant/characters/trump_merchant.tscn", [("IroncladMerchant", "TrumpMerchant")])
    copy_scene("scenes/rest_site/characters/ironclad_rest_site.tscn", "scenes/rest_site/characters/trump_rest_site.tscn", [("IroncladRestSite", "TrumpRestSite")])
    copy_scene("scenes/screens/char_select/char_select_bg_ironclad.tscn", "scenes/screens/char_select/char_select_bg_trump.tscn", [("IroncladBg", "TrumpBg")])
    copy_scene("scenes/vfx/card_trail_ironclad.tscn", "scenes/vfx/card_trail_trump.tscn", [("CardTrailIronclad", "CardTrailTrump")])
    copy_scene("materials/transitions/ironclad_transition_mat.tres", "materials/transitions/trump_transition_mat.tres")
    # Card frame: same HSV shader as every character, tinted gold (Regent's orange is h=0.12).
    copy_scene("materials/cards/frames/card_frame_orange_mat.tres", "materials/cards/frames/card_frame_trump_mat.tres",
               replace=[("shader_parameter/h = 0.12", "shader_parameter/h = 0.15"), ("shader_parameter/s = 1.5", "shader_parameter/s = 1.1"), ("shader_parameter/v = 1.2", "shader_parameter/v = 1.3")])


if __name__ == "__main__":
    make_images()
    make_scenes()

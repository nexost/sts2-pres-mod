"""Every piece of art a character needs, and how to make each one.

An item = what to draw (characters/<id>/design/cards.json for card portraits, characters/<id>/art/art_assets.json for
everything else) plus a recipe: style references from the unpacked game, a prompt template, a generation size,
post-processing, and the mod paths the finished files go to (presmod.art_outputs). The character's own look comes from
characters/<id>/character.json ("art": persona, card references and backgrounds per archetype, energy tint, ...).
Used by scripts/art_review.py; the recipe per kind is documented in docs/ART_PIPELINE.md.
"""
import os
import random
import re
import zlib

from PIL import Image, ImageEnhance, ImageFilter, ImageOps

import art_post
import presmod
from presmod import slug  # noqa: F401  (re-exported for older callers)

REPO = presmod.REPO
GAME = presmod.GAME_PCK
MOD = presmod.MOD_DIR
KIT = os.path.join(REPO, "build", "art", "ref", "kit")      # prepared style references
FRAMES = os.path.join(REPO, "build", "art", "ref", "frames")  # card frame overlays for the preview
CARDS = os.path.join(GAME, "images", "packed", "card_portraits")

CARD_STYLE = ("Flat cel-shaded digital painting with hard-edged dark shadow shapes, dark outlines, strong readable "
              "silhouette, saturated limited palette, in the exact painting style of the reference images.")
FRAMING = "Keep the main subject centered and in the upper two thirds of the image."
# The game's text box covers the lower half. Naming it made the model paint a fake one with gibberish text.
FRAMING_ANCIENT = ("Tall full-card art: keep the main subject and faces in the upper half of the image; the lower half "
                   "is plain, simple background with no objects, no text and no border.")
LIGHT_BG = "isolated on a plain flat light gray background, no shadow, no text"
GREEN_BG = "isolated on a plain flat bright green background, no ground, no shadow, no text"
CARD_ACTION = {"attack": "dynamic diagonal action with impact lines", "skill": "a clear, readable scene", "power": "an iconic, glowing, almost symmetrical composition"}
ENEMY_WORDS = re.compile(r"\b(enem|monster|foe|goblin|slime|beetle|cultist|elite|silhouette|crowd)", re.I)

# Quality presets for the review tool (tested in characters/trump/docs/07_step7_report.md §7). The style-reference LoRA
# at full strength with three references is what made the first batch muddy; lower strength, two references and more
# steps give clean, crisp images that still take the game's style. The optional second pass upscales 1.5-2x and
# re-samples for detail. int8 and fp8 give the same quality here and int8 is twice as fast, so every preset uses int8.
INT8 = "krea2_turbo_int8_convrot.safetensors"
FP8 = "krea2_turbo_fp8_scaled.safetensors"
QUALITY = {
    "draft": {"label": "Draft", "desc": "~15 s. 8 steps, 2 style references at 0.8. For trying prompts.",
              "unet": INT8, "steps": 8, "refs": 2, "style_strength": 0.8},
    "standard": {"label": "Standard", "desc": "~25 s. 12 steps, 2 style references at 0.75. Clean and crisp; the default.",
                 "unet": INT8, "steps": 12, "refs": 2, "style_strength": 0.75},
    "high": {"label": "High", "desc": "~60 s. 16 steps, 2 references at 0.75, plus a 1.5x detail pass.",
             "unet": INT8, "steps": 16, "refs": 2, "style_strength": 0.75, "hires": {"scale": 1.5, "denoise": 0.35, "steps": 10}},
    "max": {"label": "Max", "desc": "~2 min. 16 steps, 3 references at 0.75, plus a 2x detail pass. For hero art.",
            "unet": INT8, "steps": 16, "refs": 3, "style_strength": 0.75, "hires": {"scale": 2.0, "denoise": 0.35, "steps": 12}},
    "legacy": {"label": "Legacy (first batch)", "desc": "The settings of the first batch: 8 steps, 3 references at full strength.",
               "unet": INT8, "steps": 8, "refs": 3, "style_strength": 1.0},
}
DEFAULT_QUALITY = "standard"
# Power icons: the same two references for every icon made them all copies of Thorns' green star and Strength's red.
# Each icon gets its own pair from this pool of varied single-object game icons, at a lower style strength, so the
# references give the style (flat cel shading, thick outline) and the icon's text gives the shape and colours.
POWER_REF_POOL = ["flutter", "countdown", "curious", "ringing", "royalties", "slumber", "storm", "vigor", "ritual",
                  "borrowed_time", "speedster", "nostalgia"]
POWER_STYLE_STRENGTH = 0.55


def quality_settings(key, item):
    """The generation settings of a quality preset for one item, as art_gen job fields."""
    q = QUALITY.get(key or DEFAULT_QUALITY, QUALITY[DEFAULT_QUALITY])
    out = {k: v for k, v in q.items() if k not in ("label", "desc", "refs")}
    out["style"] = item["refs"][:q["refs"]]
    if item.get("style_strength_max"):
        out["style_strength"] = min(out["style_strength"], item["style_strength_max"])
    return out


def _game(rel):
    return os.path.join(GAME, rel.replace("/", os.sep))


def _card_ref(name):
    return os.path.join(CARDS, name.replace("/", os.sep) + ".png")


def _rgb(hex_color):
    h = hex_color.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


# ---------------------------------------------------------------------------------------------------------------
# Style reference kit: small game assets enlarged onto a plain background so Krea sees their style clearly.

def _enlarge(src, dst, canvas=(1024, 1024), fill=0.72, bg=(225, 225, 225), crop=None):
    if os.path.exists(dst):
        return dst
    im = Image.open(_game(src)).convert("RGBA")
    if crop:
        im = im.crop(crop)
    s = min(canvas[0] * fill / im.width, canvas[1] * fill / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    out = Image.new("RGBA", canvas, bg + (255,))
    out.alpha_composite(im, ((canvas[0] - im.width) // 2, (canvas[1] - im.height) // 2))
    out.convert("RGB").save(dst)
    return dst


def _tint(src, dst, colors):
    """A recoloured copy of a reference (Ironclad's red energy orb) in the character's energy colours (character.json
    art.energy_tint: dark, mid, light), so Krea takes the palette from it."""
    if not os.path.exists(dst):
        im = Image.open(src).convert("RGB")
        gray = ImageOps.grayscale(im)
        # Keep the light gray backdrop as is; recolour only the object.
        mask = Image.eval(ImageOps.grayscale(Image.eval(im, lambda v: abs(v - 225))), lambda v: 255 if v > 18 else 0)
        dark, mid, light = (_rgb(c) for c in colors)
        tinted = ImageOps.colorize(gray, black=dark, mid=mid, white=light)
        Image.composite(tinted, im, mask).save(dst)
    return dst


def ensure_kit(ch):
    """The style references for every kind, for one character (only the energy ones depend on the character)."""
    os.makedirs(KIT, exist_ok=True)
    k = lambda name: os.path.join(KIT, name)
    if not os.path.exists(k("orb_ironclad.png")):
        orb = Image.new("RGBA", (256, 256), (225, 225, 225, 255))
        for n in range(1, 6):
            orb.alpha_composite(Image.open(_game(f"images/ui/combat/energy_counters/ironclad/ironclad_orb_layer_{n}.png")).convert("RGBA"))
        orb.resize((1024, 1024), Image.LANCZOS).convert("RGB").save(k("orb_ironclad.png"))
    tint = ch["art"].get("energy_tint", ["2D1900", "D6961E", "FFEEAA"])
    energy = _enlarge("images/packed/sprite_fonts/ironclad_energy_icon.png", k("energy_ironclad.png"), fill=0.6)
    kit = {
        "relic": [_enlarge("images/relics/shovel.png", k("relic_shovel.png")),
                  _enlarge("images/relics/golden_compass.png", k("relic_golden_compass.png"))],
        "potion": [_enlarge(p, k("potion_%d.png" % i)) for i, p in enumerate(
            ["images/potions/" + f for f in sorted(os.listdir(_game("images/potions"))) if f.endswith(".png")][:40:13])],
        "power": [_enlarge(f"images/powers/{n}_power.png", k(f"power_{n}.png")) for n in POWER_REF_POOL],
        "top_icon": [_enlarge("images/ui/top_panel/character_icon_ironclad.png", k("top_ironclad.png"), fill=0.8),
                     _enlarge("images/ui/top_panel/character_icon_silent.png", k("top_silent.png"), fill=0.8)],
        "map_marker": [_enlarge("images/packed/map/icons/map_marker_ironclad.png", k("marker_ironclad.png"), canvas=(832, 1088), fill=0.8),
                       _enlarge("images/packed/map/icons/map_marker_silent.png", k("marker_silent.png"), canvas=(832, 1088), fill=0.8)],
        "char_button": [_enlarge("images/packed/character_select/char_select_silent.png", k("button_silent.png"), canvas=(744, 1122), fill=1.0, crop=(4, 4, 128, 191)),
                        _enlarge("images/packed/character_select/char_select_necrobinder.png", k("button_necrobinder.png"), canvas=(744, 1122), fill=1.0, crop=(4, 4, 128, 191))],
        "figure": [_enlarge("animations/character_select/ironclad/characterselect_ironclad.png", k("ironclad_bust.png"), canvas=(704, 1221), fill=1.0, crop=(2561, 0, 3265, 1221), bg=(150, 40, 30)),
                   _card_ref("ironclad/inflame")],
        "hand": [_enlarge("images/ui/hands/multiplayer_hand_ironclad_point.png", k("hand_point.png"), canvas=(640, 1792), fill=0.98, bg=(40, 200, 60)),
                 _card_ref("ironclad/inflame")],
        "fullscreen": [os.path.join(KIT, "ironclad_bust.png"), _game("animations/character_select/silent/character_select_silent_bg.png")],
        "transition": [_game("images/ui/transitions/ironclad_transition.png")],
        "orb": [_tint(k("orb_ironclad.png"), k(f"orb_{ch.id}.png"), tint)],
        "energy_icon": [_tint(energy, k(f"energy_{ch.id}.png"), tint), os.path.join(KIT, "relic_golden_compass.png")],
        "small_icon": [_tint(energy, k(f"energy_{ch.id}.png"), tint), os.path.join(KIT, "relic_golden_compass.png")],
        "prop": [_card_ref("ironclad/barricade"), _card_ref("ironclad/blood_wall")],
        "decal": [],
    }
    ensure_frames(ch)
    return kit


def _rgb(hex_color):
    return tuple(int(hex_color.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))


def ensure_frames(ch):
    """Card frame previews in portrait space: a 1100x860 overlay; the art sits at (50, 0) at 1000x760.
    Built from the game's portrait borders (card_portrait_border_*_s: 551x420 = 275x210 card units, 4 px per unit),
    tinted in the character's colours (character.json art.energy_tint: dark to between mid and light), one set per
    character in build/art/ref/frames/<id>/."""
    out_dir = os.path.join(FRAMES, ch.id)
    os.makedirs(out_dir, exist_ok=True)
    dark, mid, light = (_rgb(c) for c in ch["art"].get("energy_tint", ["2D1900", "D6961E", "FFEEAA"]))
    highlight = tuple((m + l) // 2 for m, l in zip(mid, light))
    regions = {"attack": (1329, 1, 551, 420), "skill": (1313, 423, 551, 420), "power": (674, 148, 551, 420)}
    atlas = None
    for kind, (x, y, w, h) in regions.items():
        dst = os.path.join(out_dir, kind + ".png")
        if os.path.exists(dst):
            continue
        atlas = atlas or Image.open(_game("images/atlases/ui_atlas_1.png")).convert("RGBA")
        border = atlas.crop((x, y, x + w, y + h)).resize((1100, 840), Image.LANCZOS)
        a = border.getchannel("A")
        # Inside of the window: flood from the middle over transparent pixels.
        import numpy as np
        solid = np.asarray(a) > 60
        inside = art_post._flood(~solid, [(420, 550)])
        mask = np.zeros((860, 1100), bool)
        mask[16:856] = inside
        top = np.nonzero(inside[0])[0]
        if len(top):
            mask[:16, top.min():top.max() + 1] = True
        body = Image.new("RGBA", (1100, 860), (43, 41, 33, 255))
        body.putalpha(Image.fromarray(np.where(mask, 0, 255).astype(np.uint8)))
        tinted = ImageOps.colorize(ImageOps.grayscale(border.convert("RGB")), dark, highlight).convert("RGBA")
        tinted.putalpha(a)
        body.alpha_composite(tinted, (0, 16))
        body.save(dst)
    ensure_ancient_frames()


def ensure_ancient_frames():
    """Ancient cards: the art fills the whole card (606x852) under a rounded mask and a thin glowing border, and a
    dark text box covers the lower half (AncientBorder: 306x440 card units; AncientTextBg: x 21..285, y 201..404)."""
    regions = {"attack": (824, 510, 676, 499), "skill": (1217, 1150, 674, 427), "power": (824, 1, 677, 507)}
    if all(os.path.exists(os.path.join(FRAMES, f"ancient_{k}.png")) for k in regions):
        return
    atlas = Image.open(_game("images/atlases/compressed_0.png")).convert("RGBA")
    W, H = 606, 852
    mask = atlas.crop((615, 1151, 1215, 1998)).resize((W, H), Image.LANCZOS).getchannel("A")
    border = atlas.crop((1, 1, 822, 1149)).resize((W, H), Image.LANCZOS)
    for kind, (x, y, w, h) in regions.items():
        out = Image.new("RGBA", (W, H), (22, 24, 29, 255))
        out.putalpha(mask.point(lambda v: 255 - v))
        box = (round(W * 21 / 306), round(H * 201 / 440), round(W * 285 / 306), round(H * 404 / 440))
        text_bg = atlas.crop((x, y, x + w, y + h)).resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS)
        out.alpha_composite(text_bg, box[:2])
        out.alpha_composite(border)
        out.save(os.path.join(FRAMES, f"ancient_{kind}.png"))


# ---------------------------------------------------------------------------------------------------------------
# Items

def card_prompt(ch, c):
    """Card portrait prompt: the card's art direction, the character's look when the character is in it (character.json
    art.name_in_card_art, e.g. "Donald"), the enemy rule, the archetype's background, framing and the shared style."""
    a = ch["art"]
    art = c["art"].rstrip(".")
    kind = c["type"].lower()
    parts = ["Slay the Spire card illustration."]
    name = a.get("name_in_card_art")
    if name and re.search(rf"\b{re.escape(name)}\b", art):
        parts.append(re.sub(rf"\b{re.escape(name)}\b", a.get("full_name", name), art) + ".")
        parts.append(a["persona_sentence"])
        # How the character looks in this play style (Biden: eyes closed and aviators up when asleep, red lenses as Dark Brandon).
        if a.get("card_notes", {}).get(c.get("arch")):
            parts.append(a["card_notes"][c["arch"]])
    else:
        parts.append(art + ".")
    if a.get("enemy_rule") and (c.get("arch") in a.get("enemy_rule_archetypes", []) or ENEMY_WORDS.search(art)):
        parts.append(a["enemy_rule"])
    backgrounds = a.get("card_backgrounds", {})
    # A card can name its own background (cards.json "art_background"), so cards of one style don't all share a palette.
    background = c.get("art_background") or backgrounds.get(c.get("arch"), backgrounds.get(a.get("default_archetype"), "deep saturated background"))
    parts.append(f"{CARD_ACTION.get(kind, CARD_ACTION['skill']).capitalize()}, {background}.")
    parts.append(FRAMING_ANCIENT if c["rarity"] == "Ancient" else FRAMING)
    parts.append(CARD_STYLE)
    return " ".join(parts)


def _card_refs(ch, arch):
    refs = ch["art"].get("card_refs", {})
    names = refs.get(arch) or refs.get(ch["art"].get("default_archetype")) or ["ironclad/inflame", "defect/momentum_strike", "necrobinder/dirge"]
    return [_card_ref(r) for r in names]


def _sprite_refs(ch, entries):
    """Reference images made from the character's own kept sprites (mod/images/<id>/<name>.png): each entry is a
    list of sprite names, laid side by side on green at one scale (one name: a single figure; several: a pose sheet).
    A name ending in "#head" gives a close-up of the head instead of the whole figure: a look to copy (Dark Brandon's
    red lenses) that the model doesn't count as one more figure to draw in a pose sheet.
    Entries whose sprites don't exist yet are skipped. Rebuilt when a sprite changes."""
    refs = []
    for names in entries:
        files = [n.split("#")[0] for n in names]
        paths = [os.path.join(MOD, "images", ch.id, n + ".png") for n in files]
        if not all(os.path.exists(x) for x in paths):
            continue
        out = os.path.join(REPO, "build", "art", "ref", ch.id, "ref_" + "+".join(n.replace("#", "_") for n in names) + ".png")
        if not os.path.exists(out) or os.path.getmtime(out) < max(os.path.getmtime(x) for x in paths):
            os.makedirs(os.path.dirname(out), exist_ok=True)
            sprites = [Image.open(x).convert("RGBA") for x in paths]
            if len(names) == 1 and names[0].endswith("#head"):
                sp = sprites[0]
                bbox = sp.getbbox() or (0, 0, sp.width, sp.height)
                top, height = bbox[1], bbox[3] - bbox[1]
                head = sp.crop((bbox[0], top, bbox[2], top + int(height * 0.3)))
                head = head.crop(head.getbbox() or (0, 0, head.width, head.height))
                scale = min(900 / head.width, 900 / head.height)
                head = head.resize((round(head.width * scale), round(head.height * scale)), Image.LANCZOS)
                canvas = Image.new("RGBA", (1024, 1024), (40, 200, 60, 255))
                canvas.alpha_composite(head, ((1024 - head.width) // 2, (1024 - head.height) // 2))
                canvas.convert("RGB").save(out)
                refs.append(out)
                continue
            h_max = max(sp.height for sp in sprites)
            height = 1024 if len(sprites) > 1 else 1216
            scale = (height - 80) / h_max
            sprites = [sp.resize((max(1, round(sp.width * scale)), max(1, round(sp.height * scale))), Image.LANCZOS) for sp in sprites]
            gap = 60
            width = max(832, sum(sp.width for sp in sprites) + gap * (len(sprites) + 1))
            canvas = Image.new("RGBA", (width, height), (40, 200, 60, 255))
            x = (width - sum(sp.width for sp in sprites) - gap * (len(sprites) - 1)) // 2
            for sp in sprites:
                canvas.alpha_composite(sp, (x, height - 40 - sp.height))
                x += sp.width + gap
            canvas.convert("RGB").save(out)
        refs.append(out)
    return refs


def current_refs(ch, item):
    """An item's references as of now. Those made from the character's own kept sprites are rebuilt from the sprites in
    mod/ (a Keep changes them), so a regeneration never uses an outdated idle."""
    if not item.get("sprite_refs"):
        return item["refs"]
    own = _sprite_refs(ch, item["sprite_refs"])
    return (own or item["extra_refs"]) if item["kind"] == "figure_sheet" else (own + item["extra_refs"])[:3]


def _pick_refs(pool, key, n=3):
    """n references from a pool, a fixed choice per item (the same on every run), spread over the pool."""
    return random.Random(zlib.crc32(key.encode())).sample(pool, min(n, len(pool)))


def load_items(ch):
    """All items of one character (a presmod.Character) in display order, each a dict with the recipe filled in."""
    if isinstance(ch, str):
        ch = presmod.character(ch)
    kit = ensure_kit(ch)
    a = ch["art"]
    persona = a["persona"]
    items = []
    assets = ch.assets
    design = ch.cards

    def add(spec=None, **it):
        it.setdefault("method", "styleref")
        it["outputs"] = presmod.art_outputs(ch, it["id"].split(":", 1)[1], it["kind"], spec)
        items.append(it)

    def fill(text):
        return text.replace("{persona}", persona)

    for s in assets.get("character", []):
        kind = s["kind"]
        art = fill(s["art"])
        base = dict(id="char:" + s["id"], section="Character", name=s["name"], sub=kind.replace("_", " "),
                    art=s["art"].replace("{persona}", ch["name"]), text="", kind=kind)
        if kind == "char_button":
            add(s, **base, refs=kit["char_button"], size=[832, 1216], prompt=(
                f"{art[0].upper() + art[1:]}. Bold simplified painting with big flat color shapes, graphic hard-edged shading, "
                "a thin bright rim light, strong dark outline accents, in the exact style of the reference images."))
        elif kind == "fullscreen":
            add(s, **base, refs=kit["fullscreen"], size=[1792, 832], prompt=(
                f"Slay the Spire character select screen painting. {art}. Painterly digital painting with bold flat color shapes, "
                "visible brush strokes, dramatic rim light and deep shadows, in the exact style of the reference images."),
                note="Only the right 3/4 is on screen: keep the subject in the right half.")
        elif kind == "top_icon":
            add(s, **base, refs=kit["top_icon"], size=[1024, 1024], prompt=(
                f"Tiny game UI icon of {art}, simple bold shapes, flat cel shading, thick dark outline, {LIGHT_BG}, "
                "in the exact style of the reference images."))
        elif kind == "map_marker":
            add(s, **base, refs=kit["map_marker"], size=[832, 1088], prompt=(
                f"Tiny game map marker icon: {art}, simple bold shapes, flat cel shading, thick dark outline, {LIGHT_BG}, "
                "in the exact style of the reference images."))
        elif kind == "figure":
            own = _sprite_refs(ch, s.get("refs", []))
            add(s, **base, refs=(own + kit["figure"])[:3], sprite_refs=s.get("refs", []), extra_refs=kit["figure"],
                size=[832, 1216], prompt=(
                f"Full-body game character art of {persona}, {art}. The whole figure is visible from head to shoes, "
                f"facing right, {GREEN_BG}. Painterly digital painting with bold flat color shapes, hard-edged shading "
                "and warm rim light, in the exact style of the reference images."),
                note="A sprite placed by its feet: combat poses, shop and rest site (Framework/Patches/ArtPatches.cs)."
                     + (" References: the character's own kept sprites, so the proportions match." if own else ""))
        elif kind == "figure_sheet":
            # Every pose of a set in one image, so they share one scale, one head size and one style; cut apart on
            # generation (art_post.pose_sheet). References: the character's own kept sprites (identity, proportions).
            poses = s["poses"]
            n = len(poses)
            words = {2: "two", 3: "three", 4: "four", 5: "five", 6: "six"}.get(n, str(n))
            own = _sprite_refs(ch, s.get("refs", []))
            add(s, **base, refs=own or kit["figure"], sprite_refs=s.get("refs", []), extra_refs=kit["figure"],
                size=s.get("size", [512 * n, 1024]), poses=poses, prompt=(
                f"Game character pose sheet: {words} full-body poses of the same character in {words} equal columns from left "
                "to right, with wide empty green gaps between the figures and empty space above every head, none touching the "
                "edges, all at exactly the same scale with identical body proportions and head size, standing on the same "
                f"baseline, every pose facing right. The character is {persona}. {art} Isolated on a plain flat bright green "
                "background, no ground, no shadow, no text, no labels. Painterly digital painting with bold flat color "
                "shapes, hard-edged shading and warm rim light, in the exact style of the reference images."),
                note=f"One image with all {n} poses, cut apart when it arrives; every pose gets the same height, so the game "
                     "shows them at one scale. A sheet with touching figures or an extra figure shows an error: regenerate it.")
        elif kind == "hand":
            sleeve = a.get("sleeve", "a sleeve of the character's outfit")
            add(s, **base, refs=kit["hand"], size=[640, 1792], prompt=(
                f"Game UI art of a single arm reaching up from the bottom edge of the frame: {sleeve}, the hand {art}. "
                f"Vertical composition, the arm fills the height of the frame, {GREEN_BG}. Painterly digital painting with "
                "bold flat color shapes and hard-edged shading, in the exact style of the reference images."))
        elif kind == "transition":
            add(s, **base, refs=kit["transition"], size=[1792, 832], prompt=(
                f"Abstract grayscale texture: {art}. Black, white and gray only, soft painted smoky shapes filling the whole "
                "frame, no text, in the exact style of the reference image."),
                note="Grayscale dissolve mask for the screen transition material.")

    for c in design.get("cards", []):
        ancient = c["rarity"] == "Ancient"
        sub = f"{c['type']} · {c['rarity']} · {c.get('arch', '')}"
        add(id="card:" + presmod.slug(c["id"]), section="Cards", kind="card_ancient" if ancient else "card", name=c["name"], sub=sub,
            arch=c.get("arch"), ctype=c["type"].lower(), rarity=c["rarity"], text=c["text"], art=c.get("art", c["name"]),
            refs=_card_refs(ch, c.get("arch")), size=[832, 1168] if ancient else [1216, 928], prompt=card_prompt(ch, dict(c, art=c.get("art", c["name"]))),
            frame=("ancient_" if ancient else f"{ch.id}/") + c["type"].lower(), frameMode="full" if ancient else "window",
            note="Ancient cards use full-card art (606x852); the text box covers the lower half." if ancient else "")

    for r in assets.get("relics", []):
        add(r, id="relic:" + r["id"], section="Relics", kind="relic", name=r["name"], sub="relic", art=r["art"],
            text=next((x["text"] for x in design.get("relics", []) if presmod.slug(x["id"]) == r["id"]), ""), refs=kit["relic"], size=[1024, 1024],
            prompt=(f"Game item icon of a single {r['art']}, centered, filling most of the frame, {LIGHT_BG}. Hand-painted digital "
                    "painting with soft brush texture and simple shading, in the exact style of the reference images."))
    for p in assets.get("potions", []):
        add(p, id="potion:" + p["id"], section="Potions", kind="potion", name=p["name"], sub="potion", art=p["art"],
            text=next((x["text"] for x in design.get("potions", []) if presmod.slug(x["id"]) == p["id"]), ""), refs=kit["potion"], size=[1024, 1024],
            prompt=(f"Game potion icon: {p['art']}, centered, filling most of the frame, {LIGHT_BG}. Flat cel-shaded painting with "
                    "a thin light inner outline, glass highlights and simple bold shapes, in the exact style of the reference images."))
    for p in assets.get("powers", []):
        add(p, id="power:" + p["id"], section="Powers", kind="power", name=p["name"], sub="power icon", art=p["art"], text="",
            refs=_pick_refs(kit["power"], p["id"]), style_strength_max=POWER_STYLE_STRENGTH, size=[1024, 1024],
            prompt=(f"Game status effect icon: {p['art']}. One bold simple symbol, centered, filling the frame, {LIGHT_BG}. Flat "
                    "vector-like cel shading, thick black outline, bright saturated colors, very simple readable shapes, "
                    "in the exact style of the reference images."))

    for u in assets.get("ui", []):
        kind = u["kind"]
        base = dict(id="ui:" + u["id"], section="UI & mechanics", kind=kind, name=u["name"], sub=kind.replace("_", " "), art=fill(u["art"]), text="")
        if kind == "orb":
            add(u, **base, refs=kit["orb"], size=[1024, 1024], layers=u["layers"], prompt=(
                f"Game UI element: {u['art']}, centered, {LIGHT_BG}. Flat cel shading with bold simple shapes, "
                "in the exact style of the reference image."),
                note="Layers of the energy counter (scenes/combat/energy_counters/<id>_energy_counter.tscn).")
        elif kind in ("energy_icon", "small_icon"):
            add(u, **base, refs=kit[kind], size=[1024, 1024], sizes=u.get("sizes", {}), prompt=(
                f"Tiny game UI icon: {u['art']}, centered, filling the frame, {LIGHT_BG}. Simple bold shapes, flat cel shading, "
                "thick dark outline, readable at very small size, in the exact style of the reference images."))
        elif kind == "prop":
            add(u, **base, refs=kit["prop"], size=[1344, 768], fit=u.get("fit", [640, 400]), prompt=(
                f"Side view game art of {fill(u['art'])}, filling the width of the frame, {GREEN_BG}. Painterly "
                "digital painting with bold flat color shapes and hard-edged shading, in the exact style of the reference images."),
                note=u.get("note", ""))
        elif kind == "decal":
            art = fill(u["art"])
            add(u, **base, method="t2i", refs=[], size=[1216, 704], fit=u.get("fit", [512, 300]), prompt=(
                f"{art[0].upper() + art[1:]}, isolated on a plain flat white background, nothing else in the image."),
                note=u.get("note", ""))
        else:
            raise ValueError(f"{ch.id}: unknown art kind '{kind}' for ui item {u['id']}")
    return items


# ---------------------------------------------------------------------------------------------------------------
# Post-processing: raw generation -> game-ready files. Returns {output key: local file} plus a preview path.

def _outline(icon, width=3):
    a = icon.getchannel("A").point(lambda v: 255 if v > 40 else 0)
    grown = a.filter(ImageFilter.MaxFilter(width * 2 + 1))
    out = Image.new("RGBA", icon.size, (255, 255, 255, 0))
    out.putalpha(grown)
    return out


SMALL_ICON_SIZES = {"text": 24, "gem": 74, "badge": 64}


def postprocess(item, raw, out_dir, stem):
    """Writes the game-ready files for one generation into out_dir. Returns (files, preview) where files maps
    each output key to a local path and preview is the image the review UI shows."""
    kind = item["kind"]
    im = Image.open(raw).convert("RGB")
    p = lambda key, ext="png": os.path.join(out_dir, f"{stem}_{key}.{ext}")
    files = {}
    if kind == "card":
        art_post.fit_cover(im, 1000, 760).save(p("portrait"))
        files["portrait"] = p("portrait")
    elif kind == "card_ancient":
        art_post.fit_cover(im, 606, 852).save(p("portrait"))
        files["portrait"] = p("portrait")
    elif kind in ("relic", "potion", "power"):
        halo = {"relic": 12, "potion": 9, "power": 8}[kind]
        art_post.icon(im, p("icon"), outline=halo)
        files["icon"] = p("icon")
    elif kind == "char_button":
        art_post.portrait(raw, p("button"))
        files = {"button": p("button"), "locked": p("button")[:-4] + "_locked.png"}
    elif kind == "fullscreen":
        art_post.fit_cover(im, 2560, 1200).save(p("image"))
        files["image"] = p("image")
    elif kind == "transition":
        art_post.fit_cover(ImageOps.grayscale(im), 2560, 1200).save(p("mask"))
        files["mask"] = p("mask")
    elif kind == "top_icon":
        icon = art_post.keyed_fit(im, (88, 88), margin=3)
        icon.save(p("icon"))
        _outline(icon).save(p("outline"))
        files = {"icon": p("icon"), "outline": p("outline")}
    elif kind == "map_marker":
        art_post.keyed_fit(im, (49, 64), margin=2).save(p("marker"))
        files["marker"] = p("marker")
    elif kind == "figure":
        art_post.keyed_fit(im, None, green=True).save(p("sprite"))
        files["sprite"] = p("sprite")
    elif kind == "figure_sheet":
        sprites = art_post.pose_sheet(im, len(item["poses"]))
        for pose, sprite in zip(item["poses"], sprites):
            sprite.save(p(pose))
            files[pose] = p(pose)
        # The preview: the cut poses side by side, as the game will show them (one scale).
        gap = 24
        sheet = Image.new("RGBA", (sum(sp.width for sp in sprites) + gap * (len(sprites) - 1), sprites[0].height), (0, 0, 0, 0))
        x = 0
        for sp in sprites:
            sheet.alpha_composite(sp, (x, 0))
            x += sp.width + gap
        sheet.save(p("sheet"))
        return files, p("sheet")
    elif kind == "hand":
        art_post.keyed_fit(im, (422, 1200), margin=0, green=True, anchor="bottom").save(p("hand"))
        files["hand"] = p("hand")
    elif kind == "prop":
        key = next(iter(item["outputs"]))
        art_post.keyed_fit(im, tuple(item.get("fit", [640, 400])), margin=4, green=True, anchor="bottom").save(p(key))
        files[key] = p(key)
    elif kind == "decal":
        key = next(iter(item["outputs"]))
        art_post.keyed_fit(im, tuple(item.get("fit", [512, 300])), margin=4).save(p(key))
        files[key] = p(key)
    elif kind == "orb":
        base = art_post.keyed_fit(im, (256, 256), margin=4)
        for n in item["layers"]:
            layer = base
            if n == 3:  # the inner swirl: smaller and turned
                small = base.resize((180, 180), Image.LANCZOS).rotate(40, resample=Image.BICUBIC)
                layer = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
                layer.alpha_composite(small, (38, 38))
            elif n == 5:  # the thin outer detail: darker and fainter
                layer = ImageEnhance.Brightness(base).enhance(0.55)
                layer.putalpha(base.getchannel("A").point(lambda v: v * 6 // 10))
            layer.save(p(f"layer{n}"))
            files[f"layer{n}"] = p(f"layer{n}")
    elif kind in ("energy_icon", "small_icon"):
        big = art_post.keyed_fit(im, (256, 256), margin=4)
        sizes = dict(SMALL_ICON_SIZES, **item.get("sizes", {}))
        for key in item["outputs"]:
            big.resize((sizes[key], sizes[key]), Image.LANCZOS).save(p(key))
            files[key] = p(key)
    else:
        raise ValueError(kind)
    preview = files[next(iter(files))]
    return files, preview

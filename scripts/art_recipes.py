"""Every piece of art the mod needs, and how to make each one (Step 7.5).

An item = what to draw (cards.json for card portraits, docs/design/art_assets.json for everything else) plus a recipe:
style references from the unpacked game, a prompt template, a generation size, post-processing and the mod paths
the finished files go to. Used by scripts/art_review.py.
"""
import json
import os
import re

from PIL import Image, ImageEnhance, ImageFilter, ImageOps

import art_post

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME = os.path.join(REPO, "re", "pck")
MOD = os.path.join(REPO, "mod")
KIT = os.path.join(REPO, "build", "art", "ref", "kit")      # prepared style references
FRAMES = os.path.join(REPO, "build", "art", "ref", "frames")  # card frame overlays for the preview
CARDS = os.path.join(GAME, "images", "packed", "card_portraits")

DONALD = "Donald Trump as a comic caricature, with his iconic swooping blond comb-over, orange-tan face, pouting lips, navy suit and an extra-long red tie"
DONALD_LOOK = ("Donald Trump is drawn as a comic caricature with his iconic swooping blond comb-over, orange-tan face, "
               "pouting lips, navy suit and an extra-long red tie.")
MONSTERS = ("Every enemy, monster or foe is a Slay the Spire fantasy creature (cultists, slimes, goblins, beetles, "
            "jaw worms), never a human.")
CARD_STYLE = ("Flat cel-shaded digital painting with hard-edged dark shadow shapes, dark outlines, strong readable "
              "silhouette, saturated limited palette, in the exact painting style of the reference images.")
FRAMING = "Keep the main subject centered and in the upper two thirds of the image."
FRAMING_ANCIENT = ("Tall full-card art: keep the main subject and faces in the upper half of the image; the lower half "
                   "is covered by the card's text box, so keep it simple background there.")
LIGHT_BG = "isolated on a plain flat light gray background, no shadow, no text"
GREEN_BG = "isolated on a plain flat bright green background, no ground, no shadow, no text"

# Card references per archetype: three game cards whose subject matter is close.
CARD_REFS = {
    "Wall": ["regent/heirloom_hammer", "ironclad/inflame", "ironclad/bludgeon"],
    "Deport": ["colorless/rolling_boulder", "silent/snakebite", "ironclad/break"],
    "Deals": ["colorless/hand_of_greed", "colorless/the_bomb", "ironclad/sword_boomerang"],
    "Tweets": ["defect/signal_boost", "defect/hologram", "defect/tempest"],
    "General": ["ironclad/inflame", "defect/momentum_strike", "necrobinder/dirge"],
}
CARD_BG = {
    "Wall": "radial red and orange burst background",
    "Deport": "deep purple and red background",
    "Deals": "dark purple background with a golden orange burst",
    "Tweets": "electric blue and purple background",
    "General": "warm crimson and navy background",
}
CARD_ACTION = {"attack": "dynamic diagonal action with impact lines", "skill": "a clear, readable scene", "power": "an iconic, glowing, almost symmetrical composition"}
ENEMY_WORDS = re.compile(r"\b(enem|monster|foe|goblin|slime|beetle|cultist|elite|silhouette|crowd)", re.I)


def slug(class_name):
    return re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", class_name).lower()


def _game(rel):
    return os.path.join(GAME, rel.replace("/", os.sep))


def _card_ref(name):
    return os.path.join(CARDS, name.replace("/", os.sep) + ".png")


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


def _gold(src, dst):
    """A gold-recoloured copy of a red reference (Ironclad's energy orb), so Krea takes the gold palette from it."""
    if not os.path.exists(dst):
        im = Image.open(src).convert("RGB")
        bg = Image.new("L", im.size, 0)
        gray = ImageOps.grayscale(im)
        # Keep the light gray backdrop as is; recolour only the object.
        mask = Image.eval(ImageOps.grayscale(Image.eval(im, lambda v: abs(v - 225))), lambda v: 255 if v > 18 else 0)
        gold = ImageOps.colorize(gray, black=(45, 25, 0), mid=(214, 150, 30), white=(255, 238, 170))
        Image.composite(gold, im, mask).save(dst)
    return dst


def ensure_kit():
    os.makedirs(KIT, exist_ok=True)
    k = lambda name: os.path.join(KIT, name)
    if not os.path.exists(k("orb_ironclad.png")):
        orb = Image.new("RGBA", (256, 256), (225, 225, 225, 255))
        for n in range(1, 6):
            orb.alpha_composite(Image.open(_game(f"images/ui/combat/energy_counters/ironclad/ironclad_orb_layer_{n}.png")).convert("RGBA"))
        orb.resize((1024, 1024), Image.LANCZOS).convert("RGB").save(k("orb_ironclad.png"))
    kit = {
        "relic": [_enlarge("images/relics/shovel.png", k("relic_shovel.png")),
                  _enlarge("images/relics/golden_compass.png", k("relic_golden_compass.png"))],
        "potion": [_enlarge(p, k("potion_%d.png" % i)) for i, p in enumerate(
            ["images/potions/" + f for f in sorted(os.listdir(_game("images/potions"))) if f.endswith(".png")][:40:13])],
        "power": [_enlarge("images/powers/strength_power.png", k("power_strength.png")),
                  _enlarge("images/powers/thorns_power.png", k("power_thorns.png")),
                  _enlarge("images/powers/barricade_power.png", k("power_barricade.png"))],
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
        "orb": [_gold(k("orb_ironclad.png"), k("orb_gold.png"))],
        "small_icon": [_gold(_enlarge("images/packed/sprite_fonts/ironclad_energy_icon.png", k("energy_ironclad.png"), fill=0.6), k("energy_gold.png")),
                       os.path.join(KIT, "relic_golden_compass.png")],
        "wall": [_card_ref("ironclad/barricade"), _card_ref("ironclad/blood_wall")],
    }
    ensure_frames()
    return kit


def ensure_frames():
    """Card frame previews in portrait space: a 1100x860 overlay; the art sits at (50, 0) at 1000x760.
    Built from the game's portrait borders (card_portrait_border_*_s: 551x420 = 275x210 card units, 4 px per unit)."""
    os.makedirs(FRAMES, exist_ok=True)
    regions = {"attack": (1329, 1, 551, 420), "skill": (1313, 423, 551, 420), "power": (674, 148, 551, 420)}
    atlas = None
    for kind, (x, y, w, h) in regions.items():
        dst = os.path.join(FRAMES, kind + ".png")
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
        gold = ImageOps.colorize(ImageOps.grayscale(border.convert("RGB")), (70, 50, 10), (250, 215, 110)).convert("RGBA")
        gold.putalpha(a)
        body.alpha_composite(gold, (0, 16))
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

def _load_json(rel):
    with open(os.path.join(REPO, rel), encoding="utf-8") as f:
        return json.load(f)


def _donald(text):
    return text.replace("THE_DONALD", DONALD)


def card_prompt(c):
    art = c["art"].rstrip(".")
    kind = c["type"].lower()
    parts = ["Slay the Spire card illustration."]
    if re.search(r"\bDonald\b", art):
        parts.append(re.sub(r"\bDonald\b", "Donald Trump", art) + ".")
        parts.append(DONALD_LOOK)
    else:
        parts.append(art + ".")
    if c["arch"] == "Deport" or ENEMY_WORDS.search(art):
        parts.append(MONSTERS)
    parts.append(f"{CARD_ACTION.get(kind, CARD_ACTION['skill']).capitalize()}, {CARD_BG.get(c['arch'], CARD_BG['General'])}.")
    parts.append(FRAMING_ANCIENT if c["rarity"] == "Ancient" else FRAMING)
    parts.append(CARD_STYLE)
    return " ".join(parts)


def load_items():
    """All items in display order, each a dict with the recipe filled in."""
    kit = ensure_kit()
    items = []
    assets = _load_json("docs/design/art_assets.json")
    design = _load_json("docs/design/cards.json")

    def add(**it):
        it.setdefault("method", "styleref")
        items.append(it)

    for a in assets["character"]:
        kind = a["kind"]
        art = _donald(a["art"])
        base = dict(id="char:" + a["id"], section="Character", name=a["name"], sub=kind.replace("_", " "),
                    art=a["art"].replace("THE_DONALD", "The Donald"), text="")
        if kind == "char_button":
            add(**base, kind=kind, refs=kit["char_button"], size=[832, 1216], prompt=(
                f"{art[0].upper() + art[1:]}. Bold simplified painting with big flat color shapes, graphic hard-edged shading, "
                "a thin bright rim light, strong dark outline accents, in the exact style of the reference images."),
                outputs={"button": "images/packed/character_select/char_select_trump.png",
                         "locked": "images/packed/character_select/char_select_trump_locked.png"})
        elif kind == "fullscreen":
            add(**base, kind=kind, refs=kit["fullscreen"], size=[1792, 832], prompt=(
                f"Slay the Spire character select screen painting. {art}. Painterly digital painting with bold flat color shapes, "
                "visible brush strokes, dramatic rim light and deep shadows, in the exact style of the reference images."),
                outputs={"image": "images/trump/char_select_bg.png"}, note="Step 8 wires this into the character select scene.")
        elif kind == "top_icon":
            add(**base, kind=kind, refs=kit["top_icon"], size=[1024, 1024], prompt=(
                f"Tiny game UI icon of {art}, simple bold shapes, flat cel shading, thick dark outline, {LIGHT_BG}, "
                "in the exact style of the reference images."),
                outputs={"icon": "images/ui/top_panel/character_icon_trump.png",
                         "outline": "images/ui/top_panel/character_icon_trump_outline.png"})
        elif kind == "map_marker":
            add(**base, kind=kind, refs=kit["map_marker"], size=[832, 1088], prompt=(
                f"Tiny game map marker icon: {art}, simple bold shapes, flat cel shading, thick dark outline, {LIGHT_BG}, "
                "in the exact style of the reference images."),
                outputs={"marker": "images/packed/map/icons/map_marker_trump.png"})
        elif kind == "figure":
            add(**base, kind=kind, refs=kit["figure"], size=[832, 1216], prompt=(
                f"Full-body game character art of {DONALD}, {a['art']}. The whole figure is visible from head to shoes, "
                f"facing right, {GREEN_BG}. Painterly digital painting with bold flat color shapes, hard-edged shading "
                "and warm rim light, in the exact style of the reference images."),
                outputs={"sprite": f"images/trump/{a['id']}.png"},
                note="Source art: Step 8 turns it into the combat rig (or sprite fallback), shop and rest-site scenes.")
        elif kind == "hand":
            gesture = a["id"].split("_")[1]
            add(**base, kind=kind, refs=kit["hand"], size=[640, 1792], prompt=(
                "Game UI art of a single arm reaching up from the bottom edge of the frame: a navy suit sleeve with a white "
                f"shirt cuff and a gold cufflink, the hand {a['art']}. Vertical composition, the arm fills the height of the "
                f"frame, {GREEN_BG}. Painterly digital painting with bold flat color shapes and hard-edged shading, "
                "in the exact style of the reference images."),
                outputs={"hand": f"images/ui/hands/multiplayer_hand_trump_{gesture}.png"})
        elif kind == "transition":
            add(**base, kind=kind, refs=kit["transition"], size=[1792, 832], prompt=(
                f"Abstract grayscale texture: {art}. Black, white and gray only, soft painted smoky shapes filling the whole "
                "frame, no text, in the exact style of the reference image."),
                outputs={"mask": "images/ui/transitions/trump_transition.png"},
                note="Grayscale dissolve mask for the screen transition material.")

    for c in design["cards"]:
        ancient = c["rarity"] == "Ancient"
        sub = f"{c['type']} · {c['rarity']} · {c['arch']}"
        add(id="card:" + slug(c["id"]), section="Cards", kind="card_ancient" if ancient else "card", name=c["name"], sub=sub,
            arch=c["arch"], ctype=c["type"].lower(), rarity=c["rarity"], text=c["text"], art=c["art"],
            refs=[_card_ref(r) for r in CARD_REFS.get(c["arch"], CARD_REFS["General"])],
            size=[832, 1168] if ancient else [1216, 928], prompt=card_prompt(c),
            outputs={"portrait": f"images/packed/card_portraits/trump/{slug(c['id'])}.png"},
            frame=("ancient_" if ancient else "") + c["type"].lower(), frameMode="full" if ancient else "window",
            note="Ancient cards use full-card art (606x852); the text box covers the lower half." if ancient else "")

    for r in assets["relics"]:
        add(id="relic:" + r["id"], section="Relics", kind="relic", name=r["name"], sub="relic", art=r["art"],
            text=next((x["text"] for x in design["relics"] if slug(x["id"]) == r["id"]), ""), refs=kit["relic"], size=[1024, 1024],
            prompt=(f"Game item icon of a single {r['art']}, centered, filling most of the frame, {LIGHT_BG}. Hand-painted digital "
                    "painting with soft brush texture and simple shading, in the exact style of the reference images."),
            outputs={"icon": f"images/relics/{r['id']}.png"})
    for p in assets["potions"]:
        add(id="potion:" + p["id"], section="Potions", kind="potion", name=p["name"], sub="potion", art=p["art"],
            text=next((x["text"] for x in design["potions"] if slug(x["id"]) == p["id"]), ""), refs=kit["potion"], size=[1024, 1024],
            prompt=(f"Game potion icon: {p['art']}, centered, filling most of the frame, {LIGHT_BG}. Flat cel-shaded painting with "
                    "a thin light inner outline, glass highlights and simple bold shapes, in the exact style of the reference images."),
            outputs={"icon": f"images/potions/{p['id']}.png"})
    for p in assets["powers"]:
        add(id="power:" + p["id"], section="Powers", kind="power", name=p["name"], sub="power icon", art=p["art"], text="",
            refs=kit["power"], size=[1024, 1024],
            prompt=(f"Game status effect icon: {p['art']}. One bold simple symbol, centered, filling the frame, {LIGHT_BG}. Flat "
                    "vector-like cel shading, thick black outline, bright saturated colors, very simple readable shapes, "
                    "in the exact style of the reference images."),
            outputs={"icon": f"images/powers/{p['id']}.png"})

    for u in assets["ui"]:
        kind = u["kind"]
        base = dict(id="ui:" + u["id"], section="UI & mechanics", kind=kind, name=u["name"], sub=kind.replace("_", " "), art=u["art"], text="")
        if kind == "orb":
            add(**base, refs=kit["orb"], size=[1024, 1024], layers=u["layers"], prompt=(
                f"Game UI element: {u['art']}, centered, {LIGHT_BG}. Flat cel shading with bold simple shapes, "
                "in the exact style of the reference image."),
                outputs={f"layer{n}": f"images/ui/combat/energy_counters/trump/trump_orb_layer_{n}.png" for n in u["layers"]},
                note="Step 8 points the energy counter scene at these layers.")
        elif kind == "small_icon":
            outs = ({"text": "images/packed/sprite_fonts/trump_energy_icon.png",
                     "gem": "trump_character/atlas_fallback/ui_atlas/card/energy_trump.png"} if u["id"] == "energy_icon"
                    else {"badge": "trump_character/ui/gold_cost_icon.png"})
            add(**base, refs=kit["small_icon"], size=[1024, 1024], prompt=(
                f"Tiny game UI icon: {u['art']}, centered, filling the frame, {LIGHT_BG}. Simple bold shapes, flat cel shading, "
                "thick dark outline, readable at very small size, in the exact style of the reference images."),
                outputs=outs)
        elif kind == "wall":
            add(**base, refs=kit["wall"], size=[1344, 768], prompt=(
                f"Side view game art of {u['art']}, a wide wall segment filling the width of the frame, {GREEN_BG}. Painterly "
                "digital painting with bold flat color shapes and hard-edged shading, in the exact style of the reference images."),
                outputs={"sprite": f"images/trump/wall/{u['id']}.png"}, note="Step 8 shows these in front of the character.")
        elif kind == "stamp":
            add(**base, method="t2i", refs=[], size=[1216, 704], prompt=(
                f"{u['art'][0].upper() + u['art'][1:]}, isolated on a plain flat white background, nothing else in the image."),
                outputs={"stamp": f"images/trump/ui/{u['id']}.png"}, note="Step 8 swaps the drawn DENIED stamp for this.")
    return items


# ---------------------------------------------------------------------------------------------------------------
# Post-processing: raw generation -> game-ready files. Returns {output key: local file} plus a preview path.

def _outline(icon, width=3):
    a = icon.getchannel("A").point(lambda v: 255 if v > 40 else 0)
    grown = a.filter(ImageFilter.MaxFilter(width * 2 + 1))
    out = Image.new("RGBA", icon.size, (255, 255, 255, 0))
    out.putalpha(grown)
    return out


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
    elif kind == "hand":
        art_post.keyed_fit(im, (422, 1200), margin=0, green=True, anchor="bottom").save(p("hand"))
        files["hand"] = p("hand")
    elif kind == "wall":
        art_post.keyed_fit(im, (640, 400), margin=4, green=True, anchor="bottom").save(p("sprite"))
        files["sprite"] = p("sprite")
    elif kind == "stamp":
        art_post.keyed_fit(im, (512, 300), margin=4).save(p("stamp"))
        files["stamp"] = p("stamp")
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
    elif kind == "small_icon":
        big = art_post.keyed_fit(im, (256, 256), margin=4)
        sizes = {"text": 24, "gem": 74, "badge": 64}
        for key in item["outputs"]:
            big.resize((sizes[key], sizes[key]), Image.LANCZOS).save(p(key))
            files[key] = p(key)
    else:
        raise ValueError(kind)
    preview = files[next(iter(files))]
    return files, preview

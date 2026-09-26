"""Shared configuration for every script: repo paths, the mod (mod.json) and its characters (characters/<id>/).

  import presmod
  presmod.MOD_ID                  "pres_mod"
  presmod.character_ids()         ["trump", ...]  (folders in characters/ that don't start with "_")
  presmod.character("trump")      character.json plus derived paths (see Character)
  presmod.art_outputs(char, item) mod paths an art item's finished files go to

A character is identified by its lowercase id ("trump"): the model ID entry lowercased, which is also what the game
uses in every asset path (images/packed/card_portraits/trump/, scenes/creature_visuals/trump.tscn, ...).
"""
import json
import os
import re

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD_DIR = os.path.join(REPO, "mod")                       # the Godot project that becomes the PCK
CHARACTERS_DIR = os.path.join(REPO, "characters")
GAME_PCK = os.path.join(REPO, "re", "pck")                # the unpacked game (Step 1), for style references
BUILD = os.path.join(REPO, "build")


def _load_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


# Paths that differ per PC: an environment variable (the name in capitals, e.g. STS2_GAME_DIR), else
# local_settings.json at the repo root (git-ignored; copy local_settings.example.json), else the default
# (the original author's PC). docs/SETUP.md explains each one.
_LOCAL_SETTINGS = os.path.join(REPO, "local_settings.json")
_LOCAL = _load_json(_LOCAL_SETTINGS) if os.path.exists(_LOCAL_SETTINGS) else {}


def setting(name, default):
    return os.environ.get(name.upper()) or _LOCAL.get(name) or default


GAME_DIR = setting("sts2_game_dir", r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2")
COMFY_DIR = setting("comfy_dir", r"D:\Comfy-Desktop\ComfyUI-Installs\ComfyUI\ComfyUI")   # the folder with main.py
COMFY_PYTHON = setting("comfy_python", os.path.join(COMFY_DIR, ".venv", "Scripts", "python.exe"))
# Comfy Desktop keeps models in a shared folder listed in this file; other installs don't need it.
COMFY_MODELS_YAML = setting("comfy_models_yaml", os.path.join(os.environ.get("APPDATA", ""), "Comfy Desktop", "shared_model_paths.yaml"))
COMFY_LORA_DIR = setting("comfy_lora_dir", r"D:\Comfy-Desktop\ComfyUI-Shared\models\loras")   # art_train.py writes LoRAs here
# The trailer's video tools (Step 12): ffmpeg from gyan.dev, unzipped into tools/ffmpeg/
FFMPEG = setting("ffmpeg", os.path.join(REPO, "tools", "ffmpeg", "bin", "ffmpeg.exe"))
FFPROBE = os.path.join(os.path.dirname(FFMPEG), "ffprobe.exe")


MOD = _load_json(os.path.join(REPO, "mod.json"))
MOD_ID = MOD["id"]
SRC_DIR = os.path.join(MOD_DIR, MOD_ID, "src")


def slug(class_name):
    """The game's model ID entry, lowercased: GoldenShovel -> golden_shovel."""
    return re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", class_name).lower()


def character_ids():
    """Every character of the mod, by lowercase id, in folder order (characters/_template is skipped)."""
    return sorted(d for d in os.listdir(CHARACTERS_DIR)
                  if not d.startswith(("_", ".")) and os.path.exists(os.path.join(CHARACTERS_DIR, d, "character.json")))


class Character(dict):
    """characters/<id>/character.json, plus:
      .id        lowercase id ("trump")          .entry   model ID entry ("TRUMP")
      .dir       characters/<id>/                .src     mod/pres_mod/src/Characters/<Folder>/
      .cards     design/cards.json (dict)        .assets  art/art_assets.json (dict)
    """

    @property
    def id(self):
        return self["id"].lower()

    @property
    def entry(self):
        return self["id"].upper()

    @property
    def dir(self):
        return os.path.join(CHARACTERS_DIR, self.id)

    @property
    def src(self):
        return os.path.join(SRC_DIR, "Characters", self["folder"])

    def path(self, *parts):
        return os.path.join(self.dir, *parts)

    @property
    def cards(self):
        p = self.path("design", "cards.json")
        return _load_json(p) if os.path.exists(p) else {"cards": [], "relics": [], "potions": []}

    @property
    def assets(self):
        p = self.path("art", "art_assets.json")
        return _load_json(p) if os.path.exists(p) else {}


def character(cid=None):
    """A character by id (any case); with no id, the first one."""
    ids = character_ids()
    cid = (cid or ids[0]).lower()
    if cid not in ids:
        raise SystemExit(f"Unknown character '{cid}'. Characters: {', '.join(ids)}")
    return Character(_load_json(os.path.join(CHARACTERS_DIR, cid, "character.json")))


def characters():
    return [character(c) for c in character_ids()]


# ---------------------------------------------------------------------------------------------------------------
# Where each kind of art goes in the mod. Paths are relative to mod/ and use {id} for the lowercase character id.
# An art_assets.json item can override them with its own "outputs" (for a character's special art, like Trump's Wall).

KIND_OUTPUTS = {
    "char_button": {"button": "images/packed/character_select/char_select_{id}.png",
                    "locked": "images/packed/character_select/char_select_{id}_locked.png"},
    "fullscreen": {"image": "images/{id}/char_select_bg.png"},
    "top_icon": {"icon": "images/ui/top_panel/character_icon_{id}.png",
                 "outline": "images/ui/top_panel/character_icon_{id}_outline.png"},
    "map_marker": {"marker": "images/packed/map/icons/map_marker_{id}.png"},
    "figure": {"sprite": "images/{id}/{item}.png"},
    "hand": {"hand": "images/ui/hands/multiplayer_hand_{id}_{gesture}.png"},
    "transition": {"mask": "images/ui/transitions/{id}_transition.png"},
    "card": {"portrait": "images/packed/card_portraits/{id}/{item}.png"},
    "card_ancient": {"portrait": "images/packed/card_portraits/{id}/{item}.png"},
    "relic": {"icon": "images/relics/{item}.png"},
    "potion": {"icon": "images/potions/{item}.png"},
    "power": {"icon": "images/powers/{item}.png"},
    "energy_icon": {"text": "images/packed/sprite_fonts/{id}_energy_icon.png",
                    "gem": MOD_ID + "/atlas_fallback/ui_atlas/card/energy_{id}.png"},
}


def art_outputs(ch, item_id, kind, spec=None):
    """{output key: path relative to mod/} for one art item (spec: the art_assets.json entry, if any)."""
    spec = spec or {}
    if kind == "orb":
        return {f"layer{n}": f"images/ui/combat/energy_counters/{ch.id}/{ch.id}_orb_layer_{n}.png" for n in spec["layers"]}
    if kind == "figure_sheet" and not spec.get("outputs"):
        return {pose: f"images/{ch.id}/{pose}.png" for pose in spec["poses"]}
    outputs = spec.get("outputs") or KIND_OUTPUTS.get(kind)
    if outputs is None:
        raise ValueError(f"{ch.id}: art item '{item_id}' of kind '{kind}' needs \"outputs\" in art_assets.json")
    gesture = item_id.split("_", 1)[1] if item_id.startswith("hand_") else ""
    return {k: v.format(id=ch.id, item=item_id, gesture=gesture) for k, v in outputs.items()}


# ---------------------------------------------------------------------------------------------------------------
# The scenes and resources the game loads by name for every character (paths relative to mod/, {id} = lowercase id).
# scripts/new_character.py copies them from an existing character; build.py refuses to build if one is missing.

CHARACTER_SCENES = [
    "scenes/creature_visuals/{id}.tscn",                          # combat body (Sprite2D Visuals, NCharacterPoses)
    "scenes/merchant/characters/{id}_merchant.tscn",              # shop figure (Sprite2D CharacterSprite)
    "scenes/rest_site/characters/{id}_rest_site.tscn",            # rest-site figure (Sprite2D CharacterSprite)
    "scenes/screens/char_select/char_select_bg_{id}.tscn",        # character select painting + particles
    "scenes/combat/energy_counters/{id}_energy_counter.tscn",     # energy orb (5 image layers + vfx)
    "scenes/vfx/energy/{id}/{id}_energy_vfx_back.tscn",
    "scenes/vfx/energy/{id}/{id}_energy_vfx_front.tscn",
    "scenes/vfx/card_trail_{id}.tscn",                            # trail behind a played card
    "materials/transitions/{id}_transition_mat.tres",            # screen wipe (uses the transition mask image)
    "scenes/ui/character_icons/{id}_icon.tscn",                  # top-bar icon (make_placeholders.py)
    "materials/cards/frames/card_frame_{id}_mat.tres",           # card frame hue (make_placeholders.py, character.json frame_hsv)
]

LOC_TABLES = ["ancients", "card_library", "cards", "characters", "events", "potions", "powers", "relics", "static_hover_tips"]


def missing_files(ch):
    """Scenes, localization tables and code files a character needs that don't exist yet (paths for messages)."""
    missing = [p.format(id=ch.id) for p in CHARACTER_SCENES if not os.path.exists(os.path.join(MOD_DIR, p.format(id=ch.id)))]
    missing += [f"characters/{ch.id}/localization/eng/{t}.json" for t in LOC_TABLES
                if not os.path.exists(ch.path("localization", "eng", t + ".json"))]
    if not os.path.exists(os.path.join(ch.src, ch["class"] + ".cs")):
        missing.append(os.path.relpath(os.path.join(ch.src, ch["class"] + ".cs"), REPO).replace(os.sep, "/"))
    return missing

"""
Gather the edit's media where Remotion and the mix can read them, as hard links (no copies of the 4K masters):
  build/trailer/media/clips/<clip>.mp4        every captured clip (dailies' pick of takes)
  build/trailer/audio/game/<clip>.wav         its game sound, when an audio pass recorded one
  build/trailer/media/cards/<char>/<id>.png   the card renders (T4), base and _plus
  build/trailer/media/relics|potions/*.png    the mod's relic and potion art
  build/trailer/media/keyart/*.png            the key art (build/trailer/keyart, T5)
  build/trailer/media/ui/*.png                roster portraits and combat sprites

  python trailer/tools/prep_edit.py

Run after a new capture (then dailies.py first) or new art. Safe to re-run.
"""
import json
import os
import shutil
import sys

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from moments import clip_sources  # noqa: E402

TRAILER = os.path.join(REPO, "build", "trailer")
MEDIA = os.path.join(TRAILER, "media")


def link(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if os.path.exists(dst):
        if os.path.samefile(src, dst):
            return False
        os.remove(dst)
    try:
        os.link(src, dst)
    except OSError:  # another drive: copy instead
        shutil.copy2(src, dst)
    return True


def main():
    counts = {"clips": 0, "sounds": 0, "cards": 0, "art": 0}
    for name, (video, sound) in clip_sources().items():
        counts["clips"] += link(video, os.path.join(MEDIA, "clips", name + ".mp4"))
        if sound:
            counts["sounds"] += link(sound, os.path.join(TRAILER, "audio", "game", name + ".wav"))
    capture = os.path.join(TRAILER, "capture")
    cards = [os.path.join(capture, d, "cards") for d in sorted(os.listdir(capture)) if os.path.isdir(os.path.join(capture, d, "cards"))]
    if cards:
        for character in os.listdir(cards[-1]):
            for f in os.listdir(os.path.join(cards[-1], character)):
                counts["cards"] += link(os.path.join(cards[-1], character, f), os.path.join(MEDIA, "cards", character, f))
    for kind in ("relics", "potions"):
        folder = os.path.join(REPO, "mod", "images", kind)
        for f in os.listdir(folder):
            if f.endswith(".png"):
                counts["art"] += link(os.path.join(folder, f), os.path.join(MEDIA, kind, f))
    # The key art (T5), for the title drafts and the thumbnail.
    keyart = os.path.join(TRAILER, "keyart")
    if os.path.isdir(keyart):
        for f in os.listdir(keyart):
            if f.endswith(".png"):
                counts["art"] += link(os.path.join(keyart, f), os.path.join(MEDIA, "keyart", f))
    # Roster portraits (the chat avatars) and combat sprites (the collection's centre).
    images = os.path.join(REPO, "mod", "images")
    for src, dst in ((("packed", "character_select", "char_select_trump.png"), "portrait_trump.png"),
                     (("packed", "character_select", "char_select_biden.png"), "portrait_biden.png"),
                     (("trump", "combat_idle.png"), "sprite_trump.png"),
                     (("biden", "combat_idle.png"), "sprite_biden.png"),
                     (("biden", "dark_combat_idle.png"), "sprite_brandon.png")):
        counts["art"] += link(os.path.join(images, *src), os.path.join(MEDIA, "ui", dst))
    # The collection beat's lists, from the characters' design files (names for the relic ring).
    index = {}
    for character in ("trump", "biden"):
        design = json.load(open(os.path.join(REPO, "characters", character, "design", "cards.json"), encoding="utf-8"))
        index[character] = {k: [{"id": x["id"], "name": x["name"], **({"rarity": x["rarity"]} if "rarity" in x else {})} for x in design[k]]
                            for k in ("cards", "relics", "potions")}
    with open(os.path.join(MEDIA, "collection.json"), "w", encoding="utf-8") as f:
        json.dump(index, f, indent=1)
    print("linked (new):", counts)


if __name__ == "__main__":
    main()

"""
The footage review page (T4 "dailies"): every captured clip as a small preview video with a poster frame, grouped
by section, so takes can be judged on a phone. The 4K clips stay where they are.

  python trailer/tools/dailies.py CAPTURE_DIR [CAPTURE_DIR...] [--out build/trailer/review/t4]

CAPTURE_DIR is a build/trailer/capture/<time>_<method> folder (clips/ inside; for co-op, host/clips/). Clips with the
same name in a later folder replace earlier ones (retakes). A clip's game audio (clips/<name>.wav from an audio pass)
goes into its preview. Writes index.html, previews/*.mp4, posters/*.jpg and
cards/*.webp (a sample of the rendered card images), ready to publish as an artifact.
"""
import html
import json
import os
import subprocess
import sys

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

SECTIONS = [
    ("Cold open and select screen", ("menu_", "select_")),
    ("The Donald", ("donald_wall", "donald_rain", "donald_deport", "donald_mass", "donald_fired")),
    ("Sleepy Joe", ("joe_doze", "joe_tangent", "joe_nodoff")),
    ("Dark Brandon", ("joe_wake", "joe_lasershow", "joe_micdrop", "joe_motorcade", "joe_airforce")),
    ("Together (co-op)", ("coop_",)),
    ("Montage and collection", ("donald_wrecking", "donald_chapter11", "donald_tweets", "donald_fury", "donald_golf", "joe_zero", "joe_sotu")),
]
CARD_SAMPLE = [("trump", "youre_fired"), ("trump", "make_it_rain"), ("trump", "golden_escalator"), ("trump", "wrecking_ball"),
               ("trump", "build_the_wall"), ("trump", "tweet"), ("biden", "heres_the_deal"), ("biden", "laser_show"),
               ("biden", "state_of_the_union"), ("biden", "mic_drop"), ("biden", "motorcade"), ("biden", "cup_of_joe")]


def run(cmd):
    subprocess.run(cmd, check=True)


def duration(path):
    out = subprocess.run([presmod.FFPROBE, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path],
                         capture_output=True, text=True).stdout.strip()
    return float(out or 0)


def main():
    argv = sys.argv[1:]
    out = os.path.join(REPO, "build", "trailer", "review", "t4")
    if "--out" in argv:
        i = argv.index("--out")
        out = argv[i + 1]
        del argv[i:i + 2]
    clips = {}
    sounds = {}
    cards_dir = None
    for d in argv:
        for sub in ("clips", os.path.join("host", "clips")):
            folder = os.path.join(d, sub)
            if os.path.isdir(folder):
                for f in sorted(os.listdir(folder)):
                    if f.endswith(".mp4"):
                        clips[f[:-4]] = os.path.join(folder, f)
                    elif f.endswith(".wav"):
                        sounds[f[:-4]] = os.path.join(folder, f)
        if os.path.isdir(os.path.join(d, "cards")):
            cards_dir = os.path.join(d, "cards")
    for sub in ("previews", "posters", "cards"):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    items = []
    for name, src in clips.items():
        preview = os.path.join(out, "previews", name + ".mp4")
        poster = os.path.join(out, "posters", name + ".jpg")
        length = duration(src)
        audio = ["-i", sounds[name], "-map", "0:v", "-map", "1:a", "-c:a", "aac", "-b:a", "96k", "-shortest"] if name in sounds else ["-an"]
        run([presmod.FFMPEG, "-y", "-v", "error", "-i", src] + audio[:2] + ["-vf", "scale=854:-2", "-c:v", "libx264", "-preset", "slow", "-crf", "30",
             "-pix_fmt", "yuv420p"] + audio[2:] + ["-movflags", "+faststart", preview])
        run([presmod.FFMPEG, "-y", "-v", "error", "-ss", f"{max(0, length * 0.55):.2f}", "-i", src, "-frames:v", "1", "-vf", "scale=640:-2", "-q:v", "5", poster])
        items.append((name, length))
    cards = []
    if cards_dir:
        for character, card in CARD_SAMPLE:
            src = os.path.join(cards_dir, character, card + ".png")
            if os.path.exists(src):
                dst = os.path.join(out, "cards", f"{character}_{card}.webp")
                run([presmod.FFMPEG, "-y", "-v", "error", "-i", src, "-vf", "scale=300:-2", "-q:v", "80", dst])
                cards.append(f"{character}_{card}")
    with open(os.path.join(out, "clips.json"), "w", encoding="utf-8") as f:
        json.dump({"clips": clips, "cards": cards}, f, indent=1)
    write_page(out, items, cards)
    total = sum(os.path.getsize(os.path.join(dp, fn)) for dp, _, fns in os.walk(out) for fn in fns)
    print(f"{len(items)} clips, {len(cards)} card samples, {total / 1e6:.1f} MB in {out}")


def section_of(name):
    for title, prefixes in SECTIONS:
        if name.startswith(prefixes):
            return title
    return "Other"


def write_page(out, items, cards):
    groups = {}
    for name, length in items:
        groups.setdefault(section_of(name), []).append((name, length))
    order = [t for t, _ in SECTIONS] + ["Other"]
    parts = []
    for title in order:
        if title not in groups:
            continue
        cells = "".join(
            f'<figure><video controls playsinline preload="none" poster="posters/{n}.jpg" src="previews/{n}.mp4"></video>'
            f'<figcaption><b>{html.escape(n)}</b><span>{l:.1f} s</span></figcaption></figure>'
            for n, l in sorted(groups[title]))
        parts.append(f'<section><h2>{html.escape(title)}</h2><div class="grid">{cells}</div></section>')
    card_cells = "".join(f'<img src="cards/{c}.webp" alt="{html.escape(c)}">' for c in cards)
    page = PAGE.replace("{{SECTIONS}}", "".join(parts)).replace("{{CARDS}}", card_cells).replace("{{COUNT}}", str(len(items)))
    with open(os.path.join(out, "index.html"), "w", encoding="utf-8") as f:
        f.write(page)


PAGE = """<title>Trailer Dailies</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Kreon:wght@600;700&family=Source+Sans+3:wght@400;600&family=IBM+Plex+Mono:wght@400;600&display=swap">
<style>
  :root { color-scheme: dark; --ground: #0e1513; --panel: #141e1b; --line: #273531; --ink: #ebe5d5; --muted: #97a39b; --gold: #e6b84a; }
  html, body { background: var(--ground); color: var(--ink); }
  body { font: 16px/1.5 "Source Sans 3", "Segoe UI", system-ui, sans-serif; }
  .wrap { max-width: 1200px; margin: 0 auto; padding-inline: 16px; padding-block: 26px 60px; }
  h1, h2 { font-family: Kreon, Georgia, serif; margin: 0; }
  h1 { font-size: clamp(28px, 5vw, 42px); }
  h2 { font-size: 22px; margin: 34px 0 12px; padding-bottom: 6px; border-bottom: 2px solid var(--line); }
  p { max-width: 72ch; color: var(--muted); }
  .eyebrow { font: 600 12px/1 "IBM Plex Mono", monospace; letter-spacing: .12em; text-transform: uppercase; color: var(--muted); }
  .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 12px; }
  figure { margin: 0; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; overflow: hidden; }
  video { display: block; width: 100%; aspect-ratio: 16 / 9; background: #000; }
  figcaption { display: flex; justify-content: space-between; gap: 8px; padding: 8px 10px; font: 13px/1.3 "IBM Plex Mono", monospace; }
  figcaption span { color: var(--muted); }
  .cards { display: flex; flex-wrap: wrap; gap: 10px; }
  .cards img { width: 150px; max-width: 30%; height: auto; }
</style>
<div class="wrap">
  <div class="eyebrow">Step 12 · Trailer · T4 gameplay capture · for review</div>
  <h1>Trailer Dailies</h1>
  <p>Every clip the director bot recorded ({{COUNT}}), as small previews: the masters are 4K at 60 fps with no dropped frames. "_full" is the game as played, "_clean" hides the interface. The sound is the game's own effects (recorded in a second, real-time pass, music off); in the trailer it sits quietly under the soundtrack. Name any clip to retake, and say what's off.</p>
  {{SECTIONS}}
  <section><h2>Card images (a sample of 354)</h2><p>Rendered by the game itself at 3x on a transparent background, for the card callouts and the collection beat.</p><div class="cards">{{CARDS}}</div></section>
</div>
"""

if __name__ == "__main__":
    main()

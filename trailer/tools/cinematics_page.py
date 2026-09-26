"""
The T5 review page: the finished picks (1440p60, with Lanczos vs SeedVR2 crops) once upscale.py has made them, the H3
cinematic takes (previews with H3's own sound), the key art, the title drafts and the quality A/B test, ready to publish
as an artifact.

  python trailer/tools/cinematics_page.py [--out build/trailer/review/t5]
"""
import html
import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

AI = os.path.join(REPO, "build", "trailer", "ai")
MEDIA_AI = os.path.join(REPO, "build", "trailer", "media", "ai")
# The picks, and a 1440p crop box (x, y, w, h) on a detail worth comparing, with the source frame to compare on.
FINALS = [
    ("A1 · The Donald, hero", "a1_donald_hero_s7_quality", (1300, 60, 900, 640), 60),
    ("A2 · Sleepy Joe to Dark Brandon", "a2_joe_eyes_s3_quality", (760, 120, 900, 640), 12),
    ("A3 · Dark Brandon rises", "a3_brandon_rises_s1_quality", (800, 120, 900, 640), 70),
    ("A3b · Unleashed", "a3_brandon_unleashed_s2_quality", (810, 120, 900, 640), 60),
    ("A4 · Title backdrop", "a4_title_walk_s22_s2_quality", (800, 100, 900, 640), 60),
]
SHOTS = [
    ("A1 · The Donald, hero", "a1_donald_hero", "From his select painting. Embers, a push-in, gold bricks landing on the Wall."),
    ("A2 · Sleepy Joe to Dark Brandon", "a2_joe_eyes", "From his select painting: he dozes off, then the lenses ignite red."),
    ("A3 · Dark Brandon rises", "a3_brandon_rises", "From the Dark Brandon Rises card art: lasers at the camera."),
    ("A3b · Unleashed", "a3_brandon_unleashed", "From the Unleashed card art: the jacket rips open, lightning."),
    ("A4 · Title backdrop", "a4_title_walk", "From the new key art: walking away from an explosion of gold coins."),
]


def run(cmd):
    subprocess.run(cmd, check=True)


def compare_sheet(tag, box, idx, dest):
    """Lanczos vs SeedVR2 at 1:1 pixels of the 1440p frame."""
    frames = sorted(os.listdir(os.path.join(AI, tag, "frames")))
    ups = sorted(os.listdir(os.path.join(AI, tag + "_up")))
    src = Image.open(os.path.join(AI, tag, "frames", frames[idx])).convert("RGB")
    up = Image.open(os.path.join(AI, tag + "_up", ups[idx])).convert("RGB")
    lz = src.resize(up.size, Image.LANCZOS)
    x, y, w, h = box
    sheet = Image.new("RGB", (w * 2 + 12, h + 44), (14, 21, 19))
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 26)
    except OSError:
        font = ImageFont.load_default()
    for i, (label, im) in enumerate((("Lanczos (plain resize)", lz), ("SeedVR2 7B", up))):
        sheet.paste(im.crop((x, y, x + w, y + h)), (i * (w + 12), 44))
        draw.text((i * (w + 12) + 10, 8), label, fill=(235, 229, 213), font=font)
    sheet.save(dest, quality=88)


def finals_section(out):
    cells, sheets = [], []
    for title, tag, box, idx in FINALS:
        src = os.path.join(MEDIA_AI, tag + ".mp4")
        if not os.path.exists(src) or not os.path.isdir(os.path.join(AI, tag + "_up")):
            continue
        run([presmod.FFMPEG, "-y", "-v", "error", "-i", src, "-c:v", "libx264", "-preset", "slow", "-crf", "23", "-pix_fmt", "yuv420p",
             "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", os.path.join(out, "clips", f"final_{tag}.mp4")])
        run([presmod.FFMPEG, "-y", "-v", "error", "-ss", "3.8", "-i", src, "-frames:v", "1", "-vf", "scale=960:-2", "-q:v", "4",
             os.path.join(out, "posters", f"final_{tag}.jpg")])
        cells.append(f'<figure><video controls playsinline preload="none" poster="posters/final_{tag}.jpg" src="clips/final_{tag}.mp4"></video>'
                     f'<figcaption><b>{html.escape(title)}</b><span>2520×1440 · 60 fps</span></figcaption></figure>')
        jpg = os.path.join(out, "stills", f"cmp_{tag}.jpg")
        compare_sheet(tag, box, idx, jpg)
        sheets.append(f'<figure><img src="stills/cmp_{tag}.jpg" alt="Lanczos vs SeedVR2, {html.escape(title)}">'
                      f'<figcaption><b>{html.escape(title)}</b><span>1:1 crop of the 1440p frame</span></figcaption></figure>')
    if not cells:
        return ""
    return ('<section><h2>Finals</h2><p>Your picks, finished for the 1440p60 master: SeedVR2 7B upscales H3’s 1344×768 '
            'to 2520×1440, then RIFE interpolates 24 fps to 60 so they cut cleanly against the game footage. Sound is still '
            'H3’s own; the trailer mix replaces most of it.</p>'
            f'<div class="grid wide">{"".join(cells)}</div>'
            '<p>What the upscaler buys, at 1:1 pixels: a plain resize on the left, SeedVR2 on the right.</p>'
            f'<div class="grid wide">{"".join(sheets)}</div></section>')


def main():
    out = os.path.join(REPO, "build", "trailer", "review", "t5")
    if "--out" in sys.argv:
        out = sys.argv[sys.argv.index("--out") + 1]
    for sub in ("clips", "posters", "stills"):
        os.makedirs(os.path.join(out, sub), exist_ok=True)
    sections = []
    for title, prefix, note in SHOTS:
        takes = sorted(f[:-4] for f in os.listdir(AI) if f.startswith(prefix) and f.endswith(".mp4") and "_turbo" not in f and not f.endswith("_quality.mp4"))
        cells = []
        for take in takes:
            src = os.path.join(AI, take + ".mp4")
            run([presmod.FFMPEG, "-y", "-v", "error", "-i", src, "-vf", "scale=960:-2", "-c:v", "libx264", "-preset", "slow", "-crf", "24",
                 "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", os.path.join(out, "clips", take + ".mp4")])
            run([presmod.FFMPEG, "-y", "-v", "error", "-ss", "3.8", "-i", src, "-frames:v", "1", "-vf", "scale=640:-2", "-q:v", "4",
                 os.path.join(out, "posters", take + ".jpg")])
            label = take.replace(prefix, "").replace("_quality_sage", "").strip("_").replace("_", " ")
            cells.append(f'<figure><video controls playsinline preload="none" poster="posters/{take}.jpg" src="clips/{take}.mp4"></video>'
                         f'<figcaption><b>{html.escape(label or take)}</b><span>{html.escape(take)}</span></figcaption></figure>')
        sections.append(f'<section><h2>{html.escape(title)}</h2><p>{html.escape(note)}</p><div class="grid">{"".join(cells)}</div></section>')
    stills = []
    for name, src in [(f[:-4], os.path.join(REPO, "build", "trailer", "keyart", f)) for f in sorted(os.listdir(os.path.join(REPO, "build", "trailer", "keyart")))]:
        run([presmod.FFMPEG, "-y", "-v", "error", "-i", src, "-vf", "scale=960:-2", "-q:v", "80", os.path.join(out, "stills", name + ".webp")])
        stills.append(f'<figure><img src="stills/{name}.webp" alt="{html.escape(name)}"><figcaption><b>{html.escape(name.replace("keyart_", "").replace("_", " "))}</b></figcaption></figure>')
    titles = []
    for v, label in (("spectral", "A · Spectral"), ("kreon", "B · Kreon"), ("fira", "C · Fira Condensed")):
        src = os.path.join(REPO, "trailer", "edit", "out", f"title_{v}.png")
        if os.path.exists(src):
            run([presmod.FFMPEG, "-y", "-v", "error", "-i", src, "-vf", "scale=1280:-2", "-q:v", "85", os.path.join(out, "stills", f"title_{v}.webp")])
            titles.append(f'<figure><img src="stills/title_{v}.webp" alt="{label}"><figcaption><b>{label}</b></figcaption></figure>')
    ab = ""
    ab_src = os.path.join(AI, "a1_donald_hero_s7_turbo.mp4")
    if os.path.exists(ab_src):
        for tag in ("quality", "turbo"):
            run([presmod.FFMPEG, "-y", "-v", "error", "-i", os.path.join(AI, f"a1_donald_hero_s7_{tag}.mp4"), "-vf", "scale=960:-2", "-c:v", "libx264",
                 "-preset", "slow", "-crf", "24", "-pix_fmt", "yuv420p", "-an", "-movflags", "+faststart", os.path.join(out, "clips", f"ab_{tag}.mp4")])
        ab = ('<section><h2>Why these settings</h2><p>The same prompt and seed. Left: the released quality configuration (20 steps, res_multistep, '
              'shift 12/3, native 1344×768). Right: the earlier turbo workflow (turbo LoRA at 6 steps, Spectrum, euler). The quality run '
              'moves more and keeps the painting crisp; the turbo run barely moves and grains the texture. Takes are explored with Sage '
              'attention (visually identical, twice as fast); the chosen ones are re-rendered without it.</p><div class="grid">'
              '<figure><video controls muted playsinline preload="none" src="clips/ab_quality.mp4"></video><figcaption><b>quality</b><span>~8 min</span></figcaption></figure>'
              '<figure><video controls muted playsinline preload="none" src="clips/ab_turbo.mp4"></video><figcaption><b>turbo (before)</b><span>~1.3 min</span></figcaption></figure>'
              '</div></section>')
    finals = finals_section(out)
    intro = (INTRO_FINALS if finals else INTRO_TAKES)
    takes_head = '<div class="eyebrow" style="margin-top:48px">All takes (H3 native 1344×768, 24 fps)</div>' if finals else ""
    page = (PAGE.replace("{{INTRO}}", intro).replace("{{FINALS}}", finals).replace("{{SECTIONS}}", takes_head + "".join(sections))
            .replace("{{STILLS}}", "".join(stills)).replace("{{TITLES}}", "".join(titles)).replace("{{AB}}", ab))
    with open(os.path.join(out, "index.html"), "w", encoding="utf-8") as f:
        f.write(page)
    total = sum(os.path.getsize(os.path.join(dp, fn)) for dp, _, fns in os.walk(out) for fn in fns)
    print(f"{total / 1e6:.1f} MB in {out}")


INTRO_TAKES = ("The painted cinematic shots, made with MiniMax H3 on your PC from the mod's own art, several takes each. They play "
               "here at H3's native 1344×768 and 24 fps with H3's own generated sound; the trailer upscales them to 1440p (see the "
               "questions in chat). Pick a take per shot.")
INTRO_FINALS = ("The painted cinematic shots, made with MiniMax H3 on your PC from the mod's own art. Your five picks are finished "
                "for the trailer at the top; every take, the key art and the title drafts follow.")

PAGE = """<title>Trailer Cinematics</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Kreon:wght@600;700&family=Source+Sans+3:wght@400;600&family=IBM+Plex+Mono:wght@400;600&display=swap">
<style>
  :root { color-scheme: dark; --ground: #0e1513; --panel: #141e1b; --line: #273531; --ink: #ebe5d5; --muted: #97a39b; }
  html, body { background: var(--ground); color: var(--ink); }
  body { font: 16px/1.5 "Source Sans 3", "Segoe UI", system-ui, sans-serif; }
  .wrap { max-width: 1200px; margin: 0 auto; padding-inline: 16px; padding-block: 26px 60px; }
  h1, h2 { font-family: Kreon, Georgia, serif; margin: 0; }
  h1 { font-size: clamp(28px, 5vw, 42px); }
  h2 { font-size: 22px; margin: 34px 0 6px; padding-bottom: 6px; border-bottom: 2px solid var(--line); }
  p { max-width: 74ch; color: var(--muted); margin: 6px 0 12px; }
  .eyebrow { font: 600 12px/1 "IBM Plex Mono", monospace; letter-spacing: .12em; text-transform: uppercase; color: var(--muted); }
  .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 12px; }
  .wide { grid-template-columns: repeat(auto-fill, minmax(420px, 1fr)); }
  figure { margin: 0; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; overflow: hidden; }
  video, img { display: block; width: 100%; max-width: 100%; height: auto; background: #000; }
  video { aspect-ratio: 16 / 9; }
  figcaption { display: flex; justify-content: space-between; gap: 8px; padding: 8px 10px; font: 13px/1.3 "IBM Plex Mono", monospace; }
  figcaption span { color: var(--muted); }
</style>
<div class="wrap">
  <div class="eyebrow">Step 12 · Trailer · T5 cinematic shots and graphics · for review</div>
  <h1>Trailer Cinematics</h1>
  <p>{{INTRO}}</p>
  {{FINALS}}
  {{SECTIONS}}
  <section><h2>Key art (Krea 2, the mod's art pipeline)</h2><p>New paintings of both presidents in front of the Spire. The walk images became the title backdrop (A4); a back-to-back one is the thumbnail candidate.</p><div class="grid wide">{{STILLS}}</div></section>
  <section><h2>Title logo drafts</h2><p>The game's own open-licensed fonts, gold foil, over the key art. The final title animates in on the last hit.</p><div class="grid wide">{{TITLES}}</div></section>
  {{AB}}
</div>
"""

if __name__ == "__main__":
    main()

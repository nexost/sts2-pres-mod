"""
The edit review page (T6): the rendered trailer as a web-sized encode (artifact files stop at 15 MB), a frame strip, and
the shot list from timeline.json, ready to publish as an artifact.

  python trailer/tools/edit_page.py RENDER.mp4 [--out build/trailer/review/t6] [--title "Trailer Edit v1"] [--notes notes.html] [--poster SECONDS] [--eyebrow TEXT]
"""
import html
import json
import os
import subprocess
import sys

from PIL import Image

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

TIMELINE = os.path.join(REPO, "trailer", "edit", "src", "timeline.json")
LIMIT = 14.3e6  # bytes, under the artifact's 15 MB per file


def run(cmd):
    subprocess.run(cmd, check=True)


def duration(path):
    out = subprocess.run([presmod.FFPROBE, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path],
                         capture_output=True, text=True).stdout.strip()
    return float(out)


def web_encode(src, dst):
    """720p60 H.264 (plays everywhere), two-pass to just under the size limit."""
    length = duration(src)
    audio_k = 128
    video_k = int(LIMIT * 8 / length / 1000) - audio_k - 30
    log = dst + ".2pass"
    common = ["-vf", "scale=1280:-2:flags=lanczos", "-c:v", "libx264", "-preset", "slow", "-b:v", f"{video_k}k", "-pix_fmt", "yuv420p", "-passlogfile", log]
    run([presmod.FFMPEG, "-y", "-v", "error", "-i", src] + common + ["-pass", "1", "-an", "-f", "null", "-"])
    run([presmod.FFMPEG, "-y", "-v", "error", "-i", src] + common + ["-pass", "2", "-c:a", "aac", "-b:a", f"{audio_k}k", "-movflags", "+faststart", dst])
    for f in os.listdir(os.path.dirname(dst)):
        if f.startswith(os.path.basename(log)):
            os.remove(os.path.join(os.path.dirname(dst), f))
    return video_k


def strip(src, dst, every=1.0, width=320):
    """One frame per second, 8 to a row."""
    length = duration(src)
    n = int(length / every)
    h = width * 9 // 16
    raw = subprocess.run([presmod.FFMPEG, "-v", "error", "-i", src, "-vf", f"fps=1/{every},scale={width}:{h}", "-frames:v", str(n),
                          "-f", "rawvideo", "-pix_fmt", "rgb24", "-"], capture_output=True, check=True).stdout
    frames = [Image.frombytes("RGB", (width, h), raw[i * width * h * 3:(i + 1) * width * h * 3]) for i in range(len(raw) // (width * h * 3))]
    cols = 8
    sheet = Image.new("RGB", (cols * width, ((len(frames) + cols - 1) // cols) * h), (0, 0, 0))
    for i, im in enumerate(frames):
        sheet.paste(im, ((i % cols) * width, (i // cols) * h))
    sheet.save(dst, quality=80)


def main():
    argv = sys.argv[1:]
    opts = {"--out": os.path.join(REPO, "build", "trailer", "review", "t6"), "--title": "Trailer Edit v1", "--notes": None, "--poster": "64.5", "--eyebrow": "Step 12 · Trailer · T6 the edit · for review"}
    for k in list(opts):
        if k in argv:
            i = argv.index(k)
            opts[k] = argv[i + 1]
            del argv[i:i + 2]
    src = argv[0]
    out = opts["--out"]
    os.makedirs(out, exist_ok=True)
    kbps = web_encode(src, os.path.join(out, "trailer.mp4"))
    run([presmod.FFMPEG, "-y", "-v", "error", "-ss", opts["--poster"], "-i", src, "-frames:v", "1", "-vf", "scale=1280:-2", "-q:v", "3", os.path.join(out, "poster.jpg")])
    strip(src, os.path.join(out, "strip.jpg"))
    t = json.load(open(TIMELINE, encoding="utf-8"))
    rows = []
    for s in t["shots"]:
        cam = s.get("cam")
        how = []
        if cam and cam[0] != 1:
            how.append(f"{cam[0]:.2f}×" + (f" → {s['camTo'][0]:.2f}×" if s.get("camTo") else ""))
        if s.get("rate", 1) != 1:
            how.append(f"{s['rate']}× speed")
        if s.get("freeze"):
            how.append("freeze")
        if s.get("look"):
            how.append(s["look"])
        src_name = s["src"].replace("ai/", "painted: ").replace("_quality", "")
        in_point = "" if s["src"] == "graphics" else f"{s['from']:.2f}"
        rows.append(f"<tr><td>{s['at']:05.2f}</td><td>{s['dur']:.2f}</td><td>{html.escape(src_name)}</td>"
                    f"<td>{in_point}</td><td>{html.escape(', '.join(how))}</td></tr>")
    notes = open(opts["--notes"], encoding="utf-8").read() if opts["--notes"] else ""
    page = PAGE.replace("{{TITLE}}", html.escape(opts["--title"])).replace("{{ROWS}}", "".join(rows)).replace("{{NOTES}}", notes) \
        .replace("{{KBPS}}", str(kbps)).replace("{{EYEBROW}}", html.escape(opts["--eyebrow"])).replace("{{COUNT}}", str(len(t["shots"])))
    with open(os.path.join(out, "index.html"), "w", encoding="utf-8") as f:
        f.write(page)
    total = sum(os.path.getsize(os.path.join(dp, fn)) for dp, _, fns in os.walk(out) for fn in fns)
    print(f"{total / 1e6:.1f} MB in {out} (trailer.mp4 {os.path.getsize(os.path.join(out, 'trailer.mp4')) / 1e6:.1f} MB)")


PAGE = """<title>{{TITLE}}</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Kreon:wght@600;700&family=Source+Sans+3:wght@400;600&family=IBM+Plex+Mono:wght@400;600&display=swap">
<style>
  :root { color-scheme: dark; --ground: #0e1513; --panel: #141e1b; --line: #273531; --ink: #ebe5d5; --muted: #97a39b; --gold: #e6b84a; }
  html, body { background: var(--ground); color: var(--ink); }
  body { font: 16px/1.5 "Source Sans 3", "Segoe UI", system-ui, sans-serif; margin: 0; }
  .wrap { max-width: 1200px; margin: 0 auto; padding-inline: 16px; padding-block: 26px 60px; }
  h1, h2 { font-family: Kreon, Georgia, serif; margin: 0; }
  h1 { font-size: clamp(28px, 5vw, 42px); }
  h2 { font-size: 22px; margin: 34px 0 10px; padding-bottom: 6px; border-bottom: 2px solid var(--line); }
  p, li { max-width: 78ch; color: var(--muted); }
  b, strong { color: var(--ink); }
  .eyebrow { font: 600 12px/1 "IBM Plex Mono", monospace; letter-spacing: .12em; text-transform: uppercase; color: var(--muted); }
  video, img { display: block; width: 100%; max-width: 100%; height: auto; background: #000; border-radius: 6px; }
  video { aspect-ratio: 16 / 9; margin-top: 14px; }
  .scroll { overflow-x: auto; }
  table { border-collapse: collapse; width: 100%; font: 13px/1.35 "IBM Plex Mono", monospace; }
  th, td { text-align: left; padding: 5px 8px; border-bottom: 1px solid var(--line); white-space: nowrap; }
  th { color: var(--muted); font-weight: 600; }
  td:nth-child(3) { color: var(--gold); }
</style>
<div class="wrap">
  <div class="eyebrow">{{EYEBROW}}</div>
  <h1>{{TITLE}}</h1>
  <video controls playsinline preload="metadata" poster="poster.jpg" src="trailer.mp4"></video>
  <p>A web encode (1280×720, 60 fps, {{KBPS}} kbps) so it fits here; the master is 2560×1440 at 60 fps. Sound on.</p>
  {{NOTES}}
  <h2>One frame per second</h2>
  <img src="strip.jpg" alt="One frame per second of the trailer">
  <h2>Shot list ({{COUNT}} shots)</h2>
  <p>Straight from <code>trailer/edit/src/timeline.json</code>: start and length in seconds, the source and its in-point, the camera (zoom into the 4K master) and grade.</p>
  <div class="scroll"><table><tr><th>at</th><th>len</th><th>source</th><th>in</th><th>camera, speed, grade</th></tr>{{ROWS}}</table></div>
</div>
"""

if __name__ == "__main__":
    main()

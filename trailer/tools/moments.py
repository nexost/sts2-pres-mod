"""
Moment maps for editing: each captured clip as one contact sheet (a frame every STEP seconds, time-stamped) with its game
sound's waveform and the loudest hits marked, so cuts can be set on exact source times.

  python trailer/tools/moments.py [CLIP...] [--step 0.25]

CLIP names come from build/trailer/review/t4/clips.json (written by dailies.py), or a CLIP can be a video's path (the
painted shots in build/trailer/media/ai/); none = every captured clip. Writes
build/trailer/moments/<clip>.jpg and moments.json (duration and hit times per clip).
"""
import json
import math
import os
import subprocess
import sys

import numpy as np
import soundfile as sf
from PIL import Image, ImageDraw, ImageFont

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

TRAILER = os.path.join(REPO, "build", "trailer")
OUT = os.path.join(TRAILER, "moments")
TW, TH, COLS = 320, 180, 8


def clip_sources():
    """clip name -> (video, game sound or None): dailies' clip list, sounds from the audio passes (later ones win)."""
    clips = json.load(open(os.path.join(TRAILER, "review", "t4", "clips.json"), encoding="utf-8"))["clips"]
    sounds = {}
    capture = os.path.join(TRAILER, "capture")
    for d in sorted(os.listdir(capture)):
        folder = os.path.join(capture, d, "clips")
        if d.endswith("_audio") and os.path.isdir(folder):
            for f in os.listdir(folder):
                if f.endswith(".wav"):
                    sounds[f[:-4]] = os.path.join(folder, f)
    return {k: (v, sounds.get(k)) for k, v in clips.items()}


def duration(path):
    out = subprocess.run([presmod.FFPROBE, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path],
                         capture_output=True, text=True).stdout.strip()
    return float(out or 0)


def frames(video, step, count):
    """Thumbnails at 0, step, 2*step... as RGB arrays (one ffmpeg pass)."""
    raw = subprocess.run([presmod.FFMPEG, "-v", "error", "-i", video, "-vf", f"fps=1/{step}:round=down,scale={TW}:{TH}",
                          "-frames:v", str(count), "-f", "rawvideo", "-pix_fmt", "rgb24", "-"], capture_output=True, check=True).stdout
    n = len(raw) // (TW * TH * 3)
    return [Image.frombytes("RGB", (TW, TH), raw[i * TW * TH * 3:(i + 1) * TW * TH * 3]) for i in range(n)]


def hits(sound, length):
    """Onsets of the loudest sounds: 20 ms RMS, rises well above the running level, at least 0.2 s apart."""
    x, sr = sf.read(sound, always_2d=True)
    x = x.mean(axis=1)
    hop = int(0.02 * sr)
    rms = np.sqrt(np.convolve(x ** 2, np.ones(hop) / hop, mode="same"))[::hop]
    level = 20 * np.log10(rms + 1e-6)
    base = np.convolve(level, np.ones(25) / 25, mode="same")
    rise = level - np.concatenate([[level[0]] * 5, level[:-5]])
    found = []
    for i in np.argsort(-level):
        t = i * 0.02
        if t > length or level[i] < level.max() - 24 or rise[i] < 6 and level[i] < base[i] + 6:
            continue
        if all(abs(t - f) > 0.2 for f, _ in found):
            found.append((t, level[i]))
        if len(found) >= 12:
            break
    return sorted(found), level


def sheet(name, video, sound, step):
    length = duration(video)
    count = int(length / step) + 1
    thumbs = frames(video, step, count)
    rows = math.ceil(len(thumbs) / COLS)
    wave_h = 150
    img = Image.new("RGB", (COLS * TW, rows * (TH + 22) + wave_h + 40), (12, 16, 15))
    d = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/consolab.ttf", 17)
        big = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 22)
    except OSError:
        font = big = ImageFont.load_default()
    for i, th in enumerate(thumbs):
        x, y = (i % COLS) * TW, (i // COLS) * (TH + 22)
        img.paste(th, (x, y + 22))
        d.text((x + 4, y + 2), f"{i * step:5.2f}s", fill=(230, 220, 190), font=font)
    found = []
    top = rows * (TH + 22) + 34
    d.text((6, top - 30), f"{name}   {length:.2f} s   game sound" + ("" if sound else ": none"), fill=(235, 229, 213), font=big)
    if sound:
        found, level = hits(sound, length)
        w = COLS * TW
        pts = [(int(i * 0.02 / length * w), top + wave_h - int(max(0, v + 60) / 60 * wave_h)) for i, v in enumerate(level) if i * 0.02 <= length]
        d.line(pts, fill=(120, 200, 170), width=2)
        for t, _ in found:
            px = int(t / length * w)
            d.line([(px, top), (px, top + wave_h)], fill=(255, 90, 60), width=2)
            d.text((px + 3, top + 2), f"{t:.2f}", fill=(255, 140, 110), font=font)
    img.save(os.path.join(OUT, name + ".jpg"), quality=85)
    return {"duration": round(length, 3), "hits": [round(float(t), 2) for t, _ in found]}


def main():
    argv = sys.argv[1:]
    step = 0.25
    if "--step" in argv:
        i = argv.index("--step")
        step = float(argv[i + 1])
        del argv[i:i + 2]
    os.makedirs(OUT, exist_ok=True)
    sources = clip_sources()
    names = argv or sorted(sources)
    index_path = os.path.join(OUT, "moments.json")
    index = json.load(open(index_path, encoding="utf-8")) if os.path.exists(index_path) else {}
    for name in names:
        if os.path.isfile(name):  # any video by path (e.g. the painted shots), no game sound
            video, sound, name = name, None, os.path.splitext(os.path.basename(name))[0]
        else:
            video, sound = sources[name]
        index[name] = sheet(name, video, sound, step)
        print(f"{name:28s} {index[name]['duration']:6.2f} s  hits {index[name]['hits']}", flush=True)
    with open(index_path, "w", encoding="utf-8") as f:
        json.dump(index, f, indent=1)


if __name__ == "__main__":
    main()

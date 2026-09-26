"""
Measure audio takes for the trailer (tempo, key, loudness shape, big hits) and draw a sheet of spectrograms.

  python trailer/tools/audio_report.py FILES_OR_GLOBS... [--sheet out.png]

Tempo and beats: librosa. Key: chroma matched against major and minor profiles (Krumhansl). "Hits": the strongest
onsets. The sheet shows each take's mel spectrogram with its loudness curve, so structure (build, drop, silence,
the final hit) can be seen at a glance.
"""
import glob
import json
import os
import sys
import warnings

import numpy as np

warnings.filterwarnings("ignore")
import librosa  # noqa: E402

NOTES = ["C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"]
MAJOR = np.array([6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88])
MINOR = np.array([6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17])


def key_of(chroma):
    profile = chroma.mean(axis=1)
    best = None
    for i in range(12):
        for name, ref in (("major", MAJOR), ("minor", MINOR)):
            r = np.corrcoef(profile, np.roll(ref, i))[0, 1]
            if best is None or r > best[0]:
                best = (r, f"{NOTES[i]} {name}")
    return best[1], round(float(best[0]), 2)


def analyze(path):
    y, sr = librosa.load(path, sr=22050, mono=True)
    duration = len(y) / sr
    tempo, beats = librosa.beat.beat_track(y=y, sr=sr)
    tempo = float(np.atleast_1d(tempo)[0])
    onset_env = librosa.onset.onset_strength(y=y, sr=sr)
    onsets = librosa.onset.onset_detect(onset_envelope=onset_env, sr=sr, units="frames")
    strength = onset_env[onsets] if len(onsets) else np.array([])
    top = onsets[np.argsort(strength)[-5:]] if len(onsets) else []
    hits = sorted(round(float(t), 2) for t in librosa.frames_to_time(top, sr=sr))
    rms = librosa.feature.rms(y=y)[0]
    db = librosa.amplitude_to_db(rms, ref=np.max)
    seg = np.array_split(db, 8)
    shape = [round(float(np.mean(s)), 1) for s in seg]
    tail = librosa.frames_to_time(np.where(db > -40)[0][-1], sr=sr) if (db > -40).any() else 0
    k, conf = key_of(librosa.feature.chroma_cqt(y=y, sr=sr))
    return {"file": os.path.basename(path), "seconds": round(duration, 2), "tempo": round(tempo, 1), "key": k, "key_fit": conf,
            "loudness_by_eighth_dB": shape, "strongest_hits_s": hits, "sound_ends_s": round(float(tail), 2)}, (y, sr, db)


def sheet(results, out, px_per_second=30, row_height=120):
    """One row per take: mel spectrogram (dark to hot), loudness curve (cyan), strongest hits (white ticks), a
    grid line every second (every 5 s brighter)."""
    from PIL import Image, ImageDraw
    longest = max(len(y) / sr for _, (y, sr, _) in results)
    width = int(longest * px_per_second) + 10
    label = 16
    img = Image.new("RGB", (width, (row_height + label) * len(results)), (12, 12, 16))
    draw = ImageDraw.Draw(img)
    for row, (info, (y, sr, db)) in enumerate(results):
        top = row * (row_height + label)
        draw.text((4, top + 2), f"{info['file']}  {info['seconds']}s  ~{info['tempo']} BPM  {info['key']}", fill=(230, 230, 230))
        mel = librosa.power_to_db(librosa.feature.melspectrogram(y=y, sr=sr, n_mels=row_height), ref=np.max)
        norm = np.clip((mel + 80) / 80, 0, 1)[::-1]
        w = int(len(y) / sr * px_per_second)
        cols = np.linspace(0, norm.shape[1] - 1, w).astype(int)
        v = norm[:, cols]
        rgb = np.stack([np.clip(v * 2.2, 0, 1), np.clip(v * 1.6 - 0.45, 0, 1), np.clip(0.35 * v + (v > 0.85) * 0.6, 0, 1)], axis=-1)
        img.paste(Image.fromarray((rgb * 255).astype(np.uint8)), (0, top + label))
        for s in range(int(longest) + 1):
            x = s * px_per_second
            draw.line([(x, top + label), (x, top + label + 6)], fill=(200, 200, 200) if s % 5 == 0 else (90, 90, 90))
        pts = [(i / len(db) * w, top + label + row_height - (d + 80) / 80 * row_height) for i, d in enumerate(db)]
        draw.line(pts, fill=(0, 255, 255), width=1)
        for h in info["strongest_hits_s"]:
            x = h * px_per_second
            draw.line([(x, top + label), (x, top + label + row_height)], fill=(255, 255, 255))
    img.save(out)


def main():
    argv = sys.argv[1:]
    out = None
    if "--sheet" in argv:
        i = argv.index("--sheet")
        out = argv[i + 1]
        del argv[i:i + 2]
    files = sorted({f for a in argv for f in (glob.glob(a) or [a])})
    results = [analyze(f) for f in files]
    for info, _ in results:
        print(json.dumps(info))
    if out:
        sheet(results, out)
        print("sheet:", out)


if __name__ == "__main__":
    main()

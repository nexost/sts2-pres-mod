"""
Mix the trailer's sound from the edit's timeline (trailer/edit/src/timeline.json): music cues, narration, sound
effects and the game's own audio (each shot's clip, from build/trailer/audio/game/, see shot_sounds), with the music
and game ducked under the narrator, a peak limiter and loudness
normalised for YouTube (-14 LUFS integrated, true peak at most -1 dBTP). Any clip can be "deep-fried" with speed, pitch, bass
and drive (see fry_filters and saturate).

  python trailer/tools/mix.py [--out build/trailer/media/mix.wav]

Sources are relative to build/trailer/audio/. Writes the mix and its stems (music, vo, sfx, game) next to it.
Remotion only plays the finished mix, because it can't duck, limit or measure loudness.
"""
import json
import os
import subprocess
import sys

import numpy as np
import soundfile as sf
from scipy.ndimage import maximum_filter1d, uniform_filter1d
from scipy.signal import resample_poly

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

SR = 48000
AUDIO = os.path.join(REPO, "build", "trailer", "audio")
TIMELINE = os.path.join(REPO, "trailer", "edit", "src", "timeline.json")
TARGET_LUFS = -14.0
TRUE_PEAK_DB = -1.0   # YouTube's re-encode can clip above this
CEILING_DB = -1.3
_cache = {}


def db(x):
    return 10 ** (x / 20)


def load(path, af=None):
    """Any audio file as float32 stereo at 48 kHz (decoded by ffmpeg, through the filter chain af if given)."""
    key = (path, af)
    if key not in _cache:
        cmd = [presmod.FFMPEG, "-v", "error", "-i", os.path.join(AUDIO, path)]
        if af:
            cmd += ["-af", af]
        raw = subprocess.run(cmd + ["-f", "f32le", "-ac", "2", "-ar", str(SR), "-"], capture_output=True, check=True).stdout
        _cache[key] = np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()
    return _cache[key]


def fry_filters(item):
    """The "deep-fried" controls of a clip, as an ffmpeg chain: speed < 1 slows the tape (lower and slower, like
    slowed + reverb edits), pitch lowers in semitones keeping the tempo, bass boosts the lows in dB, reverb (0..1)
    adds a dense echo tail. from/to are then times in the processed clip."""
    chain = []
    if item.get("speed", 1) != 1:
        chain.append(f"asetrate={SR * item['speed']:.0f},aresample={SR}")
    if item.get("pitch"):
        chain.append(f"rubberband=pitch={2 ** (item['pitch'] / 12):.5f}")
    if item.get("bass"):
        chain.append(f"bass=g={item['bass']}:f=90:w=0.7")
    if item.get("reverb"):
        w = item["reverb"]
        chain.append(f"aecho=0.85:0.9:61|97|149|211:{0.42 * w:.2f}|{0.34 * w:.2f}|{0.27 * w:.2f}|{0.2 * w:.2f}")
    return ",".join(chain) or None


def saturate(x, drive_db):
    """Soft tanh saturation: drive_db pushes the signal into the curve; the level is matched back afterwards."""
    if not drive_db:
        return x
    k = db(drive_db)
    rms_in = np.sqrt(np.mean(x ** 2)) + 1e-9
    y = np.tanh(x * k)
    return y * (rms_in / (np.sqrt(np.mean(y ** 2)) + 1e-9))


def fades(x, fade_in, fade_out):
    n = len(x)
    if fade_in > 0:
        k = min(n, int(fade_in * SR))
        x[:k] *= np.linspace(0, 1, k)[:, None]
    if fade_out > 0:
        k = min(n, int(fade_out * SR))
        x[n - k:] *= np.linspace(1, 0, k)[:, None]
    return x


def tape_stop(x, seconds):
    """The last `seconds` slow to a halt, pitch falling with the speed, like a tape deck losing power."""
    k = min(len(x), int(seconds * SR))
    if k < 2:
        return x
    head, tail = x[:-k], x[-k:]
    speed = np.linspace(1, 0, k) ** 1.3
    pos = np.cumsum(speed)
    pos = pos[pos < k - 1]
    out = np.stack([np.interp(pos, np.arange(k), tail[:, c]) for c in range(2)], axis=1)
    return np.concatenate([head, out])


def place(bus, x, at):
    start = int(round(at * SR))
    if start < 0:
        x, start = x[-start:], 0
    end = min(len(bus), start + len(x))
    if end > start:
        bus[start:end] += x[:end - start]


def clip_of(item):
    x = load(item["file"], fry_filters(item))
    a = int(item.get("from", 0) * SR)
    b = int(item["to"] * SR) if "to" in item else len(x)
    x = saturate(x[a:b].copy(), item.get("drive", 0))
    if "length" in item:
        x = x[:int(item["length"] * SR)]
    x *= db(item.get("gain", 0))
    if item.get("tapeStop"):
        x = tape_stop(x, item["tapeStop"])
    return fades(x, item.get("fadeIn", 0.005), item.get("fadeOut", 0.02))


def shot_sounds(t):
    """The game's own sound under each shot of the picture edit: the shot's clip (or its "sound" override) over the same
    source span, slowed with the picture when the shot plays in slow motion. Shots with "sound": false stay silent."""
    items = []
    # The audio pass recorded at the PC's volume (peaks ~-33 dBFS, the same for every clip): one trim brings it up.
    trim = t.get("mix", {}).get("gameTrimDb", 0)
    for s in t.get("shots", []):
        snd = s.get("sound", True)
        if snd is False:
            continue
        src, frm = (snd["src"], snd["from"]) if isinstance(snd, dict) else (s["src"], s["from"])
        if not os.path.exists(os.path.join(AUDIO, "game", src + ".wav")):
            continue
        rate = s.get("rate", 1)
        # fry_filters' speed stretches the clip by 1/rate, so from/to are in the stretched clip's time.
        item = {"file": f"game/{src}.wav", "at": s["at"], "from": frm / rate, "to": frm / rate + s["dur"], "gain": trim, "fadeIn": 0.008, "fadeOut": 0.02}
        if rate != 1:
            item["speed"] = rate
        items.append(item)
    return items


def envelope(x, attack, release):
    """A smoothed level (0..1) of a bus, for ducking."""
    level = np.abs(x).max(axis=1)
    level = maximum_filter1d(level, int(0.03 * SR))
    level = uniform_filter1d(level, int(attack * SR))
    held = maximum_filter1d(level, int(release * SR), origin=-(int(release * SR) // 2 - 1))
    return np.clip(held / (held.max() + 1e-9), 0, 1)


def limiter(x, ceiling_db, lookahead=0.005, release=0.08, oversample=4):
    """A look-ahead peak limiter on the true (inter-sample) peak: the level is read from a 4x oversampled copy."""
    ceiling = db(ceiling_db)
    up = np.abs(resample_poly(x, oversample, 1, axis=0))
    n = min(len(x), len(up) // oversample)
    peak = np.zeros(len(x))
    peak[:n] = up[:n * oversample].reshape(n, oversample, 2).max(axis=(1, 2))
    need = np.minimum(1.0, ceiling / np.maximum(peak, 1e-9))
    need = -maximum_filter1d(-need, int(lookahead * SR) * 2 + 1)
    gain = np.empty_like(need)
    g, coeff = 1.0, np.exp(-1 / (release * SR))
    for i, n in enumerate(need):
        g = n if n < g else n + (g - n) * coeff
        gain[i] = g
    return x * gain[:, None]


def measure(path):
    """(integrated loudness in LUFS, true peak in dBTP), as ffmpeg's EBU R128 meter reads them."""
    out = subprocess.run([presmod.FFMPEG, "-hide_banner", "-nostats", "-i", path, "-af", "ebur128=peak=true", "-f", "null", "-"],
                         capture_output=True, text=True).stderr
    summary = out[out.rfind("Summary:"):]
    integrated = float(summary.split("I:")[1].split("LUFS")[0])
    true_peak = float(summary.split("True peak:")[1].split("Peak:")[1].split("dBFS")[0])
    return integrated, true_peak


def loudness(path):
    return measure(path)[0]


def main():
    out = os.path.join(REPO, "build", "trailer", "media", "mix.wav")
    if "--out" in sys.argv:
        out = sys.argv[sys.argv.index("--out") + 1]
    t = json.load(open(TIMELINE, encoding="utf-8"))
    n = int(t["duration"] * SR)
    buses = {name: np.zeros((n, 2), dtype=np.float32) for name in ("music", "vo", "sfx", "game")}
    for item in t.get("music", []):
        place(buses["music"], clip_of(item), item["at"])
    markers = []
    for item in t.get("vo", []):
        x = clip_of({"file": f"vo/{item['line']}_t{item['take']}.mp3", **item})
        place(buses["vo"], x, item["at"])
        markers.append({"at": item["at"], "end": round(item["at"] + len(x) / SR, 3), "text": item.get("text", "")})
    # Where each line is heard, for the picture's subtitles (generated, git-ignored).
    with open(os.path.join(os.path.dirname(TIMELINE), "vo_markers.json"), "w", encoding="utf-8") as f:
        json.dump(markers, f, indent=1)
    for item in t.get("sfx", []):
        place(buses["sfx"], clip_of({"file": f"sfx/{item['name']}_t{item.get('take', 1)}.mp3", **item}), item["at"])
    for item in t.get("game", []) + shot_sounds(t):
        place(buses["game"], clip_of(item), item["at"])

    mix_cfg = t.get("mix", {})
    duck = db(mix_cfg.get("duckDb", -7)) - 1
    env = envelope(buses["vo"], attack=0.04, release=0.35)
    buses["music"] *= (1 + duck * env)[:, None]
    buses["game"] *= (1 + duck * env)[:, None]
    for name, g in mix_cfg.get("busDb", {}).items():
        buses[name] *= db(g)

    mix = sum(buses.values())
    os.makedirs(os.path.dirname(out), exist_ok=True)
    stems = os.path.join(os.path.dirname(out), "stems")
    os.makedirs(stems, exist_ok=True)
    for name, bus in buses.items():
        sf.write(os.path.join(stems, f"{name}.wav"), bus, SR, subtype="FLOAT")
    # Master: gain to the target loudness into the true-peak limiter, measured and corrected until both hold.
    tmp = out + ".pre.wav"
    sf.write(tmp, mix, SR, subtype="FLOAT")
    gain, ceiling = TARGET_LUFS - loudness(tmp), CEILING_DB
    for _ in range(5):
        master = limiter(mix * db(gain), ceiling)
        sf.write(tmp, master, SR, subtype="FLOAT")
        lufs, tp = measure(tmp)
        if abs(lufs - TARGET_LUFS) <= 0.1 and tp <= TRUE_PEAK_DB:
            break
        gain += TARGET_LUFS - lufs
        if tp > TRUE_PEAK_DB:
            ceiling -= tp - TRUE_PEAK_DB + 0.05
    sf.write(out, master, SR, subtype="PCM_24")
    os.remove(tmp)
    lufs, tp = measure(out)
    print(f"wrote {os.path.relpath(out, REPO)}: {t['duration']:.1f}s, {lufs:.1f} LUFS, true peak {tp:.1f} dBTP")


if __name__ == "__main__":
    main()

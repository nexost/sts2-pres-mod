"""
The trailer's deliverables, from the master render (video only) and the mastered mix:
  presidents_of_the_spire_1440p60.mp4           YouTube: the master picture (2560x1440, 60 fps), AAC 384 kbps
  presidents_of_the_spire_1080p60.mp4           Reddit and anywhere else: 1920x1080, 60 fps, ~15 Mbps
  presidents_of_the_spire_1080p60_captions.mp4  the same with the narration burned in, for muted autoplay
  presidents_of_the_spire.en.srt                YouTube closed captions (subtitles.py)
  thumbnail_1280x720.jpg, thumbnail_1920x1080.png
  presidents_of_the_spire_mix.wav               the mastered sound, 48 kHz 24-bit

  python trailer/tools/export.py MASTER.mov [--out build/trailer/export]

MASTER is a ProRes HQ intermediate from PNG frames: `npx remotion render src/index.ts Trailer out/trailer_master.mov
--codec=prores --prores-profile=hq --image-format=png --color-space=bt709 --muted` in trailer/edit/ (Remotion's own
ffmpeg quit on a full-length x264 slow crf 10 encode, so the final encodes run here). The thumbnail is `npx remotion
still src/index.ts Thumbnail out/thumbnail.png`. Every file is checked for its loudness and true peak after encoding.
"""
import os
import shutil
import subprocess
import sys

from PIL import Image

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402
from mix import measure  # noqa: E402

TRAILER = os.path.join(REPO, "build", "trailer")
MIX = os.path.join(TRAILER, "media", "mix.wav")
THUMB = os.path.join(REPO, "trailer", "edit", "out", "thumbnail.png")
NAME = "presidents_of_the_spire"
BT709 = ["-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709"]


def run(cmd, cwd=None):
    subprocess.run(cmd, check=True, cwd=cwd)


def main():
    argv = sys.argv[1:]
    out = os.path.join(TRAILER, "export")
    if "--out" in argv:
        i = argv.index("--out")
        out = argv[i + 1]
        del argv[i:i + 2]
    master = os.path.abspath(argv[0])
    os.makedirs(out, exist_ok=True)
    ff = [presmod.FFMPEG, "-y", "-v", "error"]

    # YouTube: the master picture at high quality (YouTube re-encodes anyway, so feed it plenty), closed GOP with a
    # keyframe every half second as YouTube recommends, BT.709, the mix in AAC at its recommended 384 kbps.
    yt = os.path.join(out, f"{NAME}_1440p60.mp4")
    run(ff + ["-i", master, "-i", MIX, "-map", "0:v", "-map", "1:a", "-c:v", "libx264", "-preset", "slow", "-crf", "12",
              "-profile:v", "high", "-pix_fmt", "yuv420p", "-g", "30", "-bf", "2", "-flags", "+cgop"] + BT709 +
        ["-c:a", "aac", "-b:a", "384k", "-ar", "48000", "-shortest", "-movflags", "+faststart", yt])
    # 1080p60 for Reddit (1 GB limit) and general sharing.
    scale = ["-vf", "scale=1920:1080:flags=lanczos", "-c:v", "libx264", "-preset", "slow", "-crf", "16", "-maxrate", "20M", "-bufsize", "40M",
             "-pix_fmt", "yuv420p"] + BT709
    audio = ["-c:a", "aac", "-b:a", "256k", "-ar", "48000", "-shortest", "-movflags", "+faststart"]
    hd = os.path.join(out, f"{NAME}_1080p60.mp4")
    run(ff + ["-i", master, "-i", MIX, "-map", "0:v", "-map", "1:a"] + scale + audio + [hd])
    # The captions cut: libass with the trailer's own font. Paths stay relative (cwd = build/trailer): a drive letter's
    # colon would end the filter's argument.
    run([presmod.FFMPEG, "-y", "-v", "error", "-i", master, "-i", MIX, "-map", "0:v", "-map", "1:a",
         "-vf", f"scale=1920:1080:flags=lanczos,ass={os.path.relpath(os.path.join(out, 'captions.ass'), TRAILER).replace(os.sep, '/')}:fontsdir=media/fonts"]
        + scale[2:] + audio + [os.path.join(out, f"{NAME}_1080p60_captions.mp4")], cwd=TRAILER)
    # Thumbnails (YouTube wants 1280x720 under 2 MB) and the mix.
    if os.path.exists(THUMB):
        im = Image.open(THUMB).convert("RGB")
        im.save(os.path.join(out, "thumbnail_1920x1080.png"))
        im.resize((1280, 720), Image.LANCZOS).save(os.path.join(out, "thumbnail_1280x720.jpg"), quality=92)
    shutil.copy2(MIX, os.path.join(out, f"{NAME}_mix.wav"))

    for f in sorted(os.listdir(out)):
        path = os.path.join(out, f)
        line = f"{f:48s} {os.path.getsize(path) / 1e6:8.1f} MB"
        if f.endswith((".mp4", ".wav")):
            lufs, tp = measure(path)
            line += f"   {lufs:.1f} LUFS, true peak {tp:.1f} dBTP"
        print(line)


if __name__ == "__main__":
    main()

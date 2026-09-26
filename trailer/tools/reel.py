"""
A listening reel: several audio takes in one video, each with its name on screen while it plays, for reviewing voices
and music on a phone.

  python trailer/tools/reel.py OUT.mp4 "Label|Sub-label=file1[+file2...]" ...

Files joined with + play back to back with a short gap (e.g. one voice's three casting lines).
"""
import os
import subprocess
import sys
import tempfile

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

FONT = "C\\:/Windows/Fonts/arialbd.ttf"
GAP = 0.7


def esc(text):
    return text.replace("\\", "\\\\").replace(":", "\\:").replace("'", "’").replace("%", "\\%")


def segment(label, sub, files, out, index, total):
    inputs = []
    parts = []
    for i, f in enumerate(files):
        inputs += ["-i", f]
        parts.append(f"[{i}:a]aresample=48000,aformat=channel_layouts=stereo,apad=pad_dur={GAP}[a{i}]")
    joined = "".join(f"[a{i}]" for i in range(len(files)))
    audio = ";".join(parts) + f";{joined}concat=n={len(files)}:v=0:a=1[aout]"
    text = (f"drawtext=fontfile='{FONT}':text='{esc(label)}':fontcolor=white:fontsize=96:x=(w-text_w)/2:y=h/2-110,"
            f"drawtext=fontfile='{FONT}':text='{esc(sub)}':fontcolor=0xB8C0BC:fontsize=44:x=(w-text_w)/2:y=h/2+20,"
            f"drawtext=fontfile='{FONT}':text='{index} / {total}':fontcolor=0x6F7A75:fontsize=32:x=(w-text_w)/2:y=h-90")
    cmd = [presmod.FFMPEG, "-y", "-hide_banner", "-loglevel", "error"] + inputs + [
        "-f", "lavfi", "-i", "color=c=0x101614:s=1280x720:r=30",
        "-filter_complex", audio + f";[{len(files)}:v]{text}[vout]",
        "-map", "[vout]", "-map", "[aout]", "-shortest", "-c:v", "libx264", "-preset", "veryfast", "-crf", "28",
        "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k", out]
    subprocess.run(cmd, check=True)


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    out = sys.argv[1]
    items = sys.argv[2:]
    with tempfile.TemporaryDirectory() as tmp:
        parts = []
        for n, item in enumerate(items, 1):
            head, files = item.split("=", 1)
            label, _, sub = head.partition("|")
            part = os.path.join(tmp, f"{n:03d}.mp4")
            segment(label, sub, files.split("+"), part, n, len(items))
            parts.append(part)
        listing = os.path.join(tmp, "list.txt")
        with open(listing, "w", encoding="utf-8") as f:
            f.writelines(f"file '{p}'\n" for p in parts)
        os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
        subprocess.run([presmod.FFMPEG, "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0",
                        "-i", listing, "-c", "copy", "-movflags", "+faststart", out], check=True)
    print("wrote", out)


if __name__ == "__main__":
    main()

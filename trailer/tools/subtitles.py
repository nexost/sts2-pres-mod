"""
Subtitles for the trailer's narration, from trailer/edit/src/vo_markers.json (written by mix.py):
  <out>/presidents_of_the_spire.en.srt   every line, for YouTube's closed captions (upload it with the video)
  <out>/captions.ass                     the burned-in style for a muted-autoplay cut (Kreon, like the trailer's text);
                                         leaves out the lines the picture already shows as big type

  python trailer/tools/subtitles.py [--out build/trailer/export]
"""
import json
import os
import sys

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MARKERS = os.path.join(REPO, "trailer", "edit", "src", "vo_markers.json")
TIMELINE = os.path.join(REPO, "trailer", "edit", "src", "timeline.json")
GAP = 0.04  # seconds between two cues


def cues():
    """The narration lines with their times, each ending before the next starts."""
    lines = sorted(json.load(open(MARKERS, encoding="utf-8")), key=lambda m: m["at"])
    out = []
    for i, m in enumerate(lines):
        end = m["end"] + 0.15
        if i + 1 < len(lines):
            end = min(end, lines[i + 1]["at"] - GAP)
        out.append((m["at"], end, m["text"]))
    return out


def on_screen_text():
    """Lines the picture shows as big kinetic type (no need to caption them twice)."""
    t = json.load(open(TIMELINE, encoding="utf-8"))
    return {g["text"].strip().rstrip(".").lower() for g in t.get("graphics", []) if g["type"] == "kinetic"}


def srt_time(s):
    ms = int(round(s * 1000))
    return f"{ms // 3600000:02d}:{ms // 60000 % 60:02d}:{ms // 1000 % 60:02d},{ms % 1000:03d}"


def ass_time(s):
    cs = int(round(s * 100))
    return f"{cs // 360000}:{cs // 6000 % 60:02d}:{cs // 100 % 60:02d}.{cs % 100:02d}"


ASS_HEAD = """[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 0
ScaledBorderAndShadow: yes

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Narrator,Kreon,58,&H00F4FAFF,&H00FFFFFF,&H00100C08,&H96000000,-1,0,0,0,100,100,1,0,1,3.2,2.2,2,120,120,64,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
"""


def main():
    out = os.path.join(REPO, "build", "trailer", "export")
    if "--out" in sys.argv:
        out = sys.argv[sys.argv.index("--out") + 1]
    os.makedirs(out, exist_ok=True)
    all_cues = cues()
    with open(os.path.join(out, "presidents_of_the_spire.en.srt"), "w", encoding="utf-8") as f:
        for i, (a, b, text) in enumerate(all_cues, 1):
            f.write(f"{i}\n{srt_time(a)} --> {srt_time(b)}\n{text}\n\n")
    shown = on_screen_text()
    burned = [c for c in all_cues if c[2].strip().rstrip(".").lower() not in shown]
    with open(os.path.join(out, "captions.ass"), "w", encoding="utf-8") as f:
        f.write(ASS_HEAD)
        for a, b, text in burned:
            # A short fade so lines don't pop.
            f.write(f"Dialogue: 0,{ass_time(a)},{ass_time(b)},Narrator,,0,0,0,,{{\\fad(80,80)}}{text}\n")
    print(f"{len(all_cues)} cues in the .srt, {len(burned)} burned in -> {os.path.relpath(out, REPO)}")


if __name__ == "__main__":
    main()

"""
Launch the game in the mod's test mode and collect results.

  python scripts/test.py ui          scripted UI walk-through (char select, run start, card library, combat)
  python scripts/test.py autoslay    AutoSlay bot plays a full run as our character (god mode)
  python scripts/test.py deportsweep Every encounter: Deport non-boss enemies one at a time, a turn after each
  python scripts/test.py deportsweep SEED A,B  ...only encounters A and B
  python scripts/test.py cards       every card base and upgraded, key effects, relics, potions and all their text
  python scripts/test.py cards SEED relics     ...relic and potion checks only
  python scripts/test.py cards SEED A,B        ...relic and potion checks, then only cards A and B

Output: build/test/<mode>_<timestamp>/ with report.json, shots/*.png, godot.log excerpt.
Test saves live in .../modded_trumptest/, never in real profiles.
"""
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GAME_DIR = r"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
USER_DATA = os.path.join(os.environ["APPDATA"], "SlayTheSpire2")
TIMEOUTS = {"ui": 300, "autoslay": 3600, "deportsweep": 3600, "cards": 1800}


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "ui"
    seed = sys.argv[2] if len(sys.argv) > 2 else "TRUMPTEST1"
    extra = []
    if len(sys.argv) > 3:
        extra = ["--trump-only", sys.argv[3]] if mode == "cards" else ["--trump-encounters", sys.argv[3]]
    if subprocess.run(["tasklist", "/FI", "IMAGENAME eq SlayTheSpire2.exe"], capture_output=True, text=True).stdout.count("SlayTheSpire2.exe"):
        sys.exit("The game is already running; close it first.")
    out = os.path.join(ROOT, "build", "test", f"{mode}_{time.strftime('%Y%m%d_%H%M%S')}")
    os.makedirs(out)
    env = dict(os.environ, SteamAppId="2868840", SteamGameId="2868840")
    started = time.time()
    print(f"Launching game: mode={mode} seed={seed}\n  output: {out}", flush=True)
    proc = subprocess.Popen([os.path.join(GAME_DIR, "SlayTheSpire2.exe"), "--trump-test", mode, "--trump-out", out, "--trump-seed", seed] + extra,
                            cwd=GAME_DIR, env=env)
    try:
        code = proc.wait(timeout=TIMEOUTS.get(mode, 600))
    except subprocess.TimeoutExpired:
        proc.kill()
        code = "TIMEOUT"
    elapsed = time.time() - started

    log = newest_log(started)
    problems = []
    if log:
        shutil.copy2(log, os.path.join(out, "godot.log"))
        problems = scan_log(log)
    report_path = os.path.join(out, "report.json")
    report = json.load(open(report_path, encoding="utf-8")) if os.path.exists(report_path) else None

    print(f"\nExit code: {code}   ({elapsed:.0f}s)")
    if report:
        print(f"Harness: {'OK' if report['ok'] else 'ERRORS'}")
        for e in report["events"]:
            print("  ", e)
        for e in report["errors"]:
            print("  ERROR", e)
    else:
        print("Harness: no report written (game crashed or harness never started)")
    print(f"Log problems: {len(problems)}")
    for p in problems[:60]:
        print("  ", p)
    if mode in ("deportsweep", "cards") and log:
        sweep_summary(log, report, out)
    shots = sorted(glob.glob(os.path.join(out, "shots", "*.png")))
    print(f"Screenshots: {len(shots)} in {os.path.join(out, 'shots')}")
    ok = code == 0 and report is not None and report["ok"] and not any(p.startswith("!") for p in problems)
    print("\nRESULT:", "PASS" if ok else "FAIL")
    sys.exit(0 if ok else 1)


def sweep_summary(log, report, out):
    """Tie every log problem to the encounter that was running (BEGIN/END markers) and list the encounters with any."""
    per = {}
    current = None
    marker = re.compile(r"\[trump_character:sweep\] (BEGIN|END) (\S+)")
    lines = open(log, encoding="utf-8", errors="replace").read().splitlines()
    problem_lines = {int(p.split()[1][1:].rstrip(":")) for p in scan_log(log) if p.startswith("!")}
    for i, line in enumerate(lines, 1):
        m = marker.search(line)
        if m:
            current = m.group(2) if m.group(1) == "BEGIN" else None
        elif i in problem_lines and current:
            per.setdefault(current, []).append(f"L{i}: {line.strip()[:200]}")
    rows = (report or {}).get("sweep", [])
    deported = sum(d if isinstance(d, int) else len(d) for d in (r.get("deported", []) for r in rows))
    turns = sum(r.get("turnsAfterDeport", 0) for r in rows)
    print(f"Sweep: {len(rows)} encounters, {deported} enemies Deported, {turns} enemy turns played after a Deport")
    print(f"Encounters with problems: {len(per)}")
    for enc, items in per.items():
        print(f"  {enc}: {len(items)}")
        for item in items[:4]:
            print("     ", item)
    with open(os.path.join(out, "sweep_problems.json"), "w", encoding="utf-8") as fh:
        json.dump(per, fh, indent=1)


def newest_log(since):
    logs = glob.glob(os.path.join(USER_DATA, "logs", "godot*.log"))
    logs = [l for l in logs if os.path.getmtime(l) >= since - 5]
    return max(logs, key=os.path.getmtime) if logs else None


def scan_log(path):
    """Errors/exceptions, plus anything mentioning our mod. Lines starting with '!' are treated as failures."""
    problems = []
    # Case-sensitive on purpose: the test save folder "modded_trumptest" is not a mod error.
    ours = re.compile(r"trump_character|TRUMP|Trump")
    bad = re.compile(r"(Exception|\bERROR\b|\[ERROR\]|USER ERROR|SCRIPT ERROR|Failed to|Missing sprite|not found)", re.I)
    # Godot's leak report at process exit happens in the unmodded game too.
    exit_noise = re.compile(r"RID allocations of type|shaders of type .* were never freed|resources still in use at exit|RIDs of type .* were leaked")
    lines = open(path, encoding="utf-8", errors="replace").read().splitlines()
    for i, line in enumerate(lines):
        if bad.search(line) and not exit_noise.search(line):
            context = " ".join(lines[i:i + 3])
            tag = "!" if ours.search(context) or "Exception" in line else " "
            problems.append(f"{tag} L{i + 1}: {line.strip()[:300]}")
    return problems


if __name__ == "__main__":
    main()

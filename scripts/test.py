"""
Launch the game in the mod's test mode and collect results.

  python scripts/test.py ui          scripted UI walk-through (char select, run start, card library, combat)
  python scripts/test.py autoslay    AutoSlay bot plays a full run as our character (god mode)
  python scripts/test.py deportsweep Every encounter: Deport non-boss enemies one at a time, a turn after each
  python scripts/test.py deportsweep SEED A,B  ...only encounters A and B
  python scripts/test.py cards       every card base and upgraded, key effects, relics, potions and all their text
  python scripts/test.py cards SEED relics     ...relic and potion checks only
  python scripts/test.py cards SEED A,B        ...relic and potion checks, then only cards A and B
  python scripts/test.py balance CHARACTERS RUNS [SEED_PREFIX] [PARALLEL] [fullheal] [favor=Wall|Deport|Deals|Tweets]
                                     RUNS runs per character by the heuristic balance bot; CHARACTERS is e.g.
                                     TRUMP or TRUMP,IRONCLAD,SILENT (one shared queue). One game launch per run,
                                     PARALLEL at once, tiled on the main monitor and muted; results per character in
                                     build/balance/<character>_<time>/

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
TIMEOUTS = {"ui": 300, "autoslay": 3600, "deportsweep": 3600, "cards": 1800, "balance": 2400}


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "ui"
    if mode == "balance":
        options = sys.argv[6:]
        favor = next((o.split("=", 1)[1] for o in options if o.startswith("favor=")), None)
        balance_batch(sys.argv[2] if len(sys.argv) > 2 else "TRUMP", int(sys.argv[3]) if len(sys.argv) > 3 else 5,
                      sys.argv[4] if len(sys.argv) > 4 else "BAL", int(sys.argv[5]) if len(sys.argv) > 5 else 1,
                      fullheal="fullheal" in options, favor=favor)
        return
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


def style_cards(style):
    """Model IDs of The Donald's cards of one play style (the `arch` field in docs/design/cards.json)."""
    cards = json.load(open(os.path.join(ROOT, "docs", "design", "cards.json"), encoding="utf-8"))["cards"]
    return [re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", c["id"]).upper() for c in cards if c["arch"].lower() == style.lower()]


def balance_batch(characters, runs, prefix, parallel=1, fullheal=False, favor=None):
    """
    RUNS balance runs per character (seeds PREFIX000, PREFIX001, ...) through one queue, `parallel` games at a time.
    CHARACTERS is one name or a comma-separated list; every character's runs share the same pool, so the next game
    starts as soon as any slot frees up. Each slot owns a window tile and a test save folder (modded_bal<slot>),
    and a run only ever uses a slot nobody else holds.
    """
    import queue
    import threading
    from concurrent.futures import ThreadPoolExecutor
    hold = os.path.join(ROOT, "build", "balance", "HOLD")
    if os.path.exists(hold):
        sys.exit(f"{characters}: skipped, {hold} exists (delete it to run batches again)")
    if subprocess.run(["tasklist", "/FI", "IMAGENAME eq SlayTheSpire2.exe"], capture_output=True, text=True).stdout.count("SlayTheSpire2.exe"):
        sys.exit("The game is already running; close it first.")
    names = [c.strip().upper() for c in characters.split(",") if c.strip()]
    stamp = time.strftime('%Y%m%d_%H%M%S')
    tag = f"_{favor.upper()}" if favor else ""
    batches = {c: os.path.join(ROOT, "build", "balance", f"{c}{tag}_{stamp}") for c in names}
    favored = ",".join(style_cards(favor)) if favor else None
    for path in batches.values():
        os.makedirs(path)
    slots = queue.Queue()
    for slot in range(parallel):
        slots.put(slot)
    lock = threading.Lock()

    def one(job):
        character, i = job
        seed = f"{prefix}{i:03d}"
        out = os.path.join(batches[character], seed)
        os.makedirs(out)
        slot = slots.get()
        try:
            env = dict(os.environ, SteamAppId="2868840", SteamGameId="2868840")
            started = time.time()
            args = [os.path.join(GAME_DIR, "SlayTheSpire2.exe"), "--trump-test", "balance", "--trump-out", out,
                    "--trump-seed", seed, "--trump-character", character, "--trump-savedir", f"modded_bal{slot}",
                    "--trump-tile", f"{slot}/{parallel}", "--trump-mute"]
            if fullheal:
                args.append("--trump-fullheal")
            if favored:
                args += ["--trump-favor", favored]
            proc = subprocess.Popen(args, cwd=GAME_DIR, env=env)
            try:
                proc.wait(timeout=TIMEOUTS["balance"])
            except subprocess.TimeoutExpired:
                proc.kill()
        finally:
            slots.put(slot)
        path = os.path.join(out, "balance.json")
        run = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {"seed": seed, "character": character, "result": "no data"}
        with lock:
            print(f"{character:9} {seed}: {run.get('result'):8} act {run.get('act')} floor {run.get('floor')}  ({time.time() - started:.0f}s, slot {slot})", flush=True)
        return character, run

    jobs = [(c, i) for c in names for i in range(runs)]
    with ThreadPoolExecutor(max_workers=parallel) as pool:
        finished = list(pool.map(one, jobs))
    for character in names:
        results = [run for c, run in finished if c == character]
        with open(os.path.join(batches[character], "runs.json"), "w", encoding="utf-8") as fh:
            json.dump(results, fh, indent=1)
        print_balance_summary(character, results)
        print(f"  Batch: {batches[character]}")


def print_balance_summary(character, results):
    done = [r for r in results if r.get("result") in ("victory", "death")]
    wins = sum(r["result"] == "victory" for r in done)
    print(f"\n{character}: {len(done)} finished runs, {wins} wins ({100 * wins / max(1, len(done)):.0f}%), "
          f"average floor {sum(r.get('floor', 0) for r in done) / max(1, len(done)):.1f}")
    deaths = {}
    for r in done:
        if r["result"] == "death" and r.get("combats"):
            enc = r["combats"][-1]["encounter"]
            deaths[enc] = deaths.get(enc, 0) + 1
    if deaths:
        print("  Deaths: " + ", ".join(f"{k} x{v}" for k, v in sorted(deaths.items(), key=lambda kv: -kv[1])))
    by_type = {}
    for r in done:
        for c in r.get("combats", []):
            t = by_type.setdefault(c["type"], [0, 0, 0])
            t[0] += 1
            t[1] += c["hpBefore"] - c["hpAfter"]
            t[2] += c["turns"]
    for t, (n, lost, turns) in sorted(by_type.items()):
        print(f"  {t:8} fights {n:3}  HP lost {lost / n:5.1f}  turns {turns / n:4.1f}")


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

"""
Launch the game in the mod's test mode and collect results. Works for every character of the mod: pick one with
-c/--character (default: the first one in characters/).

  python scripts/test.py ui [-c trump]      scripted UI walk-through: char select, run start, card library, a fight with the
                                            pose checks and the character's own mechanics, then the shop and rest site
  python scripts/test.py autoslay [SEED]    AutoSlay bot plays a full run as the character (god mode)
  python scripts/test.py cards [SEED]       every card base and upgraded, key effects, relics, potions and all their text
  python scripts/test.py cards SEED relics  ...relic and potion checks only
  python scripts/test.py cards SEED A,B     ...relic and potion checks, then only cards A and B
  python scripts/test.py deportsweep [SEED] [A,B]  a character's own extra mode (Trump: Deport enemies one at a time,
                                            in every encounter or only A and B)
  python scripts/test.py balance CHARACTERS RUNS [SEED_PREFIX] [PARALLEL] [fullheal] [favor=STYLE]
                                            RUNS runs per character by the heuristic balance bot; CHARACTERS is e.g.
                                            TRUMP or TRUMP,IRONCLAD,SILENT (one shared queue). One game launch per run,
                                            PARALLEL at once, tiled on the main monitor and muted; results per character in
                                            build/balance/<character>_<time>/. favor=STYLE makes the bot prefer the cards
                                            whose "arch" is STYLE in characters/<id>/design/cards.json.
  python scripts/test.py coop [-c trump] [CLIENT_CHARACTER]  two instances play a co-op fight over localhost (the game's
                                            --fastmp), side by side; both must record the same state every turn (~4 min)
  python scripts/test.py cleansaves [ID,...]  remove test runs and run history that use the mod, or a character that
                                            was removed (e.g. SMOKETEST): needed after deleting a character, or the next
                                            test fails on the game's "model not found" errors

Output: build/test/<mode>_<timestamp>/ with report.json, shots/*.png, godot.log excerpt.
Test saves live in .../modded_prestest/ (balance: modded_bal<slot>/), never in real profiles.
"""
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import presmod  # noqa: E402

ROOT = presmod.REPO
GAME_DIR = presmod.GAME_DIR
USER_DATA = os.path.join(os.environ["APPDATA"], "SlayTheSpire2")
TIMEOUTS = {"ui": 300, "autoslay": 3600, "cards": 1800, "balance": 2400}
EXTRA_MODE_TIMEOUT = 3600


def take_character_option(argv):
    """Removes -c/--character NAME from argv; returns the character (a presmod.Character)."""
    for flag in ("-c", "--character"):
        if flag in argv:
            i = argv.index(flag)
            name = argv[i + 1]
            del argv[i:i + 2]
            return presmod.character(name)
    return presmod.character()


def clean_test_saves(removed_entries):
    """Runs the mod's save cleanup (Dev/SaveCleanup.cs) on each test save folder: run saves and run history that use
    the mod's models, or a removed character's (removed_entries, e.g. ["BIDEN"]). It goes through the game because
    Steam Cloud also syncs the test folders and would bring back files deleted by hand."""
    ids = ",".join(f"CHARACTER.{e.upper()}" for e in removed_entries)
    # The ui/cards/extra-mode saves; balance slots (modded_bal*) only ever hold the character they last ran.
    folders = sorted({os.path.basename(p) for p in glob.glob(os.path.join(USER_DATA, "steam", "*", "modded_prestest"))})
    env = dict(os.environ, SteamAppId="2868840", SteamGameId="2868840")
    for folder in folders:
        out = os.path.join(ROOT, "build", "test", f"cleansaves_{folder}_{time.strftime('%Y%m%d_%H%M%S')}")
        os.makedirs(out)
        args = [os.path.join(GAME_DIR, "SlayTheSpire2.exe"), "--pres-cleanup", "--pres-out", out, "--pres-savedir", folder]
        if ids:
            args += ["--pres-cleanup-ids", ids]
        subprocess.run(args, cwd=GAME_DIR, env=env, timeout=300)
        result = json.load(open(os.path.join(out, "cleanup_result.json"), encoding="utf-8"))
        print(f"{folder}: removed {len(result['removed'])} file(s)" + (f", errors: {result['errors']}" if result["errors"] else ""))


def coop_test(ch, client_character=None):
    """Two instances play a co-op fight over localhost (the game's --fastmp option), side by side and muted.
    Each writes its own report; the states both recorded at the start of every turn must be identical."""
    if subprocess.run(["tasklist", "/FI", "IMAGENAME eq SlayTheSpire2.exe"], capture_output=True, text=True).stdout.count("SlayTheSpire2.exe"):
        sys.exit("The game is already running; close it first.")
    out = os.path.join(ROOT, "build", "test", f"coop_{time.strftime('%Y%m%d_%H%M%S')}")
    env = dict(os.environ, SteamAppId="2868840", SteamGameId="2868840")
    roles = [("host", ["--fastmp", "host_standard"], ch.entry, "0/2"),
             ("client", ["--fastmp", "join", "--clientId", "1001"], (client_character or ch.entry).upper(), "1/2")]
    procs = []
    print(f"Launching co-op test: {roles[0][2]} (host) + {roles[1][2]} (client)\n  output: {out}", flush=True)
    for role, net, character, tile in roles:
        role_out = os.path.join(out, role)
        os.makedirs(role_out)
        args = [os.path.join(GAME_DIR, "SlayTheSpire2.exe"), "--log-file", os.path.join(role_out, "godot.log")] + net + [
            "--pres-test", "coop", "--pres-out", role_out, "--pres-character", character,
            "--pres-savedir", f"modded_coop_{role}", "--pres-tile", tile, "--pres-mute"]
        procs.append(subprocess.Popen(args, cwd=GAME_DIR, env=env))
        time.sleep(15 if role == "host" else 0)  # the host's lobby must be open before the client joins
    for p in procs:
        try:
            p.wait(timeout=600)
        except subprocess.TimeoutExpired:
            p.kill()
    reports = {}
    ok = True
    for role, *_ in roles:
        path = os.path.join(out, role, "report.json")
        report = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else None
        reports[role] = report
        print(f"\n[{role}] " + ("no report (crashed or never started)" if report is None else ("OK" if report["ok"] else "ERRORS")))
        if report is None:
            ok = False
            continue
        ok &= report["ok"]
        for e in report["events"]:
            print("  ", e)
        for e in report["errors"]:
            print("   ERROR", e)
        log = os.path.join(out, role, "godot.log")
        if os.path.exists(log):
            problems = scan_log(log)
            desync = [p for p in problems if re.search(r"checksum|desync|diverg", p, re.I)]
            ours = [p for p in problems if p.startswith("!")]
            print(f"   log: {len(problems)} problem lines, {len(ours)} about the mod, {len(desync)} about desyncs")
            for p in (ours + desync)[:15]:
                print("    ", p)
            ok &= not ours and not desync
    # The same states on both sides: players and enemies at the start of each turn.
    if reports.get("host") and reports.get("client"):
        host_states = {s["label"]: s for s in reports["host"].get("coop", [])}
        client_states = {s["label"]: s for s in reports["client"].get("coop", [])}
        labels = [l for l in host_states if l in client_states]
        diffs = [(l, json.dumps(host_states[l], sort_keys=True), json.dumps(client_states[l], sort_keys=True)) for l in labels
                 if json.dumps(host_states[l], sort_keys=True) != json.dumps(client_states[l], sort_keys=True)]
        print(f"\nState comparison: {len(labels)} turn(s) recorded by both, {len(diffs)} different")
        for label, a, b in diffs:
            print(f"  {label}\n    host:   {a[:600]}\n    client: {b[:600]}")
        ok &= bool(labels) and not diffs
    print(f"\nRESULT: {'PASS' if ok else 'FAIL'}")
    sys.exit(0 if ok else 1)


def main():
    argv = sys.argv[1:]
    ch = take_character_option(argv)
    mode = argv[0] if argv else "ui"
    if mode == "balance":
        options = argv[5:]
        favor = next((o.split("=", 1)[1] for o in options if o.startswith("favor=")), None)
        balance_batch(argv[1] if len(argv) > 1 else ch.entry, int(argv[2]) if len(argv) > 2 else 5,
                      argv[3] if len(argv) > 3 else "BAL", int(argv[4]) if len(argv) > 4 else 1,
                      fullheal="fullheal" in options, favor=favor)
        return
    if mode == "coop":
        coop_test(ch, argv[1] if len(argv) > 1 else None)
        return
    if mode == "cleansaves":
        clean_test_saves(argv[1].split(",") if len(argv) > 1 else [])
        return
    seed = argv[1] if len(argv) > 1 else "PRESTEST1"
    extra = ["--pres-character", ch.entry]
    if len(argv) > 2:
        extra += ["--pres-only", argv[2]] if mode == "cards" else ["--pres-encounters", argv[2]]
    if subprocess.run(["tasklist", "/FI", "IMAGENAME eq SlayTheSpire2.exe"], capture_output=True, text=True).stdout.count("SlayTheSpire2.exe"):
        sys.exit("The game is already running; close it first.")
    out = os.path.join(ROOT, "build", "test", f"{mode}_{time.strftime('%Y%m%d_%H%M%S')}")
    os.makedirs(out)
    env = dict(os.environ, SteamAppId="2868840", SteamGameId="2868840")
    started = time.time()
    print(f"Launching game: mode={mode} character={ch.entry} seed={seed}\n  output: {out}", flush=True)
    proc = subprocess.Popen([os.path.join(GAME_DIR, "SlayTheSpire2.exe"), "--pres-test", mode, "--pres-out", out, "--pres-seed", seed] + extra,
                            cwd=GAME_DIR, env=env)
    try:
        code = proc.wait(timeout=TIMEOUTS.get(mode, EXTRA_MODE_TIMEOUT))
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
    if (report or {}).get("sweep") and log:
        sweep_summary(log, report, out)
    shots = sorted(glob.glob(os.path.join(out, "shots", "*.png")))
    print(f"Screenshots: {len(shots)} in {os.path.join(out, 'shots')}")
    ok = code == 0 and report is not None and report["ok"] and not any(p.startswith("!") for p in problems)
    print("\nRESULT:", "PASS" if ok else "FAIL")
    sys.exit(0 if ok else 1)


def style_cards(character_entry, style):
    """Model IDs of a mod character's cards of one play style (the `arch` field in characters/<id>/design/cards.json).
    Empty for base-game characters."""
    if character_entry.lower() not in presmod.character_ids():
        return []
    cards = presmod.character(character_entry).cards["cards"]
    return [presmod.slug(c["id"]).upper() for c in cards if c.get("arch", "").lower() == style.lower()]


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
    favored = {c: ",".join(style_cards(c, favor)) for c in names} if favor else {}
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
            args = [os.path.join(GAME_DIR, "SlayTheSpire2.exe"), "--pres-test", "balance", "--pres-out", out,
                    "--pres-seed", seed, "--pres-character", character, "--pres-savedir", f"modded_bal{slot}",
                    "--pres-tile", f"{slot}/{parallel}", "--pres-mute"]
            if fullheal:
                args.append("--pres-fullheal")
            if favored.get(character):
                args += ["--pres-favor", favored[character]]
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
    marker = re.compile(r"\[" + presmod.MOD_ID + r":sweep\] (BEGIN|END) (\S+)")
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
    # Our mod id and every character's model ID and class name (case-sensitive, so save folder names don't match).
    names = [presmod.MOD_ID] + [n for c in presmod.characters() for n in (c.entry, c["class"])]
    ours = re.compile("|".join(re.escape(n) for n in names))
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

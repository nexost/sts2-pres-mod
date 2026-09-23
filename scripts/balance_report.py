"""
Compare balance-bot batches (scripts/test.py balance ...) across characters.

  python scripts/balance_report.py                 newest batch of every character in build/balance/
  python scripts/balance_report.py PREFIX          only batches whose runs used seeds starting with PREFIX (e.g. B18)

Prints, per character: runs, win rate, floors reached, HP lost per fight by act and room type, deaths by encounter.
For The Donald also: cards picked and played, Wall heights and Deports per fight, Gold gained.
"""
import glob
import json
import os
import statistics
import sys
from collections import Counter, defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load_batches(prefix):
    batches = {}
    for path in sorted(glob.glob(os.path.join(ROOT, "build", "balance", "*", "runs.json")), key=os.path.getmtime):
        runs = json.load(open(path, encoding="utf-8"))
        runs = [r for r in runs if r.get("result") in ("victory", "death")]
        for r in runs:
            # Early batches recorded victories as floor 0 (the run state is gone after the win); use the last fight.
            if r["result"] == "victory" and not r.get("floor") and r.get("combats"):
                r["floor"], r["act"] = r["combats"][-1]["floor"], r["combats"][-1]["act"]
        if not runs or (prefix and not str(runs[0].get("seed", "")).startswith(prefix)):
            continue
        batches[runs[0]["character"]] = (os.path.dirname(path), runs)
    return batches


def summarize(character, runs):
    floors = [r["floor"] for r in runs]
    wins = sum(r["result"] == "victory" for r in runs)
    print(f"\n=== {character}: {len(runs)} runs, {wins} wins ({100 * wins / len(runs):.0f}%), "
          f"floor avg {statistics.mean(floors):.1f} / median {statistics.median(floors):.0f} / best {max(floors)}")
    fights = defaultdict(list)
    for r in runs:
        for c in r["combats"]:
            fights[(c["act"], c["type"])].append(c)
    for (act, kind), cs in sorted(fights.items()):
        lost = [c["hpBefore"] - c["hpAfter"] for c in cs]
        turns = [c["turns"] for c in cs]
        died = sum(c["died"] for c in cs)
        print(f"  act {act} {kind:8} fights {len(cs):3}  HP lost avg {statistics.mean(lost):5.1f} (median {statistics.median(lost):4.0f})"
              f"  turns {statistics.mean(turns):4.1f}  deaths {died}")
    deaths = Counter(r["combats"][-1]["encounter"] for r in runs if r["result"] == "death" and r["combats"])
    print("  deaths by encounter: " + ", ".join(f"{k} {v}" for k, v in deaths.most_common(8)))


def donald_details(runs):
    picks = Counter()
    offered = Counter()
    plays = Counter()
    for r in runs:
        for p in r.get("picks", []):
            picks[p["picked"]] += 1
            offered.update(p["offered"])
        plays.update(r.get("plays", {}))
    combats = [c for r in runs for c in r["combats"]]
    walls = [c["maxWall"] for c in combats]
    print("  Wall max per fight: avg {:.1f}, median {:.0f}, reached 25+: {:.0f}%, 45+: {:.0f}%, 70+: {:.0f}%".format(
        statistics.mean(walls), statistics.median(walls),
        100 * sum(w >= 25 for w in walls) / len(walls), 100 * sum(w >= 45 for w in walls) / len(walls), 100 * sum(w >= 70 for w in walls) / len(walls)))
    print("  Deports per fight: {:.2f}, fights with a Deport: {:.0f}%".format(
        statistics.mean(c["deports"] for c in combats), 100 * sum(c["deports"] > 0 for c in combats) / len(combats)))
    print("  Gold gained per fight (rewards excluded): {:.1f}".format(statistics.mean(c["goldGained"] for c in combats)))
    print("  most played: " + ", ".join(f"{k} {v}" for k, v in plays.most_common(14)))
    rate = {k: picks[k] / offered[k] for k in offered if offered[k] >= 3}
    print("  picked when offered (3+ offers): " + ", ".join(f"{k} {v:.0%}" for k, v in sorted(rate.items(), key=lambda kv: -kv[1])[:12]))


def main():
    prefix = sys.argv[1] if len(sys.argv) > 1 else ""
    batches = load_batches(prefix)
    if not batches:
        sys.exit("No balance batches found.")
    for character, (path, runs) in batches.items():
        summarize(character, runs)
        if character == "TRUMP":
            donald_details(runs)
        print(f"  ({path})")


if __name__ == "__main__":
    main()

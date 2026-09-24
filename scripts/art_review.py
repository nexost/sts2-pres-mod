"""Art review tool (Step 7.5): every piece of art the mod needs, in one browser page.

  python scripts/art_review.py [--port 8190] [--host 127.0.0.1] [--no-browser]

For each item (cards, relics, potions, powers, character screens, UI): keep the current image, regenerate it
(one or several items at once, updated live in the page), flip between past versions, or edit its prompt.
"Keep" copies the game-ready files into mod/ (then `python scripts/build.py --install` puts them in the game).

It starts the headless ComfyUI (port 8189) itself when it isn't running, and stops it on exit.
State lives in build/art/review/state.json; every generated version is kept in build/art/review/items/.
Use --host 0.0.0.0 to open it from a phone on the same network.
"""
import argparse
import json
import mimetypes
import os
import queue
import random
import shutil
import subprocess
import sys
import threading
import time
import urllib.error
import urllib.request
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import unquote, urlparse

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import art_gen  # noqa: E402
import art_recipes  # noqa: E402

REPO = art_recipes.REPO
REVIEW = os.path.join(REPO, "build", "art", "review")
STATE_FILE = os.path.join(REVIEW, "state.json")
PAGE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "art_review", "index.html")
COMFY_DIR = r"D:\Comfy-Desktop\ComfyUI-Installs\ComfyUI\ComfyUI"
COMFY_MODELS_YAML = os.path.join(os.environ.get("APPDATA", ""), "Comfy Desktop", "shared_model_paths.yaml")
SERVE_ROOTS = [REVIEW, os.path.join(REPO, "build", "art", "ref"), os.path.join(art_recipes.GAME, "images"),
               os.path.join(art_recipes.GAME, "animations")]  # the last two: game art used as style references


def now():
    return time.time()


class Review:
    def __init__(self):
        self.lock = threading.RLock()
        self.items = art_recipes.load_items()
        self.by_id = {it["id"]: it for it in self.items}
        self.state = {"items": {}}
        if os.path.exists(STATE_FILE):
            with open(STATE_FILE, encoding="utf-8") as f:
                self.state = json.load(f)
        self.jobs = {j["id"]: j for j in self.state.pop("jobs", [])}  # local job id -> job (survive a restart)
        self.recall_trashed_numbers()
        for s in self.state["items"].values():
            if "reviewed" not in s and s["versions"] and s["kept"]:
                s["reviewed"] = max(v["v"] for v in s["versions"]) if s["current"] == s["kept"] else s["kept"]
        self.errors = {}        # item id -> last error
        self.listeners = []
        self.comfy_up = False
        self.comfy_proc = None
        self.durations = []     # recent generation times, for the ETA

    # ---- state --------------------------------------------------------------------------------------------
    def recall_trashed_numbers(self):
        """Version numbers are never reused: count the ones already moved to the trash too."""
        trash = os.path.join(REVIEW, "trash")
        if not os.path.isdir(trash):
            return
        for d in os.listdir(trash):
            meta = os.path.join(trash, d, "versions.json")
            if not os.path.exists(meta):
                continue
            item_id = d.rsplit("_", 1)[0].replace("__", ":")
            with open(meta, encoding="utf-8") as f:
                numbers = [v["v"] for v in json.load(f)]
            if item_id in self.by_id and numbers:
                s = self.st(item_id)
                s["last_v"] = max(s.get("last_v", 0), *numbers)
    def st(self, item_id):
        return self.state["items"].setdefault(item_id, {"versions": [], "current": None, "kept": None, "prompt": None})

    def save(self):
        tmp = STATE_FILE + ".tmp"
        with open(tmp, "w", encoding="utf-8") as f:
            json.dump(dict(self.state, jobs=list(self.jobs.values())), f, indent=1)
        os.replace(tmp, STATE_FILE)

    def url(self, path):
        for root in SERVE_ROOTS:
            if os.path.abspath(path).startswith(os.path.abspath(root)):
                rel = os.path.relpath(path, REPO).replace(os.sep, "/")
                return "/files/" + rel
        return None

    def vurl(self, path, v):
        """A version's file URL. The creation time makes it unique: version numbers used to be reused after a
        delete, and the browser (told these files never change) showed the old picture for the new one."""
        u = self.url(path)
        return f"{u}?t={int(v['created'] * 1000)}" if u else u

    @staticmethod
    def status(s):
        """none: no images. review: nothing kept yet, or versions made since the last Keep. kept: otherwise.
        Which version is on screen doesn't matter, so flipping between versions never changes the status."""
        if not s["versions"]:
            return "none"
        if not s["kept"]:
            return "review"
        newest = max(v["v"] for v in s["versions"])
        return "review" if newest > s.get("reviewed", s["kept"]) else "kept"

    def view(self, item_id):
        it = self.by_id[item_id]
        s = self.st(item_id)
        pend = [j for j in self.jobs.values() if j["item"] == item_id]
        big = (lambda v: v["raw"]) if it["kind"] == "char_button" else (lambda v: v["preview"])
        versions = [{"v": v["v"], "seed": v["seed"], "thumb": self.vurl(v["thumb"], v), "full": self.vurl(big(v), v),
                     "created": v["created"], "custom": v.get("custom", False),
                     "quality": art_recipes.QUALITY.get(v.get("quality") or "", {}).get("label", "Legacy (first batch)")}
                    for v in s["versions"]]
        cur = s["current"]
        status = self.status(s)
        return {
            "id": item_id, "section": it["section"], "kind": it["kind"], "name": it["name"], "sub": it["sub"],
            "arch": it.get("arch"), "ctype": it.get("ctype"), "rarity": it.get("rarity"), "text": it.get("text", ""),
            "art": it.get("art", ""), "note": it.get("note", ""), "frame": it.get("frame"), "frameMode": it.get("frameMode"),
            "prompt": s["prompt"] or it["prompt"], "promptCustom": bool(s["prompt"]), "defaultPrompt": it["prompt"],
            "refs": [self.url(r) or "" for r in it["refs"]], "outputs": list(it["outputs"].values()),
            "versions": versions, "current": cur, "kept": s["kept"], "status": status,
            "pending": [{"state": j["state"], "pos": j.get("pos"), "since": j.get("started") or j["created"],
                         "quality": art_recipes.QUALITY.get(j.get("quality") or "", {}).get("label", "")} for j in pend],
            "error": self.errors.get(item_id),
        }

    def summary(self):
        running = [j for j in self.jobs.values() if j["state"] == "running"]
        waiting = [j for j in self.jobs.values() if j["state"] != "running"]
        avg = sum(self.durations[-20:]) / len(self.durations[-20:]) if self.durations else 26.0
        return {"comfy": self.comfy_up, "running": len(running), "queued": len(waiting), "avg": round(avg, 1),
                "eta": round(avg * (len(running) + len(waiting))), "now": now()}

    # ---- live updates -------------------------------------------------------------------------------------
    def emit(self, msg):
        data = json.dumps(msg)
        for q in list(self.listeners):
            q.put(data)

    def emit_item(self, item_id):
        self.emit({"type": "item", "item": self.view(item_id)})

    def emit_summary(self):
        self.emit({"type": "summary", "summary": self.summary()})

    # ---- actions ------------------------------------------------------------------------------------------
    def regenerate(self, ids, count=1, quality=None):
        quality = quality if quality in art_recipes.QUALITY else art_recipes.DEFAULT_QUALITY
        with self.lock:
            for item_id in ids:
                if item_id not in self.by_id:
                    continue
                self.errors.pop(item_id, None)
                for _ in range(max(1, min(int(count), 8))):
                    jid = "j%d_%d" % (int(now() * 1000), random.randint(0, 1 << 30))
                    self.jobs[jid] = {"id": jid, "item": item_id, "state": "waiting", "created": now(),
                                      "seed": random.randint(1, 2 ** 31 - 1), "prompt_id": None, "quality": quality}
                self.emit_item(item_id)
            self.save()
            self.emit_summary()

    def keep(self, ids):
        with self.lock:
            for item_id in ids:
                s = self.st(item_id)
                v = self.version(item_id, s["current"])
                if not v:
                    continue
                for key, rel in self.by_id[item_id]["outputs"].items():
                    dst = os.path.join(art_recipes.MOD, rel.replace("/", os.sep))
                    os.makedirs(os.path.dirname(dst), exist_ok=True)
                    shutil.copyfile(v["files"][key], dst)
                s["kept"] = s["current"]
                s["reviewed"] = max(x["v"] for x in s["versions"])
                self.emit_item(item_id)
            self.save()

    def unkeep(self, ids):
        with self.lock:
            for item_id in ids:
                self.st(item_id)["kept"] = None
                self.emit_item(item_id)
            self.save()

    def delete_versions(self, item_id, numbers):
        """Remove versions from an item. The kept version is never removed. Files go to build/art/review/trash/
        (not erased), so a wrong click can be undone by hand."""
        with self.lock:
            s = self.st(item_id)
            doomed_numbers = set(numbers) - {s["kept"]}
            doomed = [v for v in s["versions"] if v["v"] in doomed_numbers]
            if not doomed:
                return 0
            trash = os.path.join(REVIEW, "trash", "%s_%d" % (item_id.replace(":", "__"), int(now() * 1000)))
            os.makedirs(trash, exist_ok=True)
            for v in doomed:
                for f in {v["raw"], v["preview"], v["thumb"], *v["files"].values()}:
                    if os.path.exists(f):
                        shutil.move(f, os.path.join(trash, os.path.basename(f)))
            with open(os.path.join(trash, "versions.json"), "w", encoding="utf-8") as f:
                json.dump(doomed, f, indent=1)
            s["last_v"] = max([s.get("last_v", 0)] + [v["v"] for v in s["versions"]])
            s["versions"] = [v for v in s["versions"] if v["v"] not in doomed_numbers]
            if s["current"] in doomed_numbers:
                s["current"] = s["kept"] or (s["versions"][-1]["v"] if s["versions"] else None)
            self.save()
            self.emit_item(item_id)
            return len(doomed)

    def prune(self, ids, legacy_only=False):
        """Delete every version that is neither kept nor the one currently shown (legacy_only: only first-batch ones)."""
        total = 0
        for item_id in ids:
            s = self.st(item_id)
            keep = {s["kept"], s["current"]} if not legacy_only else {s["kept"]}
            doomed = [v["v"] for v in s["versions"] if v["v"] not in keep and (not legacy_only or v.get("quality") in (None, "legacy"))]
            total += self.delete_versions(item_id, doomed)
        return total

    def set_version(self, item_id, v):
        with self.lock:
            if self.version(item_id, v):
                self.st(item_id)["current"] = v
                self.save()
                self.emit_item(item_id)

    def set_prompt(self, item_id, prompt):
        with self.lock:
            prompt = (prompt or "").strip()
            self.st(item_id)["prompt"] = prompt if prompt and prompt != self.by_id[item_id]["prompt"] else None
            self.save()
            self.emit_item(item_id)

    def cancel(self, ids):
        with self.lock:
            drop = [j for j in self.jobs.values() if j["item"] in ids]
            queued = [j["prompt_id"] for j in drop if j["prompt_id"] and j["state"] == "queued"]
            running = [j for j in drop if j["state"] == "running"]
            for j in drop:
                if j["state"] != "running":
                    del self.jobs[j["id"]]
            self.save()
        if queued:
            self.comfy_post("/queue", {"delete": queued})
        for j in running:
            self.comfy_post("/interrupt", {"prompt_id": j["prompt_id"]})
        with self.lock:
            for item_id in set(ids):
                if item_id in self.by_id:
                    self.emit_item(item_id)
            self.emit_summary()

    def version(self, item_id, v):
        return next((x for x in self.st(item_id)["versions"] if x["v"] == v), None)

    # ---- ComfyUI ------------------------------------------------------------------------------------------
    def comfy_get(self, path, timeout=5):
        return json.loads(urllib.request.urlopen(art_gen.SERVER + path, timeout=timeout).read())

    def comfy_post(self, path, data):
        try:
            return art_gen._post(path, data)
        except Exception as e:  # noqa: BLE001
            return {"error": str(e)}

    def check_comfy(self):
        try:
            self.comfy_get("/system_stats", timeout=2)
            return True
        except Exception:  # noqa: BLE001
            return False

    def start_comfy(self):
        python = os.path.join(COMFY_DIR, ".venv", "Scripts", "python.exe")
        log = open(os.path.join(REPO, "build", "art", "comfy.log"), "w", encoding="utf-8")
        args = [python, "-s", "main.py", "--extra-model-paths-config", COMFY_MODELS_YAML,
                "--output-directory", art_gen.COMFY_OUT, "--input-directory", art_gen.COMFY_IN,
                "--port", "8189", "--listen", "127.0.0.1", "--disable-pinned-memory", "--disable-auto-launch"]
        flags = getattr(subprocess, "CREATE_NO_WINDOW", 0)
        self.comfy_proc = subprocess.Popen(args, cwd=COMFY_DIR, stdout=log, stderr=subprocess.STDOUT, creationflags=flags)
        print("Starting ComfyUI (log: build/art/comfy.log)...", flush=True)

    def stop_comfy(self):
        if self.comfy_proc and self.comfy_proc.poll() is None:
            self.comfy_proc.terminate()
            print("Stopped ComfyUI.")

    def loop(self):
        """Background thread: submit waiting jobs, follow ComfyUI's queue, collect finished images."""
        started_comfy = False
        while True:
            try:
                up = self.check_comfy()
                if up != self.comfy_up:
                    self.comfy_up = up
                    self.emit_summary()
                if not up:
                    if not started_comfy and self.comfy_proc is None:
                        self.start_comfy()
                        started_comfy = True
                    time.sleep(1.5)
                    continue
                self.submit_waiting()
                self.follow_queue()
            except Exception as e:  # noqa: BLE001
                print("loop error:", e, flush=True)
            time.sleep(0.7)

    def submit_waiting(self):
        with self.lock:
            waiting = sorted((j for j in self.jobs.values() if j["state"] == "waiting"), key=lambda j: j["created"])
        for j in waiting:
            it = self.by_id[j["item"]]
            prompt = self.st(j["item"])["prompt"] or it["prompt"]
            job = {"name": j["item"].replace(":", "__") + "_%d" % j["seed"], "method": it["method"], "prompt": prompt,
                   "style": it["refs"], "size": it["size"], "seed": j["seed"]}
            job.update(art_recipes.quality_settings(j.get("quality"), it))
            try:
                res = art_gen._post("/prompt", {"prompt": art_gen.build_graph(job)})
            except urllib.error.HTTPError as e:
                self.fail(j, e.read().decode("utf-8", "replace")[:500])
                continue
            with self.lock:
                if j["id"] in self.jobs:
                    j.update(prompt_id=res["prompt_id"], state="queued", prompt=prompt, custom=bool(self.st(j["item"])["prompt"]))
                    self.save()
                    self.emit_item(j["item"])

    def follow_queue(self):
        q = self.comfy_get("/queue")
        running = [r[1] for r in q.get("queue_running", [])]
        pending = [r[1] for r in sorted(q.get("queue_pending", []), key=lambda r: r[0])]
        changed = set()
        with self.lock:
            jobs = [j for j in self.jobs.values() if j["prompt_id"]]
        for j in jobs:
            pid = j["prompt_id"]
            if pid in running:
                if j["state"] != "running":
                    j.update(state="running", started=now(), pos=None)
                    changed.add(j["item"])
            elif pid in pending:
                pos = pending.index(pid) + 1
                if j.get("pos") != pos:
                    j["pos"] = pos
                    changed.add(j["item"])
            else:
                hist = self.comfy_get("/history/" + pid)
                if pid not in hist:
                    # Not queued, not running, no result: ComfyUI restarted and lost it. Submit it again.
                    j.setdefault("missing", now())
                    if now() - j["missing"] > 10:
                        j.update(state="waiting", prompt_id=None, pos=None)
                        j.pop("missing", None)
                        changed.add(j["item"])
                    continue
                j.pop("missing", None)
                h = hist[pid]
                if h.get("status", {}).get("status_str") == "error":
                    msgs = [m for m in h["status"].get("messages", []) if m[0] == "execution_error"]
                    self.fail(j, msgs[0][1].get("exception_message", "error") if msgs else "ComfyUI error")
                    continue
                try:
                    self.finish(j, h)
                except Exception as e:  # noqa: BLE001
                    self.fail(j, f"post-processing failed: {e}")
        with self.lock:
            for item_id in changed:
                self.emit_item(item_id)
            if changed:
                self.emit_summary()

    def fail(self, j, message):
        with self.lock:
            self.jobs.pop(j["id"], None)
            self.save()
            self.errors[j["item"]] = message
            self.emit_item(j["item"])
            self.emit_summary()

    def finish(self, j, h):
        img = h["outputs"]["save"]["images"][0]
        src = os.path.join(art_gen.COMFY_OUT, img["subfolder"], img["filename"])
        item_id = j["item"]
        it = self.by_id[item_id]
        with self.lock:
            s = self.st(item_id)
            n = max([v["v"] for v in s["versions"]] + [s.get("last_v", 0)]) + 1
            s["last_v"] = n
        self.add_version(item_id, src, j["seed"], j.get("prompt") or it["prompt"], n, custom=j.get("custom", False),
                         quality=j.get("quality"))
        os.remove(src)
        with self.lock:
            if j.get("started"):
                self.durations.append(now() - j["started"])
            self.jobs.pop(j["id"], None)
            self.save()
            self.emit_item(item_id)
            self.emit_summary()

    def add_version(self, item_id, raw_src, seed, prompt, n, custom=False, make_current=True, quality=None):
        it = self.by_id[item_id]
        d = os.path.join(REVIEW, "items", item_id.replace(":", "__"))
        os.makedirs(d, exist_ok=True)
        raw = os.path.join(d, f"v{n}_raw.png")
        shutil.copyfile(raw_src, raw)
        files, preview = art_recipes.postprocess(it, raw, d, f"v{n}")
        thumb = os.path.join(d, f"v{n}_thumb.png")
        from PIL import Image
        t = Image.open(preview)
        t.thumbnail((520, 520), Image.LANCZOS)
        t.save(thumb)
        with self.lock:
            s = self.st(item_id)
            s["versions"].append({"v": n, "seed": seed, "prompt": prompt, "custom": custom, "created": now(), "quality": quality,
                                  "raw": raw, "preview": preview, "thumb": thumb, "files": files})
            if make_current or not s["current"]:
                s["current"] = n
            self.save()


# ---- Step 7 test pieces: imported once as the first versions of their items ----------------------------------
STEP7 = [
    ("card:build_the_wall", "B_build_the_wall_s%d.png", [1, 2, 3], 1),
    ("card:deport", "B_deport_s%d.png", [1, 2, 3], 3),
    ("card:net_worth", "B_net_worth_s%d.png", [1, 2, 3], 1),
    ("relic:golden_shovel", "B_golden_shovel_s%d.png", [1, 2, 3], 3),
    ("char:char_button", "B_portrait_v2_s%d.png", [1, 2, 3], 3),
]


def import_step7(r):
    test = os.path.join(REPO, "build", "art", "test")
    for item_id, pattern, seeds, chosen in STEP7:
        s = r.st(item_id)
        if s["versions"]:
            continue
        for i, seed in enumerate(seeds, 1):
            src = os.path.join(test, pattern % seed)
            if os.path.exists(src):
                r.add_version(item_id, src, seed, r.by_id[item_id]["prompt"], i, make_current=False)
        if s["versions"]:
            s["current"] = s["kept"] = chosen  # these are the files already in the mod
    r.save()


# ---- HTTP ------------------------------------------------------------------------------------------------------
def make_handler(r):
    class H(BaseHTTPRequestHandler):
        def log_message(self, *a):
            pass

        def send_json(self, obj, code=200):
            body = json.dumps(obj).encode()
            self.send_response(code)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def do_GET(self):
            path = urlparse(self.path).path
            if path in ("/", "/index.html"):
                body = open(PAGE, "rb").read()
                self.send_response(200)
                self.send_header("Content-Type", "text/html; charset=utf-8")
                self.send_header("Content-Length", str(len(body)))
                self.send_header("Cache-Control", "no-store")
                self.end_headers()
                self.wfile.write(body)
            elif path == "/api/state":
                with r.lock:
                    self.send_json({"items": [r.view(it["id"]) for it in r.items], "summary": r.summary(),
                                    "qualities": [{"key": k, "label": q["label"], "desc": q["desc"]} for k, q in art_recipes.QUALITY.items()],
                                    "defaultQuality": art_recipes.DEFAULT_QUALITY})
            elif path == "/api/events":
                self.send_response(200)
                self.send_header("Content-Type", "text/event-stream")
                self.send_header("Cache-Control", "no-cache")
                self.end_headers()
                q = queue.Queue()
                r.listeners.append(q)
                try:
                    while True:
                        try:
                            data = q.get(timeout=15)
                            self.wfile.write(f"data: {data}\n\n".encode())
                        except queue.Empty:
                            self.wfile.write(b": ping\n\n")
                        self.wfile.flush()
                except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError, OSError):
                    pass
                finally:
                    r.listeners.remove(q)
            elif path.startswith("/files/"):
                full = os.path.abspath(os.path.join(REPO, unquote(path[len("/files/"):])))
                if not any(full.startswith(os.path.abspath(root)) for root in SERVE_ROOTS) or not os.path.isfile(full):
                    self.send_error(404)
                    return
                body = open(full, "rb").read()
                self.send_response(200)
                self.send_header("Content-Type", mimetypes.guess_type(full)[0] or "application/octet-stream")
                self.send_header("Content-Length", str(len(body)))
                self.send_header("Cache-Control", "max-age=31536000, immutable")
                self.end_headers()
                self.wfile.write(body)
            else:
                self.send_error(404)

        def do_POST(self):
            path = urlparse(self.path).path
            data = json.loads(self.rfile.read(int(self.headers.get("Content-Length") or 0)) or b"{}")
            ids = [i for i in data.get("ids", []) if i in r.by_id]
            if path == "/api/regenerate":
                r.regenerate(ids, data.get("count", 1), data.get("quality"))
            elif path == "/api/generate_missing":
                with r.lock:
                    busy = {j["item"] for j in r.jobs.values()}
                    todo = [it["id"] for it in r.items if not r.st(it["id"])["versions"] and it["id"] not in busy
                            and (not data.get("section") or it["section"] == data["section"])]
                r.regenerate(todo, 1, data.get("quality"))
            elif path == "/api/keep":
                r.keep(ids)
            elif path == "/api/unkeep":
                r.unkeep(ids)
            elif path == "/api/version":
                r.set_version(data["id"], int(data["v"]))
            elif path == "/api/prompt":
                r.set_prompt(data["id"], data.get("prompt"))
            elif path == "/api/cancel":
                r.cancel(ids)
            elif path == "/api/delete":
                self.send_json({"ok": True, "deleted": r.delete_versions(data["id"], [int(v) for v in data.get("versions", [])])})
                return
            elif path == "/api/prune":
                self.send_json({"ok": True, "deleted": r.prune(ids, bool(data.get("legacyOnly")))})
                return
            else:
                self.send_error(404)
                return
            self.send_json({"ok": True})

    return H


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, default=8190)
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--no-browser", action="store_true")
    a = ap.parse_args()
    os.makedirs(REVIEW, exist_ok=True)
    r = Review()
    import_step7(r)
    threading.Thread(target=r.loop, daemon=True).start()
    server = ThreadingHTTPServer((a.host, a.port), make_handler(r))
    server.daemon_threads = True
    url = f"http://127.0.0.1:{a.port}/"
    print(f"Art review: {url}  ({len(r.items)} items). Ctrl+C to stop.", flush=True)
    if not a.no_browser:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        r.stop_comfy()


if __name__ == "__main__":
    sys.exit(main())

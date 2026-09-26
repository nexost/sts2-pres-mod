"""
The trailer's music cues with MiniMax Music 3 in ComfyUI (docs/00_project_plan.md, Step 12). Cues and their captions
are in trailer/audio/music.json.

  python trailer/tools/music.py [CUE,...] [--takes N] [--seed-base S]

Each take is a new seed. ComfyUI must be running (python scripts/art_review.py starts it, or Comfy Desktop); the
server is the COMFY_URL environment variable, default http://127.0.0.1:8189. Results are downloaded through ComfyUI's
API into build/trailer/audio/music/<cue>_s<seed>.flac, whatever ComfyUI's own output folder is.
"""
import json
import os
import sys
import time
import urllib.parse
import urllib.request

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SERVER = os.environ.get("COMFY_URL", "http://127.0.0.1:8189")
OUT = os.path.join(REPO, "build", "trailer", "audio", "music")


def post(path, data):
    req = urllib.request.Request(SERVER + path, data=json.dumps(data).encode(), headers={"Content-Type": "application/json"})
    return json.loads(urllib.request.urlopen(req, timeout=60).read())


def get(path):
    return json.loads(urllib.request.urlopen(SERVER + path, timeout=60).read())


def graph(cue, seed, steps, cfg):
    """The official template's graph (audio_minimax_music_3.json), in API form."""
    return {
        "1": {"class_type": "UNETLoader", "inputs": {"unet_name": "minimax_music3_dit_fp16.safetensors", "weight_dtype": "default"}},
        "2": {"class_type": "CLIPLoader", "inputs": {"clip_name": "minimax_music3_text_encoder_pruned_int8_convrot.safetensors", "type": "minimax", "device": "default"}},
        "3": {"class_type": "VAELoader", "inputs": {"vae_name": "minimax_music3_dav.safetensors"}},
        "4": {"class_type": "MiniMaxMusic3TextEncode", "inputs": {"clip": ["2", 0], "caption": cue["caption"], "lyrics": cue["lyrics"],
                                                                   "seed": seed, "max_duration": cue["seconds"], "cfg_scale": cfg, "top_k": 50}},
        "5": {"class_type": "ConditioningZeroOut", "inputs": {"conditioning": ["4", 0]}},
        "6": {"class_type": "EmptyMiniMaxMusic3LatentAudio", "inputs": {"seconds": ["4", 1], "batch_size": 1}},
        "7": {"class_type": "KSampler", "inputs": {"model": ["1", 0], "positive": ["4", 0], "negative": ["5", 0], "latent_image": ["6", 0],
                                                   "seed": seed, "steps": steps, "cfg": cfg, "sampler_name": "euler", "scheduler": "simple", "denoise": 1.0}},
        "8": {"class_type": "VAEDecodeAudio", "inputs": {"samples": ["7", 0], "vae": ["3", 0]}},
        "9": {"class_type": "SaveAudio", "inputs": {"audio": ["8", 0], "filename_prefix": f"trailer_music/{cue['name']}_s{seed}"}},
    }


def main():
    argv = sys.argv[1:]
    takes, seed_base = 4, 1
    if "--takes" in argv:
        i = argv.index("--takes")
        takes = int(argv[i + 1])
        del argv[i:i + 2]
    if "--seed-base" in argv:
        i = argv.index("--seed-base")
        seed_base = int(argv[i + 1])
        del argv[i:i + 2]
    spec = json.load(open(os.path.join(REPO, "trailer", "audio", "music.json"), encoding="utf-8"))
    only = set(argv[0].split(",")) if argv else None
    os.makedirs(OUT, exist_ok=True)
    jobs = []
    for cue in spec["cues"]:
        if only and cue["name"] not in only:
            continue
        for t in range(takes):
            seed = seed_base + t
            pid = post("/prompt", {"prompt": graph(cue, seed, spec["steps"], spec["cfg"])})["prompt_id"]
            jobs.append((cue["name"], seed, pid, time.time()))
    print(f"Queued {len(jobs)} takes", flush=True)
    for name, seed, pid, queued in jobs:
        while True:
            hist = get("/history/" + pid).get(pid)
            if hist and hist.get("status", {}).get("completed"):
                break
            if hist and hist.get("status", {}).get("status_str") == "error":
                print(f"{name} s{seed}: ComfyUI error {json.dumps(hist['status'])[:400]}", flush=True)
                break
            time.sleep(2)
        for node in (hist or {}).get("outputs", {}).values():
            for a in node.get("audio", []):
                q = urllib.parse.urlencode({"filename": a["filename"], "subfolder": a.get("subfolder", ""), "type": a.get("type", "output")})
                data = urllib.request.urlopen(f"{SERVER}/view?{q}", timeout=120).read()
                dst = os.path.join(OUT, f"{name}_s{seed}{os.path.splitext(a['filename'])[1]}")
                with open(dst, "wb") as f:
                    f.write(data)
                print(f"{name} s{seed}: {os.path.relpath(dst, REPO)} ({time.time() - queued:.0f}s since queued)", flush=True)


if __name__ == "__main__":
    main()

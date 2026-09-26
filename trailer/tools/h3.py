"""
The trailer's cinematic shots with MiniMax H3 (joint video + audio) in ComfyUI (docs/00_project_plan.md, Step 12).

  python trailer/tools/h3.py JOBS.json [--only NAME,...] [--seeds 1,2] [--preset quality|quality_sage|turbo]

JOBS.json (e.g. trailer/ai/shots.json): {"jobs": [{"name": "...", "prompt": "...", "first": "path.png", "last": "path.png"
(optional), "seconds": 5.17, "width": 1344, "height": 768}]}. Paths are relative to the repo.

Presets (the research behind them is in the plan's T5 notes):
  quality       the released configuration: res_multistep, 20 steps (21 sigmas), shift 12 video / 3 audio, guidance 1,
                no acceleration, the native 1344x768 canvas.
  quality_sage  the same with Sage attention (about twice as fast; the docs call the loss minimal).
  turbo         the 4-8 step Turbo LoRA with Spectrum step forecasting (fast; the user's earlier workflow).

Frames come back as lossless PNGs through ComfyUI's API and are assembled here: build/trailer/ai/<name>_s<seed>_<preset>.mp4
(H.264 at high quality with H3's own audio) plus the frames folder for the edit.
"""
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.parse
import urllib.request
import uuid

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

SERVER = os.environ.get("COMFY_URL", "http://127.0.0.1:8189")
OUT = os.path.join(REPO, "build", "trailer", "ai")
UNET = os.environ.get("H3_UNET", "minimax_h3_fl2va_pruned_int8_convrot.safetensors")


def post(path, data):
    req = urllib.request.Request(SERVER + path, data=json.dumps(data).encode(), headers={"Content-Type": "application/json"})
    return json.loads(urllib.request.urlopen(req, timeout=60).read())


def get(path):
    return json.loads(urllib.request.urlopen(SERVER + path, timeout=60).read())


def upload(path):
    """Sends an image to ComfyUI's input folder (whatever folder the running server uses); returns its name there."""
    name = f"trailer_{uuid.uuid4().hex[:8]}_{os.path.basename(path)}"
    boundary = uuid.uuid4().hex
    with open(path, "rb") as f:
        payload = f.read()
    body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"image\"; filename=\"{name}\"\r\nContent-Type: image/png\r\n\r\n").encode() + payload + \
        f"\r\n--{boundary}\r\nContent-Disposition: form-data; name=\"overwrite\"\r\n\r\ntrue\r\n--{boundary}--\r\n".encode()
    req = urllib.request.Request(SERVER + "/upload/image", data=body, headers={"Content-Type": f"multipart/form-data; boundary={boundary}"})
    return json.loads(urllib.request.urlopen(req, timeout=60).read())["name"]


def frames_for(seconds):
    """The model's 17k+5 grid at 24 fps (124 = 5.17 s)."""
    n = max(5, round(seconds * 24))
    return n + (5 - n % 17) % 17


def graph(job, seed, preset, first=None, last=None):
    g = {
        "unet": {"class_type": "UNETLoader", "inputs": {"unet_name": UNET, "weight_dtype": "default"}},
        "clip": {"class_type": "CLIPLoader", "inputs": {"clip_name": "qwen3vl_32b_minimax_h3_nvfp4_awq.safetensors", "type": "minimax", "device": "default"}},
        "vae": {"class_type": "VAELoader", "inputs": {"vae_name": "minimax_h3_video_vae_fp16.safetensors"}},
        "avae": {"class_type": "VAELoader", "inputs": {"vae_name": "minimax_h3_audio_vae_fp32.safetensors"}},
        "cond": {"class_type": "MiniMaxH3ImageToVideo", "inputs": {"clip": ["clip", 0], "vae": ["vae", 0], "prompt": job["prompt"],
                                                                "width": job.get("width", 1344), "height": job.get("height", 768),
                                                                "length": frames_for(job.get("seconds", 5.17))}},
        "noise": {"class_type": "RandomNoise", "inputs": {"noise_seed": seed}},
        "guider": {"class_type": "BasicGuider", "inputs": {"model": ["model", 0], "conditioning": ["cond", 0]}},
        "sample": {"class_type": "SamplerCustomAdvanced", "inputs": {"noise": ["noise", 0], "guider": ["guider", 0], "sampler": ["sampler", 0],
                                                                     "sigmas": ["sigmas", 0], "latent_image": ["cond", 1]}},
        "decode": {"class_type": "VAEDecode", "inputs": {"samples": ["sample", 0], "vae": ["vae", 0]}},
        "adecode": {"class_type": "VAEDecodeAudio", "inputs": {"samples": ["sample", 0], "vae": ["avae", 0]}},
        "save": {"class_type": "SaveImage", "inputs": {"images": ["decode", 0], "filename_prefix": f"trailer_h3/{job['name']}_s{seed}_{preset}/f"}},
        "asave": {"class_type": "SaveAudio", "inputs": {"audio": ["adecode", 0], "filename_prefix": f"trailer_h3/{job['name']}_s{seed}_{preset}/audio"}},
    }
    if first:
        g["first"] = {"class_type": "LoadImage", "inputs": {"image": first}}
        g["cond"]["inputs"]["first_frame"] = ["first", 0]
    if last:
        g["last"] = {"class_type": "LoadImage", "inputs": {"image": last}}
        g["cond"]["inputs"]["last_frame"] = ["last", 0]
    model = ["unet", 0]
    if preset == "turbo":
        g["turbo"] = {"class_type": "MiniMaxH3TurboLoRA", "inputs": {"model": model, "lora_name": "minimax h3\\minimax_h3_turbo_v4_step600_ema_pruned_comfyui.safetensors", "strength": 1.0}}
        model = ["turbo", 0]
    if preset in ("quality_sage", "turbo"):
        g["sage"] = {"class_type": "PathchSageAttentionKJ", "inputs": {"model": model, "sage_attention": "auto", "allow_compile": False}}
        model = ["sage", 0]
    g["shift"] = {"class_type": "MiniMaxH3SigmaShift", "inputs": {"model": model, "shift_video": 12.0, "shift_audio": 6.0 if preset == "turbo" else 3.0}}
    model = ["shift", 0]
    if preset == "turbo":
        g["spectrum"] = {"class_type": "SpectrumApplyMiniMaxH3", "inputs": {
            "model": model, "enabled": True, "blend_weight": 0.5, "degree": 1, "ridge_lambda": 0.1, "window_size": 2.0, "flex_window": 0.75,
            "warmup_steps": 3, "tail_actual_steps": 1, "max_history": 8, "debug": False, "history_storage": "system_ram",
            "bootstrap_first_forecast": True, "anchor_residual_feedback": False, "selective_rollback_correction": False,
            "offline_smoothing_replay": False, "audio_blend_weight": 0.5}}
        model = ["spectrum", 0]
    g["guider"]["inputs"]["model"] = model
    steps, sampler = (6, "euler") if preset == "turbo" else (20, "res_multistep")
    g["sigmas"] = {"class_type": "BasicScheduler", "inputs": {"model": model, "scheduler": "simple", "steps": steps, "denoise": 1.0}}
    g["sampler"] = {"class_type": "KSamplerSelect", "inputs": {"sampler_name": sampler}}
    return g


def fetch(outputs, dest):
    os.makedirs(os.path.join(dest, "frames"), exist_ok=True)
    audio = None
    for node in outputs.values():
        for item in node.get("images", []):
            q = urllib.parse.urlencode({"filename": item["filename"], "subfolder": item.get("subfolder", ""), "type": item.get("type", "output")})
            with open(os.path.join(dest, "frames", item["filename"]), "wb") as f:
                f.write(urllib.request.urlopen(f"{SERVER}/view?{q}", timeout=120).read())
        for item in node.get("audio", []):
            q = urllib.parse.urlencode({"filename": item["filename"], "subfolder": item.get("subfolder", ""), "type": item.get("type", "output")})
            audio = os.path.join(dest, "audio" + os.path.splitext(item["filename"])[1])
            with open(audio, "wb") as f:
                f.write(urllib.request.urlopen(f"{SERVER}/view?{q}", timeout=120).read())
    return audio


def assemble(dest, audio, out):
    frames = sorted(os.listdir(os.path.join(dest, "frames")))
    pattern = os.path.join(dest, "frames", frames[0].rsplit("_", 2)[0] + "_%05d_.png")
    cmd = [presmod.FFMPEG, "-y", "-v", "error", "-framerate", "24", "-start_number", frames[0].rsplit("_", 2)[1], "-i", pattern]
    if audio:
        cmd += ["-i", audio, "-c:a", "aac", "-b:a", "192k", "-shortest"]
    cmd += ["-c:v", "libx264", "-preset", "slow", "-crf", "12", "-pix_fmt", "yuv420p", "-movflags", "+faststart", out]
    subprocess.run(cmd, check=True)


def main():
    argv = sys.argv[1:]
    opts = {"--only": None, "--seeds": "1", "--preset": "quality"}
    for flag in list(opts):
        if flag in argv:
            i = argv.index(flag)
            opts[flag] = argv[i + 1]
            del argv[i:i + 2]
    spec = json.load(open(os.path.join(REPO, argv[0]), encoding="utf-8"))
    only = set(opts["--only"].split(",")) if opts["--only"] else None
    seeds = [int(s) for s in opts["--seeds"].split(",")]
    presets = opts["--preset"].split(",")
    queued = []
    for job in spec["jobs"]:
        if only and job["name"] not in only:
            continue
        first = upload(os.path.join(REPO, job["first"])) if job.get("first") else None
        last = upload(os.path.join(REPO, job["last"])) if job.get("last") else None
        for preset in presets:
            for seed in seeds:
                pid = post("/prompt", {"prompt": graph(job, seed, preset, first, last)})["prompt_id"]
                queued.append((job["name"], seed, preset, pid, time.time()))
    print(f"Queued {len(queued)} clips", flush=True)
    os.makedirs(OUT, exist_ok=True)
    for name, seed, preset, pid, t0 in queued:
        while True:
            hist = get("/history/" + pid).get(pid)
            status = (hist or {}).get("status", {})
            if status.get("completed") or status.get("status_str") == "error":
                break
            time.sleep(3)
        if status.get("status_str") == "error":
            print(f"{name} s{seed} {preset}: ComfyUI error {json.dumps(status)[:600]}", flush=True)
            continue
        tag = f"{name}_s{seed}_{preset}"
        dest = os.path.join(OUT, tag)
        if os.path.isdir(dest):
            shutil.rmtree(dest)
        audio = fetch(hist["outputs"], dest)
        assemble(dest, audio, os.path.join(OUT, tag + ".mp4"))
        print(f"{tag}: {os.path.relpath(os.path.join(OUT, tag + '.mp4'), REPO)} ({time.time() - t0:.0f}s since queued)", flush=True)


if __name__ == "__main__":
    main()

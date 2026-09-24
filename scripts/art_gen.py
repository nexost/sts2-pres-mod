"""Generate art through a local ComfyUI server (Krea 2).

Two methods, compared in Step 7:
  styleref  Krea 2 Turbo + the krea2_style_reference LoRA: a game image is passed in as the style reference.
  lora      Krea 2 Turbo + a style LoRA trained on the game's art (plain text-to-image).

Usage:
  python scripts/art_gen.py JOBS.json [--only NAME,NAME] [--seeds N]

JOBS.json is a list of jobs:
  {"name": "build_the_wall", "method": "styleref", "prompt": "...", "style": ["path/to/ref.png"],
   "size": [1216, 928], "seed": 1, "loras": [["file.safetensors", 1.0]], "out": "build/art/test/x.png"}

The server must be running (see docs/07_step7_report.md for the launch command). Default: http://127.0.0.1:8189.
"""
import argparse
import json
import os
import shutil
import sys
import time
import urllib.request

SERVER = os.environ.get("COMFY_URL", "http://127.0.0.1:8189")
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COMFY_IN = os.path.join(REPO, "build", "art", "comfy_in")
COMFY_OUT = os.path.join(REPO, "build", "art", "comfy_out")

UNET = "krea2_turbo_int8_convrot.safetensors"
CLIP = "qwen3vl_4b_fp8_scaled.safetensors"
VAE = "qwen_image_vae.safetensors"
STYLE_LORA = "krea2_style_reference.safetensors"


def _post(path, data):
    req = urllib.request.Request(SERVER + path, json.dumps(data).encode(), {"Content-Type": "application/json"})
    return json.loads(urllib.request.urlopen(req).read())


def _get(path):
    return json.loads(urllib.request.urlopen(SERVER + path).read())


def stage_input(path):
    """Copy an image into ComfyUI's input folder and return the name LoadImage expects."""
    os.makedirs(COMFY_IN, exist_ok=True)
    name = os.path.basename(os.path.dirname(path)) + "__" + os.path.basename(path)
    shutil.copyfile(path, os.path.join(COMFY_IN, name))
    return name


def build_graph(job):
    w, h = job.get("size", [1024, 1024])
    seed = job.get("seed", 1)
    steps = job.get("steps", 8)
    g = {
        "unet": {"class_type": "UNETLoader", "inputs": {"unet_name": job.get("unet", UNET), "weight_dtype": "default"}},
        "clip": {"class_type": "CLIPLoader", "inputs": {"clip_name": CLIP, "type": "krea2", "device": "default"}},
        "vae": {"class_type": "VAELoader", "inputs": {"vae_name": VAE}},
    }
    model = ["unet", 0]
    loras = list(job.get("loras", []))
    if job["method"] == "styleref":
        loras.insert(0, [STYLE_LORA, job.get("style_strength", 1.0)])
    for i, (name, strength) in enumerate(loras):
        g[f"lora{i}"] = {"class_type": "LoraLoaderModelOnly",
                         "inputs": {"model": model, "lora_name": name, "strength_model": strength}}
        model = [f"lora{i}", 0]
    g["msf"] = {"class_type": "ModelSamplingFlux",
                "inputs": {"model": model, "max_shift": 1.15, "base_shift": 0.5, "width": w, "height": h}}

    if job["method"] == "styleref":
        enc = {"clip": ["clip", 0], "vae": ["vae", 0], "prompt": job["prompt"]}
        for i, path in enumerate(job["style"][:3]):
            g[f"ref{i}"] = {"class_type": "LoadImage", "inputs": {"image": stage_input(path)}}
            enc[f"image{i + 1}"] = [f"ref{i}", 0]
        g["enc"] = {"class_type": "TextEncodeQwenImageEditPlus", "inputs": enc}
        g["pos"] = {"class_type": "FluxKontextMultiReferenceLatentMethod",
                    "inputs": {"conditioning": ["enc", 0], "reference_latents_method": "index_timestep_zero"}}
        positive = ["pos", 0]
    else:
        g["enc"] = {"class_type": "CLIPTextEncode", "inputs": {"clip": ["clip", 0], "text": job["prompt"]}}
        positive = ["enc", 0]
    g["neg"] = {"class_type": "ConditioningZeroOut", "inputs": {"conditioning": positive}}
    g["guider"] = {"class_type": "CFGGuider",
                   "inputs": {"model": ["msf", 0], "positive": positive, "negative": ["neg", 0], "cfg": job.get("cfg", 1.0)}}
    g["sampler"] = {"class_type": "KSamplerSelect", "inputs": {"sampler_name": job.get("sampler", "euler")}}
    g["sched"] = {"class_type": "BasicScheduler",
                  "inputs": {"model": ["msf", 0], "scheduler": job.get("scheduler", "simple"), "steps": steps, "denoise": 1.0}}
    g["noise"] = {"class_type": "RandomNoise", "inputs": {"noise_seed": seed}}
    g["latent"] = {"class_type": "EmptyLatentImage", "inputs": {"width": w, "height": h, "batch_size": 1}}
    g["sample"] = {"class_type": "SamplerCustomAdvanced",
                   "inputs": {"noise": ["noise", 0], "guider": ["guider", 0], "sampler": ["sampler", 0],
                              "sigmas": ["sched", 0], "latent_image": ["latent", 0]}}
    final = ["sample", 0]
    hires = job.get("hires")
    if hires:
        # Second pass: upscale the latent and re-sample it partially, which adds detail and cleans up smears.
        g["up"] = {"class_type": "LatentUpscaleBy", "inputs": {"samples": final, "upscale_method": "bislerp", "scale_by": hires.get("scale", 1.5)}}
        g["sched2"] = {"class_type": "BasicScheduler",
                       "inputs": {"model": ["msf", 0], "scheduler": job.get("scheduler", "simple"),
                                  "steps": hires.get("steps", steps), "denoise": hires.get("denoise", 0.45)}}
        g["noise2"] = {"class_type": "RandomNoise", "inputs": {"noise_seed": seed + 1}}
        g["sample2"] = {"class_type": "SamplerCustomAdvanced",
                        "inputs": {"noise": ["noise2", 0], "guider": ["guider", 0], "sampler": ["sampler", 0],
                                   "sigmas": ["sched2", 0], "latent_image": ["up", 0]}}
        final = ["sample2", 0]
    g["decode"] = {"class_type": "VAEDecode", "inputs": {"samples": final, "vae": ["vae", 0]}}
    g["save"] = {"class_type": "SaveImage", "inputs": {"images": ["decode", 0], "filename_prefix": "sts2trump/" + job["name"]}}
    return g


def run(job):
    t0 = time.time()
    pid = _post("/prompt", {"prompt": build_graph(job)})["prompt_id"]
    while True:
        hist = _get("/history/" + pid)
        if pid in hist:
            h = hist[pid]
            if h.get("status", {}).get("status_str") == "error":
                raise RuntimeError(json.dumps(h["status"].get("messages"))[:2000])
            imgs = h["outputs"]["save"]["images"]
            src = os.path.join(COMFY_OUT, imgs[0]["subfolder"], imgs[0]["filename"])
            break
        time.sleep(0.5)
    out = job.get("out")
    if out:
        os.makedirs(os.path.dirname(out), exist_ok=True)
        shutil.copyfile(src, out)
    print(f"{job['name']}: {time.time() - t0:.1f}s -> {out or src}", flush=True)
    return out or src


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("jobs")
    ap.add_argument("--only", default="")
    ap.add_argument("--seeds", type=int, default=1, help="run each job with this many seeds (seed, seed+1, ...)")
    a = ap.parse_args()
    jobs = json.load(open(a.jobs, encoding="utf-8"))
    only = set(filter(None, a.only.split(",")))
    for job in jobs:
        if only and job["name"] not in only:
            continue
        for k in range(a.seeds):
            j = dict(job, seed=job.get("seed", 1) + k)
            if a.seeds > 1:
                j["name"] = f"{job['name']}_s{j['seed']}"
                if job.get("out"):
                    base, ext = os.path.splitext(job["out"])
                    j["out"] = f"{base}_s{j['seed']}{ext}"
            run(j)


if __name__ == "__main__":
    sys.exit(main())

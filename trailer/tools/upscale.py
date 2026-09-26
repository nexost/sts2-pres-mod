"""
Finish the trailer's H3 shots for the 1440p60 master: SeedVR2 upscales them to 1440p (temporally stable), then RIFE
interpolates 5x and every other frame is kept (24 fps -> 120 -> exactly 60 fps). Runs through ComfyUI's API.

  python trailer/tools/upscale.py TAG [TAG...] [--model seedvr2_ema_7b_fp16.safetensors] [--batch 21] [--swap 36] [--cap N] [--debug] [--no-rife]

--swap moves that many DiT blocks to system RAM (7B has 36): SeedVR2 is a one-step model, so the swap costs one weight transfer
per batch, and it leaves the 4090's VRAM for the frames. Without it the 7B at 1440p spills into shared memory and crawls.
Batch 21 (a 4n+1 size) peaks at ~12 GB in the DiT and ~18 GB in the tiled VAE decode; 41 overflows 24 GB.
--cap N upscales only the first N frames (a quick memory and speed probe); --debug prints SeedVR2's VRAM report.

TAG is a take folder in build/trailer/ai/ (frames/ and audio.*, written by h3.py). Output:
  build/trailer/media/ai/<TAG>.mp4   2520x1440 at 60 fps with H3's audio, for the edit
  build/trailer/ai/<TAG>_up/         the 1440p frames at 24 fps
The running ComfyUI writes into build/art/comfy_out/ (art_review.py starts it that way); frames are read from there.
"""
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.request

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(REPO, "scripts"))
import presmod  # noqa: E402

SERVER = os.environ.get("COMFY_URL", "http://127.0.0.1:8189")
AI = os.path.join(REPO, "build", "trailer", "ai")
COMFY_OUT = os.path.join(REPO, "build", "art", "comfy_out")
MEDIA = os.path.join(REPO, "build", "trailer", "media", "ai")
RIFE_CHUNK = 20          # source frames per RIFE pass (+1 shared with the next), to keep memory reasonable
RIFE_MULT = 5            # 24 fps x 5 = 120 fps, every other frame kept = 60 fps


def post(path, data):
    req = urllib.request.Request(SERVER + path, data=json.dumps(data).encode(), headers={"Content-Type": "application/json"})
    return json.loads(urllib.request.urlopen(req, timeout=60).read())


def run_graph(graph, label):
    pid = post("/prompt", {"prompt": graph})["prompt_id"]
    t0 = time.time()
    while True:
        hist = json.loads(urllib.request.urlopen(f"{SERVER}/history/{pid}", timeout=60).read()).get(pid)
        status = (hist or {}).get("status", {})
        if status.get("completed"):
            break
        if status.get("status_str") == "error":
            raise RuntimeError(f"{label}: ComfyUI error {json.dumps(status)[:800]}")
        time.sleep(3)
    files = []
    for node in hist["outputs"].values():
        for item in node.get("images", []):
            files.append(os.path.join(COMFY_OUT, item.get("subfolder", ""), item["filename"]))
    print(f"  {label}: {len(files)} frames in {time.time() - t0:.0f}s", flush=True)
    return sorted(files)


def seedvr2_graph(folder, prefix, model, batch, swap, cap, debug):
    return {
        "load": {"class_type": "LoadImagesFromFolderKJ", "inputs": {"folder": folder, "width": -1, "height": -1, "keep_aspect_ratio": "crop",
                                                                   "image_load_cap": cap, "start_index": 0, "include_subfolders": False}},
        "dit": {"class_type": "SeedVR2LoadDiTModel", "inputs": {"model": model, "device": "cuda:0", "blocks_to_swap": swap, "swap_io_components": swap > 0,
                                                                 "offload_device": "cpu", "cache_model": False, "attention_mode": "sdpa"}},
        "vae": {"class_type": "SeedVR2LoadVAEModel", "inputs": {"model": "ema_vae_fp16.safetensors", "device": "cuda:0", "encode_tiled": True,
                                                                 "encode_tile_size": 1024, "encode_tile_overlap": 128, "decode_tiled": True,
                                                                 "decode_tile_size": 1024, "decode_tile_overlap": 128, "tile_debug": "false",
                                                                 "offload_device": "cpu", "cache_model": False}},
        "up": {"class_type": "SeedVR2VideoUpscaler", "inputs": {"image": ["load", 0], "dit": ["dit", 0], "vae": ["vae", 0], "seed": 42,
                                                                "resolution": 1440, "max_resolution": 0, "batch_size": batch, "uniform_batch_size": False,
                                                                "temporal_overlap": 4, "prepend_frames": 0, "color_correction": "lab",
                                                                "input_noise_scale": 0.0, "latent_noise_scale": 0.0, "offload_device": "cpu",
                                                                "enable_debug": debug}},
        "save": {"class_type": "SaveImage", "inputs": {"images": ["up", 0], "filename_prefix": prefix}},
    }


def rife_graph(folder, start, count, prefix):
    return {
        "load": {"class_type": "LoadImagesFromFolderKJ", "inputs": {"folder": folder, "width": -1, "height": -1, "keep_aspect_ratio": "crop",
                                                                   "image_load_cap": count, "start_index": start, "include_subfolders": False}},
        "model": {"class_type": "FrameInterpolationModelLoader", "inputs": {"model_name": "rife_v4.26.safetensors"}},
        "interp": {"class_type": "FrameInterpolate", "inputs": {"interp_model": ["model", 0], "images": ["load", 0], "multiplier": RIFE_MULT}},
        "save": {"class_type": "SaveImage", "inputs": {"images": ["interp", 0], "filename_prefix": prefix}},
    }


def copy_frames(files, dest):
    if os.path.isdir(dest):
        shutil.rmtree(dest)
    os.makedirs(dest)
    for i, f in enumerate(files):
        shutil.copy2(f, os.path.join(dest, f"f_{i:05d}.png"))


def finish(tag, model, batch, swap, cap, debug, rife):
    src = os.path.join(AI, tag)
    audio = next((os.path.join(src, f) for f in os.listdir(src) if f.startswith("audio")), None)
    print(f"{tag}:", flush=True)
    up_files = run_graph(seedvr2_graph(os.path.join(src, "frames"), f"trailer_up/{tag}/f", model, batch, swap, cap, debug), "SeedVR2 to 1440p")
    up_dir = os.path.join(AI, tag + "_up")
    copy_frames(up_files, up_dir)
    frames_dir, fps = up_dir, 24
    if rife:
        n = len(up_files)
        smooth = []
        for k, start in enumerate(range(0, n - 1, RIFE_CHUNK)):
            count = min(RIFE_CHUNK + 1, n - start)
            out = run_graph(rife_graph(up_dir, start, count, f"trailer_up/{tag}_rife/c{k:02d}/f"), f"RIFE part {k + 1}")
            smooth += out if k == 0 else out[1:]
        frames_dir, fps = os.path.join(AI, tag + "_60"), 60
        copy_frames(smooth[::2], frames_dir)
    os.makedirs(MEDIA, exist_ok=True)
    out = os.path.join(MEDIA, tag + ".mp4")
    cmd = [presmod.FFMPEG, "-y", "-v", "error", "-framerate", str(fps), "-i", os.path.join(frames_dir, "f_%05d.png")]
    if audio:
        cmd += ["-i", audio, "-c:a", "aac", "-b:a", "192k", "-shortest"]
    cmd += ["-c:v", "libx264", "-preset", "slow", "-crf", "12", "-pix_fmt", "yuv420p", "-movflags", "+faststart", out]
    subprocess.run(cmd, check=True)
    print(f"  -> {os.path.relpath(out, REPO)}", flush=True)


def main():
    argv = sys.argv[1:]
    model, batch, swap, cap, debug, rife = "seedvr2_ema_7b_fp16.safetensors", 21, 36, 0, False, True
    if "--model" in argv:
        i = argv.index("--model")
        model = argv[i + 1]
        del argv[i:i + 2]
    if "--batch" in argv:
        i = argv.index("--batch")
        batch = int(argv[i + 1])
        del argv[i:i + 2]
    if "--swap" in argv:
        i = argv.index("--swap")
        swap = int(argv[i + 1])
        del argv[i:i + 2]
    if "--cap" in argv:
        i = argv.index("--cap")
        cap = int(argv[i + 1])
        del argv[i:i + 2]
    if "--debug" in argv:
        argv.remove("--debug")
        debug = True
    if "--no-rife" in argv:
        argv.remove("--no-rife")
        rife = False
    for tag in argv:
        finish(tag, model, batch, swap, cap, debug, rife)


if __name__ == "__main__":
    main()

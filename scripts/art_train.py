"""Train a Krea 2 style LoRA on the game's own art, inside the local ComfyUI (no extra downloads).

  python scripts/art_train.py dataset [--folder sts2_style]
      Copies the Step 7 training set (52 card arts, 20 relics, fixed random seed) from the unpacked game into
      build/art/comfy_in/<folder>.
  python scripts/art_train.py caption [--folder sts2_style]
      Captions every image in build/art/comfy_in/<folder> with Qwen3-VL (the Krea 2 text encoder) and writes
      <image>.txt next to it. The trigger phrase is prepended: "sts2 card art," or "sts2 relic icon,".
  python scripts/art_train.py train [--folder sts2_style] [--steps 1500] [--rank 32] [--lr 0.0001] [--name sts2_style]
      Trains a LoRA with ComfyUI's TrainLoraNode and copies it into the shared loras folder.

Train on Krea 2 Raw (--unet krea2_raw_fp8_scaled.safetensors, a 13.1 GB download) and run on Turbo: see Step 11 in
docs/00_project_plan.md. The server must be running (see docs/07_step7_report.md).
"""
import argparse
import glob
import os
import shutil
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import art_gen  # noqa: E402

LORA_DIR = r"D:\Comfy-Desktop\ComfyUI-Shared\models\loras"
CAPTION_PROMPT = (
    "Write one caption for this image, for training an image generator. In one or two sentences, describe what is "
    "shown: the subjects, what they are doing, the objects, the main colors and the composition. Do not describe the "
    "art style or the medium, and do not start with 'The image'."
)
TRIGGER = {"card": "sts2 card art", "relic": "sts2 relic icon"}


def wait(pid):
    while True:
        h = art_gen._get("/history/" + pid)
        if pid in h:
            if h[pid].get("status", {}).get("status_str") == "error":
                raise RuntimeError(str(h[pid]["status"].get("messages"))[:3000])
            return h[pid]
        time.sleep(1)


def dataset(folder):
    """The Step 7 training set: 52 card arts (1024x768) and 20 relics (512x512 on the relic backdrop colour)."""
    import random
    from PIL import Image
    random.seed(42)
    game = os.path.join(art_gen.REPO, "re", "pck", "images")
    out = os.path.join(art_gen.COMFY_IN, folder)
    os.makedirs(out, exist_ok=True)
    for ch, n in [("ironclad", 9), ("silent", 9), ("regent", 9), ("necrobinder", 9), ("defect", 8), ("colorless", 8)]:
        files = [f for f in sorted(glob.glob(os.path.join(game, "packed", "card_portraits", ch, "*.png")))
                 if Image.open(f).size == (1000, 760) and "beta" not in f]
        for f in random.sample(files, n):
            im = Image.open(f).convert("RGB").resize((1024, 784), Image.LANCZOS).crop((0, 8, 1024, 776))
            im.save(os.path.join(out, f"card_{ch}_{os.path.basename(f)}"))
    for f in random.sample(sorted(glob.glob(os.path.join(game, "relics", "*.png"))), 20):
        im = Image.open(f).convert("RGBA").resize((448, 448), Image.LANCZOS)
        bg = Image.new("RGBA", (512, 512), (58, 50, 44, 255))
        bg.alpha_composite(im, (32, 32))
        bg.convert("RGB").save(os.path.join(out, "relic_" + os.path.basename(f)))
    print(len(glob.glob(os.path.join(out, "*.png"))), "images in", out)


def clean(text):
    """Drop the chat role the model sometimes echoes ("assistant", "Assistant:")."""
    text = text.strip()
    if text.lower().startswith("assistant"):
        text = text[len("assistant"):].lstrip(" :")
    return text


def caption(folder):
    root = os.path.join(art_gen.COMFY_IN, folder)
    for path in sorted(glob.glob(os.path.join(root, "*.png"))):
        txt = os.path.splitext(path)[0] + ".txt"
        if os.path.exists(txt):
            continue
        g = {
            "clip": {"class_type": "CLIPLoader", "inputs": {"clip_name": art_gen.CLIP, "type": "krea2", "device": "default"}},
            "img": {"class_type": "LoadImage", "inputs": {"image": folder + "/" + os.path.basename(path)}},
            "gen": {"class_type": "TextGenerate", "inputs": {
                "clip": ["clip", 0], "image": ["img", 0], "prompt": CAPTION_PROMPT, "max_length": 160,
                "sampling_mode": "off", "thinking": False, "use_default_template": True}},
            "show": {"class_type": "PreviewAny", "inputs": {"source": ["gen", 0]}},
        }
        pid = art_gen._post("/prompt", {"prompt": g})["prompt_id"]
        out = wait(pid)["outputs"]["show"]
        text = (out.get("text") or out.get("ui", {}).get("text") or [""])[0].strip().replace("\n", " ")
        text = clean(text)
        kind = "relic" if os.path.basename(path).startswith("relic_") else "card"
        text = f"{TRIGGER[kind]}, {text}"
        with open(txt, "w", encoding="utf-8") as f:
            f.write(text)
        print(os.path.basename(path), "->", text[:150], flush=True)


def train(folder, steps, rank, lr, name, unet, depth):
    g = {
        "unet": {"class_type": "UNETLoader", "inputs": {"unet_name": unet, "weight_dtype": "default"}},
        "clip": {"class_type": "CLIPLoader", "inputs": {"clip_name": art_gen.CLIP, "type": "krea2", "device": "default"}},
        "vae": {"class_type": "VAELoader", "inputs": {"vae_name": art_gen.VAE}},
        "data": {"class_type": "LoadImageTextDataSetFromFolder", "inputs": {"folder": folder}},
        "make": {"class_type": "MakeTrainingDataset", "inputs": {
            "images": ["data", 0], "texts": ["data", 1], "vae": ["vae", 0], "clip": ["clip", 0]}},
        "bucket": {"class_type": "ResolutionBucket", "inputs": {"latents": ["make", 0], "conditioning": ["make", 1]}},
        "train": {"class_type": "TrainLoraNode", "inputs": {
            "model": ["unet", 0], "latents": ["bucket", 0], "positive": ["bucket", 1],
            "batch_size": 1, "grad_accumulation_steps": 1, "steps": steps, "learning_rate": lr, "rank": rank,
            "optimizer": "AdamW", "loss_function": "MSE", "seed": 0, "training_dtype": "bf16", "lora_dtype": "bf16",
            "quantized_backward": False, "algorithm": "LoRA", "gradient_checkpointing": True, "checkpoint_depth": depth,
            "offloading": False, "existing_lora": "[None]", "bucket_mode": True, "bypass_mode": True}},
        "save": {"class_type": "SaveLoRA", "inputs": {"lora": ["train", 0], "prefix": "loras/" + name, "steps": ["train", 2]}},
    }
    t0 = time.time()
    pid = art_gen._post("/prompt", {"prompt": g})["prompt_id"]
    print("training", pid, flush=True)
    wait(pid)
    files = sorted(glob.glob(os.path.join(art_gen.COMFY_OUT, "loras", name + "*.safetensors")), key=os.path.getmtime)
    dst = os.path.join(LORA_DIR, name + ".safetensors")
    shutil.copyfile(files[-1], dst)
    print(f"done in {(time.time() - t0) / 60:.1f} min -> {dst}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("cmd", choices=["dataset", "caption", "train"])
    ap.add_argument("--folder", default="sts2_style")
    ap.add_argument("--steps", type=int, default=1500)
    ap.add_argument("--rank", type=int, default=32)
    ap.add_argument("--lr", type=float, default=0.0001)
    ap.add_argument("--name", default="sts2_style")
    ap.add_argument("--unet", default="krea2_raw_fp8_scaled.safetensors", help="train on Raw, run on Turbo")
    ap.add_argument("--depth", type=int, default=2, help="gradient checkpointing depth (1 patched only the top module and ran out of VRAM)")
    a = ap.parse_args()
    if a.cmd == "dataset":
        dataset(a.folder)
    elif a.cmd == "caption":
        caption(a.folder)
    else:
        train(a.folder, a.steps, a.rank, a.lr, a.name, a.unet, a.depth)


if __name__ == "__main__":
    sys.exit(main())

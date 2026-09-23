"""List every asset tied to one character in the recovered game project, with image sizes."""
import os, sys, csv
from PIL import Image

ROOT = r"C:\Users\exeet\sts2-trump-mod\re\pck"
char = sys.argv[1] if len(sys.argv) > 1 else "ironclad"
out = sys.argv[2] if len(sys.argv) > 2 else None
rows = []
for dp, dn, fn in os.walk(ROOT):
    rel_dir = os.path.relpath(dp, ROOT).replace("\\", "/")
    if rel_dir.startswith((".godot", "src", "localization")):
        continue
    for f in fn:
        if f.endswith((".import", ".cs", ".uid")):
            continue
        rel = f"{rel_dir}/{f}"
        if char not in rel.lower():
            continue
        size = ""
        if f.lower().endswith((".png", ".jpg", ".webp")):
            try:
                with Image.open(os.path.join(dp, f)) as im:
                    size = f"{im.width}x{im.height}"
            except Exception as e:
                size = f"err {e}"
        rows.append((rel, size, os.path.getsize(os.path.join(dp, f))))
rows.sort()
if out:
    with open(out, "w", newline="") as fh:
        w = csv.writer(fh); w.writerow(["path", "pixels", "bytes"]); w.writerows(rows)
for r in rows:
    if "card_portraits" in r[0] or "card_atlas" in r[0]:
        continue
    print(f"{r[0]:<95} {r[1]:>10}")
print("total files:", len(rows), "| card portrait files:", sum("card_portraits" in r[0] for r in rows))

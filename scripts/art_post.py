"""Turn raw generations into game-ready images.

  python scripts/art_post.py card SRC DST       card portrait, 1000x760 (the game's size)
  python scripts/art_post.py portrait SRC DST   character select button, 132x195, cut by the game's mask (+ DST_locked)
  python scripts/art_post.py relic SRC DST      relic icon, 256x256 RGBA: background keyed out, the game's
                                                translucent black outline added (12 px, alpha 128, like base relics)
"""
import os
import sys
from collections import deque

import numpy as np
from PIL import Image, ImageEnhance, ImageFilter


def fit_cover(im, w, h):
    """Scale to cover w x h, then center-crop."""
    s = max(w / im.width, h / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    x, y = (im.width - w) // 2, (im.height - h) // 2
    return im.crop((x, y, x + w, y + h))


def card(src, dst):
    fit_cover(Image.open(src).convert("RGB"), 1000, 760).save(dst)


BUTTON_MASK = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                           "re", "pck", "images", "packed", "character_select", "char_select_button_mask.png")


def portrait(src, dst):
    """The button art, cut by the game's button mask. Also writes the _locked variant (darkened) next to it."""
    im = fit_cover(Image.open(src).convert("RGB"), 132, 195).convert("RGBA")
    mask = Image.open(BUTTON_MASK).getchannel("A")
    im.putalpha(mask)
    im.save(dst)
    locked = ImageEnhance.Brightness(im.convert("LA").convert("RGBA")).enhance(0.45)
    locked.putalpha(mask)
    base, ext = os.path.splitext(dst)
    locked.save(base + "_locked" + ext)


def _flood(close, seeds):
    h, w = close.shape
    seen = np.zeros((h, w), bool)
    q = deque()
    for y, x in seeds:
        if close[y, x] and not seen[y, x]:
            seen[y, x] = True
            q.append((y, x))
    while q:
        y, x = q.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < h and 0 <= nx < w and close[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                q.append((ny, nx))
    return seen


def key_background(im, tol=28, hole_tol=14, min_hole=0.0005):
    """Alpha mask of the object: removes the background connected to the border, plus enclosed
    background-coloured holes (like the inside of a handle) bigger than `min_hole` of the image."""
    a = np.asarray(im.convert("RGB")).astype(np.int16)
    h, w, _ = a.shape
    border = np.concatenate([a[0], a[-1], a[:, 0], a[:, -1]])
    bg = np.median(border, axis=0)
    diff = np.abs(a - bg).max(axis=2)
    edge = [(y, x) for x in range(w) for y in (0, h - 1)] + [(y, x) for y in range(h) for x in (0, w - 1)]
    seen = _flood(diff <= tol, edge)
    # Enclosed holes: a stricter tolerance so light parts of the object survive.
    rest = (diff <= hole_tol) & ~seen
    ys, xs = np.nonzero(rest)
    done = np.zeros((h, w), bool)
    for y, x in zip(ys, xs):
        if done[y, x]:
            continue
        comp = _flood(rest, [(y, x)])
        done |= comp
        if comp.sum() >= min_hole * h * w:
            seen |= comp
    mask = Image.fromarray(np.where(seen, 0, 255).astype(np.uint8))
    # Close pinholes, then soften the edge by one pixel.
    return mask.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))


def despill(im):
    """Pull green fringe left by a green-screen background back to neutral."""
    a = np.asarray(im).astype(np.int16).copy()
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    limit = np.maximum(r, b)
    a[..., 1] = np.where(g > limit, limit, g)
    return Image.fromarray(a.astype(np.uint8), im.mode)


def keyed(im, green=False):
    """The object cut out of its plain background, trimmed to its bounding box (RGBA)."""
    im = im.convert("RGB")
    mask = key_background(im, tol=70 if green else 28, hole_tol=45 if green else 14)
    obj = im.convert("RGBA")
    obj.putalpha(mask)
    if green:
        obj = despill(obj)
    return obj.crop(mask.getbbox())


def _parts(mask, step=4):
    """Connected parts of a boolean mask, found on a copy scaled down by step: (area, full-size boolean mask) each."""
    small = mask[::step, ::step]
    left = small.copy()
    parts = []
    for y, x in zip(*np.nonzero(small)):
        if not left[y, x]:
            continue
        comp = _flood(left, [(y, x)])
        left &= ~comp
        full = np.kron(comp, np.ones((step, step), bool))[:mask.shape[0], :mask.shape[1]]
        parts.append((int(comp.sum()), full & mask))
    return sorted(parts, key=lambda p: -p[0])


def pose_sheet(im, count, margin=12):
    """Cut a pose sheet (count full-body figures in count equal columns on green) into count sprites of the same
    height, each anchored at its own feet, so a game that fits every pose to one height shows them all at one scale.

    Near each expected column boundary the cut goes through the emptiest column, so a fingertip or a stray speck
    bridging two figures doesn't matter; slivers of a neighbour and specks are then dropped from each column.
    Raises ValueError when two figures really overlap, or a column holds no figure or two figures."""
    im = im.convert("RGB")
    mask = key_background(im, tol=70, hole_tol=45)
    obj = im.convert("RGBA")
    obj.putalpha(mask)
    obj = despill(obj)
    alpha = np.asarray(mask) > 40
    h, w = alpha.shape
    cover = np.convolve(alpha.sum(axis=0).astype(float), np.ones(9) / 9, mode="same")
    peak = cover.max()
    cuts = []
    for i in range(1, count):
        c = i * w / count
        lo, hi = int(c - 0.4 * w / count), int(c + 0.4 * w / count)
        x = lo + int(np.argmin(cover[lo:hi]))
        if cover[x] > 0.08 * peak:
            raise ValueError(f"figures {i} and {i + 1} overlap: no clear gap between them")
        cuts.append(x)
    bounds = [0] + cuts + [w]
    a_full = np.asarray(obj.getchannel("A")).copy()
    sprites_boxes = []
    for k, (x0, x1) in enumerate(zip(bounds, bounds[1:]), 1):
        parts = _parts(alpha[:, x0:x1])
        if not parts:
            raise ValueError(f"column {k} has no figure")
        main = parts[0][0]
        if sum(1 for area, _ in parts if area >= 0.35 * main) > 1:
            raise ValueError(f"column {k} holds more than one figure")
        keep = np.zeros_like(alpha[:, x0:x1])
        for area, part in parts:
            if area >= 0.05 * main:
                keep |= part
        # Grow the kept area a little so the soft edge of the alpha survives, then clear everything else.
        keep_img = Image.fromarray(keep.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(5))
        seg_alpha = a_full[:, x0:x1] * (np.asarray(keep_img) > 0)
        ys, xs = np.nonzero(seg_alpha > 40)
        sprites_boxes.append((x0, seg_alpha, int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1))
    height = max(b[5] - b[3] for b in sprites_boxes) + 2 * margin
    sprites = []
    for x0, seg_alpha, left, top, right, bottom in sprites_boxes:
        seg = obj.crop((x0, 0, x0 + seg_alpha.shape[1], h))
        seg.putalpha(Image.fromarray(seg_alpha.astype(np.uint8)))
        cut = seg.crop((left, top, right, bottom))
        canvas = Image.new("RGBA", (cut.width + 2 * margin, height), (0, 0, 0, 0))
        canvas.alpha_composite(cut, (margin, height - margin - cut.height))
        sprites.append(canvas)
    return sprites


def keyed_fit(im, size, margin=0, green=False, anchor="center"):
    """Cut out and fit inside size (w, h) with a margin; size None keeps the trimmed cut-out as is."""
    obj = keyed(im, green)
    if size is None:
        return obj
    w, h = size
    s = min((w - 2 * margin) / obj.width, (h - 2 * margin) / obj.height)
    obj = obj.resize((max(1, round(obj.width * s)), max(1, round(obj.height * s))), Image.LANCZOS)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    y = h - margin - obj.height if anchor == "bottom" else (h - obj.height) // 2
    canvas.alpha_composite(obj, ((w - obj.width) // 2, y))
    return canvas


def icon(im, dst, size=256, outline=12, margin=6):
    """A 256x256 icon like the game's relics, potions and powers: the object keyed out and fitted, over a
    translucent black halo (the silhouette grown by `outline` px, alpha 128)."""
    canvas = keyed_fit(im, (size, size), margin=outline + margin)
    alpha = canvas.getchannel("A").point(lambda v: 255 if v > 40 else 0)
    grown = alpha.filter(ImageFilter.MaxFilter(outline * 2 + 1)).filter(ImageFilter.GaussianBlur(0.7))
    halo = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    halo.putalpha(grown.point(lambda v: v * 128 // 255))
    halo.alpha_composite(canvas)
    halo.save(dst)


def relic(src, dst):
    icon(Image.open(src), dst)


if __name__ == "__main__":
    {"card": card, "portrait": portrait, "relic": relic}[sys.argv[1]](sys.argv[2], sys.argv[3])

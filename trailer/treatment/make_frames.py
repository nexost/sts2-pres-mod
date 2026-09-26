"""Storyboard frames for the trailer treatment (T1), cropped from test screenshots and the select paintings.

The screenshots come from `test.py ui` and `test.py vfx` runs in build/test/ (git-ignored); change the folder names below
to newer runs if needed. Output: trailer/treatment/frames/*.jpg (960x540, the framing each trailer shot aims for).
Run from the repo root: python trailer/treatment/make_frames.py
"""
import os
from PIL import Image

OUT = 'trailer/treatment/frames'
T = 'build/test/ui_20260925_152636/shots/'     # The Donald, ui
B = 'build/test/ui_20260925_152438/shots/'     # Sleepy Joe, ui
V = 'build/test/vfx_20260924_230644/shots/'    # Sleepy Joe, vfx
VT = 'build/test/vfx_20260924_224427/shots/'   # The Donald, vfx
C = 'build/test/coop_20260925_084102/host/shots/'

# name: (screenshot, crop as fractions x0, y0, x1, y1). Equal width and height fractions keep 16:9.
FRAMES = {
    'f01_spire':      (T + '000_main_menu.png', (.60, .04, 1.0, .44)),
    'f02_monsters':   (VT + '000_trump_gold_gain.png', (.48, .18, 1.0, .70)),
    'f05_wall':       (T + '016_wall_70_stage4.png', (.12, .22, .62, .72)),
    'f06_rain':       (VT + '007_trump_make_it_rain_1.png', (.40, .08, .95, .63)),
    'f07_deport':     (T + '021_deport_denied_stamp.png', (.45, .30, .95, .80)),
    'f08_fired':      (VT + '017_trump_youre_fired_slam.png', (.0, .05, .95, 1.0)),
    'f09_doze':       (V + '001_biden_doze_vignette_8_of_10.png', (.00, .25, .50, .75)),
    'f10_tangent':    (V + '010_biden_tangent_bubble.png', (.05, .25, .55, .75)),
    'f11_nodoff':     (B + '013_nodded_off.png', (.00, .10, .75, .85)),
    'f13_laser':      (V + '005_biden_laser_hit1.png', (.08, .12, .93, .97)),
    'f14_motorcade':  (V + '020_biden_motorcade_1.png', (.00, .20, .60, .80)),
    'f14b_micdrop':   (V + '014_biden_mic_drop_impact.png', (.35, .20, .95, .80)),
    'f15_coop':       (C + '003_combat_turn1.png', (.00, .25, .60, .85)),
    'f16_aisle':      (V + '027_biden_reach_across_standin.png', (.00, .15, .70, .85)),
    'f17_wrecking':   (VT + '009_trump_wrecking_ball_swing.png', (.20, .10, .90, .80)),
    'f17b_fury':      (VT + '006_trump_fire_and_fury.png', (.25, .12, 1.0, .87)),
    'f17c_lasershow': (V + '008_biden_laser_show_1.png', (.00, .05, .90, .95)),
}


def save(im, name, quality=78):
    im.save(os.path.join(OUT, name + '.jpg'), quality=quality)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (src, (x0, y0, x1, y1)) in FRAMES.items():
        im = Image.open(src).convert('RGB')
        w, h = im.size
        im = im.crop((int(x0 * w), int(y0 * h), int(x1 * w), int(y1 * h))).resize((960, 540), Image.LANCZOS)
        save(im, name)

    # The character-select roster row, on a dark 16:9 plate (the ascension panel sits right above it). It stops at
    # Sleepy Joe: the "?" after him is the game's random-character button, not a character.
    im = Image.open(B + '001_char_select.png').convert('RGB')
    w, h = im.size
    row = im.crop((int(.26 * w), int(.80 * h), int(.692 * w), int(.955 * h)))
    rh = int(row.height * 960 / row.width)
    plate = Image.new('RGB', (960, 540), (14, 20, 18))
    plate.paste(row.resize((960, rh), Image.LANCZOS), (0, (540 - rh) // 2))
    save(plate, 'f03_candidates', 80)

    # The select paintings (2560x1200)
    tp = Image.open('mod/images/trump/char_select_bg.png').convert('RGB')
    bp = Image.open('mod/images/biden/char_select_bg.png').convert('RGB')
    save(tp.crop((768, 96, 2560, 1104)).resize((960, 540), Image.LANCZOS), 'f04_donald_painting', 80)
    save(bp.crop((1480, 0, 2380, 506)).resize((960, 540), Image.LANCZOS), 'f12_joe_face', 80)
    save(bp.crop((768, 96, 2560, 1104)).resize((960, 540), Image.LANCZOS), 'f18_joe_painting', 80)
    save(tp.crop((1400, 0, 2560, 1200)).resize((580, 600), Image.LANCZOS), 'key_donald', 80)
    save(bp.crop((1300, 0, 2460, 1200)).resize((580, 600), Image.LANCZOS), 'key_joe', 80)


if __name__ == '__main__':
    main()

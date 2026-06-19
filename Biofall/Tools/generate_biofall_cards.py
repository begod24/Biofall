from __future__ import annotations

import math
import os
import random
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT_OPERATORS = ROOT / "Assets/Art/UI/GeneratedCards/Operators"
OUT_MISSIONS = ROOT / "Assets/Art/UI/GeneratedCards/Missions"
OUT_PREVIEW = ROOT / "Assets/Art/UI/GeneratedCards/Preview"


BG = (6, 0, 7)
PANEL = (56, 4, 7)
PANEL_2 = (104, 13, 22)
RED = (142, 24, 35)
RED_GLOW = (188, 30, 45)
TEXT = (224, 215, 211)
MUTED = (121, 93, 95)
CYAN = (86, 160, 170)
AMBER = (205, 138, 54)
BLACK = (4, 5, 6)
ARMOR = (28, 33, 34)
FABRIC = (15, 22, 23)
OLIVE = (47, 58, 44)


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Helvetica Bold.ttf" if bold else "/System/Library/Fonts/Supplemental/Helvetica.ttf",
        "/Library/Fonts/Arial Bold.ttf" if bold else "/Library/Fonts/Arial.ttf",
    ]
    for p in candidates:
        if p and os.path.exists(p):
            return ImageFont.truetype(p, size=size)
    return ImageFont.load_default()


FONT_BIG = font(52, True)
FONT_MED = font(34, True)
FONT_SMALL = font(22, True)
FONT_TINY = font(16, False)


def rounded(draw: ImageDraw.ImageDraw, box, radius, fill, outline=None, width=1):
    draw.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def vertical_gradient(size, top, bottom):
    w, h = size
    img = Image.new("RGB", size, top)
    px = img.load()
    for y in range(h):
        t = y / max(1, h - 1)
        col = tuple(int(top[i] * (1 - t) + bottom[i] * t) for i in range(3))
        for x in range(w):
            px[x, y] = col
    return img.convert("RGBA")


def add_noise(img: Image.Image, amount=16, alpha=35):
    rng = random.Random(17)
    noise = Image.new("RGBA", img.size, (0, 0, 0, 0))
    px = noise.load()
    for y in range(img.height):
        for x in range(img.width):
            v = rng.randint(-amount, amount)
            if v >= 0:
                px[x, y] = (v, v, v, alpha)
            else:
                px[x, y] = (0, 0, 0, alpha)
    return Image.alpha_composite(img, noise)


def add_scanlines(img: Image.Image, step=4, alpha=28):
    overlay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)
    for y in range(0, img.height, step):
        d.line((0, y, img.width, y), fill=(0, 0, 0, alpha), width=1)
    return Image.alpha_composite(img, overlay)


def text_center(draw, xy, value, fnt, fill=TEXT, anchor="mm"):
    draw.text(xy, value, font=fnt, fill=fill, anchor=anchor)


def glow_line(base, points, fill, width=6, glow=18):
    glow_img = Image.new("RGBA", base.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow_img)
    gd.line(points, fill=fill[:3] + (90,), width=width + glow, joint="curve")
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(glow / 2))
    base.alpha_composite(glow_img)
    d = ImageDraw.Draw(base)
    d.line(points, fill=fill, width=width, joint="curve")


def card_base(w, h, title=None, subtitle=None):
    img = vertical_gradient((w, h), (9, 0, 10), (2, 0, 4))
    img = add_noise(img, 10, 18)
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, w, h), outline=(30, 6, 10), width=8)
    d.rectangle((22, 22, w - 22, h - 22), outline=(82, 13, 20), width=2)
    d.rectangle((30, 30, w - 30, h - 30), outline=(32, 8, 12), width=1)
    for y in (140, h - 170):
        d.rectangle((34, y, w - 34, y + 2), fill=(92, 13, 20))
    if title:
        text_center(d, (w // 2, 72), title, FONT_MED)
    if subtitle:
        text_center(d, (w // 2, 108), subtitle, FONT_TINY, fill=MUTED)
    return add_scanlines(img)


@dataclass
class OperatorSpec:
    filename: str
    title: str
    role: str
    armor_scale: float
    weapon: str
    accent: tuple[int, int, int]
    mask: str
    stance: str


def draw_operator(spec: OperatorSpec):
    w, h = 768, 1024
    img = card_base(w, h, spec.title, spec.role)
    d = ImageDraw.Draw(img)

    # Atmospheric backlight.
    halo = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    hd.ellipse((170, 190, 598, 860), fill=RED_GLOW + (32,))
    halo = halo.filter(ImageFilter.GaussianBlur(80))
    img.alpha_composite(halo)

    cx = w // 2
    ground_y = 884
    scale = spec.armor_scale

    # Shadow and floor.
    d.ellipse((cx - 190, ground_y - 24, cx + 190, ground_y + 30), fill=(0, 0, 0, 135))
    for i in range(8):
        y = ground_y + i * 5
        d.line((80, y, w - 80, y), fill=(70, 10, 14, max(16, 50 - i * 5)), width=1)

    # Legs.
    leg_w = int(44 * scale)
    d.rounded_rectangle((cx - 80, 610, cx - 80 + leg_w, ground_y), radius=20, fill=FABRIC, outline=(48, 55, 55), width=2)
    d.rounded_rectangle((cx + 34, 610, cx + 34 + leg_w, ground_y), radius=20, fill=FABRIC, outline=(48, 55, 55), width=2)
    d.rounded_rectangle((cx - 104, ground_y - 28, cx - 22, ground_y + 16), radius=14, fill=(9, 10, 10))
    d.rounded_rectangle((cx + 18, ground_y - 28, cx + 106, ground_y + 16), radius=14, fill=(9, 10, 10))
    d.rectangle((cx - 88, 720, cx - 32, 754), fill=(35, 41, 41))
    d.rectangle((cx + 38, 720, cx + 94, 754), fill=(35, 41, 41))

    # Torso and armor.
    shoulder_w = int(190 * scale)
    torso = (cx - shoulder_w // 2, 325, cx + shoulder_w // 2, 650)
    d.rounded_rectangle(torso, radius=48, fill=OLIVE if spec.title == "CBRN MEDIC" else FABRIC, outline=(58, 63, 61), width=2)
    vest = (torso[0] + 26, 372, torso[2] - 26, 608)
    d.rounded_rectangle(vest, radius=24, fill=ARMOR, outline=(70, 76, 75), width=2)
    d.rectangle((vest[0] + 18, 410, vest[2] - 18, 445), fill=(46, 51, 51))
    d.rectangle((vest[0] + 18, 468, vest[2] - 18, 506), fill=(38, 43, 43))
    d.rectangle((vest[0] + 24, 526, vest[0] + 74, 594), fill=(35, 39, 39))
    d.rectangle((vest[2] - 74, 526, vest[2] - 24, 594), fill=(35, 39, 39))
    d.line((cx, 380, cx, 608), fill=(14, 16, 16), width=4)
    d.rectangle((cx - 12, 350, cx + 12, 388), fill=spec.accent)

    # Arms.
    arm_fill = (17, 24, 24)
    left_arm = [(torso[0] + 20, 370), (torso[0] - 45, 475), (torso[0] - 18, 650), (torso[0] + 45, 620), (torso[0] + 54, 420)]
    right_arm = [(torso[2] - 20, 370), (torso[2] + 48, 485), (torso[2] + 20, 650), (torso[2] - 43, 620), (torso[2] - 54, 420)]
    d.polygon(left_arm, fill=arm_fill, outline=(49, 55, 55))
    d.polygon(right_arm, fill=arm_fill, outline=(49, 55, 55))
    d.ellipse((torso[0] - 56, 636, torso[0] + 6, 696), fill=(8, 9, 9))
    d.ellipse((torso[2] - 6, 636, torso[2] + 58, 696), fill=(8, 9, 9))

    # Head, helmet, mask.
    d.ellipse((cx - 58, 238, cx + 58, 350), fill=(38, 42, 41), outline=(80, 83, 80), width=2)
    d.pieslice((cx - 76, 220, cx + 76, 324), 180, 360, fill=(18, 22, 22), outline=(71, 76, 76), width=2)
    d.rectangle((cx - 66, 284, cx + 66, 316), fill=(18, 22, 22))
    if spec.mask == "full":
        d.rounded_rectangle((cx - 42, 292, cx + 42, 352), radius=18, fill=(9, 11, 11), outline=(69, 76, 74), width=2)
        d.ellipse((cx - 36, 308, cx - 8, 334), fill=(14, 24, 25), outline=CYAN, width=1)
        d.ellipse((cx + 8, 308, cx + 36, 334), fill=(14, 24, 25), outline=CYAN, width=1)
    else:
        d.rounded_rectangle((cx - 36, 304, cx + 36, 346), radius=16, fill=(10, 12, 12), outline=(65, 71, 70), width=2)
        d.rectangle((cx - 44, 280, cx + 44, 302), fill=(22, 25, 25))

    # Role gear.
    if spec.title == "HEAVY":
        d.rounded_rectangle((torso[0] - 24, 392, torso[0] + 30, 574), radius=14, fill=(39, 43, 42), outline=(88, 86, 75), width=2)
        d.rounded_rectangle((torso[2] - 30, 392, torso[2] + 24, 574), radius=14, fill=(39, 43, 42), outline=(88, 86, 75), width=2)
    if spec.title == "CBRN MEDIC":
        d.rounded_rectangle((vest[0] + 18, 516, vest[0] + 86, 598), radius=12, fill=(52, 38, 38), outline=RED, width=2)
        d.line((vest[0] + 52, 532, vest[0] + 52, 582), fill=TEXT, width=4)
        d.line((vest[0] + 30, 557, vest[0] + 74, 557), fill=TEXT, width=4)
        d.rounded_rectangle((torso[2] + 16, 500, torso[2] + 56, 608), radius=14, fill=(29, 42, 40), outline=CYAN, width=2)
    if spec.title == "BREACHER":
        d.rectangle((vest[2] - 40, 398, vest[2] - 20, 590), fill=(75, 75, 68))
        d.rectangle((vest[2] - 53, 410, vest[2] - 7, 438), fill=(54, 54, 49))

    # Weapon.
    if spec.weapon == "rifle":
        pts = [(cx - 205, 570), (cx + 195, 500)]
        glow_line(img, pts, (15, 18, 18, 255), width=18, glow=3)
        d.rectangle((cx - 20, 523, cx + 70, 552), fill=(12, 14, 14), outline=(61, 64, 62))
        d.rectangle((cx + 55, 512, cx + 185, 528), fill=(12, 14, 14))
        d.rectangle((cx - 122, 556, cx - 30, 575), fill=(12, 14, 14))
    elif spec.weapon == "heavy":
        d.rectangle((cx - 230, 546, cx + 230, 584), fill=(12, 14, 14), outline=(70, 72, 70))
        d.rectangle((cx - 80, 502, cx + 70, 550), fill=(12, 14, 14), outline=(70, 72, 70))
        d.rectangle((cx + 170, 532, cx + 250, 548), fill=(12, 14, 14))
    elif spec.weapon == "shotgun":
        d.rectangle((cx - 210, 548, cx + 215, 570), fill=(12, 14, 14), outline=(70, 72, 70))
        d.rectangle((cx - 50, 518, cx + 48, 550), fill=(12, 14, 14), outline=(70, 72, 70))

    # Bottom name plate.
    rounded(d, (54, 888, w - 54, 974), 6, (48, 4, 7, 205), outline=(118, 19, 30), width=2)
    text_center(d, (w // 2, 920), spec.title, FONT_MED)
    text_center(d, (w // 2, 952), spec.role, FONT_TINY, fill=MUTED)

    img.save(OUT_OPERATORS / spec.filename)


@dataclass
class MissionSpec:
    filename: str
    title: str
    subtitle: str
    kind: str
    accent: tuple[int, int, int]


def draw_buildings(d, horizon, color):
    rng = random.Random(8)
    x = 0
    while x < 1024:
        bw = rng.randint(48, 110)
        bh = rng.randint(70, 180)
        d.rectangle((x, horizon - bh, x + bw, horizon), fill=color)
        for wx in range(x + 10, x + bw - 10, 22):
            for wy in range(horizon - bh + 18, horizon - 8, 26):
                if rng.random() < 0.22:
                    d.rectangle((wx, wy, wx + 8, wy + 12), fill=(70, 18, 22))
        x += bw + rng.randint(3, 12)


def draw_mission(spec: MissionSpec):
    w, h = 1024, 576
    img = vertical_gradient((w, h), (8, 0, 9), (1, 0, 3))
    d = ImageDraw.Draw(img)
    # Background atmosphere.
    fog = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    fd = ImageDraw.Draw(fog)
    fd.rectangle((0, 220, w, h), fill=(45, 8, 14, 48))
    fd.ellipse((280, 110, 760, 460), fill=spec.accent + (42,))
    fog = fog.filter(ImageFilter.GaussianBlur(70))
    img.alpha_composite(fog)
    d = ImageDraw.Draw(img)

    draw_buildings(d, 260, (10, 10, 14))
    d.rectangle((0, 260, w, h), fill=(13, 13, 14))
    for i in range(16):
        y = 300 + i * 18
        d.line((0, y, w, y + 40), fill=(37, 11, 15, 70), width=2)

    if spec.kind == "beacon":
        d.rectangle((440, 190, 584, 392), fill=(34, 39, 41), outline=(104, 19, 28), width=3)
        d.rectangle((475, 112, 549, 210), fill=(20, 25, 28), outline=CYAN, width=2)
        d.ellipse((462, 96, 562, 148), fill=(26, 42, 45), outline=CYAN, width=3)
        glow_line(img, [(512, 96), (512, 22)], CYAN + (210,), width=6, glow=40)
        for x in (330, 690):
            d.rectangle((x, 342, x + 150, 382), fill=(53, 17, 20), outline=(104, 23, 30), width=2)
    elif spec.kind == "facility":
        d.rectangle((250, 180, 780, 390), fill=(25, 27, 29), outline=(92, 20, 29), width=3)
        d.rectangle((470, 260, 560, 390), fill=(7, 8, 9), outline=(62, 70, 70), width=2)
        for x in range(290, 735, 70):
            d.rectangle((x, 212, x + 34, 248), fill=(29, 47, 49), outline=CYAN, width=1)
        glow_line(img, [(120, 430), (260, 370), (430, 350), (720, 376), (930, 452)], RED_GLOW + (170,), width=10, glow=35)
    elif spec.kind == "grid":
        for x in (290, 520, 750):
            d.line((x, 160, x, 404), fill=(58, 63, 62), width=8)
            d.line((x - 60, 205, x + 60, 205), fill=(58, 63, 62), width=6)
            d.line((x - 45, 245, x + 45, 245), fill=(58, 63, 62), width=5)
        glow_line(img, [(100, 440), (240, 380), (430, 410), (620, 355), (930, 395)], AMBER + (190,), width=8, glow=26)
        d.rectangle((420, 328, 605, 430), fill=(27, 28, 28), outline=AMBER, width=2)
    elif spec.kind == "extraction":
        d.rectangle((230, 150, 794, 392), fill=(19, 20, 23), outline=(77, 18, 25), width=3)
        d.rectangle((400, 210, 624, 392), fill=(6, 7, 8), outline=(87, 94, 93), width=2)
        glow_line(img, [(512, 208), (512, 48)], CYAN + (190,), width=9, glow=55)
        for x in range(210, 815, 70):
            d.rectangle((x, 405, x + 44, 442), fill=(81, 19, 24), outline=(130, 28, 35), width=1)

    # Bio-growth shapes.
    rng = random.Random(hash(spec.filename) & 0xFFFF)
    for _ in range(17):
        x = rng.randint(0, w)
        y = rng.randint(290, h)
        r = rng.randint(20, 70)
        d.ellipse((x - r, y - r // 2, x + r, y + r // 2), fill=(70, 5, 12, 115))
        d.ellipse((x - r // 2, y - r // 4, x + r // 2, y + r // 4), fill=(114, 17, 28, 95))

    # Frame and title plate.
    d.rectangle((0, 0, w, h), outline=(29, 5, 9), width=8)
    d.rectangle((24, 24, w - 24, h - 24), outline=(96, 16, 25), width=2)
    rounded(d, (42, 392, 982, 532), 8, (30, 1, 5, 225), outline=(111, 18, 28), width=2)
    text_center(d, (512, 434), spec.title, FONT_BIG)
    text_center(d, (512, 480), spec.subtitle, FONT_SMALL, fill=MUTED)

    img = add_noise(img, 8, 14)
    img = add_scanlines(img, 4, 25)
    img.save(OUT_MISSIONS / spec.filename)


def make_preview():
    ops = [Image.open(OUT_OPERATORS / f).resize((192, 256)) for f in [
        "OP_TeamLead.png", "OP_Heavy.png", "OP_CBRNMedic.png", "OP_Breacher.png"
    ]]
    missions = [Image.open(OUT_MISSIONS / f).resize((256, 144)) for f in [
        "MS_ProtocolZero.png", "MS_TheFacility.png", "MS_PowerGrid.png", "MS_ExtractionCorridor.png"
    ]]
    w, h = 1080, 760
    img = card_base(w, h, "BIOFALL GENERATED CARDS", "OPERATORS + MISSIONS")
    d = ImageDraw.Draw(img)
    x = 96
    for op in ops:
        img.alpha_composite(op.convert("RGBA"), (x, 150))
        x += 222
    x = 58
    for ms in missions:
        img.alpha_composite(ms.convert("RGBA"), (x, 480))
        x += 256
    img.save(OUT_PREVIEW / "Cards_Preview.png")


def main():
    OUT_OPERATORS.mkdir(parents=True, exist_ok=True)
    OUT_MISSIONS.mkdir(parents=True, exist_ok=True)
    OUT_PREVIEW.mkdir(parents=True, exist_ok=True)

    operators = [
        OperatorSpec("OP_TeamLead.png", "TEAM LEAD", "ASSAULT / COMMAND", 1.0, "rifle", CYAN, "half", "lead"),
        OperatorSpec("OP_Heavy.png", "HEAVY", "SUPPRESSION", 1.18, "heavy", AMBER, "full", "heavy"),
        OperatorSpec("OP_CBRNMedic.png", "CBRN MEDIC", "SUPPORT / SAMPLE", 0.98, "rifle", (180, 42, 54), "full", "medic"),
        OperatorSpec("OP_Breacher.png", "BREACHER", "ENTRY / SCOUT", 0.92, "shotgun", (130, 130, 118), "half", "breacher"),
    ]
    for spec in operators:
        draw_operator(spec)

    missions = [
        MissionSpec("MS_ProtocolZero.png", "PROTOCOL ZERO", "QUARANTINE BEACON", "beacon", CYAN),
        MissionSpec("MS_TheFacility.png", "THE FACILITY", "SIGNAL SOURCE", "facility", RED_GLOW),
        MissionSpec("MS_PowerGrid.png", "POWER GRID", "SECTOR REACTIVATION", "grid", AMBER),
        MissionSpec("MS_ExtractionCorridor.png", "EXTRACTION", "CORRIDOR K-17", "extraction", CYAN),
    ]
    for spec in missions:
        draw_mission(spec)

    make_preview()


if __name__ == "__main__":
    main()

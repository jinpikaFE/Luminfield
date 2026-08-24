#!/usr/bin/env python3
"""Render the exact 256x192 world topology as an image-generation reference."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SCALE = 4
WIDTH = 256
HEIGHT = 192
OUTPUT = Path("artifacts/design/world_master_topology_reference.png")


COLORS = {
    "home": "#273b52",
    "woods": "#174b4f",
    "meadow": "#3f6759",
    "city": "#4c466b",
    "crystal": "#244f68",
    "wetlands": "#15536b",
    "ruins": "#3c3b59",
    "road": "#d0a75f",
    "water": "#16728b",
    "core": "#6a526e",
    "civic": "#665a87",
    "facility": "#57496d",
    "anchor": "#7bf1d2",
    "border": "#101829",
}


def px(value: int) -> int:
    return value * SCALE


def rect(draw: ImageDraw.ImageDraw, bounds: tuple[int, int, int, int], color: str) -> None:
    x0, y0, x1, y1 = bounds
    draw.rectangle((px(x0), px(y0), px(x1 + 1) - 1, px(y1 + 1) - 1), fill=color)


def biome_at(x: int, y: int) -> str:
    if x < 64 and y < 64:
        return "home"
    if 64 <= x < 192 and 32 <= y < 128:
        return "city"
    if x < 64 and y >= 64:
        return "woods"
    if 64 <= x < 192 and y < 32:
        return "meadow"
    if x >= 192 and y < 96:
        return "wetlands"
    if (x >= 192 and y >= 96) or (x >= 128 and y >= 128):
        return "ruins"
    return "crystal"


def is_path(x: int, y: int) -> bool:
    return any(
        (
            17 <= x <= 21 and 29 <= y <= 48,
            44 <= y <= 51 and 18 <= x <= 72,
            74 <= y <= 85 and 18 <= x <= 238,
            122 <= x <= 133 and 16 <= y <= 176,
            18 <= y <= 25 and 64 <= x <= 190,
            138 <= y <= 145 and 36 <= x <= 238,
            80 <= x <= 87 and 128 <= y <= 158,
            138 <= y <= 145 and 70 <= x <= 85,
            222 <= x <= 229 and 36 <= y <= 112,
            76 <= y <= 83 and 190 <= x <= 227,
            44 <= y <= 51 and 224 <= x <= 232,
            76 <= y <= 83 and 19 <= x <= 63,
            38 <= x <= 45 and 64 <= y <= 176,
            154 <= y <= 161 and 126 <= x <= 238,
        )
    )


def is_water(x: int, y: int) -> bool:
    if is_path(x, y):
        return False
    biome = biome_at(x, y)
    if biome == "wetlands":
        islet_dx = (x - 214) / 7
        islet_dy = (y - 54) / 5
        if islet_dx * islet_dx + islet_dy * islet_dy <= 1:
            return False
        dx = (x - 224) / 27
        dy = (y - 45) / 22
        if dx * dx + dy * dy < 1:
            return True
        return y > 48 and ((x * 374761393 + y * 668265263) & 0xFFFFFFFF) % 11 < 3
    if biome == "crystal":
        import math

        stream_x = 83 + round(math.sin(y * 0.16) * 4)
        return abs(x - stream_x) <= 2
    return False


def main() -> None:
    image = Image.new("RGB", (px(WIDTH), px(HEIGHT)), COLORS["border"])
    draw = ImageDraw.Draw(image)

    for y in range(HEIGHT):
        for x in range(WIDTH):
            color = COLORS[biome_at(x, y)]
            if is_water(x, y):
                color = COLORS["water"]
            if is_path(x, y):
                color = COLORS["road"]
            draw.rectangle((px(x), px(y), px(x + 1) - 1, px(y + 1) - 1), fill=color)

    rect(draw, (0, 0, 47, 31), COLORS["core"])
    rect(draw, (108, 64, 150, 92), COLORS["civic"])
    rect(draw, (106, 104, 190, 127), COLORS["facility"])

    for x in range(0, WIDTH + 1, 32):
        draw.line((px(x), 0, px(x), px(HEIGHT)), fill="#ffffff22", width=1)
    for y in range(0, HEIGHT + 1, 32):
        draw.line((0, px(y), px(WIDTH), px(y)), fill="#ffffff22", width=1)

    anchors = {
        "FARM": (24, 16),
        "BEGINNER ARCH": (52, 49),
        "MEADOW CIRCLE": (136, 14),
        "WOODS GROVE": (52, 112),
        "CITY CIVIC": (128, 80),
        "CITY PAVILION": (148, 54),
        "FACILITIES": (148, 116),
        "WETLAND ISLET": (214, 56),
        "CRYSTAL RIDGE": (92, 174),
        "RUINS": (184, 178),
    }
    font = ImageFont.load_default()
    for label, (x, y) in anchors.items():
        radius = 3 * SCALE
        center = (px(x) + SCALE // 2, px(y) + SCALE // 2)
        draw.ellipse(
            (
                center[0] - radius,
                center[1] - radius,
                center[0] + radius,
                center[1] + radius,
            ),
            outline=COLORS["anchor"],
            width=2,
        )
        draw.text((center[0] + radius + 2, center[1] - 5), label, fill="#f7f2d0", font=font)

    draw.rectangle((0, 0, px(WIDTH) - 1, px(HEIGHT) - 1), outline=COLORS["border"], width=8)
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    image.save(OUTPUT)
    print(OUTPUT.resolve())


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Blend high-density world region masters into their shared composition images."""

from pathlib import Path

from PIL import Image


ROOT = Path("assets/generated/world/overworld")


def smoothstep(value: float) -> float:
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def feather(
    source: Path,
    target: Path,
    feather_pixels: int,
    edges: tuple[bool, bool, bool, bool],
) -> None:
    image = Image.open(source).convert("RGBA")
    width, height = image.size
    alpha = Image.new("L", image.size, 255)
    pixels = alpha.load()
    left, top, right, bottom = edges

    for y in range(height):
        vertical = 1.0
        if top:
            vertical = min(vertical, y / feather_pixels)
        if bottom:
            vertical = min(vertical, (height - 1 - y) / feather_pixels)
        vertical = smoothstep(vertical)
        for x in range(width):
            horizontal = 1.0
            if left:
                horizontal = min(horizontal, x / feather_pixels)
            if right:
                horizontal = min(horizontal, (width - 1 - x) / feather_pixels)
            pixels[x, y] = round(255 * min(vertical, smoothstep(horizontal)))

    image.putalpha(alpha)
    image.save(target, optimize=True)


def main() -> None:
    for source in sorted(ROOT.glob("sector_*_gleamrise_source.png")):
        target = source.with_name(source.name.replace("_source", ""))
        feather(source, target, 80, (True, True, True, True))

    for source in sorted(ROOT.glob("city_master_*_source.png")):
        target = source.with_name(source.name.replace("_source", ""))
        feather(source, target, 72, (True, True, True, True))

    for source in sorted(ROOT.glob("beginner_master_*_source.png")):
        target = source.with_name(source.name.replace("_source", ""))
        feather(source, target, 80, (False, False, True, True))


if __name__ == "__main__":
    main()

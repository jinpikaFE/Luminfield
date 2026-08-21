#!/usr/bin/env python3
"""Normalize a generated color-key atlas into stable RGB/RGBA assets."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("chroma_output", type=Path)
    parser.add_argument("runtime_output", type=Path)
    parser.add_argument("--columns", type=int, required=True)
    parser.add_argument("--rows", type=int, required=True)
    parser.add_argument("--width", type=int, required=True)
    parser.add_argument("--height", type=int, required=True)
    parser.add_argument("--max-cell-width", type=int, required=True)
    parser.add_argument("--max-cell-height", type=int, required=True)
    parser.add_argument("--baseline-inset", type=int, default=37)
    return parser.parse_args()


def is_chroma(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, _ = pixel
    return (
        red >= 145
        and blue >= 115
        and green <= 110
        and red - green >= 70
        and blue - green >= 40
    )


def is_strong_chroma(pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, _ = pixel
    return red >= 220 and blue >= 205 and green <= 55


def extract_subject(cell: Image.Image) -> Image.Image:
    rgba = cell.convert("RGBA")
    pixels = rgba.load()
    removed: set[tuple[int, int]] = set()
    pending: deque[tuple[int, int]] = deque()

    def enqueue(x: int, y: int) -> None:
        point = (x, y)
        if point in removed or not is_chroma(pixels[x, y]):
            return
        removed.add(point)
        pending.append(point)

    for x in range(rgba.width):
        enqueue(x, 0)
        enqueue(x, rgba.height - 1)
    for y in range(rgba.height):
        enqueue(0, y)
        enqueue(rgba.width - 1, y)

    while pending:
        x, y = pending.popleft()
        if x > 0:
            enqueue(x - 1, y)
        if x + 1 < rgba.width:
            enqueue(x + 1, y)
        if y > 0:
            enqueue(x, y - 1)
        if y + 1 < rgba.height:
            enqueue(x, y + 1)

    for y in range(rgba.height):
        for x in range(rgba.width):
            red, green, blue, alpha = pixels[x, y]
            pixels[x, y] = (
                red,
                green,
                blue,
                0
                if alpha == 0
                or (x, y) in removed
                or is_strong_chroma((red, green, blue, alpha))
                else 255,
            )

    bounds = rgba.getbbox()
    if bounds is None:
        raise ValueError("Generated atlas cell does not contain a subject")
    return rgba.crop(bounds)


def main() -> None:
    args = parse_args()
    source = Image.open(args.input).convert("RGBA")
    if args.columns <= 0 or args.rows <= 0:
        raise ValueError("Atlas rows and columns must be positive")
    if args.width % args.columns != 0 or args.height % args.rows != 0:
        raise ValueError("Output dimensions must divide evenly into the atlas grid")

    output_cell_width = args.width // args.columns
    output_cell_height = args.height // args.rows
    runtime = Image.new("RGBA", (args.width, args.height), (0, 0, 0, 0))

    for row in range(args.rows):
        for column in range(args.columns):
            source_box = (
                (column * source.width + args.columns // 2) // args.columns,
                (row * source.height + args.rows // 2) // args.rows,
                ((column + 1) * source.width + args.columns // 2)
                // args.columns,
                ((row + 1) * source.height + args.rows // 2) // args.rows,
            )
            subject = extract_subject(source.crop(source_box))
            scale = min(
                args.max_cell_width / subject.width,
                args.max_cell_height / subject.height,
            )
            width = max(1, round(subject.width * scale))
            height = max(1, round(subject.height * scale))
            subject = subject.resize((width, height), Image.Resampling.NEAREST)
            x = column * output_cell_width + (output_cell_width - width) // 2
            baseline = (row + 1) * output_cell_height - args.baseline_inset
            y = baseline - height
            runtime.alpha_composite(subject, (x, y))

    chroma = Image.new("RGB", runtime.size, (255, 0, 255))
    chroma.paste(runtime.convert("RGB"), mask=runtime.getchannel("A"))
    args.chroma_output.parent.mkdir(parents=True, exist_ok=True)
    chroma.save(args.chroma_output)
    runtime.save(args.runtime_output)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Compose generated region masters into continuous seasonal world backdrops."""

from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path("assets/generated/world/overworld")
WORLD_SIZE = (4096, 3072)
SECTOR_SIZE = 1024
SECTOR_BLEED = 48
CITY_RECT = (1024, 512, 2048, 1536)
CITY_BLEED = 64
CITY_SIZE = (2048, 1536)
CITY_PATCH_SIZE = (1152, 896)
CITY_PATCH_OVERLAP = 256
CITY_CENTER_POSITION = (600, 360)
CITY_CENTER_SIZE = (850, 850)
CITY_CENTER_FEATHER = 96
CITY_QUADRANTS = {
    "nw": ((0, 0), (False, False, True, True)),
    "ne": ((896, 0), (True, False, False, True)),
    "sw": ((0, 640), (False, True, True, False)),
    "se": ((896, 640), (True, True, False, False)),
}
SEASONS = ("gleamrise", "rainveil", "starharvest", "longnight")
SECTOR_COORDINATES = (
    (1, 0),
    (2, 0),
    (0, 1),
    (1, 1),
    (2, 1),
    (3, 0),
    (3, 1),
    (0, 2),
    (1, 2),
    (2, 2),
    (3, 2),
)
SEASON_TINTS = {
    "gleamrise": (1.0, 1.0, 1.0),
    "rainveil": (0.84, 0.94, 1.0),
    "starharvest": (1.0, 0.82, 0.68),
    "longnight": (0.74, 0.82, 1.0),
}


def edge_weights(
    width: int,
    height: int,
    bleed: int,
    edges: tuple[bool, bool, bool, bool],
) -> np.ndarray:
    horizontal = np.ones(width, dtype=np.float32)
    vertical = np.ones(height, dtype=np.float32)
    ramp = np.sin(np.linspace(0, np.pi / 2, bleed, dtype=np.float32)) ** 2
    left, top, right, bottom = edges
    if left:
        horizontal[:bleed] = ramp
    if right:
        horizontal[-bleed:] = ramp[::-1]
    if top:
        vertical[:bleed] = ramp
    if bottom:
        vertical[-bleed:] = ramp[::-1]
    return vertical[:, None] * horizontal[None, :]


def tint(image: Image.Image, season: str) -> Image.Image:
    factors = np.asarray(SEASON_TINTS[season], dtype=np.float32)
    pixels = np.asarray(image.convert("RGB"), dtype=np.float32)
    pixels = np.clip(pixels * factors, 0, 255).astype(np.uint8)
    return Image.fromarray(pixels, "RGB")


def add_layer(
    colors: np.ndarray,
    weights: np.ndarray,
    image: Image.Image,
    position: tuple[int, int],
    mask: np.ndarray,
    strength: float = 1.0,
) -> None:
    x, y = position
    pixels = np.asarray(image.convert("RGB"), dtype=np.float32)
    height, width = pixels.shape[:2]
    layer_weight = mask * strength
    target_x = max(0, x)
    target_y = max(0, y)
    target_right = min(WORLD_SIZE[0], x + width)
    target_bottom = min(WORLD_SIZE[1], y + height)
    source_x = target_x - x
    source_y = target_y - y
    source_right = source_x + target_right - target_x
    source_bottom = source_y + target_bottom - target_y
    visible_pixels = pixels[
        source_y:source_bottom,
        source_x:source_right,
    ]
    visible_weight = layer_weight[
        source_y:source_bottom,
        source_x:source_right,
    ]
    colors[target_y:target_bottom, target_x:target_right] += (
        visible_pixels * visible_weight[:, :, None]
    )
    weights[target_y:target_bottom, target_x:target_right] += visible_weight


def expanded_region(
    image: Image.Image,
    width: int,
    height: int,
    bleed: int,
) -> Image.Image:
    return image.resize(
        (width + bleed * 2, height + bleed * 2),
        Image.Resampling.LANCZOS,
    )


def padded_region(image: Image.Image, bleed: int) -> Image.Image:
    pixels = np.asarray(image.convert("RGB"), dtype=np.uint8)
    padded = np.pad(
        pixels,
        ((bleed, bleed), (bleed, bleed), (0, 0)),
        mode="edge",
    )
    return Image.fromarray(padded, "RGB")


def compose_city_master(season: str) -> None:
    colors = np.zeros((CITY_SIZE[1], CITY_SIZE[0], 3), dtype=np.float32)
    weights = np.zeros((CITY_SIZE[1], CITY_SIZE[0]), dtype=np.float32)

    for quadrant, (position, edges) in CITY_QUADRANTS.items():
        panel = Image.open(
            ROOT / f"city_quadrant_{quadrant}_gleamrise_source.png"
        ).convert("RGB")
        panel = tint(panel, season).resize(
            CITY_PATCH_SIZE,
            Image.Resampling.LANCZOS,
        )
        mask = edge_weights(
            panel.width,
            panel.height,
            CITY_PATCH_OVERLAP,
            edges,
        )
        x, y = position
        pixels = np.asarray(panel, dtype=np.float32)
        colors[y:y + panel.height, x:x + panel.width] += (
            pixels * mask[:, :, None]
        )
        weights[y:y + panel.height, x:x + panel.width] += mask

    city = np.clip(colors / weights[:, :, None], 0, 255).astype(np.uint8)
    center = Image.open(
        ROOT / "city_center_gleamrise_source.png"
    ).convert("RGB")
    center = tint(center, season).resize(
        CITY_CENTER_SIZE,
        Image.Resampling.LANCZOS,
    )
    center_mask = edge_weights(
        center.width,
        center.height,
        CITY_CENTER_FEATHER,
        (True, True, True, True),
    )
    center_x, center_y = CITY_CENTER_POSITION
    center_pixels = np.asarray(center, dtype=np.float32)
    region = city[
        center_y:center_y + center.height,
        center_x:center_x + center.width,
    ].astype(np.float32)
    blended = (
        region * (1 - center_mask[:, :, None]) +
        center_pixels * center_mask[:, :, None]
    )
    city[
        center_y:center_y + center.height,
        center_x:center_x + center.width,
    ] = np.clip(blended, 0, 255).astype(np.uint8)
    city_image = Image.fromarray(city, "RGB")
    city_image.save(
        ROOT / f"city_master_{season}_source.png",
        optimize=True,
    )

    alpha = edge_weights(
        CITY_SIZE[0],
        CITY_SIZE[1],
        72,
        (True, True, True, True),
    )
    runtime = city_image.convert("RGBA")
    runtime.putalpha(Image.fromarray((alpha * 255).astype(np.uint8), "L"))
    runtime.save(ROOT / f"city_master_{season}.png", optimize=True)


def compose(season: str) -> None:
    base = Image.open(
        ROOT / f"world_master_{season}_source.png"
    ).convert("RGB").resize(WORLD_SIZE, Image.Resampling.LANCZOS)
    base_pixels = np.asarray(base, dtype=np.float32)
    colors = base_pixels * 0.18
    weights = np.full(WORLD_SIZE[::-1], 0.18, dtype=np.float32)

    beginner = expanded_region(
        Image.open(ROOT / f"beginner_master_{season}_source.png"),
        SECTOR_SIZE,
        SECTOR_SIZE,
        SECTOR_BLEED,
    )
    beginner_mask = edge_weights(
        beginner.width,
        beginner.height,
        SECTOR_BLEED * 2,
        (False, False, True, True),
    )
    add_layer(
        colors,
        weights,
        beginner,
        (-SECTOR_BLEED, -SECTOR_BLEED),
        beginner_mask,
        1.15,
    )

    for column, row in SECTOR_COORDINATES:
        panel = Image.open(
            ROOT / f"sector_{column}_{row}_gleamrise_source.png"
        ).convert("RGB")
        panel = tint(panel, season)
        panel = expanded_region(
            panel,
            SECTOR_SIZE,
            SECTOR_SIZE,
            SECTOR_BLEED,
        )
        left = column > 0
        top = row > 0
        right = column < 3
        bottom = row < 2
        mask = edge_weights(
            panel.width,
            panel.height,
            SECTOR_BLEED * 2,
            (left, top, right, bottom),
        )
        x = column * SECTOR_SIZE - SECTOR_BLEED
        y = row * SECTOR_SIZE - SECTOR_BLEED
        add_layer(colors, weights, panel, (x, y), mask)

    city_x, city_y, city_width, city_height = CITY_RECT
    city = padded_region(
        Image.open(ROOT / f"city_master_{season}_source.png"),
        CITY_BLEED,
    )
    city_mask = edge_weights(
        city.width,
        city.height,
        CITY_BLEED * 2,
        (True, True, True, True),
    )
    add_layer(
        colors,
        weights,
        city,
        (city_x - CITY_BLEED, city_y - CITY_BLEED),
        city_mask,
        1.25,
    )

    result = np.clip(colors / weights[:, :, None], 0, 255).astype(np.uint8)
    city_native = np.asarray(
        Image.open(
            ROOT / f"city_master_{season}_source.png"
        ).convert("RGB"),
        dtype=np.float32,
    )
    city_native_mask = edge_weights(
        city_width,
        city_height,
        96,
        (True, True, True, True),
    )
    city_region = result[
        city_y:city_y + city_height,
        city_x:city_x + city_width,
    ].astype(np.float32)
    result[
        city_y:city_y + city_height,
        city_x:city_x + city_width,
    ] = np.clip(
        city_region * (1 - city_native_mask[:, :, None]) +
        city_native * city_native_mask[:, :, None],
        0,
        255,
    ).astype(np.uint8)
    Image.fromarray(result, "RGB").save(
        ROOT / f"world_composite_{season}.png",
        optimize=True,
    )


def main() -> None:
    for season in SEASONS:
        compose_city_master(season)
    for season in SEASONS:
        compose(season)


if __name__ == "__main__":
    main()

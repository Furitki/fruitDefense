"""Deterministic source-to-runtime exporter for the Sunny Orchard UI art set.

Checked-in SVGs remain the lossless masters for the established vector library. Reviewed
ImageGen PNGs own the compact-control family and four control glyph silhouettes. This
script normalizes all action glyphs to a neutral white alpha-mask contract, writes runtime
exports and metadata, and composes the review gallery; it does not synthesize new forms.
"""

from __future__ import annotations

import hashlib
import json
import math
import time
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont


SET_ID = "sunny-orchard"
REVISION = "1"
SOURCE_SCALE = 2.0
OPTICAL_ALPHA_THRESHOLD = 48
ART_SET_SCRIPT_GUID = "a93ac270418f41aaac52b72f5c2a5e8c"
ART_SET_ASSET_GUID = "12cc0c638d174040bb0384d7bf17ea92"

CREAM = "#FFF6E0"
LIGHT_SUN = "#FFE7A3"
AMBER = "#FFD24D"
GREEN = "#6DBE4B"
PRIMARY_ACTION = "#436C15"
DANGER_ACTION = "#9F302B"
SAGE = "#8FBF74"
SOIL = "#8B5E3C"
SOIL_DARK = "#6F482F"
TEXT_SECONDARY = "#6F5A45"
RED = "#D34E45"
BLUE = "#67AFC4"
TERRACOTTA = "#C97846"
WHITE = "#FFFDF6"
TRANSPARENT = "#00000000"


SOURCE_DIR = Path(__file__).resolve().parent
ROOT = SOURCE_DIR.parents[4]
ART_DIR = SOURCE_DIR.parents[1]
RUNTIME_DIR = ART_DIR / "Runtime" / "sunny-orchard"
SETS_DIR = ART_DIR / "Sets"
REVIEW_DIR = ART_DIR / "Sources" / "ReferenceBoards" / "Review"
SHARED_OWNER_SET_ID = "sunny-orchard-painted"
SHARED_OWNER_MANIFEST = ART_DIR / "Sources" / SHARED_OWNER_SET_ID / "art_manifest.json"
SHARED_SLOT_RANGE = range(40, 53)
PROMPT_RECORD_PATH = "Assets/UI/Art/Sources/sunny-orchard/prompt-record.json"
IMAGEGEN_OUTPUTS = {
    "icon.control-pause": "exec-9a0fb76a-f17b-49e4-93c5-04a7661c6cc1.png",
    "icon.control-continue": "exec-d3ed2b64-1335-4dce-87f8-6b173e8ea062.png",
    "icon.control-speed": "exec-78cc5602-ae68-4dd5-ba70-90f6382df655.png",
    "icon.control-close": "exec-126f11fa-8657-4735-9f50-1fff44dd9eb7.png",
    "action.compact-control": "exec-873e1abd-352d-4f13-88ff-20d9fd77ffc0.png",
    "action.compact-control-active": "exec-01b928e9-ed4c-4edf-a463-cbc1c6014582.png",
}
ACTION_GLYPH_IDS = {
    "icon.control-pause",
    "icon.control-continue",
    "icon.control-start-wave",
    "icon.control-start",
    "icon.control-speed",
    "icon.control-retry",
    "icon.control-return",
    "icon.control-close",
    "icon.control-refresh",
}
SEMANTIC_CONTAINER_TARGETS = {
    "action.primary": (0x43, 0x6C, 0x15),
    "action.danger": (0x9F, 0x30, 0x2B),
}
CONTENT_REFERENCE_RGB = (0xFF, 0xF6, 0xE0)
MINIMUM_CONTENT_CONTRAST = 4.5


def stable_guid(namespace: str, name: str) -> str:
    return hashlib.md5(f"{namespace}:{SET_ID}:{name}".encode("utf-8")).hexdigest()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def save_png(image: Image.Image, path: Path) -> None:
    """Atomically replace a PNG, tolerating brief Windows asset-scanner locks."""
    temporary = path.with_name(path.name + ".exporting")
    image.save(temporary, format="PNG", optimize=True, compress_level=9)
    try:
        for attempt in range(40):
            try:
                temporary.replace(path)
                return
            except OSError:
                if attempt == 39:
                    raise
                time.sleep(0.05)
    finally:
        if temporary.exists():
            temporary.unlink()


def optical_inset(path: Path) -> dict[str, int]:
    """Measure significant visible padding from the final runtime PNG."""
    with Image.open(path) as source:
        rgba = source.convert("RGBA")
        significant = rgba.getchannel("A").point(
            lambda alpha: 255 if alpha >= OPTICAL_ALPHA_THRESHOLD else 0)
        bbox = significant.getbbox()
        if bbox is None:
            raise RuntimeError(f"Runtime artwork has no alpha >= 48: {path}")
        return {
            "left": bbox[0],
            "top": bbox[1],
            "right": rgba.width - bbox[2],
            "bottom": rgba.height - bbox[3],
        }


def neutralize_action_glyph(image: Image.Image) -> Image.Image:
    """Preserve every alpha byte while making visible RGB a pure-white mask."""
    rgba_image = image.convert("RGBA")
    neutral = Image.new("RGBA", rgba_image.size, (0, 0, 0, 0))
    neutral.putdata([
        (255, 255, 255, alpha) if alpha else (0, 0, 0, 0)
        for alpha in rgba_image.getchannel("A").get_flattened_data()
    ])
    return neutral


def normalize_action_glyph_master(path: Path) -> None:
    """Normalize a PNG master in place without changing canvas or alpha."""
    with Image.open(path) as source:
        rgba_image = source.convert("RGBA")
    alpha_before = rgba_image.getchannel("A").tobytes()
    normalized = neutralize_action_glyph(rgba_image)
    if normalized.getchannel("A").tobytes() != alpha_before:
        raise RuntimeError(f"Action-glyph alpha changed during normalization: {path}")
    save_png(normalized, path)


def mark_tintable_import_meta(meta_path: Path) -> None:
    """Record the mask contract without changing importer geometry or GUID."""
    text = meta_path.read_text(encoding="utf-8")
    marker = "render=tintable-action-glyph;neutral-rgb=FFFFFF"
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.lstrip().startswith("userData:"):
            prefix, current = line.split("userData:", 1)
            current = current.strip()
            if marker not in current:
                current += (";" if current else "") + marker
                lines[index] = prefix + "userData: " + current
            meta_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
            return
    raise RuntimeError(f"No userData field in action-glyph meta: {meta_path}")


def validate_neutral_action_glyph(image: Image.Image, path: Path) -> None:
    for red, green, blue, alpha in image.convert("RGBA").get_flattened_data():
        expected = (255, 255, 255) if alpha else (0, 0, 0)
        if (red, green, blue) != expected:
            raise RuntimeError(
                f"Action glyph is not a clean white alpha mask: {path}")


def mark_semantic_container_import_meta(meta_path: Path, semantic_id: str) -> None:
    text = meta_path.read_text(encoding="utf-8")
    role = semantic_id.removeprefix("action.")
    target = SEMANTIC_CONTAINER_TARGETS[semantic_id]
    marker = (f"container={role};target-rgb={target[0]:02X}{target[1]:02X}{target[2]:02X}"
              f";content-reference=FFF6E0;minimum-contrast=4.5")
    lines = text.splitlines()
    for index, line in enumerate(lines):
        if line.lstrip().startswith("userData:"):
            prefix, current = line.split("userData:", 1)
            current = current.strip()
            if marker not in current:
                current += (";" if current else "") + marker
                lines[index] = prefix + "userData: " + current
            meta_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
            return
    raise RuntimeError(f"No userData field in container meta: {meta_path}")


def linear_channel(channel: int) -> float:
    value = channel / 255.0
    return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4


def relative_luminance(rgb: tuple[int, int, int]) -> float:
    return (0.2126 * linear_channel(rgb[0])
            + 0.7152 * linear_channel(rgb[1])
            + 0.0722 * linear_channel(rgb[2]))


def contrast_ratio(first: tuple[int, int, int], second: tuple[int, int, int]) -> float:
    light, dark = sorted((relative_luminance(first), relative_luminance(second)),
                         reverse=True)
    return (light + 0.05) / (dark + 0.05)


def is_semantic_container_ink(semantic_id: str, pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, alpha = pixel
    if alpha < 192:
        return False
    if semantic_id == "action.primary":
        return green >= red * 1.2 and green >= blue * 1.5
    if semantic_id == "action.danger":
        return red >= green * 1.4 and red >= blue * 1.2
    return False


def content_region_min_contrast(image: Image.Image, semantic_id: str) -> float:
    rgba_image = image.convert("RGBA")
    width, height = rgba_image.size
    content = rgba_image.crop(
        (width // 4, height // 4, width * 3 // 4, height * 3 // 4))
    ratios = [contrast_ratio(CONTENT_REFERENCE_RGB, pixel[:3])
              for pixel in content.get_flattened_data()
              if is_semantic_container_ink(semantic_id, pixel)]
    if not ratios:
        raise RuntimeError(f"No runtime content pixels found for {semantic_id}")
    minimum = min(ratios)
    if minimum < MINIMUM_CONTENT_CONTRAST:
        raise RuntimeError(
            f"{semantic_id} content contrast {minimum:.3f}:1 is below 4.5:1")
    return minimum


def alpha_safe_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Resize generated RGBA without leaking transparent RGB into the export."""
    source = image.convert("RGBA")
    alpha = source.getchannel("A")
    premultiplied = Image.merge(
        "RGBA",
        tuple(ImageChops.multiply(channel, alpha) for channel in source.split()[:3])
        + (alpha,),
    )
    resized = premultiplied.resize(size, Image.Resampling.LANCZOS)
    pixels = []
    for red, green, blue, out_alpha in resized.get_flattened_data():
        if out_alpha == 0:
            pixels.append((0, 0, 0, 0))
            continue
        scale = 255.0 / out_alpha
        pixels.append((min(255, round(red * scale)), min(255, round(green * scale)),
                       min(255, round(blue * scale)), out_alpha))
    result = Image.new("RGBA", size)
    result.putdata(pixels)
    return result


def significant_alpha_bbox(image: Image.Image) -> tuple[int, int, int, int] | None:
    significant = image.convert("RGBA").getchannel("A").point(
        lambda alpha: 255 if alpha >= OPTICAL_ALPHA_THRESHOLD else 0)
    return significant.getbbox()


def clear_low_alpha_fringe(image: Image.Image) -> Image.Image:
    result = image.convert("RGBA")
    result.putdata([
        (0, 0, 0, 0) if alpha < OPTICAL_ALPHA_THRESHOLD else (red, green, blue, alpha)
        for red, green, blue, alpha in result.get_flattened_data()
    ])
    return result


def export_generated_master(
        source_path: Path,
        runtime_path: Path,
        geometry: str,
        semantic_id: str) -> int:
    """Normalize only imagegen pixels; never synthesize a visual form."""
    with Image.open(source_path) as source:
        rgba = source.convert("RGBA")
    bbox = significant_alpha_bbox(rgba)
    if bbox is None:
        raise RuntimeError(f"Generated master has no visible pixels: {source_path}")
    crop = rgba.crop(bbox)
    if geometry == "nine-slice":
        fitted_span = 120
        fitted = clear_low_alpha_fringe(
            alpha_safe_resize(crop, (fitted_span, fitted_span)))
        exported = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
        fitted_origin = (128 - fitted_span) // 2
        exported.alpha_composite(fitted, (fitted_origin, fitted_origin))
        if significant_alpha_bbox(exported) != (
                fitted_origin, fitted_origin,
                fitted_origin + fitted_span, fitted_origin + fitted_span):
            raise RuntimeError(f"Compact-control action normalization failed: {source_path}")
        size = 128
    else:
        target_span = 62.0
        scale = min(target_span / crop.width, target_span / crop.height)
        fitted_size = (max(1, round(crop.width * scale)),
                       max(1, round(crop.height * scale)))
        fitted = clear_low_alpha_fringe(alpha_safe_resize(crop, fitted_size))
        exported = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
        optical_x = (7 if semantic_id == "icon.control-continue"
                     else 5 if semantic_id == "icon.control-speed" else 0)
        exported.alpha_composite(
            fitted, ((96 - fitted.width) // 2 + optical_x,
                     (96 - fitted.height) // 2))
        size = 96
    runtime_path.parent.mkdir(parents=True, exist_ok=True)
    save_png(exported, runtime_path)
    return size


def rgba(value: str | tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    if isinstance(value, tuple):
        return value
    raw = value.removeprefix("#")
    if len(raw) == 6:
        raw += "FF"
    return tuple(int(raw[index:index + 2], 16) for index in range(0, 8, 2))


def svg_color(value: str | tuple[int, int, int, int]) -> tuple[str, float]:
    red, green, blue, alpha = rgba(value)
    return f"#{red:02X}{green:02X}{blue:02X}", alpha / 255.0


class VectorCanvas:
    SCALE = 4

    def __init__(self, size: int, source_over: bool = False):
        self.size = size
        self.source_over = source_over
        self.commands: list[tuple] = []

    def rounded_rect(self, box, radius, fill, stroke=None, width=0):
        self.commands.append(("rounded_rect", box, radius, fill, stroke, width))

    def rect(self, box, fill, stroke=None, width=0):
        self.commands.append(("rect", box, fill, stroke, width))

    def ellipse(self, box, fill, stroke=None, width=0):
        self.commands.append(("ellipse", box, fill, stroke, width))

    def polygon(self, points, fill, stroke=None, width=0):
        self.commands.append(("polygon", points, fill, stroke, width))

    def line(self, points, fill, width):
        self.commands.append(("line", points, fill, width))

    def arc(self, box, start, end, fill, width):
        self.commands.append(("arc", box, start, end, fill, width))

    def scale_about_center(self, factor: float):
        self.transform_about_center(factor, factor)

    def transform_about_center(
        self,
        scale_x: float = 1.0,
        scale_y: float = 1.0,
        translate_x: float = 0.0,
        translate_y: float = 0.0,
    ):
        center = self.size / 2

        def x_value(number):
            return center + (number - center) * scale_x + translate_x

        def y_value(number):
            return center + (number - center) * scale_y + translate_y

        def box(bounds):
            x0, y0, x1, y1 = bounds
            return x_value(x0), y_value(y0), x_value(x1), y_value(y1)

        def points(vertices):
            return [(x_value(x), y_value(y)) for x, y in vertices]

        scaled = []
        for command in self.commands:
            kind = command[0]
            if kind == "rounded_rect":
                _, bounds, radius, fill, stroke, width = command
                scaled.append((kind, box(bounds), radius * min(scale_x, scale_y), fill, stroke, width))
            elif kind in {"rect", "ellipse"}:
                _, bounds, fill, stroke, width = command
                scaled.append((kind, box(bounds), fill, stroke, width))
            elif kind == "polygon":
                _, vertices, fill, stroke, width = command
                scaled.append((kind, points(vertices), fill, stroke, width))
            elif kind == "line":
                _, vertices, fill, width = command
                scaled.append((kind, points(vertices), fill, width))
            elif kind == "arc":
                _, bounds, start, end, fill, width = command
                scaled.append((kind, box(bounds), start, end, fill, width))
            else:
                raise ValueError(f"Unknown vector command {kind}")
        self.commands = scaled

    def render_png(self, path: Path):
        scale = self.SCALE
        image = Image.new("RGBA", (self.size * scale, self.size * scale), (0, 0, 0, 0))
        def box(value):
            return tuple(round(item * scale) for item in value)

        def points(value):
            return [(round(x * scale), round(y * scale)) for x, y in value]

        for command in self.commands:
            target = Image.new("RGBA", image.size, (0, 0, 0, 0)) if self.source_over else image
            draw = ImageDraw.Draw(target)
            kind = command[0]
            if kind == "rounded_rect":
                _, bounds, radius, fill, stroke, width = command
                draw.rounded_rectangle(box(bounds), radius=round(radius * scale), fill=rgba(fill),
                                       outline=rgba(stroke) if stroke else None,
                                       width=round(width * scale) if stroke else 1)
            elif kind == "rect":
                _, bounds, fill, stroke, width = command
                draw.rectangle(box(bounds), fill=rgba(fill), outline=rgba(stroke) if stroke else None,
                               width=round(width * scale) if stroke else 1)
            elif kind == "ellipse":
                _, bounds, fill, stroke, width = command
                draw.ellipse(box(bounds), fill=rgba(fill), outline=rgba(stroke) if stroke else None,
                             width=round(width * scale) if stroke else 1)
            elif kind == "polygon":
                _, vertices, fill, stroke, width = command
                scaled = points(vertices)
                draw.polygon(scaled, fill=rgba(fill))
                if stroke:
                    draw.line(scaled + [scaled[0]], fill=rgba(stroke), width=round(width * scale),
                              joint="curve")
            elif kind == "line":
                _, vertices, fill, width = command
                draw.line(points(vertices), fill=rgba(fill), width=round(width * scale), joint="curve")
            elif kind == "arc":
                _, bounds, start, end, fill, width = command
                draw.arc(box(bounds), start=start, end=end, fill=rgba(fill), width=round(width * scale))
            else:
                raise ValueError(f"Unknown vector command {kind}")
            if self.source_over:
                image.alpha_composite(target)

        save_png(image.resize((self.size, self.size), Image.Resampling.LANCZOS), path)

    def render_svg(self, path: Path, semantic_id: str):
        elements = []

        def paint(name: str, value) -> str:
            color, opacity = svg_color(value)
            return f'{name}="{color}" {name}-opacity="{opacity:.4f}"'

        def fmt_points(value) -> str:
            return " ".join(f"{x:g},{y:g}" for x, y in value)

        for command in self.commands:
            kind = command[0]
            if kind == "rounded_rect":
                _, (x0, y0, x1, y1), radius, fill, stroke, width = command
                attrs = paint("fill", fill)
                if stroke:
                    attrs += f' {paint("stroke", stroke)} stroke-width="{width:g}"'
                elements.append(f'<rect x="{x0:g}" y="{y0:g}" width="{x1-x0:g}" height="{y1-y0:g}" rx="{radius:g}" {attrs}/>')
            elif kind == "rect":
                _, (x0, y0, x1, y1), fill, stroke, width = command
                attrs = paint("fill", fill)
                if stroke:
                    attrs += f' {paint("stroke", stroke)} stroke-width="{width:g}"'
                elements.append(f'<rect x="{x0:g}" y="{y0:g}" width="{x1-x0:g}" height="{y1-y0:g}" {attrs}/>')
            elif kind == "ellipse":
                _, (x0, y0, x1, y1), fill, stroke, width = command
                attrs = paint("fill", fill)
                if stroke:
                    attrs += f' {paint("stroke", stroke)} stroke-width="{width:g}"'
                elements.append(f'<ellipse cx="{(x0+x1)/2:g}" cy="{(y0+y1)/2:g}" rx="{(x1-x0)/2:g}" ry="{(y1-y0)/2:g}" {attrs}/>')
            elif kind == "polygon":
                _, vertices, fill, stroke, width = command
                attrs = paint("fill", fill)
                if stroke:
                    attrs += f' {paint("stroke", stroke)} stroke-width="{width:g}" stroke-linejoin="round"'
                elements.append(f'<polygon points="{fmt_points(vertices)}" {attrs}/>')
            elif kind == "line":
                _, vertices, fill, width = command
                elements.append(f'<polyline points="{fmt_points(vertices)}" fill="none" {paint("stroke", fill)} stroke-width="{width:g}" stroke-linecap="round" stroke-linejoin="round"/>')
            elif kind == "arc":
                _, (x0, y0, x1, y1), start, end, fill, width = command
                cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
                rx, ry = (x1 - x0) / 2, (y1 - y0) / 2
                start_r, end_r = math.radians(start), math.radians(end)
                sx, sy = cx + rx * math.cos(start_r), cy + ry * math.sin(start_r)
                ex, ey = cx + rx * math.cos(end_r), cy + ry * math.sin(end_r)
                delta = (end - start) % 360
                large = 1 if delta > 180 else 0
                elements.append(f'<path d="M {sx:g} {sy:g} A {rx:g} {ry:g} 0 {large} 1 {ex:g} {ey:g}" fill="none" {paint("stroke", fill)} stroke-width="{width:g}" stroke-linecap="round"/>')

        content = "\n  ".join(elements)
        path.write_text(
            f'<?xml version="1.0" encoding="UTF-8"?>\n'
            f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.size}" height="{self.size}" viewBox="0 0 {self.size} {self.size}" role="img" aria-label="{semantic_id}">\n'
            f'  <title>{semantic_id}</title>\n  {content}\n</svg>\n',
            encoding="utf-8",
        )


def surface_canvas(stem: str, fill: str, accent: str | None = None) -> VectorCanvas:
    if stem == "surface_screen_background":
        # Semi-transparent orchard accents must source-over the opaque base. Drawing
        # them directly into one Pillow RGBA surface replaces alpha and creates black
        # holes when this background is the only full-screen layer.
        canvas = VectorCanvas(256, source_over=True)
        canvas.rect((0, 0, 256, 256), "#F5DDAE")
        canvas.ellipse((18, 22, 88, 92), "#FFE7A320")
        canvas.ellipse((188, 174, 246, 232), "#6DBE4B12")
        return canvas
    if stem == "surface_scrim":
        canvas = VectorCanvas(32)
        # RuntimeUiGui applies both Theme.Colors.Scrim and Feedback.ScrimOpacity.
        # Keep the source/export neutral so the theme remains the sole tint/opacity owner.
        canvas.rect((0, 0, 32, 32), "#FFFFFFFF")
        return canvas
    if stem == "surface_gameplay_stage":
        canvas = VectorCanvas(128)
        # Standard panels begin four logical points inside their owner. At 2x
        # source scale the heavy stage therefore also starts at pixel 8, while
        # its eight-pixel inward rail produces the approved four-point band.
        canvas.rounded_rect((8, 8, 115, 119), 14, "#5A3826")
        canvas.rounded_rect((10, 10, 113, 117), 13,
                            "#9A633D", "#4B2E20", 2)
        canvas.rounded_rect((12, 12, 111, 115), 12, "#835231")
        canvas.line([(24, 13), (99, 13)], "#B97A48", 2)
        canvas.line([(24, 113), (99, 113)], "#6B4128", 1.5)
        canvas.line([(13, 26), (13, 101)], "#B97A48", 1.5)
        canvas.line([(109, 26), (109, 101)], "#6B4128", 1.5)
        # The inner rail is square across the 20px slice partitions so no
        # protected corner curve leaks into a stretchable row or column.
        canvas.rect((16, 16, 109, 111), TRANSPARENT)
        canvas.line([(16, 16), (109, 16)], "#4B2E20", 2)
        canvas.line([(16, 111), (109, 111)], "#4B2E20", 2)
        canvas.line([(16, 16), (16, 111)], "#4B2E20", 2)
        canvas.line([(110, 16), (110, 111)], "#4B2E20", 2)
        return canvas

    canvas = VectorCanvas(128)
    canvas.rounded_rect((7, 10, 121, 123), 22, "#5B3C292E")
    canvas.rounded_rect((5, 4, 123, 119), 22, fill, SOIL, 4)
    canvas.line([(22, 9), (106, 9)], "#FFFFFF70", 2)

    if stem in {"surface_panel_raised", "surface_modal", "surface_result"}:
        canvas.ellipse((94, 15, 101, 22), AMBER, SOIL, 1.5)
        canvas.ellipse((104, 15, 111, 22), GREEN, SOIL, 1.5)
    if stem in {"surface_detail", "surface_status"}:
        canvas.polygon([(16, 20), (28, 13), (31, 27)], GREEN, SOIL, 1.5)
        canvas.line([(18, 28), (30, 15)], SOIL, 1.5)
    if stem in {"slot_tool", "slot_nursery"}:
        inner = "#FFFDF6B8" if stem == "slot_tool" else "#EAF6DDB8"
        canvas.rounded_rect((18, 17, 110, 107), 16, inner, SOIL, 2)
        canvas.line([(34, 102), (94, 102)], accent or AMBER, 3)
    elif accent:
        canvas.line([(28, 111), (100, 111)], accent, 3)
    return canvas


def icon_canvas(stem: str) -> VectorCanvas:
    c = VectorCanvas(96)

    if stem == "marker_selected":
        c.ellipse((14, 14, 82, 82), AMBER, SOIL, 4)
        c.line([(29, 49), (43, 63), (69, 33)], SOIL_DARK, 7)
    elif stem == "indicator_disabled":
        c.ellipse((16, 16, 80, 80), "#D6E2C8", SOIL, 4)
        c.line([(27, 69), (69, 27)], SOIL, 8)
    elif stem == "indicator_loading":
        c.arc((18, 18, 78, 78), 205, 500, SOIL, 8)
        c.polygon([(71, 20), (80, 38), (61, 35)], AMBER, SOIL, 2)
    elif stem == "indicator_success":
        c.ellipse((14, 14, 82, 82), GREEN, SOIL, 4)
        c.line([(28, 49), (43, 64), (70, 32)], WHITE, 7)
    elif stem == "indicator_warning":
        c.polygon([(48, 13), (84, 79), (12, 79)], AMBER, SOIL, 4)
        c.rounded_rect((44, 33, 52, 58), 4, SOIL)
        c.ellipse((44, 65, 52, 73), SOIL)
    elif stem == "indicator_error":
        c.polygon([(34, 13), (62, 13), (83, 34), (83, 62), (62, 83), (34, 83), (13, 62), (13, 34)], RED, SOIL, 4)
        c.line([(31, 31), (65, 65)], WHITE, 7)
        c.line([(65, 31), (31, 65)], WHITE, 7)
    elif stem == "indicator_drag_legal":
        target_corners(c, GREEN)
        c.line([(34, 50), (45, 61), (65, 37)], SOIL_DARK, 6)
    elif stem == "indicator_drag_illegal":
        target_corners(c, RED)
        c.ellipse((27, 27, 69, 69), TRANSPARENT, RED, 6)
        c.line([(33, 63), (63, 33)], RED, 6)
    elif stem == "indicator_merge":
        c.ellipse((16, 29, 53, 66), LIGHT_SUN, SOIL, 3)
        c.ellipse((43, 29, 80, 66), GREEN, SOIL, 3)
        c.line([(22, 75), (39, 75), (48, 63)], SOIL, 4)
        c.line([(74, 75), (57, 75), (48, 63)], SOIL, 4)
    elif stem == "indicator_swap":
        c.line([(21, 35), (71, 35)], SOIL, 6)
        c.polygon([(71, 25), (84, 35), (71, 45)], AMBER, SOIL, 2)
        c.line([(75, 61), (25, 61)], SOIL, 6)
        c.polygon([(25, 51), (12, 61), (25, 71)], GREEN, SOIL, 2)
    elif stem == "icon_resource_sun":
        for angle in range(0, 360, 45):
            r = math.radians(angle)
            c.line([(48 + math.cos(r) * 31, 48 + math.sin(r) * 31),
                    (48 + math.cos(r) * 39, 48 + math.sin(r) * 39)], SOIL, 4)
        c.ellipse((23, 23, 73, 73), AMBER, SOIL, 4)
        c.ellipse((37, 39, 43, 45), SOIL)
        c.ellipse((53, 39, 59, 45), SOIL)
        c.arc((35, 39, 61, 61), 35, 145, SOIL, 3)
    elif stem == "icon_resource_core":
        c.polygon([(48, 78), (23, 55), (20, 37), (30, 24), (45, 25), (48, 33), (51, 25), (66, 24), (76, 37), (73, 55)], RED, SOIL, 4)
        c.polygon([(49, 24), (58, 13), (72, 18), (61, 29)], GREEN, SOIL, 3)
        c.line([(47, 29), (47, 18)], SOIL, 4)
    elif stem == "icon_resource_wave":
        c.line([(15, 36), (27, 28), (39, 36), (51, 28), (63, 36), (75, 28), (82, 34)], BLUE, 7)
        c.line([(15, 54), (27, 46), (39, 54), (51, 46), (63, 54), (75, 46), (82, 52)], SOIL, 6)
        c.line([(23, 70), (73, 70)], AMBER, 5)
    elif stem == "icon_control_pause":
        c.rounded_rect((24, 17, 41, 79), 7, "#FFFFFF")
        c.rounded_rect((55, 17, 72, 79), 7, "#FFFFFF")
    elif stem == "icon_control_continue":
        c.polygon([(29, 17), (78, 48), (29, 79)], "#FFFFFF", "#FFFFFF", 4)
    elif stem == "icon_control_speed":
        c.polygon([(13, 20), (48, 48), (13, 76)], "#FFFFFF", "#FFFFFF", 4)
        c.polygon([(45, 20), (82, 48), (45, 76)], "#FFFFFF", "#FFFFFF", 4)
    elif stem == "icon_control_retry":
        c.arc((18, 18, 78, 78), 35, 330, "#FFFFFF", 7)
        c.polygon([(18, 22), (39, 19), (27, 39)], "#FFFFFF", "#FFFFFF", 2)
    elif stem == "icon_control_return":
        c.rounded_rect((43, 21, 76, 76), 7, "#FFFFFF", "#FFFFFF", 4)
        c.line([(57, 48), (18, 48)], "#FFFFFF", 7)
        c.polygon([(18, 48), (36, 32), (36, 64)], "#FFFFFF", "#FFFFFF", 2)
    elif stem == "icon_control_close":
        c.ellipse((16, 16, 80, 80), "#FFFFFF", "#FFFFFF", 4)
        c.line([(31, 31), (65, 65)], "#FFFFFF", 7)
        c.line([(65, 31), (31, 65)], "#FFFFFF", 7)
    elif stem == "icon_tool_pot":
        c.polygon([(26, 35), (70, 35), (64, 78), (32, 78)], TERRACOTTA, SOIL, 4)
        c.rounded_rect((20, 26, 76, 42), 7, "#E59A5D", SOIL, 4)
        c.ellipse((34, 17, 50, 32), GREEN, SOIL, 3)
        c.ellipse((46, 14, 63, 31), SAGE, SOIL, 3)
    elif stem == "icon_control_refresh":
        c.arc((17, 17, 79, 79), 195, 360, "#FFFFFF", 7)
        c.polygon([(76, 20), (82, 42), (61, 35)], "#FFFFFF", "#FFFFFF", 2)
        c.arc((17, 17, 79, 79), 15, 180, "#FFFFFF", 7)
        c.polygon([(20, 76), (14, 54), (35, 61)], "#FFFFFF", "#FFFFFF", 2)
    else:
        raise ValueError(f"No icon recipe for {stem}")
    c.scale_about_center(0.84)

    # Optical corrections are part of the editable vector master, not runtime
    # layout compensation. They keep every production candidate on the same
    # 96px canvas / 12px inset contract while preserving the approved silhouettes.
    optical_adjustments = {
        "indicator_loading": (1.08, 1.08, -2.6, 0.0),
        "indicator_warning": (1.0, 0.88, 0.0, -3.0),
        "indicator_drag_legal": (1.04, 1.04, 0.0, 0.0),
        "indicator_drag_illegal": (1.04, 1.04, 0.0, 0.0),
        "icon_control_pause": (1.05, 1.05, 0.0, 0.0),
        "icon_control_speed": (1.0, 1.0, 2.0, 0.0),
        "icon_control_retry": (1.08, 1.08, 3.0, 0.0),
        "icon_control_return": (1.12, 1.12, -2.0, 0.0),
    }
    if stem in optical_adjustments:
        c.transform_about_center(*optical_adjustments[stem])
    return c


def target_corners(canvas: VectorCanvas, color: str):
    canvas.line([(18, 36), (18, 18), (36, 18)], color, 6)
    canvas.line([(60, 18), (78, 18), (78, 36)], color, 6)
    canvas.line([(78, 60), (78, 78), (60, 78)], color, 6)
    canvas.line([(36, 78), (18, 78), (18, 60)], color, 6)


SLOTS = [
    (0, "surface.screen-background", "surface_screen_background", "stretch", 0, 0),
    (1, "surface.safe-area", "surface_safe_area", "nine-slice", 32, 20),
    (2, "surface.panel-standard", "surface_panel_standard", "nine-slice", 32, 20),
    (3, "surface.panel-raised", "surface_panel_raised", "nine-slice", 32, 20),
    (4, "surface.card-selectable", "surface_card_selectable", "nine-slice", 32, 20),
    (5, "surface.metric", "surface_metric", "nine-slice", 32, 20),
    (6, "surface.status", "surface_status", "nine-slice", 32, 20),
    (7, "surface.detail", "surface_detail", "nine-slice", 32, 20),
    (8, "surface.modal", "surface_modal", "nine-slice", 32, 20),
    (9, "surface.result", "surface_result", "nine-slice", 32, 20),
    (10, "surface.scrim", "surface_scrim", "stretch", 0, 0),
    (11, "action.primary", "action_primary", "nine-slice", 32, 20),
    (12, "action.secondary", "action_secondary", "nine-slice", 32, 20),
    (13, "action.quiet", "action_quiet", "nine-slice", 32, 20),
    (14, "action.danger", "action_danger", "nine-slice", 32, 20),
    (15, "slot.tool", "slot_tool", "nine-slice", 32, 20),
    (16, "slot.nursery", "slot_nursery", "nine-slice", 32, 20),
    (17, "marker.selected", "marker_selected", "icon", 0, 12),
    (18, "indicator.disabled", "indicator_disabled", "icon", 0, 12),
    (19, "indicator.loading", "indicator_loading", "icon", 0, 12),
    (20, "indicator.success", "indicator_success", "icon", 0, 12),
    (21, "indicator.warning", "indicator_warning", "icon", 0, 12),
    (22, "indicator.error", "indicator_error", "icon", 0, 12),
    (23, "indicator.drag-legal", "indicator_drag_legal", "icon", 0, 12),
    (24, "indicator.drag-illegal", "indicator_drag_illegal", "icon", 0, 12),
    (25, "indicator.merge", "indicator_merge", "icon", 0, 12),
    (26, "indicator.swap", "indicator_swap", "icon", 0, 12),
    (27, "icon.resource-sun", "icon_resource_sun", "icon", 0, 12),
    (28, "icon.resource-core", "icon_resource_core", "icon", 0, 12),
    (29, "icon.resource-wave", "icon_resource_wave", "icon", 0, 12),
    (30, "icon.control-pause", "icon_control_pause", "icon", 0, 12),
    (31, "icon.control-continue", "icon_control_continue", "icon", 0, 12),
    (32, "icon.control-speed", "icon_control_speed", "icon", 0, 12),
    (33, "icon.control-start-wave", "icon_control_continue", "icon", 0, 12),
    (34, "icon.control-retry", "icon_control_retry", "icon", 0, 12),
    (35, "icon.control-return", "icon_control_return", "icon", 0, 12),
    (36, "icon.control-close", "icon_control_close", "icon", 0, 12),
    (37, "icon.tool-pot", "icon_tool_pot", "icon", 0, 12),
    (38, "icon.control-start", "icon_control_continue", "icon", 0, 12),
    (39, "icon.control-refresh", "icon_control_refresh", "icon", 0, 12),
    (53, "action.compact-control", "action_compact_control", "nine-slice", 32, 20),
    (54, "action.compact-control-active", "action_compact_control_active",
     "nine-slice", 32, 20),
    (55, "surface.gameplay-stage", "surface_gameplay_stage",
     "nine-slice", 20, 20),
]


SURFACE_STYLES = {
    "surface_safe_area": (CREAM, None),
    "surface_panel_standard": (CREAM, None),
    "surface_panel_raised": ("#FFF0C5", AMBER),
    "surface_card_selectable": (CREAM, LIGHT_SUN),
    "surface_metric": ("#FFF0C9", LIGHT_SUN),
    "surface_status": ("#FFF4D6", AMBER),
    "surface_detail": (CREAM, GREEN),
    "surface_modal": ("#FFF8E8", AMBER),
    "surface_result": ("#FFF0C1", GREEN),
    "action_primary": (PRIMARY_ACTION, LIGHT_SUN),
    "action_secondary": (SAGE, CREAM),
    "action_quiet": (CREAM, SAGE),
    "action_danger": (DANGER_ACTION, LIGHT_SUN),
    "slot_tool": ("#FFF0C9", AMBER),
    "slot_nursery": ("#EAF6DD", GREEN),
    "surface_gameplay_stage": (TRANSPARENT, None),
}


def texture_meta(guid: str, stem: str, semantic_id: str, size: int, border: int) -> str:
    sprite_id = stable_guid("sprite", stem)
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {size}
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {border}, y: {border}, z: {border}, w: {border}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData: ui-art-set={SET_ID};slot={semantic_id};source-scale=2
  assetBundleName:
  assetBundleVariant:
"""


def default_meta(guid: str) -> str:
    return f"""fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: ui-art-source={SET_ID}
  assetBundleName:
  assetBundleVariant:
"""


def source_texture_meta(guid: str, stem: str, semantic_id: str) -> str:
    text = texture_meta(guid, stem, semantic_id, 2048, 0)
    text = text.replace("  spriteMode: 1", "  spriteMode: 0")
    text = text.replace("  spriteMeshType: 0", "  spriteMeshType: 1")
    text = text.replace("  textureType: 8", "  textureType: 0")
    text = text.replace(
        f"    spriteID: {stable_guid('sprite', stem)}", "    spriteID:")
    text = text.replace(
        f"  userData: ui-art-set={SET_ID};slot={semantic_id};source-scale=2",
        f"  userData: ui-art-source={SET_ID};slot={semantic_id};generated=built-in-imagegen")
    return text


def review_texture_meta(guid: str, stem: str) -> str:
    text = source_texture_meta(guid, stem, "review.sunny-orchard-gallery")
    return text.replace(
        "generated=built-in-imagegen", "generated=deterministic-review-gallery")


def build_art_set_asset(bindings: list[dict]) -> str:
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {ART_SET_SCRIPT_GUID}, type: 3}}",
        "  m_Name: SunnyOrchardRuntimeUiArtSet",
        "  m_EditorClassIdentifier: Assembly-CSharp::FruitDefense.UI.RuntimeUiArtSet",
        f"  setId: {SET_ID}",
        f"  revision: '{REVISION}'",
        "  bindings:",
    ]
    for item in bindings:
        border = item["slice_border"]
        inset = item["safe_inset"]
        optical = item["optical_inset"]
        lines.extend([
            f"  - slot: {item['slot']}",
            f"    texture: {{fileID: 2800000, guid: {item['guid']}, type: 3}}",
            f"    sprite: {{fileID: 21300000, guid: {item['guid']}, type: 3}}",
            "    sliceBorder:",
            f"      left: {border}",
            f"      top: {border}",
            f"      right: {border}",
            f"      bottom: {border}",
            "    safeInset:",
            f"      left: {inset}",
            f"      top: {inset}",
            f"      right: {inset}",
            f"      bottom: {inset}",
            "    opticalInset:",
            f"      left: {optical['left']}",
            f"      top: {optical['top']}",
            f"      right: {optical['right']}",
            f"      bottom: {optical['bottom']}",
            f"    pixelsPerLogicalUnit: {item['pixels_per_logical_unit']:g}",
        ])
    return "\n".join(lines) + "\n"


def nine_slice(image: Image.Image, output_size: tuple[int, int], border=32) -> Image.Image:
    width, height = output_size
    result = Image.new("RGBA", output_size, (0, 0, 0, 0))
    source_width, source_height = image.size
    xs = (0, border, source_width - border, source_width)
    ys = (0, border, source_height - border, source_height)
    dx = (0, border, width - border, width)
    dy = (0, border, height - border, height)
    for row in range(3):
        for column in range(3):
            crop = image.crop((xs[column], ys[row], xs[column + 1], ys[row + 1]))
            target = (dx[column], dy[row], dx[column + 1], dy[row + 1])
            target_size = (target[2] - target[0], target[3] - target[1])
            if crop.size != target_size:
                crop = crop.resize(target_size, Image.Resampling.BILINEAR)
            result.alpha_composite(crop, (target[0], target[1]))
    return result


def build_gallery(unique_items: list[dict]):
    width, height = 1600, 2020
    gallery = Image.new("RGB", (width, height), rgba("#F3DFC0")[:3])
    draw = ImageDraw.Draw(gallery)
    font_path = Path("C:/Windows/Fonts/arial.ttf")
    bold_path = Path("C:/Windows/Fonts/arialbd.ttf")
    font = ImageFont.truetype(str(font_path), 22) if font_path.exists() else ImageFont.load_default()
    small = ImageFont.truetype(str(font_path), 17) if font_path.exists() else ImageFont.load_default()
    title = ImageFont.truetype(str(bold_path), 38) if bold_path.exists() else font
    draw.text((64, 38), "SUNNY ORCHARD — PRODUCTION UI ART SET", fill=rgba(SOIL)[:3], font=title)
    draw.text(
        (66, 92),
        "55 semantic slots · 40 unique exports · revision 1 · SVG + built-in ImageGen masters",
        fill=rgba(TEXT_SECONDARY)[:3], font=font)

    swatches = [CREAM, LIGHT_SUN, AMBER, PRIMARY_ACTION, SAGE, SOIL, RED]
    for index, color in enumerate(swatches):
        x = 66 + index * 112
        draw.rounded_rectangle((x, 132, x + 90, 180), radius=12, fill=rgba(color)[:3], outline=rgba(SOIL)[:3], width=2)

    surface_items = [item for item in unique_items if item["geometry"] != "icon"]
    icon_items = [item for item in unique_items if item["geometry"] == "icon"]
    draw.text((64, 212), "SURFACES / ACTIONS / SLOTS", fill=rgba(SOIL)[:3], font=title)

    cell_w, cell_h = 500, 150
    start_y = 275
    for index, item in enumerate(surface_items):
        column, row = index % 3, index // 3
        x, y = 64 + column * cell_w, start_y + row * cell_h
        draw.rounded_rectangle((x, y, x + 458, y + 124), radius=18, fill=rgba("#F8E9CC")[:3])
        image = Image.open(RUNTIME_DIR / f"{item['stem']}.png").convert("RGBA")
        if item["semantic_id"] == "surface.scrim":
            # Evidence previews the SunnyOrchardDefault runtime tint. The production
            # texture itself remains opaque neutral white.
            preview = Image.new("RGBA", (92, 92), (61, 42, 32, round(173 * 0.68)))
        elif item["geometry"] == "nine-slice":
            preview = nine_slice(image, (230, 92))
        else:
            preview = image.resize((92, 92), Image.Resampling.BILINEAR)
        gallery.paste(preview, (x + 14, y + 16), preview)
        draw.text((x + 258, y + 28), item["semantic_id"], fill=rgba(SOIL)[:3], font=small)
        draw.text((x + 258, y + 62), f"{item['geometry']} · {item['size']} px", fill=rgba(TEXT_SECONDARY)[:3], font=small)

    icon_start = start_y + math.ceil(len(surface_items) / 3) * cell_h + 70
    draw.text((64, icon_start - 58), "MARKERS / INDICATORS / COMMON ICONS", fill=rgba(SOIL)[:3], font=title)
    icon_cell_w, icon_cell_h = 250, 180
    for index, item in enumerate(icon_items):
        column, row = index % 6, index // 6
        x, y = 64 + column * icon_cell_w, icon_start + row * icon_cell_h
        draw.rounded_rectangle((x, y, x + 210, y + 156), radius=18, fill=rgba(CREAM)[:3], outline=rgba("#D8BC91")[:3], width=2)
        icon = Image.open(RUNTIME_DIR / f"{item['stem']}.png").convert("RGBA")
        gallery.paste(icon, (x + 57, y + 14), icon)
        label = item["semantic_id"].replace("icon.", "").replace("indicator.", "").replace("marker.", "")
        draw.text((x + 12, y + 118), label, fill=rgba(SOIL)[:3], font=small)

    gallery_stem = "sunny-orchard-56-gallery"
    gallery_path = REVIEW_DIR / f"{gallery_stem}.png"
    save_png(gallery, gallery_path)
    gallery_meta_path = gallery_path.with_suffix(".png.meta")
    if not gallery_meta_path.exists():
        gallery_meta_path.write_text(
            review_texture_meta(stable_guid("review", gallery_stem), gallery_stem),
            encoding="utf-8")


def main():
    RUNTIME_DIR.mkdir(parents=True, exist_ok=True)
    SETS_DIR.mkdir(parents=True, exist_ok=True)
    REVIEW_DIR.mkdir(parents=True, exist_ok=True)

    # The approved convention is lowercase kebab-case. Remove obsolete output from an
    # interrupted pre-convention export rather than retaining compatibility duplicates.
    for pattern in ("*_*.svg", "*_*.svg.meta"):
        for stale in SOURCE_DIR.glob(pattern):
            stale.unlink()
    for pattern in ("*_*.png", "*_*.png.meta"):
        for stale in RUNTIME_DIR.glob(pattern):
            stale.unlink()

    unique: dict[str, dict] = {}
    bindings = []
    for slot, semantic_id, stem, geometry, border, inset in SLOTS:
        if stem not in unique:
            file_stem = stem.replace("_", "-")
            generated = semantic_id in IMAGEGEN_OUTPUTS
            if generated:
                source_path = SOURCE_DIR / f"{file_stem}.png"
                runtime_path = RUNTIME_DIR / f"{file_stem}.png"
                if not source_path.is_file():
                    raise RuntimeError(f"Missing imagegen master: {source_path}")
                if semantic_id in ACTION_GLYPH_IDS:
                    normalize_action_glyph_master(source_path)
                canvas_size = export_generated_master(
                    source_path, runtime_path, geometry, semantic_id)
            elif geometry == "icon":
                canvas = icon_canvas(stem)
            elif stem in {"surface_screen_background", "surface_scrim"}:
                canvas = surface_canvas(stem, CREAM)
            else:
                fill, accent = SURFACE_STYLES[stem]
                canvas = surface_canvas(stem, fill, accent)
            if not generated:
                source_path = SOURCE_DIR / f"{file_stem}.svg"
                runtime_path = RUNTIME_DIR / f"{file_stem}.png"
                canvas.render_svg(source_path, semantic_id)
                canvas.render_png(runtime_path)
                canvas_size = canvas.size
            source_guid = stable_guid("source", file_stem)
            runtime_guid = stable_guid("runtime", file_stem)
            source_meta_path = source_path.with_suffix(source_path.suffix + ".meta")
            runtime_meta_path = runtime_path.with_suffix(".png.meta")
            if not source_meta_path.exists():
                source_meta_path.write_text(
                    source_texture_meta(source_guid, file_stem, semantic_id)
                    if generated else default_meta(source_guid), encoding="utf-8")
            if not runtime_meta_path.exists():
                runtime_meta_path.write_text(
                    texture_meta(runtime_guid, file_stem, semantic_id, canvas_size, border), encoding="utf-8")
            if semantic_id in ACTION_GLYPH_IDS:
                with Image.open(runtime_path) as runtime:
                    runtime_mask = neutralize_action_glyph(runtime)
                validate_neutral_action_glyph(runtime_mask, runtime_path)
                save_png(runtime_mask, runtime_path)
                mark_tintable_import_meta(source_meta_path)
                mark_tintable_import_meta(runtime_meta_path)
            if semantic_id in SEMANTIC_CONTAINER_TARGETS:
                mark_semantic_container_import_meta(source_meta_path, semantic_id)
                mark_semantic_container_import_meta(runtime_meta_path, semantic_id)
            unique[stem] = {
                "stem": file_stem,
                "semantic_id": semantic_id,
                "geometry": geometry,
                "size": canvas_size,
                "source": source_path.relative_to(ROOT).as_posix(),
                "runtime": runtime_path.relative_to(ROOT).as_posix(),
                "sourceSha256": sha256(source_path),
                "runtimeSha256": sha256(runtime_path),
                "guid": runtime_guid,
                "slice_border": border,
                "safe_inset": inset,
                "optical_inset": optical_inset(runtime_path),
                "pixels_per_logical_unit": SOURCE_SCALE,
            }
            if semantic_id in ACTION_GLYPH_IDS:
                unique[stem].update({
                    "render_contract": "tintable-action-glyph",
                    "neutral_rgb": "FFFFFF",
                })
            if semantic_id in SEMANTIC_CONTAINER_TARGETS:
                with Image.open(runtime_path) as runtime:
                    minimum_contrast = content_region_min_contrast(
                        runtime, semantic_id)
                target = SEMANTIC_CONTAINER_TARGETS[semantic_id]
                unique[stem].update({
                    "container_contract": "semantic-action-container",
                    "target_rgb": f"{target[0]:02X}{target[1]:02X}{target[2]:02X}",
                    "content_reference_rgb": "FFF6E0",
                    "content_region_min_contrast": round(minimum_contrast, 4),
                })
            if generated:
                unique[stem].update({
                    "imagegen_provider": "built-in-imagegen",
                    "imagegen_output": IMAGEGEN_OUTPUTS[semantic_id],
                    "prompt_record": PROMPT_RECORD_PATH,
                })
        entry = dict(unique[stem])
        entry["slot"] = slot
        entry["semantic_id"] = semantic_id
        bindings.append(entry)

    # The finite 56-slot contract keeps A complete by explicitly sharing the
    # approved painted ornaments/illustrations. The owner manifest remains the
    # single source/runtime authority; no duplicate PNG is generated here.
    owner_manifest = json.loads(SHARED_OWNER_MANIFEST.read_text(encoding="utf-8"))
    if owner_manifest.get("setId") != SHARED_OWNER_SET_ID:
        raise RuntimeError("Shared UI art owner manifest has the wrong setId")
    owner_rows = {row["slot"]: row for row in owner_manifest["bindings"]}
    for slot in SHARED_SLOT_RANGE:
        if slot not in owner_rows:
            raise RuntimeError(f"Shared UI art owner is missing slot {slot}")
        shared = dict(owner_rows[slot])
        shared["shared_from_set"] = SHARED_OWNER_SET_ID
        shared["optical_inset"] = optical_inset(ROOT / shared["runtime"])
        bindings.append(shared)
    bindings.sort(key=lambda row: row["slot"])

    manifest = {
        "schema": "fruit-defense.runtime-ui-art-manifest.v2",
        "setId": SET_ID,
        "revision": REVISION,
        "approvedDirection": "A - Sunny Orchard",
        "sourceScale": SOURCE_SCALE,
        "slotCount": len(bindings),
        "uniqueExportCount": len(unique),
        "sharedBindings": {
            "icon.control-continue": ["icon.control-continue", "icon.control-start-wave", "icon.control-start"],
            "sunny-orchard-painted": [row["semantic_id"] for row in bindings if row.get("shared_from_set")]
        },
        "bindings": bindings,
        "importContract": {
            "textureType": "Sprite (2D and UI)",
            "spriteMode": "Single",
            "meshType": "Full Rect",
            "sRGB": True,
            "alphaIsTransparency": True,
            "filter": "Bilinear",
            "wrap": "Clamp",
            "mipmaps": False,
            "readWrite": False,
            "compression": "Uncompressed",
            "pixelsPerUnit": 100,
        },
    }
    manifest_path = SOURCE_DIR / "art_manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    manifest_meta_path = manifest_path.with_suffix(".json.meta")
    if not manifest_meta_path.exists():
        manifest_meta_path.write_text(
            default_meta(stable_guid("source", "art_manifest")), encoding="utf-8")

    asset_path = SETS_DIR / "SunnyOrchardRuntimeUiArtSet.asset"
    asset_path.write_text(build_art_set_asset(bindings), encoding="utf-8")
    asset_path.with_suffix(".asset.meta").write_text(
        f"fileFormatVersion: 2\nguid: {ART_SET_ASSET_GUID}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: production-art-set={SET_ID};revision={REVISION}\n  assetBundleName:\n  assetBundleVariant:\n",
        encoding="utf-8",
    )

    build_gallery(list(unique.values()))
    print(f"Generated {len(unique)} unique exports for {len(bindings)} bindings.")


if __name__ == "__main__":
    main()

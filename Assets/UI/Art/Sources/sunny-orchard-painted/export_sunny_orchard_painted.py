#!/usr/bin/env python3
"""Export approved painted masters without generating any artwork.

The script downsamples reviewed RGBA masters, normalizes action-glyph RGB to a
neutral white alpha-mask contract, validates the finite runtime contract, and
writes the ownership manifest. It intentionally contains no shape, icon,
ornament, or palette drawing code.
"""

from __future__ import annotations

import hashlib
import json
import math
import re
import time
from dataclasses import dataclass
from pathlib import Path
from statistics import median

from PIL import Image, ImageChops, ImageDraw


SET_ID = "sunny-orchard-painted"
REVISION = "1"
SOURCE_SCALE = 2.0
OPTICAL_ALPHA_THRESHOLD = 48
MICRO_ALPHA_CLEANUP_THRESHOLD = 96
ART_SET_SCRIPT_GUID = "a93ac270418f41aaac52b72f5c2a5e8c"
PROMPT_RECORD_PATH = (
    "Assets/UI/Art/Sources/sunny-orchard-painted/prompt-record.json")
IMAGEGEN_OUTPUTS = {
    "icon.control-pause": "exec-2e5b3dbb-dc58-41a6-94b8-0d7eba8ad9d6.png",
    "icon.control-continue": "exec-c19e8f10-8869-427e-a72f-206406e73573.png",
    "icon.control-start-wave": "exec-c19e8f10-8869-427e-a72f-206406e73573.png",
    "icon.control-start": "exec-c19e8f10-8869-427e-a72f-206406e73573.png",
    "icon.control-speed": "exec-e448bbe8-e34e-4f41-a954-f179ffe9e5ca.png",
    "icon.control-close": "exec-344e3cd7-8d96-42e7-8065-7eddad406700.png",
    "action.compact-control": "exec-89dc048b-4c7d-44e5-88ba-19fe1e8f94ce.png",
    "action.compact-control-active": "exec-444fa67c-1ea2-4dfd-a130-494ecaa44bae.png",
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
PLACEMENT_NORMALIZED_ACTION_GLYPH_IDS = {
    "icon.control-pause",
    "icon.control-continue",
    "icon.control-start-wave",
    "icon.control-start",
    "icon.control-speed",
    "icon.control-close",
}


@dataclass(frozen=True)
class Slot:
    index: int
    stem: str
    semantic_id: str
    geometry: str
    source_stem: str | None = None


SLOTS = (
    Slot(0, "surface-screen-background", "surface.screen-background", "stretch"),
    Slot(1, "surface-safe-area", "surface.safe-area", "nine-slice"),
    Slot(2, "surface-panel-standard", "surface.panel-standard", "nine-slice"),
    Slot(3, "surface-panel-raised", "surface.panel-raised", "nine-slice"),
    Slot(4, "surface-card-selectable", "surface.card-selectable", "nine-slice"),
    Slot(5, "surface-metric", "surface.metric", "nine-slice"),
    Slot(6, "surface-status", "surface.status", "nine-slice"),
    Slot(7, "surface-detail", "surface.detail", "nine-slice"),
    Slot(8, "surface-modal", "surface.modal", "nine-slice"),
    Slot(9, "surface-result", "surface.result", "nine-slice"),
    Slot(10, "surface-scrim", "surface.scrim", "stretch"),
    Slot(11, "action-primary", "action.primary", "nine-slice"),
    Slot(12, "action-secondary", "action.secondary", "nine-slice"),
    Slot(13, "action-quiet", "action.quiet", "nine-slice"),
    Slot(14, "action-danger", "action.danger", "nine-slice"),
    Slot(15, "slot-tool", "slot.tool", "nine-slice"),
    Slot(16, "slot-nursery", "slot.nursery", "nine-slice"),
    Slot(17, "marker-selected", "marker.selected", "icon"),
    Slot(18, "indicator-disabled", "indicator.disabled", "icon"),
    Slot(19, "indicator-loading", "indicator.loading", "icon"),
    Slot(20, "indicator-success", "indicator.success", "icon"),
    Slot(21, "indicator-warning", "indicator.warning", "icon"),
    Slot(22, "indicator-error", "indicator.error", "icon"),
    Slot(23, "indicator-drag-legal", "indicator.drag-legal", "icon"),
    Slot(24, "indicator-drag-illegal", "indicator.drag-illegal", "icon"),
    Slot(25, "indicator-merge", "indicator.merge", "icon"),
    Slot(26, "indicator-swap", "indicator.swap", "icon"),
    Slot(27, "icon-resource-sun", "icon.resource-sun", "icon"),
    Slot(28, "icon-resource-core", "icon.resource-core", "icon"),
    Slot(29, "icon-resource-wave", "icon.resource-wave", "icon"),
    Slot(30, "icon-control-pause", "icon.control-pause", "icon"),
    Slot(31, "icon-control-continue", "icon.control-continue", "icon"),
    Slot(32, "icon-control-speed", "icon.control-speed", "icon"),
    Slot(33, "icon-control-continue", "icon.control-start-wave", "icon"),
    Slot(34, "icon-control-retry", "icon.control-retry", "icon"),
    Slot(35, "icon-control-return", "icon.control-return", "icon"),
    Slot(36, "icon-control-close", "icon.control-close", "icon"),
    Slot(37, "icon-tool-pot", "icon.tool-pot", "icon"),
    Slot(38, "icon-control-continue", "icon.control-start", "icon"),
    Slot(39, "icon-control-refresh", "icon.control-refresh", "icon"),
    Slot(40, "ornament-screen-corner", "ornament.screen-corner", "icon"),
    Slot(41, "surface-section-ribbon", "surface.section-ribbon", "nine-slice"),
    Slot(42, "surface-illustration-frame", "surface.illustration-frame", "nine-slice"),
    Slot(43, "ornament-metric-divider", "ornament.metric-divider", "stretch"),
    Slot(44, "ornament-result-banner", "ornament.result-banner", "stretch"),
    Slot(45, "illustration-orchard-vista", "illustration.orchard-vista", "stretch"),
    Slot(46, "illustration-lobby-orchard-01", "illustration.lobby-orchard-01", "stretch"),
    Slot(47, "illustration-lobby-orchard-02", "illustration.lobby-orchard-02", "stretch"),
    Slot(48, "illustration-lobby-orchard-03", "illustration.lobby-orchard-03", "stretch"),
    Slot(49, "icon-resource-sun-micro", "icon.resource-sun-micro", "icon",
         "icon-resource-sun"),
    Slot(50, "icon-resource-core-micro", "icon.resource-core-micro", "icon",
         "icon-resource-core"),
    Slot(51, "icon-resource-wave-micro", "icon.resource-wave-micro", "icon",
         "icon-resource-wave"),
    Slot(52, "illustration-shell-orchard-depth", "illustration.shell-orchard-depth",
         "stretch"),
    Slot(53, "action-compact-control", "action.compact-control", "nine-slice"),
    Slot(54, "action-compact-control-active", "action.compact-control-active",
         "nine-slice"),
    Slot(55, "surface-gameplay-stage", "surface.gameplay-stage", "nine-slice"),
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


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


def target_size(slot: Slot) -> tuple[int, int]:
    if 49 <= slot.index <= 51:
        return (18, 18)
    if slot.index == 52:
        return (402, 874)
    if slot.index == 0:
        return (256, 256)
    if slot.index == 10:
        return (32, 32)
    if slot.index == 45:
        return (256, 144)
    if slot.index == 43:
        return (24, 96)
    if slot.index == 44:
        return (256, 72)
    if 46 <= slot.index <= 48:
        return (168, 108)
    size = 128 if slot.geometry == "nine-slice" else 96
    return (size, size)


def subdirectory(slot: Slot) -> str:
    if slot.index <= 16 or slot.index in (41, 42, 53, 54, 55):
        return "surfaces"
    if slot.index in (40, 43, 44):
        return "ornaments"
    if 45 <= slot.index <= 48 or slot.index == 52:
        return "illustrations"
    return "icons"


def source_stem(slot: Slot) -> str:
    return slot.source_stem or slot.stem


def pixels_per_logical_unit(slot: Slot) -> float:
    return 1.0 if 49 <= slot.index <= 51 else SOURCE_SCALE


def safe_inset(slot: Slot) -> int:
    if slot.geometry == "nine-slice":
        return 20
    if 49 <= slot.index <= 51:
        return 1
    if slot.geometry == "icon":
        return 12
    return 0


def slice_border(slot: Slot) -> int:
    if slot.index == 55:
        return 20
    return 32 if slot.geometry == "nine-slice" else 0


def neutralize_action_glyph(image: Image.Image) -> Image.Image:
    """Preserve every alpha byte while making visible RGB a pure-white mask."""
    rgba = image.convert("RGBA")
    neutral = Image.new("RGBA", rgba.size, (0, 0, 0, 0))
    neutral.putdata([
        (255, 255, 255, alpha) if alpha else (0, 0, 0, 0)
        for alpha in rgba.getchannel("A").get_flattened_data()
    ])
    return neutral


def normalize_action_glyph_master(path: Path) -> None:
    """Normalize a PNG master in place without changing canvas or alpha."""
    with Image.open(path) as source:
        rgba = source.convert("RGBA")
    alpha_before = rgba.getchannel("A").tobytes()
    normalized = neutralize_action_glyph(rgba)
    if normalized.getchannel("A").tobytes() != alpha_before:
        raise RuntimeError(f"Action-glyph alpha changed during normalization: {path}")
    save_png(normalized, path)


def mark_tintable_import_meta(meta_path: Path) -> None:
    """Record the mask contract without changing importer geometry or GUID."""
    text = meta_path.read_text(encoding="utf-8")
    marker = "render=tintable-action-glyph;neutral-rgb=FFFFFF"
    match = re.search(r"(?m)^(\s*userData:)\s*(.*)$", text)
    if match is None:
        raise RuntimeError(f"No userData field in action-glyph meta: {meta_path}")
    current = match.group(2).strip()
    if marker not in current:
        updated = current + (";" if current else "") + marker
        text = text[:match.start(2)] + updated + text[match.end(2):]
        meta_path.write_text(text, encoding="utf-8")


def validate_neutral_action_glyph(image: Image.Image, path: Path) -> None:
    for red, green, blue, alpha in image.convert("RGBA").get_flattened_data():
        expected = (255, 255, 255) if alpha else (0, 0, 0)
        if (red, green, blue) != expected:
            raise RuntimeError(
                f"Action glyph is not a clean white alpha mask: {path}")


def is_semantic_container_ink(semantic_id: str, pixel: tuple[int, int, int, int]) -> bool:
    red, green, blue, alpha = pixel
    if alpha < 192:
        return False
    if semantic_id == "action.primary":
        return green >= red * 1.2 and green >= blue * 1.5
    if semantic_id == "action.danger":
        return red >= green * 1.4 and red >= blue * 1.2
    return False


def normalize_semantic_container_master(
        image: Image.Image, semantic_id: str) -> Image.Image:
    """Re-anchor painted container ink while preserving texture deltas and alpha."""
    rgba = image.convert("RGBA")
    width, height = rgba.size
    content = rgba.crop((width // 4, height // 4, width * 3 // 4, height * 3 // 4))
    samples = [pixel for pixel in content.get_flattened_data()
               if is_semantic_container_ink(semantic_id, pixel)]
    if not samples:
        raise RuntimeError(f"No semantic container ink found for {semantic_id}")
    anchor = tuple(round(median(pixel[channel] for pixel in samples))
                   for channel in range(3))
    target = SEMANTIC_CONTAINER_TARGETS[semantic_id]
    delta = tuple(target[channel] - anchor[channel] for channel in range(3))
    normalized = []
    for pixel in rgba.get_flattened_data():
        red, green, blue, alpha = pixel
        if is_semantic_container_ink(semantic_id, pixel):
            red = min(255, max(0, red + delta[0]))
            green = min(255, max(0, green + delta[1]))
            blue = min(255, max(0, blue + delta[2]))
        normalized.append((red, green, blue, alpha))
    result = Image.new("RGBA", rgba.size)
    result.putdata(normalized)
    if result.getchannel("A").tobytes() != rgba.getchannel("A").tobytes():
        raise RuntimeError(f"Semantic container alpha changed: {semantic_id}")
    return result


def normalize_semantic_container_master_file(path: Path, semantic_id: str) -> None:
    with Image.open(path) as source:
        rgba = source.convert("RGBA")
    normalized = normalize_semantic_container_master(rgba, semantic_id)
    if normalized.tobytes() != rgba.tobytes():
        save_png(normalized, path)


def mark_semantic_container_import_meta(meta_path: Path, semantic_id: str) -> None:
    text = meta_path.read_text(encoding="utf-8")
    role = semantic_id.removeprefix("action.")
    target = SEMANTIC_CONTAINER_TARGETS[semantic_id]
    marker = (f"container={role};target-rgb={target[0]:02X}{target[1]:02X}{target[2]:02X}"
              f";content-reference=FFF6E0;minimum-contrast=4.5")
    match = re.search(r"(?m)^(\s*userData:)\s*(.*)$", text)
    if match is None:
        raise RuntimeError(f"No userData field in container meta: {meta_path}")
    current = match.group(2).strip()
    if marker not in current:
        updated = current + (";" if current else "") + marker
        text = text[:match.start(2)] + updated + text[match.end(2):]
        meta_path.write_text(text, encoding="utf-8")


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


def content_region_min_contrast(image: Image.Image, semantic_id: str) -> float:
    rgba = image.convert("RGBA")
    width, height = rgba.size
    content = rgba.crop((width // 4, height // 4, width * 3 // 4, height * 3 // 4))
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
    """Resize RGBA without letting RGB from transparent pixels bleed into edges."""
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    premultiplied = Image.merge(
        "RGBA",
        tuple(ImageChops.multiply(channel, alpha) for channel in rgba.split()[:3])
        + (alpha,),
    )
    resized = premultiplied.resize(size, Image.Resampling.LANCZOS)
    pixels = list(resized.get_flattened_data())
    unpremultiplied = []
    for red, green, blue, out_alpha in pixels:
        if out_alpha == 0:
            unpremultiplied.append((0, 0, 0, 0))
            continue
        scale = 255.0 / out_alpha
        unpremultiplied.append(
            (min(255, round(red * scale)), min(255, round(green * scale)),
             min(255, round(blue * scale)), out_alpha)
        )
    result = Image.new("RGBA", size)
    result.putdata(unpremultiplied)
    return result


def fit_alpha_content(
        image: Image.Image,
        size: tuple[int, int],
        padding: int,
        offset_adjust: tuple[int, int] = (0, 0)) -> Image.Image:
    """Tight-crop transparent art, then scale uniformly into a fixed-aspect canvas."""
    rgba = image.convert("RGBA")
    bbox = rgba.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError("Fixed-aspect ornament has no visible pixels")
    crop = rgba.crop(bbox)
    available = (size[0] - padding * 2, size[1] - padding * 2)
    scale = min(available[0] / crop.width, available[1] / crop.height)
    fitted_size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    fitted = alpha_safe_resize(crop, fitted_size)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    offset = ((size[0] - fitted.width) // 2 + offset_adjust[0],
              (size[1] - fitted.height) // 2 + offset_adjust[1])
    canvas.alpha_composite(fitted, offset)
    return canvas


def clear_low_alpha_fringe(image: Image.Image, threshold: int = 48) -> Image.Image:
    """Drop near-invisible keyed pixels so Bilinear sampling cannot reveal chroma spill."""
    rgba = image.convert("RGBA")
    cleaned = []
    for red, green, blue, alpha in rgba.get_flattened_data():
        cleaned.append((0, 0, 0, 0) if alpha < threshold else (red, green, blue, alpha))
    rgba.putdata(cleaned)
    return rgba


def significant_alpha_bbox(
        image: Image.Image,
        threshold: int = OPTICAL_ALPHA_THRESHOLD) -> tuple[int, int, int, int] | None:
    """Return the half-open bbox of pixels that are visibly owned by the asset."""
    significant = image.convert("RGBA").getchannel("A").point(
        lambda alpha: 255 if alpha >= threshold else 0)
    return significant.getbbox()


def optical_inset(image: Image.Image) -> dict[str, int]:
    """Measure transparent padding from the final runtime PNG, never its master."""
    rgba = image.convert("RGBA")
    bbox = significant_alpha_bbox(rgba)
    if bbox is None:
        raise RuntimeError("Runtime artwork has no alpha >= optical threshold")
    return {
        "left": bbox[0],
        "top": bbox[1],
        "right": rgba.width - bbox[2],
        "bottom": rgba.height - bbox[3],
    }


def normalize_action_surface(image: Image.Image) -> Image.Image:
    """Normalize reviewed action ink to a shared 120 px box on a 128 px canvas."""
    rgba = image.convert("RGBA")
    bbox = significant_alpha_bbox(rgba)
    if bbox is None:
        raise RuntimeError("Action master has no alpha >= optical threshold")
    crop = rgba.crop(bbox)
    fitted = clear_low_alpha_fringe(alpha_safe_resize(crop, (120, 120)))
    canvas = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    canvas.alpha_composite(fitted, (4, 4))
    normalized_bbox = significant_alpha_bbox(canvas)
    if normalized_bbox != (4, 4, 124, 124):
        raise RuntimeError(
            f"Normalized action bbox must be [4,4,124,124), got {normalized_bbox}")
    return canvas


def normalize_micro_icon(image: Image.Image) -> Image.Image:
    """Build one final-size 18px icon with a 1px protected edge."""
    rgba = clear_low_alpha_fringe(image.convert("RGBA"))
    bbox = significant_alpha_bbox(rgba)
    if bbox is None:
        raise RuntimeError("Micro icon master has no significant alpha")
    crop = rgba.crop(bbox)
    scale = min(16.0 / crop.width, 16.0 / crop.height)
    fitted_size = (max(1, round(crop.width * scale)),
                   max(1, round(crop.height * scale)))
    fitted = clear_low_alpha_fringe(
        alpha_safe_resize(crop, fitted_size), MICRO_ALPHA_CLEANUP_THRESHOLD)
    canvas = Image.new("RGBA", (18, 18), (0, 0, 0, 0))
    offset = ((18 - fitted.width) // 2, (18 - fitted.height) // 2)
    canvas.alpha_composite(fitted, offset)
    return canvas


def cover_crop(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Center-crop an opaque master to the declared portrait aspect, then resize."""
    rgba = image.convert("RGBA")
    target_ratio = size[0] / size[1]
    source_ratio = rgba.width / rgba.height
    if source_ratio > target_ratio:
        crop_width = max(1, round(rgba.height * target_ratio))
        left = (rgba.width - crop_width) // 2
        crop = rgba.crop((left, 0, left + crop_width, rgba.height))
    else:
        crop_height = max(1, round(rgba.width / target_ratio))
        top = (rgba.height - crop_height) // 2
        crop = rgba.crop((0, top, rgba.width, top + crop_height))
    return alpha_safe_resize(crop, size)


def silhouette_iou(first: Image.Image, second: Image.Image) -> float:
    first_mask = first.convert("RGBA").getchannel("A").point(
        lambda alpha: 255 if alpha >= OPTICAL_ALPHA_THRESHOLD else 0)
    second_mask = second.convert("RGBA").getchannel("A").point(
        lambda alpha: 255 if alpha >= OPTICAL_ALPHA_THRESHOLD else 0)
    intersection = ImageChops.multiply(first_mask, second_mask)
    union = ImageChops.lighter(first_mask, second_mask)
    intersection_count = sum(1 for value in intersection.get_flattened_data() if value)
    union_count = sum(1 for value in union.get_flattened_data() if value)
    return intersection_count / union_count if union_count else 1.0


def clear_visible_key_magenta(image: Image.Image) -> Image.Image:
    """Remove only leaked background-key pixels; preserve ordinary painted antialiasing."""
    rgba = image.convert("RGBA")
    cleaned = []
    for red, green, blue, alpha in rgba.get_flattened_data():
        if alpha > 0 and (red, green, blue) == (255, 0, 255):
            cleaned.append((0, 0, 0, 0))
        else:
            cleaned.append((red, green, blue, alpha))
    rgba.putdata(cleaned)
    return rgba


def clear_icon_safe_edge(image: Image.Image, safe_inset: int = 12) -> Image.Image:
    """Keep resampling ringing inside the finite 96 px icon safe box."""
    rgba = image.convert("RGBA")
    width, height = rgba.size
    rgba.paste((0, 0, 0, 0), (0, 0, safe_inset, height))
    rgba.paste((0, 0, 0, 0), (width - safe_inset, 0, width, height))
    rgba.paste((0, 0, 0, 0), (0, 0, width, safe_inset))
    rgba.paste((0, 0, 0, 0), (0, height - safe_inset, width, height))
    return rgba


def unity_guid(meta_path: Path) -> str:
    match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", meta_path.read_text())
    if match is None:
        raise RuntimeError(f"No Unity GUID in {meta_path}")
    return match.group(1)


def normalize_target_import_meta(
        slot: Slot, runtime_path: Path, runtime_root: Path) -> None:
    """Give newly introduced target-tier PNGs the established Sprite importer contract.

    The production set already owns a reviewed importer template. Reusing it keeps
    this exporter focused on sizing and metadata, while preserving each new PNG's
    Unity GUID across repeated exports.
    """
    if not 49 <= slot.index <= 52:
        return
    meta_path = runtime_path.with_suffix(".png.meta")
    if not meta_path.exists():
        raise RuntimeError(
            f"Import {runtime_path} once to allocate its stable Unity GUID")
    template_path = runtime_root / "icons" / "icon-resource-sun.png.meta"
    template = template_path.read_text(encoding="utf-8")
    guid = unity_guid(meta_path)
    maximum_size = 18 if slot.index <= 51 else 1024
    sprite_id = hashlib.sha256(
        f"{SET_ID}:{slot.semantic_id}:sprite".encode("utf-8")).hexdigest()[:32]
    normalized = re.sub(
        r"(?m)^guid:\s*[0-9a-f]{32}\s*$", f"guid: {guid}", template)
    normalized = re.sub(
        r"(?m)^(\s*maxTextureSize:)\s*\d+\s*$",
        rf"\g<1> {maximum_size}", normalized)
    normalized = re.sub(
        r"(?m)^(\s*spriteID:)\s*[0-9a-f]*\s*$",
        rf"\g<1> {sprite_id}", normalized)
    normalized = re.sub(
        r"(?m)^\s*userData:.*$",
        "  userData: ui-art-set=" + SET_ID + ";slot=" + slot.semantic_id
        + ";target-size=" + str(target_size(slot)[0]) + "x"
        + str(target_size(slot)[1]), normalized)
    meta_path.write_text(normalized, encoding="utf-8")


def stable_guid(scope: str, semantic_id: str) -> str:
    return hashlib.md5(
        f"{SET_ID}:{scope}:{semantic_id}".encode("utf-8")).hexdigest()


def ensure_generated_import_meta(
        slot: Slot, source_root: Path, runtime_root: Path) -> None:
    """Create stable source/runtime importer metadata for newly authored slots."""
    if slot.index not in (53, 54, 55):
        return
    folder = subdirectory(slot)
    source_path = source_root / folder / f"{source_stem(slot)}.png"
    runtime_path = runtime_root / folder / f"{slot.stem}.png"
    if not source_path.is_file():
        raise RuntimeError(f"Missing imagegen master: {source_path}")

    source_meta = source_path.with_suffix(".png.meta")
    if not source_meta.exists():
        source_template = source_root / "surfaces/action-quiet.png.meta"
        text = source_template.read_text(encoding="utf-8")
        text = re.sub(r"(?m)^guid:\s*[0-9a-f]{32}\s*$",
                      "guid: " + stable_guid("source", slot.semantic_id), text)
        text = re.sub(r"(?m)^(\s*maxTextureSize:)\s*\d+\s*$",
                      r"\g<1> 2048", text)
        provenance = ("generated=built-in-imagegen" if slot.index in (53, 54)
                      else "authored=deterministic-vector")
        text = re.sub(
            r"(?m)^\s*userData:.*$",
            "  userData: ui-art-source=" + SET_ID + ";slot=" + slot.semantic_id
            + ";" + provenance, text)
        source_meta.write_text(text, encoding="utf-8")

    runtime_meta = runtime_path.with_suffix(".png.meta")
    if not runtime_meta.exists():
        runtime_template = runtime_root / "surfaces/action-quiet.png.meta"
        text = runtime_template.read_text(encoding="utf-8")
        text = re.sub(r"(?m)^guid:\s*[0-9a-f]{32}\s*$",
                      "guid: " + stable_guid("runtime", slot.semantic_id), text)
        text = re.sub(r"(?m)^(\s*maxTextureSize:)\s*\d+\s*$",
                      r"\g<1> " + str(max(target_size(slot))), text)
        sprite_id = hashlib.sha256(
            f"{SET_ID}:{slot.semantic_id}:sprite".encode("utf-8")).hexdigest()[:32]
        text = re.sub(r"(?m)^(\s*spriteID:)\s*[0-9a-f]*\s*$",
                      r"\g<1> " + sprite_id, text)
        border = slice_border(slot)
        text = re.sub(
            r"(?m)^\s*spriteBorder:.*$",
            "  spriteBorder: {x: " + str(border) + ", y: " + str(border)
            + ", z: " + str(border) + ", w: " + str(border) + "}", text)
        provenance = ("generated=built-in-imagegen" if slot.index in (53, 54)
                      else "authored=deterministic-vector")
        text = re.sub(
            r"(?m)^\s*userData:.*$",
            "  userData: ui-art-set=" + SET_ID + ";slot=" + slot.semantic_id
            + ";source-scale=2;" + provenance, text)
        runtime_meta.write_text(text, encoding="utf-8")


def alpha_bbox(image: Image.Image) -> tuple[int, int, int, int] | None:
    return image.getchannel("A").getbbox()


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
        "  m_Name: SunnyOrchardPaintedRuntimeUiArtSet",
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


def export_unique(slot: Slot, source_root: Path, runtime_root: Path) -> None:
    folder = subdirectory(slot)
    source_path = source_root / folder / f"{source_stem(slot)}.png"
    runtime_path = runtime_root / folder / f"{slot.stem}.png"
    if not source_path.is_file():
        raise RuntimeError(f"Missing approved master: {source_path}")
    if not runtime_path.with_suffix(".png.meta").is_file():
        raise RuntimeError(f"Missing stable runtime meta: {runtime_path}.meta")

    if slot.semantic_id in ACTION_GLYPH_IDS:
        normalize_action_glyph_master(source_path)
        mark_tintable_import_meta(source_path.with_suffix(".png.meta"))
        mark_tintable_import_meta(runtime_path.with_suffix(".png.meta"))
    if slot.semantic_id in SEMANTIC_CONTAINER_TARGETS:
        normalize_semantic_container_master_file(source_path, slot.semantic_id)
        mark_semantic_container_import_meta(
            source_path.with_suffix(".png.meta"), slot.semantic_id)
        mark_semantic_container_import_meta(
            runtime_path.with_suffix(".png.meta"), slot.semantic_id)

    with Image.open(source_path) as source:
        rgba = source.convert("RGBA")
        size = target_size(slot)
        if 11 <= slot.index <= 14 or slot.index in (53, 54):
            exported = normalize_action_surface(rgba)
        elif 49 <= slot.index <= 51:
            exported = normalize_micro_icon(rgba)
        elif slot.index == 52:
            exported = cover_crop(rgba, size)
        elif slot.semantic_id in PLACEMENT_NORMALIZED_ACTION_GLYPH_IDS:
            optical_x = (7 if slot.semantic_id in {
                "icon.control-continue", "icon.control-start-wave", "icon.control-start"
            } else 4 if slot.semantic_id == "icon.control-speed" else 0)
            exported = fit_alpha_content(
                clear_low_alpha_fringe(rgba), size, 17, (optical_x, 0))
        elif slot.index in (43, 44):
            exported = fit_alpha_content(rgba, size, 4 if slot.index == 43 else 8)
        else:
            exported = (alpha_safe_resize(rgba, size)
                        if slot.index >= 40
                        else rgba.resize(size, Image.Resampling.LANCZOS))
        if slot.index >= 40 and not (49 <= slot.index <= 51):
            exported = clear_low_alpha_fringe(exported)
        if slot.geometry == "icon" and not (49 <= slot.index <= 51):
            bbox = alpha_bbox(exported)
            if bbox is None:
                raise RuntimeError(f"Icon has no visible pixels: {source_path}")
            width = bbox[2] - bbox[0]
            height = bbox[3] - bbox[1]
            if width > 70 or height > 70:
                # Scale the complete reviewed canvas, rather than cropping and
                # geometrically recentering the symbol. This preserves the
                # artist-reviewed optical offset while leaving one pixel of
                # resampling headroom inside the 12 px runtime safe inset.
                scale = min(70.0 / width, 70.0 / height)
                reduced_size = max(1, round(size[0] * scale))
                reduced = exported.resize(
                    (reduced_size, reduced_size), Image.Resampling.LANCZOS)
                safe_canvas = Image.new("RGBA", size, (0, 0, 0, 0))
                offset = ((size[0] - reduced_size) // 2,
                          (size[1] - reduced_size) // 2)
                safe_canvas.alpha_composite(reduced, offset)
                exported = safe_canvas
        # A second pass is required after the optional icon safe-box resize,
        # whose Lanczos ringing can otherwise recreate an alpha-1 key pixel.
        post_resize_bbox = (alpha_bbox(exported)
                            if slot.geometry == "icon"
                            and slot.index < 49
                            else None)
        if (post_resize_bbox is not None
                and (post_resize_bbox[0] < 12 or post_resize_bbox[1] < 12
                     or post_resize_bbox[2] > 84 or post_resize_bbox[3] > 84)):
            exported = clear_icon_safe_edge(exported)
        exported = clear_visible_key_magenta(exported)
        if slot.semantic_id in ACTION_GLYPH_IDS:
            exported = neutralize_action_glyph(exported)
            validate_neutral_action_glyph(exported, runtime_path)
        runtime_path.parent.mkdir(parents=True, exist_ok=True)
        save_png(exported, runtime_path)

    with Image.open(runtime_path) as runtime:
        rgba = runtime.convert("RGBA")
        if rgba.size != target_size(slot):
            raise RuntimeError(f"Unexpected runtime size: {runtime_path} {rgba.size}")
        if slot.index == 0 and rgba.getchannel("A").getextrema() != (255, 255):
            raise RuntimeError("Screen background must be fully opaque")
        if slot.index == 10 and rgba.getpixel((0, 0)) != (255, 255, 255, 255):
            raise RuntimeError("Scrim must remain neutral opaque white")
        if slot.geometry == "icon" and slot.index < 49:
            bbox = alpha_bbox(rgba)
            inset = 12
            if (bbox is None or bbox[0] < inset or bbox[1] < inset
                    or bbox[2] > 96 - inset or bbox[3] > 96 - inset):
                raise RuntimeError(
                    f"Icon exceeds {inset} px safe inset: {runtime_path} {bbox}")
        if 49 <= slot.index <= 51:
            bbox = significant_alpha_bbox(rgba)
            if bbox is None or bbox[0] < 1 or bbox[1] < 1 or bbox[2] > 17 or bbox[3] > 17:
                raise RuntimeError(f"Micro icon exceeds 1 px edge: {runtime_path} {bbox}")
            major = max(bbox[2] - bbox[0], bbox[3] - bbox[1])
            if major < 15:
                raise RuntimeError(f"Micro icon under-fills target canvas: {runtime_path} {bbox}")
        if slot.index == 52 and rgba.getchannel("A").getextrema() != (255, 255):
            raise RuntimeError("Shell orchard depth illustration must be fully opaque")
        if slot.index == 55:
            bbox = significant_alpha_bbox(rgba)
            if bbox != (8, 8, 116, 120):
                raise RuntimeError(
                    f"Gameplay stage bbox must be [8,8,116,120): {runtime_path} {bbox}")
            if rgba.getpixel((64, 64))[3] != 0 or rgba.getpixel((0, 0))[3] != 0:
                raise RuntimeError(
                    "Gameplay stage must keep its center and outer corners transparent")
        if 11 <= slot.index <= 14 or slot.index in (53, 54):
            bbox = significant_alpha_bbox(rgba)
            if bbox != (4, 4, 124, 124):
                raise RuntimeError(
                    f"Action bbox must be [4,4,124,124): {runtime_path} {bbox}")
        if any(alpha > 0 and (red, green, blue) == (255, 0, 255)
               for red, green, blue, alpha in rgba.get_flattened_data()):
            raise RuntimeError(f"Visible key-magenta fringe: {runtime_path}")
        if slot.semantic_id in ACTION_GLYPH_IDS:
            validate_neutral_action_glyph(rgba, runtime_path)
        if slot.semantic_id in SEMANTIC_CONTAINER_TARGETS:
            content_region_min_contrast(rgba, slot.semantic_id)


def main() -> None:
    source_root = Path(__file__).resolve().parent
    project_root = source_root.parents[4]
    runtime_root = project_root / "Assets/UI/Art/Runtime" / SET_ID

    unique: dict[str, Slot] = {}
    for slot in SLOTS:
        unique.setdefault(slot.stem, slot)
    for slot in unique.values():
        ensure_generated_import_meta(slot, source_root, runtime_root)
        export_unique(slot, source_root, runtime_root)
        runtime_path = runtime_root / subdirectory(slot) / f"{slot.stem}.png"
        normalize_target_import_meta(slot, runtime_path, runtime_root)

    bindings = []
    for slot in SLOTS:
        folder = subdirectory(slot)
        source_path = source_root / folder / f"{source_stem(slot)}.png"
        runtime_path = runtime_root / folder / f"{slot.stem}.png"
        with Image.open(runtime_path) as runtime:
            measured_optical_inset = optical_inset(runtime)
        item = {
                "stem": slot.stem,
                "semantic_id": slot.semantic_id,
                "geometry": slot.geometry,
                "size": target_size(slot)[0] if target_size(slot)[0] == target_size(slot)[1] else 0,
                "width": target_size(slot)[0],
                "height": target_size(slot)[1],
                "source": source_path.relative_to(project_root).as_posix(),
                "runtime": runtime_path.relative_to(project_root).as_posix(),
                "sourceSha256": sha256(source_path),
                "runtimeSha256": sha256(runtime_path),
                "guid": unity_guid(runtime_path.with_suffix(".png.meta")),
                "slice_border": slice_border(slot),
                "safe_inset": safe_inset(slot),
                "optical_inset": measured_optical_inset,
                "pixels_per_logical_unit": pixels_per_logical_unit(slot),
                "slot": slot.index,
            }
        if slot.semantic_id in ACTION_GLYPH_IDS:
            item.update({
                "render_contract": "tintable-action-glyph",
                "neutral_rgb": "FFFFFF",
            })
        if slot.semantic_id in SEMANTIC_CONTAINER_TARGETS:
            with Image.open(runtime_path) as runtime:
                minimum_contrast = content_region_min_contrast(
                    runtime, slot.semantic_id)
            target = SEMANTIC_CONTAINER_TARGETS[slot.semantic_id]
            item.update({
                "container_contract": "semantic-action-container",
                "target_rgb": f"{target[0]:02X}{target[1]:02X}{target[2]:02X}",
                "content_reference_rgb": "FFF6E0",
                "content_region_min_contrast": round(minimum_contrast, 4),
            })
        if slot.semantic_id in IMAGEGEN_OUTPUTS:
            item.update({
                "imagegen_provider": "built-in-imagegen",
                "imagegen_output": IMAGEGEN_OUTPUTS[slot.semantic_id],
                "prompt_record": PROMPT_RECORD_PATH,
            })
        bindings.append(item)

    micro_images = []
    for slot in SLOTS[49:52]:
        with Image.open(runtime_root / "icons" / f"{slot.stem}.png") as image:
            micro_images.append((slot.semantic_id, image.convert("RGBA")))
    for first_index in range(len(micro_images)):
        for second_index in range(first_index + 1, len(micro_images)):
            first_id, first = micro_images[first_index]
            second_id, second = micro_images[second_index]
            overlap = silhouette_iou(first, second)
            if overlap >= 0.80:
                raise RuntimeError(
                    f"Micro silhouettes are confusable: {first_id} vs {second_id} IoU={overlap:.3f}")

    manifest = {
        "schema": "fruit-defense.runtime-ui-art-manifest.v2",
        "setId": SET_ID,
        "revision": REVISION,
        "approvedDirection": "Sunny Orchard Painted v3 target-size hierarchy",
        "sourceScale": SOURCE_SCALE,
        "slotCount": len(SLOTS),
        "uniqueExportCount": len(unique),
        "sharedBindings": {
            "icon.control-continue": [
                "icon.control-continue",
                "icon.control-start-wave",
                "icon.control-start",
            ]
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
    manifest_path = source_root / "art_manifest.json"
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    art_set_path = project_root / "Assets/UI/Art/Sets/SunnyOrchardPaintedRuntimeUiArtSet.asset"
    art_set_path.write_text(build_art_set_asset(bindings), encoding="utf-8")
    build_gallery(source_root, runtime_root)
    print(f"Exported {len(unique)} PNGs and {len(SLOTS)} bindings to {manifest_path}")


def build_gallery(source_root: Path, runtime_root: Path) -> None:
    """Compose reviewed exports only; this does not create or alter asset art."""
    review_root = source_root.parent / "ReferenceBoards" / "Review"
    review_root.mkdir(parents=True, exist_ok=True)
    unique: dict[str, Slot] = {}
    for slot in SLOTS:
        unique.setdefault(slot.stem, slot)
    gallery_slots = tuple(unique.values())
    columns = 5
    rows = math.ceil(len(gallery_slots) / columns)
    cell_width, cell_height = 240, 190
    gallery = Image.new("RGBA", (cell_width * columns, cell_height * rows), (242, 232, 213, 255))
    draw = ImageDraw.Draw(gallery)
    for offset, slot in enumerate(gallery_slots):
        path = runtime_root / subdirectory(slot) / f"{slot.stem}.png"
        with Image.open(path) as image:
            tile = image.convert("RGBA")
        max_width, max_height = 184, 132
        scale = min(max_width / tile.width, max_height / tile.height)
        display_size = (max(1, round(tile.width * scale)), max(1, round(tile.height * scale)))
        display = alpha_safe_resize(tile, display_size)
        column, row = offset % columns, offset // columns
        cell_x, cell_y = column * cell_width, row * cell_height
        draw.rectangle((cell_x + 4, cell_y + 4, cell_x + cell_width - 4, cell_y + cell_height - 4),
                       fill=(255, 246, 224, 255), outline=(139, 94, 60, 96), width=1)
        x = cell_x + (cell_width - display.width) // 2
        y = cell_y + 10 + (max_height - display.height) // 2
        checker = Image.new("RGBA", display.size, (248, 238, 219, 255))
        checker_draw = ImageDraw.Draw(checker)
        checker_size = 10
        for checker_y in range(0, display.height, checker_size):
            for checker_x in range(0, display.width, checker_size):
                if (checker_x // checker_size + checker_y // checker_size) % 2:
                    checker_draw.rectangle((checker_x, checker_y,
                                            checker_x + checker_size - 1,
                                            checker_y + checker_size - 1),
                                           fill=(215, 197, 170, 255))
        checker.alpha_composite(display)
        gallery.alpha_composite(checker, (x, y))
        draw.text((cell_x + 10, cell_y + 150),
                  f"{slot.index:02d} {slot.semantic_id}", fill=(75, 50, 30, 255))
        draw.text((cell_x + 10, cell_y + 166),
                  f"{slot.geometry} {tile.width}x{tile.height}", fill=(107, 81, 55, 255))
        draw.rectangle((cell_x, cell_y,
                        (column + 1) * cell_width - 1, (row + 1) * cell_height - 1),
                       outline=(139, 94, 60, 96), width=1)
    save_png(gallery, review_root / "sunny-orchard-painted-56-gallery.png")


if __name__ == "__main__":
    main()

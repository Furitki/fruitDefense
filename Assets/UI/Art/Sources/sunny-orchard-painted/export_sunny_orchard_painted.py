#!/usr/bin/env python3
"""Export approved painted masters without generating any artwork.

The script only downsamples reviewed RGBA masters, validates the finite runtime
contract, and writes the ownership manifest. It intentionally contains no
shape, icon, ornament, or palette drawing code.
"""

from __future__ import annotations

import hashlib
import json
import math
import re
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


SET_ID = "sunny-orchard-painted"
REVISION = "1"
SOURCE_SCALE = 2.0
OPTICAL_ALPHA_THRESHOLD = 48
ART_SET_SCRIPT_GUID = "a93ac270418f41aaac52b72f5c2a5e8c"


@dataclass(frozen=True)
class Slot:
    index: int
    stem: str
    semantic_id: str
    geometry: str


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
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def target_size(slot: Slot) -> tuple[int, int]:
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
    if slot.index <= 16 or slot.index in (41, 42):
        return "surfaces"
    if slot.index in (40, 43, 44):
        return "ornaments"
    if slot.index >= 45:
        return "illustrations"
    return "icons"


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


def fit_alpha_content(image: Image.Image, size: tuple[int, int], padding: int) -> Image.Image:
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
    offset = ((size[0] - fitted.width) // 2, (size[1] - fitted.height) // 2)
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
            f"    pixelsPerLogicalUnit: {SOURCE_SCALE:g}",
        ])
    return "\n".join(lines) + "\n"


def export_unique(slot: Slot, source_root: Path, runtime_root: Path) -> None:
    folder = subdirectory(slot)
    source_path = source_root / folder / f"{slot.stem}.png"
    runtime_path = runtime_root / folder / f"{slot.stem}.png"
    if not source_path.is_file():
        raise RuntimeError(f"Missing approved master: {source_path}")
    if not runtime_path.with_suffix(".png.meta").is_file():
        raise RuntimeError(f"Missing stable runtime meta: {runtime_path}.meta")

    with Image.open(source_path) as source:
        rgba = source.convert("RGBA")
        size = target_size(slot)
        if 11 <= slot.index <= 14:
            exported = normalize_action_surface(rgba)
        elif slot.index in (43, 44):
            exported = fit_alpha_content(rgba, size, 4 if slot.index == 43 else 8)
        else:
            exported = (alpha_safe_resize(rgba, size)
                        if slot.index >= 40
                        else rgba.resize(size, Image.Resampling.LANCZOS))
        if slot.index >= 40:
            exported = clear_low_alpha_fringe(exported)
        if slot.geometry == "icon":
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
        post_resize_bbox = alpha_bbox(exported) if slot.geometry == "icon" else None
        if (post_resize_bbox is not None
                and (post_resize_bbox[0] < 12 or post_resize_bbox[1] < 12
                     or post_resize_bbox[2] > 84 or post_resize_bbox[3] > 84)):
            exported = clear_icon_safe_edge(exported)
        exported = clear_visible_key_magenta(exported)
        runtime_path.parent.mkdir(parents=True, exist_ok=True)
        exported.save(runtime_path, format="PNG", optimize=True, compress_level=9)

    with Image.open(runtime_path) as runtime:
        rgba = runtime.convert("RGBA")
        if rgba.size != target_size(slot):
            raise RuntimeError(f"Unexpected runtime size: {runtime_path} {rgba.size}")
        if slot.index == 0 and rgba.getchannel("A").getextrema() != (255, 255):
            raise RuntimeError("Screen background must be fully opaque")
        if slot.index == 10 and rgba.getpixel((0, 0)) != (255, 255, 255, 255):
            raise RuntimeError("Scrim must remain neutral opaque white")
        if slot.geometry == "icon":
            bbox = alpha_bbox(rgba)
            if bbox is None or bbox[0] < 12 or bbox[1] < 12 or bbox[2] > 84 or bbox[3] > 84:
                raise RuntimeError(f"Icon exceeds 12 px safe inset: {runtime_path} {bbox}")
        if 11 <= slot.index <= 14:
            bbox = significant_alpha_bbox(rgba)
            if bbox != (4, 4, 124, 124):
                raise RuntimeError(
                    f"Action bbox must be [4,4,124,124): {runtime_path} {bbox}")
        if any(alpha > 0 and (red, green, blue) == (255, 0, 255)
               for red, green, blue, alpha in rgba.get_flattened_data()):
            raise RuntimeError(f"Visible key-magenta fringe: {runtime_path}")


def main() -> None:
    source_root = Path(__file__).resolve().parent
    project_root = source_root.parents[4]
    runtime_root = project_root / "Assets/UI/Art/Runtime" / SET_ID

    unique: dict[str, Slot] = {}
    for slot in SLOTS:
        unique.setdefault(slot.stem, slot)
    for slot in unique.values():
        export_unique(slot, source_root, runtime_root)

    bindings = []
    for slot in SLOTS:
        folder = subdirectory(slot)
        source_path = source_root / folder / f"{slot.stem}.png"
        runtime_path = runtime_root / folder / f"{slot.stem}.png"
        with Image.open(runtime_path) as runtime:
            measured_optical_inset = optical_inset(runtime)
        bindings.append(
            {
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
                "slice_border": 32 if slot.geometry == "nine-slice" else 0,
                "safe_inset": 20 if slot.geometry == "nine-slice" else (12 if slot.geometry == "icon" else 0),
                "optical_inset": measured_optical_inset,
                "pixels_per_logical_unit": SOURCE_SCALE,
                "slot": slot.index,
            }
        )

    manifest = {
        "schema": "fruit-defense.runtime-ui-art-manifest.v2",
        "setId": SET_ID,
        "revision": REVISION,
        "approvedDirection": "Sunny Orchard Painted v2",
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
    gallery.save(review_root / "sunny-orchard-painted-49-gallery.png",
                 format="PNG", optimize=True, compress_level=9)


if __name__ == "__main__":
    main()

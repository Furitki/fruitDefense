#!/usr/bin/env python3
"""Extract and export the finite Sunny Orchard Painted production set.

Selected action and structural materials enter the pipeline as individual,
hash-locked ImageGen masters. Remaining owned masters, retained reviewed icons,
ornaments and illustrations pass through the same alpha-safe export and
ownership pipeline.
"""

from __future__ import annotations

import hashlib
import json
import re
import time
from collections import deque
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


SET_ID = "sunny-orchard-painted"
REVISION = "9"
SOURCE_SCALE = 2.0
OPTICAL_ALPHA_THRESHOLD = 48
MICRO_ALPHA_CLEANUP_THRESHOLD = 96
ART_SET_SCRIPT_GUID = "a93ac270418f41aaac52b72f5c2a5e8c"
PROMPT_RECORD_PATH = (
    "Assets/UI/Art/Sources/sunny-orchard-painted/prompt-record.json")
IMAGEGEN_OUTPUTS = {
    "action.primary": "exec-ab169a87-8f13-4c41-87e7-8307a82512d1.png",
    "action.secondary": "exec-ab169a87-8f13-4c41-87e7-8307a82512d1.png",
    "action.quiet": "exec-4f60d853-d710-471a-a549-8c953c5b5a8a.png",
    "action.danger": "exec-4f60d853-d710-471a-a549-8c953c5b5a8a.png",
    "action.compact-control": "exec-dc14d5db-4629-4cb2-9b3c-357061c5bf4f.png",
    "action.compact-control-active": "exec-dc14d5db-4629-4cb2-9b3c-357061c5bf4f.png",
    "surface.safe-area": "exec-6f239a99-081d-4bff-a41c-6abb17dffeb1.png",
    "surface.panel-standard": "exec-6f239a99-081d-4bff-a41c-6abb17dffeb1.png",
    "surface.panel-raised": "exec-6f239a99-081d-4bff-a41c-6abb17dffeb1.png",
    "surface.metric": "exec-69f7835e-0e63-4461-9bb8-4260eb8f3961.png",
    "surface.card-selectable": "exec-96e27751-69a6-4b1f-b63e-7faeb5c58472.png",
    "slot.tool": "exec-96e27751-69a6-4b1f-b63e-7faeb5c58472.png",
    "slot.nursery": "exec-e30c0381-7420-4cad-9d29-b51464b8339f.png",
    "surface.gameplay-stage": "exec-26bcbc75-0425-4308-91bb-6296e8465e12.png",
    "icon.control-pause": "exec-2e5b3dbb-dc58-41a6-94b8-0d7eba8ad9d6.png",
    "icon.control-continue": "exec-c19e8f10-8869-427e-a72f-206406e73573.png",
    "icon.control-start-wave": "exec-c19e8f10-8869-427e-a72f-206406e73573.png",
    "icon.control-start": "exec-c19e8f10-8869-427e-a72f-206406e73573.png",
    "icon.control-speed": "exec-e448bbe8-e34e-4f41-a954-f179ffe9e5ca.png",
    "icon.control-close": "exec-344e3cd7-8d96-42e7-8065-7eddad406700.png",
}
IMAGEGEN_DIRECT_ROOT = (
    "openspec/changes/polish-sky-paper-ui-eight-point/evidence/"
    "direct-replacement-v2/imagegen")
IMAGEGEN_GEOMETRY_MASKS = {
    "action-green.png": (
        "action-geometry-mask.png",
        "EC721B4B64BD33EE3782AACAA064EA7679126F0BB8D61454ED834838BCDD7964"),
}
IMAGEGEN_CONNECTED_BACKGROUND_ASSETS = frozenset({
    "metric-capsule.png",
    "slot-nursery.png",
})
LINE_FREE_CARRIER_IDS = frozenset({"surface.metric", "slot.nursery"})
IMAGEGEN_DIRECT_ASSETS = (
    ("action.primary", "action-primary", "action-green.png",
     "99D2B2401474619491065EC405F73FC2BFF145638BA73F866E6132B8D6DFC5DA"),
    ("action.secondary", "action-secondary", "action-green.png",
     "99D2B2401474619491065EC405F73FC2BFF145638BA73F866E6132B8D6DFC5DA"),
    ("action.quiet", "action-quiet", "action-quiet-retained.png",
     "95212C90A09454C7A9824D5731CF9A162685DF603F3C7F97B798CF99E19C2DF6"),
    ("action.danger", "action-danger", "action-danger-retained.png",
     "BACC2C3610F0693ABF9CFE3755B4C77C24CA4DAB20AB900FF99B6939585AF2CA"),
    ("action.compact-control", "action-compact-control", "action-yellow.png",
     "BE9E3F520F570CDD44A11950515A023EB1E785309F5EC81F432394DB8395C094"),
    ("action.compact-control-active", "action-compact-control-active",
     "action-yellow.png",
     "BE9E3F520F570CDD44A11950515A023EB1E785309F5EC81F432394DB8395C094"),
    ("surface.safe-area", "surface-safe-area", "panel-paper.png",
     "475B96F6E034081DC23B68D6D7EB27F5729C5E87421301E43B61703A5D733F0F"),
    ("surface.panel-standard", "surface-panel-standard", "panel-paper.png",
     "475B96F6E034081DC23B68D6D7EB27F5729C5E87421301E43B61703A5D733F0F"),
    ("surface.panel-raised", "surface-panel-raised", "panel-paper.png",
     "475B96F6E034081DC23B68D6D7EB27F5729C5E87421301E43B61703A5D733F0F"),
    ("surface.metric", "surface-metric", "metric-capsule.png",
     "5539902850BA5EA59A20B356B34A7539516108D349B95F3E458AA2EA4590DC51"),
    ("surface.card-selectable", "surface-card-selectable", "card-lime.png",
     "C7809FFB49687308F74ABD2F0497BD251EE967E1D91EF23B87E1F6729AE35C87"),
    ("slot.tool", "slot-tool", "card-lime.png",
     "C7809FFB49687308F74ABD2F0497BD251EE967E1D91EF23B87E1F6729AE35C87"),
    ("slot.nursery", "slot-nursery", "slot-nursery.png",
     "335028CADB393454E90B8D0D66DC27B3F39C2839D13C12084B3BB4204131ABFE"),
    ("surface.gameplay-stage", "surface-gameplay-stage", "stage-frame.png",
     "DB106DA52028C9128DD56FD76D6FC7084DFDDC639634AF3C3D359537C0EF9750"),
)
IMAGEGEN_MATERIAL_IDS = frozenset(row[0] for row in IMAGEGEN_DIRECT_ASSETS)

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
    "action.primary": (0xA0, 0xC7, 0x3D),
    "action.secondary": (0xA0, 0xC7, 0x3D),
    "action.danger": (0xC8, 0x14, 0x09),
}
CONTENT_REFERENCE_RGB = {
    "action.primary": (0x56, 0x34, 0x1F),
    "action.secondary": (0x56, 0x34, 0x1F),
    "action.danger": (0xFF, 0xF9, 0xEE),
}
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


@dataclass(frozen=True)
class ReferenceMaterialStyle:
    recipe: str
    face: tuple[int, int, int, int]
    outline: tuple[int, int, int, int] = (0x6D, 0x48, 0x28, 255)
    rim: tuple[int, int, int, int] = (0xFF, 0xF4, 0xDB, 255)
    highlight: tuple[int, int, int, int] = (0xFF, 0xFF, 0xF8, 255)
    shadow: tuple[int, int, int, int] = (0x78, 0x49, 0x28, 96)
    transparent_center_inset: int = 0


REFERENCE_MATERIAL_ANATOMY = (
    "outer-cream-rim|face|soil-outline|upper-highlight|short-bottom-shadow")
LINE_FREE_CARRIER_MATERIAL_ANATOMY = (
    "rounded-paper-face|soft-tonal-edge|upper-highlight|short-bottom-shadow|no-linear-rail")
REFERENCE_MATERIAL_STYLES = {
    "surface.status": ReferenceMaterialStyle(
        "sunlight-phase-status", (0xFF, 0xD2, 0x54, 255),
        outline=(0xA5, 0x70, 0x22, 255),
        highlight=(0xFF, 0xEC, 0xA4, 255)),
    "surface.detail": ReferenceMaterialStyle(
        "raised-detail-paper", (0xFF, 0xF6, 0xE3, 255),
        outline=(0xB8, 0x91, 0x62, 255)),
    "surface.modal": ReferenceMaterialStyle(
        "raised-modal-paper", (0xFF, 0xF7, 0xE8, 255),
        outline=(0x9C, 0x72, 0x47, 255)),
    "surface.result": ReferenceMaterialStyle(
        "sunlit-result-paper", (0xFF, 0xED, 0xC2, 255),
        outline=(0x9C, 0x72, 0x47, 255),
        highlight=(0xFF, 0xF9, 0xE4, 255)),
    "surface.section-ribbon": ReferenceMaterialStyle(
        "pale-leaf-section", (0xED, 0xF3, 0xCC, 255),
        outline=(0x87, 0xA9, 0x3E, 255),
        highlight=(0xFA, 0xFC, 0xE8, 255)),
    "surface.illustration-frame": ReferenceMaterialStyle(
        "cream-illustration-frame", (0xF1, 0xD9, 0xA7, 255),
        outline=(0x73, 0x4C, 0x2B, 255), transparent_center_inset=48),
}


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


def author_reference_material(style: ReferenceMaterialStyle) -> Image.Image:
    """Build one 2x source master with five independently visible material layers."""
    canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    draw.rounded_rectangle((8, 14, 248, 248), radius=40, fill=style.shadow)
    draw.rounded_rectangle((8, 8, 248, 240), radius=40, fill=style.rim)
    draw.rounded_rectangle((14, 14, 242, 234), radius=34, fill=style.outline)
    draw.rounded_rectangle((18, 18, 238, 228), radius=30, fill=style.face)
    draw.line((34, 23, 222, 23), fill=style.highlight, width=5)
    draw.arc((20, 20, 236, 226), start=200, end=340,
             fill=style.highlight, width=3)
    if style.transparent_center_inset:
        inset = style.transparent_center_inset
        radius = 20 if inset <= 40 else 12
        draw.rounded_rectangle(
            (inset, inset, 256 - inset, 256 - inset), radius=radius,
            fill=(0, 0, 0, 0))
    return canvas


def author_gameplay_stage(style: ReferenceMaterialStyle) -> Image.Image:
    """Build the 20 px protected transparent-center gameplay-stage rail."""
    canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    draw.rounded_rectangle((16, 24, 238, 248), radius=30, fill=style.shadow)
    draw.rounded_rectangle((16, 16, 238, 238), radius=30, fill=style.rim)
    draw.rounded_rectangle((22, 22, 232, 232), radius=24, fill=style.outline)
    draw.rounded_rectangle((26, 26, 228, 228), radius=20, fill=style.face)
    draw.line((40, 29, 222, 29), fill=style.highlight, width=5)
    draw.rounded_rectangle((32, 32, 222, 222), radius=6, fill=(0, 0, 0, 0))
    return canvas


def author_reference_material_masters(source_root: Path) -> None:
    """Author only non-action surfaces that still use the deterministic kit."""
    for slot in SLOTS:
        style = REFERENCE_MATERIAL_STYLES.get(slot.semantic_id)
        if style is None:
            continue
        master = (author_gameplay_stage(style)
                  if slot.semantic_id == "surface.gameplay-stage"
                  else author_reference_material(style))
        path = source_root / subdirectory(slot) / f"{source_stem(slot)}.png"
        path.parent.mkdir(parents=True, exist_ok=True)
        save_png(master, path)


def mark_reference_material_import_meta(
        slot: Slot, source_root: Path, runtime_root: Path) -> None:
    if slot.semantic_id not in REFERENCE_MATERIAL_STYLES:
        return
    for owner, path in (
            ("ui-art-source", source_root / subdirectory(slot)
             / f"{source_stem(slot)}.png.meta"),
            ("ui-art-set", runtime_root / subdirectory(slot)
             / f"{slot.stem}.png.meta")):
        text = path.read_text(encoding="utf-8")
        marker = (f"{owner}={SET_ID};slot={slot.semantic_id};source-scale=2;"
                  "authored=deterministic-reference-material-kit")
        if not re.search(r"(?m)^\s*userData:.*$", text):
            raise RuntimeError(f"No userData field in material meta: {path}")
        text = re.sub(r"(?m)^\s*userData:.*$", "  userData: " + marker, text)
        path.write_text(text, encoding="utf-8")


def imagegen_material_record(
        semantic_id: str) -> tuple[str, str, str, str]:
    for asset_semantic_id, _, filename, asset_sha256 in (
            IMAGEGEN_DIRECT_ASSETS):
        if asset_semantic_id == semantic_id:
            mask = IMAGEGEN_GEOMETRY_MASKS.get(filename)
            return (f"{IMAGEGEN_DIRECT_ROOT}/{filename}", asset_sha256,
                    f"{IMAGEGEN_DIRECT_ROOT}/{mask[0]}" if mask else "",
                    "connected-neutral-background-cleanup"
                    if filename in IMAGEGEN_CONNECTED_BACKGROUND_ASSETS else "")
    raise RuntimeError(f"No direct ImageGen material for {semantic_id}")


def mark_imagegen_material_import_meta(
        slot: Slot, source_root: Path, runtime_root: Path) -> None:
    """Record one hash-locked direct ImageGen master on each material."""
    if slot.semantic_id not in IMAGEGEN_MATERIAL_IDS:
        return
    asset_path, asset_sha256, geometry_mask, background_cleanup = (
        imagegen_material_record(
        slot.semantic_id)
    )
    transform = ("geometry-alpha-mask" if geometry_mask else
                 "connected-neutral-background-cleanup"
                 if background_cleanup else "alpha-crop")
    for owner, path in (
            ("ui-art-source", source_root / subdirectory(slot)
             / f"{source_stem(slot)}.png.meta"),
            ("ui-art-set", runtime_root / subdirectory(slot)
             / f"{slot.stem}.png.meta")):
        text = path.read_text(encoding="utf-8")
        marker = (f"{owner}={SET_ID};slot={slot.semantic_id};source-scale=2;"
                  f"authored=imagegen-direct-master;transform={transform};"
                  f"asset={asset_path};asset-sha256={asset_sha256}")
        if not re.search(r"(?m)^\s*userData:.*$", text):
            raise RuntimeError(f"No userData field in ImageGen material meta: {path}")
        text = re.sub(r"(?m)^\s*userData:.*$", "  userData: " + marker, text)
        path.write_text(text, encoding="utf-8")


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
    if semantic_id in {"action.primary", "action.secondary"}:
        return green >= red * 1.2 and green >= blue * 1.5
    if semantic_id == "action.danger":
        return red >= green * 1.4 and red >= blue * 1.2
    return False


def mark_semantic_container_import_meta(meta_path: Path, semantic_id: str) -> None:
    text = meta_path.read_text(encoding="utf-8")
    role = semantic_id.removeprefix("action.")
    target = SEMANTIC_CONTAINER_TARGETS[semantic_id]
    content = CONTENT_REFERENCE_RGB[semantic_id]
    marker = (f"container={role};target-rgb={target[0]:02X}{target[1]:02X}{target[2]:02X}"
              f";content-reference={content[0]:02X}{content[1]:02X}{content[2]:02X}"
              f";minimum-contrast=4.5")
    match = re.search(r"(?m)^(\s*userData:)\s*(.*)$", text)
    if match is None:
        raise RuntimeError(f"No userData field in container meta: {meta_path}")
    current = match.group(2).strip()
    cleaned = re.sub(
        r"(?:^|;)container=[^;]+;target-rgb=[0-9A-Fa-f]{6}"
        r";content-reference=[0-9A-Fa-f]{6};minimum-contrast=[0-9.]+",
        "", current).strip(";")
    updated = cleaned + (";" if cleaned else "") + marker
    if updated != current:
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
    content_reference = CONTENT_REFERENCE_RGB[semantic_id]
    ratios = [contrast_ratio(content_reference, pixel[:3])
              for pixel in content.get_flattened_data()
              if is_semantic_container_ink(semantic_id, pixel)]
    if not ratios:
        raise RuntimeError(f"No runtime content pixels found for {semantic_id}")
    minimum = min(ratios)
    if minimum < MINIMUM_CONTENT_CONTRAST:
        raise RuntimeError(
            f"{semantic_id} content contrast {minimum:.3f}:1 is below 4.5:1; "
            "change the separate text/icon content token first and keep the "
            "reference-authoritative container raster unchanged")
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


def imagegen_content_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    """Find generated content while ignoring transparent fringe pixels."""
    visible = image.convert("RGBA").getchannel("A").point(
        lambda alpha: 255 if alpha >= 8 else 0)
    bbox = visible.getbbox()
    if bbox is None:
        raise RuntimeError("Direct ImageGen material has no visible pixels")
    return bbox


def fit_direct_imagegen_master(
        image: Image.Image, semantic_id: str) -> Image.Image:
    """Crop one transparent ImageGen output directly into the 2x master."""
    rgba = image.convert("RGBA")
    crop = rgba.crop(imagegen_content_bbox(rgba))
    is_stage = semantic_id == "surface.gameplay-stage"
    fitted_size = (224, 234) if is_stage else (240, 240)
    offset = (16, 16) if is_stage else (8, 8)
    fitted = alpha_safe_resize(crop, fitted_size)
    master = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    master.alpha_composite(fitted, offset)
    return master


def fit_rgb_imagegen_master_with_geometry_mask(
        image: Image.Image, geometry_mask: Image.Image) -> Image.Image:
    """Use only the approved target alpha when ImageGen bakes a checkerboard."""
    rgb = image.convert("RGB")
    foreground = Image.new("1", rgb.size)
    foreground.putdata([
        255 if max(pixel) - min(pixel) >= 8 or min(pixel) < 215 else 0
        for pixel in rgb.get_flattened_data()
    ])
    bbox = foreground.getbbox()
    if bbox is None:
        raise RuntimeError("RGB ImageGen material has no detectable foreground")
    fitted = rgb.crop(bbox).resize((240, 240), Image.Resampling.LANCZOS)
    master = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    master.paste(fitted, (8, 8))
    approved_alpha = geometry_mask.convert("RGBA").getchannel("A")
    if approved_alpha.size != master.size:
        raise RuntimeError("ImageGen geometry mask must be 256x256")
    master.putalpha(approved_alpha)
    return master


def extract_center_connected_material_from_neutral_background(
        image: Image.Image) -> Image.Image:
    """Remove only the edge-connected neutral checkerboard around one material.

    The selected ImageGen output is RGB even though transparency was requested.
    Its checkerboard is bright and neutral while the paper carrier is warm. The
    cleanup derives alpha from this same output, then keeps only the foreground
    component connected to the image center. It never reuses legacy geometry or
    paints visible material pixels.
    """
    rgb = image.convert("RGB")
    width, height = rgb.size
    pixels = list(rgb.get_flattened_data())
    background = bytearray(width * height)
    queue: deque[int] = deque()

    def is_neutral_background(index: int) -> bool:
        red, green, blue = pixels[index]
        return max(red, green, blue) - min(red, green, blue) <= 10 \
            and min(red, green, blue) >= 225

    def enqueue_background(index: int) -> None:
        if not background[index] and is_neutral_background(index):
            background[index] = 1
            queue.append(index)

    for x in range(width):
        enqueue_background(x)
        enqueue_background((height - 1) * width + x)
    for y in range(height):
        enqueue_background(y * width)
        enqueue_background(y * width + width - 1)

    while queue:
        index = queue.popleft()
        x = index % width
        y = index // width
        if x > 0:
            enqueue_background(index - 1)
        if x + 1 < width:
            enqueue_background(index + 1)
        if y > 0:
            enqueue_background(index - width)
        if y + 1 < height:
            enqueue_background(index + width)

    center = (height // 2) * width + width // 2
    if background[center]:
        raise RuntimeError(
            "Direct ImageGen material center was classified as background")
    foreground = bytearray(width * height)
    foreground[center] = 1
    queue.append(center)
    while queue:
        index = queue.popleft()
        x = index % width
        y = index // width
        for neighbor in (
                index - 1 if x > 0 else -1,
                index + 1 if x + 1 < width else -1,
                index - width if y > 0 else -1,
                index + width if y + 1 < height else -1):
            if neighbor >= 0 and not background[neighbor] \
                    and not foreground[neighbor]:
                foreground[neighbor] = 1
                queue.append(neighbor)

    cleaned = Image.new("RGBA", rgb.size)
    cleaned.putdata([
        (red, green, blue, 255) if foreground[index] else (0, 0, 0, 0)
        for index, (red, green, blue) in enumerate(pixels)
    ])
    if cleaned.getchannel("A").getbbox() is None:
        raise RuntimeError("Connected background cleanup found no material")
    return cleaned


def validate_imagegen_material_ownership() -> None:
    """Forbid master reuse across semantic edge/anatomy contracts."""
    contracts_by_filename: dict[str, set[str]] = {}
    for semantic_id, _, filename, _ in IMAGEGEN_DIRECT_ASSETS:
        contract = ("line-free-carrier" if semantic_id in LINE_FREE_CARRIER_IDS
                    else "reviewed-material")
        contracts_by_filename.setdefault(filename, set()).add(contract)
    mixed = {
        filename: contracts for filename, contracts in contracts_by_filename.items()
        if len(contracts) > 1
    }
    if mixed:
        raise RuntimeError(
            "Direct ImageGen master is shared across incompatible edge contracts: "
            f"{mixed}")


def validate_transparent_imagegen_edge(
        image: Image.Image, semantic_id: str) -> None:
    """Reject hidden matte RGB and low-alpha ringing for every direct material."""
    for red, green, blue, alpha in image.convert("RGBA").get_flattened_data():
        if alpha == 0 and (red != 0 or green != 0 or blue != 0):
            raise RuntimeError(
                f"{semantic_id} contains hidden RGB under zero alpha: "
                f"rgba=({red},{green},{blue},{alpha})")
        if 0 < alpha < OPTICAL_ALPHA_THRESHOLD:
            raise RuntimeError(
                f"{semantic_id} contains low-alpha resize ringing: "
                f"rgba=({red},{green},{blue},{alpha})")


def has_metric_perimeter_rail(image: Image.Image) -> bool:
    """Detect a continuous dark authored rail around the compact metric carrier."""
    rgba = image.convert("RGBA")
    width, height = rgba.size
    pixels = list(rgba.get_flattened_data())
    minimum_x, maximum_x = width // 4, width - width // 4
    minimum_y, maximum_y = height // 4, height - height // 4
    search_depth = max(1, min(width, height) // 4)

    def is_dark(index: int) -> bool:
        red, green, blue, alpha = pixels[index]
        return alpha >= 96 and red + green + blue < 225 * 3

    def is_rail(indices: list[int]) -> bool:
        return sum(1 for index in indices if is_dark(index)) * 4 \
            >= len(indices) * 3

    for offset in range(search_depth):
        top = offset
        bottom = height - 1 - offset
        if is_rail([top * width + x for x in range(minimum_x, maximum_x)]) \
                or is_rail([bottom * width + x
                            for x in range(minimum_x, maximum_x)]):
            return True
        left = offset
        right = width - 1 - offset
        if is_rail([y * width + left for y in range(minimum_y, maximum_y)]) \
                or is_rail([y * width + right
                            for y in range(minimum_y, maximum_y)]):
            return True
    return False


def validate_line_free_carrier_edge(
        image: Image.Image, semantic_id: str) -> None:
    """Reject edge pixels that PC filtering can reconstruct as four lines."""
    for red, green, blue, alpha in image.convert("RGBA").get_flattened_data():
        if alpha == 0 or alpha == 255:
            continue
        minimum = min(red, green, blue)
        maximum = max(red, green, blue)
        if maximum < 160 or maximum - minimum <= 10 and minimum < 225:
            raise RuntimeError(
                f"{semantic_id} contains a neutral dark semi-transparent fringe: "
                f"rgba=({red},{green},{blue},{alpha})")
    if semantic_id == "surface.metric" and has_metric_perimeter_rail(image):
        raise RuntimeError(
            "surface.metric contains a continuous dark perimeter rail")


def extract_imagegen_material_masters(
        project_root: Path, source_root: Path) -> None:
    """Integrate hash-locked individual ImageGen outputs without a sheet pass."""
    validate_imagegen_material_ownership()
    for semantic_id, stem, filename, expected_sha256 in (
            IMAGEGEN_DIRECT_ASSETS):
        asset_path = project_root / IMAGEGEN_DIRECT_ROOT / filename
        if not asset_path.is_file():
            raise RuntimeError(f"Missing direct ImageGen material: {asset_path}")
        if sha256(asset_path) != expected_sha256:
            raise RuntimeError(f"Direct ImageGen material hash drift: {asset_path}")
        with Image.open(asset_path) as source:
            mask_record = IMAGEGEN_GEOMETRY_MASKS.get(filename)
            if mask_record:
                mask_path = project_root / IMAGEGEN_DIRECT_ROOT / mask_record[0]
                if (not mask_path.is_file()
                        or sha256(mask_path) != mask_record[1]):
                    raise RuntimeError(f"ImageGen geometry mask drift: {mask_path}")
                with Image.open(mask_path) as source_mask:
                    master = fit_rgb_imagegen_master_with_geometry_mask(
                        source, source_mask.convert("RGBA"))
            elif filename in IMAGEGEN_CONNECTED_BACKGROUND_ASSETS:
                cleaned = extract_center_connected_material_from_neutral_background(
                    source)
                master = fit_direct_imagegen_master(cleaned, semantic_id)
            else:
                master = fit_direct_imagegen_master(source, semantic_id)

        master = clear_low_alpha_fringe(master)
        validate_transparent_imagegen_edge(master, semantic_id)
        if semantic_id in LINE_FREE_CARRIER_IDS:
            validate_line_free_carrier_edge(master, semantic_id)

        center_alpha = master.crop((80, 80, 176, 176)).getchannel(
            "A").getextrema()
        if semantic_id == "surface.gameplay-stage":
            if center_alpha[1] > 8:
                raise RuntimeError(
                    "Generated gameplay-stage center is not transparent")
            if master.getpixel((0, 0))[3] != 0:
                raise RuntimeError(
                    "Generated gameplay-stage outer corner is not transparent")
        elif center_alpha[0] < 248:
            raise RuntimeError(
                f"Generated material center is not stretch-safe: {semantic_id}")
        save_png(master, source_root / "surfaces" / f"{stem}.png")


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


def ensure_reference_material_import_meta(
        slot: Slot, source_root: Path, runtime_root: Path) -> None:
    """Create stable importer metadata for reference-kit slots added after v1."""
    if slot.index not in (53, 54):
        return
    folder = subdirectory(slot)
    source_path = source_root / folder / f"{source_stem(slot)}.png"
    runtime_path = runtime_root / folder / f"{slot.stem}.png"
    if not source_path.is_file():
        raise RuntimeError(f"Missing reference material master: {source_path}")

    source_meta = source_path.with_suffix(".png.meta")
    if not source_meta.exists():
        source_template = source_root / "surfaces/action-quiet.png.meta"
        text = source_template.read_text(encoding="utf-8")
        text = re.sub(r"(?m)^guid:\s*[0-9a-f]{32}\s*$",
                      "guid: " + stable_guid("source", slot.semantic_id), text)
        text = re.sub(r"(?m)^(\s*maxTextureSize:)\s*\d+\s*$",
                      r"\g<1> 2048", text)
        text = re.sub(
            r"(?m)^\s*userData:.*$",
            "  userData: ui-art-source=" + SET_ID + ";slot=" + slot.semantic_id
            + ";source-scale=2;authored=deterministic-reference-material-kit", text)
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
        text = re.sub(
            r"(?m)^\s*userData:.*$",
            "  userData: ui-art-set=" + SET_ID + ";slot=" + slot.semantic_id
            + ";source-scale=2;authored=deterministic-reference-material-kit", text)
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
                        or slot.semantic_id in IMAGEGEN_MATERIAL_IDS
                        else rgba.resize(size, Image.Resampling.LANCZOS))
        if slot.semantic_id in IMAGEGEN_MATERIAL_IDS \
                or slot.index >= 40 and not (49 <= slot.index <= 51):
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
            if bbox != (8, 8, 120, 125):
                raise RuntimeError(
                    f"Gameplay stage bbox must be [8,8,120,125): {runtime_path} {bbox}")
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
        if slot.semantic_id in IMAGEGEN_MATERIAL_IDS:
            validate_transparent_imagegen_edge(rgba, slot.semantic_id)
        if slot.semantic_id in LINE_FREE_CARRIER_IDS:
            validate_line_free_carrier_edge(rgba, slot.semantic_id)


def main() -> None:
    source_root = Path(__file__).resolve().parent
    project_root = source_root.parents[4]
    runtime_root = project_root / "Assets/UI/Art/Runtime" / SET_ID

    author_reference_material_masters(source_root)
    extract_imagegen_material_masters(project_root, source_root)

    unique: dict[str, Slot] = {}
    for slot in SLOTS:
        unique.setdefault(slot.stem, slot)
    for slot in unique.values():
        ensure_reference_material_import_meta(slot, source_root, runtime_root)
        mark_reference_material_import_meta(slot, source_root, runtime_root)
        mark_imagegen_material_import_meta(slot, source_root, runtime_root)
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
            content_reference = CONTENT_REFERENCE_RGB[slot.semantic_id]
            item.update({
                "container_contract": "semantic-action-container",
                "target_rgb": f"{target[0]:02X}{target[1]:02X}{target[2]:02X}",
                "content_reference_rgb": (
                    f"{content_reference[0]:02X}{content_reference[1]:02X}"
                    f"{content_reference[2]:02X}"),
                "content_region_min_contrast": round(minimum_contrast, 4),
            })
        style = REFERENCE_MATERIAL_STYLES.get(slot.semantic_id)
        if style is not None:
            item.update({
                "authoring_contract": "deterministic-reference-material-kit",
                "material_recipe": style.recipe,
                "material_anatomy": REFERENCE_MATERIAL_ANATOMY,
                "outer_cream_rgb": "{:02X}{:02X}{:02X}".format(*style.rim[:3]),
                "face_rgb": "{:02X}{:02X}{:02X}".format(*style.face[:3]),
                "soil_outline_rgb": "{:02X}{:02X}{:02X}".format(*style.outline[:3]),
                "upper_highlight_rgb": "{:02X}{:02X}{:02X}".format(*style.highlight[:3]),
                "bottom_shadow_rgb": "{:02X}{:02X}{:02X}".format(*style.shadow[:3]),
            })
            if slot.semantic_id == "slot.tool":
                item["content_layout_contract"] = (
                    "main-icon|multiply|target-glyph|corner-inventory-badge")
            if slot.semantic_id in {"action.primary", "action.secondary"}:
                item["content_tone"] = "primary"
            elif slot.semantic_id == "action.danger":
                item["content_tone"] = "inverse"
        if slot.semantic_id in IMAGEGEN_MATERIAL_IDS:
            asset_path, asset_sha256, geometry_mask, background_cleanup = (
                imagegen_material_record(slot.semantic_id))
            item.update({
                "authoring_contract": "imagegen-direct-master",
                "material_anatomy": (
                    LINE_FREE_CARRIER_MATERIAL_ANATOMY
                    if slot.semantic_id in LINE_FREE_CARRIER_IDS
                    else REFERENCE_MATERIAL_ANATOMY),
                "generated_asset": asset_path,
                "generated_asset_sha256": asset_sha256,
                "deterministic_transform": (
                     "content-crop|transparent-padding|alpha-safe-resize"
                     + ("|approved-geometry-alpha-mask"
                        if geometry_mask else "")
                     + ("|connected-neutral-background-cleanup"
                        if background_cleanup else "")),
            })
            if slot.semantic_id == "slot.nursery":
                item["render_contract"] = "line-free-rounded-paper-slot"
            elif slot.semantic_id == "surface.metric":
                item["render_contract"] = "line-free-rounded-paper-metric"
            if slot.semantic_id in {"action.primary", "action.secondary"}:
                item["content_tone"] = "primary"
            elif slot.semantic_id == "action.danger":
                item["content_tone"] = "inverse"
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
        "approvedDirection": "Reference-faithful Sky Paper Orchard material kit",
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
    print(f"Exported {len(unique)} PNGs and {len(SLOTS)} bindings to {manifest_path}")


if __name__ == "__main__":
    main()

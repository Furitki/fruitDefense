#!/usr/bin/env python3
"""Deterministic A/B Dual-Grid terrain-art preparation and packaging pipeline.

The script deliberately does not call an image model. ``prepare`` creates the exact
semantic topology and prompt contract; a human or agent saves the real model return;
``finalize`` then performs only deterministic image transforms and mechanical QA.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Callable, Iterable

try:
    from PIL import Image, ImageDraw
except ImportError as exc:  # pragma: no cover - actionable CLI error
    raise SystemExit("Pillow is required: python -m pip install 'Pillow>=10,<13'") from exc


SCHEMA = "fruit-defense.dual-grid-art-pipeline.v3"
PROJECT_ROOT = Path(__file__).resolve().parents[1]
NW, NE, SE, SW = 1, 2, 4, 8
STRESS_ATLAS_SCHEMA = "fruit-defense.dual-grid-stress-atlas.v2"
STRESS_TILE_COUNT = 16
STRESS_VERTEX_SIZE = STRESS_TILE_COUNT + 1
STRESS_RUNTIME_TILE_SIZE = 32
STRESS_PANEL_SIZE = STRESS_TILE_COUNT * STRESS_RUNTIME_TILE_SIZE
STRESS_ATLAS_SIZE = STRESS_PANEL_SIZE * 2
STRESS_ATLAS_RELATIVE_PATH = "candidate/Stress-All-1024.png"
CANONICAL_SWASTIKA = (
    (8, 6, 13, 12),
    (5, 14, 15, 11),
    (2, 3, 7, 9),
    (0, 4, 10, 1),
)
A_MOTHER_MASKS = (
    (4, 12, 12, 8),
    (6, 15, 15, 9),
    (6, 15, 15, 9),
    (2, 3, 3, 1),
)
B_OVERSCAN_MASKS = (
    (0, 0, 4, 8, 0, 0),
    (4, 8, 6, 13, 12, 8),
    (2, 5, 14, 15, 11, 1),
    (0, 2, 3, 7, 9, 0),
    (0, 0, 4, 10, 1, 0),
    (0, 0, 2, 1, 0, 0),
)
B_VERTEX_LATTICE = (
    (0, 0, 0, 0, 0, 0, 0),
    (0, 0, 0, 1, 0, 0, 0),
    (0, 1, 0, 1, 1, 1, 0),
    (0, 0, 1, 1, 1, 0, 0),
    (0, 0, 0, 0, 1, 0, 0),
    (0, 0, 0, 1, 0, 0, 0),
    (0, 0, 0, 0, 0, 0, 0),
)
BAYER8 = (
    0, 48, 12, 60, 3, 51, 15, 63,
    32, 16, 44, 28, 35, 19, 47, 31,
    8, 56, 4, 52, 11, 59, 7, 55,
    40, 24, 36, 20, 43, 27, 39, 23,
    2, 50, 14, 62, 1, 49, 13, 61,
    34, 18, 46, 30, 33, 17, 45, 29,
    10, 58, 6, 54, 9, 57, 5, 53,
    42, 26, 38, 22, 41, 25, 37, 21,
)


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def decoded_rgba_sha256(image: Image.Image) -> str:
    return sha256_bytes(image.convert("RGBA").tobytes())


def parse_color(value: str) -> tuple[int, int, int, int]:
    token = value.strip().lstrip("#")
    if len(token) not in (6, 8):
        raise ValueError(f"Color must be #RRGGBB or #RRGGBBAA: {value}")
    channels = tuple(int(token[index : index + 2], 16) for index in range(0, len(token), 2))
    return channels + (255,) if len(channels) == 3 else channels


def png_bytes(image: Image.Image) -> bytes:
    buffer = io.BytesIO()
    image.convert("RGBA").save(buffer, format="PNG", optimize=False)
    return buffer.getvalue()


def write_bytes(path: Path, data: bytes, force: bool) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        current = path.read_bytes()
        if current == data:
            return
        if not force:
            raise FileExistsError(f"Refusing to overwrite changed output without --force: {path}")
    path.write_bytes(data)


def write_image(path: Path, image: Image.Image, force: bool) -> None:
    write_bytes(path, png_bytes(image), force)


def write_json(path: Path, value: object, force: bool) -> None:
    data = (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    write_bytes(path, data, force)


def write_text(path: Path, value: str, force: bool) -> None:
    write_bytes(path, value.rstrip().encode("utf-8") + b"\n", force)


def load_profile(path: Path) -> dict:
    profile = json.loads(path.read_text(encoding="utf-8"))
    required = ("id", "route", "landformLabel", "baseLabel", "stylePrompt")
    missing = [key for key in required if not profile.get(key)]
    if missing:
        raise ValueError(f"Profile is missing required keys: {', '.join(missing)}")
    route = str(profile["route"]).upper()
    if route not in ("A", "B"):
        raise ValueError("Profile route must be A or B")
    profile["route"] = route
    profile.setdefault("landformColor", "#FF4779")
    profile.setdefault("baseColor", "#FFFFFF")
    profile.setdefault("reviewSize", 256)
    profile.setdefault("runtimeSize", 32)
    profile.setdefault("boundaryBandWidth", 12)
    profile.setdefault("candidateMode", "route-default")
    profile.setdefault("protectedReviewWidth", 32)
    profile.setdefault("crossoverWidth", 16)
    if profile["reviewSize"] != 256 or profile["runtimeSize"] not in (32, 64):
        raise ValueError("v1 contract requires reviewSize=256 and runtimeSize=32 or 64")
    profile.setdefault(
        "runtimeSampling", "center-point" if profile["runtimeSize"] == 32 else "lanczos"
    )
    if profile["runtimeSampling"] not in ("center-point", "lanczos"):
        raise ValueError("runtimeSampling must be center-point or lanczos")
    if profile["runtimeSize"] > 32 and profile["runtimeSampling"] != "lanczos":
        raise ValueError("Runtime sizes above 32 require lanczos sampling")
    if profile["candidateMode"] not in ("route-default", "pure-model", "protected-hybrid"):
        raise ValueError("candidateMode must be route-default, pure-model, or protected-hybrid")
    protected = int(profile["protectedReviewWidth"])
    crossover = int(profile["crossoverWidth"])
    if protected < 0 or crossover < 0 or protected + crossover > 128:
        raise ValueError("Protected and crossover widths must be non-negative and fit the tile")
    brush = profile.get("unityBrush")
    if not isinstance(brush, dict):
        raise ValueError("Profile is missing required unityBrush registration metadata")
    brush_required = (
        "brushId", "assetFolderName", "displayName", "landformDisplayName",
        "baseDisplayName", "landformSurfaceId", "baseSurfaceId",
        "contourStyleId", "edgeStyleId",
    )
    brush_missing = [key for key in brush_required if not brush.get(key)]
    if brush_missing:
        raise ValueError("unityBrush is missing required keys: " + ", ".join(brush_missing))
    if any(character in str(brush["assetFolderName"]) for character in "/\\:*"):
        raise ValueError("unityBrush.assetFolderName must be one safe folder name")
    if brush["landformSurfaceId"] == brush["baseSurfaceId"]:
        raise ValueError("unityBrush foreground and background surfaces must differ")
    brush.setdefault("foregroundMask", 15)
    brush.setdefault("backgroundMask", 0)
    for key in ("foregroundMask", "backgroundMask"):
        brush[key] = int(brush[key])
        if brush[key] < 0 or brush[key] > 15:
            raise ValueError(f"unityBrush.{key} must be in the inclusive range 0..15")
    if brush["foregroundMask"] == brush["backgroundMask"]:
        raise ValueError("unityBrush foreground and background masks must differ")
    return profile


def brush_import_descriptor(profile: dict) -> dict:
    brush = profile["unityBrush"]
    return {
        "schema": "fruit-defense.terrain-brush-import.v2",
        "profileId": profile["id"],
        "brushId": brush["brushId"],
        "assetFolderName": brush["assetFolderName"],
        "displayName": brush["displayName"],
        "landformDisplayName": brush["landformDisplayName"],
        "baseDisplayName": brush["baseDisplayName"],
        "landformSurfaceId": brush["landformSurfaceId"],
        "baseSurfaceId": brush["baseSurfaceId"],
        "contourStyleId": brush["contourStyleId"],
        "edgeStyleId": brush["edgeStyleId"],
        "foregroundMask": brush["foregroundMask"],
        "backgroundMask": brush["backgroundMask"],
        "runtimeTileSize": profile["runtimeSize"],
        "runtimeMaskDirectory": f"Runtime{profile['runtimeSize']}",
        "sourceManifest": "manifest.json",
    }


def validate_brush_package(output_root: Path, descriptor: dict, manifest: dict) -> None:
    if descriptor.get("schema") not in (
        "fruit-defense.terrain-brush-import.v1",
        "fruit-defense.terrain-brush-import.v2",
    ):
        raise ValueError("BrushImport.json has an unsupported schema")
    if descriptor.get("profileId") != manifest.get("profileId"):
        raise ValueError("BrushImport.json profileId does not match the pipeline manifest")
    if manifest.get("runtimeMaskCount") != 16:
        raise ValueError("Brush import requires a sixteen-mask pipeline manifest")
    runtime_size = int(descriptor.get("runtimeTileSize", 32))
    manifest_runtime_size = int(manifest.get("runtimeTileSize", 32))
    if runtime_size not in (32, 64) or runtime_size != manifest_runtime_size:
        raise ValueError("Brush runtime tile size is unsupported or disagrees with manifest")
    if descriptor.get("runtimeMaskDirectory") != f"Runtime{runtime_size}":
        raise ValueError("Brush runtime directory does not match its declared tile size")
    runtime_root = output_root / "candidate" / descriptor["runtimeMaskDirectory"]
    missing = [f"Mask-{mask:02d}.png" for mask in range(16)
               if not (runtime_root / f"Mask-{mask:02d}.png").is_file()]
    if missing:
        raise FileNotFoundError("Brush import Runtime masks are missing: " + ", ".join(missing))


def package_brush(profile_path: Path, output_root: Path, force: bool) -> dict:
    profile = load_profile(profile_path)
    output_root = assert_safe_output_root(output_root)
    manifest_path = output_root / "candidate" / "manifest.json"
    if not manifest_path.is_file():
        raise FileNotFoundError(f"Pipeline manifest not found: {manifest_path}")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    descriptor = brush_import_descriptor(profile)
    validate_brush_package(output_root, descriptor, manifest)
    write_json(output_root / "candidate" / "BrushImport.json", descriptor, force)
    return descriptor


def repackage_runtime(profile_path: Path, source_root: Path, output_root: Path) -> dict:
    profile = load_profile(profile_path)
    source_root = source_root.resolve()
    output_root = assert_safe_output_root(output_root)
    if output_root.exists():
        raise FileExistsError(
            f"Runtime repackaging requires a new versioned output root: {output_root}"
        )
    validate(source_root)
    source_manifest_path = source_root / "candidate" / "manifest.json"
    source_manifest_hash = sha256_file(source_manifest_path)
    shutil.copytree(source_root, output_root)

    manifest_path = output_root / "candidate" / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    review_masks: dict[int, Image.Image] = {}
    for mask in range(16):
        with Image.open(
            output_root / "candidate" / "Review256" / f"Mask-{mask:02d}.png"
        ) as opened:
            review_masks[mask] = opened.convert("RGBA")
            review_masks[mask].load()

    runtime_masks, _stress_masks, stress_record = save_common_outputs(
        output_root, review_masks, profile, True
    )
    manifest["outputRoot"] = str(output_root)
    manifest["runtimeTileSize"] = int(profile["runtimeSize"])
    manifest["runtimeMaskDirectory"] = f"Runtime{profile['runtimeSize']}"
    manifest["runtimeSamplingMethod"] = profile["runtimeSampling"]
    manifest["stressRuntimeTileSize"] = STRESS_RUNTIME_TILE_SIZE
    manifest["runtimeAdjacency"] = adjacency_metrics(runtime_masks)
    manifest["stressAtlas"] = stress_record
    manifest["repackagedFrom"] = {
        "outputRoot": str(source_root),
        "manifestSha256": source_manifest_hash,
        "reviewMasksUnchanged": True,
    }
    manifest["generatedPngFiles"] = [
        image_record(path, output_root) for path in sorted(output_root.rglob("*.png"))
    ]
    manifest["completedAt"] = utc_now()
    write_json(manifest_path, manifest, True)

    descriptor = brush_import_descriptor(profile)
    validate_brush_package(output_root, descriptor, manifest)
    write_json(output_root / "candidate" / "BrushImport.json", descriptor, True)
    ready_path = output_root / "evidence" / "ready.json"
    ready = json.loads(ready_path.read_text(encoding="utf-8"))
    ready["manifestPath"] = str(manifest_path)
    ready["manifestSha256"] = sha256_file(manifest_path)
    ready["runtimeTileSize"] = int(profile["runtimeSize"])
    ready["runtimeRepackaged"] = True
    write_json(ready_path, ready, True)
    validation = validate(output_root)
    return {
        "schema": SCHEMA,
        "status": "ready-for-user-visual-review",
        "profileId": profile["id"],
        "sourceRoot": str(source_root),
        "outputRoot": str(output_root),
        "runtimeTileSize": int(profile["runtimeSize"]),
        "runtimeMaskDirectory": descriptor["runtimeMaskDirectory"],
        "validation": validation["status"],
        "visualInspectionPerformed": False,
    }


def assert_safe_output_root(output_root: Path) -> Path:
    resolved = output_root.resolve()
    if resolved == Path(resolved.anchor) or len(resolved.parts) < 3:
        raise ValueError(f"Unsafe output root: {resolved}")
    return resolved


def validate_permutation(layout: Iterable[Iterable[int]]) -> None:
    flattened = sorted(mask for row in layout for mask in row)
    if flattened != list(range(16)):
        raise AssertionError("Canonical swastika must contain every mask exactly once")


def mask_from_lattice(lattice: tuple[tuple[int, ...], ...], row: int, column: int) -> int:
    return (
        (NW if lattice[row][column] else 0)
        | (NE if lattice[row][column + 1] else 0)
        | (SE if lattice[row + 1][column + 1] else 0)
        | (SW if lattice[row + 1][column] else 0)
    )


def validate_static_contract() -> None:
    validate_permutation(CANONICAL_SWASTIKA)
    generated = tuple(
        tuple(mask_from_lattice(B_VERTEX_LATTICE, row, column) for column in range(6))
        for row in range(6)
    )
    if generated != B_OVERSCAN_MASKS:
        raise AssertionError("B lattice and 6x6 mask matrix disagree")
    cropped = tuple(tuple(row[1:5]) for row in B_OVERSCAN_MASKS[1:5])
    if cropped != CANONICAL_SWASTIKA:
        raise AssertionError("B central crop is not the canonical swastika")


def render_mask_board(
    masks: tuple[tuple[int, ...], ...], tile_size: int, landform: tuple[int, int, int, int],
    base: tuple[int, int, int, int]
) -> Image.Image:
    rows, columns = len(masks), len(masks[0])
    image = Image.new("RGBA", (columns * tile_size, rows * tile_size), base)
    pixels = image.load()
    half = tile_size // 2
    for row, mask_row in enumerate(masks):
        if len(mask_row) != columns:
            raise ValueError("Mask matrix rows must have equal length")
        for column, mask in enumerate(mask_row):
            ox, oy = column * tile_size, row * tile_size
            quadrants = (
                (NW, ox, oy),
                (NE, ox + half, oy),
                (SE, ox + half, oy + half),
                (SW, ox, oy + half),
            )
            for bit, qx, qy in quadrants:
                if mask & bit:
                    for y in range(qy, qy + half):
                        for x in range(qx, qx + half):
                            pixels[x, y] = landform
    return image


def prompt_for(profile: dict) -> str:
    route = profile["route"]
    landform = profile["landformLabel"]
    base = profile["baseLabel"]
    structure = (
        "The semantic input is a 1024x1024 complete patch: a continuous 3x3 landform area "
        "with a half-tile base-material margin on every side. Preserve that exact boundary."
        if route == "A"
        else "The semantic input is a contiguous 1536x1536 six-by-six overscan topology. "
        "Redraw the whole canvas; the center four-by-four region will be cropped mechanically."
    )
    return f"""Use case: precise topology-preserving terrain redraw
Asset type: Unity Dual-Grid top-down terrain source
Route: {route}

Semantic mapping:
- pink region = {landform}
- white region = {base}

Structure contract:
{structure}
Pink/white regions are semantic fields, not visible colors to retain. Preserve every outer
silhouette, corner occupancy, head, socket, canvas edge and registration. Do not add grid lines,
gutters, tile borders, labels, text, UI, props, characters, shadows outside the material surface,
watermarks, transparency, padding or crop.

Style contract:
{profile['stylePrompt']}
Use broad clean shapes, two or three value steps per material, sparse large details, soft
hand-drawn character and minimal texture noise. No dense grains, speckles or hard black outlines.
Return one fully opaque edge-to-edge image and no explanation.
"""


def prepare(profile_path: Path, output_root: Path, force: bool) -> dict:
    validate_static_contract()
    profile = load_profile(profile_path)
    output_root = assert_safe_output_root(output_root)
    landform = parse_color(profile["landformColor"])
    base = parse_color(profile["baseColor"])
    if landform[3] != 255 or base[3] != 255:
        raise ValueError("Semantic topology colors must be fully opaque")

    if profile["route"] == "A":
        masks = A_MOTHER_MASKS
        source_name = "Source-A-PatchTopology-1024.png"
    else:
        masks = B_OVERSCAN_MASKS
        source_name = "Source-B-OverscanTopology-1536.png"

    source = render_mask_board(masks, 256, landform, base)
    semantic_counts = {"landform": 0, "base": 0, "other": 0}
    source_bytes = source.tobytes()
    for index in range(0, len(source_bytes), 4):
        pixel = tuple(source_bytes[index : index + 4])
        if pixel == landform:
            semantic_counts["landform"] += 1
        elif pixel == base:
            semantic_counts["base"] += 1
        else:
            semantic_counts["other"] += 1
    if semantic_counts["other"] != 0:
        raise AssertionError("Semantic source contains colors outside the two-color contract")
    if profile["route"] == "A" and semantic_counts["landform"] != 768 * 768:
        raise AssertionError("A topology must contain one exact central 768x768 landform patch")
    source_path = output_root / "source" / source_name
    write_image(source_path, source, force)
    prompt_path = output_root / "request" / "pipeline-prompt.txt"
    write_text(prompt_path, prompt_for(profile), force)
    topology = {
        "schema": SCHEMA,
        "stage": "prepared",
        "profileId": profile["id"],
        "route": profile["route"],
        "maskBits": {"NW": NW, "NE": NE, "SE": SE, "SW": SW},
        "maskMatrixTopOrigin": masks,
        "vertexLatticeTopOrigin": B_VERTEX_LATTICE if profile["route"] == "B" else None,
        "canonicalCentralCrop": CANONICAL_SWASTIKA if profile["route"] == "B" else None,
        "sourcePath": str(source_path),
        "sourceSize": list(source.size),
        "sourceSha256": sha256_file(source_path),
        "landformColor": profile["landformColor"],
        "baseColor": profile["baseColor"],
        "semanticPixelCounts": semantic_counts,
        "gridLines": False,
        "promptPath": str(prompt_path),
        "promptSha256": sha256_file(prompt_path),
    }
    write_json(output_root / "source" / "topology.json", topology, force)
    return topology


def normalize_nearest(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Center-address Point normalization with an explicit, reviewable formula."""
    source = image.convert("RGBA")
    source_width, source_height = source.size
    target_width, target_height = size
    source_bytes = source.tobytes()
    output_bytes = bytearray(target_width * target_height * 4)
    source_x = [
        min(source_width - 1, ((2 * x + 1) * source_width) // (2 * target_width))
        for x in range(target_width)
    ]
    source_y = [
        min(source_height - 1, ((2 * y + 1) * source_height) // (2 * target_height))
        for y in range(target_height)
    ]
    for target_y, source_row in enumerate(source_y):
        destination_row = target_y * target_width * 4
        source_row_offset = source_row * source_width * 4
        for target_x, source_column in enumerate(source_x):
            source_index = source_row_offset + source_column * 4
            destination_index = destination_row + target_x * 4
            output_bytes[destination_index : destination_index + 4] = source_bytes[
                source_index : source_index + 4
            ]
    return Image.frombytes("RGBA", size, bytes(output_bytes))


def copy_raw(raw_path: Path, target: Path, force: bool) -> Image.Image:
    with Image.open(raw_path) as opened:
        image = opened.convert("RGBA")
        image.load()
    if image.width != image.height:
        raise ValueError(f"Model Raw must be square, found {image.width}x{image.height}: {raw_path}")
    if image.getchannel("A").getextrema() != (255, 255):
        raise ValueError(f"Model Raw must be fully opaque: {raw_path}")
    write_bytes(target, raw_path.read_bytes(), force)
    return image


def normalize_path_string(value: str | Path) -> str:
    return str(Path(value).resolve()).replace("\\", "/").casefold()


def validate_model_call(
    model_call_path: Path | None, output_root: Path, source_path: Path, prompt_path: Path,
    raw_path: Path, allow_missing: bool, force: bool
) -> dict:
    if model_call_path is None:
        if not allow_missing:
            raise ValueError(
                "Finalize requires --model-call. Use --allow-missing-model-call only for an "
                "explicitly identified legacy regression, never for a new candidate."
            )
        return {
            "status": "legacy-regression-explicitly-allowed",
            "enforced": False,
            "modelCallCount": None,
            "retryCount": None,
            "fallbackUsed": None,
        }

    model_call_path = model_call_path.resolve()
    record = json.loads(model_call_path.read_text(encoding="utf-8"))
    call_count = record.get("callCountExecuted", record.get("modelCallCount"))
    retry_count = record.get("retryCount", record.get("retryCountExecuted", 0))
    fallback_used = record.get("fallbackUsed", record.get("fallbackAllowed", False))
    required_text = ("tool", "inputPath", "inputSha256", "promptPath")
    missing = [key for key in required_text if not record.get(key)]
    if missing:
        raise ValueError(f"model-call.json is missing: {', '.join(missing)}")
    if call_count != 1 or retry_count != 0 or bool(fallback_used):
        raise ValueError("model-call.json must record one call, zero retry, and no fallback")
    if normalize_path_string(record["inputPath"]) != normalize_path_string(source_path):
        raise ValueError("model-call.json inputPath does not match this run's topology source")
    if str(record["inputSha256"]).casefold() != sha256_file(source_path):
        raise ValueError("model-call.json inputSha256 does not match the topology source")
    if normalize_path_string(record["promptPath"]) != normalize_path_string(prompt_path):
        raise ValueError("model-call.json promptPath does not match this run's prompt")
    prompt_hash = record.get("promptSha256AtCall", record.get("promptSha256"))
    if prompt_hash and str(prompt_hash).casefold() != sha256_file(prompt_path):
        raise ValueError("model-call.json prompt hash does not match this run's prompt")
    recorded_raw = record.get("rawPath")
    accepted_raw_paths = {
        normalize_path_string(raw_path),
        normalize_path_string(output_root / "model" / "Raw.png"),
    }
    if recorded_raw and normalize_path_string(recorded_raw) not in accepted_raw_paths:
        raise ValueError("model-call.json rawPath does not match the supplied Raw")
    recorded_raw_hash = record.get("rawSha256")
    if recorded_raw_hash and str(recorded_raw_hash).casefold() != sha256_file(raw_path):
        raise ValueError("model-call.json rawSha256 does not match the supplied Raw")

    target = output_root / "request" / "model-call.json"
    write_bytes(target, model_call_path.read_bytes(), force)
    return {
        "status": "enforced",
        "enforced": True,
        "path": str(target),
        "sha256": sha256_file(target),
        "tool": record["tool"],
        "toolMode": record.get("toolMode"),
        "useCase": record.get("useCase"),
        "executedAt": record.get("executedAt"),
        "modelCallCount": call_count,
        "retryCount": retry_count,
        "fallbackUsed": bool(fallback_used),
    }


def extract_a_samples(mother: Image.Image, output_root: Path, force: bool) -> dict[str, Image.Image]:
    grass = mother.crop((256, 256, 768, 768)).resize((256, 256), Image.Resampling.NEAREST)
    soil = Image.new("RGBA", (256, 256))
    corners = (
        ((0, 0, 128, 128), (0, 0)),
        ((896, 0, 1024, 128), (128, 0)),
        ((0, 896, 128, 1024), (0, 128)),
        ((896, 896, 1024, 1024), (128, 128)),
    )
    for rect, destination in corners:
        soil.paste(mother.crop(rect), destination)
    samples = {
        "grass": grass,
        "soil": soil,
        "top": mother.crop((384, 112, 640, 144)),
        "right": mother.crop((880, 384, 912, 640)),
        "bottom": mother.crop((384, 880, 640, 912)),
        "left": mother.crop((112, 384, 144, 640)),
    }
    names = {
        "grass": "Grass-Center-Point256.png",
        "soil": "Base-FourCorners-256.png",
        "top": "Boundary-Top.png",
        "right": "Boundary-Right.png",
        "bottom": "Boundary-Bottom.png",
        "left": "Boundary-Left.png",
    }
    for key, image in samples.items():
        write_image(output_root / "model" / "Samples" / names[key], image, force)
    return samples


def topology_tiles_from_guide(path: Path) -> dict[int, Image.Image]:
    with Image.open(path) as opened:
        guide = opened.convert("RGBA")
        guide.load()
    if guide.size != (1024, 1024):
        raise ValueError(f"Topology guide must be 1024x1024: {path}")
    tiles: dict[int, Image.Image] = {}
    for mask in range(16):
        column, row = mask % 4, mask // 4
        tiles[mask] = guide.crop((column * 256, row * 256, (column + 1) * 256, (row + 1) * 256))
    return tiles


def nearest_change(alpha: list[bool], x: int, y: int, size: int, band: int):
    current = alpha[x + y * size]
    directions = ((0, -1), (1, 0), (0, 1), (-1, 0))
    for distance in range(1, band + 1):
        for dx, dy in directions:
            nx, ny = x + dx * distance, y + dy * distance
            if 0 <= nx < size and 0 <= ny < size and alpha[nx + ny * size] != current:
                return current, dx, dy, distance
    return None


def boundary_pixel(samples: dict[str, Image.Image], x: int, y: int, change) -> tuple[int, int, int, int]:
    inside, dx, dy, distance = change
    grass_dx, grass_dy = ((-dx, -dy) if inside else (dx, dy))
    if (grass_dx, grass_dy) == (0, 1):
        sy = 15 + distance if inside else 16 - distance
        return samples["top"].getpixel((x, max(0, min(31, sy))))
    if (grass_dx, grass_dy) == (0, -1):
        sy = 16 - distance if inside else 15 + distance
        return samples["bottom"].getpixel((x, max(0, min(31, sy))))
    if (grass_dx, grass_dy) == (1, 0):
        sx = 15 + distance if inside else 16 - distance
        return samples["left"].getpixel((max(0, min(31, sx)), y))
    sx = 16 - distance if inside else 15 + distance
    return samples["right"].getpixel((max(0, min(31, sx)), y))


def reconstruct_a_masks(
    samples: dict[str, Image.Image], topology_guide: Path, band: int
) -> dict[int, Image.Image]:
    guide_tiles = topology_tiles_from_guide(topology_guide)
    grass_pixels = samples["grass"].load()
    soil_pixels = samples["soil"].load()
    masks: dict[int, Image.Image] = {}
    for mask in range(16):
        alpha_values = list(guide_tiles[mask].getchannel("A").tobytes())
        inside = [value > 0 for value in alpha_values]
        output = Image.new("RGBA", (256, 256))
        output_pixels = output.load()
        for y in range(256):
            for x in range(256):
                change = nearest_change(inside, x, y, 256, band)
                if change is not None:
                    output_pixels[x, y] = boundary_pixel(samples, x, y, change)
                else:
                    output_pixels[x, y] = grass_pixels[x, y] if inside[x + y * 256] else soil_pixels[x, y]
        masks[mask] = output
    return masks


def slice_b_masks(crop: Image.Image) -> dict[int, Image.Image]:
    masks: dict[int, Image.Image] = {}
    for row, mask_row in enumerate(CANONICAL_SWASTIKA):
        for column, mask in enumerate(mask_row):
            masks[mask] = crop.crop((column * 256, row * 256, (column + 1) * 256, (row + 1) * 256))
    if sorted(masks) != list(range(16)):
        raise AssertionError("B crop did not produce all masks")
    return masks


def load_trusted_masks(root: Path) -> dict[int, Image.Image]:
    root = root.resolve()
    masks: dict[int, Image.Image] = {}
    for mask in range(16):
        path = root / f"Mask-{mask:02d}.png"
        if not path.exists():
            raise FileNotFoundError(f"Trusted mask is missing: {path}")
        with Image.open(path) as opened:
            image = opened.convert("RGBA")
            image.load()
        if image.size != (256, 256) or image.getchannel("A").getextrema() != (255, 255):
            raise ValueError(f"Trusted mask must be opaque 256x256: {path}")
        masks[mask] = image
    return masks


def apply_candidate_mode(
    profile: dict, route_masks: dict[int, Image.Image], trusted_mask_root: Path | None
) -> tuple[dict[int, Image.Image], list[dict]]:
    mode = profile["candidateMode"]
    if mode in ("route-default", "pure-model"):
        ownership = [
            {
                "mask": mask,
                "routeDerivedPixels": 65536,
                "historicalPixels": 0,
                "fallbackPixels": 0,
            }
            for mask in range(16)
        ]
        return route_masks, ownership

    if trusted_mask_root is None:
        raise ValueError("protected-hybrid candidateMode requires --trusted-mask-root")
    trusted = load_trusted_masks(trusted_mask_root)
    protected = int(profile["protectedReviewWidth"])
    crossover = int(profile["crossoverWidth"])
    output: dict[int, Image.Image] = {}
    ownership: list[dict] = []
    for mask in range(16):
        model_pixels = route_masks[mask].convert("RGBA").load()
        historical_pixels = trusted[mask].convert("RGBA").load()
        candidate = Image.new("RGBA", (256, 256))
        candidate_pixels = candidate.load()
        historical_count = route_count = 0
        for y in range(256):
            for x in range(256):
                edge_distance = min(x, 255 - x, y, 255 - y)
                use_historical = edge_distance < protected
                if not use_historical and crossover and edge_distance < protected + crossover:
                    offset = edge_distance - protected
                    threshold = ((crossover - 1 - offset) * 64) // crossover
                    use_historical = BAYER8[(y & 7) * 8 + (x & 7)] < threshold
                if use_historical:
                    candidate_pixels[x, y] = historical_pixels[x, y]
                    historical_count += 1
                else:
                    candidate_pixels[x, y] = model_pixels[x, y]
                    route_count += 1
        output[mask] = candidate
        ownership.append(
            {
                "mask": mask,
                "routeDerivedPixels": route_count,
                "historicalPixels": historical_count,
                "fallbackPixels": 0,
                "totalPixels": route_count + historical_count,
            }
        )
    return output, ownership


def assemble_atlas(masks: dict[int, Image.Image], layout: tuple[tuple[int, ...], ...]) -> Image.Image:
    tile_size = masks[0].width
    atlas = Image.new("RGBA", (4 * tile_size, 4 * tile_size))
    for row, mask_row in enumerate(layout):
        for column, mask in enumerate(mask_row):
            atlas.paste(masks[mask], (column * tile_size, row * tile_size))
    return atlas


def runtime_center_sample(image: Image.Image, size: int = 32) -> Image.Image:
    if image.size != (256, 256) or size <= 0 or 256 % size:
        raise ValueError("Center sampling requires a 256px source and an exact divisor")
    stride = 256 // size
    source = image.convert("RGBA").load()
    output = Image.new("RGBA", (size, size))
    pixels = output.load()
    for y in range(size):
        for x in range(size):
            # Unity's bottom-origin center maps one row upward in PNG top-origin space.
            pixels[x, y] = source[
                x * stride + stride // 2,
                y * stride + max(0, stride // 2 - 1),
            ]
    return output


def runtime_sample(image: Image.Image, size: int, method: str) -> Image.Image:
    if method == "center-point":
        return runtime_center_sample(image, size)
    if method == "lanczos":
        return image.convert("RGBA").resize((size, size), Image.Resampling.LANCZOS)
    raise ValueError(f"Unsupported runtime sampling method: {method}")


def stress_scenarios() -> tuple[tuple[str, str, Callable[[int, int], bool]], ...]:
    return (
        ("pureLandform", "true", lambda x, y: True),
        (
            "landformWithCentralBaseHole",
            "x < 5 or x > 11 or y < 5 or y > 11",
            lambda x, y: x < 5 or x > 11 or y < 5 or y > 11,
        ),
        (
            "baseWithCentralLandformIsland",
            "5 <= x <= 11 and 5 <= y <= 11",
            lambda x, y: 5 <= x <= 11 and 5 <= y <= 11,
        ),
        (
            "diagonalMixed",
            "(((floor(x/2)+floor(y/2))&1)==0) XOR ((6<=x<=10) or (6<=y<=10))",
            lambda x, y: (((x // 2 + y // 2) & 1) == 0) ^ (6 <= x <= 10 or 6 <= y <= 10),
        ),
    )


def stress_vertex_functions() -> tuple[tuple[str, Callable[[int, int], bool]], ...]:
    return tuple((scenario_id, field) for scenario_id, _formula, field in stress_scenarios())


def stress_map(runtime_masks: dict[int, Image.Image], field: Callable[[int, int], bool]) -> Image.Image:
    output = Image.new("RGBA", (STRESS_PANEL_SIZE, STRESS_PANEL_SIZE))
    for y in range(STRESS_TILE_COUNT):
        for x in range(STRESS_TILE_COUNT):
            mask = (
                (NW if field(x, y) else 0)
                | (NE if field(x + 1, y) else 0)
                | (SE if field(x + 1, y + 1) else 0)
                | (SW if field(x, y + 1) else 0)
            )
            output.paste(
                runtime_masks[mask],
                (x * STRESS_RUNTIME_TILE_SIZE, y * STRESS_RUNTIME_TILE_SIZE),
            )
    return output


def stress_panel_rect(index: int) -> list[int]:
    return [
        (index % 2) * STRESS_PANEL_SIZE,
        (index // 2) * STRESS_PANEL_SIZE,
        STRESS_PANEL_SIZE,
        STRESS_PANEL_SIZE,
    ]


def build_stress_atlas(runtime_masks: dict[int, Image.Image]) -> tuple[Image.Image, list[dict]]:
    atlas = Image.new("RGBA", (STRESS_ATLAS_SIZE, STRESS_ATLAS_SIZE))
    panel_records: list[dict] = []
    for index, (scenario_id, formula, field) in enumerate(stress_scenarios()):
        panel = stress_map(runtime_masks, field)
        x, y, width, height = stress_panel_rect(index)
        atlas.paste(panel, (x, y))
        panel_records.append(
            {
                "id": scenario_id,
                "rect": [x, y, width, height],
                "formula": formula,
                "decodedRgbaSha256": decoded_rgba_sha256(panel),
                "opaque": panel.getchannel("A").getextrema() == (255, 255),
            }
        )
    return atlas, panel_records


def stress_atlas_record(atlas: Image.Image, path: Path, output_root: Path, panels: list[dict]) -> dict:
    return {
        "schema": STRESS_ATLAS_SCHEMA,
        "path": str(path),
        "relativePath": path.relative_to(output_root).as_posix(),
        "width": STRESS_ATLAS_SIZE,
        "height": STRESS_ATLAS_SIZE,
        "panelSize": STRESS_PANEL_SIZE,
        "tileGridSize": [STRESS_TILE_COUNT, STRESS_TILE_COUNT],
        "logicalVertexSize": [STRESS_VERTEX_SIZE, STRESS_VERTEX_SIZE],
        "runtimeTileSize": [STRESS_RUNTIME_TILE_SIZE, STRESS_RUNTIME_TILE_SIZE],
        "coordinateOrigin": "PNG top-left",
        "layout": "2x2-row-major-no-gutter",
        "decodedRgbaSha256": decoded_rgba_sha256(atlas),
        "opaque": atlas.getchannel("A").getextrema() == (255, 255),
        "panels": panels,
    }


def stress_atlas_failures(
    actual_atlas: Image.Image, record: dict, runtime_masks: dict[int, Image.Image]
) -> list[str]:
    failures: list[str] = []
    expected_atlas, expected_panels = build_stress_atlas(runtime_masks)
    actual_atlas = actual_atlas.convert("RGBA")
    if record.get("schema") != STRESS_ATLAS_SCHEMA:
        failures.append("stress-atlas-schema")
    if record.get("relativePath") != STRESS_ATLAS_RELATIVE_PATH:
        failures.append("stress-atlas-path")
    if [record.get("width"), record.get("height")] != [STRESS_ATLAS_SIZE, STRESS_ATLAS_SIZE]:
        failures.append("stress-atlas-declared-size")
    if record.get("panelSize") != STRESS_PANEL_SIZE:
        failures.append("stress-atlas-panel-size")
    if record.get("tileGridSize") != [STRESS_TILE_COUNT, STRESS_TILE_COUNT]:
        failures.append("stress-atlas-tile-grid")
    if record.get("runtimeTileSize") != [STRESS_RUNTIME_TILE_SIZE, STRESS_RUNTIME_TILE_SIZE]:
        failures.append("stress-atlas-runtime-tile-size")
    if record.get("coordinateOrigin") != "PNG top-left":
        failures.append("stress-atlas-origin")
    if record.get("layout") != "2x2-row-major-no-gutter":
        failures.append("stress-atlas-layout")
    if record.get("logicalVertexSize") != [STRESS_VERTEX_SIZE, STRESS_VERTEX_SIZE]:
        failures.append("stress-atlas-logical-vertices")
    if actual_atlas.size != (STRESS_ATLAS_SIZE, STRESS_ATLAS_SIZE):
        failures.append("stress-atlas-size")
    if actual_atlas.getchannel("A").getextrema() != (255, 255) or record.get("opaque") is not True:
        failures.append("stress-atlas-alpha")
    if record.get("decodedRgbaSha256") != decoded_rgba_sha256(expected_atlas):
        failures.append("stress-atlas-declared-pixels")
    if actual_atlas.tobytes() != expected_atlas.tobytes():
        failures.append("stress-atlas-rebuild")

    actual_panels = record.get("panels", [])
    if len(actual_panels) != len(expected_panels):
        failures.append("stress-atlas-panel-count")
    for index, expected in enumerate(expected_panels):
        if index >= len(actual_panels):
            failures.append(f"stress-panel-missing:{expected['id']}")
            continue
        actual_record = actual_panels[index]
        scenario_id = expected["id"]
        for key in ("id", "rect", "formula", "decodedRgbaSha256", "opaque"):
            if actual_record.get(key) != expected[key]:
                failures.append(f"stress-panel-metadata:{scenario_id}:{key}")
        x, y, width, height = expected["rect"]
        actual_panel = actual_atlas.crop((x, y, x + width, y + height))
        if decoded_rgba_sha256(actual_panel) != expected["decodedRgbaSha256"]:
            failures.append(f"stress-panel-rebuild:{scenario_id}")
    return failures


def mismatch_count(first: Image.Image, second: Image.Image) -> int:
    left = first.convert("RGBA").tobytes()
    right = second.convert("RGBA").tobytes()
    if len(left) != len(right):
        raise ValueError("Edge buffers must have identical sizes")
    return sum(left[index : index + 4] != right[index : index + 4] for index in range(0, len(left), 4))


def adjacency_metrics(masks: dict[int, Image.Image]) -> dict:
    horizontal_pairs = horizontal_pair_mismatch = horizontal_pixels = 0
    vertical_pairs = vertical_pair_mismatch = vertical_pixels = 0
    tile_size = masks[0].width
    if tile_size <= 0 or any(image.size != (tile_size, tile_size) for image in masks.values()):
        raise ValueError("Adjacency metrics require equally sized square masks")
    for first in range(16):
        first_image = masks[first].convert("RGBA")
        for second in range(16):
            second_image = masks[second].convert("RGBA")
            if bool(first & NE) == bool(second & NW) and bool(first & SE) == bool(second & SW):
                horizontal_pairs += 1
                count = mismatch_count(
                    first_image.crop((tile_size - 1, 0, tile_size, tile_size)),
                    second_image.crop((0, 0, 1, tile_size)),
                )
                horizontal_pixels += count
                horizontal_pair_mismatch += int(count > 0)
            if bool(first & SW) == bool(second & NW) and bool(first & SE) == bool(second & NE):
                vertical_pairs += 1
                count = mismatch_count(
                    first_image.crop((0, tile_size - 1, tile_size, tile_size)),
                    second_image.crop((0, 0, tile_size, 1)),
                )
                vertical_pixels += count
                vertical_pair_mismatch += int(count > 0)
    if horizontal_pairs != 64 or vertical_pairs != 64:
        raise AssertionError("Expected exactly 64 legal pairs per axis")
    return {
        "horizontal": {
            "legalPairCount": horizontal_pairs,
            "pairMismatchCount": horizontal_pair_mismatch,
            "pixelMismatchCount": horizontal_pixels,
        },
        "vertical": {
            "legalPairCount": vertical_pairs,
            "pairMismatchCount": vertical_pair_mismatch,
            "pixelMismatchCount": vertical_pixels,
        },
    }


def image_record(path: Path, root: Path) -> dict:
    with Image.open(path) as opened:
        rgba = opened.convert("RGBA")
        rgba.load()
    alpha_extrema = rgba.getchannel("A").getextrema()
    return {
        "path": str(path),
        "relativePath": path.relative_to(root).as_posix(),
        "sha256": sha256_file(path),
        "decodedRgbaSha256": decoded_rgba_sha256(rgba),
        "width": rgba.width,
        "height": rgba.height,
        "opaque": alpha_extrema == (255, 255),
        "alphaExtrema": list(alpha_extrema),
    }


def save_common_outputs(
    output_root: Path, review_masks: dict[int, Image.Image], profile: dict, force: bool
) -> tuple[dict[int, Image.Image], dict[int, Image.Image], dict]:
    for mask, image in review_masks.items():
        write_image(output_root / "candidate" / "Review256" / f"Mask-{mask:02d}.png", image, force)

    numeric_layout = tuple(tuple(range(row * 4, row * 4 + 4)) for row in range(4))
    review_atlas = assemble_atlas(review_masks, numeric_layout)
    swastika = assemble_atlas(review_masks, CANONICAL_SWASTIKA)
    write_image(output_root / "candidate" / "ReviewAtlas-1024.png", review_atlas, force)
    write_image(output_root / "candidate" / "SwastikaLayout-1024.png", swastika, force)

    runtime_size = int(profile["runtimeSize"])
    runtime_method = profile["runtimeSampling"]
    runtime_directory = f"Runtime{runtime_size}"
    runtime_masks = {
        mask: runtime_sample(image, runtime_size, runtime_method)
        for mask, image in review_masks.items()
    }
    for mask, image in runtime_masks.items():
        write_image(
            output_root / "candidate" / runtime_directory / f"Mask-{mask:02d}.png",
            image,
            force,
        )
    runtime_atlas = assemble_atlas(runtime_masks, numeric_layout)
    runtime_upscaled = runtime_atlas.resize((1024, 1024), Image.Resampling.NEAREST)
    write_image(
        output_root / "candidate" / f"RuntimeAtlas-{runtime_size * 4}.png",
        runtime_atlas,
        force,
    )
    write_image(output_root / "candidate" / "RuntimeAtlas-Upscaled1024.png", runtime_upscaled, force)

    stress_masks = (
        runtime_masks if runtime_size == STRESS_RUNTIME_TILE_SIZE else {
            mask: runtime_center_sample(image, STRESS_RUNTIME_TILE_SIZE)
            for mask, image in review_masks.items()
        }
    )
    if runtime_size != STRESS_RUNTIME_TILE_SIZE:
        for mask, image in stress_masks.items():
            write_image(
                output_root / "candidate" / "Runtime32" / f"Mask-{mask:02d}.png",
                image,
                force,
            )

    legacy_stress_root = output_root / "candidate" / "Stress1024"
    if legacy_stress_root.exists():
        raise FileExistsError(
            f"Legacy multi-file stress output exists; use a new versioned output root: {legacy_stress_root}"
        )
    stress_atlas, stress_panels = build_stress_atlas(stress_masks)
    stress_path = output_root / STRESS_ATLAS_RELATIVE_PATH
    write_image(stress_path, stress_atlas, force)
    return (
        runtime_masks,
        stress_masks,
        stress_atlas_record(stress_atlas, stress_path, output_root, stress_panels),
    )


def verify_common_outputs(
    output_root: Path,
    review_masks: dict[int, Image.Image],
    runtime_masks: dict[int, Image.Image],
    runtime_size: int,
    runtime_method: str,
) -> None:
    if sorted(review_masks) != list(range(16)) or sorted(runtime_masks) != list(range(16)):
        raise AssertionError("Exactly 16 review and runtime masks are required")
    for mask in range(16):
        if (review_masks[mask].size != (256, 256)
                or runtime_masks[mask].size != (runtime_size, runtime_size)):
            raise AssertionError(f"Wrong dimensions for Mask-{mask:02d}")
        if review_masks[mask].getchannel("A").getextrema() != (255, 255):
            raise AssertionError(f"Review Mask-{mask:02d} is not opaque")
        if runtime_masks[mask].tobytes() != runtime_sample(
                review_masks[mask], runtime_size, runtime_method).tobytes():
            raise AssertionError(
                f"Runtime Mask-{mask:02d} does not match declared {runtime_method} sampling"
            )
    numeric = tuple(tuple(range(row * 4, row * 4 + 4)) for row in range(4))
    with Image.open(output_root / "candidate" / "ReviewAtlas-1024.png") as opened:
        if opened.convert("RGBA").tobytes() != assemble_atlas(review_masks, numeric).tobytes():
            raise AssertionError("Review atlas is not an exact assembly")
    with Image.open(output_root / "candidate" / "SwastikaLayout-1024.png") as opened:
        if opened.convert("RGBA").tobytes() != assemble_atlas(review_masks, CANONICAL_SWASTIKA).tobytes():
            raise AssertionError("Swastika atlas is not an exact assembly")


def finalize(
    profile_path: Path, output_root: Path, raw_path: Path, topology_guide: Path | None,
    model_call_path: Path | None, allow_missing_model_call: bool,
    trusted_mask_root: Path | None, force: bool
) -> dict:
    validate_static_contract()
    profile = load_profile(profile_path)
    output_root = assert_safe_output_root(output_root)
    topology_manifest = output_root / "source" / "topology.json"
    if not topology_manifest.exists():
        prepare(profile_path, output_root, force)
    topology_before = json.loads(topology_manifest.read_text(encoding="utf-8"))
    source_path = Path(topology_before["sourcePath"])
    source_hash_before = sha256_file(source_path)
    prompt_path = output_root / "request" / "pipeline-prompt.txt"
    if not prompt_path.exists():
        raise FileNotFoundError(f"Prepared prompt is missing: {prompt_path}")
    raw_path = raw_path.resolve()
    model_provenance = validate_model_call(
        model_call_path, output_root, source_path, prompt_path, raw_path,
        allow_missing_model_call, force
    )
    raw_target = output_root / "model" / "Raw.png"
    raw = copy_raw(raw_path, raw_target, force)

    if profile["route"] == "A":
        if topology_guide is None:
            configured = profile.get("topologyGuide")
            if not configured:
                raise ValueError("Route A requires --topology-guide or profile.topologyGuide")
            topology_guide = Path(configured)
            if not topology_guide.is_absolute():
                topology_guide = PROJECT_ROOT / topology_guide
        topology_guide = topology_guide.resolve()
        if not topology_guide.exists():
            raise FileNotFoundError(topology_guide)
        normalized = normalize_nearest(raw, (1024, 1024))
        normalized_path = output_root / "model" / "Normalized-Main-1024.png"
        write_image(normalized_path, normalized, force)
        write_image(output_root / "candidate" / "Main-Board-1024.png", normalized, force)
        samples = extract_a_samples(normalized, output_root, force)
        review_masks = reconstruct_a_masks(
            samples, topology_guide, int(profile["boundaryBandWidth"])
        )
        route_details = {
            "normalizedPath": str(normalized_path),
            "normalizationFormula": (
                "source=min(sourceSize-1,floor(((2*target+1)*sourceSize)/(2*targetSize)))"
            ),
            "topologyGuidePath": str(topology_guide),
            "topologyGuideSha256": sha256_file(topology_guide),
            "boundaryBandWidth": int(profile["boundaryBandWidth"]),
            "reconstruction": "mother material samples + directional boundary strips + authoritative topology alpha",
        }
    else:
        normalized = normalize_nearest(raw, (1536, 1536))
        normalized_path = output_root / "model" / "ModelOnly-Normalized-1536.png"
        write_image(normalized_path, normalized, force)
        crop = normalized.crop((256, 256, 1280, 1280))
        crop_path = output_root / "model" / "CentralCrop-1024.png"
        write_image(crop_path, crop, force)
        review_masks = slice_b_masks(crop)
        route_details = {
            "normalizedPath": str(normalized_path),
            "normalizationFormula": (
                "source=min(sourceSize-1,floor(((2*target+1)*sourceSize)/(2*targetSize)))"
            ),
            "cropPath": str(crop_path),
            "cropRect": [256, 256, 1024, 1024],
            "overscanMaskMatrix": B_OVERSCAN_MASKS,
            "centralMaskMatrix": CANONICAL_SWASTIKA,
        }

    review_masks, ownership = apply_candidate_mode(profile, review_masks, trusted_mask_root)
    runtime_masks, stress_masks, stress_record = save_common_outputs(
        output_root, review_masks, profile, force
    )
    verify_common_outputs(
        output_root,
        review_masks,
        runtime_masks,
        int(profile["runtimeSize"]),
        profile["runtimeSampling"],
    )
    review_adjacency = adjacency_metrics(review_masks)

    runtime_adjacency = adjacency_metrics(runtime_masks)
    png_paths = sorted(output_root.rglob("*.png"))
    png_records = [image_record(path, output_root) for path in png_paths]
    if not all(record["opaque"] for record in png_records):
        raise AssertionError("Every pipeline PNG must be fully opaque")
    if sha256_file(source_path) != source_hash_before:
        raise AssertionError("Protected topology source changed during finalize")

    manifest = {
        "schema": SCHEMA,
        "status": "ready-for-user-visual-review",
        "profileId": profile["id"],
        "route": profile["route"],
        "outputRoot": str(output_root),
        "sourceTopologyPath": str(source_path),
        "sourceTopologySha256": source_hash_before,
        "promptPath": str(prompt_path),
        "promptSha256": sha256_file(prompt_path),
        "modelProvenance": model_provenance,
        "rawPath": str(raw_target),
        "rawSha256": sha256_file(raw_target),
        "rawSize": list(raw.size),
        "routeDetails": route_details,
        "candidateMode": profile["candidateMode"],
        "protectedReviewWidth": int(profile["protectedReviewWidth"]),
        "crossoverWidth": int(profile["crossoverWidth"]),
        "pixelOwnershipByMask": ownership,
        "canonicalSwastika": CANONICAL_SWASTIKA,
        "reviewMaskCount": 16,
        "runtimeMaskCount": 16,
        "runtimeTileSize": int(profile["runtimeSize"]),
        "runtimeMaskDirectory": f"Runtime{profile['runtimeSize']}",
        "runtimeSamplingMethod": profile["runtimeSampling"],
        "stressRuntimeTileSize": STRESS_RUNTIME_TILE_SIZE,
        "stressAtlas": stress_record,
        "reviewAdjacency": review_adjacency,
        "runtimeAdjacency": runtime_adjacency,
        "seamSafetyClaimed": False,
        "internalTopologyVisualReviewPending": True,
        "visualInspectionPerformed": False,
        "mechanicalQaStatus": "passed-mechanical-qa",
        "generatedPngFiles": png_records,
        "completedAt": utc_now(),
    }
    write_json(output_root / "candidate" / "manifest.json", manifest, force)
    descriptor = brush_import_descriptor(profile)
    validate_brush_package(output_root, descriptor, manifest)
    write_json(output_root / "candidate" / "BrushImport.json", descriptor, force)
    ready = {
        "schema": SCHEMA,
        "status": "ready-for-user-visual-review",
        "profileId": profile["id"],
        "route": profile["route"],
        "manifestPath": str(output_root / "candidate" / "manifest.json"),
        "manifestSha256": sha256_file(output_root / "candidate" / "manifest.json"),
        "mechanicalQaPassed": True,
        "modelProvenanceEnforced": bool(model_provenance["enforced"]),
        "seamSafetyClaimed": False,
        "visualInspectionPerformed": False,
        "humanReviewRequired": True,
    }
    write_json(output_root / "evidence" / "ready.json", ready, force)
    return manifest


def validate(output_root: Path) -> dict:
    output_root = output_root.resolve()
    manifest_path = output_root / "candidate" / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    failures: list[str] = []
    descriptor_path = output_root / "candidate" / "BrushImport.json"
    if not descriptor_path.is_file():
        failures.append("missing:candidate/BrushImport.json")
    else:
        try:
            validate_brush_package(output_root,
                json.loads(descriptor_path.read_text(encoding="utf-8")), manifest)
        except Exception as exc:
            failures.append("brush-import:" + str(exc))
    for record in manifest.get("generatedPngFiles", []):
        path = output_root / record["relativePath"]
        if not path.exists():
            failures.append(f"missing:{record['relativePath']}")
            continue
        if sha256_file(path) != record["sha256"]:
            failures.append(f"hash:{record['relativePath']}")
            continue
        with Image.open(path) as opened:
            rgba = opened.convert("RGBA")
            if list(rgba.size) != [record["width"], record["height"]]:
                failures.append(f"size:{record['relativePath']}")
            if decoded_rgba_sha256(rgba) != record["decodedRgbaSha256"]:
                failures.append(f"pixels:{record['relativePath']}")
            if rgba.getchannel("A").getextrema() != (255, 255):
                failures.append(f"alpha:{record['relativePath']}")

    try:
        source_path = Path(manifest["sourceTopologyPath"])
        if sha256_file(source_path) != manifest["sourceTopologySha256"]:
            failures.append("source-topology-hash")
        prompt_path = Path(manifest["promptPath"])
        if sha256_file(prompt_path) != manifest["promptSha256"]:
            failures.append("prompt-hash")

        with Image.open(manifest["rawPath"]) as opened:
            raw = opened.convert("RGBA")
            raw.load()
        if raw.width != raw.height:
            failures.append("raw-not-square")
        if raw.getchannel("A").getextrema() != (255, 255):
            failures.append("raw-not-opaque")
        target_size = (1024, 1024) if manifest["route"] == "A" else (1536, 1536)
        reconstructed_normalized = normalize_nearest(raw, target_size)
        normalized_path = Path(manifest["routeDetails"]["normalizedPath"])
        with Image.open(normalized_path) as opened:
            normalized = opened.convert("RGBA")
            normalized.load()
        if normalized.tobytes() != reconstructed_normalized.tobytes():
            failures.append("raw-to-normalized")

        review_masks: dict[int, Image.Image] = {}
        runtime_masks: dict[int, Image.Image] = {}
        runtime_size = int(manifest.get("runtimeTileSize", 32))
        runtime_directory = manifest.get("runtimeMaskDirectory", f"Runtime{runtime_size}")
        runtime_method = manifest.get("runtimeSamplingMethod", "center-point")
        for mask in range(16):
            with Image.open(output_root / "candidate" / "Review256" / f"Mask-{mask:02d}.png") as opened:
                review_masks[mask] = opened.convert("RGBA")
                review_masks[mask].load()
            with Image.open(
                output_root / "candidate" / runtime_directory / f"Mask-{mask:02d}.png"
            ) as opened:
                runtime_masks[mask] = opened.convert("RGBA")
                runtime_masks[mask].load()
        verify_common_outputs(
            output_root, review_masks, runtime_masks, runtime_size, runtime_method
        )

        stress_masks = runtime_masks
        if runtime_size != STRESS_RUNTIME_TILE_SIZE:
            stress_masks = {}
            for mask in range(16):
                with Image.open(
                    output_root / "candidate" / "Runtime32" / f"Mask-{mask:02d}.png"
                ) as opened:
                    stress_masks[mask] = opened.convert("RGBA")
                    stress_masks[mask].load()
                if stress_masks[mask].tobytes() != runtime_center_sample(
                        review_masks[mask], STRESS_RUNTIME_TILE_SIZE).tobytes():
                    failures.append(f"stress-runtime-sampling:{mask}")

        if "stressMaps" in manifest or "stressFormula" in manifest:
            failures.append("legacy-stress-contract")
        stress_record = manifest.get("stressAtlas", {})
        stress_path = output_root / stress_record.get("relativePath", "")
        with Image.open(stress_path) as opened:
            actual_stress_atlas = opened.convert("RGBA")
            actual_stress_atlas.load()
        failures.extend(stress_atlas_failures(actual_stress_atlas, stress_record, stress_masks))

        if adjacency_metrics(review_masks) != manifest["reviewAdjacency"]:
            failures.append("review-adjacency")
        if adjacency_metrics(runtime_masks) != manifest["runtimeAdjacency"]:
            failures.append("runtime-adjacency")
        for ownership in manifest.get("pixelOwnershipByMask", []):
            total = (
                int(ownership.get("routeDerivedPixels", 0))
                + int(ownership.get("historicalPixels", 0))
                + int(ownership.get("fallbackPixels", 0))
            )
            if total != 65536 or int(ownership.get("fallbackPixels", 0)) != 0:
                failures.append(f"pixel-ownership:{ownership.get('mask')}")

        if manifest["route"] == "A":
            with Image.open(output_root / "candidate" / "Main-Board-1024.png") as opened:
                main_board = opened.convert("RGBA")
                main_board.load()
            if main_board.tobytes() != normalized.tobytes():
                failures.append("a-main-board")
        else:
            crop_path = Path(manifest["routeDetails"]["cropPath"])
            with Image.open(crop_path) as opened:
                crop = opened.convert("RGBA")
                crop.load()
            if crop.tobytes() != normalized.crop((256, 256, 1280, 1280)).tobytes():
                failures.append("b-central-crop")

        model_provenance = manifest.get("modelProvenance", {})
        if model_provenance.get("enforced"):
            model_call_path = Path(model_provenance["path"])
            if sha256_file(model_call_path) != model_provenance["sha256"]:
                failures.append("model-call-hash")
    except Exception as exc:
        failures.append(f"derived-check:{exc}")
    result = {
        "schema": SCHEMA,
        "status": "pass" if not failures else "fail",
        "outputRoot": str(output_root),
        "checkedPngCount": len(manifest.get("generatedPngFiles", [])),
        "failures": failures,
        "validatedAt": utc_now(),
    }
    if failures:
        raise RuntimeError(json.dumps(result, ensure_ascii=False, indent=2))
    return result


def build_overview(output_root: Path, candidate_args: list[str], columns: int, force: bool) -> dict:
    output_root = assert_safe_output_root(output_root)
    if not candidate_args:
        raise ValueError("Overview requires at least one --candidate ID=PATH")
    if columns < 1:
        raise ValueError("Overview columns must be positive")
    candidates: list[tuple[str, Path, Image.Image]] = []
    for value in candidate_args:
        if "=" not in value:
            raise ValueError(f"Candidate must use ID=PATH: {value}")
        candidate_id, raw_path = value.split("=", 1)
        path = Path(raw_path).resolve()
        with Image.open(path) as opened:
            image = opened.convert("RGBA")
            image.load()
        if image.width != image.height or image.getchannel("A").getextrema() != (255, 255):
            raise ValueError(f"Overview candidate must be square and opaque: {path}")
        candidates.append((candidate_id.strip(), path, image))

    panel_size, label_height, gutter = 512, 36, 12
    rows = math.ceil(len(candidates) / columns)
    width = gutter + columns * (panel_size + gutter)
    height = gutter + rows * (panel_size + label_height + gutter)
    overview = Image.new("RGBA", (width, height), (31, 38, 46, 255))
    draw = ImageDraw.Draw(overview)
    panels = []
    for index, (candidate_id, path, image) in enumerate(candidates):
        column, row = index % columns, index // columns
        x = gutter + column * (panel_size + gutter)
        y = gutter + row * (panel_size + label_height + gutter)
        thumbnail = image.resize((panel_size, panel_size), Image.Resampling.NEAREST)
        overview.paste(thumbnail, (x, y))
        draw.text((x + 4, y + panel_size + 8), candidate_id, fill=(240, 244, 248, 255))
        panels.append(
            {
                "panelIndex": index,
                "candidateId": candidate_id,
                "sourcePath": str(path),
                "sourceSha256": sha256_file(path),
            }
        )
    overview_path = output_root / "overview" / "All-Candidates-Swastika.png"
    write_image(overview_path, overview, force)
    manifest = {
        "schema": SCHEMA,
        "status": "review-overview-only",
        "outputPath": str(overview_path),
        "outputSha256": sha256_file(overview_path),
        "columns": columns,
        "rows": rows,
        "panelSize": panel_size,
        "panels": panels,
        "runtimeSourceAllowed": False,
    }
    write_json(output_root / "overview" / "manifest.json", manifest, force)
    return manifest


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)
    for name in ("prepare", "finalize", "package", "repackage"):
        sub = subparsers.add_parser(name)
        sub.add_argument("--profile", required=True, type=Path)
        sub.add_argument("--output-root", required=True, type=Path)
        sub.add_argument("--force", action="store_true")
        if name == "finalize":
            sub.add_argument("--raw-image", required=True, type=Path)
            sub.add_argument("--topology-guide", type=Path)
            sub.add_argument("--model-call", type=Path)
            sub.add_argument("--allow-missing-model-call", action="store_true")
            sub.add_argument("--trusted-mask-root", type=Path)
        if name == "repackage":
            sub.add_argument("--source-root", required=True, type=Path)
    validate_parser = subparsers.add_parser("validate")
    validate_parser.add_argument("--output-root", required=True, type=Path)
    overview_parser = subparsers.add_parser("overview")
    overview_parser.add_argument("--output-root", required=True, type=Path)
    overview_parser.add_argument("--candidate", action="append", required=True)
    overview_parser.add_argument("--columns", type=int, default=4)
    overview_parser.add_argument("--force", action="store_true")
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    try:
        if args.command == "prepare":
            result = prepare(args.profile.resolve(), args.output_root, args.force)
        elif args.command == "finalize":
            result = finalize(
                args.profile.resolve(), args.output_root, args.raw_image,
                args.topology_guide, args.model_call, args.allow_missing_model_call,
                args.trusted_mask_root, args.force
            )
        elif args.command == "package":
            result = package_brush(args.profile.resolve(), args.output_root, args.force)
        elif args.command == "repackage":
            result = repackage_runtime(
                args.profile.resolve(), args.source_root, args.output_root
            )
        elif args.command == "validate":
            result = validate(args.output_root)
        else:
            result = build_overview(args.output_root, args.candidate, args.columns, args.force)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except Exception as exc:  # CLI boundary includes exact actionable message
        print(f"DUAL_GRID_PIPELINE_FAILED: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

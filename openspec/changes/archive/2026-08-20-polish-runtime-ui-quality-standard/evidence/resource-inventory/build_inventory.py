#!/usr/bin/env python3
"""Rebuild the task 1.4 runtime UI resource inventory and review montage."""

from __future__ import annotations

import hashlib
import json
import math
import re
import subprocess
from collections import Counter, defaultdict
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[5]
CHANGE = ROOT / "openspec/changes/polish-runtime-ui-quality-standard"
EVIDENCE = CHANGE / "evidence/resource-inventory"
SET_ID = "sunny-orchard-painted"
SOURCE_ROOT = ROOT / f"Assets/UI/Art/Sources/{SET_ID}"
RUNTIME_ROOT = ROOT / f"Assets/UI/Art/Runtime/{SET_ID}"
MANIFEST_PATH = SOURCE_ROOT / "art_manifest.json"
ARTSET_PATH = ROOT / "Assets/UI/Art/Sets/SunnyOrchardPaintedRuntimeUiArtSet.asset"
ARTSET_META = ARTSET_PATH.with_suffix(ARTSET_PATH.suffix + ".meta")
THEME_PATH = ROOT / "Assets/UI/Theme/ReleaseRuntimeUiTheme.asset"


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def meta_guid(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    match = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", text, re.MULTILINE)
    if not match:
        raise ValueError(f"No Unity GUID in {rel(path)}")
    return match.group(1)


def scalar(text: str, key: str, cast=int):
    match = re.search(rf"^\s*{re.escape(key)}:\s*([^\r\n]+)$", text, re.MULTILINE)
    if not match:
        return None
    value = match.group(1).strip()
    return cast(value) if cast else value


def parse_border(text: str) -> list[float]:
    match = re.search(
        r"^\s*spriteBorder:\s*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)\}",
        text,
        re.MULTILINE,
    )
    return [float(value) for value in match.groups()] if match else []


def parse_importer(meta_path: Path) -> dict:
    text = meta_path.read_text(encoding="utf-8")
    platforms = []
    for name in ("DefaultTexturePlatform", "Standalone", "WebGL"):
        block = re.search(
            rf"buildTarget:\s*{name}(.*?)(?=\n\s*-\s+serializedVersion:|\n\s+spriteSheet:)",
            text,
            re.DOTALL,
        )
        platforms.append(
            {
                "buildTarget": name,
                "textureCompression": scalar(block.group(1), "textureCompression") if block else None,
                "overridden": scalar(block.group(1), "overridden") if block else None,
            }
        )
    return {
        "textureType": scalar(text, "textureType"),
        "spriteMode": scalar(text, "spriteMode"),
        "spriteMeshType": scalar(text, "spriteMeshType"),
        "sRGBTexture": scalar(text, "sRGBTexture"),
        "alphaUsage": scalar(text, "alphaUsage"),
        "alphaIsTransparency": scalar(text, "alphaIsTransparency"),
        "enableMipMap": scalar(text, "enableMipMap"),
        "isReadable": scalar(text, "isReadable"),
        "filterMode": scalar(text, "filterMode"),
        "wrapU": scalar(text, "wrapU"),
        "wrapV": scalar(text, "wrapV"),
        "wrapW": scalar(text, "wrapW"),
        "spritePixelsToUnits": scalar(text, "spritePixelsToUnits", float),
        "spriteBorder": parse_border(text),
        "platforms": platforms,
        "userData": scalar(text, "userData", None),
    }


def rgba_distance(a: tuple[int, ...], b: tuple[int, ...]) -> float:
    return math.sqrt(sum((a[index] - b[index]) ** 2 for index in range(4)))


def premultiplied_rgba(rgba: tuple[int, int, int, int]) -> tuple[float, float, float, float]:
    alpha = rgba[3] / 255.0
    return (rgba[0] * alpha, rgba[1] * alpha, rgba[2] * alpha, rgba[3])


def image_metrics(path: Path, geometry: str, border: int, safe_inset: int) -> dict:
    image = Image.open(path).convert("RGBA")
    width, height = image.size
    pixels = list(image.get_flattened_data())
    visible = [(index % width, index // width, rgba) for index, rgba in enumerate(pixels) if rgba[3] > 0]
    alpha = [rgba[3] for rgba in pixels]
    alpha_sum = sum(alpha)
    if visible:
        xs = [item[0] for item in visible]
        ys = [item[1] for item in visible]
        bbox = [min(xs), min(ys), max(xs) + 1, max(ys) + 1]
        bbox_width = bbox[2] - bbox[0]
        bbox_height = bbox[3] - bbox[1]
    else:
        bbox = [0, 0, 0, 0]
        bbox_width = bbox_height = 0
    if alpha_sum:
        centroid_x = sum((index % width + 0.5) * value for index, value in enumerate(alpha)) / alpha_sum
        centroid_y = sum((index // width + 0.5) * value for index, value in enumerate(alpha)) / alpha_sum
    else:
        centroid_x = centroid_y = 0.0
    outer_indexes = set()
    for x in range(width):
        outer_indexes.add(x)
        outer_indexes.add((height - 1) * width + x)
    for y in range(height):
        outer_indexes.add(y * width)
        outer_indexes.add(y * width + width - 1)
    low_alpha_chroma = 0
    transparent_rgb = 0
    for rgba in pixels:
        r, g, b, a = rgba
        if a == 0 and (r or g or b):
            transparent_rgb += 1
        if 0 < a < 48 and max(r, g, b) - min(r, g, b) >= 72:
            low_alpha_chroma += 1
    metrics = {
        "width": width,
        "height": height,
        "aspect": round(width / height, 6),
        "alphaMin": min(alpha),
        "alphaMax": max(alpha),
        "opaquePixels": sum(value == 255 for value in alpha),
        "visiblePixels": len(visible),
        "semiTransparentPixels": sum(0 < value < 255 for value in alpha),
        "lowAlphaPixelsLt48": sum(0 < value < 48 for value in alpha),
        "transparentPixelsWithRgb": transparent_rgb,
        "visibleKeyMagentaPixelCount": sum(
            1 for rgba in pixels if rgba[3] > 0 and rgba[:3] == (255, 0, 255)
        ),
        "visibleKeyMagentaCoordinates": [
            [index % width, index // width, rgba[3]]
            for index, rgba in enumerate(pixels)
            if rgba[3] > 0 and rgba[:3] == (255, 0, 255)
        ][:16],
        "lowAlphaHighChromaPixels": low_alpha_chroma,
        "alphaBoundingBox": bbox,
        "alphaBoundingBoxSize": [bbox_width, bbox_height],
        "alphaBoundingBoxRatio": [round(bbox_width / width, 6), round(bbox_height / height, 6)],
        "alphaOccupancy": round(len(visible) / (width * height), 6),
        "alphaMassCentroid": [round(centroid_x, 4), round(centroid_y, 4)],
        "alphaMassCenterOffset": [round(centroid_x - width / 2, 4), round(centroid_y - height / 2, 4)],
        "visibleOuterEdgePixels": sum(alpha[index] > 0 for index in outer_indexes),
        "safeInsetContainsAlphaBounds": (
            bbox[0] >= safe_inset
            and bbox[1] >= safe_inset
            and bbox[2] <= width - safe_inset
            and bbox[3] <= height - safe_inset
        ) if visible and safe_inset > 0 else None,
    }
    if geometry.lower().replace("-", "") == "nineslice" and border > 0:
        boundaries = {}
        for name, left, right, axis in (
            ("left", border - 1, border, "x"),
            ("right", width - border - 1, width - border, "x"),
            ("top", border - 1, border, "y"),
            ("bottom", height - border - 1, height - border, "y"),
        ):
            pairs = []
            if axis == "x":
                for y in range(height):
                    pairs.append((image.getpixel((left, y)), image.getpixel((right, y))))
            else:
                for x in range(width):
                    pairs.append((image.getpixel((x, left)), image.getpixel((x, right))))
            one_sided = sum((a[3] == 0) != (b[3] == 0) for a, b in pairs)
            one_sided_alpha16 = sum((a[3] >= 16) != (b[3] >= 16) for a, b in pairs)
            one_sided_alpha48 = sum((a[3] >= 48) != (b[3] >= 48) for a, b in pairs)
            one_sided_significant = sum(
                max(a[3], b[3]) >= 48 and min(a[3], b[3]) < 16 for a, b in pairs
            )
            alpha_delta = max(abs(a[3] - b[3]) for a, b in pairs)
            rgba_delta = max(rgba_distance(a, b) for a, b in pairs)
            premultiplied_delta = max(
                rgba_distance(premultiplied_rgba(a), premultiplied_rgba(b)) for a, b in pairs
            )
            boundaries[name] = {
                "oneSidedTransparentPairsRaw": one_sided,
                "oneSidedCoveredPairsAtAlpha16": one_sided_alpha16,
                "oneSidedCoveredPairsAtAlpha48": one_sided_alpha48,
                "oneSidedSignificantCoveragePairs": one_sided_significant,
                "maxAlphaDelta": alpha_delta,
                "maxRgbaDistance": round(rgba_delta, 3),
                "maxPremultipliedRgbaDistance": round(premultiplied_delta, 3),
            }
        center = image.crop((border, border, width - border, height - border))
        center_alpha = list(center.getchannel("A").get_flattened_data())
        metrics["nineSlice"] = {
            "border": border,
            "centerSize": [width - 2 * border, height - 2 * border],
            "centerAlphaMin": min(center_alpha),
            "centerAlphaMax": max(center_alpha),
            "boundaries": boundaries,
        }
    return metrics


def serialized_reference_hits(guids: list[str]) -> dict[str, list[str]]:
    hits = {}
    globs = ("*.asset", "*.unity", "*.prefab", "*.mat", "*.controller", "*.anim", "*.overridecontroller")
    for guid in guids:
        command = ["rg", "-l", "--fixed-strings", guid, "Assets"]
        for glob in globs:
            command.extend(("-g", glob))
        result = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, check=False)
        if result.returncode not in (0, 1):
            raise RuntimeError(result.stderr.strip() or f"rg failed for {guid}")
        hits[guid] = sorted(line.replace("\\", "/") for line in result.stdout.splitlines() if line)
    return hits


def family(semantic_id: str) -> str:
    if semantic_id.startswith("icon."):
        return "icon"
    if semantic_id.startswith("indicator."):
        return "indicator"
    if semantic_id.startswith("marker."):
        return "marker"
    if semantic_id.startswith("ornament."):
        return "ornament"
    if semantic_id.startswith("illustration."):
        return "illustration"
    if semantic_id.startswith("surface."):
        return "surface"
    if semantic_id.startswith("action."):
        return "action"
    if semantic_id.startswith("slot."):
        return "slot"
    return "other"


def build_inventory() -> dict:
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    artset_text = ARTSET_PATH.read_text(encoding="utf-8")
    theme_text = THEME_PATH.read_text(encoding="utf-8")
    artset_guid = meta_guid(ARTSET_META)
    entries = []
    runtime_paths = set()
    source_paths = set()
    for binding in sorted(manifest["bindings"], key=lambda item: item["slot"]):
        source_path = ROOT / binding["source"]
        runtime_path = ROOT / binding["runtime"]
        runtime_meta_path = Path(str(runtime_path) + ".meta")
        runtime_paths.add(runtime_path.resolve())
        source_paths.add(source_path.resolve())
        runtime_metrics = image_metrics(
            runtime_path,
            binding["geometry"],
            int(binding["slice_border"]),
            int(binding["safe_inset"]),
        )
        source_metrics = image_metrics(
            source_path,
            binding["geometry"],
            int(binding["slice_border"]),
            int(binding["safe_inset"]),
        )
        entry = {
            "slot": binding["slot"],
            "semanticId": binding["semantic_id"],
            "stem": binding["stem"],
            "family": family(binding["semantic_id"]),
            "geometry": binding["geometry"],
            "manifestDimensions": [binding["width"], binding["height"]],
            "sliceBorder": binding["slice_border"],
            "safeInset": binding["safe_inset"],
            "pixelsPerLogicalUnit": binding["pixels_per_logical_unit"],
            "source": binding["source"],
            "runtime": binding["runtime"],
            "sourceSha256": sha256(source_path),
            "runtimeSha256": sha256(runtime_path),
            "manifestSourceSha256Matches": sha256(source_path) == binding["sourceSha256"],
            "manifestRuntimeSha256Matches": sha256(runtime_path) == binding["runtimeSha256"],
            "runtimeGuid": meta_guid(runtime_meta_path),
            "manifestGuidMatches": meta_guid(runtime_meta_path) == binding["guid"],
            "importer": parse_importer(runtime_meta_path),
            "sourceMetrics": source_metrics,
            "runtimeMetrics": runtime_metrics,
            "sourceToRuntimeAspectDelta": round(abs(source_metrics["aspect"] - runtime_metrics["aspect"]), 6),
            "sourceToRuntimeAspectRelativeError": round(
                abs(source_metrics["aspect"] - runtime_metrics["aspect"]) / source_metrics["aspect"], 6
            ),
            "artSetGuidReferenceCount": artset_text.count(binding["guid"]),
        }
        entries.append(entry)

    guid_binding_counts = Counter(entry["runtimeGuid"] for entry in entries)
    for entry in entries:
        entry["expectedArtSetGuidReferenceCount"] = 2 * guid_binding_counts[entry["runtimeGuid"]]
        entry["artSetGuidReferenceCountMatches"] = (
            entry["artSetGuidReferenceCount"] == entry["expectedArtSetGuidReferenceCount"]
        )

    all_runtime_pngs = {path.resolve() for path in RUNTIME_ROOT.rglob("*.png")}
    all_source_pngs = {path.resolve() for path in SOURCE_ROOT.rglob("*.png")}
    review_assets = []
    for category in ("Approved", "Review"):
        category_root = ROOT / f"Assets/UI/Art/Sources/ReferenceBoards/{category}"
        for path in sorted(category_root.glob("*.png")):
            guid = meta_guid(Path(str(path) + ".meta"))
            review_assets.append(
                {
                    "category": category.lower(),
                    "path": rel(path),
                    "sha256": sha256(path),
                    "guid": guid,
                    "releaseSerializedReferences": [],
                }
            )
    review_reference_hits = serialized_reference_hits([item["guid"] for item in review_assets])
    for item in review_assets:
        item["releaseSerializedReferences"] = review_reference_hits[item["guid"]]

    ancillary_role = {
        "Assets/UI/Art/Sources/sunny-orchard-painted/art_manifest.json": "binding-manifest",
        "Assets/UI/Art/Sources/sunny-orchard-painted/export_sunny_orchard_painted.py": "deterministic-exporter",
        "Assets/UI/Art/Sources/sunny-orchard-painted/prompt-record.json": "generation-provenance",
        "Assets/UI/Art/Sources/sunny-orchard-painted/README.md": "source-ownership-documentation",
        "Assets/UI/Art/Sources/sunny-orchard-painted/icons/alignment-audit.md": "icon-optical-audit",
        "Assets/UI/Art/Sources/sunny-orchard-painted/icons/prompt-record.md": "icon-generation-provenance",
        "Assets/UI/Art/Runtime/sunny-orchard-painted/README.md": "runtime-contract-documentation",
        "Assets/UI/Art/Sources/ReferenceBoards/Approved/README.md": "approved-board-documentation",
    }
    ancillary_resources = []
    for root in (
        SOURCE_ROOT,
        RUNTIME_ROOT,
        ROOT / "Assets/UI/Art/Sources/ReferenceBoards/Approved",
        ROOT / "Assets/UI/Art/Sources/ReferenceBoards/Review",
    ):
        for path in sorted(root.rglob("*")):
            if not path.is_file() or path.suffix.lower() in {".png", ".meta"}:
                continue
            meta_path = Path(str(path) + ".meta")
            path_text = rel(path)
            ancillary_resources.append(
                {
                    "path": path_text,
                    "sha256": sha256(path),
                    "guid": meta_guid(meta_path) if meta_path.exists() else None,
                    "role": ancillary_role.get(path_text, "unclassified"),
                }
            )

    families = defaultdict(list)
    unique_by_runtime = {}
    for entry in entries:
        unique_by_runtime.setdefault(entry["runtime"], entry)
    for entry in unique_by_runtime.values():
        if entry["family"] in {"icon", "indicator", "marker", "ornament"}:
            metrics = entry["runtimeMetrics"]
            families[entry["family"]].append(
                {
                    "semanticId": entry["semanticId"],
                    "bboxRatio": metrics["alphaBoundingBoxRatio"],
                    "occupancy": metrics["alphaOccupancy"],
                    "centerOffset": metrics["alphaMassCenterOffset"],
                }
            )

    set_assets = []
    for path in sorted((ROOT / "Assets/UI/Art/Sets").glob("*RuntimeUiArtSet.asset")):
        set_assets.append({"path": rel(path), "guid": meta_guid(Path(str(path) + ".meta"))})
    legacy_manifest_path = ROOT / "Assets/UI/Art/Sources/sunny-orchard/art_manifest.json"
    legacy_manifest = json.loads(legacy_manifest_path.read_text(encoding="utf-8"))
    painted_by_slot = {entry["slot"]: entry for entry in manifest["bindings"]}
    shared_rows = []
    unexpected_external_rows = []
    mirror_fields = (
        "semantic_id", "stem", "geometry", "size", "width", "height", "source", "runtime",
        "sourceSha256", "runtimeSha256", "guid", "slice_border", "safe_inset", "pixels_per_logical_unit",
    )
    for row in legacy_manifest["bindings"]:
        shared_from = row.get("shared_from_set", "")
        if shared_from:
            owner = painted_by_slot.get(row["slot"])
            mismatches = [field for field in mirror_fields if owner is None or row.get(field) != owner.get(field)]
            shared_rows.append(
                {
                    "slot": row["slot"],
                    "semanticId": row["semantic_id"],
                    "sharedFromSet": shared_from,
                    "ownerMirrorFieldMismatches": mismatches,
                }
            )
        elif not row["runtime"].startswith("Assets/UI/Art/Runtime/sunny-orchard/"):
            unexpected_external_rows.append(
                {"slot": row["slot"], "semanticId": row["semantic_id"], "runtime": row["runtime"]}
            )

    return {
        "schema": "fruit-defense-runtime-ui-resource-inventory-v1",
        "change": "polish-runtime-ui-quality-standard",
        "setId": manifest["setId"],
        "revision": manifest["revision"],
        "activeArtSet": {
            "asset": rel(ARTSET_PATH),
            "guid": artset_guid,
            "theme": rel(THEME_PATH),
            "themeReferencesActiveGuid": artset_guid in theme_text,
        },
        "counts": {
            "manifestBindings": len(entries),
            "uniqueRuntimeExports": len(runtime_paths),
            "uniqueSourcePngs": len(source_paths),
            "productionRuntimePngsOnDisk": len(all_runtime_pngs),
            "productionSourcePngsOnDisk": len(all_source_pngs),
            "duplicateBindingRuntimePaths": {
                path: count for path, count in Counter(entry["runtime"] for entry in entries).items() if count > 1
            },
        },
        "ownership": {
            "unboundProductionRuntimePngs": sorted(rel(Path(path)) for path in all_runtime_pngs - runtime_paths),
            "unboundProductionSourcePngs": sorted(rel(Path(path)) for path in all_source_pngs - source_paths),
            "missingRuntimePngs": sorted(rel(Path(path)) for path in runtime_paths - all_runtime_pngs),
            "missingSourcePngs": sorted(rel(Path(path)) for path in source_paths - all_source_pngs),
            "productionArtSets": set_assets,
            "reviewAssets": review_assets,
            "ancillaryResources": ancillary_resources,
            "unclassifiedAncillaryResources": [
                item["path"] for item in ancillary_resources if item["role"] == "unclassified"
            ],
            "mixedSetAudit": {
                "activePaintedRowsWithSharedFromSet": [
                    {"slot": row["slot"], "semanticId": row["semantic_id"], "sharedFromSet": row.get("shared_from_set")}
                    for row in manifest["bindings"] if row.get("shared_from_set")
                ],
                "sunnyOrchardSharedRows": shared_rows,
                "sunnyOrchardUnexpectedExternalRows": unexpected_external_rows,
            },
        },
        "familyOpticalMetrics": dict(families),
        "bindings": entries,
    }


def draw_montage(inventory: dict) -> None:
    entries_by_runtime = {}
    for entry in inventory["bindings"]:
        entries_by_runtime.setdefault(entry["runtime"], entry)
    entries = list(entries_by_runtime.values())
    cell_w, cell_h = 240, 190
    cols = 5
    rows = math.ceil(len(entries) / cols)
    image = Image.new("RGB", (cols * cell_w, rows * cell_h + 70), "#F2E8D5")
    draw = ImageDraw.Draw(image)
    font = ImageFont.load_default()
    draw.text((18, 14), "sunny-orchard-painted @1 | 49 bindings / 47 unique runtime exports", fill="#4B321E", font=font)
    draw.text((18, 34), "Checkerboard is evidence-only; labels are semantic IDs, not baked production text.", fill="#6B5137", font=font)
    for index, entry in enumerate(entries):
        col, row = index % cols, index // cols
        x0, y0 = col * cell_w, 70 + row * cell_h
        draw.rectangle((x0 + 4, y0 + 4, x0 + cell_w - 4, y0 + cell_h - 4), fill="#FFF6E0", outline="#C6A77A")
        preview = Image.open(ROOT / entry["runtime"]).convert("RGBA")
        max_w, max_h = 184, 132
        scale = min(max_w / preview.width, max_h / preview.height)
        resized = preview.resize((max(1, round(preview.width * scale)), max(1, round(preview.height * scale))), Image.Resampling.LANCZOS)
        checker = Image.new("RGBA", resized.size, "#F8EEDB")
        checker_draw = ImageDraw.Draw(checker)
        tile = 10
        for cy in range(0, resized.height, tile):
            for cx in range(0, resized.width, tile):
                if (cx // tile + cy // tile) % 2:
                    checker_draw.rectangle((cx, cy, cx + tile - 1, cy + tile - 1), fill="#D7C5AA")
        checker.alpha_composite(resized)
        px = x0 + (cell_w - resized.width) // 2
        py = y0 + 12 + (max_h - resized.height) // 2
        image.paste(checker.convert("RGB"), (px, py))
        label = f"{entry['slot']:02d} {entry['semanticId']}"
        draw.text((x0 + 10, y0 + 150), label, fill="#4B321E", font=font)
        draw.text((x0 + 10, y0 + 166), f"{entry['geometry']} {entry['runtimeMetrics']['width']}x{entry['runtimeMetrics']['height']}", fill="#6B5137", font=font)
    image.save(EVIDENCE / "active-49-resource-gallery.png", optimize=True)


def main() -> None:
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    inventory = build_inventory()
    (EVIDENCE / "resource-inventory.json").write_text(
        json.dumps(inventory, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    draw_montage(inventory)
    print(json.dumps(inventory["counts"], ensure_ascii=False, indent=2))
    print(json.dumps(inventory["ownership"], ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()

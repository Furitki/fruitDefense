## Context

The deterministic art pipeline builds four 1024×1024 stress maps from one set of sixteen Runtime32 masks and four fixed 33×33 logical vertex functions. Repeating each scenario over 32×32 tiles is visually bulky but adds no new pixels for a deterministic tile pair. Exact horizontal and vertical socket enumeration is a separate numeric check and must remain separate in meaning even when its result is referenced by the same manifest.

## Goals / Non-Goals

**Goals:**

- Emit exactly one pressure-test PNG per candidate while preserving native Runtime32 tile pixels and all four scenario types.
- Reduce representative repetition density to 16×16 tiles per scenario without weakening independent adjacency enumeration.
- Give the one image a fixed, machine-decodable layout and enough manifest metadata to locate and identify every scenario.
- Reconstruct the whole image and each quadrant independently during validation so failures remain localizable.
- Reduce generated PNG count without weakening source, Runtime, atlas, adjacency, or ownership checks.

**Non-Goals:**

- Proving artistic quality, topology intent, or seamlessness from the combined image.
- Encoding all 128 legal adjacency pairs as additional visual panels.
- Changing Runtime32 masks, Unity imports, gameplay, persistence, scenes, WebGL output, or platform adapters.
- Adding labels, grid lines, gutters, scaling, compression changes, or decorative framing to the test pixels.

## Decisions

### Use a fixed 1024×1024 native-tile 2×2 atlas

Each scenario uses a 17×17 logical vertex field to place 16×16 native Runtime32 tiles, producing one 512×512 panel. The atlas places `pureLandform` at `[0,0,512,512]`, `landformWithCentralBaseHole` at `[512,0,512,512]`, `baseWithCentralLandformIsland` at `[0,512,512,512]`, and `diagonalMixed` at `[512,512,512,512]`, using PNG top-left coordinates.

The density reduction changes the number and arrangement of repeated tile placements, but never resizes a Runtime tile. Central ranges scale from `10..22` to `5..11`, checker blocks from 3 to 2 vertices, and the cross from `12..20` to `6..10`. Mechanical analysis confirms that the four scaled fields still contain all sixteen masks. Directly scaling the interim 2048 atlas was rejected because it would shrink Runtime tiles and could hide one-pixel defects.

### Make the manifest the legend

The manifest stores one `stressAtlas` object containing schema version, path, dimensions, panel size, layout, and one panel record per scenario. Every panel record carries its identifier, rectangle, public formula, decoded RGBA hash, and opacity result. The atlas itself remains pure terrain pixels.

### Validate both the complete atlas and each decoded panel

Validation rebuilds the four expected stress images from Runtime32, assembles an expected atlas, compares the whole decoded buffer, then crops the actual atlas by manifest rectangles and compares every crop to its corresponding expected scenario. Whole-image comparison catches layout or untracked-pixel changes; panel comparison names the broken scenario.

### Keep seam enumeration as independent evidence

The one image covers the same four representative scenes as before. The existing 64 horizontal and 64 vertical legal-pair enumeration remains in `reviewAdjacency` and `runtimeAdjacency`, with true mismatch counts and `seamSafetyClaimed=false`. The image does not visually duplicate that exhaustive data.

## Risks / Trade-offs

- [Fewer repetitions may reduce visual dwell time on one pair] → Keep 960 horizontal and 960 vertical visible placements across the four panels and retain exhaustive 64+64 pair checks independently.
- [Downstream scripts expect `Stress1024/*.png`] → Treat this as a deliberate output-contract break, document the new manifest lookup, and make validation reject the legacy multi-file contract for new manifests.
- [A quadrant may be swapped while remaining visually plausible] → Record exact rectangles and identifiers, then compare both whole-atlas and per-panel decoded bytes.
- [One corrupt file loses every visual scenario] → Preserve file hash plus decoded RGBA hash and fail validation immediately; source masks allow deterministic regeneration.

## Migration Plan

1. Add compact atlas assembly and manifest helpers with unit coverage.
2. Replace per-scenario writes and `stressMaps` with one atlas write and `stressAtlas` metadata.
3. Rebuild the four formulas on 17×17 logical vertex fields and assert aggregate 16-mask coverage.
4. Replace multi-file validation with whole-atlas and crop-level reconstruction checks.
5. Remove obsolete profile `stressNames`, update the formal pipeline, rebuild Design KB, and regenerate A/B regression evidence in a new versioned directory.
6. Roll back by reverting the script and documentation change; prior evidence directories remain immutable and readable by their original manifests.

## Open Questions

None. The fixed 16×16 density is sufficient for one-image review while native tile pixels and existing mechanical checks remain intact.

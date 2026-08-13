## Why

The Dual-Grid art pipeline currently emits four separate 1024×1024 stress images with 32×32 repeated tiles per scenario even though every repeated tile is selected from the same sixteen Runtime32 masks. Reviewers need one compact artifact that preserves native tile pixels and all four representative scenarios without carrying unnecessary repetition density.

## What Changes

- **BREAKING**: Replace the four files under `candidate/Stress1024/` with one fixed-layout `candidate/Stress-All-1024.png` artifact.
- Reduce each scenario from a 32×32 tile field to a proportional 16×16 field while keeping every placed Runtime tile at its native 32×32 pixels.
- Place the four 512×512 scenarios into fixed 2×2 quadrants, with no tile scaling, labels, gutters, overlays, or filtering inside the image.
- Record quadrant identifiers, pixel rectangles, formulas, and hashes in the candidate manifest.
- Make validation crop the single image back into four regions and independently rebuild every region from Runtime32 masks and the declared vertex functions.
- Keep exhaustive 64 horizontal and 64 vertical legal-pair seam checks as machine-readable evidence; the image remains representative visual evidence and does not claim seam safety.
- Update the formal art pipeline, automated tests, A/B regression evidence, and Design KB static snapshot.

## Capabilities

### New Capabilities
- `dual-grid-art-evidence`: Defines the deterministic single-image stress artifact, its lossless layout, manifest contract, and independent reconstruction validation.

### Modified Capabilities

None.

## Impact

- Affects `scripts/dual_grid_tile_pipeline.py`, the PowerShell wrapper only if its public contract needs clarification, profile compatibility handling, pipeline tests, generated evidence, and `docs/art/dual-grid-tile-generation-pipeline.md`.
- Removes four per-scenario stress PNG outputs from new runs and replaces them with one 1024×1024 PNG; downstream consumers that enumerate `candidate/Stress1024/*.png` or expect the interim 2048 atlas must adopt the manifest-declared atlas path, tile-grid size, and quadrant table.
- Adds no runtime dependency and changes no gameplay, persistence, Unity asset binding, scene flow, WebGL behavior, or mini-game platform status.

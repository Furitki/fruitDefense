# Editable UI art sources

Create one folder per stable art-set ID. Each production asset must have an editable lossless master here and an optimized export with the same semantic basename under `../../Runtime/<set-id>/`.

Preferred tool-neutral masters are SVG for vector-like components and OpenRaster (`.ora`) for layered raster work. A lossless PNG may accompany a master as a flattened review/export source, but generated review output by itself is not a production master.

Keep source notes, export scale, protected nine-slice borders, icon canvas/safe inset, and the destination runtime path in the set manifest. Temporary experiments belong outside `Assets/UI` and must never be referenced by a release asset.

The authoritative quality and replacement workflow is documented in [`docs/ui/ui-visual-system.md`](../../../../docs/ui/ui-visual-system.md#13-%E8%B4%A8%E9%87%8F%E6%A0%87%E5%87%86%E4%B8%8E%E6%9D%83%E5%A8%81%E5%B7%A5%E4%BD%9C%E6%B5%81). Edit the single reviewed master, run the set-owned deterministic exporter, preserve every existing runtime `.meta`, then run release validation before preview or activation. A source change is not complete while the manifest, runtime export, importer contract, optical metrics, or review gallery is stale.

Production trees must contain only classified masters and deterministic authoring support. Raw generation, rejected experiments, cache files, duplicate “final” variants, and review-only images belong outside a production set folder; no source or review GUID may enter a release dependency.

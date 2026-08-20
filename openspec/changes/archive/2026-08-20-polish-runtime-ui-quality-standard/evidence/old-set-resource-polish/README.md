# Sunny Orchard old production-candidate optical remediation

`sunny-orchard` remains a complete, non-active production candidate. The same optical quality contract used by the active painted set applies to it; the validator is not relaxed or scoped to the active set.

## Scope and method

The real release-validator log reported 11 failures across eight of the original 96 px icons. The existing lossless SVG recipes were corrected with deterministic affine transforms in `export_sunny_orchard.py`; no raster generation or image-generation model was used. This is the imagegen-skill boundary for an established repository-native vector icon system.

- alpha-mass centroid: at most 4 px from canvas center on either axis;
- common-family alpha major dimension: 60–72 px;
- legal/illegal drag-cue alpha short edge: at least 64 px;
- canvas and safe-inset contract remains 96 px / 12 px;
- silhouettes, palette, semantic distinction, paths, bindings, and runtime importer GUIDs remain stable.

Exact pre/post measurements are recorded in `metrics.json`. The review montage `sunny-orchard-optical-remediation-gallery.png` shows every changed export at 96 px, 32 px, 24 px, and grayscale. The production gallery was regenerated in place and reviewed as a whole.

## Determinism and preservation

Two consecutive executions of `python Assets/UI/Art/Sources/sunny-orchard/export_sunny_orchard.py` produced zero file-hash differences across the source set, runtime set, ArtSet asset, manifest, and production gallery.

- exactly eight owned SVG masters and eight runtime PNG exports changed visually;
- all 30 other owned runtime PNG hashes are unchanged;
- all runtime PNG `.meta` byte hashes and GUIDs are unchanged;
- `SunnyOrchardRuntimeUiArtSet.asset` and its `.meta` are unchanged;
- existing source/runtime `.meta` paths remain present and all GUIDs remain unchanged;
- the exporter now creates `.meta` only when missing and never rewrites an existing importer record;
- shared slots 40–48 still point to the painted owner and were not modified.

The manifest and galleries changed only to record/show the eight new runtime hashes. Unity, browser, and WebGL build were intentionally not started in this resource-static pass; the owning validation agent performs the merged release-validator/P0 run after this handoff.

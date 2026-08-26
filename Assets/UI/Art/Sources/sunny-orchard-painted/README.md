# Sunny Orchard Painted sources

`sunny-orchard-painted@1` is the user-approved active painted production set derived from the approved Sunny Orchard board and the reviewed component proof v2.

Production source ownership is split into:

- `surfaces/`: reviewed surface/action/slot masters plus the section ribbon and illustration frame;
- `icons/`: 22 image-generated, reviewed state/common-icon masters plus three deterministic 18 px resource derivatives;
- `ornaments/` and `illustrations/`: reviewed composition layers for page corners, dividers, result treatment, route thumbnails, orchard vista, and the portrait shell depth master;
- `art_manifest.json`: exact 56-slot source/runtime ownership, hashes, target tiers, and measured optical insets;
- `prompt-record.json`: built-in ImageGen prompts, reviewed references, selected outputs, alpha contracts, and source hashes, including the local compact-control assets in slots 53-54;
- `export_sunny_orchard_painted.py`: deterministic neutral-mask normalization, downsample, alpha/safe-inset validation, optical measurement, and manifest emission.

The exporter contains no shape, icon, ornament, or texture drawing code. Final visual forms come from the reviewed PNG masters. For action glyphs only, it preserves every alpha byte while replacing visible RGB with `#FFFFFF` and hidden pixels with transparent black, so runtime content tokens own color. Source prompts and optical-alignment notes are retained with their owning subfolders.

Runtime optical bounds use the final exported PNG and the shared significant-alpha threshold `alpha >= 48`. Slots 11-14 are export-normalized without altering their reviewed masters: significant pixels are tight-cropped, premultiplied-alpha resized to `120x120`, and placed at `(4,4)` on the `128x128` runtime canvas. Their required half-open significant bbox is therefore `[4,4,124,124)`. Every binding serializes the measured `{left, top, right, bottom}` values as manifest `optical_inset` and ArtSet `opticalInset`; runtime layout must use this separately from the interaction-safe `safeInset`.

Slots 30-36 and 38-39 are tintable action glyphs. Their seven unique masters are `pause`, shared `continue/start-wave/start`, `speed`, `retry`, `return`, `close`, and `refresh`. Source and runtime PNGs use strict white RGB wherever alpha is nonzero, contain no baked hue, gradient, highlight, or shadow, and retain their existing canvas, significant-alpha silhouette, optical bounds, GUID, and Sprite Single geometry. `icon-tool-pot` and every `icon-resource-*` asset remain outside this mask contract.

`action.primary` and `action.danger` are semantic container assets, not runtime-tinted neutral surfaces. The exporter re-anchors only their green/red content ink to median targets `#436C15` and `#9F302B`, preserving painted per-pixel texture differences, alpha, outline, canvas, and `32px` nine-slice geometry. Against warm-white content `#FFF6E0`, the final central 50% content regions measure at least `5.2068:1` (Primary) and `5.7689:1` (Danger), above the `4.5:1` release threshold.

Compact-control slots 53-54 deliberately stay simpler than the general painted family: inactive and active are two complete, matching filled rounded-square surfaces with quiet cream centers. The inactive surface owns one soil-brown outline; the active surface replaces it with one muted amber outline. Leaves, flowers, knots, nodes, dots, inner frames, detached ornaments, layered overlays, and center texture are prohibited so the icon or multiplier remains the first read at `52x52`.

Slots 49-51 are final-size `18x18` micro icons derived from the reviewed 96 px resource masters, with a one-pixel edge, target-raster alpha cleanup, optical centering, and pairwise silhouette-IoU rejection. Slot 52 is center-cover cropped from its prompt-recorded portrait master to the authoritative `402x874` shell raster. It may appear only behind Lobby and Settlement content surfaces; Battle deliberately excludes it.

Visual authority and review evidence:

- `Assets/UI/Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png`
- `openspec/changes/unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/sunny-orchard-core-components-v2.png`
- `openspec/changes/unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/README.md`

Do not reference this source directory from a release theme or scene. Update reviewed masters, preserve runtime `.meta` files, run the exporter, validate in the Visual System editor workflow, and inspect real WebGL captures before activation.

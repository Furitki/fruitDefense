# Sunny Orchard Painted sources

`sunny-orchard-painted@8` is the active reference-faithful production set. Its finite nine-slice kit follows the approved sky-paper reference and text-free Battle chrome styleboard.

Production source ownership is split into:

- `surfaces/`: owned surface/action/slot masters, including fourteen high-exposure action, shell, panel, card, slot, and stage masters integrated from hash-locked individual ImageGen outputs, the remaining deterministic low-exposure material kit, and the retained reviewed sky screen background;
- `icons/`: 22 image-generated, reviewed state/common-icon masters plus three deterministic 18 px resource derivatives;
- `ornaments/` and `illustrations/`: reviewed composition layers for the reference-derived orange-and-leaf page corner, dividers, result treatment, route thumbnails, orchard vista, and the portrait shell depth master;
- `art_manifest.json`: exact 56-slot source/runtime ownership, hashes, target tiers, and measured optical insets;
- `prompt-record.json`: provenance for the retained ImageGen-authored icons, ornaments, background, and illustrations;
- `export_sunny_orchard_painted.py`: direct ImageGen master integration, deterministic low-exposure material authoring, neutral-mask normalization, downsample, alpha/safe-inset validation, optical measurement, and manifest emission.

The fourteen selected high-exposure materials originate only from the individual ImageGen masters recorded in `prompt-record.json`. The exporter verifies every fixed SHA-256 value, crops the generated alpha content, pads/resizes with alpha-safe filtering, and never paints or recolors their rim, face, outline, highlight, shadow, texture, or stage frame. When ImageGen returns the green action as an RGB checkerboard plate, export applies only its separately hash-locked approved geometry alpha. The rail-free nursery tile instead uses one deterministic connected-neutral-background cleanup against its own RGB pixels, so legacy alpha geometry cannot expose a dark or colored edge fringe. `slot.nursery` deliberately keeps its rounded cream paper face and soft tonal depth while containing no solid linear rim or dashed rail. Remaining low-exposure nine-slices continue to use the fixed deterministic material kit. No path bakes text or gameplay art. Retained action glyphs preserve their alpha while visible RGB is normalized to `#FFFFFF`, so runtime content tokens own color.

For these reference-backed materials, every explicitly supplied or user-approved reference visual parameter is P0 authority. The general visual-system contract fills only parameters the reference leaves unspecified. Contrast validation treats the approved raster as immutable: it recalibrates the separate text/icon content token first and never darkens, recolors, regenerates, overlays, or substitutes the master to satisfy a generic foreground pairing. If the reference locks both foreground and background into a failing pair, export stops for an explicit design decision.

Runtime optical bounds use the final exported PNG and the shared significant-alpha threshold `alpha >= 48`. Slots 11-14 are export-normalized without altering their reviewed masters: significant pixels are tight-cropped, premultiplied-alpha resized to `120x120`, and placed at `(4,4)` on the `128x128` runtime canvas. Their required half-open significant bbox is therefore `[4,4,124,124)`. Every binding serializes the measured `{left, top, right, bottom}` values as manifest `optical_inset` and ArtSet `opticalInset`; runtime layout must use this separately from the interaction-safe `safeInset`.

Slots 30-36 and 38-39 are tintable action glyphs. Their seven unique masters are `pause`, shared `continue/start-wave/start`, `speed`, `retry`, `return`, `close`, and `refresh`. Source and runtime PNGs use strict white RGB wherever alpha is nonzero, contain no baked hue, gradient, highlight, or shadow, and retain their existing canvas, significant-alpha silhouette, optical bounds, GUID, and Sprite Single geometry. `icon-tool-pot` and every `icon-resource-*` asset remain outside this mask contract.

`action.primary` and `action.secondary` share the approved light-green direct master and pair with soil-brown `#56341F` content; `action.danger` pairs with inverse warm-white `#FFF9EE`. The manifest records measured final central-region contrast for each actual semantic pair without programmatic recoloring.

Compact-control slots 53-54 are mutually exclusive ImageGen-authored cream-rimmed yellow controls with deep golden/soil outlines, yellow-orange faces, top highlights, and short shadows. They contain no glyph or text. The active surface deepens the orange face without changing its canvas.

Slots 49-51 are final-size `18x18` micro icons derived from the reviewed 96 px resource masters, with a one-pixel edge, target-raster alpha cleanup, optical centering, and pairwise silhouette-IoU rejection. Slot 52 is center-cover cropped from its prompt-recorded portrait master to the authoritative `402x874` shell raster. It may appear only behind Lobby and Settlement content surfaces; Battle deliberately excludes it.

Visual authority and review evidence:

- `openspec/changes/reset-sky-paper-orchard-ui/evidence/reference/sky-paper-orchard-reference.png`
- `openspec/changes/reset-sky-paper-orchard-ui/evidence/imagegen/reference-faithful-battle-chrome-styleboard.png`
- `Assets/UI/Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png`
- `openspec/changes/unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/sunny-orchard-core-components-v2.png`
- `openspec/changes/unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/README.md`

Do not reference this source directory from a release theme or scene. Update reviewed masters, preserve runtime `.meta` files, run the exporter, validate in the Visual System editor workflow, and inspect real WebGL captures before activation.

# Sunny Orchard Painted sources

`sunny-orchard-painted@1` is the user-approved active painted production set derived from the approved Sunny Orchard board and the reviewed component proof v2.

Production source ownership is split into:

- `surfaces/`: reviewed surface/action/slot masters plus the section ribbon and illustration frame;
- `icons/`: 21 image-generated, reviewed state/common-icon masters;
- `ornaments/` and `illustrations/`: reviewed composition layers for page corners, dividers, result treatment, route thumbnails, and orchard vista;
- `art_manifest.json`: exact 49-slot source/runtime ownership, hashes, and measured optical insets;
- `export_sunny_orchard_painted.py`: deterministic downsample, alpha/safe-inset validation, optical measurement, and manifest emission only.

The exporter contains no shape, icon, palette, ornament, or texture drawing code. Final visual forms come from the reviewed PNG masters. Source prompts and optical-alignment notes are retained with their owning subfolders.

Runtime optical bounds use the final exported PNG and the shared significant-alpha threshold `alpha >= 48`. Slots 11-14 are export-normalized without altering their reviewed masters: significant pixels are tight-cropped, premultiplied-alpha resized to `120x120`, and placed at `(4,4)` on the `128x128` runtime canvas. Their required half-open significant bbox is therefore `[4,4,124,124)`. Every binding serializes the measured `{left, top, right, bottom}` values as manifest `optical_inset` and ArtSet `opticalInset`; runtime layout must use this separately from the interaction-safe `safeInset`.

Visual authority and review evidence:

- `Assets/UI/Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png`
- `openspec/changes/unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/sunny-orchard-core-components-v2.png`
- `openspec/changes/unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/README.md`

Do not reference this source directory from a release theme or scene. Update reviewed masters, preserve runtime `.meta` files, run the exporter, validate in the Visual System editor workflow, and inspect real WebGL captures before activation.

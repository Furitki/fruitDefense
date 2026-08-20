# Citrus Mint production candidate

## Status and boundary

`citrus-mint` (display name: &#x67D1;&#x6A58;&#x8584;&#x8377;) is a complete,
unapproved, non-active production candidate. The release theme remains bound
to approved `sunny-orchard@1`. This candidate is not referenced by a scene,
runtime-code path, or `Builds/WebGL`, and OpenSpec task 3.4 remains unchecked.

## Visual direction

- Cream `#FFF8E8`, pale mint `#DFF4E8` / `#A7DCCB`, mint/teal hierarchy
  `#57B59A` / `#2F7F6D`, citrus emphasis `#F2A23A` / `#FFD48A`, and brown
  linework `#7A5138`.
- Primary action `#2F7F6D` against release inverse text `#FFF6E0` measures
  `4.457094:1`, above the production minimum `3.0:1`.
- Surfaces use light split-color edges, citrus disks, and twin-leaf marks.
  Buttons, slots, state indicators, and common icons use outlines or interior
  geometry distinct from Sunny Orchard and avoid a heavy wood treatment.
- Selected, Disabled, Loading, Success, Warning, Error, drag, merge, and swap
  retain non-color shape cues.

## Deterministic production pipeline

[`export_citrus_mint.py`](../../../../../Assets/UI/Art/Sources/citrus-mint/export_citrus_mint.py)
uses editable SVG masters and deterministic Pillow raster export. Image
generation was not used, and Sunny Orchard PNGs were not copied as a new
treatment. The only byte-identical export across A and C is the contract-owned
neutral white `surface-scrim.png`.

- 38 SVG masters produce 38 independent PNGs. The 40 semantic bindings for
  `action.start`, `action.start-wave`, and `action.continue` intentionally
  share the same play sprite while retaining independent bindings.
- Runtime PNGs use standalone Sprite Single / FullRect, 2x source scale,
  lowercase kebab-case names, uniform icon canvases, and safe insets.
- All 65,536 pixels of `surface-screen-background.png` have `alpha=255`;
  decorative overlays use source-over composition on an opaque cream base.
- Two consecutive exporter runs produced zero byte differences across the
  Sources tree, Runtime tree, ArtSet, manifest, and gallery.

## Artifacts and export checkpoint

- [Candidate gallery](citrus-mint-candidate-gallery.png)
- [Evidence manifest](citrus-mint-art-manifest.json)
- Production manifest: `Assets/UI/Art/Sources/citrus-mint/art_manifest.json`
- ArtSet: `Assets/UI/Art/Sets/CitrusMintRuntimeUiArtSet.asset`

| Artifact | SHA-256 |
| --- | --- |
| ArtSet asset | `551BDF78741E87CE42D3CE4C6E9324A59F1ACD95C90C6FA5F5C1556936717880` |
| Manifest | `8BA56499DC8996622B5FE8C854EFB27554864D68AF1274EDC279F67AE57B2492` |
| Gallery | `B391677028165FD563109946A1C3CCEF0CFE34D896F52359F344729E060D1D8E` |

The manifest records the final SHA-256 of every SVG master and runtime PNG.
Unity subsequently normalizes importer metadata without changing those source
or runtime payload hashes, stable GUIDs, ArtSet bindings, or the checkpoint
hashes above.

## Validation

Mechanical checks passed: exactly 40 required slot bindings, 38 unique
exports, manifest source/runtime hashes match, icon safe-inset checks report
zero errors, and the screen background is fully opaque. All 38 imported PNGs
are Sprite Single / FullRect, sRGB, alpha transparency, Clamp, Bilinear,
mipmap-off, and uncompressed.

- [`unity-release-validator.log`](unity-release-validator.log):
  `Valid (0 warning(s))`.
- [`p0-final.log`](p0-final.log): release validation,
  `RUNTIME_UI_NINE_SLICE_SOURCE_UV_OK bindings=30`,
  `RUNTIME_UI_SCREEN_BACKGROUND_OPAQUE_OK sets=2`, visual-system, glyph,
  binding-cache/performance, Shell, and `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`
  all passed with no compiler, exception, or assertion failure.
- `openspec validate unify-runtime-ui-visual-system --strict`: pass.

The release theme references `sunny-orchard@1` once and Citrus Mint zero times;
the four release scenes also contain zero Citrus Mint references. The existing
WebGL loader/data/framework/wasm/index hashes remained byte-for-byte unchanged
and contain no Citrus Mint identifier, proving zero current release dependency.
No candidate activation, Undo operation, scene mutation, or build was run.

Protected Sunny Orchard source/runtime trees, ArtSet/theme and their metas, all
four release scenes, and all five existing WebGL payload/index files retained
their pre-candidate hashes. Assets-wide GUID duplicates, missing/orphan
candidate metas, candidate assets under `Resources`, and `GeneratedInvalid*`
are all zero.

# Task 1.4 — production resource inventory

## Scope and authority

This inventory freezes the resource-side facts for active `sunny-orchard-painted@1` before tasks 3.5/3.6 change any production pixels. The release theme references `SunnyOrchardPaintedRuntimeUiArtSet.asset` GUID `91aa538ae02449cba8c971ffe4d427eb`.

- Machine-readable facts: [`resource-inventory.json`](resource-inventory.json)
- Reproducible collector: [`build_inventory.py`](build_inventory.py)
- Unique-export review montage: [`active-49-resource-gallery.png`](active-49-resource-gallery.png)
- Collector output: [`build-inventory.log`](build-inventory.log)
- Approved direction: `Assets/UI/Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png`, SHA-256 `B9F976C8DC36761B62086A8BE63C2CF4A2FCBED683E3B62CC78526FCB14D87E1`
- Current review board: `Assets/UI/Art/Sources/ReferenceBoards/Review/sunny-orchard-painted-49-gallery.png`, SHA-256 `5AB0655189DC8ED6A62830BFA8A081B295F1896272D2E65B9241CC51084685C2`

The generated montage uses a checkerboard only to expose alpha. It is review evidence, not a production source and is not referenced by the release.

## Inventory result

| Check | Result |
|---|---:|
| Required manifest bindings | 49 |
| Unique source PNGs | 47 |
| Unique runtime PNGs | 47 |
| Intentional shared export | `icon.control-continue` used by Continue, Start Wave, and Start |
| Missing or unbound production PNGs | 0 |
| Unclassified production ancillary files | 0 |
| Production ArtSets | active painted + inactive Sunny Orchard |
| Painted rows with cross-set ownership | 0 |
| Sunny Orchard explicit shared rows | 9, slots 40–48, exact owner mirrors |
| Unexpected mixed-set rows | 0 |
| Approved/review board serialized release references | 0 |

Every runtime importer currently matches Sprite Single, FullRect, sRGB, alpha transparency, Bilinear, Clamp, mipmaps off, read/write off, 100 PPU, and uncompressed Default/Standalone/WebGL settings. Manifest file hashes, Unity GUIDs, dimensions, ArtSet references, and geometry match on all 49 rows. The raw ArtSet contains two serialized GUID references per binding (Texture and Sprite), which the inventory accounts for.

The eight non-PNG support resources are all classified: binding manifest, deterministic exporter, prompt provenance, source/runtime ownership READMEs, icon alignment audit, and approved-board README. No stale candidate directory or extra production PNG was found.

## Confirmed resource defects

These are blocking resource defects for tasks 3.5/3.6; they are intentionally not repaired in the 1.4 baseline.

| Severity | Resource | Evidence | Pass criterion |
|---|---|---|---|
| High | `surface.safe-area` | runtime pixel `(116,122)` is exact RGB `#FF00FF`, alpha 1 | no visible key-magenta pixel at any nonzero alpha |
| High | `indicator.loading` | runtime pixel `(40,77)` is exact RGB `#FF00FF`, alpha 1 | same |
| High | `icon.control-return` | runtime pixel `(70,79)` is exact RGB `#FF00FF`, alpha 1 | same |
| High | `icon.control-refresh` | runtime pixel `(29,65)` is exact RGB `#FF00FF`, alpha 1 | same |
| High | `icon.control-speed` | alpha-mass centroid x offset `-5.725` source px | absolute offset per axis no more than 4 source px |
| High | `indicator.warning` | alpha-mass centroid y offset `+4.7493` source px | same |
| High | `indicator.error` | alpha-mass centroid y offset `+4.4107` source px | same |
| High | `indicator.drag-legal` | alpha bbox `52×70` on `96×96`; at the 24-logical cue it is about 13 logical px wide and is visually weak against the illegal cue | legal/illegal family alpha short edge at least 64 source px; rendered optical short edge 16 logical px and major edge 18 logical px, while retaining a distinct non-color cue |

The optical findings agree with the route audit. Ordinary low-alpha antialiasing and RGB stored under fully transparent pixels are recorded for diagnosis but are not failures by themselves.

## Alpha, slice, and aspect facts

- `surface.screen-background`, `surface.scrim`, and all four illustrations are fully opaque (`alphaMin=alphaMax=255`).
- Icon, indicator, marker, and transparent ornament alpha bounds remain inside their declared safe inset, and none touches its outer canvas edge.
- All 18 NineSlice assets have a positive `64×64` stretch center with a 32 px border. A significant source-boundary mismatch means one side is at least alpha 48 while the other is below alpha 16; all four inner boundaries have zero such mismatches. Raw alpha 0/1 transitions and premultiplied color deltas remain diagnostic only.
- `surface.illustration-frame` intentionally has a transparent center. Both sides of a transparent slice boundary are legal; a validator must not impose the filled-surface opacity rule on that semantic slot.
- Illustration source-to-runtime aspect error is `0.05%`, `0.74%`, `0.51%`, and `0.28%`, all below 1%. Runtime illustration dimensions exactly match the manifest and remain fully opaque.
- `ornament.metric-divider` (`24×96`) and `ornament.result-banner` (`256×72`) are intentional tight crops from square generation masters and use fixed-aspect Stretch drawing. They are not illustration-aspect failures.

## Contract supplied to task 2.5

The production resource validator should consume the shared quality profile and enforce these finite rules without adding a second manifest authority:

1. Require exactly the 49 semantic slots with no missing or duplicate semantic IDs. Allow only the declared three-slot reuse of the Continue runtime export; each slot still owns an independent binding.
2. Match manifest SHA-256, GUID, dimensions, geometry, slice, safe inset, PPU, importer contract, and ArtSet Texture/Sprite reference for every row.
3. Treat any production PNG under the set's Sources or Runtime directory as owned data: it must be bound. Whitelisted manifest/exporter/provenance/audit/README files are ancillary; other files are unclassified failures.
4. Reject exact visible `#FF00FF` at any alpha greater than zero. Require opaque screen background, scrim, and illustrations. Require transparent-family outer padding and safe-inset containment. Do not reject intended low-alpha antialiasing merely because transparent RGB exists.
5. Measure 96 px icon-family alpha bbox, occupancy, alpha-mass centroid, and actual smallest rendered optical size. The profile limit is 4 source px per centroid axis; the major bbox is normally 60–72 px. H-04 additionally requires the legal/illegal family short edge to be at least 64 source px. Thin/directional ornaments require explicit semantic classification rather than a global exception. The smallest rendered optical short/major edges are 16/18 logical px and stroke weight is at least 2 logical px.
6. Validate NineSlice dimensions/import border and keep protected corner/edge motifs out of the stretch bands. Use the significant alpha boundary rule `max(alpha)>=48 && min(alpha)<16`; allow the illustration frame's both-transparent center and do not fail on 47↔48 antialiasing.
7. Require illustration runtime dimensions and opacity, with source-to-runtime aspect error no greater than 1%. Treat fixed-aspect tight-cropped ornaments as their own geometry class.
8. A cross-set row must declare `shared_from_set`; the owner set must be unique, local, and non-chained, and the owner row must exactly mirror semantic, geometry, dimensions, slice, safe inset, PPU, paths, hashes, and GUID. Reject undeclared mixed-set paths.
9. Reject Theme/scene/prefab/ArtSet dependencies on Approved, Review, evidence, or editable source roots. Review boards remain serialized-reference-free.

Task 1.4 records the baseline and does not relax any threshold. Production fixes and regenerated review evidence belong to tasks 3.5/3.6 after the route defect inventory is complete.

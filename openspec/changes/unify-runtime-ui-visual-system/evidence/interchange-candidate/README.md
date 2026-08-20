# Orchard Woodcraft interchange candidate evidence

Status: **historical pre-rejection interchange evidence.** The user explicitly rejected this treatment after the technical preview was completed. Its production source, runtime exports, importer metadata, and ArtSet definition were removed by the task-7.3 follow-up; the evidence below is retained only to preserve the completed preview/isolation record and cannot be activated. A `sunny-orchard` remains the only approved release direction and serialized active set. Task 3.4 intentionally remains unchecked.

## Candidate construction

- Set identity: `orchard-woodcraft` revision `1`.
- Coverage: 40 independently serialized semantic bindings backed by 38 unique SVG masters and 38 PNG exports. Continue, start-wave, and start intentionally share the same play-symbol Sprite.
- Production method: repository-native deterministic vector recipes in `export_orchard_woodcraft.py`; no image-generation call and no style-board crop. The B board is a visual-treatment reference only.
- Geometry/import parity with A: 128 px nine-slice assets with 32 px protected borders, 96 px icons with 12 px safe inset, source scale `2`, and standalone Sprite Single / Full Rect / sRGB / alpha / Bilinear / Clamp / no mipmaps / no Read-Write / uncompressed import.
- Material treatment: quiet oat-paper/linen centers, walnut outlines, shallow wood-edge bands, small peg/leaf details, sage action surfaces, and terracotta emphasis. Texture details stay in protected borders so text-bearing centers and layout geometry do not change.
- Shared Primary Action refinement: the candidate's `action.primary` center is `#559A39`, exactly matching the release semantic token and its `3.0:1` inverse-text contrast gate.
- Opaque screen background: translucent woodcraft decorations are source-over composited onto the opaque oat base instead of replacing destination alpha. All 65,536 runtime pixels now have alpha `255`; path, size, importer metadata, GUID `0b885b575f676b9519d17f54bc2954d3`, ArtSet binding, and SVG master are unchanged.
- State cues: selected, disabled, loading, success, warning, error, drag-legal, drag-illegal, merge, and swap each retain a distinct shape cue in addition to color.
- `surface-scrim` is exactly one opaque neutral-white color `(255,255,255,255)`; release-theme tint and feedback opacity remain the only runtime scrim authorities.

Reference board SHA-256: `1C96CA79DD89EBBE50BC6E61F12E2237D0B38150912BB35FC645735EA65E07EB`.

## Visual review

[`orchard-woodcraft-candidate-gallery.png`](orchard-woodcraft-candidate-gallery.png) was inspected at original resolution against the B board and the A production gallery. It shows the full candidate, not cropped reference-board pixels. The candidate is recognizably woodcraft while preserving A's component silhouettes, slot count, semantic ordering, safe margins, and quiet copy surfaces.

Gallery SHA-256: `18C79F881FB28C266EA4B4B77CBAC4DB46D3FB623B688BD014FD52383F838C32`.

## Validation

Unity `6000.3.19f1` imported the candidate and ran the existing Visual System workflow without candidate-specific editor code:

1. `FruitDefense.Editor.RuntimeUiVisualSystemActivation.ValidateReleaseAndWorkflowOrThrow`
   - registry order selects `orchard-woodcraft@1` before `sunny-orchard@1`, so this run exercises B as the isolated candidate;
   - release validation: `Valid (0 warning(s))`;
   - isolated preview clone: passed;
   - valid activation on a non-persistent clone, single Undo group, Undo restoration, and invalid-candidate zero-mutation rejection: passed;
   - process return code: `0`.
2. `FruitDefense.Editor.RuntimeUiVisualSystemSmoke.Run`
   - exercises the still-active A set, preview isolation, illegal activation rejection, atomic activation/Undo, in-place PNG reimport GUID/binding preservation, and release-theme/scene byte restoration;
   - marker: `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`;
   - process return code: `0`.

Raw logs:

- [`unity-preview-validation.log`](unity-preview-validation.log)
- [`unity-a-active-smoke.log`](unity-a-active-smoke.log)

Manifest-to-file SHA checks reported zero errors; all icon alpha bounds fit the declared 12 px safe inset; runtime PNG count is 38; source SVG count is 38; no candidate path is under or contains `Resources`.

Candidate artifact hashes:

- `surface-screen-background.png`: `AEE6F21A977DBC297834DFC5D1C73B256D9E7E5F367ADC35F4D3CDEA12347CD` -> `7FFFE6E76F3D220121807C8433E4F8BC625FA1406344B6068435D091EF7DBDDE`
- `art_manifest.json`: `3452A41E3BC7CF1F8CF5F9D9D57B7FD092907A327255E6B30B796DBEEE4A101A`
- `OrchardWoodcraftRuntimeUiArtSet.asset`: `C0671BEA7D1938C2BE78525E35A356A6FEC55B8806BD31F1DA878A3016C901EA`

## Release isolation

Before/after SHA-256 values were identical for the release theme and every scene. The four release-scene examples are:

| Asset | Before and after SHA-256 |
| --- | --- |
| `ReleaseRuntimeUiTheme.asset` | `375990DC5E2C670AAE5B34212C27D9C83982C53C8CEEC88DEAE27E62AB18C911` |
| `Bootstrap.unity` | `27AD84F0D624DA6C1BE7152AD801990E6AE832E0A92019E6D585D35421E8ABD1` |
| `Lobby.unity` | `B4FA8E3B1656D1440A47D38FFA6B2E0CAD512E40DEE673D35EC39E505FDA2A6C` |
| `Battle.unity` | `C6CF5D7246B4FE21EB205FF0D7D740B3B5FE2C1D482D5721F13D75C311621C4E` |
| `Settlement.unity` | `FB6A7204EE71A9C38551920A88287F394EF45899974F599D8C89C6B6BC6569BC` |

The release theme's `activeArtSet` GUID remains `12cc0c638d174040bb0384d7bf17ea92`, which resolves to `SunnyOrchardRuntimeUiArtSet.asset`. B is not referenced by the theme, Bootstrap, or any release scene.

## Approval boundary

This evidence proved technical interchangeability and preview isolation only; it never approved B or authorized release activation. The subsequent user decision rejected B, so no later activation is permitted from this historical evidence. A remains active and task 3.4 remains incomplete.

# PC Nursery Alpha-Fringe Correction

Date: 2026-08-28

## Superseded diagnosis

The rail-only correction was incomplete. Its ImageGen output was an RGB checkerboard plate, and production combined those RGB pixels with the previous slot's alpha geometry. The generated rounded silhouette and the legacy mask did not coincide. Low-alpha edge pixels therefore retained neutral/dark and orange RGB; the runtime resize additionally produced black `rgba=(0,0,0,1)` ringing. Particular PC scale factors reconstructed those hidden pixels as four perimeter lines while WebGL filtering could make them appear absent.

The rejected masked candidate is preserved as `rejected-slot-nursery-mask-fringe.png` with SHA-256 `995C3C6188886D6D75801204569C62C3768C9DF031334695AB48A9B2C810038D`.

## Replacement boundary

Mode: built-in ImageGen precise-object edit.

Final prompt:

> Clean only the alpha fringe and outer edge of this warm cream rounded-paper carrier so it has no visible dark, black, gray, orange, yellow, solid, dashed, dotted, or rectangular perimeter line at any scale. Preserve the cream paper face, subtle texture, rounded silhouette and shallow bottom depth. Output genuine RGBA transparency, with alpha-safe cream edge RGB and no checkerboard, border, marker, text or icon.

ImageGen again returned an RGB checkerboard plate. The accepted deterministic integration now:

1. flood-fills only the bright neutral background connected to the plate edges;
2. retains only the warm material component connected to the plate center;
3. crops/pads and performs premultiplied-alpha-safe resize;
4. clears low-alpha ringing after the source-master resize and again after the runtime resize;
5. rejects hidden RGB and neutral/dark partial-alpha pixels.

The cleanup derives geometry from the selected output itself. The legacy `slot-nursery-geometry-mask.png` is no longer active.

| Artifact | SHA-256 |
|---|---|
| direct ImageGen plate | `335028CADB393454E90B8D0D66DC27B3F39C2839D13C12084B3BB4204131ABFE` |
| source 256x256 PNG | `62569658B0D3741FCCD92B2F0B0CBBA06502F1B256EC9C99BEE52C16E6924F1D` |
| runtime 128x128 PNG | `519881A6A6E48073CA5B5323A5245D3615F68E30C3A3F16115EC41F7CD0DDC20` |
| revision-8 manifest | `15208732826D2D2FBA17CC14F6B51B8B4304988BD54460960EA2631BCA2C82B1` |
| revision-8 ArtSet | `1412D1CF5F637C5D122B009F7CE190C52DA7311AB3CB81523CCE4AB3D570ADC5` |

## Pixel gates

| Output | Orange rail | Hidden RGB at alpha 0 | Dark/neutral partial-alpha fringe |
|---|---:|---:|---:|
| source 256x256 | 0 | 0 | 0 |
| runtime 128x128 | 0 | 0 | 0 |

The Unity production validator now enforces the same three conditions for `slot.nursery`, in addition to its direct-master provenance and line-free anatomy contract.

## Validation and Windows package

- deterministic exporter repeat: all source/runtime/manifest/ArtSet hashes stable;
- `openspec validate polish-sky-paper-ui-eight-point --strict`: passed;
- `CompactControlAcceptanceSmoke.Run`: `COMPACT_CONTROL_ACCEPTANCE_SMOKE_OK` in `Logs/pc-alpha-fringe-compact-final.log`;
- Windows x64 build: `Build Finished, Result: Success` in `Logs/pc-alpha-fringe-windows-build.log`;
- rebuilt `Assembly-CSharp.dll`: `43E29867386787F8B1FA5E5FD355F07364FFEFD1C73BAA349A12C33727B946BA`;
- rebuilt `resources.assets`: `1ABEE9D5B5B21B87F8ED272D90DFA2C08D1F69EFA4185896931BDF8994223EFA`.

The rebuilt player launched successfully as the `水果塔防` window. Windows Graphics Capture still rejected this Unity window with `SetIsBorderRequired failed: 不支持此接口 (0x80004002)`, so no automated PC screenshot is claimed and no WebGL screenshot substitutes for PC review.

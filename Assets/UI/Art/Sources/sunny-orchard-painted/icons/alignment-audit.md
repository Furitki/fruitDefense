# sunny-orchard-painted icon alignment audit

## Runtime optical-bound contract

The production manifest and ArtSet measure each binding from the final runtime PNG at the significant-alpha threshold `alpha >= 48`. The manifest field is `optical_inset`; the matching Unity serialization field is `opticalInset`. Both store `{left, top, right, bottom}` canvas padding and are distinct from `safeInset`.

Painted action slots 11-14 share a deterministic export-normalized visible box. Each reviewed master is left unchanged; its significant pixels are tight-cropped, premultiplied-alpha resized to `120x120`, then placed at `(4,4)` on the `128x128` runtime canvas.

| Slot | Semantic binding | Significant bbox `[left,top,right,bottom)` | Optical inset |
|---:|---|---:|---:|
| 11 | `action.primary` | `[4,4,124,124)` | `4,4,4,4` |
| 12 | `action.secondary` | `[4,4,124,124)` | `4,4,4,4` |
| 13 | `action.quiet` | `[4,4,124,124)` | `4,4,4,4` |
| 14 | `action.danger` | `[4,4,124,124)` | `4,4,4,4` |

Audit target: 96×96 RGBA runtime canvas, content alpha threshold >3/255, 12px safe inset, 72×72 maximum export box, 36px logical visual box at 2× source scale. Area percentage is measured against the central 72×72 box. `Outside` counts alpha pixels outside `[12,84) × [12,84)`.

| Asset | Alpha bbox `x,y,w,h` | Bbox center | Alpha-weighted centroid | Area px | Box fill | Outside |
|---|---:|---:|---:|---:|---:|---:|
| `icon-control-close` | 15,16,66,64 | 47.5,47.5 | 46.74,48.17 | 3169 | 61.1% | 0 |
| `icon-control-continue` | 15,15,66,66 | 47.5,47.5 | 45.02,49.23 | 2763 | 53.3% | 0 |
| `icon-control-pause` | 15,15,66,66 | 47.5,47.5 | 47.96,48.53 | 3359 | 64.8% | 0 |
| `icon-control-refresh` | 17,15,62,66 | 47.5,47.5 | 47.80,48.09 | 2325 | 44.8% | 0 |
| `icon-control-retry` | 16,15,64,67 | 47.5,48.0 | 47.96,48.98 | 2757 | 53.2% | 0 |
| `icon-control-return` | 15,15,67,66 | 48.0,47.5 | 48.50,51.61 | 3338 | 64.4% | 0 |
| `icon-control-speed` | 14,27,70,43 | 48.5,48.0 | 44.08,48.12 | 1851 | 35.7% | 0 |
| `icon-resource-core` | 14,15,68,67 | 47.5,48.0 | 47.84,51.88 | 3218 | 62.1% | 0 |
| `icon-resource-sun` | 15,15,66,66 | 47.5,47.5 | 47.85,48.25 | 2750 | 53.0% | 0 |
| `icon-resource-wave` | 15,14,67,68 | 48.0,47.5 | 47.83,49.46 | 3171 | 61.2% | 0 |
| `icon-tool-pot` | 14,15,67,66 | 47.0,47.5 | 48.73,46.93 | 3506 | 67.6% | 0 |
| `indicator-disabled` | 14,18,67,58 | 47.0,46.5 | 47.79,47.65 | 2654 | 51.2% | 0 |
| `indicator-drag-illegal` | 14,14,67,67 | 47.0,47.0 | 49.02,47.68 | 2863 | 55.2% | 0 |
| `indicator-drag-legal` | 15,13,66,69 | 47.5,47.0 | 47.51,51.02 | 2955 | 57.0% | 0 |
| `indicator-error` | 15,15,66,65 | 47.5,47.0 | 48.01,51.52 | 2904 | 56.0% | 0 |
| `indicator-loading` | 14,15,68,66 | 47.5,47.5 | 47.53,46.14 | 1905 | 36.7% | 0 |
| `indicator-merge` | 15,15,66,66 | 47.5,47.5 | 48.01,50.32 | 1897 | 36.6% | 0 |
| `indicator-success` | 15,17,66,62 | 47.5,47.5 | 47.98,49.80 | 2955 | 57.0% | 0 |
| `indicator-swap` | 15,19,66,57 | 47.5,47.0 | 47.73,48.60 | 2355 | 45.4% | 0 |
| `indicator-warning` | 15,17,66,60 | 47.5,46.5 | 48.05,51.85 | 2305 | 44.5% | 0 |
| `marker-selected` | 14,16,67,63 | 47.0,47.0 | 47.48,49.22 | 2888 | 55.7% | 0 |

## Visual checks

- **Optical center:** all bbox centers land within 1px of the 96×96 canvas center and every alpha-weighted centroid is now within the quality-profile limit of 4 source px per axis. `speed`, `warning`, and `error` were translated in their reviewed masters without changing their anatomy.
- **Stroke language:** state, resource, and tool icons retain the warm-brown outline family and their intrinsic palette. Action glyphs (pause, shared continue/start, speed, retry, return, close, and refresh) preserve the audited alpha anatomy but use a strict neutral-white mask so runtime content tokens own color.
- **State discrimination:** selected/success are independently gold/green; disabled uses a minus and closed leaf; loading is an open seed sequence; warning/error use the same caution anatomy with amber/red plus an exclamation; drag legal/illegal, merge, and swap use distinct receptacle/prohibition/convergence/opposed-arrow silhouettes. Legal drag now has a `66×69` alpha box at the audit threshold (short edge ≥64) and remains a green arrow/check/pot rather than the illegal red circle/slash.
- **Common-icon discrimination:** retry has one arrow around one seed; refresh has two arrows around two seeds; continue is one triangle; speed is two triangles with a motion tick. All remain recognizable without labels.
- **Transparent padding:** every runtime asset has zero audited alpha outside the 12px safe inset. No source checkerboard, white field, backing tile, or cast background shadow survives export.

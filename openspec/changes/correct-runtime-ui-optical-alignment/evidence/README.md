# Runtime UI optical-alignment evidence

## Outcome

The active `sunny-orchard-painted@1` UI now uses final-runtime alpha bounds for
optical composition. The paused modal, shared actions, metrics, and single-line
titles were checked on Editor geometry and real WebGL pixels.

## Measured before / after

| Check | Before | After |
| --- | ---: | ---: |
| Painted Primary / Danger significant-alpha envelope on the 128 px source canvas | `122x120` / `109x108`, with Danger visibly smaller and shifted upward | all four action surfaces are exactly `120x120` at `[4,4,124,124)` |
| Paused title glyph center vs. 52 px ribbon owner center | approximately `-11 px` | `0.07 logical pt` full / `0.91 logical pt` inset |
| Paused warning-icon vs. hint-copy vertical centers | approximately `16 px` apart | `0.55 px` apart (`416.71` / `417.26`) |
| Continue / Restart held-press changed raster | unequal resource envelopes; no final-raster containment gate | both exactly `138x48`, centered at `(125,492)` / `(277,492)` and contained by their `142x52` owners with a 2 px inset on every side |
| Resting Continue / Restart saturated surface bounds | visibly different | height difference `0 px`, width difference `1 px` at the conservative color threshold; exact alpha metadata is identical |

The after-pixel measurements use
[`05-paused.png`](webgl/final-402x874-full/05-paused.png). Title, hint, resting
surface, held-press bounds, and inset counts are recomputed by the acceptance
script and recorded in JSON rather than inferred from layout rectangles.

## Resource and layout contract

- Both exporters write schema `fruit-defense.runtime-ui-art-manifest.v2` and
  include `optical_inset` for all `49/49` bindings.
- Both ArtSets serialize the same 49 optical insets generated from their final
  runtime PNGs. The release validator recomputes alpha bounds at threshold 48
  and requires an exact match.
- Painted action masters and `.meta` files were preserved. The deterministic
  exporter was run twice across 91 mechanical outputs with zero hash changes.
- `safeInset` remains a safe-canvas contract. Shared action, metric, and inline
  composition now uses `opticalInset`; page presenters do not have a second
  optical-geometry path.
- Single-line text is first resolved to a finite, vertically centered line box,
  then receives the validated typography-role optical offset.

## Unity validation

| Suite | Marker |
| --- | --- |
| Focused runtime UI quality | `RUNTIME_UI_QUALITY_OK cases=59 viewports=4` |
| Runtime UI visual system | `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK` |
| Aggregate project smoke | `FRUIT_DEFENSE_SMOKE_OK` |

The exact commands, exit status, and terminal markers are recorded in
[`validation-markers.md`](unity/validation-markers.md). Raw Unity logs were not
retained because they contain machine-local network discovery data.

## WebGL validation

Build payload identity:

- loader `bdd789111db2`
- data `78d62a3c45c5`
- framework `7b327fa58679`
- wasm `8d135a1947bd`

Acceptance evidence:

- [`final-402x874-full/acceptance.json`](webgl/final-402x874-full/acceptance.json):
  full viewport, canonical paused modal, pre-release wave press, both modal
  action presses, continuation/restart, selection/drag/detail/terminal states.
- [`final-402x874-inset/acceptance.json`](webgl/final-402x874-inset/acceptance.json):
  the same hard gates with `safeTop=44` and `safeBottom=34`.
- [`final-cross-route-402x874-full/flow-acceptance.json`](webgl/final-cross-route-402x874-full/flow-acceptance.json):
  Lobby -> Battle -> Settlement -> Lobby and retry, including the Settlement
  motion checkpoint.
- [`final-shell-402x874-full/shell-visual-evidence.json`](webgl/final-shell-402x874-full/shell-visual-evidence.json):
  Lobby default/alternate selection, selection motion, and Start held press.

The full and inset manifests both report
`pausedModalFinalRasterOpticalAlignment=pass` and
`pauseActionsPressedBeforeReleaseAndContained=pass`. The full manifest measures
Continue press bounds `[56,468,194,516)` and Restart press bounds
`[208,468,346,516)`, each contained within its authoritative owner.

Ordinary WebGL only was validated. This evidence does not claim Douyin or
WeChat conversion support.

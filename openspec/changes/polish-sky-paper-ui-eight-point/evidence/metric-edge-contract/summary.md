# Header Metric Line Root Cause and Prevention Contract

## User Evidence

- Screenshot: `header-metric-user-evidence.png`
- SHA-256: `44C45F6913CB7F89084D82735341D4B76B2A8411DF87A9F7C736F416252D000B`
- The accepted nursery correction is visible in the same runtime family, while the three Header metric carriers still show a four-side outline.

## Root Cause

1. `surface.metric` incorrectly reused `panel-paper.png` together with `surface.safe-area`, `surface.panel-standard`, and `surface.panel-raised`. The large-panel master owns a continuous tan inner outline that is valid at panel scale but becomes a four-side rail when compressed into a compact metric capsule.
2. The old exporter cleared low-alpha ringing for `slot.nursery` and later icon slots only. `surface.metric` is an early ImageGen material slot, so its 128px runtime resize retained dark/neutral partial-alpha pixels. The previous runtime PNG contained 161 such pixels.
3. Fractional PC host/GUI scales bilinearly mix the authored rail and dirty partial-alpha edge into neighboring pixels. Aligned/integer scales can land between or on the thin source rows and make the defect much less visible, which is why only some resolutions exposed it strongly.
4. This is not a duplicate Header draw or a WebGL/WebGL2 issue. It is a production-master ownership error plus incomplete transparent-edge normalization, revealed by platform-dependent sampling.

## Correction

- `surface.metric` now owns `metric-capsule.png`, a separate line-free ImageGen master. It no longer shares `panel-paper.png`.
- Because the selected output is RGB with a baked neutral checkerboard, alpha is derived only from that output's own edge-connected background and center-connected warm carrier. No panel, legacy, or foreign geometry mask participates.
- The metric remains a visible warm-paper rounded carrier through its face, silhouette, highlight, and shallow tonal depth; only the solid/dashed perimeter-rail anatomy is rejected.
- Source/runtime identities after deterministic export:
  - generated master SHA-256: `5539902850BA5EA59A20B356B34A7539516108D349B95F3E458AA2EA4590DC51`
  - source SHA-256: `013C9C23B69759440BD8FD7818076ECA681BA97C625EB4FB2C597BB18EABAA5B`
  - runtime SHA-256: `19599182C749802040E425299BBF4A47FC9EEDE5FF5B977C0E271DE2979AFB5D`

## Non-Regression Gates

1. Direct ImageGen master reuse is allowed only when edge, outline, shadow, alpha, anatomy, render contract, and deterministic transform are identical. The exporter and Unity validator reject incompatible reuse.
2. Every transparent direct ImageGen nine-slice clears low-alpha ringing after both source and runtime resize; `alpha == 0` must imply `RGB == 0`, and no `0 < alpha < 48` pixel may remain.
3. A checkerboard/opaque ImageGen plate uses background cleanup derived from that same output, or a separately hash-locked same-semantic and measurement-locked mask. Old-resource and cross-semantic masks are forbidden.
4. Line-free carriers reject dark/neutral partial-alpha fringe. `surface.metric` additionally rejects a continuous dark rail across at least 75% of the protected middle span on any side.
5. WebGL visibility cannot close a PC sampling defect. Transparent nine-slice changes require the Windows package build and PC-scale evidence; if capture is unavailable, the exact limitation is recorded instead of substituting WebGL proof.

## Verification

- Strict OpenSpec: `Change 'polish-sky-paper-ui-eight-point' is valid`.
- Deterministic exporter repeat: passed twice from unchanged inputs.
  - manifest SHA-256: `EB10CBA87EC3863F0CB81D49AAE25D52F3D243FFD084420BCE908CFE55847566`
  - ArtSet SHA-256: `B585F0263CB0AC1549BA1B7BFD1C16370A37A250C15AA05ED423891A8370FDCA`
- Direct ImageGen pixel gate: 28 unique source/runtime PNGs scanned; hidden RGB and `0 < alpha < 48` counts are zero. Metric and nursery dark/neutral partial-alpha counts are zero. Metric/panel do not share a generated asset and the metric continuous-rail detector returned no side.
- Focused Unity: `Logs/metric-edge-compact.log` contains `COMPACT_CONTROL_ACCEPTANCE_SMOKE_OK` and return code `0`.
- UI nine-slice technical checks: `Logs/metric-edge-ui-visual-system.log` contains `RUNTIME_UI_NINE_SLICE_PARTITION_OK draws=1 pcScales=6` and `RUNTIME_UI_NINE_SLICE_SOURCE_UV_OK bindings=20`. The broader visual-system smoke then stops on the unrelated existing source-authority gate `image-analysis.ps1 exceeds the 900-line module boundary`.
- Aggregate Unity: `Logs/metric-edge-project-smoke.log` was run and stops on the unrelated current-worktree assertion `Combat feedback SDF render smoke failed: role-route prepares every admitted label` before the UI aggregate stage.
- Windows build: `Logs/metric-edge-windows-build.log` contains `Build Finished, Result: Success` and return code `0`.
  - `FruitDefense.exe` SHA-256: `70ED4BAFFC5BF8CBA6116A39811A9C0D2AADFBBACF3836EA822592EC6484A1CA`
  - `Assembly-CSharp.dll` SHA-256: `BD1A918AB13A990BA53498D6B5B872541936149B80BD76874ACCAA147B2C4410`
  - `resources.assets` SHA-256: `1ABEE9D5B5B21B87F8ED272D90DFA2C08D1F69EFA4185896931BDF8994223EFA`
  - `sharedassets0.assets.resS` SHA-256: `5635F472E6D14CA69DBAE0B3DBA8578BA16F0F9B7F40A688A2B549E91E7CD99B`
- PC-package capture: the built player opened as the unique `水果塔防` window. Windows Graphics Capture failed twice, including after refreshing the window handle, with `SetIsBorderRequired failed: 不支持此接口 (0x80004002)`. Capture stopped after the permitted retry; no WebGL image substitutes for PC evidence.

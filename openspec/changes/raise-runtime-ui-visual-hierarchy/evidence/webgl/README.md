# Runtime UI visual hierarchy WebGL evidence

This branch is accepted against ordinary WebGL at `402x874` in both full and `44/34` inset safe-area profiles. The refreshed flow manifests resolve build identity loader `1aadd40e5a5e`, data `bc210cc4a4b5`, framework `7b327fa58679`, and wasm `d045287bba48`.

## What changed visibly

- Lobby and Settlement now share one text-free, portrait-cropped orchard depth layer behind their readable surfaces. Battle deliberately excludes it so the board remains the dominant interaction plane.
- Lobby cards retain equal geometry but expose more of the three route illustrations; the selected state remains one localized marker and warm card fill.
- Battle uses three final-raster `18x18` resource icons. Their significant Alpha boxes are all `16x16`; the worst pairwise silhouette IoU is `0.757`, below the `0.80` confusion gate.
- Settlement promotes the orchard vista to a framed cover-cropped hero. Its result ornament is now sized from the runtime PNG's `240x32` significant-alpha envelope rather than the transparent `256x72` canvas, producing a declared `330x44` optical target around the unchanged SectionTitle typography.
- Read-only metrics are borderless icon/copy rows inside the result card and Battle header. Closed borders remain reserved for structural containers and actions.
- Pause title/copy/button optical checks remain green, and pressed/hover evidence keeps every action inside its authoritative owner.

## Captures

- [Full Lobby](full-402x874-flow-victory/01-lobby.png)
- [Full Battle ready](full-402x874-battle/01-ready.png)
- [Full paused modal](full-402x874-battle/05-paused.png)
- [Full victory settlement](full-402x874-flow-victory/03-settlement-victory.png)
- [Inset Battle ready](inset-402x874-battle/01-ready.png)
- [Inset paused modal](inset-402x874-battle/05-paused.png)
- [Inset defeat settlement](inset-402x874-flow-defeat/03-settlement-defeat.png)

The machine-readable measurements are in [visual-measurements.json](visual-measurements.json). Canonical state, delivery, interaction, final-raster optical, and flow assertions are recorded in the adjacent `acceptance.json` and `flow-acceptance.json` files.

## Payload

The four WebGL payloads total `10,243,595` bytes, up `577,684` bytes (`5.9765%`) from build `78d62a3c45c5`. Almost all growth remains the portrait illustration in the data payload; the optical-framing and borderless-metric follow-up adds only `3,641` bytes over the previous accepted hierarchy build.

## Settlement final-raster gates

- Full victory: rendered banner color envelope `338x52`, outcome glyphs `55x25`, top/bottom padding `14/13px`.
- Inset defeat: rendered banner color envelope `308x47`, outcome glyphs `52x23`, top/bottom padding `13/11px`.
- All six measured metric-row edge bands have a maximum closed brown-border run fraction of `0.0`.

## Manual review

The large shell backdrop is intentionally the most visible new resource and materially improves route identity and value depth. The three Battle micro icons now read at actual size and have stronger negative-space separation, but their labels remain compact by design; further visual growth should come from new page-specific hero/illustration masters, not by enlarging the header or adding more ornament to Battle.

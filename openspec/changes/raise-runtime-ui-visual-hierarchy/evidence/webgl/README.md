# Runtime UI visual hierarchy WebGL evidence

This branch is accepted against ordinary WebGL at `402x874` in both full and `44/34` inset safe-area profiles. All four manifests resolve the same build identity: loader `9d9cf0cf4f3f`, data `3ff58edba777`, framework `7b327fa58679`, and wasm `ddf8cd141907`.

## What changed visibly

- Lobby and Settlement now share one text-free, portrait-cropped orchard depth layer behind their readable surfaces. Battle deliberately excludes it so the board remains the dominant interaction plane.
- Lobby cards retain equal geometry but expose more of the three route illustrations; the selected state remains one localized marker and warm card fill.
- Battle uses three final-raster `18x18` resource icons. Their significant Alpha boxes are all `16x16`; the worst pairwise silhouette IoU is `0.757`, below the `0.80` confusion gate.
- Settlement promotes the orchard vista to a framed cover-cropped hero, then uses three repeated metric rows and a clear two-action closeout.
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

The four WebGL payloads total `10,239,954` bytes, up `574,043` bytes (`5.9388%`) from build `78d62a3c45c5`. Almost all growth is the new portrait illustration in the data payload (`+572,251` bytes); framework is unchanged and wasm grows only `1,792` bytes.

## Manual review

The large shell backdrop is intentionally the most visible new resource and materially improves route identity and value depth. The three Battle micro icons now read at actual size and have stronger negative-space separation, but their labels remain compact by design; further visual growth should come from new page-specific hero/illustration masters, not by enlarging the header or adding more ornament to Battle.

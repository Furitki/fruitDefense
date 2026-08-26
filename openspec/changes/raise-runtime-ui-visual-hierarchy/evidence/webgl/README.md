# Runtime UI visual hierarchy WebGL evidence

The canonical branch matrix remains accepted at `402x874` in both full and `44/34` inset safe-area profiles, as recorded in `visual-measurements.json` and the existing full/inset route captures. The task 5.3 outcome-emphasis supplement uses full and `24/34` inset captures to exercise the same typography gate at a second non-zero top/bottom inset scale. These interactive fixture captures use the dedicated acceptance WebGL profile, and both retained manifests record `verifiedProfile=acceptance`. The frozen final build identity is loader `e34e92f59c48`, data `2ca8d78775e6`, framework `3cda7dcd6ad6`, and wasm `b07b062af21a`.

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
- [Full victory emphasis](emphasis-full-victory/03-settlement-victory.png)
- [Inset defeat emphasis](emphasis-inset-defeat/03-settlement-defeat.png)
- [Full victory outcome hidden](emphasis-full-victory/03a-settlement-hidden-victory.png)
- [Full victory outcome appearing](emphasis-full-victory/03a-settlement-motion-victory.png)
- [Inset defeat outcome hidden](emphasis-inset-defeat/03a-settlement-hidden-defeat.png)
- [Inset defeat outcome appearing](emphasis-inset-defeat/03a-settlement-motion-defeat.png)
- [Full victory emphasis manifest](emphasis-full-victory/flow-acceptance.json)
- [Inset defeat emphasis manifest](emphasis-inset-defeat/flow-acceptance.json)

The general hierarchy measurements and canonical `44/34` inset identity remain in [visual-measurements.json](visual-measurements.json). Canonical state, delivery, interaction, final-raster optical, and flow assertions are recorded in the adjacent `acceptance.json` and `flow-acceptance.json` files; the task 5.3 reveal and outline supplement is owned by the two linked emphasis manifests.

## Payload

The dedicated acceptance build's four immutable Build payloads total `12,757,383` bytes: loader `117,893` bytes (`e34e92f59c48aadc2314e57e329f90aacee294b5f99a431bc1ed22bbb885113b`), data `6,623,142` bytes (`2ca8d78775e6be843a0ba9b5de678f2524a6989a3d97eea1b31fe4da40c59b05`), framework `71,639` bytes (`3cda7dcd6ad61f8b005b8570a733814e4206679574244f5afa2d2c395c74ec33`), and wasm `5,944,709` bytes (`b07b062af21ae7d4bc131bc2726ffd7c8fd045e4a30ce06ae6cf5510b2fbfb5b`). The complete acceptance output, including `index.html` and the project-owned host resources, totals `12,767,625` bytes. This includes the acceptance-only bridge and fixture surface, so it is evidence identity rather than a release-size comparison; release payload accounting belongs to the release build gate.

## Automated validation

- Editor quality: `RUNTIME_UI_QUALITY_OK cases=80 viewports=4`.
- Editor visual system: `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK`; release validator: `Valid (0 warning(s))`.
- Acceptance runtime isolation: `FRUIT_DEFENSE_ACCEPTANCE_RUNTIME_ISOLATION_OK`.
- Frozen WebGL build: `FRUIT_DEFENSE_ACCEPTANCE_WEB_BUILD_OK`; both browser runs: `FRUIT_DEFENSE_FLOW_ACCEPTANCE_OK`.
- Script gates: `FRUIT_DEFENSE_ACCEPTANCE_SELF_CHECK_OK`, `FRUIT_DEFENSE_WEBGL_BUILD_PROFILE_PROBE_SELF_CHECK_OK`, and the route/session publisher behavior probe all pass. The URL profile self-check includes a dynamic host whose base URL reports `acceptance` but whose exact final query reports `release`; the portrait runner rejects it before Chrome or Unity messaging.

## Settlement final-raster gates

- Full victory: rendered banner color envelope `338x52`; independent hidden-delta final outcome ink `61x31`; exact connected outline thickness `2px`; vertical occupancy `0.596154`; padding `138/11/139/10px` with `1px` top/bottom imbalance.
- Inset defeat: rendered banner color envelope `316x49`; independent hidden-delta final outcome ink `58x29`; exact connected outline thickness `2px`; vertical occupancy `0.591837`; padding `129/10/129/10px` with `0px` top/bottom imbalance.
- Final ink is derived independently from the stable screenshot against its hidden frame with a maximum-channel delta greater than `8`, not from `fillSupport OR outlineMap`. Full victory records `1617` final-ink pixels and `748/748/748/748` outline candidate/connected/connected-final-covered/candidate-final-covered pixels; inset defeat records `1364` and `651/651/651/651`. A synthetic candidate present in the stable color mask but absent from the independent delta mask fails closed.
- The authoritative direct-cardinal gate requires every ring side to contain at least `8%` of that ring and every outer side to retain at least `20%` of the preceding ring. For the 2px outer ring, full victory requires at least `27` pixels per side and records `83/59/51/70` with minimum previous-ring retention `0.614458`; inset defeat requires at least `19` and records `38/44/31/41` with minimum retention `0.274336`. Bounding-box `expansionFromFill` remains an AA observation only. Detached candidates plus four-sided 1px, local-residual, and locally-gapped synthetic negatives are all rejected for their intended gate.
- The outcome-only reveal gate is route- and session-bound: every accepted telemetry sample proves `fruitDefenseAppRoute=2`, identity route `2/settlement`, and a matching reveal-history entry with `route=2` and the current non-empty Settlement `sessionId`. Full victory records hidden/appearing/stable fill-and-outline counts `0/0 → 316/749 → 316/748`; inset defeat records `0/0 → 233/657 → 230/651`. Each cycle's retained history tail is exactly `hidden → appearing → stable` for one session. The outcome-only comparison windows record `1617` changed pixels with actual bounds `[170,159,231,190]` inside `[140,158,262,192]` full, and `1962` with `[144,172,258,203]` inside `[144,171,258,204]` inset.
- All six measured metric-row edge bands have a maximum closed brown-border run fraction of `0.0`.

## Manual review

The large shell backdrop is intentionally the most visible new resource and materially improves route identity and value depth. The three Battle micro icons now read at actual size and have stronger negative-space separation, but their labels remain compact by design; further visual growth should come from new page-specific hero/illustration masters, not by enlarging the header or adding more ornament to Battle.

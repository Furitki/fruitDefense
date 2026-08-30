# Eight-point candidate Battle Gate A

This is the new user-review gate. Automated acceptance passed, but the
candidate is not called `8/10` until the user explicitly scores or approves it.

## Review images

- Three-way ready-state comparison:
  [comparison-reference-approved7-candidate.png](comparison-reference-approved7-candidate.png).
- New candidate four-state contact sheet:
  [candidate-four-state-contact-sheet.png](candidate-four-state-contact-sheet.png).
- Full 402x874 evidence:
  [ready](402x874-full/01-ready.png),
  [active](402x874-full/02-active-wave.png),
  [paused](402x874-full/05-paused.png), and
  [selected tool](402x874-full/09-selected-tool.png).
- Representative 402x874 safe-area evidence (`top=44`, `bottom=34`):
  [ready](402x874-inset-44-34/01-ready.png),
  [active](402x874-inset-44-34/02-active-wave.png),
  [paused](402x874-inset-44-34/05-paused.png), and
  [selected tool](402x874-inset-44-34/09-selected-tool.png).

Both manifests report `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`:

- [402x874 full acceptance](402x874-full/acceptance.json)
- [402x874 inset acceptance](402x874-inset-44-34/acceptance.json)

## Production identities

- Runtime UI: `ui.sunny-orchard@6 / sunny-orchard-painted@5`.
- Active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`.
- Action production plate SHA-256:
  `636D372D867A75BF1B5A0E3E31B2BB2E3CF35A9E8692F73A01327DF202BC9E36`.
- Structural production plate SHA-256:
  `EEFF38C2C504FB81C40DB4C19EC1ABC01266FE519A92C6AF18E77837D755A5BF`.
- Theme asset SHA-256:
  `2F662DF87A53A038803DD054F2AC752AD810A31F7F549D91A78D93A45FF4243E`.
- ArtSet asset SHA-256:
  `01A065197081BCD36C8C8B77A6213A86511E9B76D4213A7AEFCB7823E3A6B9BE`.
- Display font SHA-256:
  `6B5F7097630A9236B33B38C365CECBD8BC64062ACADF9EAC907C09D10F0D2EE9`.
- Reading font SHA-256:
  `80F96E594CA0803386487D2D27CA45184E7807BAEB6B02731B9A2F03EAD12CDD`.

The fourteen selected material destinations retain their baseline GUIDs and
slice/safe geometry. The final exporter repeatability check covered 58 source,
runtime, manifest, ArtSet, and `.meta` files with zero byte drift; all fourteen
translucent-magenta fringe counts are zero.

## Build and automated gates

- `FruitDefense.Editor.ProjectSetup.SmokeValidate` passed with
  `RUNTIME_UI_VISUAL_SYSTEM_SMOKE_OK` and `FRUIT_DEFENSE_SMOKE_OK` in
  `Logs/polish-sky-paper-ui-eight-point-smoke.log`.
- Final-pixel minimum contrast: primary `5.2341:1`, secondary `5.9471:1`,
  danger `5.4461:1`, active compact control normal `7.9777:1`, and active
  compact control disabled `6.0665:1`.
- Ordinary release build passed through `FruitDefense.Editor.WebBuild.Build`:
  `profile=release`, payload size `12667233`, versions
  `data=80ce456558c5`, `framework=105600a04ca3`, `loader=22dc03a9e48a`,
  `wasm=547293f46d1a`.
- The same-code acceptance payload passed:
  `profile=acceptance`, payload size `12719683`, versions
  `data=4ccd6da30540`, `framework=18e5add4f294`, `loader=364f789f66f7`,
  `wasm=5380a62c4d7c`.
- Full and inset acceptance preserve the same composite level identity,
  runtime theme/ArtSet identity, declared control map, draw/hit projection,
  text containment, panel geometry, compact-control lifecycle, and required
  ready/active/paused/selected-detail state sequence.

Reference pointer centers at 402x874 include header pause `(288,74)`, header
speed `(346,74)`, wave action `(291,544)`, pause continue `(125,492)`, pause
restart `(277,492)`, first tool `(71,634)`, first nursery slot `(61,732)`,
first board cell `(46,249)`, and detail close `(352,604)`.

## Generation boundary

The action and structural materials are built-in ImageGen pixels. Production
uses only fixed semantic crop composition, chroma exterior removal/despill,
transparent padding, alpha-safe resize, measurement, hashing, and export. No
script paints or recolors their face, rim, outline, highlight, shadow, dashed
rail, or stage frame. Exact prompts, selected output identities, rejected
attempts, cell maps, and hashes are recorded in
[the ImageGen evidence](../imagegen/README.md) and the production
`prompt-record.json`.

The failed dark active-control correction and the rejected top-only stage-rail
correction remain review evidence only. Neither is referenced by the exporter,
manifest, ArtSet, theme, or runtime.


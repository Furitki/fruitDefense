# UI reset evidence index

## ImageGen action-material Battle Gate A — user approved

The 6/10 V2 layout is retained, but all six action surfaces are now rebuilt from
one reviewed ImageGen component sheet. The exporter only performs fixed crop,
exterior chroma-alpha cleanup, transparent padding, alpha-safe resize,
measurement, hashing, and export; it no longer owns a procedural button-painting
recipe or fallback.

- Runtime identity: `ui.sunny-orchard@5 / sunny-orchard-painted@4`.
- Selected generated sheet:
  [imagegen-action-material-sheet-chroma.png](imagegen/imagegen-action-material-sheet-chroma.png),
  SHA-256 `7F06E07A56F943B9FD2178F45678AD5C6211C5A7A389B86155B04413F218FC56`.
- Fixed-source repeatability: two consecutive exports produced zero hash drift
  across the six source masters, six runtime PNGs, manifest, and ArtSet asset.
- Final measured warm-white content contrast: primary `5.533:1`, secondary
  `9.179:1`, danger `5.839:1`.
- Acceptance payload: loader `364f789f66f7`, data `ca397e80b8c8`, framework
  `18e5add4f294`, wasm `5380a62c4d7c`.
- Canonical same-payload evidence:
  [acceptance.json](rework-imagegen-gate-a/402x874-full/acceptance.json),
  [ready](rework-imagegen-gate-a/402x874-full/01-ready.png),
  [active](rework-imagegen-gate-a/402x874-full/02-active-wave.png),
  [paused](rework-imagegen-gate-a/402x874-full/05-paused.png), and
  [selected tool](rework-imagegen-gate-a/402x874-full/09-selected-tool.png).
- Reference comparison:
  [comparison-reference-imagegen.png](rework-imagegen-gate-a/comparison-reference-imagegen.png).
- Aggregate Unity smoke: `FRUIT_DEFENSE_SMOKE_OK` in
  `Logs/reset-sky-paper-imagegen-action-smoke.log`.
- Acceptance WebGL build: `FRUIT_DEFENSE_ACCEPTANCE_WEB_BUILD_OK` in
  `Logs/reset-sky-paper-imagegen-action-webgl-gate-a.log`.

The user explicitly approved this Gate A candidate. Their comparative visual
ratings were `3/10` for the superseded procedural-button implementation,
`8.5/10` for the first ImageGen visual output, `7.5/10` for the production
solid-chroma/extracted action surfaces, and approximately `7/10` for the final
composed page. The user also recorded one non-blocking follow-up observation:
the current green is darker and less fresh than the supplied reference. That
observation is not claimed as fixed in this approved payload; any lighter-green
revision requires a new ImageGen edit and visual review while preserving the
published content-contrast threshold.

## Superseded V2 Battle Gate A — user rated 6/10

The rejected 3/10 canvas was replaced for the Battle vertical slice. This
superseded candidate follows the supplied reference's component hierarchy and relative
402×874 composition: floating two-row Header, three metric capsules, yellow
compact controls, one large paper page, inset soil stage, paired phase/Wave
actions, four recipe cards, five dashed nursery slots, and one full-width green
refresh action. The user rated it 6/10 and required ImageGen-generated or
generated-preview-cropped button resources before another Gate A review.

- Superseded identity: `ui.sunny-orchard@4 / sunny-orchard-painted@3`.
- Active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`.
- Display/control face: `FruitDefense-OrchardDisplay-400.ttf`, SHA-256
  `6b5f7097630a9236b33b38c365cecbd8bc64062acadf9eac907c09d10f0d2ee9`.
- Reading/metric face: `NotoSansSC-Reading-400.ttf`, SHA-256
  `80f96e594ca0803386487d2d27ca45184e7807baeb6b02731b9a2f03ead12cdd`.
- Deterministic art aggregate SHA-256:
  `1E3B656D4852649C7D0F59C16099AF2CF2B5EECF97D812BD3BD17AECF8311022`.
- Acceptance payload: loader `94cab099e787`, data `64668872e68e`, framework
  `0b5cf9bd437a`, wasm `3af7fd4f7d31`.
- Canonical full-state manifest:
  [rework-v2-gate-a/402x874-final/acceptance.json](rework-v2-gate-a/402x874-final/acceptance.json).
- Canonical review frames:
  [ready](rework-v2-gate-a/402x874-final/01-ready.png),
  [active](rework-v2-gate-a/402x874-final/02-active-wave.png),
  [paused](rework-v2-gate-a/402x874-final/05-paused.png), and
  [selected tool](rework-v2-gate-a/402x874-final/09-selected-tool.png).
- Reference comparison:
  [comparison-reference-v2.png](rework-v2-gate-a/comparison-reference-v2.png).
- Aggregate Unity smoke: `FRUIT_DEFENSE_SMOKE_OK` in
  `Logs/reset-sky-paper-rework-v2-final-smoke.log`.
- Acceptance WebGL build: `FRUIT_DEFENSE_ACCEPTANCE_WEB_BUILD_OK` in
  `Logs/reset-sky-paper-rework-v2-webgl-gate-a.log`.

## Rejected V1 engineering evidence

> **Visual status: rejected.** The user rated this implementation 3/10 for
> reference similarity: the palette was related, but the button decomposition
> and page composition were not. The manifests below remain useful engineering
> evidence for geometry, input, fonts, and WebGL delivery; they are not current
> visual acceptance and must not authorize route-wide rollout.

This index owns the verified evidence for `reset-sky-paper-orchard-ui`. The
supplied reference image is interpretation-only evidence; no production scene,
asset, or runtime code depends on it.

## V1 engineering baseline (not visually accepted)

- Release identity: `ui.sunny-orchard@3 / sunny-orchard-painted@2`.
- Active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`.
- Production inventory: 56 semantic slots, 54 unique text-free PNG exports, one
  complete locally owned active treatment.
- Display/control face: `NotoSansSC-Display-700.ttf`, SHA-256
  `5e673e3c73b37f2d4a5f6544e102c737cce52b0a8a77c1959fb3538cdd587ddf`.
- Reading/metric face: `NotoSansSC-Reading-400.ttf`, SHA-256
  `80f96e594ca0803386487d2d27ca45184e7807baeb6b02731b9a2f03ead12cdd`.
- Deterministic exporter evidence: [imagegen/deterministic-export.md](imagegen/deterministic-export.md).
- Reference scope and hierarchy interpretation: [reference/reference-notes.md](reference/reference-notes.md).

## Build identities

- Final acceptance payload: loader `bbfea3324700`, data `bee994b34594`,
  framework `778fb14f0cef`, wasm `832f735906e7`.
- Final ordinary Release WebGL payload: loader `7192d4d9f558`, data
  `720771fe35bb`, framework `71aac2cb275e`, wasm `733bada9e4e7`.
- Build logs: `Logs/reset-sky-paper-webgl-acceptance-final-handshake.log` and
  `Logs/reset-sky-paper-webgl-release-final.log`.

The acceptance build differs from the ordinary Release build only by the
deterministic acceptance instrumentation used to expose route, hit-target, and
settlement-reveal identities. Every canonical Gate A and Gate B capture below
uses the final acceptance payload identity above.

## Rejected Gate A — Battle vertical slice

- [402×874 full](after-webgl-402x874/full-final/acceptance.json)
- [402×874 safe-top 44 / safe-bottom 34](after-webgl-402x874/inset-44-34-final/acceptance.json)

Both manifests pass automated ready, active, paused, selected-detail,
compact-control, pointer, projection, and interaction-polish checks. They failed
the human reference-fidelity gate because the final canvas retained generic flat
buttons, a different Header, no reference-like paper page shell, and unrelated
recipe/nursery component anatomy.

## V1 route-wide engineering evidence (not rollout authorization)

- Full flows: [360×800](gate-b/360x800-full-flow/flow-acceptance.json),
  [375×812](gate-b/375x812-full-flow/flow-acceptance.json),
  [402×874](gate-b/402x874-full-flow/flow-acceptance.json), and
  [430×932](gate-b/430x932-full-flow/flow-acceptance.json).
- Safe-area flow: [402×874, top 44 / bottom 34](gate-b/402x874-inset-44-34-full-flow/flow-acceptance.json).
- Wide-host fit: [1280×720](gate-b/1280x720-wide-flow/flow-acceptance.json).
- Reveal/motion sequence: [402×874 interaction evidence](gate-b/402x874-flow-interaction/flow-acceptance.json).
- Bootstrap and alternate selection: [shell visual evidence](gate-b/shell-visual-orchard-02-402x874/shell-visual-evidence.json).
- Recoverable bootstrap failure: [shell error evidence](gate-b/shell-error-402x874/shell-error-evidence.json).

The canonical manifests verify current payload delivery, warm-cache identity,
safe containment, text bounds, optical padding, exact connected outcome
outline, authoritative pointer targets, Return, Retry, and complete centered
wide-host fit. The `failed-*`, `passed-*-before-*`, and `probe-*` directories are
retained as closed diagnostic history and are not canonical acceptance.

## Automated validation

- Aggregate Unity smoke: `FRUIT_DEFENSE_SMOKE_OK` in
  `Logs/reset-sky-paper-ui-final-smoke.log`.
- UI quality matrix: `RUNTIME_UI_QUALITY_OK cases=80 viewports=4` in
  `Logs/reset-sky-paper-ui-quality-release-publish.log`.
- Acceptance analyzer self-check: `FRUIT_DEFENSE_ACCEPTANCE_SELF_CHECK_OK` in
  `Logs/reset-sky-paper-acceptance-selfcheck-final.log`.
- OpenSpec strict validation: `openspec validate reset-sky-paper-orchard-ui
  --strict` passed after this index and `tasks.md` were synchronized.

## Scope boundary

This evidence proves the ordinary WebGL shared baseline only. It does not prove
Douyin or WeChat conversion, simulator behavior, device integration, or release
authorization for either mini-game platform. The stable game-design overview
remains unchanged because no core loop or gameplay-direction decision changed.

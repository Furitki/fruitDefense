# Final runtime UI quality audit

Status: **pass** for the ordinary WebGL runtime UI quality contract.

This is a pass/fail audit, not an aesthetic score. It replaces the withdrawn
rectangle-only `100/100` conclusion. The [5.1 redline audit](5.1-redline-audit/README.md)
records why that conclusion was invalid; the [before/after index](5.9-final-acceptance/before-after.md)
shows the reviewed corrections, and the [original-resolution manual audit](5.9-final-acceptance/manual-audit.md)
records the final visual disposition.

## Release and evidence identity

- Unity: `6000.3.19f1`
- Runtime UI: `ui.sunny-orchard@1 / sunny-orchard-painted@1`
- Active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`
- Theme GUID: `932a9783b6b3eee45970ab3ef84a4c39`
- Contract: 49 required semantic slots. Both production ArtSets validate against
  that complete contract and the same optical/resource quality profile.
- Final evidence: [ordinary-WebGL acceptance](5.9-final-acceptance/README.md),
  [desktop-host acceptance](5.3-webgl-host/README.md), and the
  [canonical portrait matrix](5.9-final-acceptance/README.md#acceptance-matrix).

All 17 retained acceptance JSON documents have `accepted=true` and record the
same four payload hashes. The evidence tree contains 90 retained PNGs: 87
canonical host/route images plus three explicitly classified infrastructure
regression images.

| Ordinary WebGL payload | SHA-256 |
| --- | --- |
| loader | `CFAA2D82D6D07C12674952310A75B305ECBB1BC55F3C302F8E29C114C5C5DC76` |
| data | `7FA3EF3D7C43FA535FEF4EA935C85866FBBFFBE36198CD54256302D97431BE56` |
| framework | `0C5ECD20FC1C192495E6C368F0642CB5CB2937296CAFAE6130224EA9262081E6` |
| wasm | `B0689F4279535D61F913E108EF6B61B8BD071A066856DF94EF74C20EE0113C4E` |

## Pass/fail audit

| Quality area | Result | Final evidence |
| --- | --- | --- |
| Desktop host containment and input | **Pass** | The portrait canvas uses uniform contain at 1280×720, 1440×900, and 1024×640; it is centered, fully visible, has no document scroll, and preserves exact canvas-relative pointer round trips. The stable localhost service also invalidates ETags after in-place file replacement, so an ordinary reload receives the current host. See the [host audit](5.3-webgl-host/README.md) and [localhost cache audit](5.11-local-server-cache.md). |
| Packaged Chinese typography and finite copy | **Pass** | Shared Noto Sans SC styles validate every finite copy across 360/375/402/430 full and inset geometries. No required line clips, shrinks implicitly, escapes its authority, or collides with an icon/action. |
| Action and repeated-metric visual groups | **Pass** | Every icon-plus-label action centers the rendered ink union within 2 logical points with a 4–8 gap and stroke clearance. Peer metric icon centers/baselines differ by at most 1, and icon ink stays at least 8 inside the row. |
| Battle projection and chrome geometry | **Pass** | Opposite battlefield gutters differ by at most 1 logical point. Drawing and hit testing consume the same projection; grid coordinates, commands, simulation, and interaction priority remain unchanged. Header, status, tray, detail, pause, and terminal groups are contained. |
| Lobby, terminal, and Settlement composition | **Pass** | Lobby and Settlement occupy the portrait page with intentional rhythm rather than accidental lower dead space. The Battle terminal banner carries finite `胜利`/`失败` semantics, and route actions retain one draw/hit authority. See the [before/after index](5.9-final-acceptance/before-after.md). |
| Contrast and non-color state cues | **Pass** | Normal, selected, pressed, loading, disabled, success, warning, error, modal, and drag states use semantic tokens and retain independent shape/icon/text cues. Shell primary-action raster contrast is at least `5.6767:1` in the final matrix. |
| Resource and nine-slice quality | **Pass** | Both production sets satisfy the 49-slot contract, importer/manifest/ArtSet identity, optical bounds, alpha-edge, protected nine-slice, illustration-aspect, ownership, unbound-file, and release-dependency gates. See the [final resource audit](final-resource-audit/README.md). |
| Required states, routes, and real input | **Pass** | Bootstrap Loading/error, Lobby default/selected/Loading, Battle ready through terminal/restart, and Settlement victory/defeat/Return/Retry are present. Start, selection, pause, restart, Return, and Retry use real input with no visible or measured hit drift. |
| Canonical/manual review integrity | **Pass** | Native PNG review found no default/legacy skin, mixed-set chrome, CJK clipping, overlap, nine-slice seam, ornament stretch, transparent/black hole, or visually off-center required composition. Infrastructure misses are retained outside canonical evidence and are not product passes. |

There are **0 open Blocker, 0 open High, and 0 open Medium** product defects.
The [closed severity inventory](severity-ranked-defects.md#final-closure-ledger)
preserves the original 1 Blocker, 10 High, and 4 Medium findings and the later
alignment-redline closure without converting any failed observation into a
relaxed threshold.

## Unity gates and API entry points

| Gate | Authoritative entry | Final result |
| --- | --- | --- |
| Finite copy, visual groups, route balance, contrast, and source authority | [`RuntimeUiQualitySmoke.Run`](../../../../Assets/Editor/Tests/RuntimeUiQualitySmoke.cs) | `RUNTIME_UI_QUALITY_OK cases=59 viewports=4` |
| Battle layout/projection and interaction boundaries | [`BattleUiLayoutSmoke.Run`](../../../../Assets/Editor/Tests/BattleUiLayoutSmoke.cs) | `FRUIT_DEFENSE_BATTLE_UI_LAYOUT_OK` |
| Release resources and dependency graph | [`RuntimeUiVisualSystemValidator.ValidateReleaseOrThrow`](../../../../Assets/Editor/Tools/RuntimeUiVisualSystemValidator.cs) | zero validation errors/warnings |
| Candidate preview, activation, Undo/Redo, and restoration | [`RuntimeUiVisualSystemSmoke.Run`](../../../../Assets/Editor/Tests/RuntimeUiVisualSystemSmoke.cs) | both 49-slot sets preview; inactive production candidate activates atomically and the approved active theme is restored byte-for-byte |
| UI plus project aggregate | [`ProjectSetup.SmokeValidate`](../../../../Assets/Editor/Tools/ProjectSetup.cs) | `FRUIT_DEFENSE_SMOKE_OK` |
| Simulation/session preservation | `DeterministicSimulationSmoke.Run` and `BattleSessionHostSmoke.Run` | both pass without command, session, or simulation drift |
| Unique aggregate release gate | [`P0ValidationSuite.Run`](../../../../Assets/Editor/Tests/P0ValidationSuite.cs) | `FRUIT_DEFENSE_P0_RELEASE_GATE_OK` |

The stable daily authoring entry remains `Fruit Defense/UI/Visual System`.
Release validation is aggregated through Project Smoke and the unique P0 gate;
there are no disposable per-check daily menus.

## Resource replacement proof

- `sunny-orchard@1` and `sunny-orchard-painted@1` each resolve all 49 required
  semantic bindings and pass the same release-quality validator. Explicitly
  declared shared ownership is validated; it is not inheritance or fallback.
- Isolated preview renders the component/state gallery and representative route
  chrome without modifying the serialized release theme or any release scene.
- Activation validates first and changes only the theme's active ArtSet in one
  named Undo group. Undo and Redo preserve code, scenes, layout, identity, and
  interaction geometry; final restoration reproduces the starting theme bytes.
- Invalid candidates cause no mutation. In-place export/reimport keeps stable
  paths and `.meta` GUIDs, so every semantic consumer updates without presenter,
  scene, prefab, layout, or hit-geometry edits.
- Deterministic source/export and ownership evidence is consolidated in the
  [resource inventory](resource-inventory/README.md),
  [painted-set remediation](resource-polish/README.md),
  [alternate-set remediation](old-set-resource-polish/README.md), and
  [final resource audit](final-resource-audit/README.md).

## Manual review boundary

The [manual audit](5.9-final-acceptance/manual-audit.md) is user-review-ready
evidence that the stated product gates pass. It deliberately does not assign a
beauty, taste, or artistic-quality score on the user's behalf; future art
direction may still be reviewed without weakening these release gates.

This evidence proves only the ordinary WebGL build and the project-owned Unity
runtime UI contract. It does **not** prove Douyin or WeChat conversion,
simulator/device compatibility, package compliance, payment/ads integration,
or any mini-game adapter. Those targets remain governed by separate platform
gates and cannot silently fall back to the Web adapter.

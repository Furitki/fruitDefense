# 4.3 canonical ordinary WebGL matrix

Status: **accepted** on 2026-08-20 after task 4.2 preflight passed.

All runs use the exact payload and release identity recorded in
[the 4.2 preflight](../4.2-preflight/README.md):
`ui.sunny-orchard@1 / sunny-orchard-painted@1`, active ArtSet GUID
`91aa538ae02449cba8c971ffe4d427eb`, payload versions
`34035c24f91b / 1ca6385231e1 / 2311a6f94ac3 / ba2503f395b7`.

## ShellVisual matrix

| Viewport | Full | Representative inset |
|---|---|---|
| 360 x 800 | [0/0](shell-visual/360x800-full/shell-visual-evidence.json) | [32/24](shell-visual/360x800-inset32-24/shell-visual-evidence.json) |
| 375 x 812 | [0/0](shell-visual/375x812-full/shell-visual-evidence.json) | [40/21](shell-visual/375x812-inset40-21/shell-visual-evidence.json) |
| 402 x 874 | [0/0](../4.2-preflight/canonical/shell-visual-402x874-full/shell-visual-evidence.json) | [44/34](../4.2-preflight/canonical/shell-visual-402x874-inset44-34/shell-visual-evidence.json) |
| 430 x 932 | [0/0](shell-visual/430x932-full/shell-visual-evidence.json) | [50/36](shell-visual/430x932-inset50-36/shell-visual-evidence.json) |

The actual Bootstrap initializing frame was captured without a loader or
runtime hook in
[360 x 800 inset](shell-visual/360x800-inset32-24/00-bootstrap-initializing.png).
Other manifests truthfully record `not-captured` when the finite frame completed
before a stable screenshot. Formal 402 full/inset Bootstrap error evidence is
linked below.

## 402 cross-route matrix

The 402 evidence was generated as the blocking preflight and is reused here;
it is not duplicated or represented as a second execution.

| Mode | Full | Inset 44/34 |
|---|---|---|
| ShellVisual | [manifest](../4.2-preflight/canonical/shell-visual-402x874-full/shell-visual-evidence.json) | [manifest](../4.2-preflight/canonical/shell-visual-402x874-inset44-34/shell-visual-evidence.json) |
| ShellError | [manifest](../4.2-preflight/canonical/shell-error-402x874-full/shell-error-evidence.json) | [manifest](../4.2-preflight/canonical/shell-error-402x874-inset44-34/shell-error-evidence.json) |
| Direct Battle | [manifest](../4.2-preflight/canonical/battle-402x874-full/acceptance.json) | [manifest](../4.2-preflight/canonical/battle-402x874-inset44-34/acceptance.json) |
| Flow victory | [manifest](../4.2-preflight/canonical/flow-victory-402x874-full/flow-acceptance.json) | [manifest](../4.2-preflight/canonical/flow-victory-402x874-inset44-34/flow-acceptance.json) |
| Flow defeat | [manifest](../4.2-preflight/canonical/flow-defeat-402x874-full/flow-acceptance.json) | [manifest](../4.2-preflight/canonical/flow-defeat-402x874-inset44-34/flow-acceptance.json) |

## Manifest and manual audit

- 16 manifests were parsed: 10 cross-route/preflight plus 6 additional Shell
  runs. Every manifest has `accepted=true` and one release identity/GUID.
- All manifests report the same four content SHA-256 values. ShellError stores
  the same values as asset versions/ETags; it does not emit duplicate payload
  objects.
- 83 canonical PNGs were reviewed at native resolution: 64 under 4.2 and 19
  here, including the real Bootstrap initializing frame.
- Lobby default/alternate/Loading remained aligned at all eight transforms;
  input identities changed only after the expected level and Start clicks.
- Battle route/session identity stayed authoritative across pause, restart,
  selected tool, legal/illegal drag, detail, terminal preview, and preview
  restart. Actual Battle-to-Settlement submission is proven by both Flow modes.
- Flow manifests prove Lobby -> Battle -> Settlement -> Lobby and Retry ->
  Battle using new session identities while preserving level/map/wave/rule/theme
  identity inside each session.
- Manual review found no default skin, legacy chrome, mixed ArtSet, clip,
  stretch, overlap, seam, transparent/black hole, CJK collision, contrast
  regression, or input drift. Header metrics, tray titles, Loading/Disabled
  semantics, Lobby/Settlement vertical rhythm, vista aspect, result cues, and
  Return/Retry were specifically checked.

This is canonical evidence for ordinary WebGL only.


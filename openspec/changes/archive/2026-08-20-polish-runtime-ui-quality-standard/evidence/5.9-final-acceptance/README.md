# 5.9 final WebGL visual acceptance

Status: **accepted** for the ordinary WebGL baseline. This evidence does not
claim mini-game platform support and does not assign an aesthetic score on the
user's behalf.

## Frozen payload and runtime UI identity

All canonical captures resolve the same release identity:

- Theme: `ui.sunny-orchard@1`
- ArtSet: `sunny-orchard-painted@1`
- active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`
- loader: `CFAA2D82D6D07C12674952310A75B305ECBB1BC55F3C302F8E29C114C5C5DC76`
- data: `7FA3EF3D7C43FA535FEF4EA935C85866FBBFFBE36198CD54256302D97431BE56`
- framework: `0C5ECD20FC1C192495E6C368F0642CB5CB2937296CAFAE6130224EA9262081E6`
- wasm: `B0689F4279535D61F913E108EF6B61B8BD071A066856DF94EF74C20EE0113C4E`

The manifests cross-check as 17 accepted JSON documents: one desktop-host
manifest with three host matrices, eight Shell manifests, two Bootstrap error
manifests, two direct-Battle manifests, and four end-to-end Flow manifests.
There are 90 retained PNGs: 87 canonical images (three desktop-host plus 84
portrait route/state screenshots) and three useful infrastructure-regression
screenshots. The 17 retained JSON files are all canonical manifests.

## Acceptance matrix

- Desktop contain: 1280x720, 1440x900, 1024x640. The logical canvas remains
  402x874, is uniformly contained and centered, has no page scroll, and the
  Lobby pointer round-trip error is zero.
- Shell: 360x800, 375x812, 402x874, and 430x932, each full and with the approved
  top/bottom inset pair. Every case contains default, alternate selection, and
  Loading/transition evidence. The two available initializing frames are also
  retained.
- 402x874 full/inset cross-route: formal Bootstrap error, direct Battle, Flow
  victory, and Flow defeat. Together with the shared 402 Shell cases this is
  the required ten-group cross-route matrix.

Canonical evidence is under [`canonical/`](canonical/); desktop host evidence
is under [`host-preflight/`](host-preflight/). The itemized original-resolution
review is [`manual-audit.md`](manual-audit.md).

## Manual result

Original PNG review passed the product gates rather than relying on each
script's `accepted` flag. The desktop frame is completely visible; Lobby card
titles, thumbnail/copy groups, markers, and the centered Start group are intact.
Battle header metrics share baselines without edge contact; opposite battlefield
gutters remain balanced; status, wave action, tray headings, and detail do not
overlap. Selected, legal, and illegal cues are visually distinct. The compact
plant-detail copy is complete in two lines, while between-wave copy remains one
complete line. Pause and victory/defeat terminal compositions have populated
outcome banners, vistas, messages, indicators, and centered Restart actions.
Settlement victory/defeat preserves the enlarged vista, contained metric icons,
centered Retry/Return groups, and balanced lower space in full and inset modes.

No reviewed canonical image showed Unity default skin, legacy/mixed-set chrome,
Chinese clipping, nine-slice seams, stretched ornaments, transparent/black
background holes, action-group drift, or visible draw/hit drift. Shell primary
action contrast is at least `5.6767:1` across all 24 default/selected/Loading
samples, above the `3.0:1` large-action gate.

## Instrumentation and regressions

The stable portrait acceptance control points were synchronized to the final
single layout/hit authority (Lobby card centers, Lobby Start, Settlement Retry
and Return); `-SelfCheck` passed. No runtime timing or product geometry was
changed. The canonical Shell matrix used the already-supported CPU-throttle 20
capture path and every transition was captured on attempt 1.

Two bounded infrastructure failures are retained under
[`regressions/`](regressions/README.md): the pre-correction stale control point
and a CPU-throttle 8 transition miss. Neither is canonical or a product pass.
Per the cleanup contract, their useful screenshots remain while capture-server
stdout/stderr files were removed.

## Reproduction

The evidence was produced with `scripts/accept-webgl-host.ps1` for the desktop
host and `scripts/accept-webgl-portrait.ps1 -ServeLocal` for Shell, ShellError,
direct Battle, and Flow modes. The portrait output used an isolated random port
and browser profile per command. All owned Chrome instances and random capture
servers were released; the user-managed port 4173 service was not touched.

The before/after index is [`before-after.md`](before-after.md). The previous
100/100 conclusion remains invalidated; replacement scoring is outside task 5.9.

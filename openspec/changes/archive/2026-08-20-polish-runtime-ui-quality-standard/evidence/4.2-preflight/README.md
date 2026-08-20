# 4.2 ordinary WebGL quality preflight

Status: **accepted** on 2026-08-20.

This preflight used a newly generated ordinary WebGL player. It does not claim
Douyin or WeChat mini-game support. The user-visible server on port 4173 was
not stopped, rebound, or replaced.

## Release identity and payload

- Runtime UI: `ui.sunny-orchard@1 / sunny-orchard-painted@1`
- Active ArtSet GUID: `91aa538ae02449cba8c971ffe4d427eb`
- Build log: [webgl-build.log](webgl-build.log), including
  `FRUIT_DEFENSE_WEB_BUILD_OK` and Unity return code 0.

| Payload | Bytes | SHA-256 |
|---|---:|---|
| `WebGL.loader.js` | 117,893 | `34035C24F91B91E9E6AEB447743A0031D761F433D034614758E6F0CD4E65519F` |
| `WebGL.data.unityweb` | 5,575,646 | `1CA6385231E1AC7E9E1FE1085CF5778FCC662573BD4A2119A7E29DDB5F16D443` |
| `WebGL.framework.js.unityweb` | 69,099 | `2311A6F94AC3A7402E5DDF95F625C21D6B5607DFAA10951A830A1C76AA446FD0` |
| `WebGL.wasm.unityweb` | 3,864,044 | `BA2503F395B7C2CA05A2140A32F8B383FD1BCF5F12D63BAD9D4C1D526FBF90BA` |
| `index.html` | 5,907 | `931B2343B58589CEE6CC12E1398BF03E0E2B7E6BD46D74632100ED8EA8AF6E13` |

## 402 x 874 full/inset runs

Every manifest below is accepted and reports the identity, GUID, payload
versions, requested viewport, safe-area transform, and route/input checks.

| Mode | Full | Inset 44/34 |
|---|---|---|
| ShellVisual | [manifest](canonical/shell-visual-402x874-full/shell-visual-evidence.json) | [manifest](canonical/shell-visual-402x874-inset44-34/shell-visual-evidence.json) |
| ShellError | [manifest](canonical/shell-error-402x874-full/shell-error-evidence.json) | [manifest](canonical/shell-error-402x874-inset44-34/shell-error-evidence.json) |
| Direct Battle | [victory manifest](canonical/battle-402x874-full/acceptance.json) | [defeat manifest](canonical/battle-402x874-inset44-34/acceptance.json) |
| Flow victory | [manifest](canonical/flow-victory-402x874-full/flow-acceptance.json) | [manifest](canonical/flow-victory-402x874-inset44-34/flow-acceptance.json) |
| Flow defeat | [manifest](canonical/flow-defeat-402x874-full/flow-acceptance.json) | [manifest](canonical/flow-defeat-402x874-inset44-34/flow-acceptance.json) |

The script's authoritative input centers were synchronized with the approved
Lobby, Settlement, terminal action, tool tray, and nursery rectangles before
capture. Real clicks exercised selection, Start, terminal restart, Return, and
Retry; the Battle manifests also preserve distinct available/selected hashes
and legal/illegal drag evidence.

## Original-resolution review

- **Bootstrap:** the formal invalid-level path shows the compact application
  modal, one error cue, finite copy, continuous opaque paper background, and no
  raw diagnostic detail in both 402 geometries. The short normal initializing
  frame was not stable in these two runs; a real, non-hooked capture is retained
  in the 360 inset Shell matrix and shows the compact Loading presentation with
  spinner and readable copy.
- **Lobby:** the three 136-point cards, 16:9 orchard thumbnails, marker, title,
  body copy, selected state, primary CTA, and Loading cue remain contained. The
  measured primary-label contrast is `5.7477:1` for normal, selected, and
  transition captures. The new lower CTA input maps to the same drawn rectangle.
- **Battle:** all three header metrics are readable and baseline-aligned; tray
  titles clear their borders; slot labels and icons stay contained. Ready,
  active, between-wave, immediate-next-wave, pause/continue/restart,
  selected-tool, legal/illegal drag, dense board, plant detail, terminal result,
  and terminal restart were reviewed. Compact two-line status copy is complete;
  between-wave remains a single line; no battlefield interaction geometry
  drift was observed.
- **Settlement:** victory and defeat retain one state cue, a visible 16:9 vista,
  separated metric rows, localized level names, and lower Retry/Return actions.
  Both actions route correctly and remain aligned in full and inset layouts.
- **Shared rendering:** no default Unity skin, legacy/mixed ArtSet, black or
  transparent hole, double background, nine-slice seam, stretched leaf, clip,
  overlap, or unsafe-area escape was found at original resolution.

The first full Shell attempt at CPU throttle 8 lost the deliberately short
Lobby Loading frame to the route transition and failed closed. It is retained
as [acceptance-infrastructure regression evidence](regressions/README.md).
The bounded rerun used the script's existing throttle parameter at 20 and
captured a valid route-0 Loading frame; no runtime timing was changed.


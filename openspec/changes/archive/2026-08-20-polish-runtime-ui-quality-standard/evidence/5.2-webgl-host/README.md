# 5.2 project-owned WebGL host

Status: **pass**.

The stable WebGL workflow now selects
`PROJECT:FruitDefensePortraitContain`. The host is authored under
`Assets/WebGLTemplates/FruitDefensePortraitContain/`; no host CSS or layout
script is patched into `Builds/WebGL` after Unity builds it.

## Contract

- Logical desktop canvas: `402×874`.
- Desktop scale: `min(innerWidth / 402, innerHeight / 874)`.
- The scaled canvas is centered on both axes and all four edges remain inside
  the viewport.
- `html`, `body`, and the fixed host hide overflow and overscroll; page scroll
  offsets remain zero.
- The desktop render target remains `402×874`; CSS supplies the sole visual
  scale, so Unity's native DOM-to-canvas pointer mapping remains authoritative.
- `resize`, `orientationchange`, `visualViewport.resize`, and resolution-media
  changes re-run the same layout function. The resolution listener is re-armed
  after a device-pixel-ratio change.
- The existing mobile portrait path remains a full-viewport host so the
  established portrait acceptance baseline is not replaced by a desktop-only
  behavior.

The project selection is serialized in `ProjectSettings/ProjectSettings.asset`.
`FruitDefense.Editor.WebBuild.Build` also explicitly selects the same template,
validates required source markers, and rejects missing or empty host files.
After Unity builds, it compares the project-owned CSS and JS to the generated
files byte-for-byte before applying the pre-existing payload versioning and
acceptance-instance injection.

## Source self-check

[`source-selfcheck/host-source-self-check.json`](source-selfcheck/host-source-self-check.json)
drives the project-owned CSS/JS in headless desktop Chrome without Unity. One
browser document is resized through all cases, including a DPR transition:

| Viewport | DPR | Expected/actual scale | Canvas rect | Backing | Scroll |
| --- | ---: | --- | --- | --- | --- |
| `1280×720` | 1 | `0.8237986270` | `(474.421875,0,331.15625,720)` | `402×874` | `0,0` |
| `1024×640` | 2 | `0.7322654462` | `(364.8125,0,294.359375,640)` | `402×874` | `0,0` |
| `1440×900` | 1 | `1.0297482838` | `(513.015625,0,413.953125,900)` | `402×874` | `0,0` |

Every canvas center is within `0.51` CSS px of the viewport center, every edge
is contained, and document dimensions equal the viewport. The self-check uses
a random local port and a disposable Chrome profile; it leaves no server log or
profile in the repository.

## Unity and build gates

- [`unity/compile.log`](unity/compile.log): Unity batch compile, zero `CS` errors,
  clean return code `0`.
- [`unity/webbuild.log`](unity/webbuild.log): successful WebGL build and
  `FRUIT_DEFENSE_WEB_BUILD_OK` with
  `template=PROJECT:FruitDefensePortraitContain` and
  `host=fruit-defense-portrait-contain-v1`.

Source/build byte identities:

| File | Source SHA-256 | Built SHA-256 | Result |
| --- | --- | --- | --- |
| `fruit-defense-host.css` | `58554ff972656670dd05f2098116939bcd6238e8e278002313653485b2b66368` | same | pass |
| `fruit-defense-host.js` | `2a85c0cf776d2de448fcc79ea1a635dfe6fecaee92beafb4d738ec2705b94318` | same | pass |

The Unity build and host checks did not manage the already-running port 4173.
All new host checks use a separately allocated random port.

# Tasks 3.5/3.6 — painted production resource polish

Status: **static resource final; Unity validation intentionally deferred to the Foundation aggregate run.**

This pass closes the resource-owned parts of H-04/H-05 and the four release-runtime key-magenta findings from task 1.4. It does not edit the result banner: B-01 is owned by the Battle terminal layout.

## Reviewed changes

| Resource | Production change | Static result |
|---|---|---|
| `indicator.drag-legal` | one built-in imagegen edit of the existing legal arrow/check/pot, then connected-background alpha extraction and deterministic fit | bbox `52×70 → 66×71`; centroid `(+2.639,+2.120) → (-0.492,+3.021)`; short edge ≥64; safe inset pass |
| `icon.control-speed` | translated the existing reviewed master and constrained the resampling edge to the declared safe box | centroid x `-5.725 → -3.909` |
| `indicator.warning` | translated the existing reviewed master upward 8 master px | centroid y `+4.749 → +3.850` |
| `indicator.error` | translated the existing reviewed master upward 8 master px | centroid y `+4.411 → +3.520` |
| `surface.safe-area` | removed its exact key-magenta alpha-1 source pixel and re-exported | runtime `(116,122)` key pixel removed |
| `indicator.loading` | exporter removes an exact key pixel after final icon resampling | runtime `(40,77)` removed |
| `icon.control-return` | same | runtime `(70,79)` removed |
| `icon.control-refresh` | same | runtime `(29,65)` removed |

All 47 runtime exports now contain zero visible exact `#FF00FF` pixels. The exact-key cleanup is applied after optional icon safe-box resampling, because Lanczos ringing was proven to recreate an alpha-1 key pixel if cleanup ran earlier. Ordinary painted low-alpha antialiasing is preserved.

The selected legal cue is still a green planting arrow with cream check entering one terracotta pot. The illegal cue remains a red circle/slash over an arrow and pot. [`resource-polish-review.png`](resource-polish-review.png) was inspected at original, 32 px, 24 px, and grayscale 24 px; the two state silhouettes remain distinguishable without relying on color. The common cream check/arrow and prohibition stroke remain visibly thicker than the 2-logical critical-stroke requirement at their acceptance draw size.

## Imagegen provenance

Only `indicator.drag-legal` required new painted proportions. The exact selected prompt, rejected proportion iterations, generated-output identity, alpha-extraction boundary, and selected/master hashes are recorded in [`imagegen-edit-record.md`](imagegen-edit-record.md). The selected raw built-in output is preserved as [`imagegen-indicator-drag-legal-selected-raw.png`](imagegen-indicator-drag-legal-selected-raw.png), SHA-256 `CA20383E49A330FD1A5A1BCD611884BA496168EF1AD74C97A52F9F73EB6DC43D`.

The processed evidence master [`indicator-drag-legal-master-preview.png`](indicator-drag-legal-master-preview.png) is byte-identical to the production source and has SHA-256 `87802785B1D95BAC02E358CEAD3CF7A9F80E8CDD8B863E262896C12ABD442DC6`. The runtime preview is review evidence only.

## Hash and identity audit

Machine-readable before/after hashes and measured alpha geometry are in [`post-fix-audit.json`](post-fix-audit.json); the protected baseline is [`pre-fix-hashes.json`](pre-fix-hashes.json).

Exactly five production masters changed:

- `icon-control-speed.png`: `E9169153… → 005B1119…`
- `indicator-drag-legal.png`: `0F569E09… → 87802785…`
- `indicator-error.png`: `2233CE77… → A3232876…`
- `indicator-warning.png`: `CF25B353… → 656624BF…`
- `surface-safe-area.png`: `07833D06… → F1A7CF4B…`

Exactly eight runtime PNGs changed:

- `icon-control-refresh.png`: `493500FB… → 93DF8523…`
- `icon-control-return.png`: `B473A7B6… → 6C3947AB…`
- `icon-control-speed.png`: `E156F7D5… → 330A3227…`
- `indicator-drag-legal.png`: `2D5CA190… → A504B653…`
- `indicator-error.png`: `AA41AA89… → AB86245C…`
- `indicator-loading.png`: `8F040F59… → E86AFDA1…`
- `indicator-warning.png`: `E8EC1B8D… → CFC08910…`
- `surface-safe-area.png`: `9862ED4A… → 91AD6C09…`

The other 39 runtime PNG hashes are unchanged. All 47 runtime PNG `.meta` hashes and GUIDs are unchanged; target GUIDs remain:

- safe area `82f763a9fcef976d97dbd5f1934a9c9d`
- loading `eaf59d86d571f66b1e8c75d86a58a802`
- return `7ed6fffab755e14d6e9bab3ec87299dc`
- refresh `769dbb3d6263bbb2c65bad050acf6cc9`
- speed `fb4c422d6646ae4aba60104a1c9d5305`
- warning `d8f02eaa7df58c0be17ba66cc6ca5002`
- error `459ce6468dfd1a7d1428715e18f2aff8`
- legal drag `45f3778aa93c2ed717f563223da0418c`

The painted ArtSet stayed byte-identical at `95F04AB82E5FE5F23E9A70CE948EB14C986014931CA549DB04A1A070271C2A62`; the release Theme stayed byte-identical at `ECD23FBAB1EE206A23203C2BF8507A9AD737E5364ED7D0F66578F5920055867B`. No scene, layout, result banner, A-set resource, or runtime code was edited.

## Deterministic export and review gallery

The manifest was regenerated from the reviewed masters (`DE7BD45D… → DC4EFE34…`). The review gallery was expanded from the nine added roles to all 47 unique exports while remaining outside release dependencies (`5AB06551… → CA83304A…`). The full gallery was inspected at original resolution:

- `Assets/UI/Art/Sources/ReferenceBoards/Review/sunny-orchard-painted-49-gallery.png`

Two consecutive exporter executions both returned 0 and changed zero tracked export bytes. Exact per-run results and final 47 PNG/manifest/ArtSet/gallery hashes are in [`export-determinism.json`](export-determinism.json).

The source tree intentionally retains unrelated historical alpha-1 key pixels in six older masters whose runtime exports are clean. Task 2.5 validates release runtime pixels for the key-magenta gate; those unrelated source-only pixels were not expanded into this finite H-04/H-05/four-runtime-pixel repair.

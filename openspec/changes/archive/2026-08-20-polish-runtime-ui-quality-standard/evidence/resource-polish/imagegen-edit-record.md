# `indicator.drag-legal` imagegen edit record

## Boundary

The current legal-drag cue failed H-04 because its `52×70` alpha box rendered only about 13 logical px wide in a 24 logical badge. The illegal cue and every other visual role were references only. Built-in `image_gen` was used to redraw the legal cue's proportions; Pillow was restricted to connected-background removal, alpha cleanup, resizing, placement, and PNG export.

All calls used the `precise-object-edit` taxonomy and repeated these invariants: keep one green planting arrow with cream check entering one terracotta pot, preserve the Sunny Orchard gouache/cartoon style and warm-brown outline, retain transparent padding, do not introduce the illegal red prohibition sign, text, backing panel, scenery, logo, watermark, or cropped edge.

## Iterations

| Output | Request delta | Decision |
|---|---|---|
| `exec-535296ac-2b2c-4b10-96c4-9375a4ab0869.png` | widen arrow, pot, and silhouette against the existing legal target and illegal family reference | rejected: still vertically dominant |
| `exec-191e9741-104c-4a80-9f01-aea32c5bae7a.png` | shorten upper curve/pot and target compact near-square proportions | rejected: alpha silhouette remained too narrow |
| `exec-24ce2584-4f3d-4f43-9f42-2509b116e940.png` | widen/squat the complete cue | rejected: short edge became height rather than width |
| `exec-aee4bc84-d01d-4f85-bf7d-3d54347d93b5.png` | target a `1.03` width/height ratio | rejected: returned to a vertically dominant silhouette |
| `exec-e5275b14-5d1f-4273-a992-d1f5ae1e559b.png` | preserve the selected wide anatomy while increasing vertical pot/arrow extent | **selected** |

Only the selected raw output is retained in the workspace: [`imagegen-indicator-drag-legal-selected-raw.png`](imagegen-indicator-drag-legal-selected-raw.png), SHA-256 `CA20383E49A330FD1A5A1BCD611884BA496168EF1AD74C97A52F9F73EB6DC43D`.

## Exact selected prompt

```text
Use case: precise-object-edit
Asset type: production game UI state-indicator master
Input images: Image 1 is the wide legal planting icon edit target.
Primary request: Preserve the current width exactly. Increase only the visible art height by about 20%: make the terracotta pot body deeper and lift the upper leaf/arrow crown slightly, while keeping the same wide arrow head and pot width. The complete artwork bounding box must have width-to-height ratio between 1.00 and 1.08. Keep the symbol centered in the square with equal clear top/bottom margins. Preserve arrow entering pot and cream check.
Constraints: preserve the exact Sunny Orchard painterly style, palette, warm-brown outline, textures, leaves, and anatomy of Image 1; genuinely transparent background; no checkerboard or white field baked into pixels; no narrowing of the current visible width; no red prohibition sign; no text; no new object; no panel; no scenery; no logo; no watermark; no cropped edge. Change only pot depth, upper vertical extent, and centering.
```

The built-in output was `1254×1254`, opaque, and contained a baked neutral checker even though true alpha was requested. The technical conversion flood-filled only bright low-saturation background connected to the outer canvas, eroded the source mask by 3 px to remove checker antialias, premultiplied RGB by alpha, fit the reviewed art to `512×544`, and placed it at `(134,112)` on the existing `768×768` master. The resulting master is [`indicator-drag-legal-master-preview.png`](indicator-drag-legal-master-preview.png), byte-identical to the production source at the time of export, SHA-256 `87802785B1D95BAC02E358CEAD3CF7A9F80E8CDD8B863E262896C12ABD442DC6`.

The final runtime cue measures `66×71` at alpha greater than zero, centroid offset `(-0.492,+3.021)` source pixels, stays inside the 12 px safe inset, and keeps a green arrow/check/pot silhouette that remains distinct from the illegal red circle/slash in grayscale.

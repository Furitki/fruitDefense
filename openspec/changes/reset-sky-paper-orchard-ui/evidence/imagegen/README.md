# ImageGen evidence and selected masters

Built-in ImageGen was used with the user attachment as a style reference only. The
reference was never an edit target and none of its text, layout, board, or gameplay
pixels were transferred.

## Reference-faithful rework style board

### `reference-faithful-battle-chrome-styleboard.png`

- Review-only evidence; it is not referenced by the release theme, ArtSet, scene,
  or runtime code.
- SHA-256:
  `3299f4c3a8e22dc93180dbf1b5a9db93bb5c9ecf71edd731c171d84caa5a7aa4`.
- Purpose: lock the visible component construction rejected by the first pass:
  cream outer rims, rounded colored faces, upper highlights, soil outlines,
  short bottom shadows, the large warm-paper page shell, raised metric capsules,
  yellow compact controls, paired phase/Wave blocks, recipe cards, dashed
  nursery slots, and the thick green bottom action.
- Generation mode: built-in ImageGen, using the user attachment as a
  style/component-composition reference.
- Prompt summary: recreate the portrait Battle chrome as a clean text-free
  casual-game UI style board; preserve the reference's bands, gutters, nesting,
  rounded clay-like volume, and orchard palette; prohibit labels, watermarks,
  flat generic rectangles, glass, neon, photorealism, and baked gameplay units.

Production assets are independent project-owned masters created by the
deterministic exporter. No crop from this board or the supplied reference enters
the release dependency graph.

## Selected project masters

### `sky-paper-screen-background-master.png`

- Production destination:
  `Assets/UI/Art/Sources/sunny-orchard-painted/surfaces/surface-screen-background.png`
- Source SHA-256:
  `5a75fc83df74ce15f6c975743564a6ff3bc8d364ee9922d19cda301b5a2039a6`
- Prompt:

  > Clear light-sky-blue painted paper texture; subtle hand-painted 2D mobile UI
  > background, soft sky-blue wash over fine matte paper grain, uniform square field,
  > no focal point or directional light; opaque, low contrast, no text, icons, fruit,
  > leaves, clouds, panels, border, vignette, watermark, checkerboard, or transparency.

### `orange-leaf-corner-master.png`

- Production destination:
  `Assets/UI/Art/Sources/sunny-orchard-painted/ornaments/ornament-screen-corner.png`
- Source SHA-256:
  `6c32743e24537f90860b1151609938f9a625d6aa3cf770d7d7ab0a1b3c633ec9`
- Prompt:

  > One tiny original orchard sticker containing exactly one small orange attached to
  > three clean green leaves; hand-painted 2D mobile UI, crisp silhouette, restrained
  > shading, soil-brown/leaf-green outline, thin warm-white sticker rim; compact and
  > mirror-safe with at least 20% transparent padding, genuinely transparent background,
  > no blossom, strawberry, extra fruit, text, panel, checkerboard, watermark, or shadow.

## Rejected attempts

- `rejected-baked-checkerboard-corner.png`: the simplification edit returned opaque RGB
  with a baked checkerboard, so it cannot enter the ArtSet.
- `rejected-baked-checkerboard-stage.png`: the generated stage candidate returned opaque
  RGB with a baked checkerboard center.
- `rejected-opaque-stage-edit.png`: the edit retained black opaque exterior and center
  pixels instead of the required transparent nine-slice opening.

The existing owned `surface.gameplay-stage` master remains authoritative until a
candidate passes the same native-alpha and nine-slice rules. Runtime files are produced
only by the deterministic painted exporter.

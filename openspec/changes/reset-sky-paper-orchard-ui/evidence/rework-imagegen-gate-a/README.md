# ImageGen action-material Gate A

This evidence is the first Battle Gate A candidate whose six action surfaces
originate from ImageGen pixels. The supplied reference image remains evidence
only and is not cropped or shipped.

- Left side of `comparison-reference-imagegen.png`: supplied reference.
- Right side: `402x874-full/01-ready.png` from the same acceptance payload used
  for active, paused, and selected-tool states.
- Runtime identity: `ui.sunny-orchard@5 / sunny-orchard-painted@4`.
- Selected ImageGen output: `exec-936d7b44-6b4a-4eaf-8dfe-321f23e20e19.png`.
- Production sheet hash:
  `7F06E07A56F943B9FD2178F45678AD5C6211C5A7A389B86155B04413F218FC56`.
- Export transform: fixed crop, exterior chroma-alpha cleanup/despill,
  transparent padding, alpha-safe resize, optical measurement, and hashing.
- Explicitly absent: procedural action rim/face/outline/highlight/shadow drawing,
  prompt regeneration during export, runtime fallback, baked text, and icons.

Automated acceptance passed, and the user explicitly approved this Battle Gate
A candidate. The user rated the first ImageGen visual output `8.5/10`, the
production solid-chroma/extracted action surfaces `7.5/10`, and the composed
page approximately `7/10`, compared with `3/10` for the superseded procedural
button implementation. The accepted payload remains unchanged. The user's
note that its green is darker and less fresh than the reference is retained as
a non-blocking direction for a separately reviewed ImageGen revision, not as a
completed adjustment.

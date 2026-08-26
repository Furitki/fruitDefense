# sunny-orchard-painted icon generation record

## Authority and method

- Generator: built-in Codex `image_gen` (`gpt-image` generation action, 2026-08-19).
- Style references only:
  - `Assets/UI/Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png`
  - `openspec/changes/unify-runtime-ui-visual-system/evidence/sunny-orchard-redo-proof/sunny-orchard-core-components-v2.png`
- Shared generation contract: one isolated, straight-on, centered orchard-cartoon UI glyph on genuine alpha. The V3 compact-control glyphs were selected as minimum flat geometry with no backing plate, highlight, gradient, shadow, texture, leaf, decoration, text, scenery, logo, watermark, or cropped edge. No geometry was drawn by script. Production action-glyph masters are deterministic neutral-white derivatives of those selected silhouettes, as recorded below.
- Export contract: each accepted generation was cleaned, cropped, proportionally scaled, and centered into its own 768×768 RGBA master. Runtime exports are independently derived 96×96 RGBA PNGs with alpha content constrained to the central 72×72 export box (12px safe inset). The 72px export box represents the approved 36px logical visual box at 2× art scale.

## Per-asset primary requests and generation outputs

| Asset | Primary request summary | Accepted generation output |
|---|---|---|
| `marker-selected` | Gold orchard medallion, cream check, exactly two base leaves | `exec-32062147-179e-4b7f-bb8e-7d29e006dde2.png` |
| `indicator-disabled` | Muted sage leaf medallion with bold cream minus | `exec-6d1de73f-f86a-435c-92fd-48cfdc4848f6.png` |
| `indicator-loading` | Eight alternating amber/green seeds, open circular progress sequence | `exec-f116267a-e7ca-4b9c-b77c-29372dacb518.png` |
| `indicator-success` | Green orchard medallion with cream check and leaves | `exec-0d65233a-2531-46f8-9d84-39017986d2f1.png` |
| `indicator-warning` | Amber rounded triangle, cream exclamation, base leaves | `exec-dc95c145-be12-405f-b3ea-5f23f7c006d2.png` |
| `indicator-error` | Fruit-red rounded triangle, cream exclamation, base leaves | `exec-f7e00638-70e7-4193-87f2-ef1fe734f557.png` |
| `indicator-drag-legal` | Green drag arrow entering terracotta pot with check | `exec-a9df89ea-938d-4862-b3eb-c1f09b43a7a3.png` |
| `indicator-drag-illegal` | Red prohibition slash over drag arrow and pot | `exec-da1cb777-4ab7-48be-8acf-9f8e2664246d.png` |
| `indicator-merge` | Two small sprouts and inward arrows converging to one larger sprout | `exec-ba8de6b8-4540-42cd-9f62-3a2de5bbb991.png` |
| `indicator-swap` | Two opposed curved arrows in a horizontal exchange loop | `exec-6f088acf-169e-40ce-98f7-31ae54407798.png` |
| `icon-resource-sun` | Golden sun with alternating rays and no face | `exec-d7c13804-57e4-4344-9e15-6b8039794dd9.png` |
| `icon-resource-core` | Compact cluster of three orchard core fruits | `exec-ea33adc5-4543-4298-99d1-6340f5a58380.png` |
| `icon-resource-wave` | Layered blue curling wave with cream foam | `exec-b14ede4b-c506-4d40-8643-69fc08b79d65.png` |
| `icon-control-pause` | Two flat brown rounded bars only | `exec-2e5b3dbb-dc58-41a6-94b8-0d7eba8ad9d6.png` |
| `icon-control-continue` | One flat brown right-pointing triangle only | `exec-c19e8f10-8869-427e-a72f-206406e73573.png` |
| `icon-control-speed` | Two flat brown right-pointing triangles only | `exec-e448bbe8-e34e-4f41-a954-f179ffe9e5ca.png` |
| `icon-control-retry` | Single clockwise retry arrow around one amber seed | `exec-8ce6c470-2c2b-4a37-81c6-73634559e378.png` |
| `icon-control-return` | Compact orchard cottage/home silhouette | `exec-ce0b2322-cee9-4831-a2c6-46c9380c7d5c.png` |
| `icon-control-close` | Two equal flat brown diagonal bars forming one X | `exec-344e3cd7-8d96-42e7-8065-7eddad406700.png` |
| `icon-tool-pot` | Empty straight-on terracotta pot with soil opening | `exec-52030fbc-8e6c-46cc-9ee5-a7e7cde665d1.png` |
| `icon-control-refresh` | Two green refresh arrows around a pair of amber seeds | `exec-946a8235-e01a-47bc-a82b-32e22954ae99.png` |

`icon-control-continue.png` is intentionally the only unique art file for the Continue, Start Wave, and Start semantic bindings. The bindings remain separate in the ArtSet contract.

## 2026-08-26 tintable action-glyph normalization

The seven unique action-glyph PNG masters were normalized in place to the shared tintable contract: every pixel with `alpha > 0` has RGB `#FFFFFF`, every `alpha == 0` pixel is transparent black, and every source alpha byte and canvas dimension is unchanged. Runtime exports apply the same white-mask rule after their existing geometry pipeline, so no soil-brown, amber, green, gradient, highlight, shadow, or interaction-state color remains baked into the glyph. Unity GUIDs and importer geometry are preserved; the runtime action content resolver now owns final color.

| Unique production master | Shared semantic bindings | Current source SHA-256 |
|---|---|---|
| `icon-control-pause.png` | `icon.control-pause` | `487E2EDAB4269934A6387A8DCFE38A1B9FCDF4326FE05FA9D75E85581183490B` |
| `icon-control-continue.png` | `icon.control-continue`, `icon.control-start-wave`, `icon.control-start` | `1D92148CA17DD05684F98C657B6BD384BF6516BD6BFC389A7D57AA4C0071A150` |
| `icon-control-speed.png` | `icon.control-speed` | `8E195A483D04BFA0F254AC01E9CEBF8CD94453A68ED3ED50F3D509E5750E8A71` |
| `icon-control-retry.png` | `icon.control-retry` | `F5F6E79A7CEC66F8A2BA965CC8712DEFBFFE4834A0D0241378EBCDECAF26AF0E` |
| `icon-control-return.png` | `icon.control-return` | `22F4D830C252E3B6179A0A17B219CCCA2290AC050FE89271D1CD5AA3D83EC24C` |
| `icon-control-close.png` | `icon.control-close` | `AC98CDB08E561D953A6CE6626782835932D75018FF6CD9547E19F155F80F55FF` |
| `icon-control-refresh.png` | `icon.control-refresh` | `F32C4F6E0D707E7CC9C0B722623ADF7BF73F58D075C3DB81AE21DBED30F0276B` |

`icon-tool-pot.png` and `icon-resource-*` are deliberately excluded because they are content/resource art with intrinsic color, not action glyphs.

## 2026-08-20 quality-standard edit — legal drag cue

- Mode: built-in `image_gen`, `precise-object-edit`.
- Edit target: the existing `indicator-drag-legal.png` master.
- Same-family scale reference: `indicator-drag-illegal.png`; it was not edited.
- Selected generated output: `exec-e5275b14-5d1f-4273-a992-d1f5ae1e559b.png`, preserved in the change evidence as `imagegen-indicator-drag-legal-selected-raw.png`, SHA-256 `CA20383E49A330FD1A5A1BCD611884BA496168EF1AD74C97A52F9F73EB6DC43D`.
- The generator again baked a neutral checker field despite the true-alpha instruction. The selected art was converted to the production master only by connected neutral-background removal, a 3 px source-edge erosion, premultiplied resampling, a `512×544` technical fit, and placement at `(134,112)` on the existing `768×768` canvas. No vector/Pillow shape was drawn.
- Final master SHA-256: `87802785B1D95BAC02E358CEAD3CF7A9F80E8CDD8B863E262896C12ABD442DC6`.

Final selected edit prompt:

```text
Use case: precise-object-edit
Asset type: production game UI state-indicator master
Input images: Image 1 is the wide legal planting icon edit target.
Primary request: Preserve the current width exactly. Increase only the visible art height by about 20%: make the terracotta pot body deeper and lift the upper leaf/arrow crown slightly, while keeping the same wide arrow head and pot width. The complete artwork bounding box must have width-to-height ratio between 1.00 and 1.08. Keep the symbol centered in the square with equal clear top/bottom margins. Preserve arrow entering pot and cream check.
Constraints: preserve the exact Sunny Orchard painterly style, palette, warm-brown outline, textures, leaves, and anatomy of Image 1; genuinely transparent background; no checkerboard or white field baked into pixels; no narrowing of the current visible width; no red prohibition sign; no text; no new object; no panel; no scenery; no logo; no watermark; no cropped edge. Change only pot depth, upper vertical extent, and centering.
```

The complete iteration record, including rejected proportion variants and the deterministic edge-cleanup boundary, is stored with the change evidence under `evidence/resource-polish/`.

# Sunny Orchard redo proof

This directory is review-only evidence for the user-requested art-quality reset. None of these images is referenced by `RuntimeUiTheme`, a `RuntimeUiArtSet`, a release scene, or `Assets/UI/Art/Runtime`.

## Authority and current status

- Visual authority: `Assets/UI/Art/Sources/ReferenceBoards/Approved/sunny-orchard-style-board.png`
- Authority SHA-256: `B9F976C8DC36761B62086A8BE63C2CF4A2FCBED683E3B62CC78526FCB14D87E1`
- OpenSpec task `3.4` remains unchecked.
- The currently runnable release set remains unchanged while this proof is reviewed.
- The rejected Citrus Mint candidate is not activated and is not treated as an approved visual direction.

## Review candidates

### `sunny-orchard-core-components-v2.png` (current review target)

- Size: 1536 x 1024
- SHA-256: `B618BA77B941300F4405DA98088ED8F1C478E3BEA48D38ADD13345E6AA6BE3E9`
- Purpose: restrained art-quality and component-family review after the first optical-alignment pass.
- Changes from v1: reduced gold/highlight/shadow weight, smaller card check and button play medallions, normalized resource-icon visual boxes, normalized selected/disabled/warning visual weight, and quieter result ribbon/stars.
- Important: this remains a review-only drawing target. It is neither runtime artwork nor exact `402 x 874` geometry evidence.

Built-in image edit prompt summary:

> Preserve the six v1 component groups and approved orchard language. Reduce gold edging, inner highlights and shadows by about 35%; shrink and re-anchor the selected check; shrink and recenter the button play medallion; normalize sun/core/wave and selected/disabled/warning icons by perceived visual box and stroke weight; quiet the result ribbon/star hierarchy. Do not add components, text, programmer-art geometry or mismatched icon canvases.

### `sunny-orchard-core-components-v1.png`

- Size: 1536 x 1024
- SHA-256: `E8721A3449F164E2A004E11CF1AB639556199192342D383BE0BCDF7B03D48734`
- Purpose: first-pass art-quality and component-family review, retained as the before state for the v2 optical correction.
- Scope: selected level card, primary action, three-resource bar, selected/disabled/warning indicators, result card, and plant detail card.
- Important: this is a visual-direction sheet, not a runtime slice sheet and not proof of exact `402 x 874` geometry.

Built-in image generation prompt summary:

> Match the approved Sunny Orchard reference with hand-painted orchard illustration, warm parchment, soil-brown outlines, leaf-green actions, sunlight-amber selection, shallow carved depth, consistent icon canvases and optical centering. Produce the six required component groups. Avoid flat geometric SVG/programmer art, thin hairlines, generic rounded rectangles, raw primitive icons, inconsistent icon scale, glossy mobile-game chrome, and text baked into artwork.

### `sunny-orchard-route-concept-v1.png`

- Size: 1672 x 941
- SHA-256: `DA4933916CA88FF1035C24D89A0A36D8D6E03A5F86A66DBFBAC5FB20E408DF1E`
- Purpose: cross-route art-direction comparison for Lobby, Battle, and Settlement.
- Important: the composition successfully demonstrates the approved orchard material and illustration hierarchy, but it enlarges several Lobby and Settlement regions. It is concept evidence only and cannot be used as geometry or replacement proof.

Built-in image generation prompt summary:

> Use the approved board as the sole visual-style authority and the existing Lobby, Battle, and Settlement captures as information references. Render a coherent hand-painted orchard triptych with consistent illustrated icons, shallow surfaces, leaf anchors, amber selection and quiet cream metrics. Preserve functional density and avoid generic script-drawn chrome. Do not treat generated text or altered component proportions as production authority.

## Rejected generation

`rejected/lobby-layout-drift-v1.png` attempted a strict in-place Lobby restyle. It is rejected because the generator changed the three locked `370 x 82` card heights, Start-button proportions, typography scale, and content positions despite explicit invariants. It must not be used as a runtime-layout reference.

## Production gate after visual approval

Approval of the component sheet authorizes a separate production pass. That pass must:

1. keep current draw/hit rectangles and semantic slots;
2. author independent `base 9-slice`, `border-shadow`, `ornament`, `content-icon`, and `state-marker` layers;
3. keep leaves, thumbnails, banners, and orchard illustrations outside nine-slice stretch centers;
4. manually align every icon by optical mass, not by transparent-canvas bounds;
5. review real 360/375/402/430 full and inset WebGL captures before activation;
6. leave OpenSpec task `3.4` unchecked until the user approves the real in-game replacement.

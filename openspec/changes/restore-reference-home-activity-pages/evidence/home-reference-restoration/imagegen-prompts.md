# Home 参考还原 ImageGen 固定提示词

所有调用均为内置 ImageGen、单组件编辑，并以 `reference-splits/` 下对应裁剪作为唯一图像输入。生成原件保留在 Codex 默认生成目录，项目内副本保存在同级 `imagegen/`；原件未删除。

## `icon.hub-home`

输出：`exec-568a67c0-0394-40ac-86d1-aadfad4f62e4.png`

> Precise object extraction for a production game UI icon. Extract only the brown house symbol from the reference crop. Preserve the reference silhouette exactly: one simple filled house, softly rounded roof corners, one centered doorway negative space. Remove all Chinese text, paper background, blue strip, leaves, shadows outside the symbol, and every unrelated pixel. Do not redesign, add detail, add gradients, add texture, add outlines, add scenery, add props, or add a second object. Center the isolated symbol on a square canvas with generous transparent padding. True transparent background, not checkerboard. No text, no letters, no numbers, no border, no shadow. Crisp and readable when rendered at 33 logical pixels.

## `icon.hub-activity`

输出：`exec-1b5f523c-f2f4-40ef-9d57-c773dfe734f9.png`

> Precise object extraction for a production game UI icon. Extract only the brown calendar-with-star symbol from the reference crop. Preserve the reference silhouette: one simple rounded calendar outline, two short top binder tabs, one centered five-point star. Remove all Chinese text, paper background, blue strip, shadows outside the symbol, and every unrelated pixel. Do not redesign, add detail, add gradients, add painterly texture, add scenery, add props, or add a second object. Center the isolated symbol on a square canvas with generous transparent padding. True transparent background, not checkerboard. No text, no letters, no numbers, no border around the canvas, no shadow. Crisp and readable when rendered at 33 logical pixels.

## `icon.hub-growth`

输出：`exec-0dc1e404-1f7d-4635-aa4e-4a322b8d8ac9.png`

> Precise object extraction for a production game UI icon. Extract only the brown two-leaf sprout symbol and its minimal flat oval base from the reference crop. Preserve the reference silhouette exactly: one connected sprout, two leaves, one tiny flat base. Remove all Chinese text, paper background, blue strip, shadows outside the symbol, and every unrelated pixel. Do not redesign, add detail, add gradients, add painterly texture, add soil clumps, add scenery, add fruit, add props, or add a second object. Center the isolated symbol on a square canvas with generous transparent padding. True transparent background, not checkerboard. No text, no letters, no numbers, no border around the canvas, no shadow. Crisp and readable when rendered at 33 logical pixels.

## `surface.hub-navigation-base`

输出：`exec-f1577221-c06a-41ab-83e4-5e7201f27f9c.png`

> Precise component edit for a production game UI raster. Isolate the continuous full-width warm paper bottom-navigation tray from the reference crop. Preserve its reference silhouette and material: one connected low tray, gently arched top edge, softly rounded outer corners, pale warm cream paper, subtle vertical stitched separators, thin warm tan edge, shallow lower paper thickness and shadow. Remove the selected yellow tab, all three icons, all Chinese labels, the underline, leaves, flowers, apples, blue background, and every outside ornament. Fill the removed icon and label areas with the same uninterrupted reference paper material while preserving the two subtle separators. Output only this single tray component on a true transparent background. No text, no icons, no fruit, no leaves, no extra panels, no black outline, no thick border, no strong texture, no checkerboard. Wide canvas matching the source tray aspect ratio; crisp at 402 by 80 display pixels.

## `surface.hub-navigation-selected-tab`

输出：`exec-669b662f-2ed8-4bf0-955b-d1930a73a662.png`

> Precise component edit for a production game UI raster. Isolate only the selected yellow paper tab silhouette from the reference crop. Preserve its exact reference anatomy: one softly rounded raised paper tab, warm pale sunlight-yellow face, thin warm-gold edge, restrained inner stitched highlight, short shallow lower shadow, slightly tapered sides. Remove the house icon, Chinese label, yellow underline, leaves, flower fragments, blue background, neighboring tray, and every unrelated pixel. Fill removed content with matching uninterrupted yellow paper material. Output one text-free and icon-free tab on a true transparent background. No text, no icons, no ornaments, no black outline, no thick border, no checkerboard. Keep generous transparent padding and a vertical tab canvas suitable for 134 by 90 display pixels.

## `surface.card-selectable`

输出：`exec-02d2c426-49fd-4b02-97d3-8b88c5970a6d.png`

> Precise component edit for a production game UI nine-slice master. From the reference crop, keep only the selected level-card paper surface and its reference material. Remove the landscape illustration, all Chinese text, all stars, the checkmark ribbon, and every content pixel; fill those removed areas with the same uninterrupted pale sunlight-cream paper face. Preserve the card's softly rounded rectangular silhouette, very thin warm yellow outer edge, restrained inner stitched highlight, subtle paper texture, and shallow lower shadow. The center and all broad edge stretches must remain plain and seamless. Output exactly one empty text-free selected-card surface on a true transparent background. No icons, no labels, no illustration, no ribbon, no stars, no black or near-black outline, no thick frame, no extra panels, no checkerboard. Use a square canvas with generous transparent padding and protect the corner anatomy for nine-slice use.

## `icon.hub-activity`（最终简化版）

输出：`exec-1939b089-7cd9-44b3-9f91-c6a3671c9af1.png`

> Create one production-ready bottom-navigation icon derived from the attached reference calendar/star symbol. Output a single isolated icon only, centered on a genuinely transparent background. Preserve the reference identity: compact rounded calendar silhouette with one large centered five-point star and exactly two short top binder tabs. Make it radically simpler and more readable at 24 px: use broad SOLID warm soil-brown filled shapes, thick forms, very large negative spaces, no thin outline-only calendar, no micro-detail, no texture, no shadow, no glow, no text, no label, no panel, no black stroke, no checkerboard. The whole icon should be one compact dominant silhouette; binder tabs may remain visually attached or very close. Square canvas, generous transparent padding, crisp hard alpha edge. Match the gentle handmade rounded character of the reference while minimizing perimeter complexity.

该版仍为轮廓式日历，24px 门禁未通过，已被下方最终版替代。

## `icon.hub-activity`（最终实心剪影版）

输出：`exec-46dbc74e-49ff-4a76-a853-7c0b646225fa.png`

> Edit the attached navigation icon itself. Keep its compact rounded calendar identity and warm soil-brown color, but simplify the alpha silhouette much further for perfect readability at 24 px. Make ONE connected solid brown calendar body: fill the entire calendar interior brown instead of using an outline-only frame. Carve exactly one very large centered five-point STAR-SHAPED TRANSPARENT CUTOUT through the solid body. Merge both top binder tabs physically into the calendar body with thick short necks, so there are no detached components. Use completely flat near-solid color with only imperceptible tonal variation, no internal frame opening, no separate star object, no outline-only construction, no thin gaps, no small holes, no texture, no shadow, no glow, no black stroke, no text, no label, no panel, no checkerboard. Genuinely transparent background outside and in the single star cutout. One isolated centered icon, generous transparent padding, crisp hard alpha, very low perimeter complexity.

## `icon.hub-growth`（最终简化版）

输出：`exec-68d23152-a65e-4b57-9a35-10533143e573.png`

> Create one production-ready bottom-navigation icon derived from the attached reference sprout/growth symbol. Output a single isolated icon only, centered on a genuinely transparent background. Preserve the reference identity: one short upright stem growing from one compact oval soil base, with exactly two broad leaves, one left and one right. Make it radically simpler and more readable at 24 px: broad SOLID warm soil-brown filled silhouette, thicker stem, fuller leaves, compact proportions, minimal perimeter, no internal veins, no thin outline-only shapes, no micro-detail, no texture, no shadow, no glow, no text, no label, no panel, no black stroke, no checkerboard. Keep all pieces visibly connected as one dominant silhouette. Square canvas, generous transparent padding, crisp hard alpha edge. Match the gentle handmade rounded character of the reference while minimizing complexity.

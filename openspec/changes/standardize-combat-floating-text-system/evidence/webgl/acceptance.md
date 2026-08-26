# Combat Floating Text WebGL Acceptance

- Date: 2026-08-26 (Asia/Shanghai)
- Baseline: ordinary WebGL only; this is not Douyin or WeChat conversion evidence
- Viewport: 402 × 874 CSS pixels
- Route: `?acceptance=1&route=battle&levelId=orchard-01&safeTop=0&safeBottom=0`
- Build marker: `FRUIT_DEFENSE_WEB_BUILD_OK`
- Payload: `data=7252e9cc8305`, `framework=20f3bbafce4f`, `loader=4101608c44ba`, `wasm=7526068fdfd3`
- Total build size: 10,287,266 bytes

## Interaction scenario

1. Entered the deterministic battle acceptance route.
2. Refreshed the five-fruit bench and dragged the one-star durian into the top-left route cell.
3. Started wave 1 at 1× and captured a continuous sequence.
4. Reloaded the same build, repeated the setup at 2×, and captured a continuous sequence.

## Accepted result

- Coral heavy-damage copy remains distinct from the green grass, brown route, enemies, and range overlay.
- One area-impact profile admits no more than three same-tick labels; the labels occupy three deterministic inward lanes at the upper edge.
- Heavy entry and rebound are visible without making the labels teleport or changing the gameplay position.
- 2× preserves a readable real-time lifetime instead of halving the entire animation.
- Fatal damage does not emit a duplicate numeric label.
- Same-tick terminal results collapse to `击败×N`; both Chinese glyphs and the count render in the final WebGL raster.
- Terminal copy uses a separate inward display band and does not overlap the three damage lanes.
- The packaged Noto Sans SC font and Unity glyph texture remain the renderer. A finite glyph inventory is warmed at initialization. No project-authored bitmap/SpriteAtlas was generated because no accepted WebGL evidence crossed the documented renderer-time or steady-allocation gate.

## Key evidence

- `accepted-defeat-1x-15.png`: 1× heavy damage plus separated `击败`
- `accepted-defeat-1x-23.png`: 1× same-tick `击败×2`
- `accepted-defeat-2x-05.png`: 2× three-lane heavy damage plus separated `击败`
- `accepted-defeat-2x-09.png`: 2× same-tick `击败×2`

The full `accepted-defeat-1x-*` and `accepted-defeat-2x-*` sequences retain the rhythm evidence before, during, and after these selected frames.

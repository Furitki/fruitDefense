# Cell-Aligned Square Terrain Prompts v2

## Approved in-game grid preview

- Generator: OpenAI built-in `imagegen`.
- Edit target: `codex-clipboard-3a487a70-46f5-4115-8bfb-186c939ec668.png`.
- Accepted output: `approved-in-game-grid-reference-v2.png`.
- User approval: use this exact game-grid version as the source for tile splitting.

```text
Use case: precise-object-edit
Asset type: in-game square-grid terrain previsualization for a portrait casual tower-defense game
Input images: Image 1 is the edit target and the authoritative game screenshot.
Primary request: Change only the terrain artwork inside the dark-brown battlefield rectangle. Replace it with a polished square-cell map preview using harmonious hand-painted grass and soil square tiles. This must look like the terrain is already running inside the actual game, not like a standalone concept image or a tileset sheet.
Grid geometry: preserve the existing battlefield bounds and straight top-down view. Make an exact 8-column by 7-row square grid with equal-size square cells. The playable grass region is exactly 7 columns by 5 rows, touching the left battlefield edge as in the source screenshot. One full row of soil cells is above it, one full column of soil cells is to its right, and one full row of soil cells is below it. Every grass and soil tile uses the same square cell size. Grass-to-soil boundaries align exactly to cell edges. Grid lines are thin, subtle, and consistently visible across both grass and soil.
Style/medium: fresh, cheerful, clean 2D casual mobile-game art; softly hand-painted gouache/cartoon finish; broad simple color shapes; restrained texture; designed to read clearly at the small on-screen scale.
Color palette: harmonize with the screenshot's cream, yellow, orange, and green UI. Use warm soft yellow-green grass and medium-light caramel/cocoa soil. Grass and soil should have compatible saturation and contrast; no neon lime and no dark muddy red-brown.
Tile appearance: each square tile has one or two large, very soft painted tonal patches only. Slight natural variation between neighboring cells, but no obvious repeated stamp. Keep the centers uncluttered. A restrained soft grassy fringe may appear along grass-to-soil cell edges, but it must remain inside the square-cell boundary and must not obscure the grid structure.
Preserve exactly: the 1280x720 canvas, blue background, cream game panels and borders, all Chinese UI text, counters, buttons, icons, plants, characters, selection markers, lower deck UI, layout, scale, camera framing, and every pixel outside the battlefield terrain rectangle. Do not redesign or redraw the interface.
Constraints: change only the battlefield terrain; keep the grid mathematically regular; no perspective; no diagonal cells; no isometric view; no rounded floating island; no large continuous texture painted across multiple cells; no new props; no extra plants; no text changes; no logo; no watermark.
Avoid: standalone terrain poster, borderless lawn, hidden grid, dual-grid transition pieces, microtexture, fine noise, speckles, photorealism, 3D bevels, thick outlines, glossy plastic, checkerboard repetition, mismatched tile sizes.
```

## Deterministic split contract

- Grass source rectangle: `x=692, y=292, width=48, height=48` in the accepted `1672 × 941` output.
- Soil source rectangle: `x=788, y=196, width=48, height=48` in the accepted `1672 × 941` output.
- Each crop is normalized to `64 × 64` with FFmpeg Lanczos scaling.
- No recoloring, repainting, alpha extraction, edge synthesis, or additional generative pass is allowed.
- The crop owns one complete soft inset frame so one regular grid rhythm appears per repeated gameplay cell.

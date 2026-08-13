# Layered terrain AI edge provenance

Reference image: `codex-clipboard-19be1f07-7664-4e32-8c76-8cdb571ee6b1.png`

Evidence directory: `Builds/Evidence/layered-terrain/`

The AI pass owns the painted contact ribbon. The canonical 16-slot Dual-Grid alpha sockets own
cross-tile topology. Chroma removal, atlas cropping, downsampling and canonical alpha application
only package the generated artwork; they do not synthesize, blur or procedurally beautify an edge.
The outermost matching RGBA socket samples are locked after the AI pass so every compatible pair
is byte-identical at a tile boundary. The protected socket samples are topology constraints, while
the interior contact-ribbon appearance remains the AI result.

## Grass on soil

```text
Use case: precise game-asset edit.
Input images: Image 1 is the edit target and immutable 4x4 topology guide for masks 00 through 15 in row-major order. Image 2 is style reference only.
Primary request: refine only the visible contact ribbon where bright green grass meets warm sandy soil in Image 1, matching Image 2's cheerful hand-painted fantasy overworld style. Replace the chunky pixel border with a smooth, organic, painterly grass fringe and thin warm-earth contact accent. Keep the green material inside each topology silhouette.
Composition/framing: exact 1024x1024 square; preserve the 4x4 grid, every cell boundary, row-major mask order, and the exact occupied/empty corner connectivity of every tile.
Protected invariants: mask 00 stays completely empty; mask 15 stays completely filled; masks 05 and 10 keep their two disconnected diagonal components; each corner socket and each crossing at a tile-cell border must remain in exactly the same position as Image 1; no topology changes; no bridges across magenta regions; no extra islands; no labels; no numbers; no grid lines.
Scene/backdrop: perfectly flat solid #ff00ff in all outside regions, with no shadows, gradients, texture, reflections, floor plane, or lighting variation; never use #ff00ff inside the grass or soil artwork.
Output intent: a crop-ready 4x4 atlas for a Unity dual-grid terrain TileSet, with crisp antialiased silhouettes, consistent edge width, seamless socket continuity, no text, no logos, no watermark.
```

Raw result: `Builds/Evidence/layered-terrain/grass-on-soil-ai-atlas.png`

## Soil on grass

```text
Use case: precise game-asset edit.
Input images: Image 1 is the edit target and immutable 4x4 topology guide for masks 00 through 15 in row-major order. Image 2 is style reference only.
Primary request: refine only the visible contact ribbon where warm sandy soil landform meets bright green grass below it in Image 1, matching Image 2's cheerful hand-painted fantasy overworld style. Keep the warm sandy material inside each topology silhouette. Add a restrained soft grass-pressure fringe and small earthen irregularities along the contact, clearly reading as SANDY SOIL ON TOP OF GRASS, the reverse ordered pair of grass on soil.
Composition/framing: exact square atlas; preserve the 4x4 grid, every cell boundary, row-major mask order, and exact occupied/empty corner connectivity.
Protected invariants: mask 00 stays completely empty; mask 15 stays completely filled; masks 05 and 10 keep their two disconnected diagonal components; each corner socket and each crossing at a tile-cell border remains in exactly the same position as Image 1; no topology changes; no bridges across magenta; no extra islands; no labels; no numbers; no grid lines.
Scene/backdrop: perfectly flat solid #ff00ff in all outside regions, with no shadows, gradients, texture, reflections, floor plane, or lighting variation; never use #ff00ff inside the soil or edge artwork.
Output intent: crop-ready 4x4 atlas for a Unity dual-grid terrain TileSet, crisp antialiased silhouettes, consistent edge width, seamless socket continuity, no text, no logos, no watermark.
```

Raw result: `Builds/Evidence/layered-terrain/soil-on-grass-ai-atlas.png`

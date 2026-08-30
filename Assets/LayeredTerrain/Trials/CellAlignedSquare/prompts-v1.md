# Cell-Aligned Square Terrain Prompts v1

## Shared intent

- Use case: stylized Unity 2D top-down tileable game texture.
- Style reference: `Assets/LayeredTerrain/GrassSoil/Square/Sources/ApprovedStyleReference.png` is used only for palette, brushwork, and cleanliness.
- Runtime constraint: one full texture repeat is mapped to every 46-pixel gameplay cell.
- Visual target: friendly clean cartoon hand-painting, close to flat color, with only very broad and low-contrast variation.
- Production constraints: opaque, seamless on both axes, no border, no focal mark, no objects, no text, and no watermark.

## GrassSquareBase-v1

Generate a new light spring-green grass texture harmonized with the approved reference. Fill the complete orthographic square with grass. Use a broad matte gouache wash and at most one or two canvas-scale, extremely soft tonal drifts with no recognizable shape. Keep local color deviation around three percent so the image reads as a calm flat color at 46x46 pixels. Remove grass blades, flowers, dirt, speckles, grain, camouflage blobs, high-frequency mottling, directional lighting, edge darkening, and perspective. Opposite edges must continue naturally in both axes.

## SoilSquareBase-v1

Generate a new light honey-ochre soil texture harmonized with the approved reference and the grass texture. Fill the complete orthographic square with soil. Use a broad matte gouache wash and at most one or two canvas-scale, extremely soft tonal drifts with no recognizable shape. Keep local color deviation around three percent so the image reads as a calm flat color at 46x46 pixels. Remove stones, pebbles, cracks, roots, grass, speckles, grain, brush-shaped blobs, high-frequency mottling, directional lighting, edge darkening, and perspective. Opposite edges must continue naturally in both axes.

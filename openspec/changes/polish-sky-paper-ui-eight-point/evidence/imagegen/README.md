# Historical ImageGen material-sheet evidence

Built-in ImageGen was used. The supplied UI image was a style/composition
reference, the approved 7/10 Battle frame was a comparison reference, and the
previous action sheet was an anatomy reference. No supplied-reference pixels
are cropped or shipped.

These sheets are superseded production evidence. The current primary/secondary
surface is the separately reviewed light-green
[`action-green.png`](../direct-replacement-v2/imagegen/action-green.png), whose
approved visual parameters are P0 authority. Prompt steps below that darkened a
container for universal warm-white `4.5:1` contrast record the rejected former
contract and MUST NOT be replayed. Current contrast correction changes the
separate text/icon content token first and leaves that raster unchanged.

## Selected action sheet

- Base generated output: `exec-4f60d853-d710-471a-a549-8c953c5b5a8a.png`.
- Contrast-corrected active-control output:
  `exec-9972394c-2d59-4fd2-98dc-57d033bceed6.png`.
- Production evidence file:
  [imagegen-action-material-sheet-selected.png](imagegen-action-material-sheet-selected.png).
- SHA-256: `636D372D867A75BF1B5A0E3E31B2BB2E3CF35A9E8692F73A01327DF202BC9E36`.
- Size/background: `1024×1536` RGB on opaque generated `#FF00FF` chroma.
- Fixed cell map:

| Semantic slot | Crop `[x0,y0,x1,y1]` |
| --- | --- |
| `action.compact-control` | `[0,80,496,528]` |
| `action.compact-control-active` | `[512,80,992,528]` |
| `action.primary` | `[0,528,496,1024]` |
| `action.secondary` | `[512,528,992,1024]` |
| `action.quiet` | `[0,1024,496,1488]` |
| `action.danger` | `[512,1024,992,1488]` |

Historical prompt chain:

1. `Generate a new text-free and icon-free 2×3 action-surface material sheet;
   preserve the accepted cream rim, soil outline, highlight, rounded silhouette,
   painted texture, and short shadow; make both green buttons visibly fresher,
   lighter, and more yellow-leaf than the approved dark baseline; use the fixed
   role order yellow, orange-yellow, primary green, secondary green, cream,
   danger red; isolate every component with generous gutters and no text,
   icons, symbols, decoration, or watermark.`
2. `Change only the baked checkerboard/white exterior to one perfectly uniform
   opaque #FF00FF background; preserve all six components, positions, colors,
   rims, outlines, highlights, and shadows.`
3. `Darken only the central faces of the two green buttons and danger red so
   warm-white content can pass 4.5:1; keep the fresh impression in the narrow
   lime top planes and highlights; preserve all other pixels and #FF00FF.`
4. `Change only the middle-left primary central face by a small amount to a
   fresh medium leaf green near #397509; preserve the vivid lime top plane,
   every other component, anatomy, and uniform #FF00FF background.`
5. `From that selected output, change only the top-right active-control central
   face to a lighter sunny golden yellow-orange near #F8B800 so dark brown
   #56341F content stays above 4.8:1; preserve its exact material anatomy and
   uniform #FF00FF background.`

The production plate is a deterministic crop-only composition. Five cells are
retained from the base output and `[512,80,992,528]` is copied from the
contrast-corrected output. No script paints or recolors any button pixel. The
superseded dark correction `exec-ecd73996-d8f1-4d71-9330-d6c130da7ae6.png`
remains review evidence only because it reduced the measured contrast.

Measured source-center warm-white contrast after the final ImageGen correction:
primary `5.5671:1`, secondary `6.3277:1`, danger `5.6285:1`; the corrected
active control measures `5.9965:1` against deep-brown content. The exporter
performs no color correction.

## Selected structural sheet

- Base generated output: `exec-2c26a7da-cbf2-43dc-b962-5cdf8b9b8478.png`.
- Nine-slice-corrected stage output:
  `exec-868e1b66-45d8-4a2a-bfdb-32c5f639c9ca.png`.
- Production evidence file:
  [imagegen-structural-material-sheet-chroma.png](imagegen-structural-material-sheet-chroma.png).
- SHA-256: `EEFF38C2C504FB81C40DB4C19EC1ABC01266FE519A92C6AF18E77837D755A5BF`.
- Size/background: `1024×1536` RGB on opaque generated `#FF00FF` chroma;
  the gameplay-stage opening is also key magenta and becomes transparent.
- Fixed cell map:

| Semantic slot | Crop `[x0,y0,x1,y1]` |
| --- | --- |
| `surface.safe-area` | `[32,16,496,464]` |
| `surface.panel-standard` | `[528,16,992,464]` |
| `surface.panel-raised` | `[32,472,496,808]` |
| `surface.metric` | `[512,480,992,808]` |
| `surface.card-selectable` | `[32,824,496,1168]` |
| `slot.tool` | `[512,824,992,1168]` |
| `slot.nursery` | `[32,1184,496,1528]` |
| `surface.gameplay-stage` | `[512,1168,992,1528]` |

Final prompt chain:

1. `Generate exactly eight isolated, text-free, icon-free hand-painted orchard
   UI structural surfaces in a 2×4 grid: warm-paper page shell, standard panel,
   raised panel, metric capsule, pale fresh-leaf selectable card, recipe/tool
   card, dashed nursery slot, and transparent-center cream-rimmed soil stage
   frame; match the new action family's rim, outline, highlight, texture, and
   shallow shadow; use genuine transparent gutters and no extra objects.`
2. `Change only the baked checkerboard exterior and the stage opening to one
   perfectly uniform opaque #FF00FF background; preserve all eight components,
   grid positions, colors, paper texture, rims, outlines, dashed rail, and
   shadows.`
3. `From that selected output, change only the bottom-right gameplay-stage
   frame to substantially thinner inner rails so all visible frame pixels fit
   inside the fixed 20px nine-slice border; preserve its outer silhouette,
   cream rim, soil rail, rounded corners, shallow shadow, position, and uniform
   #FF00FF opening.`

The production structural plate is another deterministic crop-only
composition. Seven cells remain from the base output and
`[512,1168,992,1528]` is copied from the corrected-stage output. No script
paints or recolors any structural pixel.

`exec-c669115a-f382-442c-b417-6ddaa88a204d.png` is retained only as the
rejected top-rail review attempt. It strengthened the top edge but weakened the
stage's side and bottom soil rails, so none of its pixels enter the selected
plate or production export.

## Production boundary

The selected sheets are retained evidence and fixed pixel sources. Extraction
may crop the listed cells, remove exterior chroma, despill the alpha fringe, add
transparent padding, alpha-safe resize, measure, hash, and export. It may not
paint, recolor, repair, or synthesize any material layer. Runtime binds only the
normalized source/runtime PNGs, never these sheets.

The initial checkerboard outputs and contrast-failing action corrections remain
review evidence only. They are not alternate production sources or fallbacks.

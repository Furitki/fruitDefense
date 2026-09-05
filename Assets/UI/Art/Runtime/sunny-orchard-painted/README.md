# Sunny Orchard Painted runtime exports

This directory contains 60 unique standalone Sprite Single PNG exports used by the complete 62-slot `sunny-orchard-painted@19` art set. `icon-control-continue.png` intentionally serves Continue, Start Wave, and Start; `surfaces/surface-gameplay-stage.png` is the standalone transparent-center heavy stage frame.

- `surfaces/`: screen/scrim, surface/action/slot exports, section ribbon, and illustration frame;
- `icons/`: 10 state exports, 14 common icons, and three 18 px final-raster resource icons;
- `ornaments/` and `illustrations/`: fixed-aspect composition layers, three Lobby route thumbnails, the Hub Activity reward illustration, and the `402x874` shell depth layer;
- importer contract: Single, Full Rect, sRGB, alpha, Bilinear, Clamp, no mipmaps, read/write off, uncompressed, and explicit per-tier PPU;
- runtime geometry: 128 px with 32 px border for nine-slice assets, 96 px with 12 px safe inset for common icons, 18 px with a 1 px edge for micro icons, a `402x874` portrait shell layer, 256 px opaque screen background, and 32 px neutral scrim.
- every nine-slice export owns distinct cream rim, face, soil outline, upper highlight, and short bottom-shadow ink; the selected action and structural exports come from hash-locked individual ImageGen masters, while remaining low-exposure variants retain their semantic construction inside protected borders.
- the seven unique `icon-control-*` exports are strict white alpha masks whose color is supplied by the runtime action content token; tool and resource icons retain intrinsic color.
- `action-primary.png` preserves the original rounded-square fixed raster and `action-secondary.png` preserves its separate approved light-green raster; both own their container pixels and remain at least `4.5:1` against their recorded dark content references. `action-danger.png` remains at least `4.5:1` against inverse warm-white `#FFF9EE`. Runtime code must not multiply another container tint over them.

Do not hand-edit these PNGs or their `.meta` files. Edit the reviewed source masters and run `Assets/UI/Art/Sources/sunny-orchard-painted/export_sunny_orchard_painted.py`.

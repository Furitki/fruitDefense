# Sunny Orchard Painted runtime exports

This directory contains 51 standalone Sprite Single PNG exports used by the complete 53-slot `sunny-orchard-painted@1` art set. `icon-control-continue.png` intentionally serves Continue, Start Wave, and Start.

- `surfaces/`: screen/scrim, surface/action/slot exports, section ribbon, and illustration frame;
- `icons/`: 10 state exports, 11 common icons, and three 18 px final-raster resource icons;
- `ornaments/` and `illustrations/`: fixed-aspect composition layers, three Lobby route thumbnails, and the `402x874` shell depth layer;
- importer contract: Single, Full Rect, sRGB, alpha, Bilinear, Clamp, no mipmaps, read/write off, uncompressed, and explicit per-tier PPU;
- runtime geometry: 128 px with 32 px border for nine-slice assets, 96 px with 12 px safe inset for common icons, 18 px with a 1 px edge for micro icons, a `402x874` portrait shell layer, 256 px opaque screen background, and 32 px neutral scrim.

Do not hand-edit these PNGs or their `.meta` files. Edit the reviewed source masters and run `Assets/UI/Art/Sources/sunny-orchard-painted/export_sunny_orchard_painted.py`.

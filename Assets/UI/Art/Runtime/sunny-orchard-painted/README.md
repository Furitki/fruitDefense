# Sunny Orchard Painted runtime exports

This directory contains 47 standalone Sprite Single PNG exports used by the complete 49-slot `sunny-orchard-painted@1` art set. `icon-control-continue.png` intentionally serves Continue, Start Wave, and Start.

- `surfaces/`: screen/scrim, surface/action/slot exports, section ribbon, and illustration frame;
- `icons/`: 10 state exports plus 11 common icons;
- `ornaments/` and `illustrations/`: fixed-aspect composition layers and three Lobby route thumbnails;
- importer contract: Single, Full Rect, sRGB, alpha, Bilinear, Clamp, no mipmaps, read/write off, uncompressed, PPU 100;
- runtime geometry: 128 px with 32 px border for nine-slice assets, 96 px with 12 px safe inset for icons, 256 px opaque screen background, and 32 px neutral scrim.

Do not hand-edit these PNGs or their `.meta` files. Edit the reviewed source masters and run `Assets/UI/Art/Sources/sunny-orchard-painted/export_sunny_orchard_painted.py`.

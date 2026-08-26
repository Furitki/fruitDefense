# `sunny-orchard` production exports

Stable set ID: `sunny-orchard`  
Visual direction: approved A「阳光果园」

This directory contains 41 independently produced semantic PNG exports for 43 locally owned roles. The complete 56-slot ArtSet adds 13 explicit shared painted composition and target-tier bindings recorded by its manifest. The three play semantics intentionally share `icon-control-continue.png`; `surface-gameplay-stage.png` is independently owned and no other original slot shares a production Sprite.

All exports are standalone Sprite Single assets using sRGB, straight alpha, Full Rect, Bilinear, Clamp, no mipmaps, disabled Read/Write, and explicit uncompressed import. Nine-slice assets carry matching 32 px importer/ArtSet borders; icons carry a uniform 12 px safe inset. There is no atlas, Multiple sprite, style-board crop, placeholder, fallback, or screen-specific asset.

The seven unique `icon-control-*` exports are strict white alpha masks whose color is supplied by the runtime action content token. `icon-tool-pot` and resource icons retain their intrinsic colors and are not action masks.

`action-primary.png` and `action-danger.png` own the final `#436C15` and `#9F302B` semantic container fields. Runtime code must not multiply a second container tint over these colored nine-slice exports.

Each export must have the same semantic basename as its owned master under `Assets/UI/Art/Sources/sunny-orchard/`. Replace exports in place and preserve their `.meta` GUIDs so an art iteration updates every consumer without scene, code, layout, or presenter edits.

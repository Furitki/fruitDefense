# Reference APK resource status

## Source identity

- Supplied file: `F:/download/yydwlxq_gwp_528.apk`
- SHA-256: `113ccaa5ea377991b7429921f446142946c10f4430008103a1f60f57d9890db6`
- Package/version observed by static inspection: `com.yydwlxqcy02.cs`, `3.0.2` (`versionCode 61`)
- Unity resources: 27,122 UnityFS bundles, Unity 2022.3 family

## Static inventory relevant to UI learning

- 826 Android UI Prefab bundles
- 474 UI texture directory bundles, approximately 130.95 MiB
- 3,625 icon bundles, approximately 79.16 MiB
- 2,119 UI effect Prefab bundles
- 25 font bundles and 433 Spine Prefab bundles
- Priority route families: `mainui_new_canvas`, `battle_main_panel`, `battle_prepare`, mission/PVP result, `shop_window`, common reward/tips/slot, guide, and rank

Paths and page/dependency groupings based only on filenames remain inference until payload decoding succeeds.

## Extraction status

The UnityFS container header, block directory, and node directory are readable for every bundle. Each payload contains one protected LZMA block. A small sample proves a changing transform/key stream after the first 32 compressed bytes; fixed XOR and simple periodic variants do not produce a complete SerializedFile. No UI Prefab, Sprite, texture, Font asset, or Spine object has therefore passed complete decode and visual verification.

## Production decision for this change

- Reference-derived raster assets admitted to production: **none**.
- Active ArtSet during the interaction-polish implementation: existing validated Sunny Orchard production set.
- Reason: no candidate currently satisfies complete decode, provenance, visual inspection, import, optical, nine-slice, and WebGL requirements.
- The implementation applies transferable interaction timing, hierarchy, cancellation, and local emphasis patterns without copying the reference APK's Lua/C# ownership model.

## Admission record required for a later candidate

Before any decoded candidate is copied into the editable source hierarchy, append a row containing:

| Source bundle/path | Decoding tool/version | Output hash | Format/dimensions | Alpha/color space | Intended semantic slot | Import/slicing settings | Authorization evidence | Provisional replacement owner | Validation evidence |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

The runtime export must retain the existing semantic slot and stable project path so later owned art replaces it in place without route code, layout changes, compatibility branches, or fallback resources.

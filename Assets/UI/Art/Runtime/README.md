# Production UI art exports

Only optimized textures and sprites eligible to fill validated `RuntimeUiArtSet` semantic slots belong here. Each folder name is the stable art-set ID and must map one-to-one to an owned source folder.

Production paths follow `Assets/UI/Art/Runtime/<set-id>/<semantic-name>.<ext>`. Concrete names use the `surface-*`, `action-*`, `slot-*`, `marker-*`, `indicator-*`, and `icon-*` grammar documented in `Assets/UI/README.md`; they never encode a route or screen.

No raw generation output, approved reference board, review capture, fixture, or partial fallback asset belongs here. A set definition must fill every required slot explicitly; a deliberately shared production binding is legal only when the manifest names its single owning set and the validator proves an exact mirror rather than inheritance or fallback.

Never hand-edit a runtime PNG or its `.meta`. Follow the [authoritative source/export/preview/activation workflow](../../../../docs/ui/ui-visual-system.md#132-%E8%B5%84%E6%BA%90%E8%BF%AD%E4%BB%A3%E9%A1%BA%E5%BA%8F): re-export in place, validate all production candidates against the same finite quality contract, preview in isolation, and use one atomic Undo-able active-set change. Missing bindings, unbound exports, importer drift, visible edge contamination, failed optical/slice/aspect checks, or release references to source/review/fixture assets are hard failures.

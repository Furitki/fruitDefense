# Production UI art exports

Only optimized textures and sprites eligible to fill validated `RuntimeUiArtSet` semantic slots belong here. Each folder name is the stable art-set ID and must map one-to-one to an owned source folder.

Production paths follow `Assets/UI/Art/Runtime/<set-id>/<semantic-name>.<ext>`. Concrete names use the `surface-*`, `action-*`, `slot-*`, `marker-*`, `indicator-*`, and `icon-*` grammar documented in `Assets/UI/README.md`; they never encode a route or screen.

No raw generation output, approved reference board, review capture, fixture, or partial fallback asset belongs here. A set definition must fill every required slot explicitly with locally owned runtime assets; inherited, shared, or mixed-set bindings are invalid.

Never hand-edit a runtime PNG or its `.meta`. Follow the [authoritative source/export/preview/activation workflow](../../../../docs/ui/ui-visual-system.md#132-%E8%B5%84%E6%BA%90%E8%BF%AD%E4%BB%A3%E9%A1%BA%E5%BA%8F): re-export in place and validate the sole complete production treatment against the finite quality contract. Missing bindings, unbound exports, importer drift, visible edge contamination, failed optical/slice/aspect checks, or release references to source/review/fixture assets are hard failures.

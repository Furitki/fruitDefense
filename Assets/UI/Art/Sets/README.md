# Runtime UI art-set definitions

Store production `RuntimeUiArtSet` ScriptableObject assets here. Definitions use stable set IDs and content revisions and may reference only textures/sprites under the matching `Assets/UI/Art/Runtime/<set-id>/` folder.

The approved release set ID is `sunny-orchard-painted`. `SunnyOrchardPaintedRuntimeUiArtSet.asset` is revision `1`, serializes all 49 required bindings, and is the only set referenced by the release theme.

`SunnyOrchardRuntimeUiArtSet.asset` is a complete, non-active 49-binding technical alternate used for isolated preview and atomic replacement validation. Its original 40 roles remain owned under `Art/Runtime/sunny-orchard`; the nine composition roles introduced with the painted contract are explicit serialized references to the reviewed shared production assets, recorded as `shared_from_set` in its manifest. This is not runtime inheritance or fallback.

Definitions must not resolve missing bindings through runtime inheritance, filename lookup, or fallback, and must never reference source, reference-board, review, raw-generation, or fixture assets. Any deliberately shared production binding is explicit, validated, and listed by the owning manifest.

Use `Fruit Defense/UI/Visual System` as the only daily candidate workflow. Validation precedes preview and activation; preview uses an isolated theme clone, and activation changes only the release theme's active-set reference through the single `Activate Runtime UI Art Set` Undo group. A failed candidate leaves theme, scenes, code, layout, and Presenter bindings unchanged. The complete workflow and aggregate/P0 gates are owned by the [stable UI guide](../../../../docs/ui/ui-visual-system.md#13-%E8%B4%A8%E9%87%8F%E6%A0%87%E5%87%86%E4%B8%8E%E6%9D%83%E5%A8%81%E5%B7%A5%E4%BD%9C%E6%B5%81).

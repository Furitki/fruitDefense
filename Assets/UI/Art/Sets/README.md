# Runtime UI art-set definitions

Store production `RuntimeUiArtSet` ScriptableObject assets here. Definitions use stable set IDs and content revisions and may reference only textures/sprites under the matching `Assets/UI/Art/Runtime/<set-id>/` folder.

The approved release set ID is `sunny-orchard-painted`. `SunnyOrchardPaintedRuntimeUiArtSet.asset` revision `6` serializes all 56 required bindings and is the sole production ArtSet referenced by the release theme.

Superseded alternate ArtSet definitions are not retained in production. Every binding must resolve to the matching set's own runtime/source directories; `shared_from_set`, inheritance, fallback, filename lookup, and mixed treatments are invalid.

Definitions must never reference source, reference-board, review, raw-generation, or fixture assets.

Use `Fruit Defense/UI/Visual System` as the daily validation and preview workflow. A future replacement must first arrive as one complete local 56-slot treatment and pass isolated validation before replacing the current set; the release hierarchy itself keeps one production ArtSet. The complete workflow and aggregate/P0 gates are owned by the [stable UI guide](../../../../docs/ui/ui-visual-system.md#13-%E8%B4%A8%E9%87%8F%E6%A0%87%E5%87%84%E4%B8%8E%E6%9D%83%E5%A8%81%E5%B7%A5%E4%BD%9C%E6%B5%81).

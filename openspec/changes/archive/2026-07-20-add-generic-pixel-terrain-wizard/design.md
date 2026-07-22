## Context

The native pixel baker is already profile-driven internally, but its public menu, source bootstrap, evidence filenames, and smoke validator are centered on the single `PixelGrass` sample. A custom profile can be created by hand, yet artists must know implementation paths, a one-source terrain has no explicit model, and baking another profile overwrites the sample evidence.

AI source creation has an additional boundary: the user explicitly requires bitmap generation to use the Codex imagegen skill and forbids editor or shell scripts from drawing source art. Unity cannot invoke that conversation skill directly and the project must not add an API key, network client, or hidden procedural fallback.

## Goals / Non-Goals

**Goals:**

- Provide one editor window for manual one-source, manual two-source, and imagegen-assisted terrain setup.
- Create or update a uniquely named profile and owned output folder, then bake and validate all sixteen assets.
- Give every terrain independent atlas/JSON evidence and make aggregate smoke validate all valid pixel profiles.
- Express AI work as a deterministic request with prompts, constraints, and exact target asset paths that Codex can fulfill with the imagegen skill.
- Apply a generated TileSet to the selected `DualGridTilemap` without editing its generated layer by hand.
- Keep the existing PixelGrass profile and filenames compatible.

**Non-Goals:**

- Calling an image API from Unity, storing credentials, or generating raster source art in C#, PowerShell, Python, shaders, or other scripts.
- Inventing missing manual or AI source images, converting non-pixel low-resolution art, or authoring final production art direction.
- Changing runtime mask rules, gameplay, release scenes, colliders, navigation, persistence, platform adapters, or the high-resolution baker.

## Decisions

### Model source layout and provenance on the profile

`DualGridPixelTerrainProfile` gains backward-compatible enums for `GrassAndSoil` versus `Single`, and `Imagegen` versus `Manual`. Existing serialized profiles retain `GrassAndSoil + Imagegen` as enum value zero. In single-source mode the grass texture is also the effective soil texture, so one asset is sufficient without duplicating a PNG.

The profile exposes a generic configuration method used by the wizard. Direct inspector editing remains available for advanced users.

### Keep AI generation outside Unity through a strict request artifact

The wizard's AI mode writes `<terrain-root>/Sources/<terrain-id>-imagegen-request.json`. The request records schema version, required skill `imagegen`, a `scriptDrawingAllowed: false` invariant, one or two prompts, and exact PNG target paths. A button copies a concise Codex instruction that references this request.

Unity never writes those PNG paths. It only refreshes the AssetDatabase, verifies that imagegen outputs now exist and are opaque, and enables baking. This preserves the user's generation constraint without adding credentials or pretending that a local editor script is an AI generator.

### Derive ownership from terrain id and asset root

The wizard owns a terrain root, defaulting to `Assets/DualGridTerrain/<TerrainId>`, with `Sources`, `Generated`, and `<TerrainId>BakeProfile.asset`. Terrain ids must be valid filename stems and output roots must remain under `Assets/`.

Existing source assets selected in manual mode are referenced in place; they are never copied or overwritten. AI source targets live under the owned `Sources` folder, but only imagegen may create or replace the PNGs.

### Make evidence and validation profile-specific

The baker derives a kebab-case evidence stem from `TerrainId`. `PixelGrass` keeps `pixel-grass-dual-grid-*`; other profiles receive `<terrain-stem>-dual-grid-*`. Bake, report, and validation methods receive the derived paths instead of global constants.

The no-argument public validator discovers all `DualGridPixelTerrainProfile` assets, requires each profile to be valid and baked, and validates pixels, importers, evidence hashes, and TileSet slots. A profile overload supports the wizard's Validate button.

### Keep the wizard as orchestration, not a second baker

The editor window delegates profile validation and all image composition to `DualGridPixelTileSetGenerator.Bake`. It exposes Create/Update and Bake, Refresh AI Sources, Validate, Reveal Output, and Apply to Selected Map actions. Applying records Undo, assigns the TileSet, aligns the output, rebuilds, and marks the scene dirty.

This avoids duplicating topology or importer logic in UI code.

## Risks / Trade-offs

- [AI mode is a two-surface workflow rather than an in-Unity network call] -> Make the copied Codex instruction and expected target paths explicit; this is required to enforce the imagegen-only boundary.
- [Aggregate smoke may fail on an unfinished profile asset] -> The wizard creates/updates and bakes in one action by default; failures identify the exact profile path.
- [Two terrain ids can normalize to the same evidence stem] -> Require unique terrain ids in the wizard and include the originating profile path in JSON; document the collision before overwriting evidence.
- [A single source produces less material contrast] -> Treat it as an intentional mode and retain independent edge-color and rim-width controls.
- [Manual textures may have unsuitable alpha or dense non-pixel detail] -> Validate opacity before output and show source constraints in the wizard; never silently preprocess source art.

## Migration Plan

1. Add backward-compatible profile fields and generic path helpers.
2. Generalize bake/evidence/validation while regenerating PixelGrass to prove unchanged compatibility.
3. Add the editor wizard and imagegen request format.
4. Extend smoke validation and exercise manual single-source, manual two-source, and AI-request paths.

Rollback removes the wizard and new profile fields, then restores the PixelGrass-only evidence helpers. Existing generated TileSets remain ordinary Unity assets and runtime data requires no migration.

## Open Questions

- Direct automation from a future Codex/Unity integration can replace the clipboard handoff only if it invokes the approved imagegen skill and preserves the same request contract.
- Optional terrain variants, animated tiles, and authored sixteen-mask templates remain separate future capabilities.

## Context

The accepted trial proves that a base-only 8×7 composition—35 grass cells inside a 21-cell soil U-frame—fits the current battlefield projection and palette renderer. The release catalog still builds every plantable cell as a square grass landform over soil and all three bundled themes point at one default palette. Trial assets are deliberately isolated and cannot become release dependencies.

The change crosses canonical map construction, theme identity, Unity asset publication, Battle-scene registration, and visual acceptance. Gameplay topology and the shared projection remain authoritative.

## Goals / Non-Goals

**Goals:**

- Make the approved square grass/soil treatment visible in the real `orchard-01` Battle flow.
- Preserve the exact first-level grid, route, core, markers, plantable cells, interactions, and deterministic simulation.
- Keep `orchard-02`, `orchard-03`, and authored maps using their current palette and layered visual composition.
- Keep runtime assets independent from trial scenes, review masters, prompts, and provenance files.

**Non-Goals:**

- Recoloring or regenerating the approved tiles.
- Replacing Dual-Grid terrain globally or removing layered authoring support.
- Changing level balance, progression, waves, controls, UI semantics, persistence, or platform support.

## Decisions

### Publish normalized copies instead of binding trial files

The two approved 64×64 PNGs will be copied byte-for-byte into a production-owned first-level terrain folder with production import metadata. The release palette will reference only those copies. This keeps review artifacts outside the release dependency graph and gives the approved pixels stable runtime ownership.

Alternative considered: bind the trial palette directly. Rejected because it couples release behavior to disposable validation content and violates the visual-system asset boundary.

### Give the first level a stable palette identity

Add `palette.orchard-01.square-grid`, bind `theme.orchard-01.day` to it, and keep the other themes on `palette.orchard.default`. Register both production palettes in the Battle scene and in deterministic project setup.

Alternative considered: overwrite `palette.orchard.default`. Rejected because that would silently change later levels and authored maps inheriting their themes.

### Select canonical visual composition explicitly at map construction

The single-route factory will require an explicit plantable-cell visual style. `orchard-01` uses base-only grass; existing later levels and fixtures explicitly use layered square grass-on-soil. Route and core cells remain base-only soil in both styles. The factory still produces one canonical layered-map aggregate, so rendering, gameplay compilation, projection, and hit testing do not fork.

Alternative considered: special-case the `orchard-01` map ID in the renderer or factory. Rejected because presentation would depend on an incidental identity and the map data would no longer describe what is rendered.

### Keep the first-level palette complete

The dedicated palette replaces only the grass and soil base bindings with approved production textures and retains the default palette’s other base, landform, and directed-edge bindings. Although `orchard-01` requests no optional overlays, a complete palette keeps catalog validation and future explicit authoring deterministic; there is no runtime fallback between palettes.

## Risks / Trade-offs

- [Texture seams become visible under filtering] → Use the accepted 64×64 normalized exports with repeat wrap and the same import contract proven by the trial; verify a real portrait WebGL canvas.
- [The wrong level changes] → Assert exact theme-to-palette identities and exact visual-cell counts for all bundled levels.
- [Gameplay changes while rewriting visuals] → Compare first-level route/capability/marker identities and deterministic fingerprint expectations; only visual source fields may differ.
- [Scene regeneration drops the new palette] → Make `ProjectSetup.ConfigureBattlefieldTerrain` the deterministic registry owner and validate the serialized Battle scene.

## Migration Plan

1. Publish production texture copies and the dedicated first-level palette.
2. Make first-level map construction base-only and update its theme palette identity.
3. Regenerate/configure the Battle scene so both production palettes are serialized.
4. Run focused validation, aggregate editor smoke, ordinary WebGL build, and first-level portrait capture.

Rollback is a normal source revert of this change; no save migration is required because level, map, gameplay, and persistence identities do not change.

## Open Questions

None. The approved preview, target level, and level-isolation boundary are explicit.

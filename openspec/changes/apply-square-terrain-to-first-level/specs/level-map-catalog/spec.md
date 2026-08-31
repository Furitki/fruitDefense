## MODIFIED Requirements

### Requirement: U-shaped teaching level
`orchard-01` SHALL provide a continuous U-shaped tile route with a teaching wave set and forgiving baseline rules that exercise planting, starting waves, movement, and merge preparation without requiring a new progression or reward system. Its presentation SHALL resolve `palette.orchard-01.square-grid` and SHALL describe every map cell as exactly one base-only square surface: 35 plantable interior cells use the approved production grass tile and the 21-cell U-shaped route/core frame uses the approved production soil tile, with no landform, contour, or edge overlay identities.

#### Scenario: Validate the teaching composition
- **WHEN** `orchard-01` is resolved and validated
- **THEN** its route has the entry, two U turns, exit, and core relationship required by the teaching map, its ordered teaching waves fit its rule-set bounds, its theme resolves `palette.orchard-01.square-grid`, and its 8×7 visual map contains exactly 35 base-only grass cells and 21 base-only soil cells

#### Scenario: Preserve teaching-level gameplay
- **WHEN** the square presentation is applied to `orchard-01`
- **THEN** its stable level and map identities, gameplay capabilities, route topology, markers, projection, hit-test geometry, deterministic simulation, persistence, and release scene flow remain unchanged

#### Scenario: Keep later bundled levels isolated
- **WHEN** all three bundled levels are resolved after the first-level square presentation is applied
- **THEN** only `orchard-01` references `palette.orchard-01.square-grid`, while `orchard-02` and `orchard-03` continue to reference `palette.orchard.default` and retain their layered terrain compositions

## ADDED Requirements

### Requirement: Production ownership of first-level square terrain
The release Battle SHALL bind `orchard-01` only to production-owned normalized texture and palette assets, SHALL register every terrain palette referenced by the bundled catalog, and MUST NOT depend on trial scenes, prompt records, provenance records, review masters, or trial palettes.

#### Scenario: Inspect release dependencies
- **WHEN** the Battle scene and first-level palette dependencies are validated
- **THEN** both required production palettes are registered, the first-level grass and soil bindings resolve to normalized 64×64 production textures, and no release dependency path is under `Assets/LayeredTerrain/Trials/`

#### Scenario: Build ordinary WebGL
- **WHEN** the normal WebGL build launches `Bootstrap → Lobby → Battle` for `orchard-01` at the portrait acceptance viewport
- **THEN** the real Battle canvas shows the approved 7×5 grass field inside the soil U-frame with continuous square-cell coverage, readable gameplay content, and no missing-palette diagnostic

## Context

`BattlefieldMapDefinition` currently uses one row-major `BattlefieldCellRole` value per cell. `Plantable`, `Route`, and `Blocked` describe gameplay space, while `Entry`, `Exit`, and `Core` describe semantic locations; the ordered route then repeats part of that information, initial flowerpot candidates use a separate group type, and enemy spawning is implicit because a new enemy starts at route progress zero. The integrated Dual-Grid presenter further maps plantable cells permanently to grass and route cells permanently to stone.

That structure is sufficient for the current three maps but cannot express a decorative grass cell that is not plantable, one cell with several compatible capabilities, channel-specific collision, multiple named routes or spawn markers, item points, or more than one semantic marker on a cell without expanding an exclusive enum and adding more special-case fields.

The project already separates a level into stable map, wave-set, rule-set, and theme identities, derives all battle geometry through `BattlefieldProjection`, and requires deterministic simulation, snapshot continuation, editor smoke validation, and real WebGL portrait acceptance. The new map model must preserve those boundaries and the exact current player-visible behavior while changing the authoring and compiled-data contract underneath them.

## Goals / Non-Goals

**Goals:**

- Give designers three explicit mental layers: semantic visual surfaces, gameplay topology/collision, and typed gameplay markers.
- Preserve ordered route topology as canonical data and give every route and marker a stable semantic identity.
- Allow multiple compatible cell capabilities and markers without making visual art authoritative for gameplay.
- Keep current `IsPlantable`, `IsRoute`, route sampling, core, initial-pot, projection, interaction, and terrain-rendering callers available as compiled compatibility views during migration.
- Preserve the three bundled maps, deterministic outcomes, snapshot compatibility, square-grid projection, safe-area containment, and release flow.
- Move terrain art selection under the resolved presentation theme through a stable palette identity while reusing the existing generated Dual-Grid assets.

**Non-Goals:**

- Enabling multi-route enemy selection, branching movement, runtime item drops, player spawn behavior, or trigger execution in this change.
- Adding a general-purpose visual map editor, remote map download, arbitrary marker scripting, reflection-driven marker handlers, or a generic property bag.
- Replacing the immediate-mode battle presenter with runtime Unity Tilemaps, Unity physics, NavMesh, or collider-driven deterministic gameplay.
- Changing route shapes, balance, initial pots, rewards, economy, progression, scene order, or platform-adapter readiness.

## Decisions

### Keep one map aggregate with three typed submodels

The authoring and portable map definition will remain one aggregate so a level designer opens one map asset rather than coordinating three independent assets. The aggregate contains:

1. a row-major visual surface layout using stable semantic surface IDs such as `surface.soil`, `surface.grass`, and `surface.stone-road`;
2. a row-major gameplay cell definition compiled from a finite set of capability and collision-channel IDs;
3. stable named ordered routes, typed marker groups, and stable typed markers.

The compiler exposes separate immutable presentation and gameplay views. Simulation receives topology, routes, and gameplay markers; presentation receives surfaces, the resolved theme palette, projection, and read-only gameplay affordances needed for feedback. Neither consumer receives a mutable reference to the other layer.

Three separate ScriptableObjects were rejected because they make dimensions, versions, and ownership easier to desynchronize. Keeping the exclusive role enum and adding more values was rejected because special points and cell capabilities are different axes and valid combinations would grow combinatorially.

### Compile stable source identifiers into efficient runtime masks

Portable/source data uses finite stable identifiers for cell capabilities and collision channels. Compilation validates them and converts them to explicit runtime bit masks. Initial capabilities cover planting, enemy traversal, and placement blocking; initial collision channels cover ground movement and projectile/placement queries, with new code-defined identifiers added only through reviewed capability changes.

Collision remains deterministic grid data queried by gameplay code. It does not create Unity `Collider`, physics, or NavMesh authority. This preserves fixed-step behavior and permits a future obstacle to block one channel without overloading a catch-all `Blocked` role.

A free-form string dictionary was rejected because it would let content invent unvalidated mechanics. A serialized numeric enum alone was rejected because ordinal changes are unsafe portable identities; stable source IDs plus compiled masks provide both validation and efficient queries.

### Keep routes ordered, named, and separate from cell capabilities

Each `BattlefieldRouteDefinition` owns a stable `routeId` and a unique ordered list of in-bounds, cardinally adjacent cells. Route membership may be compiled into a lookup/capability view, but movement order is never reconstructed from a Boolean grid.

`EntryCell` and `ExitCell` become convenience views of a selected route's first and last cells. An `EnemySpawn` marker references the route and its first cell; a `RouteGoal` marker references the route and its last cell; a `Core` marker supplies the protected target location. The current P0 execution profile still requires exactly one primary enemy route, one matching spawn, one matching goal, and one core, while the schema and compiler indexes permit additional named definitions for future capability changes.

Keeping `Entry` and `Exit` as exclusive cell roles was rejected because endpoints are properties of a route and cannot scale to multiple routes. Treating route membership as visual stone occupancy was rejected because art and movement are independently authored layers.

### Use a finite typed marker union with stable identities

Every marker has a stable `markerId`, finite `kind`, in-bounds cell, and kind-specific typed references. Initial supported definitions cover `EnemySpawn`, `RouteGoal`, `Core`, `InitialPotCandidate`, `PlayerSpawn`, `ItemSpawn`, and `Trigger`; only the first four are consumed by current battle behavior. Candidate selection rules such as the current initial-pot count live in typed marker-group definitions referenced by candidate markers.

The compiler validates marker multiplicity, referenced routes/groups/content IDs, required cell capabilities, endpoint relationships, and incompatible same-cell combinations. Multiple compatible markers may share a cell. Unsupported runtime marker kinds remain validated data and cannot execute behavior until a separate capability change adds a consumer.

Subclass-per-marker Unity assets were rejected because they fragment portable catalog export. One marker with an arbitrary payload dictionary was rejected because it cannot provide field-level validation or deterministic hashing. A finite discriminated union keeps export and validation explicit.

### Store semantic surfaces, derive Dual-Grid masks, and resolve art through the theme

The map stores one semantic ground surface ID per cell, not a mask number or Unity asset. The terrain renderer derives each four-corner Dual-Grid mask from equality with the requested surface ID. The current migration deterministically assigns grass to current plantable cells, stone road to current route/entry/exit cells, and soil to the remaining cells, so the existing image remains unchanged while later maps may choose different art without changing gameplay.

`LevelPresentationThemeDefinition` gains a stable terrain-palette ID. A Unity-side palette registry maps that ID to a `BattlefieldTerrainPalette` containing the soil base and ordered semantic-surface-to-`DualGridTileSet` bindings. The release Battle scene/project setup registers required palettes rather than owning one hard-coded active grass/road trio. Missing or unknown palette/surface bindings fail catalog or release smoke validation and never silently substitute another level's palette.

Storing per-vertex masks was rejected because they are derived data and can drift from cell surfaces. Using gameplay flags as permanent surface selectors was rejected because it prevents visual-only variation and makes presentation data authoritative for player-facing affordances.

### Compile legacy-shaped convenience views before changing consumers

The first implementation stage compiles the new source into the existing query surface: plantable and route lookups, ordered primary route cells/nodes, entry/exit/core cells, initial-pot groups, route descriptors, and projection inputs. Existing simulation and presenter code then migrates behind those APIs in small steps rather than switching data model and gameplay consumers simultaneously.

The three bundled maps are authored in both representations only during test fixtures, never as two live mutable runtime sources. Parity validation compares the legacy fixture with the layered compilation and then the legacy authoring path is removed once snapshots, catalog compilation, simulation, and presentation pass.

### Separate gameplay and presentation identity

Canonical gameplay map fingerprinting includes grid dimensions/scale, compiled gameplay cell capabilities and collision channels, ordered routes, gameplay marker groups, gameplay markers, and all gameplay-affecting references in stable deterministic order. It excludes surface layout, palette ID, sprite assets, and other presentation-only values. Presentation identity is tracked separately through the level theme and terrain palette.

Bundled `mapId` values remain unchanged. Existing supported snapshots resolve through the same exact catalog/content contract, and migration tests ensure layered compilation produces the same gameplay fingerprint and outcomes for current maps. Presentation-only changes cannot alter simulation checksums, while topology or gameplay-marker changes must alter the gameplay fingerprint.

### Preserve one projection and current render/hit-test geometry

All surface cells, routes, markers, core visuals, pots, entities, feedback, hit rectangles, and drag/drop targets continue to use `BattlefieldProjection`. Marker positions resolve from their canonical cells through existing projection helpers. Terrain continues to draw soil then ordered Dual-Grid surfaces inside the clipped `GridRect`; core, interaction feedback, entities, effects, and controls remain above it.

Creating logical Tilemap GameObjects or separate marker transforms was rejected because it would introduce second coordinate and safe-area authorities. The layered model is a data-ownership change, not a scene-hierarchy change.

## Risks / Trade-offs

- [Visual surfaces and gameplay capabilities can intentionally disagree] → Make the independence explicit, validate only required usability rules, and retain plantable/interaction affordances above terrain so decorative art cannot silently communicate legality.
- [A generic marker layer can become an unreviewed scripting system] → Keep a finite discriminated union, typed fields, code-owned validators, and no arbitrary callbacks or property bags.
- [Map schema migration can invalidate snapshots or deterministic hashes] → Preserve stable bundled map IDs, add legacy-to-layered fixtures, version canonical serialization, and verify exact continuation/outcome parity before removing adapters.
- [Supporting arrays of routes suggests behavior the simulation does not implement] → Validate the P0 execution profile as exactly one primary route/spawn/goal while reserving stable IDs and indexes; add multi-route execution only through a later behavior change.
- [Theme palette lookup can fail in builds] → Use stable palette IDs, explicit Unity resource registration, catalog/reference validation, release-scene smoke checks, and no silent default substitution.
- [Three conceptual layers could become three mutable runtime truths] → Compile once into immutable presentation/gameplay views and prohibit runtime mutation of authored layer data.
- [The migration touches several mature validation surfaces] → Stage compatibility APIs first and require editor smoke, deterministic snapshot tests, all three map captures, and the normal WebGL portrait acceptance before removing legacy code.

## Migration Plan

1. Archive the completed `integrate-dual-grid-terrain-into-battlefield` change so its capability is available for this delta, then validate this change against the updated main specs.
2. Add versioned layered source DTOs, stable IDs, finite marker/capability definitions, compiler indexes, structured validation, and immutable compiled views without changing current callers.
3. Add a legacy adapter and parity fixtures that compile the three existing maps into semantic surfaces, gameplay cells, `route.main`, spawn/goal/core markers, and initial-pot marker groups.
4. Switch bundled catalog construction to the layered source while keeping `BattlefieldMapDefinition` compatibility queries and current simulation behavior.
5. Split gameplay and presentation fingerprints, preserve supported snapshot restoration, and verify deterministic continuation/outcome parity.
6. Add the terrain palette identity/registry, resolve Dual-Grid masks from semantic surfaces, and update project setup/release scene binding without changing current pixels or geometry.
7. Migrate simulation and presentation consumers to explicit compiled views where useful, then remove the exclusive-role authoring path after all compatibility checks pass.
8. Run strict OpenSpec validation, `FruitDefense.Editor.ProjectSetup.SmokeValidate`, deterministic/snapshot tests, ordinary WebGL build, and real portrait captures for all three bundled levels.

Rollback keeps the legacy adapter and current generated assets until acceptance is complete. If the new catalog or palette path fails, restore bundled construction and presenter binding to the legacy compatibility source; no profile economy or server migration is involved.

## Open Questions

None for this change. Multi-route execution, runtime item spawning, trigger behavior, and a layer-oriented visual editor require separate future proposals.

## ADDED Requirements

### Requirement: Development-only GM battle isolation
The project SHALL provide an explicit GM stress battle only in the Unity Editor and Development builds, and MUST exclude it from released playable-level catalogs, production Resources, publication manifests, profile progression, settlement submission, and the normal WebGL build entry.

#### Scenario: Editor launches GM battle
- **WHEN** a developer selects `Fruit Defense/Playtest/GM 压力测试关`
- **THEN** the application enters the GM stress battle directly without selecting or mutating a released level

#### Scenario: Release catalog is inspected
- **WHEN** the bundled or published release catalog and the normal WebGL build are validated
- **THEN** no GM level, GM launch route, or GM-only fixture is reachable

### Requirement: Eight independent vertical lanes
The GM battlefield SHALL contain exactly eight stable named routes aligned to eight grid columns, SHALL provide one top spawn marker and one goal marker for every route, and SHALL expose exactly sixteen plantable pots in the bottom two rows.

#### Scenario: GM topology is compiled
- **WHEN** the generated 8-by-7 GM battlefield is validated
- **THEN** columns 0 through 7 each own a distinct vertical route through rows 0 through 4, and rows 5 and 6 contain two complete independently addressable plant rows

#### Scenario: Enemies occupy separate lanes
- **WHEN** enemies are spawned at equal progress on two different lane IDs
- **THEN** their canonical world and projected screen positions remain in their respective columns throughout traversal

### Requirement: Bounded manual enemy generation
The GM controls SHALL provide four bundled enemy selectors, batch sizes `1`, `10`, and `50`, per-lane spawn-pad commands, and an all-lanes command. Commands SHALL enqueue enemies in deterministic per-lane FIFO order and MUST enforce a combined active-plus-pending enemy cap of 500.

#### Scenario: One lane receives a batch
- **WHEN** a developer selects an enemy type and batch size 10 and activates lane 3
- **THEN** ten enemies of that type are enqueued only for lane 3 and drain in FIFO order at the fixed GM spawn cadence

#### Scenario: All lanes receive a batch
- **WHEN** a developer selects batch size 50 and activates the all-lanes command
- **THEN** the controller attempts to enqueue the same selected enemy batch for all eight routes without exceeding the global cap

#### Scenario: Cap is reached
- **WHEN** an enemy command would raise active-plus-pending count above 500
- **THEN** the accepted count is clamped to the remaining capacity, the battle remains responsive, and the visible pending/cap metrics reflect the result

### Requirement: Unlimited drag-based plant deployment
The GM controls SHALL expose all five bundled plants as unlimited drag sources and SHALL use the same drag activation threshold, preview overlap, legal-target cue, cancel behavior, and release-to-deploy interaction as the normal battle. A successful drop SHALL place or replace a one-star plant in any of the sixteen GM pots without consuming sunlight, inventory, refreshes, expansion, merge material, equipment, or another plant. Clicking a plant source MAY select it, but clicking a pot MUST NOT deploy or replace a plant.

#### Scenario: Empty pot is planted
- **WHEN** a developer drags a bundled plant source beyond the normal activation threshold and releases it over an empty GM pot
- **THEN** a one-star plant of that type is placed immediately and no economy value changes

#### Scenario: Occupied pot is replaced
- **WHEN** a developer drags another bundled plant source and releases it over an occupied GM pot
- **THEN** the existing plant is replaced in place with a one-star plant of the selected type without a confirmation or cost

#### Scenario: Plant source or pot is clicked
- **WHEN** a developer clicks a plant source without crossing the drag threshold or clicks a GM pot without an active drag
- **THEN** no plant is deployed, moved, or replaced

#### Scenario: Plant drag misses every pot
- **WHEN** an active GM plant drag is released without overlapping a legal GM pot
- **THEN** the command is cancelled, the source remains unlimited, and no simulation or economy state changes

### Requirement: Shared plant combat execution
Every plant deployed in the GM battle SHALL use the same bundled definition, one-star attributes, compiled ability loadout, fixed-step ability runtime, targeting, projectile/effect execution, and per-cell combat-distance calibration as the same plant in a normal battle. The GM session MUST NOT introduce a separate attack loop or derive combat range from the shorter GM route length.

#### Scenario: GM damage plants engage enemies
- **WHEN** any of the four bundled damage plants is deployed within its authored range of a living GM enemy and fixed steps advance
- **THEN** its normal ability activates and damages that enemy through the shared combat runtime

#### Scenario: GM producer plant advances
- **WHEN** the bundled producer plant is deployed and its authored period elapses in the GM session
- **THEN** its normal periodic ability executes through the shared combat runtime

#### Scenario: Map route lengths differ
- **WHEN** standard and GM maps use the same map-units-per-cell but have different total route lengths
- **THEN** the same authored legacy combat distance resolves to the same number of map cells on both maps

### Requirement: Shared plant combat presentation
The GM presenter SHALL consume the same ordered battle-presentation events and SHALL render plant idle/attack reaction, authored projectile archetypes, authored combat-effect archetypes, enemy status overlays, battlefield impact beats, and floating text through the same allocation-free gameplay combat renderer and `combat-vfx-atlas` used by the normal battle. It MUST NOT replace those visuals with GM-local debug squares, generic outline impacts, or missing-asset fallbacks.

#### Scenario: GM damage plant attacks
- **WHEN** any bundled damage plant releases its normal ability and the GM presenter consumes the resulting events
- **THEN** the plant reaction, projectile when authored, ability-specific impact effect, target reaction, and damage or defeat feedback resolve through the shared combat presentation path

#### Scenario: GM producer plant activates
- **WHEN** the bundled producer ability releases in the GM session
- **THEN** the shared sun-burst presentation is visible at the plant while the resource result remains simulation-owned

#### Scenario: Combat atlas is unavailable
- **WHEN** the GM presenter cannot resolve a valid shared `combat-vfx-atlas`
- **THEN** GM initialization fails explicitly before play begins and no debug projectile or effect substitute is drawn

#### Scenario: High-density combat is rendered
- **WHEN** many plants and enemies generate presentation events in one GM stress session
- **THEN** projectile/effect drawing reuses the single loaded atlas and bounded presentation buffers without per-event texture creation or per-frame combat-render allocations

### Requirement: No-failure stress lifecycle
The GM battle SHALL disable automatic waves, core damage, lives loss, victory, defeat, reward, settlement, result submission, snapshot export, and snapshot restore. An enemy reaching its route goal SHALL be removed and counted as escaped while the session remains playable.

#### Scenario: Enemy reaches goal
- **WHEN** an enemy completes any GM route
- **THEN** the enemy is removed, escaped count increments, and no defeat or terminal state is entered

#### Scenario: Battlefield becomes empty
- **WHEN** all active and pending GM enemies have been removed or defeated
- **THEN** the session remains active and accepts more plant and spawn commands instead of entering victory or settlement

#### Scenario: Persistence is requested
- **WHEN** a caller requests snapshot, resume, or result submission for a GM session
- **THEN** the request is rejected explicitly and no profile or released battle-session state is written

### Requirement: GM control presentation and live metrics
The GM presenter SHALL reuse the approved battle UI font, action roles, selector-card states, and safe-area layout rules; SHALL derive draw and hit-test rectangles from the same geometry; and SHALL show active, pending, escaped, and cap metrics together with pause and speed controls. Selection MUST remain distinguishable without relying on color alone.

#### Scenario: Supported portrait viewport is used
- **WHEN** the GM battle renders at any supported portrait viewport and representative top/bottom safe-area inset
- **THEN** all eight spawn pads, enemy and plant selectors, batch controls, live metrics, plant pots, pause, and speed controls remain visible and independently operable without overlap or clipping

#### Scenario: A selector is active
- **WHEN** an enemy, plant, or batch option is selected
- **THEN** its selected state includes a border, marker, or shape cue in addition to its color treatment

### Requirement: Registered tile-brush battlefield terrain
The GM battlefield SHALL render through the existing registered production `terrain-brush.grass-on-soil` composition and shared layered-terrain GUI renderer. Every cell SHALL use soil as its opaque base, the bottom two plant rows SHALL use the square grass landform with the refined sixteen-mask edge, and spawn/grid guidance SHALL remain a non-authoritative overlay above terrain. Initialization MUST fail explicitly when the required orchard palette or exact brush binding is unavailable and MUST NOT use solid-color terrain fallback, copied brush assets, direct asset-path loading, or runtime-generated substitute pixels.

#### Scenario: GM battlefield is rendered
- **WHEN** the GM session initializes with the registered orchard terrain palette
- **THEN** rows 0 through 4 show the brush's soil endpoint, rows 5 and 6 show its grass endpoint, and their boundary is assembled from the exact square refined grass-on-soil masks through the shared renderer

#### Scenario: Terrain dependency is unavailable
- **WHEN** the orchard palette, square grass landform, refined grass-on-soil edge, or any required renderable mask is missing or invalid
- **THEN** GM initialization fails with a terrain dependency error before the session becomes playable and no debug-color background is drawn

#### Scenario: Terrain presentation changes
- **WHEN** the GM terrain is switched from flat debug colors to the registered brush composition
- **THEN** the canonical routes, simulation checksums, tile projection, spawn-pad hit rectangles, pot hit rectangles, and 500-unit stress behavior remain unchanged

### Requirement: Development WebGL stress acceptance
The project SHALL provide a separate Development WebGL artifact containing the GM entry and SHALL validate it on a real portrait WebGL canvas without changing or overwriting the normal `Builds/WebGL` artifact.

#### Scenario: Development build is produced
- **WHEN** the GM Development WebGL build command completes
- **THEN** it writes to a distinct development output, enables the GM session, and leaves the normal release WebGL output unchanged

#### Scenario: Staged density is inspected
- **WHEN** a real Development WebGL canvas is exercised through representative 1, 10, and 50 batch commands up to the bounded high-density state
- **THEN** evidence records canvas viewport, active/pending counts, control responsiveness, visible lane alignment, and whether frame pacing or allocations breach the acceptance threshold

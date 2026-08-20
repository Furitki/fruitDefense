## ADDED Requirements

### Requirement: Single authoritative runtime UI theme
The release application SHALL use one validated application-wide UI theme for Bootstrap, Lobby, Battle chrome, blocking overlays, and Settlement, and MUST NOT expose a parallel legacy skin, screen-local fallback theme, or level-driven application chrome.

#### Scenario: Complete release flow is initialized
- **WHEN** the player proceeds through Bootstrap, Lobby, Battle, and Settlement
- **THEN** every player-facing UI surface resolves its semantic styles and shared artwork from the same release theme identity

#### Scenario: Release theme binding is invalid
- **WHEN** project validation finds a missing, duplicate, incomplete, or non-production theme binding
- **THEN** validation fails before publication and no default Unity skin or legacy visual path is accepted as a substitute

### Requirement: Semantic visual token contract
The runtime theme SHALL define semantic color, typography, spacing, shape, outline, opacity, and feedback-duration roles, and release UI components SHALL consume those roles instead of defining screen-specific copies of equivalent visual values.

#### Scenario: Two routes render equivalent primary actions
- **WHEN** Lobby Start and Settlement Retry are both available primary actions
- **THEN** they use the same semantic typography, surface, outline, spacing, and interaction-state rules while retaining their distinct labels and commands

#### Scenario: Theme token validation runs
- **WHEN** the aggregate editor smoke validates the release theme
- **THEN** required roles are complete, spacing follows the approved rhythm, essential text sizes meet the portrait contract, and configured foreground/background pairs pass the approved contrast thresholds

### Requirement: Shared component and state vocabulary
The release UI SHALL provide shared visual contracts for screens, panels, selectable cards, primary/secondary/quiet/danger buttons, resource and metric displays, status feedback, detail cards, result cards, and blocking modals. Interactive components MUST represent normal, pressed, selected where applicable, disabled, transitioning/loading, success, warning, and error states through consistent cues, and critical state meaning MUST NOT depend on color alone.

#### Scenario: Level card is selected
- **WHEN** the player selects a different Lobby level
- **THEN** the card uses the shared selected-state combination of color and a second explicit cue without changing its hit rectangle or starting a battle

#### Scenario: Route action is unavailable during transition
- **WHEN** an action is disabled because its route is loading
- **THEN** the component uses the shared disabled/transitioning treatment, remains readable, and does not appear equivalent to an enabled action

#### Scenario: Warning or error is presented
- **WHEN** Bootstrap or a route presenter exposes a recoverable or blocking error
- **THEN** the message uses the shared status hierarchy and at least one non-color indicator while preserving an available recovery action when the flow supports one

### Requirement: Orchard-cartoon UI art direction
Shared UI art SHALL extend the established warm orchard-cartoon presentation through warm light surfaces, soil-toned linework and text, leaf-green actions, sunlight-colored emphasis, restrained fruit-red danger, rounded readable silhouettes, controlled outlines, and shallow depth. It MUST avoid unrelated visual perspective, glossy generic mobile-game chrome, pure-black framing, or decoration that obscures battlefield content and state feedback.

#### Scenario: Cross-route style board is reviewed
- **WHEN** the approved reference board shows representative Lobby, Battle, modal, and Settlement components
- **THEN** the surfaces form one recognizable family while content art and level-specific battlefield terrain remain visually distinct from application chrome

### Requirement: UI art source and runtime asset contract
Every production UI texture or sprite SHALL belong to a complete UI art set, fill a stable semantic resource slot, have an owned editable lossless source or approved master, an optimized runtime export, documented scale and transparent-safe bounds, and validated Unity import settings. Nine-slice assets MUST declare protected borders; icons MUST use the approved square canvas and safe inset; experimental generation output, transient review captures, and test fixtures MUST NOT be referenced by the release theme or release scenes.

#### Scenario: Production UI art is imported
- **WHEN** an artist adds or updates a shared panel, button, indicator, or icon
- **THEN** editor validation confirms its naming, ownership, alpha and color-space intent, dimensions, padding, slicing, filter, wrap, mipmap, compression, and theme-reference rules

#### Scenario: Experimental artwork is referenced
- **WHEN** a release theme or scene references raw generation output, a review-only image, a test fixture, or an asset outside the production runtime hierarchy
- **THEN** aggregate validation fails with the offending asset and ownership rule

### Requirement: Rapid editor-time UI art-set iteration
The project SHALL provide one stable editor workflow that discovers complete UI art sets by stable identity, validates their semantic slots and production contracts, previews their common component states and representative release surfaces, and atomically activates one set through the authoritative runtime theme. Previewing or activating a replacement set MUST NOT require a C# edit, scene or prefab rewiring, layout change, or presenter-specific texture assignment.

#### Scenario: Artist previews a candidate set
- **WHEN** an artist selects a complete candidate in the visual-system editor workflow
- **THEN** the component/state gallery and representative route chrome render from that candidate while the serialized active release set and release scenes remain unchanged

#### Scenario: Artist activates a valid replacement set
- **WHEN** an artist activates a candidate whose required slots, assets, and import contracts all validate
- **THEN** one undoable atomic edit changes the theme's active art-set reference, every route resolves the replacement through the same semantic slots, and no scene, layout, or presenter binding changes

#### Scenario: Artist activates an incomplete set
- **WHEN** a candidate omits or duplicates a required semantic slot or references a non-production asset
- **THEN** activation is rejected with actionable slot and asset errors and the previously active set remains unchanged

#### Scenario: Artist replaces a slotted texture in place
- **WHEN** an approved runtime texture is updated while its stable path, `.meta` file, and semantic slot are preserved
- **THEN** normal Unity reimport updates every shared component that consumes the slot without code or scene changes

### Requirement: Exactly one release art set
The release theme SHALL serialize exactly one complete active production UI art set, and runtime presentation MUST NOT merge multiple sets, select a set by route or level, fetch a remote set, or fall back to artwork from a previous revision.

#### Scenario: Release validation inspects art-set selection
- **WHEN** the aggregate editor smoke validates the candidate release
- **THEN** it confirms one active production set, complete slot coverage, a stable set ID and revision, and no release dependency on alternate candidates or editor fixtures

### Requirement: Stable visual-system documentation ownership
The project SHALL maintain one durable UI visual-system guide that owns component usage, state matrices, art authoring/export rules, and review criteria, while the release theme asset owns exact runtime values and references. The guide MUST NOT duplicate build hashes, dated acceptance state, platform readiness claims, or transient screenshot results.

#### Scenario: Visual-system implementation is accepted
- **WHEN** final production assets and component rules pass review
- **THEN** the guide links the authoritative theme and documents only the stable rules needed by design, art, and engineering contributors

### Requirement: Presentation-only behavior boundary
Applying or changing the runtime UI visual system SHALL NOT mutate navigation legality, battle commands, simulation state, hit geometry, content identity, persistence, snapshots, or level-specific battlefield presentation data.

#### Scenario: Theme values or shared artwork change
- **WHEN** two otherwise identical sessions use different valid presentation revisions during development
- **THEN** their route requests, deterministic battle state, gameplay hit targets, and terminal result remain identical

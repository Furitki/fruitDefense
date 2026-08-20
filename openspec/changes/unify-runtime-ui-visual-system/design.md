## Context

The release flow is `Bootstrap → Lobby → Battle → Settlement`, but its player-facing chrome is drawn through three independent immediate-mode paths. `ShellStyleSet` styles Lobby and Settlement, `FruitDefenseGame` creates its own fixed-size styles and drawing helpers for Battle, and `AppFlowCoordinator.OnGUI` draws startup and error presentation with the default Unity skin. Each path has local colors, sizes, padding, enabled states, and panel treatments. There is no authoritative art guideline, shared theme asset, component-state matrix, or validated UI-art import contract.

Recent battlefield work has established a warm orchard-cartoon presentation with cream surfaces, leaf and soil colors, raster content art, and a 402-by-874 portrait reference composition. This change codifies and extends that existing direction to application UI; it does not introduce a new game art direction or alter the stable design overview. It must remain compatible with ordinary WebGL, packaged Chinese text, supported safe areas, existing route commands, and the current draw/hit-test geometry contract.

## Goals / Non-Goals

**Goals:**

- Give Bootstrap, Lobby, Battle chrome, modals, and Settlement one recognizable orchard-cartoon visual language.
- Make exact theme values, swappable shared artwork, and component states reusable instead of being recreated in each presenter.
- Let artists preview and activate a complete replacement resource set through one editor workflow without changing code, scene wiring, layout geometry, or semantic component bindings.
- Give artists a durable, tool-neutral contract for creating, exporting, naming, slicing, importing, and reviewing runtime UI assets.
- Preserve essential Chinese readability, non-color state cues, touch targets, safe-area containment, and cross-route hierarchy at all supported portrait sizes.
- Replace the current per-screen styling paths completely and enforce the new system through editor smoke and live WebGL evidence.

**Non-Goals:**

- Migrating immediate-mode runtime UI to uGUI or UI Toolkit.
- Redesigning terrain, plants, enemies, combat effects, map topology, or level-specific battlefield palettes.
- Changing navigation, commands, gameplay rules, balance, simulation determinism, snapshots, persistence, content identity, or platform adapters.
- Adding localization infrastructure, new screens, progression, economy, monetization, or mini-game platform support.
- Adding a player-facing runtime skin selector, remote asset delivery, compatibility theme, or silent default-style fallback.

## Decisions

### Use one application-wide theme owned by the persistent composition root

A serialized `RuntimeUiTheme` ScriptableObject will own the release theme identity and exact production values: semantic colors, typography roles, spacing metrics, shape/border metrics, opacity and restrained motion durations, font reference, and exactly one active `RuntimeUiArtSet` reference. The art set owns textures, sprites, semantic resource slots, and slicing data. `AppFlowCoordinator` will own the single release theme reference and pass the same validated theme and active art set to each route presenter and Battle session host during initialization.

The UI theme is application chrome, not a `LevelPresentationThemeDefinition`; selecting another level may change battlefield terrain and content art but cannot silently restyle navigation, controls, or result surfaces. Project setup will reproduce the one Bootstrap binding, and aggregate validation will reject a missing, duplicate, or invalid release theme.

Alternatives considered:

- Per-scene theme references were rejected because they can drift while still appearing valid in isolation.
- Static hard-coded colors and sizes were rejected because they keep art direction in C# and do not expose shared artwork or authorable semantic values.
- A player-facing theme switcher was rejected because this change establishes one release language. Editor-time art-set replacement is an authoring workflow and still serializes exactly one active release set.

### Resolve components through stable semantic art slots

Every shared visual component will request artwork by a finite semantic slot such as standard panel, raised panel, primary button, secondary button, selection marker, warning indicator, or resource icon. Presenters and layouts never reference concrete texture paths or candidate-set identities. Adding a new drawing treatment for an existing slot is therefore an art-set edit; adding a genuinely new semantic component remains an explicit code and contract change.

Each `RuntimeUiArtSet` must fill every required slot exactly once and carry a stable set ID plus a content revision used in preview and acceptance evidence. A set cannot inherit missing slots from another set, resolve by filename convention at runtime, or fall back to the previously active set. This makes a complete replacement observable and prevents a mixed visual language.

Alternatives considered:

- Referencing textures directly from presenters was rejected because every iteration would require code or scene rewiring and could leave different routes on different revisions.
- Per-slot override layers were rejected because partial inheritance would preserve old artwork invisibly and make visual acceptance ambiguous.
- Runtime addressable or remote loading was rejected because local editor iteration does not require a new dependency, runtime skin system, or platform delivery path.

### Provide one editor-time preview and atomic activation workflow

A stable `Fruit Defense/UI/Visual System` editor workflow under `Assets/Editor/Tools` will list discovered art sets in stable ID order, validate a candidate, render the common component/state gallery with the candidate, and preview representative Lobby, Battle, and Settlement chrome without changing the active release theme. Preview must identify missing slots and import-contract violations before activation.

Activating a valid candidate records one undoable edit to the `RuntimeUiTheme` active-set reference, marks the theme dirty, saves assets, and refreshes previews. It does not edit scenes, prefabs, presenter fields, layouts, or code. Invalid activation is atomic: the current active set remains unchanged. Replacing a texture in place while preserving its `.meta` file and semantic slot updates all consuming components after normal Unity reimport.

Editor tests will inject complete and incomplete fixture sets from `Assets/Editor/Tests/Fixtures` to verify preview isolation, validation, activation, undo, and unchanged scene references. Fixtures and candidate previews never enter production `Resources` or the release theme. The release validator accepts only one complete active production set; alternate sets may remain as unreferenced authoring candidates and are not packaged by the normal release scene dependency graph.

### Keep layout authority separate from visual styling

`PortraitShellLayout`, `BattlefieldProjection`, and the existing Battle interaction rectangles remain authoritative for draw and hit-test geometry. A shared UI scale context will convert theme metrics and typography to each surface's existing logical scale, but visual assets and style helpers will not introduce alternate rectangles or pointer regions.

The Battle presenter may extract UI-chrome layout constants into a dedicated `BattleUiLayout`, provided every existing click, drag, modal, and detail target continues to consume the same rectangles used for drawing. Battlefield content rendering remains outside the shared UI component layer.

Alternative considered: changing layout and visual system together. Rejected because it would make input regressions and art review inseparable and would expand the change beyond consistency.

### Define semantic tokens rather than screen-specific colors

The release theme will expose semantic roles instead of names such as `LobbyGreen` or `BattleBrown`:

- color: edge background, base surface, raised surface, primary action, secondary action, selection/accent, success, warning, danger, disabled, scrim, primary text, secondary text, and inverse text;
- typography: display, screen title, section title, body, control label, metric, and supplemental text, all backed by the packaged Chinese font;
- metrics: a four-point spacing rhythm, touch-target minimum, surface inset, component gaps, outline weights, and small/medium/large shape families;
- feedback: normal, focused/hovered, pressed, selected, disabled, loading/transitioning, success, warning, and error states with restrained unscaled-time durations.

The visual direction uses warm cream surfaces, deep soil-brown text and linework, leaf green for primary actions, sunlight amber for emphasis and selection, muted sage for disabled content, and fruit red only for danger. UI artwork uses rounded, readable cartoon forms, controlled outlines and shallow depth; it avoids glossy gradients, unrelated perspective, pure-black framing, and decorative detail that competes with battlefield content.

Critical state differences will combine at least two cues where practical, such as color plus outline, icon, label, opacity, or shape. Essential text will meet the existing minimum logical size and automated foreground/background contrast checks.

### Build a small shared component vocabulary

The shared runtime layer will implement only components already required by the release flow:

- screen background and safe-area surface;
- standard and raised panels;
- selectable level card;
- primary, secondary, quiet, and danger buttons;
- resource/metric display;
- status and validation feedback;
- contextual detail card;
- blocking modal and result card.

Each component consumes the theme and an explicit state; presenters supply copy, values, and commands. Lobby selection, Battle tool selection, disabled/loading actions, pause actions, and Settlement outcomes therefore use the same state language without introducing a general widget framework.

`ShellStyleSet`, `ShellGui`, Battle's local `BuildStyles`/`Style` paths, and the coordinator's default-skin surface will be deleted after their owning screens are migrated. Compatibility-only Lobby aliases and the standalone Battle compatibility presentation are removed in the same cleanup rather than wrapped by the new system.

### Separate stable guidance, art sources, and runtime exports

`docs/ui/ui-visual-system.md` will own durable usage rules, component anatomy/state matrices, asset-authoring rules, review checklists, and links to the authoritative release theme asset. It will not duplicate transient screenshots, build hashes, or acceptance status. `README.md` will link to the guide.

Editable, tool-neutral lossless art masters and the approved style board will live under a dedicated source hierarchy outside runtime `Resources`; optimized Unity textures and sprites will live in stable set folders under `Assets/UI/Art/Runtime`, with `RuntimeUiArtSet` definitions under `Assets/UI/Art/Sets`. Runtime filenames use stable semantic roles rather than screen names. Nine-slice surfaces declare protected corners and borders; icons declare a consistent square canvas and safe inset; exported raster dimensions are integer multiples of the intended logical size at the approved source scale.

An editor validator will enforce allowed file types, alpha, sRGB intent, filter and wrap mode, mipmap policy, compression policy, slicing/border data, transparent padding bounds, source/runtime ownership, and theme references. Raw generation output, experiments, review captures, and test fixtures cannot be referenced by release scenes or the theme asset.

Alternative considered: keeping only a prose art guide. Rejected because prose alone cannot stop importer drift, missing states, or release references to experimental assets.

### Review one state matrix across the complete release flow

The acceptance catalog will cover:

- Bootstrap initializing, blocking error, and retry;
- Lobby default, alternate level selected, disabled/loading action, and recoverable error;
- Battle ready HUD, active wave, tool selected, plant detail, legal/illegal interaction feedback, pause modal, and terminal overlay;
- Settlement victory, defeat, retry disabled/loading, and return.

Editor tests verify theme structure, token constraints, component-state completeness, import contracts, safe-area geometry, and scene binding. The ordinary WebGL player supplies full-screen reference and inset-safe-area captures from the real release flow. Review compares hierarchy, typography, component shapes, state cues, asset integrity, and absence of legacy/default-skin chrome; it does not accept isolated component previews as release evidence.

## Risks / Trade-offs

- [A shared theme can make every level feel visually identical] → Keep application chrome stable while allowing existing level themes to own battlefield-only terrain and content presentation.
- [Decorative nine-slice art can blur or stretch at non-reference sizes] → Protect corners, use integer source scaling, validate slice bounds, and inspect all supported portrait viewports on the real canvas.
- [Replacing styles across several routes can create a temporary mixed release] → Deliver each implementation phase with the affected route fully converted, but block final acceptance until every release surface uses the shared theme and all legacy paths are deleted.
- [Color unification can reduce state clarity] → Validate contrast and require non-color cues for selected, disabled, warning, and error states.
- [Art-source and runtime-export folders can drift] → Give each asset one stable semantic identity, validate provenance and import settings, and prevent raw/review assets from being referenced by the release theme.
- [Rapid swapping can accidentally publish a partial or experimental set] → Preview and validate complete semantic slots first, activate atomically with Undo, and make the release gate reject any active set outside production ownership.
- [Keeping alternate art sets can increase repository size] → Keep only intentional iteration candidates, package solely the active dependency graph, and delete rejected exports during delivery cleanup without deleting approved masters.
- [Battle UI cleanup can accidentally affect simulation or input] → Keep gameplay code unchanged, retain shared draw/hit rectangles, and rerun deterministic, shell, interaction, and WebGL acceptance suites after every Battle surface migration.

## Migration Plan

1. Author and approve the orchard-cartoon UI style board, stable guide, semantic token set, component-state matrix, and asset/import contract.
2. Add the single `RuntimeUiTheme`, complete `RuntimeUiArtSet` contract, shared style/component runtime, production UI assets, editor preview/activation workflow, validators, fixture coverage, and reproducible Bootstrap binding.
3. Convert Bootstrap startup/error and Lobby as the first complete route slice; remove their default-skin and old Shell styling paths.
4. Convert Settlement using the same result, metric, and action components; remove the remaining Shell style helpers.
5. Convert Battle header, trays, controls, detail card, status feedback, pause/terminal overlays, and transition states without changing battlefield content or hit geometry; delete Battle-local styling and standalone compatibility UI.
6. Run aggregate editor smoke, deterministic and shell coverage, ordinary WebGL build, and the complete full/inset portrait state matrix. Update the stable guide only for final accepted rules, not acceptance status.

Rollback is a normal source revert of the theme, UI art, presenter, scene wiring, documentation, and validation changes. There is no save-data or content migration. The implementation must not leave both visual systems active after rollback or completion.

## Open Questions

None. Exact illustrations and texture execution are resolved at the required style-board review, while the visual direction, semantic roles, asset contract, route scope, and acceptance surfaces are fixed by this change.

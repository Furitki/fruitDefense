## Why

The current Lobby is a single level-selection page, so the game has no durable out-of-battle loop connecting activity rewards, player growth, and battle power. A shared hub plus deterministic growth projection is needed before adding equipment, cultivation, and future event content, otherwise each feature will create its own navigation, persistence, and battle-injection path.

## What Changes

- Replace the single-purpose Lobby presentation with one portrait-safe outgame hub shell containing persistent top resources, a page host, and bottom navigation for Home, Activity, and Growth.
- Keep Home as the owner of current level selection and battle start; make Activity a real reward source; make Growth the owner of equipment and cultivation subpages instead of exposing fake placeholder actions.
- Add a validated outgame-content catalog for item, activity, outgame growth-equipment, cultivation, cost, and per-gameplay growth-policy definitions, pinned by the existing root game-content manifest.
- Add atomic, idempotent activity reward claims that grant stable item or outgame growth-equipment identities to the player inventory.
- Replace the non-economic player profile with the single current progression-capable schema containing inventory, reward receipts, outgame growth-equipment, cultivation ranks, and loadout state.
- Add deterministic growth commands that consume inventory and update outgame growth-equipment, cultivation, and loadout state without allowing UI presenters to mutate profile DTOs directly.
- Resolve only the growth domains and attributes allowed by the selected gameplay before Battle, show the exact applied/suppressed preview on Home, and freeze the resolved result into an immutable, fingerprinted launch snapshot.
- Apply launch-time growth as a battle baseline before transient runtime statuses, with no live profile reads after session initialization.
- Extend the Sunny Orchard visual system with a shared hub/navigation anatomy and the required resource, activity, equipment, cultivation, and growth-preview states while preserving its approved visual language.
- Expand Editor and real-WebGL acceptance to cover navigation, reward claim, upgrade/equip, per-level growth filtering, battle launch, and return to the hub.
- **BREAKING**: remove `PlayerProfileEnvelopeV1` and the single-page `LobbyPresenter` ownership model; no legacy profile reader, migration layer, or parallel launch path is retained.

## Capabilities

### New Capabilities

- `outgame-hub-shell`: Defines Home, Activity, and Growth page navigation inside Lobby, shared chrome, page lifecycle, state preservation, and portrait-safe interaction.
- `outgame-content-catalog`: Defines manifest-pinned item, activity, outgame growth-equipment, cultivation, cost, and growth-policy content with stable identities and cross-reference validation.
- `activity-item-rewards`: Defines visible activity claim states, idempotent reward receipts, and atomic inventory grants.
- `player-growth-progression`: Defines inventory ownership, equipment/cultivation upgrades, loadouts, costs, persistence, and atomic progression commands.
- `battle-growth-projection`: Defines per-gameplay growth permission, preview, deterministic launch projection, fingerprinting, and baseline application in Battle.

### Modified Capabilities

- `local-profile-service-ports`: Replaces the non-economic V1 profile contract with the single current progression-capable profile schema and its validation/persistence rules.
- `level-selection-flow`: Moves level selection into Home and requires the selected level's exact growth policy and applied preview to be resolved before launch.
- `p0-integrated-player-flow`: Expands the player loop and integrated acceptance from a single Lobby page to Activity reward, Growth upgrade, Home launch, Battle, Settlement, and return.
- `portrait-game-interface`: Adds shared hub chrome, bottom navigation, Growth subnavigation, and balanced Activity/Growth page compositions to the supported portrait matrix.
- `runtime-ui-quality-standard`: Adds stable semantic anatomy and state language for hub navigation, resource balances, reward claims, equipment, cultivation, and growth previews.
- `webgl-visual-acceptance`: Adds canonical live-canvas states and interactions for every new outgame page and the complete reward-to-battle loop.

## Impact

- App/Shell: Lobby composition, internal page router, layout authority, presenters, flow contracts, and acceptance bridge/catalog.
- Persistence: player profile schema, validation, local store fixtures, revisioned atomic writes, and structured recovery behavior.
- Content: root game-content manifest, a new outgame catalog, ScriptableObject authoring/export, compiled indexes, and cross-catalog validation.
- Battle boundary: launch request, resolved level growth-policy reference, deterministic source identity, simulation initialization, effective-attribute pipeline, retry, and snapshot validation.
- UI: `RuntimeUiTheme`, `RuntimeUiArtSet`, copy/icon catalogs, shared actions/cards/slots/statuses, and supported safe-area layouts.
- Tests and release gates: outgame content/profile/transaction unit tests, Shell layout/flow validation, aggregate Editor smoke, and ordinary plus acceptance WebGL builds.
- No new package, backend, remote clock, daily reset, cloud economy, payment, ads, or remote content delivery is introduced in this change.

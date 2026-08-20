## Why

Lobby, Battle, Settlement, and the persistent startup/error surface currently use separate immediate-mode styles, colors, spacing, and component treatments. The release flow is functional, but it lacks one enforceable visual language and one art-production contract, making every UI polish pass local, inconsistent, and difficult to review as a whole.

## What Changes

- Establish the existing warm orchard-cartoon direction as one runtime UI visual system without changing game positioning, player flow, or gameplay rules.
- Define one authoritative set of semantic color, typography, spacing, shape, outline, elevation, opacity, and motion tokens for the 402-by-874 portrait reference composition.
- Define shared visual and interaction states for panels, cards, buttons, resource meters, selection indicators, status feedback, detail surfaces, and blocking modals.
- Define UI-art source, export, slicing, transparent-padding, scale, naming, import, and ownership rules, including a reviewed reference style board and production-ready shared UI assets.
- Separate semantic theme tokens from a complete swappable UI art set, and provide an editor-time preview/activation workflow so artists can compare or replace all shared resources without editing code, scenes, layouts, or component bindings.
- Apply the system consistently to Bootstrap startup/error presentation, Lobby, the Battle HUD/control surfaces, pause/result overlays, and Settlement while preserving their existing commands and hit geometry.
- Remove obsolete per-screen style paths and compatibility-only UI helpers after each surface adopts the shared system; do not retain a fallback theme or parallel legacy presentation.
- Extend editor validation and real WebGL portrait evidence so visual consistency, component states, Chinese readability, safe-area containment, and artwork integrity are reviewed across the complete release flow.

## Capabilities

### New Capabilities

- `runtime-ui-visual-system`: Defines the authoritative runtime visual tokens, shared component states, UI-art production contract, asset ownership, documentation, and cross-route application rules.

### Modified Capabilities

- `portrait-game-interface`: Requires Bootstrap, Lobby, Battle, and Settlement to present one coherent visual hierarchy and preserve readable, touch-safe results when shared artwork and styles scale into supported portrait safe areas.
- `webgl-visual-acceptance`: Expands required evidence from isolated Battle states to a cross-route visual-system review covering startup/error, Lobby selection, Battle HUD and modal states, and Settlement.

## Impact

- Runtime presentation under `Assets/Scripts/App`, `Assets/Scripts/Shell`, and `Assets/Scripts/FruitDefenseGame.cs`.
- New shared runtime UI theme/style code, semantic-slot UI art-set assets, and production UI art under a dedicated `Assets/UI` hierarchy, with reproducible scene wiring through existing editor project setup.
- A stable editor authoring workflow under `Assets/Editor/Tools` for validating, previewing, atomically activating, and undoing an art-set change; release runtime still has exactly one active set and no skin switcher.
- A durable `docs/ui/ui-visual-system.md` source of truth linked from `README.md`; transient review evidence remains outside the stable design overview.
- Editor validation under `Assets/Editor/Tests` and the aggregate `FruitDefense.Editor.ProjectSetup.SmokeValidate` entry, plus the ordinary WebGL build and live portrait capture path.
- No new Unity package, UI-framework migration, gameplay or balance change, persistence migration, scene-flow change, content-catalog change, or mini-game platform claim.

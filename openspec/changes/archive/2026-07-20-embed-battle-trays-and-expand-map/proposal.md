## Why

The current portrait battle screen gives a large share of the vertical viewport to a separate build panel and two persistent text surfaces, leaving the battlefield visually compressed and the primary drag sources detached from the map they affect. The battle layout should read as one cohesive play surface with the map first and its tools docked inside it.

## What Changes

- Expand the visible battlefield region downward and make it the dominant surface below the compact header.
- Move the three weapon controls and flowerpot control into an embedded tool tray inside the battle surface.
- Move the five nursery slots and refresh action into an embedded refresh tray inside the battle surface.
- Remove the persistent bottom guidance copy and the separate operation-status panel from the default battle screen.
- Keep selected-plant information available only as a compact contextual card inside the battle surface, with no reserved explanatory text when nothing is selected.
- Remove the opaque square backplates behind flowerpots and fruit icons, enlarge those icons to nearly fill their cells, and preserve interaction state through transient outlines instead of persistent frames.
- Give every plant definition an independent upward visual-height offset so its map art can sit above the flowerpot without plant-specific rendering branches; zero keeps the current center and one moves it upward by one logical point.
- Preserve planting, movement, nursery return, merging, weapon installation, pot expansion, refresh cost, wave, pause, and speed behavior.
- Extend portrait geometry and WebGL acceptance so the enlarged map and embedded controls are verified at the 402-by-874 reference viewport.

## Capabilities

### New Capabilities

- `embedded-battle-control-surface`: Enlarged portrait battlefield composition, embedded tool and refresh trays, contextual plant details, and removal of persistent bottom guidance/status surfaces.

### Modified Capabilities

None. The promoted baseline specifications do not currently own the portrait battle-control composition.

## Impact

- Runtime presentation, frameless icon drawing, configured plant-height offsets, and shared input rectangles in `Assets/Scripts/FruitDefenseGame.cs`.
- Plant presentation configuration in the bundled battle-content DTO, factory, authored asset, and canonical JSON.
- Battlefield projection, enlarged flowerpot visuals, and portrait geometry validation in `Assets/Scripts/Core/BattlefieldProjection.cs` and `Assets/Editor/ProjectSetup.cs` where required.
- WebGL acceptance coordinates and evidence for the enlarged portrait battle surface.
- No gameplay simulation, combat balance, content, persistence, platform adapter, or release-gate behavior changes.

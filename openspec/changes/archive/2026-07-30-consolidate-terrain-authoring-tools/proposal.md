## Why

The embedded terrain laboratory is already separate from playable-map authoring, but its hand-positioned Scene panel, stale `Window` naming, and hidden configured contour still make it feel like a second floating map editor. The existing two brush cards and contextual `只绘制纯图` checkbox are accepted and should remain stable while the laboratory's role and hosting are clarified.

## What Changes

- Preserve the ordinary laboratory's two directed composition brush cards and contextual `只绘制纯图` checkbox without adding another preset system or changing canonical map data.
- Reframe the terrain laboratory as a terrain-resource acceptance surface rather than a map-authoring alternative, with its configured contour and resource-validation role visible in the UI.
- Replace the hand-positioned Scene GUI area with a native Scene-view Overlay that can dock, collapse, and close using Unity's standard overlay behavior.
- Let one contour-specific edge resource drive both directed brush cards: the authored direction uses its mask directly and the reverse direction uses the complemented mask. Preserve an explicitly authored reverse resource as a compatibility override, but no longer require it.
- Keep only the currently selected edge family in active acceptance configuration and record all other edge/source families as reviewable deletion candidates without deleting assets in this change.
- Remove stale `Window` terminology from ordinary UI, tests, and active specifications while retaining only the smallest compatibility launch facade required by existing callers.
- Keep the established two-tool boundary: the canonical map editor creates playable maps; the terrain resource acceptance surface validates art and rendering only.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `layered-terrain-painter-workflow`: Recasts the embedded laboratory as a native Scene Overlay for resource acceptance, makes configured contour identity visible, preserves the accepted brush-card/pure-toggle interaction, and removes stale standalone-window behavior.
- `layered-terrain-brush-authoring`: Makes shared same-contour edge registration the default contract for every future directed pair brush, including the required mask-00 reverse-center endpoint and exact reverse compatibility override.

## Impact

- Editor UI in the layered-terrain laboratory/session files plus shared edge resolution in the runtime terrain palette and presenter; canonical map data remains unchanged.
- OpenSpec wording and concise README routing for the two authoring tools.
- No changes to gameplay rules, saves, visual-cell schema, combat simulation, release scene flow (`Bootstrap → Lobby → Battle → Settlement`), or mini-game platform status.
- Validation uses focused editor smoke coverage, `FruitDefense.Editor.ProjectSetup.SmokeValidate`, strict OpenSpec validation, and runtime presentation parity because shared reverse-mask resolution affects the release terrain presenter.

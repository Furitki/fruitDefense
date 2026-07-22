## Why

The current portrait conversion only scales and repositions the former landscape side panel, producing a narrow, sparse interface with poor information hierarchy. The deployed WebGL build also loses all player-facing text because it relies on operating-system fonts that are unavailable in the browser runtime, and the existing validation cannot reliably inspect the continuously rendering game.

## What Changes

- Replace the legacy side-panel scaling with a portrait-first game shell based on the iPhone 17 logical aspect and safe area.
- Reorganize the interface into a compact status header, a full-width battlefield, and a full-width mobile control surface with collapsible secondary information.
- Bundle a redistributable Chinese-capable font and use it consistently in WebGL instead of requesting an operating-system font at runtime.
- Define minimum readable text and touch-target sizes for the portrait interface.
- Add repeatable WebGL visual acceptance that captures the live canvas at the target portrait viewport without waiting for the Unity render loop to become idle.
- Validate the initial screen and representative interaction states before a portrait build is considered publishable.

## Capabilities

### New Capabilities

- `portrait-game-interface`: Player-facing portrait layout, safe-area behavior, readable bundled text, and touch-sized controls for the iPhone 17 reference viewport.
- `webgl-visual-acceptance`: Repeatable live-canvas capture and state-based visual checks for portrait WebGL builds.

### Modified Capabilities

None. This repository does not yet contain baseline OpenSpec capabilities.

## Impact

- Primary runtime presentation changes in `Assets/Scripts/FruitDefenseGame.cs` and new font assets under `Assets/Resources`.
- Player and WebGL resolution settings remain portrait but will use a logical-coordinate layout rather than physical-pixel placement.
- Editor/build tooling gains a visual acceptance entry point and captured evidence; gameplay simulation, combat balance, and wave logic remain unchanged.
- The WebGL build and deployed output must be regenerated after implementation.

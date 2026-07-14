## 1. Layout and State Geometry

- [x] 1.1 Confirm `restructure-battlefield-map-and-tiles` is applied and expose a lower-right battlefield control inset that does not overlap route, core, or planting targets
- [x] 1.2 Add shared geometry for the battlefield wave action and one/two-action modal layouts, with draw and hit testing using the same at-least-44-point rectangles
- [x] 1.3 Extend portrait geometry validation for ready, playing, between-wave, paused, victory, and defeat control visibility

## 2. Contextual Session Controls

- [x] 2.1 Replace the non-interactive battlefield prompt with `开始波次` in ready and `立即开始下一波` during the between-wave countdown
- [x] 2.2 Hide the wave-start action during active and terminal phases while preserving active battle status and automatic next-wave timing
- [x] 2.3 Add `继续游戏` and `重新开始` as separate pause-modal actions while retaining the existing keyboard resume path
- [x] 2.4 Centralize full-run restart cleanup and route pause, victory, and defeat restart actions through it
- [x] 2.5 Remove the persistent bottom wave/restart controls, delete `ActionRect`, and give the reclaimed height to the coordinated portrait battlefield layout

## 3. State and Reset Validation

- [x] 3.1 Add deterministic checks that ready starts wave one, between-wave early start skips the remaining timer, and playing exposes no start action
- [x] 3.2 Add reset checks proving pause restart clears simulation and transient inspection, tool, drag, reward, pulse, message, and paused presentation state
- [x] 3.3 Run Unity compilation and `FruitDefense.Editor.ProjectSetup.SmokeValidate`

## 4. WebGL Acceptance

- [x] 4.1 Update portrait acceptance to interact with the new battlefield wave action and both pause-modal actions using stable region-derived coordinates
- [x] 4.2 Build WebGL and capture ready, active-wave, between-wave, paused, continued, and restarted states at 402 by 874
- [x] 4.3 Review evidence for exact Chinese labels, absence of the former bottom row, clean restart state, safe-area containment, touch sizing, and no battlefield-target overlap

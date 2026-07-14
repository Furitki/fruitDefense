## 1. WebGL Text Foundation

- [x] 1.1 Select a redistributable Chinese-capable font, add the font and license under `Assets/Resources/Fonts`, and record its compressed WebGL size impact
- [x] 1.2 Replace operating-system font discovery with packaged font loading and an explicit fallback/error path
- [x] 1.3 Extend editor smoke validation to prove the packaged font exists and covers representative Chinese UI characters

## 2. Portrait-First Interface

- [x] 2.1 Introduce a 402-by-874 logical reference and safe-area layout helpers, then remove `SideLayoutRect` and legacy landscape side-panel geometry
- [x] 2.2 Rebuild the compact HUD and full-width battlefield regions with shared draw and hit-test rectangles
- [x] 2.3 Rebuild equipment, expansion, nursery, refresh, and persistent wave controls as a full-width mobile build tray
- [x] 2.4 Move selected-plant details into a dismissible contextual surface and convert transient guidance into a compact status/toast surface
- [x] 2.5 Enforce minimum text and touch-target sizes and verify plant, weapon, pot, nursery, pause, speed, and wave interactions against the rendered portrait geometry

## 3. WebGL Visual Acceptance

- [x] 3.1 Add a PowerShell acceptance command that launches an isolated headless Chrome process with DevTools enabled and always cleans up only the processes it owns
- [x] 3.2 Detect HTTP, Unity canvas, player-load, timeout, and screenshot failures with a non-zero command result
- [x] 3.3 Capture the initial screen, selected-plant details, active wave, and blocking modal at the 402-by-874 reference viewport
- [x] 3.4 Check captured evidence for expected Chinese labels, full-width control use, safe-area containment, and unclipped primary controls, and write a concise acceptance manifest

## 4. Build and Release Verification

- [x] 4.1 Run Unity compilation and `ProjectSetup.SmokeValidate`, then rebuild the WebGL output
- [x] 4.2 Run portrait visual acceptance against the local build and review every required state before publishing
- [x] 4.3 Publish the verified WebGL output, repeat HTTP artifact checks, and run the same visual acceptance against the deployed URL

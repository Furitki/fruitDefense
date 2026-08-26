# WebGL Battle Acceptance

## Build

- Command: `FruitDefense.Editor.WebBuild.Build`
- Unity result: `Success`
- Marker: `FRUIT_DEFENSE_WEB_BUILD_OK`
- Output size: `10,259,511` bytes
- Data payload: `673f8caab7c5`
- Framework payload: `dfdfe8bd231e`
- Loader payload: `1b13b1831b18`
- Wasm payload: `368f6086ad60`
- Build log: `Logs/combat-impact-final-release-webgl.log`

## Browser acceptance

- Runtime: the final WebGL build, not an editor mock.
- Route: direct Battle acceptance route for `orchard-01`.
- Viewport and canvas: `402 × 874`, matching the iPhone 17 portrait aspect used by this project.
- Interaction exercised: refresh roster, drag five plants into battlefield pots, start waves, pause/resume, and switch between `1×` and `2×`.
- Feedback observed: damage numbers, hit flashes, target reactions, projectiles, plant attack reactions, and battlefield-only motion remain bounded during dense waves.
- Layout observed: safe area, HUD, nursery, refresh action, plant details, battlefield, and hit targets remain visible and operable; presentation shake does not move HUD or authoritative hit-test geometry.
- Determinism boundary: changing presentation speed/feedback does not mutate simulation snapshots, checksums, or combat RNG according to the focused Editor smoke suites.

## Evidence

- `battle-402x874-ready-final.png`: final build ready in Battle.
- `battle-402x874-arranged-final.png`: five plants placed through real pointer interaction.
- `battle-402x874-1x-final.png`: live `1×` wave with damage feedback.
- `battle-402x874-2x-final.png`: live `2×` wave with damage feedback.

This is ordinary WebGL acceptance only. It does not authorize or imply Douyin or WeChat conversion support.

---
id: p0-release-baseline
parent: design-kb-home
order: 20
status: active
---

# FruitDefense P0 release baseline

Recorded on 2026-08-13 for the deployed `main` revision
`85142849f6a3883f3837fc3e9bcd981090a24f2d` with Unity `6000.3.19f1`.

## Runtime shape

- Enabled scene order: `Bootstrap`, `Lobby`, `Battle`, `Settlement`.
- `AppBootstrap` is the only persistent composition root; battle hosts are explicitly initialized and disposable.
- Default level: `orchard-01`; a battle pins its session ID, seed, and content version through settlement or retry.
- Fixed simulation: 20 Hz, five-step catch-up ceiling, serializable deterministic random state, no background catch-up.
- Runtime content: finite trigger/target/effect skill composition compiled from the bundled versioned catalog.
- Persistence: local `PlayerProfileEnvelopeV1` plus deterministic `BattleSnapshotV1`; P0 does not auto-resume a battle.

## Versioned artifacts

- Catalog ID/version: `catalog.bundled.orchard@1.0.0`.
- WebGL payload versions: loader `69214ab44707`, data `e939d5c1273f`,
  framework `713319cb0c52`, and Wasm `7b18f089ae68`.
- WebGL output: 17,658,272 bytes; entry SHA-256
  `72C82E7BEC80FC498E934D42D44F7BBB9C67951A1CC4DC419430DFFB86818BD9`.
- Ordinary WebGL public URL: `http://175.178.80.66:3000/`.

## Release gates

- Unified Unity gate: `FruitDefense.Editor.P0ValidationSuite.Run` -> `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.
- Repeated basic-attack regression passed for pea, watermelon, banana, and durian using real fixed ticks and damage.
- WebGL build: `FruitDefense.Editor.WebBuild.Build` -> `FRUIT_DEFENSE_WEB_BUILD_OK`.
- Local portrait acceptance: `scripts/accept-webgl-portrait.ps1 -ServeLocal` -> `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`.
- Online publication: `scripts/publish-online.ps1 -Execute` -> `FRUIT_DEFENSE_ONLINE_PUBLISH_OK`; remote entry health and WebGL delivery headers passed.
- Accepted local manifest: `Logs/visual-acceptance/20260813-205224/acceptance.json`.
- Accepted deployed manifest: `Logs/visual-acceptance/20260813-205257/acceptance.json`.
- The deployed warm reload transferred 0 WebGL payload bytes; strong content ETags,
  independent payload versions, immutable matching-version caching, and HTML
  revalidation all passed.
- Schema-2 publication manifest: `Builds/Pipeline/online-publish-manifest.json`.

## Deferred to P1

- Automatic local/cloud battle resume, remote content catalogs/bundles, immutable release channels, login, cloud profile, and code-package updates.
- Douyin device/SDK integration remains gated by the compatibility spike. The committed spike status is Yellow until TTSDK, developer tools, App ID, simulator, Android, and iOS device evidence are available; mini-game adapters must remain explicitly unavailable rather than masquerading as Web.

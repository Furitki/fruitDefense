---
id: p0-release-baseline
parent: design-kb-home
order: 20
status: active
---

# FruitDefense P0 release baseline

Recorded on 2026-08-13 for local branch `main` at revision
`8497ccf58bfadc495ba86fa7748ec6aad109dbaa` with Unity `6000.3.19f1`.

## Runtime shape

- Enabled scene order: `Bootstrap`, `Lobby`, `Battle`, `Settlement`.
- `AppBootstrap` is the only persistent composition root; battle hosts are explicitly initialized and disposable.
- Default level: `orchard-01`; a battle pins its session ID, seed, and content version through settlement or retry.
- Fixed simulation: 20 Hz, five-step catch-up ceiling, serializable deterministic random state, no background catch-up.
- Runtime content: finite trigger/target/effect skill composition compiled from the bundled versioned catalog.
- Persistence: local `PlayerProfileEnvelopeV1` plus deterministic `BattleSnapshotV1`; P0 does not auto-resume a battle.

## Versioned artifacts

- Catalog ID/version: `catalog.bundled.orchard@1.0.0`.
- WebGL content version: `f80e7e90b714`.
- WebGL output: 17,704,265 bytes; entry SHA-256
  `69C64E79EB82DAFCBD10B5E817301594C0B262E7A530981345B4BDC757851E7A`.
- Ordinary WebGL public URL: `http://175.178.80.66:3000/`.

## Release gates

- Unified Unity gate: `FruitDefense.Editor.P0ValidationSuite.Run` -> `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.
- Repeated basic-attack regression passed for pea, watermelon, banana, and durian using real fixed ticks and damage.
- WebGL build: `FruitDefense.Editor.WebBuild.Build` -> `FRUIT_DEFENSE_WEB_BUILD_OK`.
- Local portrait acceptance: `scripts/accept-webgl-portrait.ps1 -ServeLocal` -> `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`.
- Online publication: `scripts/publish-online.ps1 -Execute` -> `FRUIT_DEFENSE_ONLINE_PUBLISH_OK`; remote entry health and WebGL delivery headers passed.
- Accepted local manifest: `Logs/visual-acceptance/20260813-175033/acceptance.json`.
- Accepted deployed manifest: `Logs/visual-acceptance/20260813-175100/acceptance.json`.
- Publication manifest: `Builds/Pipeline/online-publish-manifest.json`.

## Deferred to P1

- Automatic local/cloud battle resume, remote content catalogs/bundles, immutable release channels, login, cloud profile, and code-package updates.
- Douyin device/SDK integration remains gated by the compatibility spike. The committed spike status is Yellow until TTSDK, developer tools, App ID, simulator, Android, and iOS device evidence are available; mini-game adapters must remain explicitly unavailable rather than masquerading as Web.

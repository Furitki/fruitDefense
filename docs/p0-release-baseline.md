# FruitDefense P0 release baseline

Recorded on 2026-07-15 for branch `codex/p0-p1-foundation` with Unity `6000.3.19f1`.

## Runtime shape

- Enabled scene order: `Bootstrap`, `Lobby`, `Battle`, `Settlement`.
- `AppBootstrap` is the only persistent composition root; battle hosts are explicitly initialized and disposable.
- Default level: `orchard-01`; a battle pins its session ID, seed, and content version through settlement or retry.
- Fixed simulation: 20 Hz, five-step catch-up ceiling, serializable deterministic random state, no background catch-up.
- Runtime content: finite trigger/target/effect skill composition compiled from the bundled versioned catalog.
- Persistence: local `PlayerProfileEnvelopeV1` plus deterministic `BattleSnapshotV1`; P0 does not auto-resume a battle.

## Versioned artifacts

- Catalog ID/version: `fruit-defense.bundled@1.0.0`.
- Catalog JSON: 84,639 bytes; SHA-256 `882B00C321CC360111B44C93895C46F881D53AE5A96F45F8B282963BCF053B7C`.
- UI font subset: 253,676 bytes; SHA-256 `63311F52E597C92D20B8DB7405EB35F48546D0B0811007A6188C287634A195FF`.
- WebGL content version: `731abd2faa66`.
- WebGL output: 8,067,407 bytes; Brotli data payload: 4,383,934 bytes.

## Release gates

- Unified Unity gate: `FruitDefense.Editor.P0ValidationSuite.Run` -> `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.
- WebGL build: `FruitDefense.Editor.WebBuild.Build` -> `FRUIT_DEFENSE_WEB_BUILD_OK`.
- Full player flow: `scripts/accept-webgl-portrait.ps1 -ServeLocal -Flow` -> `FRUIT_DEFENSE_FLOW_ACCEPTANCE_OK`.
- Existing battle acceptance: `scripts/accept-webgl-portrait.ps1 -ServeLocal` -> `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`.
- Accepted full-flow manifest: `Logs/flow-acceptance/20260715-024038/flow-acceptance.json`.
- Accepted 13-state manifest: `Logs/visual-acceptance/20260715-024114/acceptance.json`.

## Deferred to P1

- Automatic local/cloud battle resume, remote content catalogs/bundles, immutable release channels, login, cloud profile, and code-package updates.
- Douyin device/SDK integration remains gated by the compatibility spike. The committed spike status is Yellow until TTSDK, developer tools, App ID, simulator, Android, and iOS device evidence are available; mini-game adapters must remain explicitly unavailable rather than masquerading as Web.

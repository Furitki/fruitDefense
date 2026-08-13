---
id: p0-release-baseline
parent: design-kb-home
order: 20
status: active
---

# FruitDefense P0 release baseline

Recorded on 2026-08-14 for the deployed `main` revision
`e9f91d3ab21031a3957aa738ec707099ae537105` with Unity `6000.3.19f1`.

## Runtime shape

- Enabled scene order: `Bootstrap`, `Lobby`, `Battle`, `Settlement`.
- `AppBootstrap` is the only persistent composition root; battle hosts are explicitly initialized and disposable.
- Default level: `orchard-01`; a battle pins its session ID, seed, and content version through settlement or retry.
- Fixed simulation: 20 Hz, five-step catch-up ceiling, serializable deterministic random state, no background catch-up.
- Runtime content: finite trigger/target/effect skill composition compiled from the bundled versioned catalog.
- Persistence: local `PlayerProfileEnvelopeV1` plus deterministic `BattleSnapshotV1`; P0 does not auto-resume a battle.

## Versioned artifacts

- Catalog ID/version: `catalog.bundled.orchard@1.0.0`.
- WebGL payload versions: loader `c222dba000b7`, data `4774cd1e5a6d`,
  framework `ecf670072498`, and Wasm `6f50b3bba3ee`.
- WebGL output: 8,772,874 bytes; entry SHA-256
  `3A4305C70F313C8E91D15F6C27E6FF8F7FC11DA78FE141317FA00F4ADDDEFBD4`.
- The data payload is 4,712,677 bytes.
- Ordinary WebGL public URL: `http://175.178.80.66:3000/`.

## Release gates

- Unified Unity gate: `FruitDefense.Editor.P0ValidationSuite.Run` -> `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.
- Repeated basic-attack regression passed for pea, watermelon, banana, and durian using real fixed ticks and damage.
- WebGL build: `FruitDefense.Editor.WebBuild.Build` -> `FRUIT_DEFENSE_WEB_BUILD_OK`;
  two clean WebGL-targeted builds produced identical full SHA-256 digests and
  byte lengths for all four payloads.
- Local portrait acceptance: `scripts/accept-webgl-portrait.ps1 -ServeLocal` -> `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`.
- Online publication: `scripts/publish-online.ps1 -Execute` -> `FRUIT_DEFENSE_ONLINE_PUBLISH_OK`; remote entry health and WebGL delivery headers passed.
- Accepted local manifest: `Logs/visual-acceptance/20260814-000120/acceptance.json`.
- Accepted deployed manifest:
  `Logs/visual-acceptance-transition/20260814-000120-ccc72256/candidate/acceptance.json`.
- The deployed release-N to release-N+1 transition changed loader, data, framework,
  and Wasm. It expected 8,753,071 payload bytes and observed 8,754,271 transferred
  bytes including response overhead. The candidate's subsequent same-release warm
  reload transferred 0 payload bytes.
- Strong content ETags, independent payload versions, immutable matching-version
  caching, exact remote header checks, and HTML revalidation all passed.
- Schema-3 publication manifest: `Builds/Pipeline/online-publish-manifest.json`.
- This baseline authorizes ordinary WebGL only; it is not Douyin or WeChat release evidence.

## Deferred to P1

- Automatic local/cloud battle resume, remote content catalogs/bundles, immutable release channels, login, cloud profile, and code-package updates.
- Douyin device/SDK integration remains gated by the compatibility spike. The committed spike status is Yellow until TTSDK, developer tools, App ID, simulator, Android, and iOS device evidence are available; mini-game adapters must remain explicitly unavailable rather than masquerading as Web.

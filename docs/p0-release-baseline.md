---
id: p0-release-baseline
parent: design-kb-home
order: 20
status: active
---

# FruitDefense P0 release baseline

Recorded on 2026-08-13 for the deployed `main` revision
`cd946b7f58304e93842cf108a3cf92e31da9b6e0` with Unity `6000.3.19f1`.

## Runtime shape

- Enabled scene order: `Bootstrap`, `Lobby`, `Battle`, `Settlement`.
- `AppBootstrap` is the only persistent composition root; battle hosts are explicitly initialized and disposable.
- Default level: `orchard-01`; a battle pins its session ID, seed, and content version through settlement or retry.
- Fixed simulation: 20 Hz, five-step catch-up ceiling, serializable deterministic random state, no background catch-up.
- Runtime content: finite trigger/target/effect skill composition compiled from the bundled versioned catalog.
- Persistence: local `PlayerProfileEnvelopeV1` plus deterministic `BattleSnapshotV1`; P0 does not auto-resume a battle.

## Versioned artifacts

- Catalog ID/version: `catalog.bundled.orchard@1.0.0`.
- WebGL payload versions: loader `69214ab44707`, data `22d99aec373f`,
  framework `713319cb0c52`, and Wasm `7b18f089ae68`.
- WebGL output: 8,753,851 bytes; entry SHA-256
  `EB9DD988C8633C9FBFB234C37727FB63CBF48D55032BA158E8191141F99DD306`.
- The data payload is 4,711,509 bytes. Moving editor-only HD terrain bake inputs
  out of runtime `Resources` reduced the total artifact and data payload by
  8,904,421 bytes from the previous deployed baseline.
- Ordinary WebGL public URL: `http://175.178.80.66:3000/`.

## Release gates

- Unified Unity gate: `FruitDefense.Editor.P0ValidationSuite.Run` -> `FRUIT_DEFENSE_P0_RELEASE_GATE_OK`.
- Repeated basic-attack regression passed for pea, watermelon, banana, and durian using real fixed ticks and damage.
- WebGL build: `FruitDefense.Editor.WebBuild.Build` -> `FRUIT_DEFENSE_WEB_BUILD_OK`;
  two clean WebGL-targeted builds produced identical full SHA-256 digests and
  byte lengths for all four payloads.
- Local portrait acceptance: `scripts/accept-webgl-portrait.ps1 -ServeLocal` -> `FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`.
- Online publication: `scripts/publish-online.ps1 -Execute` -> `FRUIT_DEFENSE_ONLINE_PUBLISH_OK`; remote entry health and WebGL delivery headers passed.
- Accepted local manifest: `Logs/visual-acceptance/20260813-215309/acceptance.json`.
- Accepted deployed manifest:
  `Logs/visual-acceptance-transition/20260813-215308-653d81c1/candidate/acceptance.json`.
- The deployed release-N to release-N+1 transition reused loader, data, framework,
  and Wasm with 0 expected and 0 observed payload bytes. The candidate's subsequent
  same-release warm reload also transferred 0 payload bytes.
- Strong content ETags, independent payload versions, immutable matching-version
  caching, exact remote header checks, and HTML revalidation all passed.
- Schema-3 publication manifest: `Builds/Pipeline/online-publish-manifest.json`.
- This baseline authorizes ordinary WebGL only; it is not Douyin or WeChat release evidence.

## Deferred to P1

- Automatic local/cloud battle resume, remote content catalogs/bundles, immutable release channels, login, cloud profile, and code-package updates.
- Douyin device/SDK integration remains gated by the compatibility spike. The committed spike status is Yellow until TTSDK, developer tools, App ID, simulator, Android, and iOS device evidence are available; mini-game adapters must remain explicitly unavailable rather than masquerading as Web.

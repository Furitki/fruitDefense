## Context

The project builds with Unity 6000.3.19f1 and targets WebGL, but this worktree has no WXSDK/conversion package, WeChat Developer Tools, AppID, converted mini-game project, or Android/iOS run evidence. The official `wechat-miniprogram/minigame-tuanjie-transform-sdk` repository is the current source of the Unity/Tuanjie adapter. On 2026-07-15 its `main` branch resolved to commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`; its changelog identifies `v0.1.33` dated 2026-06-22 while its package manifest still declares `0.1.1`, so an exact source commit is more reliable than the embedded semantic version. The official README requires the Stable WeChat Developer Tools channel rather than the Minigame Build channel.

## Goals / Non-Goals

**Goals:**

- Make platform-readiness evidence reproducible and safe on developer and CI machines.
- Separate desktop preflight, conversion, simulator, Android, and iOS evidence.
- Define objective Green, Yellow, and Red states and prevent premature WeChat SDK integration.
- Capture exact Unity, SDK commit, conversion plugin, Developer Tools, client/base-library, host, and device versions for later changes.

**Non-Goals:**

- Installing WXSDK or Developer Tools without an explicitly reviewed version.
- Logging in, creating/using an AppID, uploading a build, or changing a release.
- Adding login, payment, ads, sharing, cloud save, production update code, or other platform APIs.
- Changing gameplay, ProjectSettings, Build Settings, or treating package splitting as runtime code hot replacement.

## Decisions

1. **The compatibility report is the gate, not SDK file presence.** A row becomes Green only when a command, artifact, simulator run, or physical-device run is recorded. Missing account or device access is Yellow; a reproducible incompatibility is Red.
2. **Pin the adapter by immutable Git commit after a successful isolated compile/conversion.** The official repository's changelog and package manifest currently expose different versions. Recording only `v0.1.33` or `0.1.1` would not identify reproducible source, so the eventual lock includes the commit SHA plus both observed metadata versions.
3. **Use Stable WeChat Developer Tools and record its exact installed version.** The SDK README explicitly warns against the Minigame Build edition. No uninstalled tool version is guessed or marked pinned.
4. **Keep platform mechanisms separate.** `wx.getUpdateManager`/`UpdateManager.applyUpdate` applies reviewed code packages through restart. WXAssetBundle/UnityWebRequest/Addressables deliver content already supported by shipped code. Ordinary resource/code subpackages and Wasm splitting optimize loading and package layout, not in-process code replacement.
5. **Do not hard-code an unverified current package-policy limit.** The preflight records actual artifact sizes. Numeric policy limits are nullable until an official policy page and the selected base-library/toolchain are pinned; the SDK changelog's 30 MB first-resource-package note is recorded as historical capability evidence rather than assumed universal code-package policy.
6. **No silent adapter fallback.** Until all release-blocking rows are Green, `WeChatMiniGame` remains unavailable. Editor and Web are explicit separate platform choices.
7. **Secrets never enter reports.** The preflight records only boolean presence flags for AppID, developer session, and upload key. Paths are reduced to installation categories and no environment value is serialized.
8. **Both Android and iOS are required for the release gate.** Each run covers cold/warm start, touch, audio, hide/show, HTTPS, cache, update callbacks, one battle, and 30 minutes of repeated play.
9. **This change is the long-lived WeChat readiness record.** It remains active while a release-blocking row is Yellow or Red or the Douyin-first release dependency is unmet. It is refreshed when the Unity baseline, candidate WXSDK/tooling, official platform requirements, Douyin release state, account access, or device availability changes; age and ordinary WebGL success do not close it.

## Risks / Trade-offs

- **[Unity 6000.3 may still be outside the adapter's proven production matrix]** -> Keep the gate Yellow until the exact commit compiles, converts, and runs; create an engine/toolchain proposal if a clean retry is Red.
- **[The official SDK publishes mutable `main` with inconsistent version metadata]** -> Store the observed full commit SHA and do not install an unpinned branch reference in production.
- **[Developer Tools, AppID, and phones require interactive authorization]** -> Complete desktop evidence now and leave account/simulator/device rows explicitly unchecked.
- **[Simulator and WebRTC preview do not prove physical-device audio, memory, or lifecycle]** -> Require separate Android and iOS evidence before Green.
- **[Official package limits and base-library behavior can change]** -> Record retrieval timestamps and selected base-library/Developer Tools versions; release CI consumes the pinned report rather than recollection.
- **[Remote bundle caching can hide fallback failures]** -> Device tests exercise a cold cache, warm cache, corrupt/unavailable target, and bundled fallback.

## Migration Plan

1. Run the non-destructive desktop preflight and save the initial Yellow report.
2. In an isolated SDK branch, review and pin one official adapter commit and one Stable Developer Tools version.
3. Export/convert Unity 6000.3.19f1, then run simulator checks with an authorized AppID.
4. Complete Android and iOS matrices and attach tool/client/base-library/device metadata.
5. Mark Green only when all release-blocking rows are evidenced, then re-run the current readiness script and clean-checkout conversion against the latest recorded baseline.
6. Authorize `add-wechat-runtime-adapter` only after the Douyin-first release path is stable, then archive this tracker after the handoff is recorded.
7. Rollback any failed experiment by deleting the isolated SDK branch/worktree; the core runtime remains untouched.

## Open Questions

- The project owner must supply or authorize a WeChat Mini Game AppID and Developer Tools login for conversion preview and device testing.
- The exact SDK commit, Stable Developer Tools version, WeChat client versions, and base-library version remain unpinned until the isolated compile/conversion run is completed.
- Current code-package size policy must be captured from the authenticated Developer Tools/publishing surface or an accessible official policy page before it becomes a CI gate.

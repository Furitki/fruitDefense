# WeChat Unity 6.3 spike status

Generated from `scripts/check-wechat-readiness.ps1` on 2026-07-15.

## Decision

**Overall gate: Yellow.** FruitDefense's ordinary Unity/WebGL baseline is healthy, but WeChat conversion and release compatibility are not yet proven. `WeChatMiniGame` must remain explicitly unavailable. This report does not authorize `add-wechat-runtime-adapter`, and the Web adapter must not silently stand in for WeChat.

The later WeChat adapter remains behind both conditions from the plan: this gate must become Green, and the Douyin-first release path must already be stable.

## Status rules

- **Green:** the required command, immutable artifact, simulator run, or physical-device run exists with exact environment/version evidence.
- **Yellow:** a prerequisite or manual platform run is missing, or a tool is present but not pinned and proven. Yellow does not authorize integration.
- **Red:** the pinned toolchain reproduces an incompatibility on a clean retry, or a required baseline cannot build. Red requires a separate engine/toolchain proposal rather than a disguised Web fallback.

## Current matrix

| Area | Status | Current evidence | Green requirement |
|---|---|---|---|
| Unity editor | Green | Unity `6000.3.19f1` installed | Keep the exact baseline pinned |
| WebGL module | Green | Matching WebGL Build Support installed | Keep it on developer/CI machines |
| Node tooling | Green, non-blocking | Node `v24.14.0` found | Pin only if selected tooling requires Node |
| Unity smoke | Green | `FRUIT_DEFENSE_SMOKE_OK` | Re-run after platform integration |
| Ordinary WebGL export | Green | `Builds/WebGL/index.html`, 8,066,271 total bytes | Rebuild before official conversion |
| Official WXSDK/converter | Yellow | Not installed; official candidate `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228` observed | Review, lock, compile, and convert the exact commit |
| SDK version metadata | Yellow | Changelog `0.1.33`; package manifest `0.1.1` | Use immutable tested commit plus both observed versions |
| Stable Developer Tools | Yellow | Not found | Install Stable edition and record exact version |
| AppID/developer session | Yellow | Presence flags false | Authorized local credentials; never commit values |
| Converted mini-game | Yellow | No converted `game.js` project | Produce with pinned SDK/tools and statically validate |
| Simulator | Yellow | No run evidence | Launch, touch, audio, lifecycle, HTTPS/cache, update, content, subpackage, Wasm checks |
| Android device | Yellow | No physical-device evidence | Cold/warm start, one battle, lifecycle/update/cache, 30 minutes |
| iOS device | Yellow | No physical-device evidence | Cold/warm start, one battle, lifecycle/update/cache, 30 minutes |
| Code-package update | Yellow | No `wx.getUpdateManager`/`applyUpdate` evidence | Prove callbacks and restart application outside battle |
| Remote content/cache | Yellow | No WXAssetBundle/Addressables/UnityWebRequest matrix | Prove cold/warm cache and target -> last-good -> bundled fallback |
| Ordinary subpackages | Yellow | No `wx.loadSubpackage` evidence | Prove package layout/loading separately from content hot update |
| Wasm splitting | Yellow | No function/startup evidence | Cover Bootstrap, Lobby, first battle, lifecycle, update UI on both OS families |
| Stability | Yellow | No device soak | Android and iOS 30-minute runs without crash/OOM |

## Version evidence

- Project: Unity `6000.3.19f1` revision `7689f4515d75`.
- Official SDK repository: `wechat-miniprogram/minigame-tuanjie-transform-sdk`.
- Official `main` observed on 2026-07-15: `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`.
- Official metadata mismatch: changelog `v0.1.33` (2026-06-22), package manifest `0.1.1`.
- Developer Tools, WeChat client, base library, host baseline: not installed/recorded, therefore unpinned.

The official commit is a reviewed candidate, not a claimed compatible pin. Task 3.1 remains open until that exact or another reviewed official commit compiles and converts Unity `6000.3.19f1` with an exact Stable Developer Tools version.

## Required unblock sequence

1. Authorize an isolated install/review of the official WXSDK commit and a Stable WeChat Developer Tools release.
2. Provide authorized AppID and Developer Tools login/session locally; keep credentials outside Git.
3. Compile and convert the ordinary WebGL baseline, record immutable SDK/tool/base-library versions, and statically validate the converted project.
4. Complete the simulator functional matrix.
5. Complete Android and iOS functional and 30-minute stability matrices.
6. Validate WXAssetBundle or Addressables plus UnityWebRequest fallback, ordinary subpackages, and Wasm splitting as separate mechanisms.
7. Re-run `scripts/check-wechat-readiness.ps1 -RequireGreen`. Only a zero exit after every blocking row has evidence can authorize later adapter work.

## Explicit boundary

No gameplay, ProjectSettings, Build Settings, SDK dependency, account state, upload, or production release was changed by this spike. Ordinary WebGL success is not recorded as WeChat conversion success, preview is not accepted as physical-device evidence, and package/Wasm splitting is not treated as runtime code hot replacement.

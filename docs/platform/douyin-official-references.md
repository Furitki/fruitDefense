# Douyin Unity mini-game official references

Retrieved on 2026-07-15. Recheck these sources before changing the pinned platform toolchain.

| Mechanism | Official source | Project decision |
|---|---|---|
| Unity WebGL conversion | [WebGL adaptation overview](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/open-capacity/overview-and-compatibility/sc_webgl_overview) | Keep gameplay code platform-neutral; the SDK and conversion glue stay in a platform assembly. WebGL does not support general managed threading. |
| Unity WebGL integration | [WebGL adaptation solution](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/open-capacity/sc_webgl_overall) | Use the official TTSDK/Stark Unity toolchain only after this spike is Green. |
| Remote AssetBundles | [TTAssetBundle memory optimization](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/performance-optimization-both/unity/startup/sc-webgl-tt-ab) | Prefer TTAssetBundle with TTSDK 5.52.10 or newer and Unity 2021.3 or newer; retain the documented UnityWebRequest fallback. |
| Code-package update | [UpdateManager](https://developer.open-douyin.com/docs/resource/zh-CN/mini-app/develop/api/foundation/update/update-manager/update-manager) | Code updates are downloaded by the host and applied through restart; no in-process C# hot replacement. |
| Ordinary subpackages | [Code packages and subpackages](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/basic-function/subpackages/introduction) | Treat subpackages as startup/download optimization. Baseline limits observed: 4 MB main package, 20 MB total package, 20 MB single subpackage unless separately entitled. |
| Wasm splitting | [Wasm splitting tool](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/performance-optimization-both/unity/startup/sc_webgl_split) | Collect functions on Android and iOS across Bootstrap, Lobby, the complete first battle, lifecycle, and update UI; splitting is not hot update. |
| Developer tools | [Developer-tool downloads and changelog](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/dev-tools/developer-instrument-update-and-download) | Version 4.5.2 was the latest listed stable tool at retrieval time; pin only after it passes Unity 6000.3.19f1 conversion. |
| WebGL networking | [Network adaptation](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/open-capacity/capability-adaptation/sc_webgl_network) | Runtime HTTP uses `UnityWebRequest` over approved HTTPS domains; no raw sockets in the core runtime. |

## Status rules

- **Green:** a repeatable command, generated artifact, simulator record, or physical-device record proves the row on the pinned versions.
- **Yellow:** the capability is plausible or documented but required tools, credentials, conversion, simulator, or device evidence is missing.
- **Red:** the pinned toolchain fails the same required check on a clean retry or an official constraint makes the planned approach unavailable.

The overall gate is Green only when every blocking row is Green. A Yellow or Red gate keeps `DouyinMiniGame` explicitly unavailable.

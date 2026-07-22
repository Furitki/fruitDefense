---
id: douyin-official-references
parent: douyin-spike-status
order: 10
status: active
---

# Douyin Unity mini-game official references

Retrieved on 2026-07-15. Recheck these sources before changing the pinned platform toolchain.

| Mechanism | Official source | Project decision |
|---|---|---|
| Unity WebGL conversion | [WebGL adaptation overview](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/open-capacity/overview-and-compatibility/sc_webgl_overview) | Keep gameplay code platform-neutral; the SDK and conversion glue stay in a platform assembly. WebGL does not support general managed threading. |
| Unity WebGL integration | [WebGL adaptation solution](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/open-capacity/sc_webgl_overall) | Use the official TTSDK/Stark Unity toolchain only after this spike is Green. |
| Remote AssetBundles | [TTAssetBundle memory optimization](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/performance-optimization-both/unity/startup/sc-webgl-tt-ab) | Minimums are TTSDK 5.52.10, Unity 2021.3, and Douyin/Douyin Lite 29.2.0. TTSDK below 6.2.0 also requires StarkSDK Unity Tools 3.31.2 or newer. Retain the documented unsupported-host fallback and test its actual memory behavior. |
| Code-package update | [UpdateManager](https://developer.open-douyin.com/docs/resource/zh-CN/mini-app/develop/api/foundation/update/update-manager/update-manager) | Code updates are downloaded by the host and applied through restart; no in-process C# hot replacement. |
| Ordinary subpackages | [Code packages and subpackages](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/basic-function/subpackages/introduction) | Treat subpackages as startup/download optimization. Baseline limits observed: 4 MB main package, 20 MB total package, 20 MB single subpackage unless separately entitled. |
| Wasm splitting | [Wasm splitting tool](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/performance-optimization-both/unity/startup/sc_webgl_split) | IDE 4.3.2 or newer is required. Collect functions on Android first and then iOS across Bootstrap, Lobby, the complete first battle, lifecycle, and update UI; splitting requires AppID-backed upload/preview and is not hot update. |
| Developer tools | [Developer-tool downloads and changelog](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/dev-tools/developer-instrument-update-and-download) | Version 4.5.2 (2026-03-24) was the latest listed stable tool at retrieval time and carries default base library 4.4/latest 4.6. Treat it as a candidate only; pin it after Unity 6000.3.19f1 conversion succeeds twice. |
| WebGL networking | [Network adaptation](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/open-capacity/capability-adaptation/sc_webgl_network) | Runtime HTTP uses `UnityWebRequest` over approved HTTPS domains; no raw sockets in the core runtime. |

## Status rules

- **Green:** a repeatable command, generated artifact, simulator record, or physical-device record proves the row on the pinned versions.
- **Yellow:** the capability is plausible or documented but required tools, credentials, conversion, simulator, or device evidence is missing.
- **Red:** the pinned toolchain fails the same required check on a clean retry or an official constraint makes the planned approach unavailable.

The overall gate is Green only when every blocking row is Green. A Yellow or Red gate keeps `DouyinMiniGame` explicitly unavailable.

## Pinning boundary

The public minimum-version statements do not prove that Unity `6000.3.19f1` works with a particular TTSDK. `douyin-toolchain-pin-template.json` therefore keeps TTSDK and converter versions null until a compile, WebGL export, conversion, and clean retry produce hashed evidence. The template also separates the editor, SDK, Unity tools, IDE, base library, Addressables, Node, converter, Wasm CLI, and Android/iOS host versions so later investigations do not collapse independent compatibility variables into one label.

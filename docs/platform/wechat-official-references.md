# WeChat Mini Game Unity 6 compatibility references

Retrieved and checked on 2026-07-15. Only WeChat official documentation and repositories owned by `wechat-miniprogram` are used as platform evidence.

## Adapter and conversion toolchain

- Official SDK repository: <https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk>
- Immutable `main` observation: `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228` from `git ls-remote` on 2026-07-15.
- Official package manifest at that commit: <https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk/blob/ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228/package.json>
- Official changelog at that commit: <https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk/blob/ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228/CHANGELOG.md>
- Official installation/readme at that commit: <https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk/blob/ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228/README.md>

The observed changelog starts with `v0.1.33` dated 2026-06-22, while `package.json` still declares `0.1.1`. The SDK is therefore not considered reproducibly pinned by either semantic version alone. A later isolated integration must lock the exact successfully tested Git commit.

The changelog records Unity 6 support as a preview when introduced and contains subsequent adapter work, but it does not establish that FruitDefense's exact Unity `6000.3.19f1` build is production-compatible. Compile, conversion, simulator, and device evidence remain mandatory.

## Developer Tools and preview

- Official WeChat Developer Tools download: <https://developers.weixin.qq.com/miniprogram/dev/devtools/download.html>
- The official SDK README instructs users to install the **Stable** Developer Tools channel and explicitly says not to use the Minigame Build edition for this adapter workflow.
- Official WeChat preview plugin repository: <https://github.com/wechat-miniprogram/minigame-unity-wechat-preview>

The preview plugin can shorten API/input/audio feedback loops, but it is a WebRTC preview path and is not accepted as conversion or physical-device stability evidence.

## Lifecycle and code-package update

- `wx.onShow`: <https://developers.weixin.qq.com/minigame/dev/api/base/app/life-cycle/wx.onShow.html>
- `wx.onHide`: <https://developers.weixin.qq.com/minigame/dev/api/base/app/life-cycle/wx.onHide.html>
- `wx.getUpdateManager`: <https://developers.weixin.qq.com/minigame/dev/api/base/update/wx.getUpdateManager.html>
- `UpdateManager.applyUpdate`: <https://developers.weixin.qq.com/minigame/dev/api/base/update/UpdateManager.applyUpdate.html>

The update manager is treated as a reviewed code-package mechanism whose downloaded update is applied through restart. It is not an in-process DLL, Lua, or arbitrary-code hot replacement mechanism.

## Content, cache, subpackages, and Wasm

- `wx.loadSubpackage`: <https://developers.weixin.qq.com/minigame/dev/api/base/subpackage/wx.loadSubpackage.html>
- High-performance-plus mode: <https://developers.weixin.qq.com/minigame/dev/guide/performance/perf-high-performance-plus.html>
- The official SDK changelog records WXAssetBundle, browser fallback to UnityWebRequest, CDN switching, cache controls, predownload headers, first-resource-package/subpackage behavior, and multiple generations of Wasm splitting support.

These are separate mechanisms:

- Remote catalog/AssetBundle/Addressables content may change only within types already supported by the shipped code.
- `UpdateManager` applies code packages through restart.
- Ordinary subpackages and Wasm splitting improve package layout/startup and are not the project's content hot-update protocol.

The changelog notes a 30 MB first-resource-package total when placed in a mini-game subpackage (2024-12-18), but this spike does not infer current global code-package limits from that note. Current main/total/single-subpackage limits remain nullable until verified against the selected Developer Tools/base-library/publishing policy.

## Required evidence before Green

The gate requires exact version metadata and logs for:

1. Unity WebGL export and official SDK conversion.
2. Stable Developer Tools simulator launch, touch, audio, lifecycle, HTTPS/cache, update callbacks, remote content, subpackages, and Wasm splitting.
3. Android and iOS cold/warm launch, the same functional checks, a complete battle, and 30 minutes of repeated play.
4. Package sizes, memory/crash/OOM observations, and fallback behavior for unavailable or invalid remote content.

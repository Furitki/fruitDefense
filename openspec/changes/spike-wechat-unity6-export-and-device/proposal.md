## Why

The project reserves a WeChat Mini Game target after the Douyin-first release, but the current Unity 6.3 WebGL project has no pinned WXSDK/conversion commit, WeChat Developer Tools installation, AppID, converted project, or physical-device evidence. A compatibility spike must establish a reproducible support decision before any WeChat SDK is merged into the runtime.

## What Changes

- Record the official WeChat Unity/Tuanjie conversion SDK, Unity 6 status, Developer Tools channel, UpdateManager, package-splitting, remote-content, cache, and lifecycle requirements used by this project.
- Add a repeatable desktop preflight that reports installed Unity, WebGL support, Node, WXSDK/conversion plugin, WeChat Developer Tools, AppID presence, generated artifacts, and package budgets without exposing secrets.
- Produce a Green/Yellow/Red compatibility matrix for Unity export/conversion, simulator, Android/iOS device, touch, audio, lifecycle, HTTPS/cache, code update, remote content, package splitting, and 30-minute stability.
- Keep the WeChat runtime adapter unavailable until every release-blocking row is Green; never silently fall back to the Web adapter.
- Do not install a large SDK, log in, upload, change gameplay, or add production platform APIs in this spike.

## Capabilities

### New Capabilities

- `wechat-unity-compatibility-gate`: Defines the evidence, status rules, and repeatable checks required before WeChat SDK integration.

### Modified Capabilities

None.

## Impact

- Adds OpenSpec evidence, a non-secret compatibility report, and local preflight tooling under the project scripts.
- Reads Unity/package/project configuration and generated builds but does not modify gameplay, presentation, ProjectSettings, Build Settings, or platform SDK dependencies.
- Establishes the entry gate for `add-wechat-runtime-adapter`, `add-wechat-code-package-update-flow`, and later WeChat packaging optimization.

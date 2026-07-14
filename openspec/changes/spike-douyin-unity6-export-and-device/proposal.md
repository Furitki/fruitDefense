## Why

The project targets Douyin Mini Game first, but the current Unity 6.3 WebGL project has no pinned TTSDK, conversion tool, developer-tool project, AppID, or device evidence. A compatibility spike must establish a reproducible support decision before any platform SDK is merged into the runtime.

## What Changes

- Record the official Douyin Unity WebGL, TTAssetBundle, Addressables, UpdateManager, code-package, and Wasm-splitting requirements used by this project.
- Add a repeatable desktop preflight that reports installed Unity, WebGL support, Node, TTSDK, Douyin developer tools, credentials, build artifacts, and package budgets without exposing secrets.
- Produce a Green/Yellow/Red compatibility report for Unity export, simulator, Android/iOS device, lifecycle, input, audio, HTTPS/cache, code update, content delivery, and 30-minute stability.
- Keep the Douyin runtime adapter unavailable until every release-blocking row is Green; never silently fall back to the Web adapter.
- Do not add login, payment, ads, sharing, cloud save, or production SDK code in this spike.

## Capabilities

### New Capabilities

- `douyin-unity-compatibility-gate`: Defines the evidence, status rules, and repeatable checks required before Douyin SDK integration.

### Modified Capabilities

None.

## Impact

- Adds OpenSpec evidence, a non-secret compatibility report, and local preflight tooling under the project scripts.
- Reads Unity/Package/project configuration and generated builds but does not modify gameplay, presentation, scenes, or platform SDK dependencies.
- Establishes the entry gate for `add-douyin-runtime-adapter` and subsequent Douyin release work.

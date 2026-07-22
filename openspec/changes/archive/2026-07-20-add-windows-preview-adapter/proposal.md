## Why

The current Windows player builds successfully but cannot initialize the application because the platform factory rejects every non-Editor, non-WebGL host. A dedicated local desktop-preview adapter is needed so developers can launch and inspect the current player flow from a PC build without changing the project's WebGL release baseline or mini-game readiness claims.

## What Changes

- Add an explicit Windows desktop-preview platform identity and adapter that initializes through the existing platform-neutral contract.
- Select the desktop-preview adapter only for Windows standalone players.
- Extend deterministic platform validation to prove desktop preview initializes successfully while Douyin and WeChat remain unavailable and never fall back to Web behavior.
- Rebuild and launch-check the Windows player through `Bootstrap → Lobby → Battle → Settlement` entry initialization.
- Keep gameplay, persistence, content, WebGL behavior, and all mini-game adapter availability unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-platform-boundary`: Extend current-host selection with an explicit Windows desktop-preview adapter while preserving WebGL and unavailable mini-game boundaries.

## Impact

- Affected runtime: `Assets/Scripts/Platform/PlatformRuntime.cs`.
- Affected validation: `Assets/Scripts/App/AppFrameworkValidation.cs` and the unified P0 validation suite that invokes it.
- Affected artifact: `Builds/Windows/FruitDefense.exe` and its data directory.
- No new SDK or package dependency; no gameplay, persistence, backend, release-platform, or game-design change.

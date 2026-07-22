## Context

`PlatformAdapterFactory.CreateCurrent()` currently selects Editor, WebGL, or explicitly unavailable mini-game adapters, then throws for a Windows standalone player. The build pipeline therefore produces a valid executable whose `AppBootstrap` immediately records `platform-adapter-creation-failed`, leaving the player flow uninitialized. Windows is needed only as a local preview and diagnostic host; WebGL remains the shared release and acceptance baseline.

## Goals / Non-Goals

**Goals:**

- Give Windows standalone builds an explicit, successful platform identity through the existing adapter contract.
- Preserve platform-neutral application initialization and visibility forwarding.
- Prove the adapter through deterministic editor validation and a real Windows-player launch check.
- Keep Windows preview distinct from WebGL and from every mini-game host.

**Non-Goals:**

- Promote Windows to a release platform or change platform order.
- Change gameplay, content, persistence, economy, scenes, UI, or WebGL behavior.
- Implement or relax Douyin/WeChat adapters, SDK gates, or release readiness.
- Add a desktop-specific launch URL, native integration, installer, updater, or save location.

## Decisions

### Add a dedicated Windows preview identity

Add `PlatformId.WindowsPreview` and a `WindowsPreviewPlatformAdapter` whose launch context is empty and whose initialization succeeds exactly once. Reusing `EditorPlatformAdapter` was rejected because a built player is not the Editor. Reusing `WebPlatformAdapter` was rejected because it would misreport the host and weaken the project's no-fallback platform boundary.

### Select preview only for Windows standalone

`CreateCurrent()` will select the new adapter under `UNITY_STANDALONE_WIN`, after the explicit mini-game symbols and before the unsupported-host branch. Other standalone targets remain rejected rather than gaining accidental support.

### Extend the existing deterministic validation surface

`AppFrameworkValidation.ValidateAdapters()` will create and initialize the Windows preview adapter explicitly, assert its identity and successful completion, and assert that it is not a Web adapter. Existing Douyin and WeChat unavailable-slot assertions remain unchanged. The unified P0 validation suite already invokes this surface.

### Treat a real player launch as the runtime acceptance surface

After a successful Windows build, start the player in a controlled window, allow application initialization to run, and inspect the player log for platform exceptions or initialization failure. The executable remaining alive through the observation window plus a clean initialization log proves the compiler-symbol selection used by the actual player, which editor-only validation cannot exercise.

## Risks / Trade-offs

- [Windows preview could be mistaken for a release target] -> Keep the identity and OpenSpec wording explicitly preview-only; do not add it to release, platform-readiness, or design-direction documents.
- [A generic standalone branch could silently authorize macOS or Linux] -> Compile the selection only under `UNITY_STANDALONE_WIN`.
- [Mini-game builds could inherit desktop behavior] -> Keep explicit mini-game symbols ahead of Windows selection and preserve unavailable adapters with their own identities.
- [Editor tests cannot exercise Windows compile symbols] -> Pair explicit factory validation with a real Windows build and player launch log.

## Migration Plan

1. Add the preview identity, adapter, and Windows-only factory branch.
2. Extend app-framework validation and run strict OpenSpec plus unified P0 validation.
3. Rebuild `Builds/Windows/FruitDefense.exe` with Unity `6000.3.19f1`.
4. Launch-check the player and retain the new build only if initialization is clean; rollback is the scoped runtime/validation patch plus generated build artifacts.

## Open Questions

None. Windows preview remains a local engineering surface and does not alter product-direction documentation.

## Context

The project builds successfully with Unity 6000.3.19f1 and has a passing WebGL portrait acceptance suite. It currently has no TTSDK package, Douyin developer-tool installation, AppID, upload credentials, converted mini-game project, or Android/iOS device evidence. Official Douyin documentation supports Unity WebGL conversion, TTAssetBundle, Addressables integration, code-package UpdateManager, ordinary subpackages, and Wasm splitting, but those capabilities have version and host constraints that must be pinned and tested.

## Goals / Non-Goals

**Goals:**

- Make platform-readiness evidence reproducible and safe to run on developer and CI machines.
- Separate desktop preflight, simulator evidence, Android evidence, and iOS evidence.
- Define objective Green, Yellow, and Red states and prevent premature SDK integration.
- Capture the exact SDK, developer-tool, host, Unity, and content-provider versions used by later platform changes.

**Non-Goals:**

- Installing TTSDK or developer tools without an explicit reviewed version.
- Logging in, creating an AppID, uploading a build, or changing a production release.
- Adding platform business APIs or changing gameplay/UI.
- Treating code subpackages or Wasm splitting as runtime code hot replacement.

## Decisions

1. **The compatibility report is the gate, not the presence of SDK files.** A row is Green only when its command, artifact, or device evidence is recorded. Missing credentials or physical-device coverage is Yellow, not an assumed pass.
2. **Official platform mechanisms remain distinct.** UpdateManager handles reviewed code packages and restarts; Addressables/AssetBundle delivery handles content already supported by shipped code; ordinary/Wasm splitting only improves startup.
3. **TTAssetBundle is preferred for Douyin remote bundles when the pinned TTSDK and host meet official requirements.** Unsupported environments use the official UnityWebRequest fallback behind the same content interface.
4. **No silent adapter fallback.** Until all release-blocking rows are Green, `DouyinMiniGame` is unavailable. Editor and Web remain explicit platform choices.
5. **Secrets never enter reports.** The preflight records only presence, version, path category, hashes, and non-secret AppID availability flags.
6. **The first device matrix covers both Android and iOS.** Each device run covers cold/warm start, touch, audio, hide/show, HTTPS, cache, UpdateManager callbacks, one battle, and 30 minutes of repeated play.
7. **This change is the long-lived Douyin readiness record.** It remains active while a release-blocking row is Yellow or Red. It is refreshed when the Unity baseline, candidate TTSDK/tooling, official platform requirements, account access, or device availability changes; it is not archived merely because the desktop/WebGL baseline passes or the item has aged.

## Risks / Trade-offs

- **[Unity 6.3 may be outside a pinned SDK's proven matrix]** -> Record Yellow and test an isolated SDK branch before modifying the main runtime; propose an engine change only if the spike is Red.
- **[Developer tools and AppID require interactive account access]** -> Keep the automated preflight complete, then pause only the account/device rows for an authorized operator.
- **[TTAssetBundle host coverage is narrower than generic downloads]** -> Keep a tested UnityWebRequest fallback and record host/version capabilities.
- **[Wasm function collection can miss early code]** -> Include Bootstrap, Lobby, the complete first battle, background/foreground, and update UI in both Android and iOS collection runs.
- **[Official limits and tools change]** -> Store observed versions and retrieval dates in the report; CI reads the pinned report rather than hard-coded recollection.

## Migration Plan

1. Run the desktop preflight against the current project and create a Yellow report for missing external tools/evidence.
2. Pin a reviewed TTSDK and Douyin developer-tool version in an isolated branch.
3. Convert and run the project in the simulator, then perform Android and iOS matrix runs.
4. Mark the gate Green only when every release-blocking row has evidence.
5. Re-run the current readiness script and clean-checkout conversion against the latest recorded baseline before closure.
6. Start `add-douyin-runtime-adapter`, then archive this tracker only after the Green evidence and handoff are recorded; rollback is removal of the isolated SDK branch with the core runtime untouched.

## Open Questions

- The project owner must supply or authorize a Douyin Mini Game AppID and developer login before simulator upload and device preview.
- Exact TTSDK and developer-tool versions remain unpinned until their Unity 6000.3.19f1 compile/export check succeeds.

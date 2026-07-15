## 1. Evidence Contract

- [x] 1.1 Define the non-secret compatibility report schema and Green/Yellow/Red classification rules
- [x] 1.2 Record official WeChat SDK, Developer Tools, UpdateManager, content/cache, package-splitting, and Unity 6 references with retrieval dates

## 2. Desktop Preflight

- [x] 2.1 Add a non-destructive PowerShell preflight for Unity, WebGL, Node, WXSDK/conversion plugin, Stable Developer Tools, configuration, credential presence, generated artifacts, and package sizes
- [x] 2.2 Run the preflight on the baseline project and save the generated compatibility report
- [x] 2.3 Verify the report contains no credential values or user-specific secret material

## 3. Conversion, Simulator, and Device Matrix

- [ ] 3.1 Pin an official WXSDK/conversion commit and Stable WeChat Developer Tools version that compile and convert Unity 6000.3.19f1
- [ ] 3.2 Produce and statically validate a converted WeChat Mini Game artifact from the Unity WebGL export
- [ ] 3.3 Complete simulator checks for launch, input, audio, lifecycle, HTTPS/cache, UpdateManager, and remote content
- [ ] 3.4 Complete the Android cold/warm launch, lifecycle, update, battle, and 30-minute stability matrix
- [ ] 3.5 Complete the iOS cold/warm launch, lifecycle, update, battle, and 30-minute stability matrix
- [ ] 3.6 Exercise WXAssetBundle or Addressables delivery plus the documented UnityWebRequest fallback, including cold/warm cache and version fallback
- [ ] 3.7 Exercise ordinary subpackages and Wasm splitting as startup/package mechanisms without treating them as runtime code hot replacement

## 4. Gate Decision

- [x] 4.1 Run the existing Unity smoke and WebGL build after spike tooling changes
- [x] 4.2 Mark the final gate Green only if all release-blocking conversion, simulator, and device rows have evidence
- [x] 4.3 Publish the next action: authorize the later WeChat adapter after Douyin stability, retain Yellow prerequisites, or create a Red engine/toolchain proposal

## 5. Long-Term Tracking Lifecycle

- [x] 5.1 Designate this change as the canonical long-lived WeChat readiness tracker and keep it unarchived while blocking rows or the Douyin-first dependency remain
- [ ] 5.2 Before closure, refresh official references, tool versions, and the readiness report against the current Unity, P0/P1, and Douyin-release baseline
- [ ] 5.3 Before closure, attach non-secret conversion, simulator, Android, iOS, content, update, subpackage, Wasm, and stability evidence with hashes
- [ ] 5.4 After all blocking rows are Green and Douyin is stable, repeat the clean-checkout gate and record the handoff to `add-wechat-runtime-adapter`

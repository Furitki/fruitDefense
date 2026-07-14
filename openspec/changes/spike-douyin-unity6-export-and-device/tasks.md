## 1. Evidence Contract

- [x] 1.1 Define the non-secret compatibility report schema and Green/Yellow/Red classification rules
- [x] 1.2 Record official Douyin mechanism and minimum-version references with retrieval dates

## 2. Desktop Preflight

- [x] 2.1 Add a non-destructive PowerShell preflight for Unity, WebGL, Node, TTSDK, developer tools, configuration, credentials, builds, and package budgets
- [x] 2.2 Run the preflight on the baseline project and save the generated compatibility report
- [x] 2.3 Verify the report contains no credential values or user-specific secret material

## 3. Simulator and Device Matrix

- [ ] 3.1 Pin a TTSDK and Douyin developer-tool version that compiles and converts Unity 6000.3.19f1
- [ ] 3.2 Complete simulator checks for launch, input, audio, lifecycle, HTTPS/cache, UpdateManager, and remote content
- [ ] 3.3 Complete the Android cold/warm launch, lifecycle, update, battle, and 30-minute stability matrix
- [ ] 3.4 Complete the iOS cold/warm launch, lifecycle, update, battle, and 30-minute stability matrix
- [ ] 3.5 Exercise Addressables through TTAssetBundle and its documented UnityWebRequest fallback
- [ ] 3.6 Collect Bootstrap, Lobby, first-battle, lifecycle, and update UI functions for the Wasm-splitting evidence set

## 4. Gate Decision

- [x] 4.1 Run the existing Unity smoke and WebGL acceptance after spike tooling changes
- [x] 4.2 Mark the final gate Green only if all release-blocking simulator and device rows have evidence
- [x] 4.3 Publish the next action: authorize `add-douyin-runtime-adapter`, retain Yellow prerequisites, or create a Red engine/toolchain proposal

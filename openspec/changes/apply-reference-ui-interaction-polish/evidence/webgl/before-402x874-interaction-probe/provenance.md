# Before UI interaction probe provenance

- Source revision: `1c6ad3e8a8f13f45018dd8ae7d719c785ddd8d34` (`feat(ui): unify runtime visual system and archive polish`)
- Build environment: detached Git worktree under the Windows user temp directory; Unity `6000.3.19f1`; `FruitDefense.Editor.WebBuild.Build`
- Acceptance command: current `scripts/accept-webgl-portrait.ps1` with `-ServeLocal -ShellVisual -InteractionPolishEvidence -LevelId orchard-02 -Width 402 -Height 874`
- Acceptance result: `FRUIT_DEFENSE_SHELL_VISUAL_OK`; see `shell-visual-evidence.json` for the complete browser/runtime metrics.
- Build result: `FRUIT_DEFENSE_WEB_BUILD_OK`; see `before-webgl-build.log` for the complete Unity output.

The old build passed the interaction probe because its existing selected-state rendering and native IMGUI pressed-state rendering both produce distinct immediate frames. This result is a visual baseline, not evidence that revision `1c6ad3e` already contains the new shared motion evaluator or press lifecycle.

The manifest's `runtimeUi.*Asset` values are paths resolved by the current acceptance script from the main workspace. Build payload versions, content hashes, screenshots, and browser state are from the isolated revision `1c6ad3e` build served from the detached worktree.

## Build payloads

| File | Bytes | SHA-256 |
|---|---:|---|
| `WebGL.data.unityweb` | 5,575,469 | `c39ef3ab5bb2ae6a674e86fc5723206ffa44992af7fbc51bd04ad30e65bccf7a` |
| `WebGL.framework.js.unityweb` | 69,018 | `0c5ecd20fc1c192495e6c368f0642cb5cb2937296cafae6130224ea9262081e6` |
| `WebGL.loader.js` | 117,893 | `cfaa2d82d6d07c12674952310a75b305ecbb1bc55f3c302f8e29c114c5c5dc76` |
| `WebGL.wasm.unityweb` | 3,881,120 | `b0689f4279535d61f913e108ef6b61b8bd071a066856df94ef74c20ee0113c4e` |

## Screenshot hashes

| Checkpoint | SHA-256 |
|---|---|
| `00-bootstrap-initializing.png` | `cf0c3059e1ee483d07fad4d4188ff531624d2b16053ac84c0f06b1640002a6c1` |
| `01-lobby-default.png` | `cf0c3059e1ee483d07fad4d4188ff531624d2b16053ac84c0f06b1640002a6c1` |
| `02a-lobby-selection-motion.png` | `ed124f2c6c84500f67a0ce649cf175ebfb9f0c5fc92f47cbc19ae1ab63c6945f` |
| `02-lobby-alternate-selection.png` | `fc66027d0788010d28b698fe3b72545441d302638db28877d9b729481a3e9331` |
| `03-lobby-start-pressed.png` | `c0fe0b41c6f21f5c847774325647b2b0f1c6d10dfc2d3d680012d5083135e9c9` |

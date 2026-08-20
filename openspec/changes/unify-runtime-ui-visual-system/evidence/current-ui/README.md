# Current UI evidence index

Release-flow 与 Battle evidence 使用 `402 × 874` CSS-pixel viewport、完整 safe area（top/bottom/left/right 均为 0）。截图来自 2026-08-17 对工作区已有普通 WebGL 构建的只读验收；未修改 runtime、scene 或 art asset。Bootstrap 目录中的 Web 宿主上下文图不属于这两组通过验收的 canvas capture。

## Release flow

命令：

```powershell
& .\scripts\accept-webgl-portrait.ps1 `
  -ServeLocal -Flow -Width 402 -Height 874 `
  -OutputDirectory "openspec\changes\unify-runtime-ui-visual-system\evidence\current-ui\release-flow"
```

结果：`FRUIT_DEFENSE_FLOW_ACCEPTANCE_OK`

- [`01-lobby.png`](release-flow/01-lobby.png)
- [`02-battle.png`](release-flow/02-battle.png)
- [`03-settlement.png`](release-flow/03-settlement.png)
- [`04-returned-lobby.png`](release-flow/04-returned-lobby.png)
- [`05-retry-battle.png`](release-flow/05-retry-battle.png)
- [`flow-acceptance.json`](release-flow/flow-acceptance.json)：viewport、safe area、route/session identity、图片指标、delivery 与 cache 检查的原始 manifest。

## Battle state matrix

命令：

```powershell
& .\scripts\accept-webgl-portrait.ps1 `
  -ServeLocal -Width 402 -Height 874 `
  -OutputDirectory "openspec\changes\unify-runtime-ui-visual-system\evidence\current-ui\battle-states"
```

结果：`FRUIT_DEFENSE_VISUAL_ACCEPTANCE_OK`

- [`01-ready.png`](battle-states/01-ready.png)
- [`02-active-wave.png`](battle-states/02-active-wave.png)
- [`03-between-wave.png`](battle-states/03-between-wave.png)
- [`04-immediate-next-wave.png`](battle-states/04-immediate-next-wave.png)
- [`05-paused.png`](battle-states/05-paused.png)
- [`06-continued.png`](battle-states/06-continued.png)
- [`07-restarted.png`](battle-states/07-restarted.png)
- [`08-adjacent-pots.png`](battle-states/08-adjacent-pots.png)
- [`09-drag-target.png`](battle-states/09-drag-target.png)
- [`10-dense-board.png`](battle-states/10-dense-board.png)
- [`11-inspection-click.png`](battle-states/11-inspection-click.png)
- [`12-destination-click-no-move.png`](battle-states/12-destination-click-no-move.png)
- [`13-after-drag-move.png`](battle-states/13-after-drag-move.png)
- [`acceptance.json`](battle-states/acceptance.json)：状态、交互、中文、frame、delivery 与 cache 检查的原始 manifest。

## Bootstrap context

- [`00-after-navigation.png`](bootstrap/00-after-navigation.png) 是请求 402 × 874 浏览器 viewport 时取得的 Web 宿主 Unity loader；由于宿主页滚动条，其 PNG 可见区为 387 × 841。它只用于证明正式运行时 UI 出现前仍有一套未统一的宿主加载视觉。
- 它不是 `AppFlowCoordinator.OnGUI` 初始化/错误/重试画面，不计作 runtime Bootstrap capture。准确阻碍和后续验收要求见 [`../../current-ui-audit.md`](../../current-ui-audit.md)。

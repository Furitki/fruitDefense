## Why

水果塔防目前只有玩法说明和视觉参考，没有可运行的 WebApp。需要先建立稳定的前端骨架、共享领域模型和单屏游戏舞台，供后续战斗、经济与装备模块并行实现。

## What Changes

- 创建 React、TypeScript、Vite WebApp，并提供开发、构建和测试命令。
- 建立植物、僵尸、花盆、投射物、波次、武器和游戏阶段的共享领域模型。
- 建立单一游戏状态容器、确定性随机数入口和逐帧更新接口。
- 实现参考图风格的响应式单屏布局、HUD、游戏舞台和基础无障碍交互。
- 提供可供独立 change 使用的模块边界与集成契约。

## Capabilities

### New Capabilities

- `web-game-shell`: 可运行、可测试、响应式的水果塔防 WebApp 外壳和共享运行时契约。

### Modified Capabilities

无。

## Impact

- 新增前端工程、共享类型、状态容器、基础组件和样式。
- 引入 React、Vite、TypeScript、Vitest 与必要的测试依赖。
- 后续三个 change 必须依赖本 change 提供的领域类型和模块接口。

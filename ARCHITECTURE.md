# 水果塔防 WebApp 架构

## 优先级与 OpenSpec changes

| 优先级 | Change | 职责 | 依赖 |
| --- | --- | --- | --- |
| P0 | `establish-game-foundation` | 工程、领域模型、时钟、响应式外壳 | 无 |
| P0 | `implement-zombie-battle` | 15 波僵尸、路径、索敌、五植物战斗 | foundation |
| P0 | `implement-plant-economy` | 阳光、刷新、苗圃、种植、移动、合成、出售 | foundation + battle 事件 |
| P1 | `implement-equipment-expansion` | 三武器、状态效果、花盆扩建和预览 | foundation + battle + economy |

## 运行时分层

```text
React Components
  ├─ HUD / Battlefield / Nursery / ToolInventory
  └─ 只读取 GameState 并派发 GameCommand
                       │
                       ▼
useGameEngine (唯一 requestAnimationFrame 与状态入口)
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
     battle.ts     economy.ts   equipment.ts
     移动/攻击      刷新/合成     武器/扩建
          └────────────┼────────────┘
                       ▼
           GameState + config.ts
```

## 核心约束

- `GameState` 是唯一运行时真相源，组件不创建独立战斗计时器。
- 三个业务模块暴露 `step(state, dt)` 风格的纯更新和 `reduce(state, command)` 命令处理。
- 战场使用 0–100 归一化坐标；SVG 负责路径/范围，DOM 负责可交互实体。
- 规则配置与运行实体分离，随机入口带种子以支持稳定测试。
- 鼠标拖拽、触摸点选和键盘操作最终落到同一 `GameCommand`，规则层再次校验合法性。
- 子 agent 不直接集成根组件；公共集成和最终验收由主 agent 负责。

## 完整单局数据流

```text
刷新五株 → 苗圃清空约束 → 种入花盆 / 合成 / 出售
    ↓
开始波次 → 僵尸生成与移动 → 植物索敌攻击 → 击杀奖励阳光
    ↓
波次奖励武器或花盆 → 安装改造 / 正交扩建 → 更强布阵
    ↓
核心生命归零失败 ← 15 波全部消灭 → 胜利
```

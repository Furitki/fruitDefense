## ADDED Requirements

### Requirement: 可运行的单页游戏
系统 SHALL 提供一个无需服务端即可启动、构建和游玩的 TypeScript WebApp。

#### Scenario: 启动开发版本
- **WHEN** 开发者运行项目开发命令
- **THEN** 浏览器显示水果塔防单页游戏且无运行时错误

### Requirement: 共享游戏状态
系统 MUST 使用统一游戏状态表达阳光、生命、波次、实体、库存、选择和游戏阶段。

#### Scenario: 重开游戏
- **WHEN** 玩家在结算页点击重新开始
- **THEN** 所有运行实体与计时器被清除并恢复为初始状态

### Requirement: 响应式单屏布局
系统 SHALL 在桌面和移动宽度下保持 HUD、战场和操作栏可见且可操作。

#### Scenario: 窄屏游玩
- **WHEN** 视口宽度缩小到 390 像素
- **THEN** 页面不产生遮挡主要按钮的水平溢出且游戏舞台按比例缩放

### Requirement: 游戏时钟控制
系统 SHALL 提供暂停、继续和一倍/二倍速度控制，并在页面失焦时避免异常时间跳跃。

#### Scenario: 暂停战斗
- **WHEN** 玩家点击暂停
- **THEN** 僵尸、投射物、攻击冷却和阳光生产均停止推进

# weapon-modifiers Specification

## Purpose
TBD - created by archiving change implement-equipment-expansion. Update Purpose after archive.
## Requirements
### Requirement: 武器库存与安装
玩家 SHALL 能将库存中的机枪、冰块或辣椒安装到未装备武器的植物，每株植物 MUST 最多装备一种武器。

#### Scenario: 安装武器
- **WHEN** 玩家把冰块放到一株未装备武器的植物
- **THEN** 冰块从库存减少且植物立即获得冰属性效果

### Requirement: 三种武器效果
机枪 MUST 提供 80% 攻速和 25% 单次伤害降低；冰块 MUST 提供 30% 两秒减速与五次命中后的一秒冻结；辣椒 MUST 提供每秒 20% 基础伤害、持续三秒且最多三层的燃烧。

#### Scenario: 冰冻累计
- **WHEN** 同一僵尸连续受到第五次冰属性命中
- **THEN** 该僵尸冻结一秒且累计重新开始

### Requirement: 向日葵武器转换
系统 SHALL 为向日葵把三种武器转换为明确的经济或辅助效果，而不是无效装备。

#### Scenario: 机枪向日葵
- **WHEN** 向日葵装备机枪
- **THEN** 其生产速度提高且界面显示转换后的效果

### Requirement: 武器回收
武器 MUST 不能直接拆卸；当装备武器的来源植物参与二合一时，来源武器 SHALL 完整返回库存，目标植物原有武器保持不变。

#### Scenario: 出售装备植物
- **WHEN** 玩家尝试出售一株装备辣椒的植物
- **THEN** 系统不提供出售操作，植物与辣椒库存均保持不变

#### Scenario: 装备植物作为合成来源
- **WHEN** 玩家把一株装备辣椒的植物拖到可与其合成的目标植物
- **THEN** 合成完成、辣椒库存增加一且升级后的目标植物保留在目标位置

### Requirement: 跨输入方式武器拖拽
玩家 SHALL 能用鼠标、触摸或触控笔把有库存的武器拖到未装备武器的植物；拖动期间系统 MUST 显示当前植物目标是否合法，并仅在合法目标释放时消耗库存和安装武器。

#### Scenario: 触摸拖动武器到植物
- **WHEN** 玩家用触摸指针把库存武器拖到未装备武器的植物并释放
- **THEN** 目标显示可安装反馈、武器库存减少一且植物立即装备该武器

#### Scenario: 拖到无效位置
- **WHEN** 玩家把武器释放在植物之外或已装备武器的植物上
- **THEN** 武器返回库存、游戏状态保持不变且界面显示取消或不可安装反馈

#### Scenario: 点击安装备用路径
- **WHEN** 玩家选择一个库存武器后点击未装备武器的植物
- **THEN** 系统安装所选武器且结果与拖放安装一致

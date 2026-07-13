## MODIFIED Requirements

### Requirement: 可持续阳光循环
系统 SHALL 仅通过向日葵生产、击杀和波次结束提供可持续阳光，并对每笔变化显示反馈；植物 MUST NOT 通过出售转换为阳光。

#### Scenario: 向日葵生产
- **WHEN** 向日葵完成当前星级的生产间隔
- **THEN** 阳光按当前星级产出增加并显示获得提示

#### Scenario: 苗圃非空
- **WHEN** 苗圃仍有植物且玩家尝试刷新
- **THEN** 系统仅提示先种植或合成且不提供出售选项

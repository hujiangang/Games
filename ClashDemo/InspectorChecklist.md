# Inspector 清单

## Lane_Left / Lane_Right
- 挂 `LanePath`
- `side` 选择对应方向
- 把 waypoint 子物体按顺序拖进 `waypoints`

## Tower_Blue_Main / Tower_Red_Main
- 挂 `Collider`
- 挂 `AttackSlotProvider`
- 调整 `slotRadius` 和 `slotCount`

## BlueUnit / RedUnit 预制体
- 挂 `CharacterController`
- 挂 `UnitCrowdAgent`
- 确认 `UnitCrowdAgent` 的 `lane` 和 `currentTarget` 之后会由生成器赋值

## BattleBootstrap
- 挂 `DemoBattleSpawner`
- 把蓝/红单位 prefab 拖进去
- 把两条 lane 拖进去
- 把出生点拖进去
- 把两个塔拖进去

## 如果你想先最简单跑起来
- `blueCount = 3`
- `redCount = 3`
- `slotCount = 6`
- `slotRadius = 1.0`
- `separationWeight = 1.4`
- `laneWeight = 1.0`
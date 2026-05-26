# 场景搭建步骤

下面这套是最小原型场景，不追求美术，只追求能看出推挤、避让和占位效果。

## 1. 新建场景
- 打开 Unity。
- 新建一个空场景，保存为 `BattleDemo`。
- 场景里先只留一个平面和一个方向光。

## 2. 搭地面
- 创建 `Plane`，命名为 `Ground`。
- 把它缩放到 `X=2, Z=3` 左右，保证有足够空间。
- 地面放平，Y 为 0。

## 3. 搭两条路线
- 创建空物体 `Lane_Left`。
- 创建空物体 `Lane_Right`。
- 每条路线下面放 4 到 6 个子物体作为 waypoint。
- waypoint 位置沿着路从己方出生点排到敌方塔前。

建议坐标示例：
- 左路：`(-6, 0, -10) -> (-6, 0, -4) -> (-2, 0, 0) -> (-6, 0, 6) -> (-6, 0, 10)`
- 右路：`(6, 0, -10) -> (6, 0, -4) -> (2, 0, 0) -> (6, 0, 6) -> (6, 0, 10)`

## 4. 搭塔和目标
- 创建两个空物体或简单 Cube，命名为 `Tower_Blue_Main` 和 `Tower_Red_Main`。
- 给这两个目标各挂一个 `Collider`。
- 再挂 `AttackSlotProvider`。
- 把蓝塔放在靠下方的一侧，把红塔放在靠上方的一侧。

## 5. 搭出生点
- 创建 `Spawn_Blue` 和 `Spawn_Red`。
- 放在各自地图边缘。
- 位置可以略微偏左、偏右，方便单位一出生就进入各自路线。

## 6. 搭单位预制体
- 创建一个简单 Capsule 或 Cube。
- 给它挂 `CharacterController`。
- 再挂 `UnitCrowdAgent`。
- 做成 prefab，命名为 `BlueUnit` 或 `RedUnit`。

## 7. 放一个生成器
- 创建空物体 `BattleBootstrap`。
- 挂 `DemoBattleSpawner`。
- 在 Inspector 里把蓝/红单位 prefab、两条路线、两个出生点、两个塔都拖进去。

## 8. 第一次运行
- 先把 `blueCount` 和 `redCount` 设成 3 或 4。
- 点击 Play。
- 观察单位是否沿路线前进、是否会在桥口聚拢、是否会在塔前围成一圈。

## 9. 如果看起来不对
- 先检查 `LanePath` 的 waypoint 顺序。
- 再检查单位的 `lane` 和 `currentTarget` 是否被 spawner 正确赋值。
- 最后再调 `laneWeight`、`separationWeight`、`slotRadius`。
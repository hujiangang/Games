# 原型接入说明

## 目标
这一套原型不是做通用 RTS 寻路，而是模拟《皇室战争》那种更强约束的群体行为。

## 场景建议
- 一张平坦地图。
- 左右两条 lane。
- 每条 lane 上放桥口、塔前和终点 waypoint。
- 双方各放一个主塔。
- 先不要加太多静态障碍。

## 单位预制体建议
- `CharacterController`
- `Collider`
- `UnitCrowdAgent`

## 塔或目标建议
- `Collider`
- `AttackSlotProvider`

## 调参优先级
1. 先调 `laneWeight`，让单位稳定沿路走。
2. 再调 `separationWeight`，让拥挤时有推挤感。
3. 再调 `slotCount` 和 `slotRadius`，让近战围塔更自然。
4. 最后再微调 `attackRange` 和 `attackStickiness`。

## 观察重点
- 单位进入桥口前是否自然收束。
- 人多时是否会被挤开，而不是死叠在一起。
- 近战是否会围着目标形成一圈，而不是都挤在同一个点。

## 下一步建议
如果这版手感还不够像《皇室战争》，下一步就把当前的 `OverlapSphere` 改成单位注册表 + 固定 tick 的 push 解算，这样性能和稳定性都会更好。
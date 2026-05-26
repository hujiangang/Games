# ClashDemo

这是一个新的 Unity 原型工程，用来单独研究《皇室战争》式的群体移动、推挤、避让和占位。

## 已放入的原型内容
- `Assets/CRCrowdPrototype/Scripts/CRSimulationTypes.cs`
- `Assets/CRCrowdPrototype/Scripts/LanePath.cs`
- `Assets/CRCrowdPrototype/Scripts/UnitCrowdAgent.cs`
- `Assets/CRCrowdPrototype/Scripts/CrowdSolver.cs`
- `Assets/CRCrowdPrototype/Scripts/AttackSlotProvider.cs`
- `Assets/CRCrowdPrototype/Scripts/BattleBootstrap.cs`
- `Assets/CRCrowdPrototype/CRCrowdPrototype.asmdef`

## 使用方式
1. 打开这个工程。
2. 等 Unity 导入脚本。
3. 按 `SceneSetup.md` 搭一个最小测试场景。
4. 用 `DemoBattleSpawner` 一键生成测试单位。
5. 先用少量单位测试，再逐步加密度。

## 你现在要研究的重点
- 固定路线怎么走。
- 单位怎么互相推挤。
- 目标周围怎么占位。
- 桥口和窄道怎么形成拥堵。
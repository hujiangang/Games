# Clash Royale Push Effect Tuning

本文档整理当前项目里“皇室战争式推挤效果”的配置方式、参数来源和调参方向。

目标不是做真实物理碰撞，而是还原以下观感：

- 同阵营单位会抱团推进
- 桥口会出现拥挤和顶推
- 重单位会更像在前面开路
- 远程单位不会过度左右横摆
- 单位不会像机器人一样提前大幅避让

---

## 1. 当前生效链路

当前地面单位的移动链路是：

1. `CardDragHandler` 生成单位
2. `UnitAgent` 初始化
3. `UnitAgent` 构造 `DtCrowdAgentParams`
4. `NavMeshManager` 把单位注册到 `DtCrowd`
5. `DtCrowd` 基于导出的 DotRecast NavMesh 做局部避障和推进
6. `UnitAgent` 叠加一层质量推挤修正

相关代码位置：

- `Assets/GamePlay/UnitAgent.cs`
- `Assets/GamePlay/UnitCrowdSnapshot.cs`
- `Assets/GamePlay/UnitCrowdInteractionSolver.cs`
- `Assets/NavMeshCore/Runtime/NavMeshManager.cs`
- `Assets/Scripts/Units/UnitData.cs`

运行时真正影响“推挤感”的配置分成三层：

1. 场景导航源配置
2. `DtCrowd` 全局配置
3. 单位自己的 `DtCrowdAgentParams` 和 `UnitCrowdConfig`
4. `UnitAgent` 的质量推挤修正

### 1.1 代码职责怎么分

为了避免所有 crowd 细节都堆在 `UnitAgent` 里，现在职责拆成了三块：

- `UnitAgent`
  负责生命周期、注册/反注册、攻击判定、更新位置、调用求解器
- `UnitCrowdSnapshot`
  负责把“当前单位这一帧需要参与推挤计算的数据”整理成快照
- `UnitCrowdInteractionSolver`
  负责真正的推挤求解、轻重单位优先级处理、重单位朝向稳线处理

这样后面查问题时可以按现象定位：

- 注册失败、寻路失败、攻击状态不对：先看 `UnitAgent`
- 质量推挤不对、多个小单位推大单位不对：看 `UnitCrowdInteractionSolver`
- 读取到的半径、质量、意图方向不对：看 `UnitCrowdSnapshot`

---

## 2. 场景怎么配

### 2.1 Layer 约定

当前推荐约定：

- `ground`：两岸地面
- `Briage`：桥面
- `Water`：河道和不可走水面
- `Default`：塔、灯光、管理器、普通装饰物

注意：

- 现有层名里是 `Briage`，这是历史拼写，功能上可以用
- 如果后面要清理工程，建议统一改成 `Bridge`

### 2.2 Tag 约定

当前推荐约定：

- `Walkable`：真正参与导航导出的可走表面
- `Untagged`：不参与导航导出的普通对象

### 2.3 哪些物体应该打 `Walkable`

应该打 `Walkable` 的物体：

- `Ground_BlueHalf`
- `Ground_RedHalf`
- 桥左右桥面
- 桥中间桥面

不应该打 `Walkable` 的物体：

- `River`
- 河道水面
- 水下装饰块
- 塔
- 管理器
- 灯光

### 2.4 当前导出器如何筛选导航源

当前 `NavMeshBakeWindow` 不是全场景乱扫，而是按下面规则采集：

1. 先找 `Tag = Walkable` 的对象
2. 再检查这些对象及子节点的 `Layer` 是否在 `WalkableLayers` 中
3. 只收满足条件的 `MeshFilter`

相关代码位置：

- `Assets/NavMeshCore/Editor/NavMeshBakeWindow.cs`

### 2.5 `WalkableLayers` 应该怎么配

当前 `NavMeshSettings.asset` 里：

- `WalkableLayers = ground + Briage`

这意味着：

- `Water` 不会被导出进导航
- `Default` 也不会被导出进导航

如果你改错这里，即使物体没打 `Walkable`，后面调试也会很混乱，所以这项尽量保持稳定。

---

## 3. 导航数据怎么导出

### 3.1 编辑器操作顺序

标准流程：

1. 打开 `NavMeshSettings`
2. 点击“打开烘焙窗口”
3. 点击“一键烘焙 NavMesh”
4. 点击“导出服务器 NavMesh 数据”

### 3.2 产物说明

会生成两份数据：

- `Assets/Resources/SceneNavMesh.asset`
  这是 Unity `NavMeshData` 资源，主要用于编辑器烘焙和校验

- `Assets/Resources/NavMesh/SceneNavMesh.bytes`
  这是 DotRecast 运行时真正加载的二进制数据

### 3.3 运行时真正读的是哪份

`UnitAgent + DtCrowd` 运行时真正读取的是：

- `SceneNavMesh.bytes`

加载位置：

- `Assets/NavMeshCore/Runtime/NavMeshManager.cs`

如果只烘焙 `.asset` 但没重新导出 `.bytes`，运行时的推挤和过桥行为不会更新。

---

## 4. 全局 Crowd 参数怎么配

这部分在：

- `Assets/NavMeshCore/Runtime/NavMeshManager.cs`

### 4.1 `DtCrowdConfig`

当前关键项：

```csharp
pathQueueSize = 48
checkLookAhead = 6
targetReplanDelay = 0.2f
maxObstacleAvoidanceCircles = 4
maxObstacleAvoidanceSegments = 6
collisionResolveFactor = 0.45f
```

#### `pathQueueSize`

含义：

- crowd 内部可排队的寻路请求数量

现在这样做的原因：

- 单位数量上来后不容易因为请求队列太小而丢目标更新

调大影响：

- 稳定性更高
- 内存和处理成本略增

#### `checkLookAhead`

含义：

- 局部路径前瞻长度

现在这样做的原因：

- 从以前更远的前瞻降到 `6`
- 让单位别太早开始“礼貌绕行”

调大影响：

- 更早绕路
- 更平滑
- 但更不像 CR 的硬顶推进

调小影响：

- 更容易出现桥口顶推
- 更接近成团压过去的效果

#### `targetReplanDelay`

含义：

- 重算移动目标的间隔

现在这样做的原因：

- 调低到 `0.2`
- 让人群推进时对目标变化反应更快

调大影响：

- 反应更钝
- 兵线会显得更拖

调小影响：

- 目标追踪更紧
- 但频繁更新会增加计算负担

#### `collisionResolveFactor`

含义：

- crowd 解决单位碰撞时的分离强度

现在这样做的原因：

- 从更高值降到 `0.45`
- 减少“被轻轻一碰就弹开”的感觉
- 保留更明显的推线压缩感

调大影响：

- 单位更容易散开
- 更不容易互相挤
- 但会失去 CR 那种抱团感

调小影响：

- 单位更能挤在一起
- 更容易有桥口顶牛效果
- 太低会出现严重重叠和穿插

### 4.2 `ConfigureObstacleAvoidance()`

当前不是一套模板打天下，而是四套：

- `0 = heavyPushParams`
- `1 = pushParams`
- `2 = rangedParams`
- `3 = cautiousParams`

#### 槽位 0：`heavyPushParams`

适用：

- 坦克、重单位

特征：

- 高 `velBias`
- 很低 `weightSide`
- 很低 `weightCurVel`
- 偏低 `weightToi`

观感：

- 更愿意顶着往前走
- 更少横向绕开别人

#### 槽位 1：`pushParams`

适用：

- 默认地面近战单位

特征：

- 比重单位略保守
- 但仍然偏推进，不偏礼让

观感：

- 适合作为 CR 风格默认兵线

#### 槽位 2：`rangedParams`

适用：

- 远程地面单位

特征：

- 仍然前进
- 但比近战更会保持一点横向安全空间

观感：

- 不会像坦克一样硬顶
- 但也不会过度乱绕

#### 槽位 3：`cautiousParams`

适用：

- 预留给更保守的单位

特征：

- 更高 `weightSide`
- 更高 `weightToi`

观感：

- 更像标准 crowd 避障
- 不是当前 CR 默认主风格

---

## 5. 单位参数怎么配

这部分主要在：

- `Assets/GamePlay/UnitAgent.cs`
- `Assets/Scripts/Units/UnitData.cs`

### 5.1 `UnitCrowdConfig`

这是单位资产里策划可配的 crowd 参数：

```csharp
turnSpeed
radius
mass
separationRadius
separationWeight
laneWeight
targetWeight
attackStickiness
reachThreshold
```

目前 `UnitAgent` 真正直接使用到的核心项有：

- `radius`
- `mass`
- `separationRadius`
- `separationWeight`

其余项现在更多是旧原型逻辑遗留或后续扩展入口。

### 5.2 `radius`

含义：

- 单位逻辑占位半径

影响：

- 通路宽度判断
- 邻居检测
- 局部避障
- 桥口是否容易堵

调大：

- 更像大体型单位
- 更早形成拥堵

调小：

- 更容易穿插
- 更容易贴得很紧

### 5.3 `mass`

含义：

- 单位“重不重”

当前实际作用：

- 影响 `GetAcceleration()`
- 影响 `GetObstacleAvoidanceType()`
- 影响质量推挤时“谁能顶动谁”

当前逻辑：

- `mass >= 1.75` 的单位默认走 `heavyPush` 槽位
- 推挤时会把其它接触单位的压力累加，再和自己的质量阻力比较

质量推挤规则：

- 大质量可以顶开小质量
- 小质量单个通常顶不动大质量
- 多个小单位同时接触同一个大单位时，总压力大于大单位阻力后，可以把它慢慢推走

调大：

- 更像前排重单位
- 更不容易显得轻飘

调小：

- 更灵活
- 更像轻兵

### 5.4 `separationRadius`

含义：

- 开始考虑分离和避让的邻居搜索范围

当前默认值：

- `0.8`

当前实现里，它会影响：

- `GetCollisionQueryRange()`
- `GetPathOptimizationRange()`

调大：

- 提前更远开始让路
- 人群更松
- 更不像 CR 推线

调小：

- 更晚才开始挤开
- 更容易出现桥口压缩和拥堵

### 5.5 `separationWeight`

含义：

- 单位主动拉开距离的力度

当前默认值：

- `0.8`

`UnitAgent` 内部还会做一次钳制：

```csharp
Mathf.Clamp(unitData.crowdConfig.separationWeight, 0.2f, 1.1f)
```

这意味着：

- 低于 `0.2` 不会继续变更挤
- 高于 `1.1` 不会继续变更散

调大：

- 人群更分散
- 远程单位更像在走位

调小：

- 更容易抱团
- 更接近 CR 推线
- 太低会互相穿插

---

## 6. `UnitAgent` 运行时参数详解

### 6.0 质量推挤修正

这层逻辑不改 `DtCrowd` 内核，而是在 `UnitAgent` 的显示/战斗位置上叠加一个水平推挤偏移。

位置：

- `Assets/GamePlay/UnitAgent.cs`

核心目的：

- 恢复 CR 那种“前排顶着走、后排越堆越能推”的感觉

基本机制：

1. 每帧收集附近接触中的地面单位
2. 根据接触重叠、对方质量、对方朝自己推进的意图，累计压力
3. 用累计压力和自身质量阻力比较
4. 只有累计压力超过自身阻力时，单位才会被推开

这就是“单个小单位推不动大单位，但多个小单位合力可以推”的来源。

当前常量：

- `PushContactSlack = 0.02`
- `PushIntentFloor = 0.35`
- `PushOffsetLerp = 14`
- `PushOffsetRecoveryLerp = 10`
- `MaxPushOffsetFactor = 0.75`
- `PushStrengthScale = 2.25`

这些值目前写在 `UnitAgent.cs` 里，属于运行时推挤修正常量。

如果后面要继续微调：

- 想让单位更容易被顶动：提高 `PushStrengthScale`
- 想让单位更难被顶动：降低 `PushStrengthScale`
- 想让挤压形变更明显：提高 `MaxPushOffsetFactor`
- 想让位移回正更快：提高 `PushOffsetRecoveryLerp`

### 6.0.1 质量优先级修正

仅靠上面的“累计压力 > 自身阻力”规则，还会留下一个典型问题：

- 轻单位高速接近重单位时
- `DtCrowd` 的局部避障仍然可能让重单位发生一点横向漂移
- 画面上看起来像“重单位给轻单位让了一下”

为了解决这个观感问题，`UnitAgent` 现在又叠加了一层“优先级偏移”：

- 轻单位接近更重单位时，会主动侧让并略微后撤
- 重单位面对更轻单位时，会抑制由 crowd 避障带来的横向漂移
- 所以最终观感会更接近 CR：小单位绕，大单位顶

这层逻辑仍然只是显示/战斗位置修正，不直接改 `DtCrowd` 内核求解。

当前常量：

- `PriorityOffsetLerp = 12`
- `PriorityOffsetRecoveryLerp = 9`
- `MaxPriorityOffsetFactor = 0.6`
- `YieldBackoffFactor = 0.45`
- `YieldSideFactor = 0.8`
- `HeavyLaneHoldFactor = 1.25`

这些值的含义：

- `PriorityOffsetLerp`
  优先级让位偏移变大时的跟随速度
- `PriorityOffsetRecoveryLerp`
  接触解除后回正速度
- `MaxPriorityOffsetFactor`
  这层偏移相对单位半径的最大上限
- `YieldBackoffFactor`
  轻单位面对重单位时，朝“远离重单位”方向退开的权重
- `YieldSideFactor`
  轻单位面对重单位时，朝“侧向绕开”方向偏移的权重
- `HeavyLaneHoldFactor`
  重单位抵抗轻单位造成横摆的强度

怎么理解：

- 如果你觉得小单位看到坦克还不够主动绕开，就先升 `YieldSideFactor`
- 如果你觉得小单位退得不够明显，就升 `YieldBackoffFactor`
- 如果你觉得大单位还是会轻微蛇形摆动，就升 `HeavyLaneHoldFactor`
- 如果你觉得这层修正太夸张，就先降 `MaxPriorityOffsetFactor`

### 6.1 `maxAcceleration = GetAcceleration()`

当前逻辑：

```csharp
Mathf.Max(moveSpeed * 5f, fallbackAcceleration / mass)
```

设计目的：

- 保证轻单位启动不拖
- 重单位不会像被弹射一样起步

调大观感：

- 更灵敏
- 转向更猛

调小观感：

- 更厚重
- 更像在推

### 6.2 `collisionQueryRange = GetCollisionQueryRange(radius)`

当前逻辑：

```csharp
Mathf.Max(separationRadius, radius * 3.5f)
```

设计目的：

- 不再像之前那样“看得过远”
- 让单位更晚开始让路

### 6.3 `pathOptimizationRange = GetPathOptimizationRange(radius)`

当前逻辑：

```csharp
Mathf.Max(separationRadius * 2f, radius * 8f)
```

设计目的：

- 保留基本路径平滑
- 但不要过度提早拉直和绕行

### 6.4 `obstacleAvoidanceType = GetObstacleAvoidanceType()`

当前逻辑：

- `mass >= 1.75` -> `0`
- `attackRange >= 4f` -> `2`
- 其他默认 -> `1`

设计目的：

- 重单位更会顶
- 远程单位略微保守
- 默认近战作为主推线模板

---

## 7. 当前资产建议值

### 7.1 默认单位

建议起点：

- `radius = 0.35`
- `mass = 1`
- `separationRadius = 0.8`
- `separationWeight = 0.8`

### 7.2 近战冲脸单位

建议起点：

- `radius = 0.3 ~ 0.4`
- `mass = 1`
- `separationRadius = 0.7 ~ 0.85`
- `separationWeight = 0.6 ~ 0.85`

### 7.3 坦克

建议起点：

- `radius = 0.45 ~ 0.55`
- `mass = 1.8 ~ 2.3`
- `separationRadius = 0.85 ~ 1.0`
- `separationWeight = 0.35 ~ 0.55`

### 7.4 远程地面单位

建议起点：

- `radius = 0.3 ~ 0.35`
- `mass = 0.9 ~ 1.2`
- `separationRadius = 0.8 ~ 1.0`
- `separationWeight = 0.75 ~ 1.0`

---

## 8. 调参方法

如果当前表现是“太散”：

- 先降 `separationWeight`
- 再降 `separationRadius`
- 再降 `weightSide`

如果当前表现是“太会提前绕路”：

- 降 `collisionQueryRange`
- 降 `checkLookAhead`
- 降 `weightToi`
- 降 `horizTime`

如果当前表现是“太容易互相穿插”：

- 略升 `collisionResolveFactor`
- 略升 `separationWeight`
- 略升 `radius`

如果当前表现是“坦克不够像在前面推”：

- 提高 `mass`
- 使用 `obstacleAvoidanceType = 0`
- 降低该单位 `separationWeight`

如果当前表现是“远程单位左右乱晃”：

- 让远程单位走 `obstacleAvoidanceType = 2`
- 适度升一点 `separationWeight`
- 不要把 `collisionQueryRange` 设得过大

---

## 9. 当前项目里最重要的配置入口

场景导航配置：

- `Assets/Scripts/Units/NavMeshSettings.asset`

单位公共 crowd 默认值：

- `Assets/Scripts/Units/UnitData.cs`

单位运行时 crowd 参数：

- `Assets/GamePlay/UnitAgent.cs`

全局 crowd / 避障模板：

- `Assets/NavMeshCore/Runtime/NavMeshManager.cs`

单位资产示例：

- `Assets/Scripts/Units/U_AKKEP.asset`
- `Assets/Scripts/Units/U_Tank.asset`

---

## 10. 结论

当前这套“皇室战争推挤效果”的核心思路，不是给单位更强的避障，而是：

- 只保留必要避障
- 降低远距离礼让
- 强化目标方向推进
- 让重单位更会顶
- 让同阵营单位保持“挤着走但不完全重叠”的状态

如果后面继续迭代，优先动这几个点：

1. `separationWeight`
2. `separationRadius`
3. `collisionResolveFactor`
4. `weightSide`
5. `weightToi`
6. 单位 `mass`

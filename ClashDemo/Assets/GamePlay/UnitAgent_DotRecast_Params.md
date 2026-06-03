# UnitAgent DotRecast Parameters

本文档说明 [UnitAgent.cs](Assets/GamePlay/UnitAgent.cs) 中 `DtCrowdAgentParams` 的用途、当前配置原因，以及后续调参时应该关注的方向。

当前代码位置：

```csharp
return new DtCrowdAgentParams
{
    radius = radius,
    height = GetAgentHeight(),
    maxAcceleration = fallbackAcceleration,
    maxSpeed = GetMoveSpeed(),
    collisionQueryRange = radius * 12f,
    pathOptimizationRange = radius * 30f,
    separationWeight = GetSeparationWeight(),
    obstacleAvoidanceType = 3,
    queryFilterType = 0,
    updateFlags = updateFlags,
    userData = this
};
```

## 1. 这组参数是做什么的

`DtCrowdAgentParams` 是 DotRecast `DtCrowd` 对单个单位的运行参数定义。

它不负责“全局寻路规则”，而是负责下面几类内容：

- 单位的逻辑体型
- 单位的速度和加速度上限
- 单位看多远来避让别人
- 单位之间要不要主动拉开距离
- 单位采用哪套局部避障模板
- 单位使用哪套导航查询过滤器

如果这些参数不明确，`DtCrowd` 只能按非常有限的信息工作，常见结果就是：

- 单位彼此重叠
- 桥口和塔前抖动
- 转弯太硬
- 单位只会朝目标硬挤
- 大单位和小单位表现几乎一样

对于皇室战争类 demo，这些都是直接影响观感的核心问题，所以这里必须显式配置。

## 2. 参数逐项说明

### `radius`

含义：

- 单位在 crowd 系统中的逻辑半径
- 用于邻居检测、局部避障、分离、通路宽度判断

为什么这样做：

- 这里不用固定值，而是走 `GetAgentRadius()`
- 原因是不同兵种体积不同，小兵、战士、坦克不应该共享同一个占位大小

调大后的表现：

- 单位更容易保持间距
- 更不容易并排挤进狭窄区域
- 桥头会更早绕让

调小后的表现：

- 更容易扎堆
- 更容易出现穿插和重叠

### `height`

含义：

- 单位的逻辑高度
- 主要影响导航投影和可达性判断

为什么这样做：

- 这里用 `GetAgentHeight()`，优先取 `CharacterController.height`
- 这样可以复用 prefab 已经配置好的体型信息，避免导航层和 Unity 碰撞层完全脱节

调得不合理的风险：

- 过低时可能让导航判断过于乐观
- 过高时可能导致某些位置注册失败或贴地不稳定

### `maxAcceleration`

含义：

- 单位速度变化的最大加速度
- 决定从静止到移动、或者转向时“追速度”的快慢

为什么这样做：

- 当前值为 `fallbackAcceleration`
- 这是为了让单位在 CR 风格里看起来响应足够快，但不是瞬间弹射

调大后的表现：

- 启动更快
- 转向更干脆
- 避障反应更激进

调小后的表现：

- 单位显得更笨重
- 转弯时拖泥带水
- 容易在桥头形成迟滞

### `maxSpeed`

含义：

- 单位的最大移动速度

为什么这样做：

- 这里走 `GetMoveSpeed()`
- 直接把 `UnitData.moveSpeed` 同步到 crowd，保证策划配置会真实反映到导航行为上

如果不这样做：

- 视觉上你改了单位速度
- 实际 crowd 仍按另一套速度运行
- 会出现手感和配置对不上的问题

### `collisionQueryRange`

含义：

- 单位进行局部碰撞和邻居感知时的搜索半径
- 可以理解为“它会看多大范围来决定怎么绕”

为什么这样做：

- 当前值是 `radius * 12f`
- 用半径做比例而不是写死，是为了让大单位天然比小单位看得更远

为什么不是更小：

- 太小会导致单位只在快撞上时才开始避让
- 结果就是桥口顶牛、塔前抖动、队形很乱

为什么不是更大：

- 太大时单位会对很远的邻居提前反应
- 视觉上容易蛇形摆动
- 还会增加 crowd 的局部处理负担

### `pathOptimizationRange`

含义：

- 路径局部优化的作用范围
- DotRecast 会在这个范围内尝试把路径拉直、减少碎折线

为什么这样做：

- 当前值是 `radius * 30f`
- 这通常要比 `collisionQueryRange` 大，因为路径平滑应当比“看邻居”看得更远

调大后的表现：

- 走位更顺
- 更容易贴着一条自然轨迹前进

调小后的表现：

- 单位更容易出现小折线
- 贴边和过桥时会显得不够顺

### `separationWeight`

含义：

- 单位之间的分离权重
- 值越大，越不愿意贴在一起

为什么这样做：

- 当前走 `GetSeparationWeight()`
- 也就是允许不同兵种配置不同的分离倾向

这对 CR 风格为什么重要：

- CR 的兵线不是完全散开，也不是完全叠在一起
- 需要的是“成团推进，但有基本个人空间”
- 这个参数就是控制这种观感的核心值之一

调大后的表现：

- 队伍更松
- 更少重叠
- 更容易横向展开

调小后的表现：

- 更容易抱团
- 桥头会更拥挤
- 塔前更容易挤成一团

### `obstacleAvoidanceType`

含义：

- 避障参数槽位索引
- 它不是“避障等级本身”，而是选用哪一组避障模板

为什么这样做：

- 当前值是 `3`
- 在 [NavMeshManager.cs](Assets/NavMeshCore/Runtime/NavMeshManager.cs) 的 `ConfigureObstacleAvoidance()` 中，已经给 crowd 配置了 0 到 3 四个槽位
- 这里选 `3`，表示该单位使用第 4 套避障参数

当前设计意图：

- 目前四个槽位都被设置为同一套参数，先保证行为稳定
- 后续可以扩展成：
- 小兵用高质量避障
- 坦克用更稳重、更少横摆的模板
- 特殊单位用更保守或更激进的模板

### `queryFilterType`

含义：

- 导航查询过滤器槽位索引
- 用于控制单位“能走哪些 polygon，不能走哪些 polygon”

为什么这样做：

- 当前填 `0`
- 因为目前只使用默认查询过滤器

后续扩展用途：

- 地面单位和空中单位走不同区域逻辑
- 特殊兵种避开危险区
- 某些单位只走主路，不走支路

它现在看起来没什么存在感，但这是以后扩展导航规则的重要入口。

### `updateFlags`

含义：

- 一组按位组合的 crowd 行为开关

当前开启项：

- `DT_CROWD_ANTICIPATE_TURNS`
- `DT_CROWD_OBSTACLE_AVOIDANCE`
- `DT_CROWD_SEPARATION`
- `DT_CROWD_OPTIMIZE_VIS`
- `DT_CROWD_OPTIMIZE_TOPO`

分别代表：

- 提前预判转弯，而不是走到折点再生硬拐弯
- 开启局部障碍避让
- 开启单位间分离
- 开启可视路径优化，让路径更直
- 开启拓扑优化，让多边形层面的路径更稳定

为什么这样做：

- 如果只给目标、不启这些标记，单位通常会更像“朝目标点硬推”
- 这不符合 CR 那种成群推进、拐桥过河、围塔挪位的需求

### `userData`

含义：

- 给 DotRecast agent 绑定业务对象
- 当前绑定的是 `this`，也就是 Unity 里的 `UnitAgent`

为什么这样做：

- 方便以后从 crowd 反查所属单位
- 方便做调试、事件关联、命中关系或可视化

当前虽然还没直接使用，但保留它是正确的工程做法。

## 3. 为什么这组参数必须显式配置

核心原因有三点。

### 3.1 `DtCrowd` 只懂局部移动，不懂你的游戏规则

它不知道：

- 谁是坦克
- 谁是小兵
- 谁该更分散
- 谁该更灵活

所以这些信息必须靠 `DtCrowdAgentParams` 显式传入。

### 3.2 皇室战争类玩法对“拥挤观感”非常敏感

如果没有：

- `radius`
- `collisionQueryRange`
- `separationWeight`
- `obstacleAvoidanceType`

单位就会：

- 互相重叠
- 桥头乱顶
- 塔前扎堆抖动

这会直接破坏 CR 式兵线推进的感觉。

### 3.3 单位手感不是只靠 `moveSpeed`

`moveSpeed` 只定义了速度上限。

真正决定观感的是组合效果：

- `maxSpeed` 决定速度上限
- `maxAcceleration` 决定响应快慢
- `pathOptimizationRange` 决定路径顺不顺
- `updateFlags` 决定会不会主动优化和避让

所以这组参数必须成体系地配置。

## 4. 当前实现的设计目标

当前这套参数，不是追求“完全物理真实”，而是追求更接近皇室战争的视觉效果：

- 地面单位能沿 DotRecast NavMesh 找到桥并过河
- 单位接近时会基本避让，不会完全叠在一起
- 行进中拐弯更顺，不是硬折线
- 塔前会停住进入攻击，而不是继续硬顶目标中心点

这是一个偏游戏观感优先的配置，而不是通用机器人导航配置。

## 5. 后续调参建议

如果后面按兵种分档，建议至少拆成三类。

### 小兵

建议特征：

- 小半径
- 高分离权重
- 高避障质量
- 较高加速度

目标效果：

- 机动快
- 容易展开
- 不容易堵成一块

### 中型近战单位

建议特征：

- 中等半径
- 中等分离权重
- 标准避障质量

目标效果：

- 成为默认兵线模板
- 平衡推进感和稳定性

### 坦克 / 大体型单位

建议特征：

- 大半径
- 较低分离权重
- 稍低横向避让欲望
- 更稳的加速度

目标效果：

- 推进更稳
- 不要左右横摆太夸张
- 让后排单位自然绕着它走

## 6. 结论

这组 `DtCrowdAgentParams` 的意义，不是“把单位塞进 crowd 就完了”，而是把游戏里的兵种特征翻译成 DotRecast 能理解的运动规则。

对当前这个 Clash Royale 风格 demo 来说，它决定了以下观感是否成立：

- 会不会自动过桥
- 会不会扎堆
- 会不会走得顺
- 会不会在塔前自然停下
- 不同兵种有没有体感差异

如果后续要继续做单位差异化，优先调整的通常是：

1. `radius`
2. `maxAcceleration`
3. `collisionQueryRange`
4. `separationWeight`
5. `obstacleAvoidanceType`


using System;
using UnityEngine;

// 单位类别（地面或空中）
public enum UnitType
{
    Ground,
    Air
}

// 单位可攻击的目标类型
public enum TargetType
{
    GroundOnly,
    AirOnly,
    GroundAndAir,
    BuildingsOnly
}

[Serializable]
public class UnitCrowdConfig
{
    [Tooltip("单位转向速度，越大转身越快。")]
    public float turnSpeed = 12f;

    [Tooltip("单位逻辑半径，用于推挤和分离计算。")]
    public float radius = 0.35f;

    [Tooltip("单位质量，后续做强弱推挤时会用到。")]
    public float mass = 1f;

    [Tooltip("开始计算避让的搜索范围。")]
    public float separationRadius = 0.8f;

    [Tooltip("分离/推开其它单位的力度。")]
    public float separationWeight = 0.8f;

    [Tooltip("沿路线前进的权重。")]
    public float laneWeight = 1f;

    [Tooltip("朝目标靠近的权重。")]
    public float targetWeight = 1.15f;

    [Tooltip("贴近目标后的粘性。")]
    public float attackStickiness = 0.65f;

    [Tooltip("视为到达 waypoint 的距离阈值。")]
    public float reachThreshold = 0.1f;
}

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Units/Unit Data")]
public class UnitData : ScriptableObject
{
    public string unitName;           // 单位唯一标识
    public GameObject prefab;         // 单位预制体
    public Sprite icon;               // UI 图标

    [Header("基础属性")]
    public int manaCost;              // 召唤所需费用
    public float health;              // 最大生命值
    public float moveSpeed;           // 移动速度
    public UnitType unitType;         // 单位类型（地面/空中）

    [Header("攻击属性")]
    public TargetType targetType;     // 可攻击的目标类型
    public float damage;              // 单次伤害
    public float attackSpeed;         // 每秒攻击次数
    public float attackRange;         // 攻击范围
    public GameObject projectilePrefab; // 投射物预制体（如果有）

    [Header("群体移动 / 推挤 / 避让")]
    [Tooltip("这个单位专用的群体移动参数，不同单位应该分别配置。")]
    public UnitCrowdConfig crowdConfig = new UnitCrowdConfig();
}

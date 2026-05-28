using UnityEngine;

namespace CRCrowdPrototype
{
    public enum UnitState
    {
        March, // 行军状态，沿路线前进.
        Chase, // 追击状态，接近敌人.
        Attack, // 攻击状态，施加伤害.
        Reposition // 重新定位状态，调整位置.
    }

    public enum LaneSide
    {
        Left, // 左侧车道
        Right // 右侧车道
    }
}

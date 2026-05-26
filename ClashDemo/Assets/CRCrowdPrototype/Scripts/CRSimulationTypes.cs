using System;
using UnityEngine;

namespace CRCrowdPrototype
{
    public enum UnitState
    {
        March,
        Chase,
        Attack,
        Reposition
    }

    public enum LaneSide
    {
        Left,
        Right
    }

    [Serializable]
    public class UnitCrowdConfig
    {
        public float moveSpeed = 3.5f;
        public float turnSpeed = 12f;
        public float radius = 0.35f;
        public float mass = 1f;
        public float separationRadius = 1.2f;
        public float separationWeight = 1.4f;
        public float laneWeight = 1.0f;
        public float targetWeight = 1.15f;
        public float attackRange = 1.05f;
        public float attackCooldown = 1f;
        public float attackStickiness = 0.65f;
        public float reachThreshold = 0.1f;
    }
}
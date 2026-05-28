using System.Collections.Generic;
using UnityEngine;

namespace CRCrowdPrototype
{
    /// <summary>
    /// 攻击站位分配器.
    /// 一群单位打同一个目标时，不乱挤、不重叠、有序站位.
    /// </summary>
    public class AttackSlotProvider : MonoBehaviour
    {
        [Header("目标周围接近点")]
        public float slotRadius = 1.0f;
        public int slotCount = 6;

        public Vector3 GetApproachDirection(Vector3 unitPosition, Vector3 targetPosition)
        {
            Vector3 flatTarget = targetPosition;
            flatTarget.y = unitPosition.y;

            List<Vector3> slots = BuildSlots(targetPosition);
            if (slots.Count == 0)
            {
                Vector3 fallback = flatTarget - unitPosition;
                fallback.y = 0f;
                return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.zero;
            }

            Vector3 bestSlot = Vector3.zero;
            float bestScore = float.MaxValue;

            for (int i = 0; i < slots.Count; i++)
            {
                Vector3 toSlot = slots[i] - unitPosition;
                toSlot.y = 0f;
                float score = toSlot.sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestSlot = slots[i];
                }
            }

            Vector3 dir = bestSlot - unitPosition;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        public List<Vector3> BuildSlots(Vector3 targetPosition)
        {
            List<Vector3> slots = new List<Vector3>(slotCount);
            if (slotCount <= 0)
                return slots;

            for (int i = 0; i < slotCount; i++)
            {
                float angle = (360f / slotCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * slotRadius;
                slots.Add(targetPosition + offset);
            }

            return slots;
        }
    }
}

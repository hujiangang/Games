using UnityEngine;

namespace CRCrowdPrototype
{
    /// <summary>
    /// 群体求解器.
    /// 让一群单位不重叠、不拥挤、不穿模的计算工具.
    /// </summary>
    public static class CrowdSolver
    {
        private static readonly Collider[] buffer = new Collider[64];

        public static Vector3 GetSeparationDirection(Vector3 position, float selfRadius, float searchRadius, Transform self)
        {
            int count = Physics.OverlapSphereNonAlloc(position, searchRadius, buffer);
            if (count <= 0)
                return Vector3.zero;

            Vector3 force = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider other = buffer[i];
                if (other == null)
                    continue;

                if (self != null && other.transform == self)
                    continue;

                Vector3 otherPos = other.transform.position;
                Vector3 delta = position - otherPos;
                delta.y = 0f;

                float dist = delta.magnitude;
                if (dist < 0.0001f)
                    continue;

                float otherRadius = GetRadius(other);
                float minDist = selfRadius + otherRadius;
                if (dist >= minDist)
                    continue;

                float overlap = minDist - dist;
                force += (delta / dist) * overlap;
            }

            if (force.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return force.normalized;
        }

        public static Vector3 ApplyPushBack(Vector3 position, float selfRadius, Transform self, float searchRadius)
        {
            int count = Physics.OverlapSphereNonAlloc(position, searchRadius, buffer);
            if (count <= 0)
                return position;

            Vector3 correction = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider other = buffer[i];
                if (other == null)
                    continue;

                if (self != null && other.transform == self)
                    continue;

                Vector3 delta = position - other.transform.position;
                delta.y = 0f;

                float dist = delta.magnitude;
                if (dist < 0.0001f)
                    continue;

                float otherRadius = GetRadius(other);
                float minDist = selfRadius + otherRadius;
                if (dist >= minDist)
                    continue;

                float overlap = minDist - dist;
                correction += (delta / dist) * overlap;
            }

            return position + correction * 0.5f;
        }

        private static float GetRadius(Collider col)
        {
            if (col.TryGetComponent(out UnitCrowdAgent agent))
                return agent.Radius;

            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }
    }
}

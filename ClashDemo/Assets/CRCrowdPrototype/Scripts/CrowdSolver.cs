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
        private static readonly UnitCrowdAgent[] agentBuffer = new UnitCrowdAgent[64];

        public static Vector3 GetSeparationDirection(Vector3 position, float selfRadius, float searchRadius, Transform self)
        {
            Transform selfRoot = self != null ? self.root : null;
            int count = Physics.OverlapSphereNonAlloc(position, searchRadius, buffer);
            if (count <= 0)
                return Vector3.zero;

            Vector3 force = Vector3.zero;
            int uniqueAgentCount = 0;

            for (int i = 0; i < count; i++)
            {
                Collider other = buffer[i];
                if (other == null)
                    continue;

                UnitCrowdAgent otherAgent = other.GetComponentInParent<UnitCrowdAgent>();
                if (otherAgent == null)
                    continue;

                if (selfRoot != null && otherAgent.transform.root == selfRoot)
                    continue;

                bool alreadyAdded = false;
                for (int j = 0; j < uniqueAgentCount; j++)
                {
                    if (agentBuffer[j] == otherAgent)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded)
                    continue;

                if (uniqueAgentCount < agentBuffer.Length)
                    agentBuffer[uniqueAgentCount++] = otherAgent;

                Vector3 otherPos = otherAgent.transform.position;
                Vector3 delta = position - otherPos;
                delta.y = 0f;

                float dist = delta.magnitude;
                if (dist < 0.0001f)
                    continue;

                float otherRadius = otherAgent.Radius;
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
            Transform selfRoot = self != null ? self.root : null;
            int count = Physics.OverlapSphereNonAlloc(position, searchRadius, buffer);
            if (count <= 0)
                return position;

            Vector3 correction = Vector3.zero;
            int uniqueAgentCount = 0;

            for (int i = 0; i < count; i++)
            {
                Collider other = buffer[i];
                if (other == null)
                    continue;

                UnitCrowdAgent otherAgent = other.GetComponentInParent<UnitCrowdAgent>();
                if (otherAgent == null)
                    continue;

                if (selfRoot != null && otherAgent.transform.root == selfRoot)
                    continue;

                bool alreadyAdded = false;
                for (int j = 0; j < uniqueAgentCount; j++)
                {
                    if (agentBuffer[j] == otherAgent)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded)
                    continue;

                if (uniqueAgentCount < agentBuffer.Length)
                    agentBuffer[uniqueAgentCount++] = otherAgent;

                Vector3 delta = position - otherAgent.transform.position;
                delta.y = 0f;

                float dist = delta.magnitude;
                if (dist < 0.0001f)
                    continue;

                float otherRadius = otherAgent.Radius;
                float minDist = selfRadius + otherRadius;
                if (dist >= minDist)
                    continue;

                float overlap = minDist - dist;
                correction += (delta / dist) * overlap;
            }

            return position + correction * 0.5f;
        }
    }
}

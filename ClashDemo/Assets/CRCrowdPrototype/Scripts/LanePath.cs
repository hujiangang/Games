using UnityEngine;

namespace CRCrowdPrototype
{
    public class LanePath : MonoBehaviour
    {
        [Header("路线设置")]
        public LaneSide side;
        public Transform[] waypoints;

        // 路线上路点数量
        public int Count => waypoints != null ? waypoints.Length : 0;

        public int GetClosestWaypointIndex(Vector3 position)
        {
            if (waypoints == null || waypoints.Length == 0)
                return -1;

            float bestDist = float.MaxValue;
            int bestIndex = 0;

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = Vector3.SqrMagnitude(position - waypoints[i].position);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        // 根据单位当前所在线段，返回它接下来应该前往的路点索引。
        public int GetTargetWaypointIndex(Vector3 position)
        {
            if (waypoints == null || waypoints.Length == 0)
                return -1;

            if (waypoints.Length == 1)
                return 0;

            float bestDistance = float.MaxValue;
            int bestSegmentStartIndex = 0;

            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null)
                    continue;

                Vector3 segmentStart = waypoints[i].position;
                Vector3 segmentEnd = waypoints[i + 1].position;
                Vector3 segment = segmentEnd - segmentStart;
                float segmentLengthSqr = segment.sqrMagnitude;

                if (segmentLengthSqr <= 0.0001f)
                    continue;

                float t = Mathf.Clamp01(Vector3.Dot(position - segmentStart, segment) / segmentLengthSqr);
                Vector3 closestPoint = segmentStart + segment * t;
                float distance = Vector3.SqrMagnitude(position - closestPoint);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSegmentStartIndex = i;
                }
            }

            return Mathf.Clamp(bestSegmentStartIndex + 1, 1, waypoints.Length - 1);
        }

        public Vector3 GetWaypointPosition(int index)
        {
            if (waypoints == null || waypoints.Length == 0)
                return transform.position;

            index = Mathf.Clamp(index, 0, waypoints.Length - 1);
            if (waypoints[index] == null)
                return transform.position;

            return waypoints[index].position;
        }

        // 计算一个点到整条路线的最近距离平方，用于选择更接近的 lane。
        public float GetClosestDistanceSqr(Vector3 position)
        {
            if (waypoints == null || waypoints.Length == 0)
                return float.MaxValue;

            if (waypoints.Length == 1 || waypoints[0] == null)
                return Vector3.SqrMagnitude(position - GetWaypointPosition(0));

            float bestDistance = float.MaxValue;

            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] == null || waypoints[i + 1] == null)
                    continue;

                Vector3 start = waypoints[i].position;
                Vector3 end = waypoints[i + 1].position;
                Vector3 segment = end - start;
                float lengthSqr = segment.sqrMagnitude;

                if (lengthSqr <= 0.0001f)
                    continue;

                float t = Mathf.Clamp01(Vector3.Dot(position - start, segment) / lengthSqr);
                Vector3 closestPoint = start + segment * t;
                float distance = Vector3.SqrMagnitude(position - closestPoint);
                if (distance < bestDistance)
                    bestDistance = distance;
            }

            return bestDistance;
        }

        public int GetNextWaypointIndex(int index)
        {
            if (waypoints == null || waypoints.Length == 0)
                return -1;

            return Mathf.Min(index + 1, waypoints.Length - 1);
        }

        public Vector3 GetForwardPoint(Vector3 position, float lookAhead = 2f)
        {
            if (waypoints == null || waypoints.Length == 0)
                return position;

            int targetIndex = GetTargetWaypointIndex(position);
            if (targetIndex < 0)
                return position;

            int previousIndex = Mathf.Max(targetIndex - 1, 0);
            Vector3 origin = waypoints[previousIndex].position;
            Vector3 target = waypoints[targetIndex].position;

            Vector3 dir = (target - origin).normalized;
            return position + dir * lookAhead;
        }

        public Vector3 GetLaneDirection(Vector3 position)
        {
            if (waypoints == null || waypoints.Length < 2)
                return side == LaneSide.Left ? Vector3.forward : Vector3.back;

            int targetIndex = GetTargetWaypointIndex(position);
            if (targetIndex < 0)
                return side == LaneSide.Left ? Vector3.forward : Vector3.back;

            int previousIndex = Mathf.Max(targetIndex - 1, 0);
            Vector3 dir = waypoints[targetIndex].position - waypoints[previousIndex].position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : (side == LaneSide.Left ? Vector3.forward : Vector3.back);
        }
    }
}

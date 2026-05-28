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

        public Vector3 GetWaypointPosition(int index)
        {
            if (waypoints == null || waypoints.Length == 0)
                return transform.position;

            index = Mathf.Clamp(index, 0, waypoints.Length - 1);
            if (waypoints[index] == null)
                return transform.position;

            return waypoints[index].position;
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

            int index = GetClosestWaypointIndex(position);
            if (index < 0)
                return position;

            int nextIndex = Mathf.Min(index + 1, waypoints.Length - 1);
            Vector3 origin = waypoints[index].position;
            Vector3 target = waypoints[nextIndex].position;

            Vector3 dir = (target - origin).normalized;
            return position + dir * lookAhead;
        }

        public Vector3 GetLaneDirection(Vector3 position)
        {
            if (waypoints == null || waypoints.Length < 2)
                return side == LaneSide.Left ? Vector3.forward : Vector3.back;

            int index = GetClosestWaypointIndex(position);
            if (index < 0)
                return side == LaneSide.Left ? Vector3.forward : Vector3.back;

            int nextIndex = Mathf.Min(index + 1, waypoints.Length - 1);
            Vector3 dir = waypoints[nextIndex].position - waypoints[index].position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : (side == LaneSide.Left ? Vector3.forward : Vector3.back);
        }
    }
}

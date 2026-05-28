using UnityEngine;

namespace CRCrowdPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public class UnitCrowdAgent : MonoBehaviour
    {
        [Header("兵种数据")]
        public UnitData unitData;
        [Tooltip("该单位在没有配置 `UnitData` 时使用的默认移速。")]
        public float fallbackMoveSpeed = 3.5f;
        [Tooltip("该单位在没有配置 `UnitData` 时使用的默认攻击距离。")]
        public float fallbackAttackRange = 1.05f;
        [Tooltip("该单位在没有配置 `UnitData` 时使用的默认攻击间隔。")]
        public float fallbackAttackCooldown = 1f;

        [Header("运行时")]
        public LanePath lane;
        public Transform currentTarget;
        public UnitState state = UnitState.March;

        [Tooltip("可选：目标周围的接近点，用来减少单位挤在同一处。")]
        public AttackSlotProvider slotProvider;

        private CharacterController controller;
        private float attackTimer;
        private Vector3 velocity;
        private int currentWaypointIndex = -1;

        public float Radius
        {
            get
            {
                if (unitData != null && unitData.crowdConfig != null)
                    return unitData.crowdConfig.radius;

                return 0.35f;
            }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (lane == null)
                return;

            EnsureWaypointProgress();
            attackTimer -= Time.deltaTime;

            ResolveState();

            Vector3 moveDir = BuildMoveDirection();
            moveDir.y = 0f;

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Vector3 desired = moveDir.normalized * GetMoveSpeed();
                velocity = Vector3.Lerp(velocity, desired, GetTurnSpeed() * Time.deltaTime);
                controller.Move(velocity * Time.deltaTime);
                FaceDirection(velocity);
            }
            else
            {
                velocity = Vector3.Lerp(velocity, Vector3.zero, GetTurnSpeed() * Time.deltaTime);
            }
        }

        private void ResolveState()
        {
            if (currentTarget == null)
            {
                state = UnitState.March;
                return;
            }

            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance <= GetAttackRange())
            {
                state = UnitState.Attack;
            }
            else if (distance <= GetAttackRange() * 3f)
            {
                state = UnitState.Chase;
            }
            else
            {
                state = UnitState.March;
            }
        }

        private Vector3 BuildMoveDirection()
        {
            UnitCrowdConfig crowdConfig = GetCrowdConfig();
            Vector3 laneDir = GetLaneMoveDirection();
            Vector3 targetDir = Vector3.zero;
            Vector3 separationDir = CrowdSolver.GetSeparationDirection(transform.position, crowdConfig.radius, crowdConfig.separationRadius, transform);
            Vector3 slotDir = Vector3.zero;

            if (currentTarget != null)
            {
                Vector3 toTarget = currentTarget.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    targetDir = toTarget.normalized;

                if (slotProvider != null)
                    slotDir = slotProvider.GetApproachDirection(transform.position, currentTarget.position);
            }

            if (state == UnitState.Attack)
            {
                if (currentTarget != null && attackTimer <= 0f)
                {
                    attackTimer = GetAttackCooldown();
                    // 这里接上实际攻击逻辑。
                }

                // 贴着目标移动，但保留少量分离，避免单位完全叠在一起。
                return (laneDir * 0.2f) + (separationDir * crowdConfig.separationWeight * 0.7f);
            }

            Vector3 move = Vector3.zero;
            move += laneDir * crowdConfig.laneWeight;
            move += targetDir * crowdConfig.targetWeight;
            move += slotDir * 0.85f;
            move += separationDir * crowdConfig.separationWeight;

            if (move.sqrMagnitude < 0.0001f)
                move = laneDir;

            return move;
        }

        private void EnsureWaypointProgress()
        {
            UnitCrowdConfig crowdConfig = GetCrowdConfig();

            if (lane == null || lane.Count == 0)
                return;

            if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = lane.GetClosestWaypointIndex(transform.position);
                currentWaypointIndex = Mathf.Max(currentWaypointIndex, 0);
            }


            Debug.Log($"Unit {gameObject.name} at waypoint index {currentWaypointIndex} on lane {lane.name}");
            Vector3 waypoint = lane.GetWaypointPosition(currentWaypointIndex);
            waypoint.y = transform.position.y;

            float distance = Vector3.Distance(transform.position, waypoint);
            if (distance <= Mathf.Max(crowdConfig.reachThreshold, 0.35f))
            {
                currentWaypointIndex = lane.GetNextWaypointIndex(currentWaypointIndex);
            }
        }

        private Vector3 GetLaneMoveDirection()
        {
            if (lane == null || lane.Count == 0)
                return Vector3.zero;

            if (currentWaypointIndex < 0)
                currentWaypointIndex = Mathf.Max(lane.GetClosestWaypointIndex(transform.position), 0);

            Vector3 waypoint = lane.GetWaypointPosition(currentWaypointIndex);
            Vector3 toWaypoint = waypoint - transform.position;
            toWaypoint.y = 0f;

            if (toWaypoint.sqrMagnitude <= 0.0001f)
            {
                int nextIndex = lane.GetNextWaypointIndex(currentWaypointIndex);
                Vector3 nextWaypoint = lane.GetWaypointPosition(nextIndex);
                Vector3 toNext = nextWaypoint - transform.position;
                toNext.y = 0f;
                return toNext.sqrMagnitude > 0.0001f ? toNext.normalized : Vector3.zero;
            }

            return toWaypoint.normalized;
        }

        public void Initialize(LanePath assignedLane, Transform assignedTarget, UnitData assignedUnitData = null)
        {
            Debug.Log($"Initializing unit {gameObject.name} with lane {assignedLane?.name}, unitData {assignedUnitData?.unitName}");
            lane = assignedLane;
            currentTarget = assignedTarget;

            if (assignedUnitData != null)
                unitData = assignedUnitData;

            if (assignedTarget != null)
                slotProvider = assignedTarget.GetComponent<AttackSlotProvider>();

            currentWaypointIndex = -1;
            EnsureWaypointProgress();
        }

        private float GetMoveSpeed()
        {
            if (unitData != null && unitData.moveSpeed > 0f)
                return unitData.moveSpeed;

            return fallbackMoveSpeed;
        }

        private float GetAttackRange()
        {
            if (unitData != null && unitData.attackRange > 0f)
                return unitData.attackRange;

            return fallbackAttackRange;
        }

        private float GetAttackCooldown()
        {
            if (unitData != null && unitData.attackSpeed > 0f)
                return 1f / unitData.attackSpeed;

            return fallbackAttackCooldown;
        }

        private float GetTurnSpeed()
        {
            return GetCrowdConfig().turnSpeed;
        }

        private UnitCrowdConfig GetCrowdConfig()
        {
            if (unitData != null && unitData.crowdConfig != null)
                return unitData.crowdConfig;

            return new UnitCrowdConfig();
        }

        private void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, GetTurnSpeed() * Time.deltaTime);
        }
    }
}

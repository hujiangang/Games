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
            Vector3 lateralSeparationDir = GetLateralSeparationDirection(separationDir, laneDir);
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

            // 行军阶段只沿路线前进，不直接朝目标抄近路。
            if (state == UnitState.March)
            {
                Vector3 marchMove = Vector3.zero;
                marchMove += laneDir * crowdConfig.laneWeight;
                marchMove += lateralSeparationDir * crowdConfig.separationWeight;

                if (marchMove.sqrMagnitude < 0.0001f)
                    marchMove = laneDir;

                return marchMove;
            }

            // 进入追击阶段后，再逐步把目标方向和占位方向叠加进来。
            if (state == UnitState.Chase)
            {
                Vector3 chaseMove = Vector3.zero;
                chaseMove += laneDir * crowdConfig.laneWeight * 0.6f;
                chaseMove += targetDir * crowdConfig.targetWeight;
                chaseMove += slotDir * 0.85f;
                chaseMove += lateralSeparationDir * crowdConfig.separationWeight;

                if (chaseMove.sqrMagnitude < 0.0001f)
                    chaseMove = laneDir;

                return chaseMove;
            }

            if (state == UnitState.Attack)
            {
                if (currentTarget != null && attackTimer <= 0f)
                {
                    attackTimer = GetAttackCooldown();
                    // 这里接上实际攻击逻辑。
                }

                // 贴着目标移动，但保留少量分离，避免单位完全叠在一起。
                Vector3 attackMove = Vector3.zero;
                attackMove += targetDir * crowdConfig.attackStickiness;
                attackMove += slotDir * 0.6f;
                attackMove += separationDir * crowdConfig.separationWeight * 0.7f;

                if (attackMove.sqrMagnitude < 0.0001f)
                    attackMove = laneDir * 0.2f;

                return attackMove;
            }

            return laneDir;
        }

        private void EnsureWaypointProgress()
        {
            UnitCrowdConfig crowdConfig = GetCrowdConfig();

            if (lane == null || lane.Count == 0)
                return;

            if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = lane.GetTargetWaypointIndex(transform.position);
                currentWaypointIndex = Mathf.Max(currentWaypointIndex, 0);
            }
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
                currentWaypointIndex = Mathf.Max(lane.GetTargetWaypointIndex(transform.position), 0);

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

        // 分离只负责横向挪位，不参与前后推进，避免把整队拉成斜线。
        private Vector3 GetLateralSeparationDirection(Vector3 separationDir, Vector3 laneDir)
        {
            separationDir.y = 0f;
            if (separationDir.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            laneDir.y = 0f;
            if (laneDir.sqrMagnitude < 0.0001f)
                return separationDir.normalized;

            Vector3 lateral = Vector3.ProjectOnPlane(separationDir, laneDir.normalized);
            lateral.y = 0f;

            if (lateral.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return lateral.normalized;
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

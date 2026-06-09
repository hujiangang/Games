using UnityEngine;
using CRCrowdPrototype;
using DotRecast.Core.Numerics;
using DotRecast.Detour.Crowd;

[RequireComponent(typeof(Transform))]
public class UnitAgent : MonoBehaviour
{
    internal static readonly System.Collections.Generic.List<UnitAgent> ActiveAgents = new System.Collections.Generic.List<UnitAgent>(128);

    [Header("目标：敌方防御塔/国王塔")]
    public Transform TargetTower;

    [Header("兵种数据")]
    public UnitData unitData;

    [Header("兜底参数")]
    public float fallbackMoveSpeed = 3.5f;
    public float fallbackAcceleration = 18f;
    public float fallbackAttackRange = 1.05f;
    public float fallbackAttackCooldown = 1f;
    public float fallbackRadius = 0.35f;
    public float fallbackHeight = 2f;
    public float repathInterval = 0.35f;

    public enum UnitAgentState
    {
        Idle,
        March,
        Attack
    }

    [Header("运行时")]
    public UnitAgentState state = UnitAgentState.Idle;

    private DtCrowdAgent _crowdAgent;
    private CharacterController _characterController;
    private Rigidbody _rigidbody;
    private Animator _animator;
    private float _attackTimer;
    private float _repathTimer;
    private Vector3 _lastRequestedTarget;
    private Vector3 _pushOffset;
    private Vector3 _priorityOffset;
    private Vector3 _lastCrowdPosition;
    private Vector3 _lastPushSamplePosition;
    private float _laneHoldPressure;
    private bool _hasLastCrowdPosition;
    private bool _hasLastPushSamplePosition;
    private bool _initialized;
    private bool _usesDotRecast;
    private bool _movementStopped;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (!ActiveAgents.Contains(this))
            ActiveAgents.Add(this);
    }

    private void OnDisable()
    {
        ActiveAgents.Remove(this);
        _hasLastCrowdPosition = false;
        _hasLastPushSamplePosition = false;
        _pushOffset = Vector3.zero;
        _priorityOffset = Vector3.zero;
        _laneHoldPressure = 0f;
    }

    private void Start()
    {
        TryInitialize();
    }

    public void Initialize(Transform targetTower, UnitData assignedUnitData = null)
    {
        TargetTower = targetTower;
        if (assignedUnitData != null)
            unitData = assignedUnitData;

        TryInitialize();
    }

    private void Update()
    {
        if (!_initialized)
            TryInitialize();

        if (!_initialized || TargetTower == null)
            return;

        _attackTimer -= Time.deltaTime;

        if (_usesDotRecast)
            UpdateGroundUnit();
        else
            UpdateAirUnit();
    }

    private void TryInitialize()
    {
        if (_initialized || TargetTower == null)
            return;

        FreezeRuntimePhysics();

        if (IsAirUnit())
        {
            _usesDotRecast = false;
            _initialized = true;
            state = UnitAgentState.March;
            return;
        }

        DtCrowdAgentParams agentParams = BuildCrowdAgentParams();
        if (!NavMeshManager.Instance.TryRegisterAgent(transform.position, agentParams, out _crowdAgent))
        {
            Debug.LogError($"UnitAgent 初始化失败：{name} 无法注册到 DotRecast Crowd。");
            return;
        }

        _usesDotRecast = true;
        _initialized = true;
        state = UnitAgentState.March;
        RequestTarget(force: true);
    }

    private void UpdateGroundUnit()
    {
        if (_crowdAgent == null)
            return;

        Vector3 crowdPosition = NavMeshManager.Instance.GetAgentPosition(_crowdAgent);
        UnitCrowdSnapshot massSnapshot = BuildRuntimeSnapshot(crowdPosition, crowdPosition + _pushOffset, crowdPosition + _pushOffset + _priorityOffset);
        UpdatePushOffset(massSnapshot);
        Vector3 pushSamplePosition = crowdPosition + _pushOffset;
        UnitCrowdSnapshot prioritySnapshot = BuildRuntimeSnapshot(crowdPosition, pushSamplePosition, pushSamplePosition + _priorityOffset);
        UpdatePriorityOffset(prioritySnapshot);

        Vector3 displayPosition = pushSamplePosition + _priorityOffset;
        UnitCrowdSnapshot movementSnapshot = BuildRuntimeSnapshot(crowdPosition, pushSamplePosition, displayPosition);
        transform.position = displayPosition;
        _lastCrowdPosition = crowdPosition;
        _lastPushSamplePosition = pushSamplePosition;
        _hasLastCrowdPosition = true;
        _hasLastPushSamplePosition = true;

        float sqrDistance = GetHorizontalDistanceSqr(TargetTower.position);
        float attackRange = GetAttackRange();
        if (sqrDistance <= attackRange * attackRange)
        {
            state = UnitAgentState.Attack;
            if (!_movementStopped)
            {
                NavMeshManager.Instance.StopAgent(_crowdAgent);
                _movementStopped = true;
            }

            FaceTowards(TargetTower.position - transform.position);
            HandleAttack();
            return;
        }

        state = UnitAgentState.March;
        _movementStopped = false;

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f || (TargetTower.position - _lastRequestedTarget).sqrMagnitude > 0.0625f)
            RequestTarget(force: false);

        FaceTowards(ResolveGroundFacingDirection(movementSnapshot));
    }

    private void UpdateAirUnit()
    {
        Vector3 toTarget = TargetTower.position - transform.position;
        toTarget.y = 0f;

        float attackRange = GetAttackRange();
        if (toTarget.sqrMagnitude <= attackRange * attackRange)
        {
            state = UnitAgentState.Attack;
            FaceTowards(toTarget);
            HandleAttack();
            return;
        }

        state = UnitAgentState.March;
        Vector3 moveDir = toTarget.normalized;
        transform.position += moveDir * GetMoveSpeed() * Time.deltaTime;
        FaceTowards(moveDir);
    }

    private void HandleAttack()
    {
        if (_attackTimer > 0f)
            return;

        _attackTimer = GetAttackCooldown();
        if (_animator != null && HasAnimatorParameter("Attack", AnimatorControllerParameterType.Trigger))
            _animator.SetTrigger("Attack");
    }

    private void RequestTarget(bool force)
    {
        if (TargetTower == null || _crowdAgent == null)
            return;

        if (!force && _repathTimer > 0f)
            return;

        if (NavMeshManager.Instance.TryRequestMoveTarget(_crowdAgent, TargetTower.position))
        {
            _lastRequestedTarget = TargetTower.position;
            _repathTimer = repathInterval;
        }
    }

    private DtCrowdAgentParams BuildCrowdAgentParams()
    {
        int updateFlags =
            DtCrowdAgentUpdateFlags.DT_CROWD_ANTICIPATE_TURNS |
            DtCrowdAgentUpdateFlags.DT_CROWD_OBSTACLE_AVOIDANCE |
            DtCrowdAgentUpdateFlags.DT_CROWD_SEPARATION |
            DtCrowdAgentUpdateFlags.DT_CROWD_OPTIMIZE_VIS |
            DtCrowdAgentUpdateFlags.DT_CROWD_OPTIMIZE_TOPO;

        float radius = GetAgentRadius();
        return new DtCrowdAgentParams
        {
            radius = radius,
            height = GetAgentHeight(),
            maxAcceleration = GetAcceleration(),
            maxSpeed = GetMoveSpeed(),
            collisionQueryRange = GetCollisionQueryRange(radius),
            pathOptimizationRange = GetPathOptimizationRange(radius),
            separationWeight = GetSeparationWeight(),
            obstacleAvoidanceType = GetObstacleAvoidanceType(),
            queryFilterType = 0,
            updateFlags = updateFlags,
            userData = this
        };
    }

    private void FreezeRuntimePhysics()
    {
        if (_characterController != null)
            _characterController.enabled = false;

        if (_rigidbody != null)
        {
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private bool IsAirUnit()
    {
        return unitData != null && unitData.unitType == UnitType.Air;
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

    private float GetAcceleration()
    {
        float moveSpeed = GetMoveSpeed();
        if (unitData != null && unitData.crowdConfig != null)
        {
            float mass = Mathf.Max(0.75f, unitData.crowdConfig.mass);
            return Mathf.Max(moveSpeed * 5f, fallbackAcceleration / mass);
        }

        return Mathf.Max(moveSpeed * 5f, fallbackAcceleration);
    }

    private float GetAgentRadius()
    {
        if (unitData != null && unitData.crowdConfig != null && unitData.crowdConfig.radius > 0f)
            return unitData.crowdConfig.radius;

        return fallbackRadius;
    }

    private float GetAgentHeight()
    {
        if (_characterController != null && _characterController.height > 0f)
            return _characterController.height;

        return fallbackHeight;
    }

    private float GetSeparationWeight()
    {
        if (unitData != null && unitData.crowdConfig != null)
            return Mathf.Clamp(unitData.crowdConfig.separationWeight, 0.2f, 1.1f);

        return 0.8f;
    }

    private float GetSeparationRadius()
    {
        if (unitData != null && unitData.crowdConfig != null && unitData.crowdConfig.separationRadius > 0f)
            return unitData.crowdConfig.separationRadius;

        return Mathf.Max(GetAgentRadius() * 2.2f, 0.75f);
    }

    private float GetCollisionQueryRange(float radius)
    {
        float separationRadius = GetSeparationRadius();
        return Mathf.Max(separationRadius, radius * 3.5f);
    }

    private float GetPathOptimizationRange(float radius)
    {
        return Mathf.Max(GetSeparationRadius() * 2f, radius * 8f);
    }

    private int GetObstacleAvoidanceType()
    {
        if (unitData != null && unitData.crowdConfig != null)
        {
            if (unitData.crowdConfig.mass >= 1.75f)
                return 0;

            if (GetAttackRange() >= 4f)
                return 2;
        }

        return 1;
    }

    private float GetHorizontalDistanceSqr(Vector3 otherPosition)
    {
        Vector3 delta = otherPosition - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude;
    }

    private void FaceTowards(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 12f * Time.deltaTime);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (_animator == null)
            return false;

        AnimatorControllerParameter[] parameters = _animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == parameterType && parameters[i].name == parameterName)
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        if (_crowdAgent == null)
            return;

        if (NavMeshManager.TryGetExistingInstance(out NavMeshManager navMeshManager))
            navMeshManager.RemoveAgent(_crowdAgent);

        _crowdAgent = null;
        _pushOffset = Vector3.zero;
        _priorityOffset = Vector3.zero;
        _laneHoldPressure = 0f;
        _hasLastCrowdPosition = false;
        _hasLastPushSamplePosition = false;
    }

    private void UpdatePushOffset(UnitCrowdSnapshot selfSnapshot)
    {
        if (!CanParticipateInMassPush())
        {
            _pushOffset = Vector3.Lerp(_pushOffset, Vector3.zero, UnitCrowdInteractionTuning.PushOffsetRecoveryLerp * Time.deltaTime);
            return;
        }

        Vector3 desiredOffset = UnitCrowdInteractionSolver.ComputeMassPushOffset(selfSnapshot, ActiveAgents);
        float lerpSpeed = desiredOffset.sqrMagnitude > _pushOffset.sqrMagnitude
            ? UnitCrowdInteractionTuning.PushOffsetLerp
            : UnitCrowdInteractionTuning.PushOffsetRecoveryLerp;
        _pushOffset = Vector3.Lerp(_pushOffset, desiredOffset, lerpSpeed * Time.deltaTime);
    }

    private void UpdatePriorityOffset(UnitCrowdSnapshot selfSnapshot)
    {
        if (!CanParticipateInMassPush())
        {
            _laneHoldPressure = 0f;
            _priorityOffset = Vector3.Lerp(_priorityOffset, Vector3.zero, UnitCrowdInteractionTuning.PriorityOffsetRecoveryLerp * Time.deltaTime);
            return;
        }

        UnitCrowdPriorityResult result = UnitCrowdInteractionSolver.ComputePriority(
            selfSnapshot,
            _lastCrowdPosition,
            _hasLastCrowdPosition,
            ActiveAgents);

        _laneHoldPressure = result.LaneHoldPressure;
        float lerpSpeed = result.Offset.sqrMagnitude > _priorityOffset.sqrMagnitude
            ? UnitCrowdInteractionTuning.PriorityOffsetLerp
            : UnitCrowdInteractionTuning.PriorityOffsetRecoveryLerp;
        _priorityOffset = Vector3.Lerp(_priorityOffset, result.Offset, lerpSpeed * Time.deltaTime);
    }

    private Vector3 GetPushIntent(Vector3 referencePosition)
    {
        if (_usesDotRecast && _crowdAgent != null && NavMeshManager.TryGetExistingInstance(out NavMeshManager navMeshManager))
        {
            Vector3 velocity = navMeshManager.GetAgentVelocity(_crowdAgent);
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.0001f)
                return velocity.normalized;
        }

        if (TargetTower == null)
            return Vector3.zero;

        Vector3 toTarget = TargetTower.position - referencePosition;
        toTarget.y = 0f;
        return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;
    }

    private bool CanParticipateInMassPush()
    {
        return _initialized && _usesDotRecast && _crowdAgent != null && !IsAirUnit();
    }

    internal bool TryBuildCrowdSnapshot(out UnitCrowdSnapshot snapshot)
    {
        if (!CanParticipateInMassPush())
        {
            snapshot = default;
            return false;
        }

        Vector3 crowdPosition = _hasLastCrowdPosition
            ? _lastCrowdPosition
            : transform.position - _pushOffset - _priorityOffset;
        Vector3 pushSamplePosition = GetPushSamplePosition();
        snapshot = BuildRuntimeSnapshot(crowdPosition, pushSamplePosition, pushSamplePosition + _priorityOffset);
        return true;
    }

    private UnitCrowdSnapshot BuildRuntimeSnapshot(Vector3 crowdPosition, Vector3 pushSamplePosition, Vector3 displayPosition)
    {
        return new UnitCrowdSnapshot(
            this,
            crowdPosition,
            pushSamplePosition,
            displayPosition,
            GetPushIntent(pushSamplePosition),
            GetAgentRadius(),
            GetMass());
    }

    private Vector3 ResolveGroundFacingDirection(UnitCrowdSnapshot selfSnapshot)
    {
        Vector3 crowdVelocity = NavMeshManager.Instance.GetAgentVelocity(_crowdAgent);
        Vector3 facingDirection = UnitCrowdInteractionSolver.ResolveFacingDirection(selfSnapshot, crowdVelocity, _laneHoldPressure);
        if (facingDirection.sqrMagnitude > 0.0001f)
            return facingDirection;

        return TargetTower != null ? TargetTower.position - selfSnapshot.DisplayPosition : Vector3.zero;
    }

    private Vector3 GetPushSamplePosition()
    {
        return _hasLastPushSamplePosition ? _lastPushSamplePosition : transform.position - _priorityOffset;
    }

    private float GetMass()
    {
        if (unitData != null && unitData.crowdConfig != null)
            return Mathf.Max(0.5f, unitData.crowdConfig.mass);

        return 1f;
    }
}

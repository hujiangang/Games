using UnityEngine;
using CRCrowdPrototype;
using DotRecast.Core.Numerics;
using DotRecast.Detour.Crowd;

[RequireComponent(typeof(Transform))]
public class UnitAgent : MonoBehaviour
{
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
    private bool _initialized;
    private bool _usesDotRecast;
    private bool _movementStopped;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
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

        transform.position = NavMeshManager.Instance.GetAgentPosition(_crowdAgent);

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

        Vector3 velocity = NavMeshManager.Instance.GetAgentVelocity(_crowdAgent);
        if (velocity.sqrMagnitude <= 0.0001f)
            velocity = TargetTower.position - transform.position;

        FaceTowards(velocity);
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
            maxAcceleration = fallbackAcceleration,
            maxSpeed = GetMoveSpeed(),
            collisionQueryRange = radius * 12f,
            pathOptimizationRange = radius * 30f,
            separationWeight = GetSeparationWeight(),
            obstacleAvoidanceType = 3,
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
            return Mathf.Max(0.5f, unitData.crowdConfig.separationWeight);

        return 1.5f;
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
        if (_crowdAgent != null)
            NavMeshManager.Instance.RemoveAgent(_crowdAgent);
    }
}

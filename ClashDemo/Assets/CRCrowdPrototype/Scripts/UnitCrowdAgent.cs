using System.Collections.Generic;
using UnityEngine;

namespace CRCrowdPrototype
{
    [RequireComponent(typeof(CharacterController))]
    public class UnitCrowdAgent : MonoBehaviour
    {
        [Header("Config")]
        public UnitCrowdConfig config = new UnitCrowdConfig();

        [Header("Runtime")]
        public LanePath lane;
        public Transform currentTarget;
        public UnitState state = UnitState.March;

        [Tooltip("Optional target slots around towers or blockers.")]
        public AttackSlotProvider slotProvider;

        private CharacterController controller;
        private float attackTimer;
        private Vector3 velocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (lane == null)
                return;

            attackTimer -= Time.deltaTime;

            ResolveState();

            Vector3 moveDir = BuildMoveDirection();
            moveDir.y = 0f;

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Vector3 desired = moveDir.normalized * config.moveSpeed;
                velocity = Vector3.Lerp(velocity, desired, config.turnSpeed * Time.deltaTime);
                controller.Move(velocity * Time.deltaTime);
                FaceDirection(velocity);
            }
            else
            {
                velocity = Vector3.Lerp(velocity, Vector3.zero, config.turnSpeed * Time.deltaTime);
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
            if (distance <= config.attackRange)
            {
                state = UnitState.Attack;
            }
            else if (distance <= config.attackRange * 3f)
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
            Vector3 laneDir = lane.GetLaneDirection(transform.position);
            Vector3 targetDir = Vector3.zero;
            Vector3 separationDir = CrowdSolver.GetSeparationDirection(transform.position, config.radius, config.separationRadius, transform);
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
                    attackTimer = config.attackCooldown;
                    // Hook your attack event here.
                }

                // Stay sticky but keep small local separation so units do not fully merge.
                return (laneDir * 0.2f) + (separationDir * config.separationWeight * 0.7f);
            }

            Vector3 move = Vector3.zero;
            move += laneDir * config.laneWeight;
            move += targetDir * config.targetWeight;
            move += slotDir * 0.85f;
            move += separationDir * config.separationWeight;

            if (move.sqrMagnitude < 0.0001f)
                move = laneDir;

            return move;
        }

        private void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, config.turnSpeed * Time.deltaTime);
        }
    }
}
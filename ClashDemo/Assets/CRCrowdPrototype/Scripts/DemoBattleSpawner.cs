using UnityEngine;

namespace CRCrowdPrototype
{
    public class DemoBattleSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        public UnitCrowdAgent blueUnitPrefab;
        public UnitCrowdAgent redUnitPrefab;

        [Header("Lanes")]
        public LanePath blueLane;
        public LanePath redLane;

        [Header("Spawn Points")]
        public Transform blueSpawnPoint;
        public Transform redSpawnPoint;

        [Header("Targets")]
        public Transform blueTarget;
        public Transform redTarget;

        [Header("Demo Settings")]
        public int blueCount = 8;
        public int redCount = 8;
        public float spacing = 0.6f;
        public bool autoSpawn = true;

        private void Start()
        {
            if (autoSpawn)
                SpawnDemoBattle();
        }

        [ContextMenu("Spawn Demo Battle")]
        public void SpawnDemoBattle()
        {
            SpawnTeam(blueUnitPrefab, blueLane, blueSpawnPoint, redTarget, blueCount, Vector3.right * spacing);
            SpawnTeam(redUnitPrefab, redLane, redSpawnPoint, blueTarget, redCount, Vector3.left * spacing);
        }

        private void SpawnTeam(UnitCrowdAgent prefab, LanePath lane, Transform spawnPoint, Transform target, int count, Vector3 offsetStep)
        {
            if (prefab == null || lane == null || spawnPoint == null)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = offsetStep * i;
                Vector3 position = spawnPoint.position + offset;
                UnitCrowdAgent unit = Instantiate(prefab, position, spawnPoint.rotation);
                unit.lane = lane;
                unit.currentTarget = target;
                if (target != null)
                    unit.slotProvider = target.GetComponent<AttackSlotProvider>();
            }
        }
    }
}
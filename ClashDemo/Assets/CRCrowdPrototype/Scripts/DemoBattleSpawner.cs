using UnityEngine;

namespace CRCrowdPrototype
{
    public class DemoBattleSpawner : MonoBehaviour
    {
        [Header("预制体")]
        public UnitCrowdAgent blueUnitPrefab;
        public UnitCrowdAgent redUnitPrefab;

        [Header("路线")]
        public LanePath blueLane;
        public LanePath redLane;

        [Header("出生点")]
        public Transform blueSpawnPoint;
        public Transform redSpawnPoint;

        [Header("目标")]
        public Transform blueTarget;
        public Transform redTarget;

        [Header("演示设置")]
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
                unit.Initialize(lane, target, unit.unitData);
            }
        }
    }
}

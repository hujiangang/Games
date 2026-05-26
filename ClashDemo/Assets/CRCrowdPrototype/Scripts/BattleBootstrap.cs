using UnityEngine;

namespace CRCrowdPrototype
{
    public class BattleBootstrap : MonoBehaviour
    {
        [Header("Optional scene wiring")]
        public LanePath leftLane;
        public LanePath rightLane;

        [ContextMenu("Validate Prototype Scene")]
        public void ValidatePrototypeScene()
        {
            Debug.Log("CRCrowdPrototype bootstrap is present. Wire your spawners, lanes, and towers in the inspector.");
        }
    }
}
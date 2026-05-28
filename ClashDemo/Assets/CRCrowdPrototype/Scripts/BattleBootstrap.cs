using UnityEngine;

namespace CRCrowdPrototype
{
    public class BattleBootstrap : MonoBehaviour
    {
        [Header("可选场景绑定")]
        public LanePath leftLane;
        public LanePath rightLane;

        [ContextMenu("检查原型场景")]
        public void ValidatePrototypeScene()
        {
            Debug.Log("CRCrowdPrototype 已挂载。请在 Inspector 里把出生点、路线和塔绑定好。");
        }
    }
}

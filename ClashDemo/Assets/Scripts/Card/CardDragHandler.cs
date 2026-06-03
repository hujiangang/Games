using UnityEngine;
using UnityEngine.EventSystems;
using CRCrowdPrototype;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("关联的单位数据")]
    public UnitData unitData; // 在 Inspector 中指定

    [Header("路线分配")]
    public Transform defaultTarget;

    [Header("放置限制")]
    [Tooltip("地面所在的 Y 高度。")]
    public float groundY = 0f;
    [Tooltip("是否限制卡牌只能放在己方半场。")]
    public bool limitToBottomHalf = true;
    [Tooltip("己方半场允许放置的最大 Z。通常中线附近填 0 或略小于 0。")]
    public float maxPlacementZ = -0.25f;
    [Tooltip("给空中单位额外增加的预览高度。没有特殊需求就保持 0。")]
    public float extraPreviewHeight = 0f;

    private GameObject unitGameObject;
    private Rigidbody[] previewRigidbodies;
    private bool[] previewUseGravityStates;
    private bool[] previewIsKinematicStates;
    private Collider[] previewColliders;
    private bool[] previewColliderStates;
    private CharacterController previewCharacterController;
    private bool previewCharacterControllerEnabled;
    private UnitAgent previewUnitAgent;
    private bool previewUnitAgentEnabled;
    private float spawnHeightOffset = 0.5f; // 预览单位相对地面的高度偏移

    void Awake()
    {
        
    }

    // 拖拽开始时，记录卡牌原位置，并生成预览单位
    public void OnBeginDrag(PointerEventData eventData)
    {

        if (unitData == null || unitData.prefab == null)
            return;

        Vector3 worldPos = GetPlacementWorldPos(eventData);
        spawnHeightOffset = CalculateSpawnHeightOffset(unitData.prefab);
        unitGameObject = Instantiate(unitData.prefab, worldPos + Vector3.up * spawnHeightOffset, Quaternion.identity);
        PreparePreviewUnit();
        SnapPreviewUnit(worldPos);
    }

    // 拖拽过程中，让预览单位跟随鼠标位置移动
    public void OnDrag(PointerEventData eventData)
    {
        if (unitGameObject == null)
            return;

        Vector3 worldPos = GetPlacementWorldPos(eventData);
        SnapPreviewUnit(worldPos);
    }

    // 拖拽结束后，确定最终落点并初始化路线和目标
    public void OnEndDrag(PointerEventData eventData)
    {
        if (unitGameObject == null)
            return;

        Vector3 worldPos = GetPlacementWorldPos(eventData);
        SnapPreviewUnit(worldPos);
        RestorePreviewUnit();
        InitializePreviewUnit(worldPos);
    }

    private Vector3 GetMouseWorldPos(PointerEventData eventData)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector3.zero;

        Ray ray = cam.ScreenPointToRay(eventData.position);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));

        if (groundPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }

    // 统一处理放置落点：先投到地面，再按规则限制到己方半场。
    private Vector3 GetPlacementWorldPos(PointerEventData eventData)
    {
        Vector3 worldPos = GetMouseWorldPos(eventData);

        if (limitToBottomHalf)
            worldPos.z = Mathf.Min(worldPos.z, maxPlacementZ);

        return worldPos;
    }

    // 根据碰撞体或 CharacterController 计算“脚底落地”时根节点应该抬起的高度。
    private float CalculateSpawnHeightOffset(GameObject prefab)
    {
        if (prefab == null)
            return 0f;

        if (prefab.TryGetComponent(out CharacterController controller))
        {
            float controllerOffset = (controller.height * 0.5f) - controller.center.y;
            return Mathf.Max(0f, controllerOffset + extraPreviewHeight);
        }

        if (prefab.TryGetComponent(out CapsuleCollider capsule))
        {
            float capsuleOffset = capsule.center.y + (capsule.height * 0.5f);
            return Mathf.Max(0f, capsuleOffset + extraPreviewHeight);
        }

        if (prefab.TryGetComponent(out BoxCollider box))
        {
            float boxOffset = box.center.y + (box.size.y * 0.5f);
            return Mathf.Max(0f, boxOffset + extraPreviewHeight);
        }

        if (prefab.TryGetComponent(out SphereCollider sphere))
        {
            float sphereOffset = sphere.center.y + sphere.radius;
            return Mathf.Max(0f, sphereOffset + extraPreviewHeight);
        }

        return Mathf.Max(0.5f, prefab.transform.position.y + extraPreviewHeight);
    }

    // 拖拽预览阶段禁用物理和自动移动，避免模型自己飘走。
    private void PreparePreviewUnit()
    {
        if (unitGameObject == null)
            return;

        previewUnitAgent = unitGameObject.GetComponent<UnitAgent>();
        if (previewUnitAgent != null)
        {
            previewUnitAgentEnabled = previewUnitAgent.enabled;
            previewUnitAgent.enabled = false;
        }

        previewCharacterController = unitGameObject.GetComponent<CharacterController>();
        if (previewCharacterController != null)
        {
            previewCharacterControllerEnabled = previewCharacterController.enabled;
            previewCharacterController.enabled = false;
        }

        previewRigidbodies = unitGameObject.GetComponentsInChildren<Rigidbody>(true);
        previewUseGravityStates = new bool[previewRigidbodies.Length];
        previewIsKinematicStates = new bool[previewRigidbodies.Length];

        for (int i = 0; i < previewRigidbodies.Length; i++)
        {
            previewUseGravityStates[i] = previewRigidbodies[i].useGravity;
            previewIsKinematicStates[i] = previewRigidbodies[i].isKinematic;
            previewRigidbodies[i].velocity = Vector3.zero;
            previewRigidbodies[i].angularVelocity = Vector3.zero;
            previewRigidbodies[i].useGravity = false;
            previewRigidbodies[i].isKinematic = true;
        }

        previewColliders = unitGameObject.GetComponentsInChildren<Collider>(true);
        previewColliderStates = new bool[previewColliders.Length];

        for (int i = 0; i < previewColliders.Length; i++)
        {
            previewColliderStates[i] = previewColliders[i].enabled;
            previewColliders[i].enabled = false;
        }
    }

    // 预览对象始终强制跟随当前鼠标落点，不允许被物理或碰撞带走。
    private void SnapPreviewUnit(Vector3 worldPosition)
    {
        if (unitGameObject == null)
            return;

        unitGameObject.transform.position = worldPosition + Vector3.up * spawnHeightOffset;
    }

    // 放下后恢复原本组件状态，让单位重新参与碰撞、物理和 AI。
    private void RestorePreviewUnit()
    {
        if (unitGameObject == null)
            return;

        if (previewUnitAgent == null)
        {
            previewUnitAgent = unitGameObject.AddComponent<UnitAgent>();
        }

        if (previewColliders != null)
        {
            for (int i = 0; i < previewColliders.Length; i++)
                previewColliders[i].enabled = previewColliderStates[i];
        }

        if (previewRigidbodies != null)
        {
            for (int i = 0; i < previewRigidbodies.Length; i++)
            {
                previewRigidbodies[i].useGravity = previewUseGravityStates[i];
                previewRigidbodies[i].isKinematic = previewIsKinematicStates[i];
                previewRigidbodies[i].velocity = Vector3.zero;
                previewRigidbodies[i].angularVelocity = Vector3.zero;
            }
        }

        if (previewCharacterController != null)
            previewCharacterController.enabled = previewCharacterControllerEnabled;

        if (previewUnitAgent != null)
            previewUnitAgent.enabled = previewUnitAgentEnabled;
    }

    private void InitializePreviewUnit(Vector3 worldPosition)
    {
        if (unitGameObject == null)
            return;

        if (unitGameObject.TryGetComponent(out UnitCrowdAgent legacyCrowdAgent))
            legacyCrowdAgent.enabled = false;

        UnitAgent agent = unitGameObject.GetComponent<UnitAgent>();
        if (agent == null)
            agent = unitGameObject.AddComponent<UnitAgent>();

        agent.Initialize(defaultTarget, unitData);
    }
}

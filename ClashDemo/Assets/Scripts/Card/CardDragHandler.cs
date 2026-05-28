using UnityEngine;
using UnityEngine.EventSystems;
using CRCrowdPrototype;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("关联的单位数据")]
    public UnitData unitData; // 在 Inspector 中指定

    [Header("路线分配")]
    public LanePath leftLane;
    public LanePath rightLane;
    public Transform defaultTarget;
    private GameObject unitGameObject;

    private float spawnHeightOffset = 0.5f; // 预览单位相对地面的高度偏移

    void Awake()
    {
        
    }

    // 拖拽开始时，记录卡牌原位置，并生成预览单位
    public void OnBeginDrag(PointerEventData eventData)
    {

        if (unitData == null || unitData.prefab == null)
            return;

        Vector3 worldPos = GetMouseWorldPos(eventData);
        spawnHeightOffset = unitData.prefab.transform.position.y;
        unitGameObject = Instantiate(unitData.prefab, worldPos + Vector3.up * spawnHeightOffset, Quaternion.identity);
    }

    // 拖拽过程中，让预览单位跟随鼠标位置移动
    public void OnDrag(PointerEventData eventData)
    {
        if (unitGameObject == null)
            return;

        Vector3 worldPos = GetMouseWorldPos(eventData);
        unitGameObject.transform.position = worldPos + Vector3.up * spawnHeightOffset;
    }

    // 拖拽结束后，确定最终落点并初始化路线和目标
    public void OnEndDrag(PointerEventData eventData)
    {
        if (unitGameObject == null)
            return;

        Vector3 worldPos = GetMouseWorldPos(eventData);
        unitGameObject.transform.position = worldPos + Vector3.up * spawnHeightOffset;
        InitializePreviewUnit(worldPos);
    }

    private Vector3 GetMouseWorldPos(PointerEventData eventData)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector3.zero;

        Ray ray = cam.ScreenPointToRay(eventData.position);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }

    private void InitializePreviewUnit(Vector3 worldPosition)
    {
        if (unitGameObject == null)
            return;

        if (!unitGameObject.TryGetComponent(out UnitCrowdAgent agent))
            return;

        LanePath selectedLane = worldPosition.x <= 0f ? leftLane : rightLane;
        agent.Initialize(selectedLane, defaultTarget, unitData);
    }
}

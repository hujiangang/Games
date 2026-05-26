using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Associated UnitData")]
    public UnitData unitData; // Assigned in inspector

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector3 originalPosition;

    private GameObject unitGameObject;

    private float spawnHeight = 0f; // Adjust as needed for unit's height

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    // Save original position when drag starts
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;

        Vector3 worldPos = GetMouseWorldPos(eventData);
        spawnHeight = unitData.prefab.transform.position.y;
        Debug.Log($"Spawning unit at world position: {unitData.prefab.transform.position}");
        
        unitGameObject = Instantiate(unitData.prefab, worldPos, Quaternion.identity);
    }

    // Move card along with drag, adjusting for canvas scale
    public void OnDrag(PointerEventData eventData)
    {
        //rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        Vector3 worldPos = GetMouseWorldPos(eventData);
        unitGameObject.transform.position = worldPos;
    }

    // On drag end, raycast to detect spawn position and send spawn command if valid
    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 screenPos = eventData.position;
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 position = hit.point;
            position.y = spawnHeight;
            unitGameObject.transform.position = position;
        }
        // Reset card position after drag ends
        //rectTransform.anchoredPosition = originalPosition;
    }

    private Vector3 GetMouseWorldPos(PointerEventData eventData)
    {
        float planeDistance = 25f;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(eventData.position.x, eventData.position.y, Camera.main.nearClipPlane + planeDistance)
        );
        return worldPos;
    }

    private Vector3 GetSafePreviewPosition(Vector2 screenPos)
    {
        float previewPlaneDistance = 25f;
        return Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane + previewPlaneDistance)
        );
    }
}

using UnityEngine;


/// <summary>
/// 拖拽与吸附逻辑.
/// </summary>
public class DraggablePiece : MonoBehaviour
{
    public Vector3 targetPosition; // 正确的位置
    private bool isLocked = false; // 是否已经拼对
    private float snapDistance = 0.5f; // 吸附距离
    private Vector3 offset;

    void OnMouseDown()
    {
        if (isLocked) return;
        // 记录鼠标与物体的偏移，防止物体中心瞬间跳到鼠标点
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = transform.position - new Vector3(mousePos.x, mousePos.y, 0);
    }

    void OnMouseDrag()
    {
        if (isLocked) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x, mousePos.y, 0) + offset;
    }

    void OnMouseUp()
    {
        if (isLocked) return;
        // 检测是否靠近目标位置
        if (Vector3.Distance(transform.position, targetPosition) < snapDistance)
        {
            transform.position = targetPosition; // 自动吸附
            isLocked = true; // 锁定，不能再动
            GetComponent<MeshRenderer>().sortingOrder = -1; // 放到下层
            CheckLevelComplete();
        }
    }

    void CheckLevelComplete()
    {
        // 这里可以调用一个管理器来检查是否所有碎片都 isLocked
        Debug.Log("碎片已吸附！");
    }
}
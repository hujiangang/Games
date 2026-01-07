using UnityEngine;


/// <summary>
/// 拖拽功能.
/// </summary>
public class DraggableComponent : MonoBehaviour
{
    public Vector3 targetPos;        // 关卡数据中的原始坐标
    public bool isSnapped = false;   // 是否已吸附
    private Vector3 offset;
    private MeshRenderer m_Renderer;
    private static int originalOrder = 2;

    void Start()
    {
        m_Renderer = GetComponent<MeshRenderer>();
        targetPos = GameObject.Find("TargetFrame").transform.position;
    }

    void OnMouseDown()
    {
        if (isSnapped) return;

        Debug.Log($"OnMouseDown {transform.name}");

        // 拖动时提升层级，显示在最前面
        originalOrder++;
        m_Renderer.sortingOrder = originalOrder;

        Vector3 mousePos = GetWorldMousePos();
        offset = transform.position - mousePos;
    }

    void OnMouseDrag()
    {
        if (isSnapped) return;
        transform.position = GetWorldMousePos() + offset;
    }

    void OnMouseUp()
    {
        if (isSnapped) return;

        // 检查吸附条件：距离足够近
        if (Vector3.Distance(transform.position, targetPos) < 0.5f)
        {
            SnapToTarget();
        }
        else
        {
            Debug.Log($"OnMouseUp {transform.name}, set sortingOrder {originalOrder}");
        }
    }

    void SnapToTarget()
    {
        transform.position = targetPos;
        isSnapped = true;
        //m_Renderer.sortingOrder = 0; // 回到标准层级

        // 播放个小音效或缩放动画（可选）
        transform.localScale = Vector3.one;

        // 通知管理器检查是否胜利
        FindObjectOfType<GamePlay>().CheckWinCondition();
    }

    Vector3 GetWorldMousePos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
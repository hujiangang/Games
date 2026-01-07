using UnityEngine;


/// <summary>
/// 拖拽功能.
/// </summary>
public class DraggableComponent : MonoBehaviour
{
    public Vector3 targetPos;        // 关卡数据中的原始坐标

    /// <summary>
    /// 是否已吸附.
    /// </summary>
    public bool isSnapped = false;   
    private Vector3 offset;

    [HideInInspector]
    public Vector3 correctWorldPos;

    private MeshRenderer m_Renderer;

    /// <summary>
    /// 吸附灵敏度.
    /// </summary>
    private static int originalOrder = 2;
    private float snapDistance = 0.4f;

    /// <summary>
    /// 静态变量或引用管理器，判定是否全关结束.
    /// </summary>
    public static bool isGlobalLocked = false;


    void Awake(){
         m_Renderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        targetPos = GameObject.Find("TargetFrame").transform.position;
    }

    void OnMouseDown()
    {
        if (isGlobalLocked) return;

        isSnapped = false;

        originalOrder++;
        m_Renderer.sortingOrder = originalOrder;

        Vector3 mousePos = GetWorldMousePos();
        offset = transform.position - mousePos;
    }

    void OnMouseDrag()
    {
        if (isGlobalLocked) return;
        transform.position = GetWorldMousePos() + offset;
    }

    void OnMouseUp()
    {
       if (isGlobalLocked) return;

        // 检查吸附条件：距离足够近
        if (Vector3.Distance(transform.position, correctWorldPos) < snapDistance)
        {
            SnapToTarget();
        }
        else
        {
             isSnapped = false;
            Debug.Log($"OnMouseUp {transform.name}, set sortingOrder {originalOrder}");
        }
    }

    void SnapToTarget()
    {
        transform.position = correctWorldPos;
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
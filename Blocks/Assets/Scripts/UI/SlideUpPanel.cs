using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SlideUpPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public RectTransform rootTransform;   // 拖入 SlidePanel_Root
    public RectTransform arrowIcon;       // 拖入 Arrow_Icon

    [Header("Settings")]
    public float collapsedY = 0;       // 折叠时露出的高度
    public float expandedY = 300;        // 展开后的总高度
    public float snapThreshold = 0.4f;    // 滑动超过40%自动吸附
    public float lerpSpeed = 15f;         // 动画平滑度

    private float targetY;
    private bool isDragging = false;
    private bool isOpen = false;

    void Start()
    {
        // 初始设为折叠状态
        targetY = collapsedY;
        SetPanelY(collapsedY);
    }

    void Update()
    {
        if (!isDragging)
        {
            // 平滑磁吸动画
            float currentY = rootTransform.anchoredPosition.y;
            float nextY = Mathf.Lerp(currentY, targetY, Time.deltaTime * lerpSpeed);
            SetPanelY(nextY);

            // 箭头旋转动画：展开时向下，折叠时向上
            float rotationGoal = isOpen ? 90f : -90f;
            float nextRot = Mathf.LerpAngle(arrowIcon.localEulerAngles.z, rotationGoal, Time.deltaTime * lerpSpeed);
            arrowIcon.localEulerAngles = new Vector3(0, 0, nextRot);
        }
    }

    private void SetPanelY(float y)
    {
        rootTransform.anchoredPosition = new Vector2(0, y);
    }

    // --- 实现拖拽接口 ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 跟随手指移动坐标
        float currentY = rootTransform.anchoredPosition.y;
        float newY = currentY + eventData.delta.y;

        // 限制滑动上下界，并增加一点点“拉不动”的阻力感
        newY = Mathf.Clamp(newY, collapsedY - 20f, expandedY + 20f);
        SetPanelY(newY);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // 计算当前位置占总行程的百分比
        float progress = (rootTransform.anchoredPosition.y - collapsedY) / (expandedY - collapsedY);

        // 自动吸附判断
        if (progress > snapThreshold)
        {
            OpenPanel();
        }
        else
        {
            ClosePanel();
        }
    }

    public void OpenPanel()
    {
        targetY = expandedY;
        isOpen = true;
    }

    public void ClosePanel()
    {
        targetY = collapsedY;
        isOpen = false;
    }

    // 点击把手区域也可以直接切换
    public void OnHandleClick()
    {
        if (isOpen) ClosePanel(); else OpenPanel();
    }
}
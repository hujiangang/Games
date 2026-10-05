using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI点击音效管理器
/// 自动为场景中所有按钮添加点击音效组件
/// </summary>
public class UIClickSoundManager : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("是否在Start时自动为所有按钮添加点击音效")]
    public bool autoAddToAllButtons = true;

    void Start()
    {
        if (autoAddToAllButtons)
        {
            AddClickSoundToAllButtons();
        }
    }

    /// <summary>
    /// 为场景中所有按钮添加点击音效
    /// </summary>
    public void AddClickSoundToAllButtons()
    {
        // 查找场景中所有的按钮
        Button[] allButtons = FindObjectsOfType<Button>(true);
        
        int addedCount = 0;
        foreach (Button button in allButtons)
        {
            // 如果按钮还没有 UIClickSound 组件，则添加
            if (button.GetComponent<UIClickSound>() == null)
            {
                button.gameObject.AddComponent<UIClickSound>();
                addedCount++;
            }
        }

        Debug.Log($"[UIClickSoundManager] 已为 {addedCount} 个按钮添加点击音效");
    }

    /// <summary>
    /// 为特定按钮添加点击音效
    /// </summary>
    public static void AddClickSound(Button button)
    {
        if (button != null && button.GetComponent<UIClickSound>() == null)
        {
            button.gameObject.AddComponent<UIClickSound>();
        }
    }

    /// <summary>
    /// 移除按钮的点击音效
    /// </summary>
    public static void RemoveClickSound(Button button)
    {
        if (button != null)
        {
            UIClickSound clickSound = button.GetComponent<UIClickSound>();
            if (clickSound != null)
            {
                Destroy(clickSound);
            }
        }
    }
}

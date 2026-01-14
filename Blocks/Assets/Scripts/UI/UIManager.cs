using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum UIWindowType
{
    HintWindow,
}

/// <summary>
/// UI 管理类.
/// </summary>
public class UIManager : MonoBehaviour
{
    public Dictionary<UIWindowType, BasicUI> uiWindows = new();

    public static UIManager instance;


    public BasicUI GetUIWindow(UIWindowType type)
    {
        if (uiWindows.TryGetValue(type, out BasicUI uiWindow))
        {
            return uiWindow;
        }
        Debug.LogError($"UIWindowType {type} 未注册");
        return null;
    }

    public void RegisterUIWindow(UIWindowType type, BasicUI uiWindow)
    {
        if (uiWindows.ContainsKey(type))
        {
            Debug.LogError($"UIWindowType {type} 已注册");
            return;
        }
        uiWindows[type] = uiWindow;
    }

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 打开提示窗口.
    /// </summary>
    /// <param name="levelData"></param>
    public void OpenHintWindow(LevelData levelData)
    {
        HintWindow hintWindow = GetUIWindow(UIWindowType.HintWindow) as HintWindow;
        if (hintWindow != null)
        {
            hintWindow.OpenHint(levelData);
        }
    }

    /// <summary>
    /// 清除提示窗口相关数据.
    /// </summary>
    public void ClearHintWindow()
    {
        HintWindow hintWindow = GetUIWindow(UIWindowType.HintWindow) as HintWindow;
        if (hintWindow != null)
        {
            hintWindow.ClearPreviews();
        }
    }
}

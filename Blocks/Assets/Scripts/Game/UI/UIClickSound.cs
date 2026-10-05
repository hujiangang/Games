using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI按钮点击音效组件
/// 自动为按钮添加点击音效
/// </summary>
[RequireComponent(typeof(Button))]
public class UIClickSound : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        
        // 在按钮原有的点击事件之前添加音效
        button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        // 触发点击音效事件
        GameEvents.InvokeBasicEvent(GameBasicEvent.UIClick);
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LocalizedLabel : MonoBehaviour
{
    private string key;
    private object[] arguments;
    public void Bind(string textKey,params object[] args) { key=textKey;arguments=args;Refresh(); }
    void Refresh()
    {
        if(string.IsNullOrEmpty(key)) return;
        var label=GetComponent<Text>();label.text=GameText.Get(key,arguments);
        var font=Resources.Load<Font>(GameText.FontResource);if(font) label.font=font;
    }
    void OnEnable() { GameText.Changed+=Refresh;Refresh(); }
    void OnDisable() { GameText.Changed-=Refresh; }
}

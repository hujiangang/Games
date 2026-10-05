using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps dialogs and previews centered inside the same safe area as the HUD.</summary>
public class SafePuzzleCanvas : MonoBehaviour
{
    public RectTransform safeRoot;
    public void Refresh() => PuzzleScreen.FitCanvas(GetComponent<CanvasScaler>(),safeRoot);
    void LateUpdate() { Refresh(); }
    public static RectTransform Add(GameObject canvas)
    {
        var root=new GameObject("Safe content",typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform,false);
        var layout=canvas.AddComponent<SafePuzzleCanvas>();layout.safeRoot=root;layout.Refresh();return root;
    }
}

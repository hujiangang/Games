using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HintWindow : BasicUI
{
    public GameObject hintPanel;
    public Transform previewAnchor;
    public Material pieceMaterial;
    public float displayTime=4;
    public static bool IsShowing { get; private set; }
    private GameObject overlay;
    void Start()
    {
        UIManager.instance?.RegisterUIWindow(UIWindowType.HintWindow,this);
        if(hintPanel) hintPanel.SetActive(false);
        displayTime=Mathf.Max(4,displayTime);
    }
    public void ClearPreviews()
    {
        StopAllCoroutines();IsShowing=false;
        if(overlay) { overlay.SetActive(false);Destroy(overlay);overlay=null; }
        if(hintPanel) hintPanel.SetActive(false);
    }
    public void OpenHint(LevelData data)
    {
        if(IsShowing || data==null) return;
        IsShowing=true;
        overlay=new GameObject("Pattern preview",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=31000;
        var safe=SafePuzzleCanvas.Add(overlay);
        var shade=new GameObject("Shade",typeof(RectTransform),typeof(Image),typeof(Button));
        shade.transform.SetParent(overlay.transform,false);
        shade.transform.SetAsFirstSibling();
        var rt=shade.GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
        shade.GetComponent<Image>().color=new Color(.1f,.12f,.18f,.92f);
        shade.GetComponent<Button>().onClick.AddListener(ClearPreviews);
        foreach(var p in data.pieces) {
            var go=new GameObject("Preview",typeof(RectTransform),typeof(PolygonGraphic));go.transform.SetParent(safe,false);
            var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(760,760);
            go.GetComponent<PolygonGraphic>().Set(p,data.boardHalfSize>0?data.boardHalfSize:2);
        }
        var label=new GameObject("Caption",typeof(RectTransform),typeof(Text));
        label.transform.SetParent(safe,false);
        var lrt=label.GetComponent<RectTransform>();lrt.sizeDelta=new Vector2(900,100);lrt.anchoredPosition=new Vector2(0,-500);
        var text=label.GetComponent<Text>();text.font=Resources.Load<Font>("Fonts/BlocksUI");
        text.fontSize=32;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;
        GameText.Bind(text,"hint.caption");
        StartCoroutine(AutoClose());
    }
    IEnumerator AutoClose() { yield return new WaitForSecondsRealtime(Mathf.Max(4,displayTime));ClearPreviews(); }
    void OnDisable() { ClearPreviews(); }
}

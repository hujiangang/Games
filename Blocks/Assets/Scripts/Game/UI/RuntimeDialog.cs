using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable modal with an explicit opt-in button and a cancel path.</summary>
public static class RuntimeDialog
{
    private static GameObject root;
    public static bool IsOpen => root;
    public static void Message(string title,string message) => Confirm(title,message,"action.ok",null,null,false);
    public static void Confirm(string title,string message,string accept,Action yes,Action no=null,bool cancellable=true)
    {
        if(root) return;
        root=new GameObject("Dialog",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
        var safe=SafePuzzleCanvas.Add(root);
        var shade=Rect("Shade",root.transform,Vector2.zero,Vector2.zero);
        shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;
        shade.gameObject.AddComponent<Image>().color=new Color(.06f,.09f,.15f,.65f);
        shade.SetAsFirstSibling();
        var panel=Rect("Panel",safe,new Vector2(940,600),Vector2.zero);
        panel.gameObject.AddComponent<RoundedPanel>().color=new Color(.97f,.99f,1);
        Label(title,panel,new Vector2(740,80),new Vector2(0,175),42);
        Label(message,panel,new Vector2(850,210),new Vector2(0,30),36);
        Button(accept,panel,new Vector2(cancellable?210:0,-185),()=>Close(yes),new Color(.20f,.48f,.48f));
        if(cancellable) Button("action.cancel",panel,new Vector2(-210,-185),()=>Close(no),new Color(.38f,.49f,.56f));
    }
    static void Close(Action action) { var old=root;root=null;if(old) { old.SetActive(false);UnityEngine.Object.Destroy(old); }action?.Invoke(); }
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
        rt.sizeDelta=size;rt.anchoredPosition=position;return rt;
    }
    static void Label(string value,Transform parent,Vector2 size,Vector2 pos,int fontSize)
    {
        var rt=Rect("Text",parent,size,pos);var text=rt.gameObject.AddComponent<Text>();
        text.font=Resources.Load<Font>("Fonts/BlocksUI");text.fontSize=fontSize;GameText.Bind(text,value);
        text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.18f,.22f,.27f);text.raycastTarget=false;
    }
    static void Button(string title,Transform parent,Vector2 pos,Action action,Color color)
    {
        var rt=Rect(title,parent,new Vector2(390,140),pos);rt.gameObject.AddComponent<RoundedPanel>().color=color;
        var b=rt.gameObject.AddComponent<Button>();rt.gameObject.AddComponent<UIClickSound>();b.onClick.AddListener(()=>action());
        Label(title,rt,new Vector2(370,120),Vector2.zero,36);
        rt.GetComponentInChildren<Text>().color=Color.white;
    }
}

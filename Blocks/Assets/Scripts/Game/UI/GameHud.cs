using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Level title and a collapsible tool drawer. Branding stays outside the puzzle scene.</summary>
public class GameHud : MonoBehaviour
{
    private RectTransform safeRoot;
    private Text title,counter,hints,winTitle;
    private Button previous,next,hintButton,mute,sidebar,continueButton;
    private PuzzleIcon soundIcon,videoIcon,previousIcon,nextIcon,hintIcon;
    private CanvasScaler scaler;
    private GameObject completion;
    private RectTransform progress;
    private int current,total;
    private bool finished;
    private Font font;
    public PuzzleDrawer Drawer { get; private set; }
    static readonly Color Ink=new(.17f,.33f,.40f);
    void Awake()
    {
        font=Resources.Load<Font>("Fonts/BlocksUI");
        var old=GameObject.Find("UICanvas");
        if(old) {
            var group=old.GetComponent<CanvasGroup>();if(!group) group=old.AddComponent<CanvasGroup>();
            group.alpha=0;group.interactable=false;group.blocksRaycasts=false;
        }
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        scaler=gameObject.AddComponent<CanvasScaler>();gameObject.AddComponent<GraphicRaycaster>();
        safeRoot=Rect("Safe area",transform,Vector2.zero,Vector2.zero);
        title=Label("",safeRoot,new Vector2(850,80),new Vector2(0,-70),46,Ink);
        title.rectTransform.anchorMin=title.rectTransform.anchorMax=new Vector2(.5f,1);

        var viewport=Rect("Drawer viewport",safeRoot,new Vector2(1000,PuzzleDrawer.Height),Vector2.zero);
        viewport.anchorMin=viewport.anchorMax=new Vector2(.5f,0);viewport.pivot=new Vector2(.5f,0);
        viewport.gameObject.AddComponent<RectMask2D>();
        var panel=Rect("Tool drawer",viewport,new Vector2(1000,PuzzleDrawer.Height),Vector2.zero);
        panel.anchorMin=panel.anchorMax=new Vector2(.5f,0);panel.pivot=new Vector2(.5f,0);
        var paper=panel.gameObject.AddComponent<RoundedPanel>();paper.radius=42;paper.color=new Color(.97f,.99f,1,.98f);
        var content=Rect("Drawer controls",panel,new Vector2(1000,PuzzleDrawer.Height),Vector2.zero);
        content.anchorMin=content.anchorMax=new Vector2(.5f,0);content.pivot=new Vector2(.5f,0);
        var groupControls=content.gameObject.AddComponent<CanvasGroup>();
        var handle=Rect("Menu handle",panel,new Vector2(1000,110),new Vector2(0,PuzzleDrawer.Height-55));
        handle.anchorMin=handle.anchorMax=new Vector2(.5f,0);
        handle.gameObject.AddComponent<Image>().color=Color.clear;
        var grip=Rect("Grip",handle,new Vector2(90,9),new Vector2(0,33));
        var gripImage=grip.gameObject.AddComponent<RoundedPanel>();gripImage.color=new Color(.65f,.77f,.81f);gripImage.raycastTarget=false;
        Drawer=handle.gameObject.AddComponent<PuzzleDrawer>();Drawer.panel=panel;Drawer.controls=groupControls;
        Drawer.arrow=Icon(handle,PuzzleIcon.Kind.Expand,new Vector2(0,-7),56,Ink);

        hintButton=IconButton("action.hint",content,new Vector2(-140,252),PuzzleIcon.Kind.Eye,()=>GameEvents.InvokeBasicEvent(GameBasicEvent.Look));
        hintButton.GetComponent<Image>().color=new Color(.20f,.48f,.48f);
        hintIcon=hintButton.GetComponentInChildren<PuzzleIcon>();hintIcon.color=Color.white;
        var badge=Rect("Hint count",hintButton.transform,new Vector2(82,54),new Vector2(66,63));
        var badgeImage=badge.gameObject.AddComponent<RoundedPanel>();badgeImage.radius=24;badgeImage.color=new Color(1,.79f,.49f);badgeImage.raycastTarget=false;
        hints=Label("",badge,new Vector2(78,52),Vector2.zero,32,Ink);
        videoIcon=Icon(hintButton.transform,PuzzleIcon.Kind.Video,new Vector2(-50,-52),35,Color.white);
        videoIcon.gameObject.SetActive(false);
        mute=IconButton("action.sound",content,new Vector2(140,252),PuzzleIcon.Kind.Volume,()=>GameEvents.InvokeBasicEvent(GameBasicEvent.TurnAudio));
        soundIcon=mute.GetComponentInChildren<PuzzleIcon>();
        sidebar=IconButton("action.sidebar",content,new Vector2(280,252),PuzzleIcon.Kind.Sidebar,DouyinHost.OpenSidebar);
        sidebar.gameObject.SetActive(false);
        previous=IconButton("action.previous",content,new Vector2(-370,93),PuzzleIcon.Kind.Previous,()=>GameEvents.InvokeBasicEvent(GameBasicEvent.PrevLevel));
        next=IconButton("action.next",content,new Vector2(370,93),PuzzleIcon.Kind.Next,()=>GameEvents.InvokeBasicEvent(GameBasicEvent.NextLevel));
        previousIcon=previous.GetComponentInChildren<PuzzleIcon>();nextIcon=next.GetComponentInChildren<PuzzleIcon>();
        counter=Label("",content,new Vector2(410,64),new Vector2(0,110),42,Ink,true);
        var track=Rect("Level progress",content,new Vector2(370,10),new Vector2(0,57));
        track.anchorMin=track.anchorMax=new Vector2(.5f,0);
        var trackImage=track.gameObject.AddComponent<RoundedPanel>();trackImage.color=new Color(.82f,.90f,.93f);trackImage.raycastTarget=false;
        progress=Rect("Completed progress",track,Vector2.zero,Vector2.zero);
        progress.anchorMin=Vector2.zero;progress.anchorMax=new Vector2(0,1);progress.offsetMin=progress.offsetMax=Vector2.zero;
        var fill=progress.gameObject.AddComponent<RoundedPanel>();fill.color=new Color(.2f,.48f,.48f);fill.raycastTarget=false;
        Drawer.SetOpen(false,true);

        completion=Rect("Completion",safeRoot,new Vector2(900,410),new Vector2(0,-180)).gameObject;
        completion.AddComponent<RoundedPanel>().color=new Color(.97f,.99f,1,.98f);
        winTitle=Label("",completion.transform,new Vector2(840,110),new Vector2(0,90),52,new Color(.2f,.4f,.35f));
        continueButton=TextButton("action.next",completion.transform,new Vector2(0,-85),new Vector2(480,140),Continue);
        completion.SetActive(false);RefreshLayout();
    }
    void Start() { if(AudioComponent.Instance) SetAudio(AudioComponent.Instance.IsMuted()); }
    void Update()
    {
        RefreshLayout();bool available=!AdService.IsBusy && !HintWindow.IsShowing;
        hintButton.interactable=available && !finished;mute.interactable=available;
        previous.interactable=available && current>1;
        next.interactable=available && current<total && current<UserDataManager.GetCurrentLevel();
        previousIcon.color=new Color(Ink.r,Ink.g,Ink.b,previous.interactable?1:.25f);
        nextIcon.color=new Color(Ink.r,Ink.g,Ink.b,next.interactable?1:.25f);
        hintIcon.color=new Color(1,1,1,hintButton.interactable?1:.4f);
        continueButton.interactable=available;
        bool hasSidebar=DouyinHost.SidebarAvailable;sidebar.gameObject.SetActive(hasSidebar);sidebar.interactable=available;
        hintButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(hasSidebar?-280:-140,252);
        mute.GetComponent<RectTransform>().anchoredPosition=new Vector2(hasSidebar?0:140,252);
    }
    public void RefreshLayout() { PuzzleScreen.FitCanvas(scaler,safeRoot);Drawer?.Refresh(); }
    void Continue() { GameEvents.InvokeBasicEvent(current==total?GameBasicEvent.Play:GameBasicEvent.NextLevel); }
    void UpdateLevel(int level,int count,LevelUnlockStatus status)
    {
        current=level;total=count;GameText.Bind(counter,"level.counter",level,count);
        GameText.Bind(title,"level.Level_"+level+".name");
        int completed=0;for(int i=1;i<=count;i++) if(UserDataManager.IsCompleted(i)) completed++;
        progress.anchorMax=new Vector2(completed/(float)Mathf.Max(1,count),1);
    }
    void SetHints(int count) { GameText.Bind(hints,count>0?"hint.remaining":"hint.reward",count);videoIcon.gameObject.SetActive(count<=0); }
    void SetAudio(bool value) { soundIcon.Symbol=value?PuzzleIcon.Kind.Muted:PuzzleIcon.Kind.Volume; }
    void ResetHud() { finished=false;completion.SetActive(false); }
    void Complete()
    {
        UpdateLevel(current,total,LevelUnlockStatus.Unlocked);
        finished=true;completion.SetActive(true);
        GameText.Bind(winTitle,current==total?"win.all":"win.level",total);
        GameText.Bind(continueButton.GetComponentInChildren<Text>(),current==total?"action.replay":"action.next");
    }
    void OnEnable()
    {
        GameEvents.RegisterEvent<int,int,LevelUnlockStatus>(GameBasicEvent.UpdateLevel,UpdateLevel);
        GameEvents.RegisterEvent<int>(GameBasicEvent.UpdateLookCount,SetHints);
        GameEvents.RegisterEvent<bool>(GameBasicEvent.UpdateAudio,SetAudio);
        GameEvents.RegisterBasicEvent(GameBasicEvent.ResetUI,ResetHud);
        GameEvents.RegisterBasicEvent(GameBasicEvent.CompleteLevel,Complete);
    }
    void OnDisable()
    {
        GameEvents.UnregisterEvent<int,int,LevelUnlockStatus>(GameBasicEvent.UpdateLevel,UpdateLevel);
        GameEvents.UnregisterEvent<int>(GameBasicEvent.UpdateLookCount,SetHints);
        GameEvents.UnregisterEvent<bool>(GameBasicEvent.UpdateAudio,SetAudio);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.ResetUI,ResetHud);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CompleteLevel,Complete);
    }
    RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
        rt.sizeDelta=size;rt.anchoredPosition=position;return rt;
    }
    Text Label(string value,Transform parent,Vector2 size,Vector2 position,int fontSize,Color color,bool bottom=false)
    {
        var rt=Rect(value,parent,size,position);if(bottom) rt.anchorMin=rt.anchorMax=new Vector2(.5f,0);
        var text=rt.gameObject.AddComponent<Text>();text.font=font;text.fontSize=fontSize;text.text=value;
        text.alignment=TextAnchor.MiddleCenter;text.color=color;text.raycastTarget=false;return text;
    }
    PuzzleIcon Icon(Transform parent,PuzzleIcon.Kind symbol,Vector2 position,float size,Color color)
    {
        var rt=Rect(symbol.ToString(),parent,Vector2.one*size,position);
        var icon=rt.gameObject.AddComponent<PuzzleIcon>();icon.Symbol=symbol;icon.color=color;icon.raycastTarget=false;return icon;
    }
    Button BaseButton(string name,Transform parent,Vector2 position,Vector2 size,Action action)
    {
        var rt=Rect(name,parent,size,position);rt.gameObject.AddComponent<RoundedPanel>().color=new Color(.84f,.92f,.94f);
        var button=rt.gameObject.AddComponent<Button>();rt.gameObject.AddComponent<UIClickSound>();button.onClick.AddListener(()=>action());return button;
    }
    Button IconButton(string name,Transform parent,Vector2 position,PuzzleIcon.Kind symbol,Action action)
    {
        var button=BaseButton(name,parent,position,Vector2.one*148,action);
        button.GetComponent<RectTransform>().anchorMin=button.GetComponent<RectTransform>().anchorMax=new Vector2(.5f,0);
        Icon(button.transform,symbol,Vector2.zero,86,Ink);return button;
    }
    Button TextButton(string name,Transform parent,Vector2 position,Vector2 size,Action action)
    {
        var button=BaseButton(name,parent,position,size,action);GameText.Bind(Label("",button.transform,size,Vector2.zero,40,Ink),name);return button;
    }
}

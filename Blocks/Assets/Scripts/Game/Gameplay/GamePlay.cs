using System.Collections;
using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;

public enum LevelUnlockStatus { Locked, Unlocked, Current }

/// <summary>Level session orchestration. Geometry, input, persistence and ads have separate owners.</summary>
public class GamePlay : MonoBehaviour
{
    public static bool isGlobalLocked;
    public static bool IsStartOperation;
    public Material pieceMaterial;
    public Vector3 spawnCenter=new(0,-2,0);
    public float spawnRadius=1.2f;
    public int currentLevel;
    private int sumLevel,selectLevel;
    private LevelData currLevelData;
    private readonly List<DraggableComponent> allPieces=new();
    private readonly Dictionary<int,LevelData> cache=new();
    private GameObject targetFrameObj;
    private Rect targetFrameRect;
    private bool completed;
    public int PieceCount => allPieces.Count;
    public Rect BoardBounds => targetFrameRect;

    void Awake()
    {
        isGlobalLocked=false;IsStartOperation=false;
        sumLevel=LevelPersistence.GetSumLevel();
        UserDataManager.Load(sumLevel);
        currentLevel=selectLevel=UserDataManager.GetCurrentLevel();
        if(!GetComponent<AdService>()) gameObject.AddComponent<AdService>();
        if(Camera.main && !Camera.main.GetComponent<PuzzleCamera>()) Camera.main.gameObject.AddComponent<PuzzleCamera>();
        new GameObject("Game HUD",typeof(RectTransform)).AddComponent<GameHud>();
    }
    void Start() { Play(); }
    public LevelData GetLevelData(int level)
    {
        if(level<1 || level>sumLevel) return null;
        if(!cache.TryGetValue(level,out var data)) {
            data=LevelPersistence.Load($"Level_{level}");
            if(data!=null) cache[level]=data;
        }
        return data;
    }
    void ClearPieces()
    {
        foreach(var p in allPieces) if(p) { p.gameObject.SetActive(false);Destroy(p.gameObject); }
        allPieces.Clear();
    }
    void DrawTargetFrame(float half)
    {
        if(!targetFrameObj) targetFrameObj=GameObject.Find("TargetFrame");
        if(!targetFrameObj) targetFrameObj=new GameObject("TargetFrame");
        var center=new Vector3(0,2.15f,0);
        targetFrameObj.transform.position=center;
        targetFrameRect=new Rect(center.x-half,center.y-half,half*2,half*2);
        var line=targetFrameObj.GetComponent<LineRenderer>();
        if(!line) line=targetFrameObj.AddComponent<LineRenderer>();
        line.useWorldSpace=false;line.loop=true;line.positionCount=4;
        float e=half+.035f;
        line.SetPositions(new[]{new Vector3(-e,e,.1f),new Vector3(e,e,.1f),new Vector3(e,-e,.1f),new Vector3(-e,-e,.1f)});
        line.startWidth=line.endWidth=.055f;line.numCornerVertices=3;
        line.sharedMaterial=pieceMaterial;
        line.startColor=line.endColor=new Color(.40f,.58f,.64f);
    }
    void SpawnPieces()
    {
        int count=currLevelData.pieces.Count;
        var layout=OpeningLayouts.Get(currLevelData.levelName);
        if(layout==null || layout.positions.Length!=count || layout.order.Length!=count)
            throw new System.InvalidOperationException("Missing full-size opening layout: "+currLevelData.levelName);
        DraggableComponent.globalTopOrder=count+1;
        for(int slot=0;slot<count;slot++) {
            int index=layout.order[slot];var pd=currLevelData.pieces[index];
            var go=new GameObject($"GamePiece_{index+1}") { tag="PuzzlePiece",layer=LayerMask.NameToLayer("PuzzlePiece") };
            var pp=go.AddComponent<PuzzlePiece>();
            pp.Init(pd.vertices,pieceMaterial,pd.color,slot+1);
            var target=pp.transform.position+targetFrameObj.transform.position;
            pp.correctWorldPos=target;
            var draggable=go.AddComponent<DraggableComponent>();
            draggable.Init(targetFrameRect,target,targetFrameObj.transform.position,slot+1);
            draggable.snapDistance=currLevelData.snapDistance>0?currLevelData.snapDistance:.32f;
            draggable.SetInitialPosition(layout.positions[index]);
            allPieces.Add(draggable);
        }
    }
    public void CheckFinish6()
    {
        if(completed || isGlobalLocked || currLevelData==null) return;
        var paths=new Paths64();
        foreach(var p in allPieces) {
            if(!p || p.transform.localScale!=Vector3.one) return;
            paths.Add(p.GetWorldPath(PuzzleGeometry.Scale));
        }
        if(!PuzzleGeometry.IsComplete(paths,PuzzleGeometry.Frame(targetFrameRect),currLevelData.pieces.Count)) return;
        completed=true;isGlobalLocked=true;
        // Persist before UI callbacks or ads can interrupt the session.
        bool firstClear=!UserDataManager.IsCompleted(currentLevel);
        UserDataManager.CompleteLevel(currentLevel);
        GameEvents.InvokeBasicEvent(GameBasicEvent.CompleteLevel);
        AdService.Instance?.RecordCompletion(firstClear,currentLevel);
    }
    public void CheckFinish() => CheckFinish6();
    void Play()
    {
        if(AdService.IsBusy || selectLevel>UserDataManager.GetCurrentLevel()) return;
        var data=GetLevelData(selectLevel);
        if(data==null) { Debug.LogError("No valid level data; cannot start a session.");return; }
        ClearPieces();
        currLevelData=data;currentLevel=selectLevel;
        completed=false;isGlobalLocked=false;IsStartOperation=false;
        DrawTargetFrame(data.boardHalfSize>0?data.boardHalfSize:2);
        SpawnPieces();
        if(UIManager.instance) UIManager.instance.ClearHintWindow();
        GameEvents.InvokeBasicEvent(GameBasicEvent.ResetUI);
        StartCoroutine(PublishState());
    }
    IEnumerator PublishState()
    {
        yield return null;
        UpdateSelection();
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount,UserDataManager.GetHintCount());
        if(AudioComponent.Instance) GameEvents.InvokeEvent(GameBasicEvent.UpdateAudio,AudioComponent.Instance.IsMuted());
    }
    void UpdateSelection()
    {
        var state=selectLevel>UserDataManager.GetCurrentLevel()?LevelUnlockStatus.Locked:
            UserDataManager.IsCompleted(selectLevel)?LevelUnlockStatus.Unlocked:LevelUnlockStatus.Current;
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLevel,selectLevel,sumLevel,state);
    }
    void OnLook()
    {
        if(completed || AdService.IsBusy || HintWindow.IsShowing || currLevelData==null) return;
        if(UserDataManager.GetHintCount()>0) { ShowHint();return; }
        AdService.Instance?.OfferReward(() => {
            UserDataManager.IncHintCount();
            GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount,UserDataManager.GetHintCount());
        });
    }
    void ShowHint()
    {
        if(!UIManager.instance || !UIManager.instance.CanShowHint()) return;
        if(UserDataManager.ConsumeHintCount()) {
            UIManager.instance.OpenHintWindow(currLevelData);
            GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount,UserDataManager.GetHintCount());
        }
    }
    void PrevLevel() { Navigate(-1); }
    void NextLevel()
    {
        if(AdService.IsBusy) return;
        if(completed && AdService.Instance && AdService.Instance.TryInterstitial(()=>Navigate(1))) return;
        Navigate(1);
    }
    void Navigate(int direction)
    {
        if(AdService.IsBusy || HintWindow.IsShowing) return;
        int candidate=Mathf.Clamp(currentLevel+direction,1,sumLevel);
        if(candidate==currentLevel || candidate>UserDataManager.GetCurrentLevel()) return;
        selectLevel=candidate;Play();
    }
    void StartGameOprate() { selectLevel=currentLevel;UpdateSelection(); }
    void OnApplicationPause(bool paused) { if(paused) UserDataManager.Save(UserDataManager.userData); }
    void OnApplicationQuit() { UserDataManager.Save(UserDataManager.userData); }
    void OnEnable()
    {
        GameEvents.RegisterBasicEvent(GameBasicEvent.Look,OnLook);
        GameEvents.RegisterBasicEvent(GameBasicEvent.CheckFinish,CheckFinish6);
        GameEvents.RegisterBasicEvent(GameBasicEvent.PrevLevel,PrevLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.NextLevel,NextLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.StartGameOprate,StartGameOprate);
        GameEvents.RegisterBasicEvent(GameBasicEvent.Play,Play);
    }
    void OnDisable()
    {
        GameEvents.UnregisterBasicEvent(GameBasicEvent.Look,OnLook);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CheckFinish,CheckFinish6);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PrevLevel,PrevLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.NextLevel,NextLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.StartGameOprate,StartGameOprate);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.Play,Play);
    }
}

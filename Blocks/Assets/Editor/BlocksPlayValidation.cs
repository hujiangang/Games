using System;
using Clipper2Lib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine.EventSystems;

/// <summary>Run only in the isolated validation project. Drives actual drag/drop and level events.</summary>
[InitializeOnLoad]
public static class BlocksPlayValidation
{
    static BlocksPlayValidation()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Blocks.BatchPlay",false))
                new GameObject("Validation runner").AddComponent<BlocksPlayValidationRunner>();
        };
    }
    public static void RunBatch()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.validation/"))
            throw new InvalidOperationException("Play validation must use the isolated .validation project.");
        SessionState.SetBool("Blocks.BatchPlay",true);
        PlayerPrefs.DeleteKey("ColorBlocks.Profile.v2");
        PlayerPrefs.DeleteKey("ColorBlocks.Profile.v2.backup");
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        EditorApplication.isPlaying=true;
    }
}

public class BlocksPlayValidationRunner : MonoBehaviour
{
    private int victories;
    private readonly List<string> failures=new();
    private string reportFolder;
    void Awake()
    {
        reportFolder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../ValidationResults"));
        Directory.CreateDirectory(reportFolder);
        Application.logMessageReceived+=OnLog;
        GameEvents.RegisterBasicEvent(GameBasicEvent.CompleteLevel,OnVictory);
    }
    void OnLog(string message,string trace,LogType type)
    { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) failures.Add(message); }
    void OnVictory() { victories++; }
    IEnumerator Start()
    {
        yield return null;yield return null;
        var game=FindObjectOfType<GamePlay>();
        if(!game) { Finish("Missing gameplay");yield break; }
        UserDataManager.userData.currentLevel=1;
        for(int i=0;i<50;i++) UserDataManager.userData.levelUnlockStatus[i]=false;
        GameText.SetLocale("zh-CN");
        yield return VerifyDrawer();
        GameEvents.InvokeBasicEvent(GameBasicEvent.Play);yield return null;yield return null;
        string previousBackground=null;
        // Existing profile is isolated by the validation project's product name.
        for(int level=1;level<=50;level++) {
            if(game.currentLevel!=level) { Finish("Navigation failed at "+level);yield break; }
            var pieces=new List<DraggableComponent>(DraggableComponent.Active);
            if(pieces.Count!=game.GetLevelData(level).pieces.Count) { Finish("Wrong piece count");yield break; }
            if(level==1 || level==10 || level==25 || level==50) Capture("level-"+level+"-tray");
            if(level==50) VerifyScreenProfiles();
            var background=FindObjectOfType<BackgroundComponent>();
            if(background.CurrentPath==previousBackground) failures.Add("Background repeated on level change");
            previousBackground=background.CurrentPath;
            int before=victories;
            // Full-size polygons may overlap, but each must retain a usable visible surface.
            var opening=new Rect(-2.351f,-5.001f,4.702f,4.702f);
            for(int a=0;a<pieces.Count;a++) {
                var bounds=pieces[a].GetComponent<Renderer>().bounds;
                if(!opening.Contains(bounds.min) || !opening.Contains(bounds.max)) failures.Add("Opening piece outside initial area "+level);
                if(pieces[a].transform.localScale!=Vector3.one) failures.Add("Opening piece scaled "+level);
                var own=new Paths64{pieces[a].GetWorldPath(PuzzleGeometry.Scale)};
                var covers=new Paths64();
                foreach(var piece in pieces) if(piece.sortingOrder>pieces[a].sortingOrder) covers.Add(piece.GetWorldPath(PuzzleGeometry.Scale));
                double visible=PuzzleGeometry.Area(Clipper.Difference(own,covers,FillRule.NonZero))/PuzzleGeometry.Area(own);
                if(visible<.119) failures.Add("Opening piece obscured "+level+": "+visible);
            }
            // Exercise real input picking through the overlapping collider stack.
            var openingLayout=OpeningLayouts.Get(game.GetLevelData(level).levelName);
            var input=FindObjectOfType<InputManager>();
            var begin=typeof(InputManager).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic);
            var cancel=typeof(InputManager).GetMethod("Cancel",BindingFlags.Instance|BindingFlags.NonPublic);
            for(int rank=pieces.Count-1;rank>=0;rank--) {
                var expected=pieces[rank];
                Vector2 grab=openingLayout.grabPoints[openingLayout.order[rank]];
                // Probe a central patch, not just one lucky pixel at a polygon edge.
                foreach(var offset in new[]{Vector2.zero,Vector2.left*.18f,Vector2.right*.18f,Vector2.up*.18f,Vector2.down*.18f}) {
                    begin.Invoke(input,new object[]{(Vector2)Camera.main.WorldToScreenPoint(grab+offset),-1});
                    if(!expected.IsDragging) failures.Add("Covered opening piece cannot be picked "+level+" rank "+rank);
                    cancel.Invoke(input,null);
                }
            }
            for(int rank=0;rank<pieces.Count;rank++) {
                pieces[rank].sortingOrder=rank+1;pieces[rank].GetComponent<Renderer>().sortingOrder=rank+1;
            }
            var sample=pieces[0];Vector3 origin=sample.transform.position,scale=sample.transform.localScale;
            sample.StartDragging(origin);sample.FollowMouse(Vector2.zero);sample.CancelDragging();
            if(sample.transform.position!=origin || sample.transform.localScale!=scale) failures.Add("Cancelled pickup lost initial layout "+level);
            var answer=sample.GetComponent<PuzzlePiece>().correctWorldPos;
            sample.StartDragging(sample.transform.position);sample.StopDragging(answer);
            sample.StartDragging(sample.transform.position);sample.StopDragging(new Vector2(-.31f,-2.4f));
            if(Vector3.Distance(sample.transform.position,new Vector3(-.31f,-2.4f,0))>.001f ||
                sample.transform.localScale!=Vector3.one || sample.isSnapped) failures.Add("Free board exit resized or repositioned piece "+level);
            // Beside and above the board must work, with no need to return to the initial area.
            foreach(var point in new[]{new Vector2(PuzzleScreen.DragBounds.xMax-.08f,1),new Vector2(0,4.65f)}) {
                sample.StartDragging(sample.transform.position);sample.StopDragging(point);
                if(Vector2.Distance(sample.transform.position,point)>.001f || sample.isSnapped)
                    failures.Add("Unsnapped free drop rejected "+level+" at "+point);
            }
            sample.StartDragging(sample.transform.position);sample.StopDragging(new Vector2(.42f,-3.15f));
            Vector3 free=sample.transform.position;
            sample.StartDragging(free);sample.FollowMouse(answer);sample.CancelDragging();
            if(sample.transform.position!=free || sample.transform.localScale!=Vector3.one) failures.Add("Cancel lost free position "+level);
            sample.StartDragging(free);sample.StopDragging(new Vector2(100,100));
            if(Vector2.Distance(sample.transform.position,PuzzleScreen.DragBounds.max)>.001f)
                failures.Add("Offscreen pointer lost reachable grab point "+level);
            sample.StartDragging(sample.transform.position);sample.StopDragging(free);
            var other=pieces[1];other.StartDragging(other.transform.position);other.StopDragging(free);
            if(Vector3.Distance(other.transform.position,free)>.001f || !other.CanDrag) failures.Add("Loose overlap was rejected "+level);
            if(victories!=before) failures.Add("Free placement triggered victory "+level);
            if(level==10) Capture("free-placement");
            for(int p=0;p<pieces.Count;p++) {
                var piece=pieces[p];var target=piece.GetComponent<PuzzlePiece>().correctWorldPos;
                piece.StartDragging(piece.transform.position);
                piece.FollowMouse(target+(Vector3)new Vector2(.025f,-.018f));
                piece.StopDragging(target+(Vector3)new Vector2(.025f,-.018f));
                if(!piece.isSnapped || Vector3.Distance(piece.transform.position,target)>.001f)
                    failures.Add($"Level {level} piece {p}: near-answer snap failed");
                if(p<pieces.Count-1 && victories!=before) failures.Add("Premature victory at "+level);
            }
            game.CheckFinish6();game.CheckFinish6();
            if(victories!=before+1) failures.Add($"Level {level}: expected one victory, got {victories-before}");
            if(!UserDataManager.IsCompleted(level)) failures.Add("Progress not saved "+level);
            Vector3 locked=sample.transform.position;sample.StartDragging(locked);sample.StopDragging(new Vector2(0,-2));
            if(sample.CanDrag || sample.transform.position!=locked) failures.Add("Completed board accepted a drag "+level);
            if(level==1 || level==10 || level==25 || level==50) Capture("level-"+level+"-complete");
            if(level<50) { GameEvents.InvokeBasicEvent(GameBasicEvent.NextLevel);yield return null;yield return null; }
        }
        yield return VerifyMenuNavigation();
        UserDataManager.Load(50);
        if(UserDataManager.GetCurrentLevel()!=50 || !UserDataManager.IsCompleted(50)) failures.Add("Final progress failed to reload");
        // Date-based refill must occur once, and must not discard earned rewards.
        UserDataManager.userData.hintDay=DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        UserDataManager.userData.hintCount=0;
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount,0);
        if(UserDataManager.GetHintCount()!=3) failures.Add("Daily refill failed");
        UserDataManager.ConsumeHintCount();
        if(UserDataManager.GetHintCount()!=2) failures.Add("Hints refilled twice in one day");
        UserDataManager.userData.hintDay=DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        UserDataManager.userData.hintCount=7;
        if(UserDataManager.GetHintCount()!=7) failures.Add("Daily refill discarded earned hints");
        if(UserDataManager.CompleteLevel(0)) failures.Add("Invalid level zero accepted");
        UserDataManager.Save(UserDataManager.userData);UserDataManager.Save(UserDataManager.userData);
        PlayerPrefs.SetString("ColorBlocks.Profile.v2","corrupted");UserDataManager.Load(50);
        if(UserDataManager.GetCurrentLevel()!=50) failures.Add("Backup recovery lost progress");
        yield return VerifyAdPolicy();
        yield return VerifyPresentation();
        Finish(null);
    }
    IEnumerator VerifyDrawer()
    {
        var hud=FindObjectOfType<GameHud>();var drawer=hud.Drawer;
        if(drawer.IsOpen || drawer.controls.blocksRaycasts) failures.Add("Drawer is not initially collapsed");
        if(hud.GetComponentInChildren<RawImage>()) failures.Add("In-level branding remains");
        var pointer=new PointerEventData(EventSystem.current) { pointerId=-1 };
        var cam=Camera.main;float size=cam.orthographicSize;Vector3 position=cam.transform.position;
        drawer.OnPointerDown(pointer);drawer.OnPointerClick(pointer);
        yield return new WaitForSecondsRealtime(.35f);
        if(!drawer.IsOpen || !drawer.IsSettled || !drawer.controls.blocksRaycasts) failures.Add("Handle click did not expand drawer");
        var lockedNext=Array.Find(drawer.controls.GetComponentsInChildren<Button>(),b=>b.name=="action.next");
        if(lockedNext.IsInteractable()) failures.Add("Locked next level is interactable");
        lockedNext.OnPointerClick(pointer);
        if(FindObjectOfType<GamePlay>().currentLevel!=1) failures.Add("Locked next button changed level");
        if(cam.transform.position!=position || Mathf.Abs(cam.orthographicSize-size)>.0001f) failures.Add("Drawer expansion moved board");
        Capture("drawer-expanded",393,852,new Rect(0,34,393,759));
        var sound=Array.Find(hud.GetComponentsInChildren<Button>(),b=>b.name=="action.sound");
        int clicks=0;Action clicked=()=>clicks++;
        GameEvents.RegisterBasicEvent(GameBasicEvent.UIClick,clicked);
        bool wasMuted=AudioComponent.Instance.IsMuted();sound.onClick.Invoke();
        GameEvents.UnregisterBasicEvent(GameBasicEvent.UIClick,clicked);
        if(clicks!=1) failures.Add("Button played click more than once: "+clicks);
        if(sound.GetComponentInChildren<PuzzleIcon>().Symbol!=(wasMuted?PuzzleIcon.Kind.Volume:PuzzleIcon.Kind.Muted)) failures.Add("Speaker icon state did not change");
        Capture("drawer-muted",393,852,new Rect(0,34,393,759));sound.onClick.Invoke();
        var hint=Array.Find(hud.GetComponentsInChildren<Button>(),b=>b.name=="action.hint");
        int hintsBefore=UserDataManager.GetHintCount();hint.onClick.Invoke();
        if(!HintWindow.IsShowing || UserDataManager.GetHintCount()!=hintsBefore-1) failures.Add("Eye icon did not open hint");
        drawer.OnPointerDown(pointer);drawer.OnPointerClick(pointer);
        if(!drawer.IsOpen) failures.Add("Hint overlay allowed drawer interaction");
        FindObjectOfType<HintWindow>().ClearPreviews();
        drawer.OnPointerDown(pointer);drawer.OnBeginDrag(pointer);
        pointer.delta=new Vector2(0,-PuzzleDrawer.Travel*hud.GetComponent<Canvas>().scaleFactor);
        drawer.OnDrag(pointer);drawer.OnEndDrag(pointer);yield return new WaitForSecondsRealtime(.35f);
        if(drawer.IsOpen || !drawer.IsSettled || drawer.controls.blocksRaycasts) failures.Add("Downward swipe did not collapse drawer");
        drawer.OnPointerDown(pointer);drawer.OnBeginDrag(pointer);pointer.delta=-pointer.delta;
        drawer.OnDrag(pointer);drawer.OnEndDrag(pointer);yield return new WaitForSecondsRealtime(.35f);
        if(!drawer.IsOpen || !drawer.IsSettled) failures.Add("Upward swipe did not expand drawer");
        var piece=DraggableComponent.Active[0];piece.StartDragging(piece.transform.position);piece.CancelDragging();
        yield return new WaitForSecondsRealtime(.35f);
        if(!drawer.IsOpen || !drawer.controls.blocksRaycasts) failures.Add("Picking up a piece closed drawer");
        drawer.SetOpen(false,true);
    }
    IEnumerator VerifyMenuNavigation()
    {
        var game=FindObjectOfType<GamePlay>();var drawer=FindObjectOfType<GameHud>().Drawer;
        var pointer=new PointerEventData(EventSystem.current) { pointerId=-1 };
        drawer.SetOpen(true,true);yield return null;
        var buttons=drawer.controls.GetComponentsInChildren<Button>();
        Array.Find(buttons,b=>b.name=="action.previous").OnPointerClick(pointer);
        yield return null;yield return null;
        if(game.currentLevel!=49 || !drawer.IsOpen) failures.Add("Previous icon failed to navigate with drawer open");
        drawer.SetOpen(true,true);yield return null;
        Array.Find(buttons,b=>b.name=="action.next").OnPointerClick(pointer);
        yield return null;yield return null;
        if(game.currentLevel!=50 || !drawer.IsOpen) failures.Add("Next icon failed to navigate with drawer open");
    }
    IEnumerator VerifyAdPolicy()
    {
        var service=AdService.Instance;
        var fake=new ControlledAds();
        typeof(AdService).GetField("provider",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(service,fake);
        int rewards=0;
        service.OfferReward(()=>rewards++);
        ClickDialog("action.cancel");
        if(fake.requests!=0 || rewards!=0) failures.Add("Ad opt-out launched an ad or granted reward");
        service.OfferReward(()=>rewards++);ClickDialog("action.watch");
        var old=fake.done;old(AdResult.Completed);old(AdResult.Completed);
        if(rewards!=1 || AdService.IsBusy) failures.Add("Completed ad did not settle exactly once");
        service.OfferReward(()=>rewards++);ClickDialog("action.watch");
        old(AdResult.Completed); // a previous request's delayed close must not pay again
        if(rewards!=1) failures.Add("Stale ad callback paid a reward");
        fake.done(AdResult.Skipped);ClickDialog("action.ok");
        if(rewards!=1 || AdService.IsBusy) failures.Add("Skipped ad rewarded or left controls locked");
        service.OfferReward(()=>rewards++);ClickDialog("action.watch");
        fake.done(AdResult.Failed);ClickDialog("action.ok");
        if(rewards!=1 || AdService.IsBusy) failures.Add("Failed ad rewarded or left controls locked");
        service.OfferReward(()=>rewards++);ClickDialog("action.watch");
        typeof(AdService).GetField("deadline",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(service,-1f);
        yield return null;yield return null;
        ClickDialog("action.ok");
        if(rewards!=1 || AdService.IsBusy) failures.Add("Ad timeout rewarded or left controls locked");
        // Exercise the actual gameplay callback: watching earns a stored use, without auto-consuming it.
        GameEvents.InvokeBasicEvent(GameBasicEvent.Play);yield return null;yield return null;
        UserDataManager.userData.hintDay=DateTime.Now.ToString("yyyy-MM-dd");
        UserDataManager.userData.hintCount=0;
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount,0);
        GameEvents.InvokeBasicEvent(GameBasicEvent.Look);
        Capture("reward-opt-in",393,852,new Rect(0,34,393,759));
        ClickDialog("action.watch");
        var earned=fake.done;earned(AdResult.Completed);earned(AdResult.Completed);
        if(UserDataManager.GetHintCount()!=1 || HintWindow.IsShowing) failures.Add("Earned use was not retained or was consumed automatically");
        UserDataManager.Load(50);
        if(UserDataManager.GetHintCount()!=1) failures.Add("Earned use failed to persist");
        GameEvents.InvokeBasicEvent(GameBasicEvent.Look);
        if(UserDataManager.GetHintCount()!=0 || !HintWindow.IsShowing) failures.Add("Explicit hint action did not consume one use");
        Capture("hint-preview",393,852,new Rect(0,34,393,759));
        FindObjectOfType<HintWindow>().ClearPreviews();
        typeof(AdService).GetField("started",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(service,-10000f);
        typeof(AdService).GetField("lastAd",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(service,-10000f);
        service.RecordCompletion(true,6);service.RecordCompletion(true,7);service.RecordCompletion(true,8);
        int transitions=0;
        if(!service.TryInterstitial(()=>transitions++)) failures.Add("Eligible interstitial did not start");
        var close=fake.done;close(AdResult.Failed);close(AdResult.Completed);
        if(transitions!=1 || AdService.IsBusy) failures.Add("Interstitial error did not resume once");
        service.RecordCompletion(true,9);service.RecordCompletion(true,10);service.RecordCompletion(true,11);
        if(service.TryInterstitial(()=>transitions++)) failures.Add("Interstitial cooldown was bypassed");
        typeof(AdService).GetField("lastAd",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(service,-10000f);
        int hintBefore=UserDataManager.GetHintCount();
        if(!service.TryInterstitial(()=>transitions++)) failures.Add("Interstitial close test did not start");
        close=fake.done;close(AdResult.Completed);close(AdResult.Completed);
        if(transitions!=2 || UserDataManager.GetHintCount()!=hintBefore) failures.Add("Interstitial close changed rewards or continued twice");
    }
    IEnumerator VerifyPresentation()
    {
        var background=FindObjectOfType<BackgroundComponent>();var seen=new HashSet<string>();
        for(int i=0;i<100 && seen.Count<background.AvailableCount;i++) {
            string before=background.CurrentPath;background.SelectNext();
            if(before==background.CurrentPath) failures.Add("Background selection repeated");
            if(seen.Add(background.CurrentPath)) Capture(Path.GetFileName(background.CurrentPath),393,852,new Rect(0,34,393,759));
        }
        if(seen.Count!=4) failures.Add("Expected four background variants");
        if(!GameText.SetLocale("en")) failures.Add("English table unavailable");
        yield return null;
        var title=FindObjectOfType<GameHud>().GetComponentsInChildren<Text>();
        if(!Array.Exists(title,t=>t.text==GameText.Get("level.Level_50.name"))) failures.Add("Live language switch did not update title");
        Capture("locale-en",393,852,new Rect(0,34,393,759));
        RuntimeDialog.Confirm("ad.reward.title","ad.reward.body","action.watch",null);
        Capture("locale-en-dialog",393,852,new Rect(0,34,393,759));
        foreach(var text in GameObject.Find("Dialog").GetComponentsInChildren<Text>()) {
            if(text.preferredHeight>text.rectTransform.rect.height+1) failures.Add("English dialog text clipped: "+text.text);
        }
        GameText.SetLocale("zh-CN");
        if(!Array.Exists(GameObject.Find("Dialog").GetComponentsInChildren<Text>(),t=>t.text==GameText.Get("ad.reward.title"))) failures.Add("Open dialog did not change language");
        ClickDialog("action.cancel");
        if(GameText.SetLocale("unknown-language") || GameText.Locale!="zh-CN") failures.Add("Missing locale changed selected language");
        FindObjectOfType<GameHud>().Drawer.SetOpen(false,true);
    }
    void ClickDialog(string name)
    {
        var dialog=GameObject.Find("Dialog");
        if(dialog) foreach(var button in dialog.GetComponentsInChildren<Button>())
            if(button.name==name) { button.onClick.Invoke();return; }
        failures.Add("Dialog button missing: "+name);
    }
    sealed class ControlledAds : IAdProvider
    {
        public Action<AdResult> done;public int requests;
        public bool RewardAvailable=>true;
        public bool InterstitialAvailable=>true;
        public void Initialize(DouyinConfig c) { }
        public void ShowRewarded(Action<AdResult> result,Action shown) { requests++;done=result;shown(); }
        public void ShowInterstitial(Action<AdResult> result,Action shown) { requests++;done=result;shown(); }
        public void Cancel() { }
        public void Dispose() { }
    }
    void VerifyScreenProfiles()
    {
        var profiles=new[] {
            PuzzleScreen.Calculate(360,640,new Rect(0,0,360,640)),
            PuzzleScreen.Calculate(375,667,new Rect(0,20,375,627)),
            PuzzleScreen.Calculate(393,852,new Rect(0,34,393,759)),
            PuzzleScreen.Calculate(412,915,new Rect(0,24,412,867)),
            PuzzleScreen.Calculate(720,1600,new Rect(0,48,720,1496)),
            PuzzleScreen.Calculate(1080,2400,new Rect(0,72,1080,2220)),
            PuzzleScreen.Calculate(768,1024,new Rect(0,20,768,984)),
            PuzzleScreen.Calculate(1000,1000,new Rect(0,24,1000,952)) };
        var drawer=FindObjectOfType<GameHud>().Drawer;
        foreach(var profile in profiles) {
            drawer.SetOpen(false,true);
            Capture($"screen-{profile.width}x{profile.height}",profile.width,profile.height,profile.safe,true);
            drawer.SetOpen(true,true);
            Capture($"screen-{profile.width}x{profile.height}-expanded",profile.width,profile.height,profile.safe,true);
        }
        drawer.SetOpen(false,true);
        File.WriteAllText(Path.Combine(reportFolder,"screen-validation.txt"),
            "8 viewport profiles: 360x640, 375x667, 393x852, 412x915, 720x1600, 1080x2400, 768x1024, 1000x1000.\n"+
            "Verified initial layout, title and collapsed/expanded drawer in safe area; icon controls; no in-level logo or restart; 16 rendered screenshots. Simulated viewports; physical devices and Douyin host not tested.");
    }
    void Capture(string name,int width=540,int height=960,Rect? safe=null,bool verify=false)
    {
        var cam=Camera.main;if(!cam) return;
        var preview=PuzzleScreen.Preview;
        PuzzleScreen.Preview=PuzzleScreen.Calculate(width,height,safe??new Rect(0,0,width,height));
        var target=new RenderTexture(width,height,24);var old=cam.targetTexture;var active=RenderTexture.active;
        cam.targetTexture=target;PuzzleScreen.FitCamera(cam);
        var overlays=new List<Canvas>();
        foreach(var canvas in FindObjectsOfType<Canvas>()) if(canvas.renderMode==RenderMode.ScreenSpaceOverlay) {
            overlays.Add(canvas);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        }
        var hud=FindObjectOfType<GameHud>();hud.RefreshLayout();
        foreach(var layout in FindObjectsOfType<SafePuzzleCanvas>()) layout.Refresh();
        foreach(var background in FindObjectsOfType<BackgroundComponent>()) background.Refresh();
        Canvas.ForceUpdateCanvases();
        if(verify) {
            var m=PuzzleScreen.Current;
            var probe=new PointerEventData(EventSystem.current) { position=new Vector2(m.safe.center.x,m.safe.yMin+240*m.scale) };
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(probe,hits);
            bool drawerHit=hits.Exists(hit=>hit.gameObject.transform.IsChildOf(hud.Drawer.panel));
            if(drawerHit!=hud.Drawer.IsOpen) failures.Add("Drawer hit testing mismatch: "+name);
            var world=PuzzleScreen.WorldContent;
            var min=cam.WorldToScreenPoint(world.min);var max=cam.WorldToScreenPoint(world.max);
            if(min.x<m.content.xMin-.1f || min.y<m.content.yMin-.1f || max.x>m.content.xMax+.1f || max.y>m.content.yMax+.1f)
                failures.Add("Board or tray clipped: "+name);
            var corners=new Vector3[4];
            foreach(var button in hud.GetComponentsInChildren<Button>()) {
                var group=button.GetComponentInParent<CanvasGroup>();
                if(group && group.alpha<.01f) continue;
                if(button.name=="重来") failures.Add("Restart control remains");
                button.GetComponent<RectTransform>().GetWorldCorners(corners);
                foreach(var c in corners) {
                    var pt=cam.WorldToScreenPoint(c);
                    if(pt.x<m.safe.xMin-.1f || pt.y<m.safe.yMin-.1f || pt.x>m.safe.xMax+.1f || pt.y>m.safe.yMax+.1f)
                        failures.Add("Unsafe button "+button.name+": "+name);
                }
            }
        }
        cam.Render();RenderTexture.active=target;
        var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
        if(name=="hint-preview") {
            var previews=FindObjectsOfType<PolygonGraphic>();
            if(previews.Length!=12) failures.Add("Hint preview piece count mismatch");
            foreach(var graphic in previews) {
                var data=(PieceData)typeof(PolygonGraphic).GetField("piece",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(graphic);
                Vector2 centroid=Vector2.zero;foreach(var vertex in data.vertices) centroid+=vertex;
                centroid/=data.vertices.Count;
                Vector3 screenPoint=cam.WorldToScreenPoint(graphic.rectTransform.TransformPoint(centroid*190));
                var pixel=texture.GetPixel(Mathf.Clamp(Mathf.RoundToInt(screenPoint.x),0,width-1),Mathf.Clamp(Mathf.RoundToInt(screenPoint.y),0,height-1));
                if(Vector4.Distance(pixel,data.color)>.15f) failures.Add("Hint polygon was not visibly rendered: "+data.color);
            }
        }
        File.WriteAllBytes(Path.Combine(reportFolder,name+".png"),texture.EncodeToPNG());
        cam.targetTexture=old;RenderTexture.active=active;target.Release();Destroy(target);Destroy(texture);
        foreach(var canvas in overlays) canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        PuzzleScreen.Preview=preview;PuzzleScreen.FitCamera(cam);hud.RefreshLayout();
        foreach(var layout in FindObjectsOfType<SafePuzzleCanvas>()) layout.Refresh();
    }
    void Finish(string extra)
    {
        if(extra!=null) failures.Add(extra);
        string result=failures.Count==0?$"PASS: 50 actual level sessions; {victories} victories; free placement beside/above/below board, stable picked-up size, loose overlap, cancellation/viewport edge recovery, completed-board lock, near-answer snapping, no premature/duplicate victory, navigation, daily hints, final progress reload, invalid level rejection and corrupted-save backup recovery.\nPASS: drawer click/swipe, hit testing, icon states, single click sound, full-size visible opening pieces and manually retained menu state; four nonrepeating backgrounds and live Chinese/English text; 8 viewport/safe-area profiles with collapsed and expanded screenshots.\nPASS: controlled ad provider tests: opt-out, complete, skip, error, timeout, duplicate/stale callbacks, retained/persisted +1 hint, explicit consumption, interstitial close/error recovery without rewards and cooldown. Real TTSDK/device ads remain untested.":string.Join("\n",failures);
        File.WriteAllText(Path.Combine(reportFolder,"play-validation.txt"),result);
        SessionState.SetBool("Blocks.BatchPlay",false);
        Application.logMessageReceived-=OnLog;
        EditorApplication.Exit(failures.Count==0?0:1);
    }
}

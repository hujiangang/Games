using System;
using UnityEngine;

/// <summary>Reward policy and pacing independent from the platform adapter.</summary>
public class AdService : MonoBehaviour
{
    public static AdService Instance { get; private set; }
    public static bool IsBusy => (Instance && Instance.gate.Pending) || RuntimeDialog.IsOpen || DouyinHost.IsHidden;
    private IAdProvider provider;
    private DouyinConfig config;
    private readonly AdRequestGate gate=new();
    private float started,lastAd=-10000,deadline;
    private int clears,latestLevel;
    private Action<AdResult> timeoutCompletion;
    void Awake()
    {
        if(Instance && Instance!=this) { Destroy(this);return; }
        Instance=this;started=Time.realtimeSinceStartup;
        config=DouyinConfig.Load();
#if BLOCKS_DOUYIN
        provider=new DouyinAdProvider();
#else
        provider=new UnavailableAdProvider();
#endif
#if UNITY_EDITOR
        if(config.editorSimulation) provider=new EditorAdProvider();
#endif
        try { provider.Initialize(config); }
        catch(Exception e) { Debug.LogWarning("Ad initialization failed: "+e.GetType().Name);provider=new UnavailableAdProvider(); }
    }
    public void OfferReward(Action reward)
    {
        if(IsBusy) return;
        if(!provider.RewardAvailable) {
            RuntimeDialog.Message("dialog.notice","ad.unavailable");return;
        }
        RuntimeDialog.Confirm("ad.reward.title","ad.reward.body","action.watch",()=>Begin(true,result=> {
            if(result==AdResult.Completed) reward?.Invoke();
            else RuntimeDialog.Message("dialog.notice",result==AdResult.Skipped?"ad.skipped":"ad.failed");
        }));
    }
    public void RecordCompletion(bool firstClear,int level) { if(firstClear) { clears++;latestLevel=level; } }
    public bool TryInterstitial(Action next)
    {
        float now=Time.realtimeSinceStartup;
        if(IsBusy || !provider.InterstitialAvailable || latestLevel<config.firstInterstitialLevel ||
            clears<Mathf.Max(3,config.interstitialEveryClears) || now-started<Mathf.Max(90,config.initialDelaySeconds) ||
            now-lastAd<Mathf.Max(120,config.interstitialCooldownSeconds)) return false;
        clears=0;
        Begin(false,result=>next?.Invoke());
        return true;
    }
    void Begin(bool reward,Action<AdResult> completed)
    {
        int token=gate.Begin();
        deadline=Time.realtimeSinceStartup+Mathf.Clamp(config.loadTimeoutSeconds,5,30);
        AudioComponent.Instance?.SetAdPaused(true);
        Action<AdResult> finish=result=> {
            if(!gate.Complete(token)) return;
            timeoutCompletion=null;
            lastAd=Time.realtimeSinceStartup;
            try { provider.Cancel(); } catch(Exception e) { Debug.LogWarning("Ad cleanup: "+e.GetType().Name); }
            AudioComponent.Instance?.SetAdPaused(false);
            completed?.Invoke(result);
        };
        timeoutCompletion=finish;
        Action shown=()=> { if(gate.Pending) { lastAd=Time.realtimeSinceStartup;deadline=Time.realtimeSinceStartup+180; } };
        try {
            if(reward) provider.ShowRewarded(finish,shown); else provider.ShowInterstitial(finish,shown);
        } catch(Exception e) { Debug.LogWarning("Ad request failed: "+e.GetType().Name);finish(AdResult.Failed); }
    }
    void Update() { if(gate.Pending && Time.realtimeSinceStartup>deadline) timeoutCompletion?.Invoke(AdResult.Timeout); }
    void OnApplicationFocus(bool focused) { if(focused && gate.Pending) deadline=Mathf.Max(deadline,Time.realtimeSinceStartup+5); }
    public void ResumeFromHost() { if(gate.Pending) deadline=Mathf.Max(deadline,Time.realtimeSinceStartup+5); }
    void OnDestroy()
    {
        gate.Cancel();timeoutCompletion=null;provider?.Dispose();
        AudioComponent.Instance?.SetAdPaused(false);
        if(Instance==this) Instance=null;
    }
}
#if UNITY_EDITOR
public sealed class EditorAdProvider : IAdProvider
{
    public bool RewardAvailable => true;
    public bool InterstitialAvailable => true;
    public void Initialize(DouyinConfig c) { }
    public void ShowRewarded(Action<AdResult> done,Action shown)
    {
        shown();
        RuntimeDialog.Confirm("editor.reward.title","editor.ad.body","editor.complete",()=>done(AdResult.Completed),()=>done(AdResult.Skipped));
    }
    public void ShowInterstitial(Action<AdResult> done,Action shown)
    { shown();RuntimeDialog.Confirm("editor.interstitial.title","editor.ad.body","editor.close",()=>done(AdResult.Completed),()=>done(AdResult.Skipped)); }
    public void Cancel() { }
    public void Dispose() { }
}
#endif

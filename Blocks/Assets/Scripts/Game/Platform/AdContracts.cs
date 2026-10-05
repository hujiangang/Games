using System;

public enum AdResult { Completed, Skipped, Unavailable, Failed, Timeout }
public interface IAdProvider : IDisposable
{
    bool RewardAvailable { get; }
    bool InterstitialAvailable { get; }
    void Initialize(DouyinConfig config);
    void ShowRewarded(Action<AdResult> done,Action shown);
    void ShowInterstitial(Action<AdResult> done,Action shown);
    void Cancel();
}
[Serializable]
public class DouyinConfig
{
    public string appId="",rewardedVideoAdId="",interstitialAdId="";
    public bool enabled=false,editorSimulation=false;
    public int firstInterstitialLevel=6,interstitialEveryClears=3;
    public float interstitialCooldownSeconds=120,initialDelaySeconds=90,loadTimeoutSeconds=20;
    public static DouyinConfig Load()
    {
        var asset=UnityEngine.Resources.Load<UnityEngine.TextAsset>("Config/Douyin");
        return asset?UnityEngine.JsonUtility.FromJson<DouyinConfig>(asset.text):new DouyinConfig();
    }
}
/// <summary>A completed request can be settled only once; stale callbacks cannot settle a new one.</summary>
public sealed class AdRequestGate
{
    private int token;
    public bool Pending { get; private set; }
    public int Begin() { Pending=true;return ++token; }
    public bool Complete(int id) { if(!Pending || id!=token) return false;Pending=false;return true; }
    public void Cancel() { Pending=false;token++; }
}
public sealed class UnavailableAdProvider : IAdProvider
{
    public bool RewardAvailable => false;
    public bool InterstitialAvailable => false;
    public void Initialize(DouyinConfig config) { }
    public void ShowRewarded(Action<AdResult> done,Action shown) => done(AdResult.Unavailable);
    public void ShowInterstitial(Action<AdResult> done,Action shown) => done(AdResult.Unavailable);
    public void Cancel() { }
    public void Dispose() { }
}

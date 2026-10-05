#if BLOCKS_DOUYIN
using System;
using TTSDK;

/// <summary>TTSDK 6.2.1+ adapter. Enable BLOCKS_DOUYIN only after importing the official SDK.</summary>
public sealed class DouyinAdProvider : IAdProvider
{
    private DouyinConfig config;
    private bool initialized;
    private bool supportsInterstitial;
    private TTRewardedVideoAd rewarded;
    private TTInterstitialAd interstitial;
    private int generation;
    private bool disposed;
    public bool RewardAvailable => initialized && !string.IsNullOrWhiteSpace(config.rewardedVideoAdId);
    public bool InterstitialAvailable => initialized && supportsInterstitial && !string.IsNullOrWhiteSpace(config.interstitialAdId);
    public void Initialize(DouyinConfig value)
    {
        config=value;
        if(!config.enabled || string.IsNullOrWhiteSpace(config.appId)) return;
        TT.InitSDK((code,env)=> {
            if(disposed) return;
            initialized=code==0 && TT.InContainerEnv && env.GameAppId==config.appId;
            supportsInterstitial=initialized && env.m_HostEnum==HostEnum.Douyin;
            if(initialized) DouyinHost.Attach();
        });
    }
    public void ShowRewarded(Action<AdResult> done,Action shown)
    {
        if(!RewardAvailable) { done(AdResult.Unavailable);return; }
        Cancel();int request=++generation;bool displayed=false;
        rewarded=TT.CreateRewardedVideoAd(new CreateRewardedVideoAdParam { AdUnitId=config.rewardedVideoAdId,Multiton=false });
        var ad=rewarded;
        ad.OnLoad+=()=> { if(request!=generation || displayed) return;displayed=true;try { shown();ad.Show(); } catch(Exception) { done(AdResult.Failed); } };
        ad.OnClose+=(ended,count)=> { if(request==generation) done(ended && count>=1?AdResult.Completed:AdResult.Skipped); };
        ad.OnError+=(code,message)=> { if(request==generation) done(AdResult.Failed); };
        ad.Load();
    }
    public void ShowInterstitial(Action<AdResult> done,Action shown)
    {
        if(!InterstitialAvailable) { done(AdResult.Unavailable);return; }
        Cancel();int request=++generation;bool displayed=false;
        interstitial=TT.CreateInterstitialAd(new CreateInterstitialAdParam { InterstitialAdId=config.interstitialAdId });
        var ad=interstitial;
        ad.OnLoad+=()=> { if(request!=generation || displayed) return;displayed=true;try { shown();ad.Show(); } catch(Exception) { done(AdResult.Failed); } };
        ad.OnClose+=()=> { if(request==generation) done(AdResult.Completed); };
        ad.OnError+=(code,message)=> { if(request==generation) done(AdResult.Failed); };
        ad.Load();
    }
    public void Cancel()
    {
        generation++;
        var r=rewarded;var i=interstitial;rewarded=null;interstitial=null;
        r?.Destroy();i?.Destroy();
    }
    public void Dispose() { disposed=true;initialized=false;DouyinHost.Detach();Cancel(); }
}
#endif

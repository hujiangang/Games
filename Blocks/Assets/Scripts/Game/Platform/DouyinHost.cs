using System.Collections.Generic;
using UnityEngine;
#if BLOCKS_DOUYIN
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
#endif

/// <summary>Host lifecycle and sidebar capability. Desktop builds expose no unavailable entry.</summary>
public static class DouyinHost
{
    public static bool SidebarAvailable { get; private set; }
    public static bool IsHidden { get; private set; }
#if BLOCKS_DOUYIN
    private static bool attached;
#endif
    public static void Attach()
    {
#if BLOCKS_DOUYIN
        if(attached) return;
        attached=true;IsHidden=false;
        TT.GetAppLifeCycle().OnHide+=Hide;
        TT.GetAppLifeCycle().OnShow+=Show;
        TT.CheckScene(TTSideBar.SceneEnum.SideBar,value=>SidebarAvailable=value,()=>{},(code,message)=>SidebarAvailable=false);
#endif
    }
    static void Hide()
    {
        IsHidden=true;
        foreach(var piece in DraggableComponent.Active) if(piece) piece.CancelDragging();
        UserDataManager.Save(UserDataManager.userData);
        AudioComponent.Instance?.SetHostPaused(true);
    }
    static void Show(Dictionary<string,object> args)
    {
        IsHidden=false;AudioComponent.Instance?.SetHostPaused(false);
        AdService.Instance?.ResumeFromHost();
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount,UserDataManager.GetHintCount());
    }
    public static void OpenSidebar()
    {
#if BLOCKS_DOUYIN
        if(!SidebarAvailable || AdService.IsBusy) return;
        TT.NavigateToScene(new JsonData { ["scene"]="sidebar" },()=>{},()=>{},
            (code,message)=>RuntimeDialog.Message("sidebar.title","sidebar.failed"));
#endif
    }
    public static void Detach()
    {
#if BLOCKS_DOUYIN
        if(attached) {
            TT.GetAppLifeCycle().OnHide-=Hide;
            TT.GetAppLifeCycle().OnShow-=Show;
        }
        attached=false;
#endif
        SidebarAvailable=false;IsHidden=false;
    }
}

using System;
using UnityEngine;

public enum GameBasicEvent {
    CheckFinish,Play,Look,PrevLevel,NextLevel,TurnAudio,UpdateLevel,CompleteLevel,UpdateAudio,
    StartGameOprate,ResetUI,UpdateLookCount,PieceSnapped,PieceDraggedStart,UIClick
}
public static class GameEvents
{
    private static readonly EventBus<GameBasicEvent> bus=new();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { bus.Clear();DraggableComponent.Active.Clear();GamePlay.isGlobalLocked=false;GamePlay.IsStartOperation=false; }
    public static void RegisterBasicEvent(GameBasicEvent e,Action action) => bus.Subscribe(e,action);
    public static void UnregisterBasicEvent(GameBasicEvent e,Action action) => bus.Unsubscribe(e,action);
    public static void InvokeBasicEvent(GameBasicEvent e) => bus.Get<Action>(e)?.Invoke();
    public static void RegisterEvent<T>(GameBasicEvent e,Action<T> a) => bus.Subscribe(e,a);
    public static void RegisterEvent<T,U>(GameBasicEvent e,Action<T,U> a) => bus.Subscribe(e,a);
    public static void RegisterEvent<T,U,V>(GameBasicEvent e,Action<T,U,V> a) => bus.Subscribe(e,a);
    public static void RegisterEvent<T,U,V,W,X>(GameBasicEvent e,Action<T,U,V,W,X> a) => bus.Subscribe(e,a);
    public static void UnregisterEvent<T>(GameBasicEvent e,Action<T> a) => bus.Unsubscribe(e,a);
    public static void UnregisterEvent<T,U>(GameBasicEvent e,Action<T,U> a) => bus.Unsubscribe(e,a);
    public static void UnregisterEvent<T,U,V>(GameBasicEvent e,Action<T,U,V> a) => bus.Unsubscribe(e,a);
    public static void UnregisterEvent<T,U,V,W,X>(GameBasicEvent e,Action<T,U,V,W,X> a) => bus.Unsubscribe(e,a);
    public static void InvokeEvent<T>(GameBasicEvent e,T a) => bus.Get<Action<T>>(e)?.Invoke(a);
    public static void InvokeEvent<T,U>(GameBasicEvent e,T a,U b) => bus.Get<Action<T,U>>(e)?.Invoke(a,b);
    public static void InvokeEvent<T,U,V>(GameBasicEvent e,T a,U b,V c) => bus.Get<Action<T,U,V>>(e)?.Invoke(a,b,c);
    public static void InvokeEvent<T,U,V,W,X>(GameBasicEvent e,T a,U b,V c,W d,X f) => bus.Get<Action<T,U,V,W,X>>(e)?.Invoke(a,b,c,d,f);
}

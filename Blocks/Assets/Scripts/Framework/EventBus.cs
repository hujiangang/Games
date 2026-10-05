using System;
using System.Collections.Generic;

/// <summary>Typed event channels. Different payload signatures never share a delegate.</summary>
public sealed class EventBus<TKey>
{
    private readonly Dictionary<(TKey,Type),Delegate> listeners=new();
    public void Subscribe<T>(TKey key,T handler) where T:Delegate
    {
        var channel=(key,typeof(T));listeners.TryGetValue(channel,out var old);
        listeners[channel]=Delegate.Combine(old,handler);
    }
    public void Unsubscribe<T>(TKey key,T handler) where T:Delegate
    {
        var channel=(key,typeof(T));
        if(!listeners.TryGetValue(channel,out var old)) return;
        var next=Delegate.Remove(old,handler);
        if(next==null) listeners.Remove(channel);else listeners[channel]=next;
    }
    public T Get<T>(TKey key) where T:Delegate => listeners.TryGetValue((key,typeof(T)),out var action)?(T)action:null;
    public void Clear() => listeners.Clear();
}

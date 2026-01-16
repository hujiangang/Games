using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameBasicEvent
{
    CheckFinish, // 检查是否完成.
    Play, // 重新开始.
    Look, // 看一眼.
    PrevLevel, // 上一关.
    NextLevel, // 下一关.
    TurnAudio, // 开关声音.
    UpdateLevel, // 更新关卡.
    CompleteLevel, // 完成一关.
    UpdateAudio, // 更新音频开关.

    StartGameOprate, // 开始游戏操作.

    ResetUI,    // 重置UI相关.
    UpdateLookCount, // 更新看题次数.
}

/// <summary>
/// 游戏事件系统，支持无参数和带参数的事件
/// </summary>
public static class GameEvents
{
    // 无参数事件字典
    private static readonly Dictionary<GameBasicEvent, Action> _basicEvents = new();
    
    // 泛型事件字典，支持带参数的事件
    private static readonly Dictionary<GameBasicEvent, Delegate> _genericEvents = new();
    
    /// <summary>
    /// 注册无参数事件
    /// </summary>
    /// <param name="@event">事件类型</param>
    /// <param name="action">事件处理函数</param>
    public static void RegisterBasicEvent(GameBasicEvent @event, Action action)
    {
        if (!_basicEvents.ContainsKey(@event))
        {
            _basicEvents.Add(@event, action);
        } 
        else
        {
            _basicEvents[@event] += action;
        }
    }

    /// <summary>
    /// 注销无参数事件
    /// </summary>
    /// <param name="@event">事件类型</param>
    /// <param name="action">事件处理函数</param>
    public static void UnregisterBasicEvent(GameBasicEvent @event, Action action)
    {
        if (_basicEvents.ContainsKey(@event))
        {
            _basicEvents[@event] -= action;
            // 如果没有更多订阅者，移除事件
            if (_basicEvents[@event] == null)
            {
                _basicEvents.Remove(@event);
            }
        }
    }

    /// <summary>
    /// 触发无参数事件
    /// </summary>
    /// <param name="@event">事件类型</param>
    public static void InvokeBasicEvent(GameBasicEvent @event)
    {
        if (_basicEvents.TryGetValue(@event, out var action))
        {
            action?.Invoke();
        }
    }
    
    /// <summary>
    /// 注册带1个参数的泛型事件
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="action">事件处理函数</param>
    public static void RegisterEvent<T>(GameBasicEvent eventName, Action<T> action)
    {
        if (!_genericEvents.ContainsKey(eventName))
        {
            _genericEvents.Add(eventName, action);
        }
        else
        {
            _genericEvents[eventName] = Delegate.Combine(_genericEvents[eventName], action);
        }
    }
    
    /// <summary>
    /// 注册带2个参数的泛型事件
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="action">事件处理函数</param>
    public static void RegisterEvent<T1, T2>(GameBasicEvent eventName, Action<T1, T2> action)
    {
        if (!_genericEvents.ContainsKey(eventName))
        {
            _genericEvents.Add(eventName, action);
        }
        else
        {
            _genericEvents[eventName] = Delegate.Combine(_genericEvents[eventName], action);
        }
    }
    
    /// <summary>
    /// 注册带3个参数的泛型事件
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="action">事件处理函数</param>
    public static void RegisterEvent<T1, T2, T3>(GameBasicEvent eventName, Action<T1, T2, T3> action)
    {
        if (!_genericEvents.ContainsKey(eventName))
        {
            _genericEvents.Add(eventName, action);
        }
        else
        {
            _genericEvents[eventName] = Delegate.Combine(_genericEvents[eventName], action);
        }
    }
    
    /// <summary>
    /// 注销带1个参数的泛型事件
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="action">事件处理函数</param>
    public static void UnregisterEvent<T>(GameBasicEvent eventName, Action<T> action)
    {
        if (_genericEvents.ContainsKey(eventName))
        {
            _genericEvents[eventName] = Delegate.Remove(_genericEvents[eventName], action);
            // 如果没有更多订阅者，移除事件
            if (_genericEvents[eventName] == null)
            {
                _genericEvents.Remove(eventName);
            }
        }
    }
    
    /// <summary>
    /// 注销带2个参数的泛型事件
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="action">事件处理函数</param>
    public static void UnregisterEvent<T1, T2>(GameBasicEvent eventName, Action<T1, T2> action)
    {
        if (_genericEvents.ContainsKey(eventName))
        {
            _genericEvents[eventName] = Delegate.Remove(_genericEvents[eventName], action);
            // 如果没有更多订阅者，移除事件
            if (_genericEvents[eventName] == null)
            {
                _genericEvents.Remove(eventName);
            }
        }
    }
    
    /// <summary>
    /// 注销带3个参数的泛型事件
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="action">事件处理函数</param>
    public static void UnregisterEvent<T1, T2, T3>(GameBasicEvent eventName, Action<T1, T2, T3> action)
    {
        if (_genericEvents.ContainsKey(eventName))
        {
            _genericEvents[eventName] = Delegate.Remove(_genericEvents[eventName], action);
            // 如果没有更多订阅者，移除事件
            if (_genericEvents[eventName] == null)
            {
                _genericEvents.Remove(eventName);
            }
        }
    }
    
    /// <summary>
    /// 触发带1个参数的泛型事件
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="arg">事件参数</param>
    public static void InvokeEvent<T>(GameBasicEvent eventName, T arg)
    {
        if (_genericEvents.TryGetValue(eventName, out var action))
        {
            (action as Action<T>)?.Invoke(arg);
        }
    }
    
    /// <summary>
    /// 触发带2个参数的泛型事件
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="arg1">事件参数1</param>
    /// <param name="arg2">事件参数2</param>
    public static void InvokeEvent<T1, T2>(GameBasicEvent eventName, T1 arg1, T2 arg2)
    {
        if (_genericEvents.TryGetValue(eventName, out var action))
        {
            (action as Action<T1, T2>)?.Invoke(arg1, arg2);
        }
    }
    
    /// <summary>
    /// 触发带3个参数的泛型事件
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <param name="arg1">事件参数1</param>
    /// <param name="arg2">事件参数2</param>
    /// <param name="arg3">事件参数3</param>
    public static void InvokeEvent<T1, T2, T3>(GameBasicEvent eventName, T1 arg1, T2 arg2, T3 arg3)
    {
        if (_genericEvents.TryGetValue(eventName, out var action))
        {
            (action as Action<T1, T2, T3>)?.Invoke(arg1, arg2, arg3);
        }
    }


    public static void InvokeEvent<T1, T2, T3, T4,T5>(GameBasicEvent eventName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (_genericEvents.TryGetValue(eventName, out var action))
        {
            (action as Action<T1, T2, T3, T4,T5>)?.Invoke(arg1, arg2, arg3, arg4, arg5);    
        }
    }

    public static void UnregisterEvent<T1, T2, T3, T4,T5>(GameBasicEvent eventName, Action<T1, T2, T3, T4, T5> action)
    {
        if (_genericEvents.ContainsKey(eventName))
        {
            _genericEvents[eventName] = Delegate.Remove(_genericEvents[eventName], action);
            if (_genericEvents[eventName] == null)
            {
                _genericEvents.Remove(eventName);
            }
        }
    }

     public static void RegisterEvent<T1, T2, T3, T4,T5>(GameBasicEvent eventName, Action<T1, T2, T3, T4, T5> action)
    {
        if (!_genericEvents.ContainsKey(eventName))
        {
            _genericEvents.Add(eventName, action);
        }
        else
        {
            _genericEvents[eventName] = Delegate.Combine(_genericEvents[eventName], action);
        }
    }

}

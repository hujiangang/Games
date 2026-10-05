using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class UserData
{
    public int currentLevel=1,hintCount=3;
    public long saveTime;
    public string hintDay;
    public List<bool> levelUnlockStatus=new();
}

/// <summary>Versioned local profile with backup and one-time import of the old file.</summary>
public static class UserDataManager
{
    private const string Key="ColorBlocks.Profile.v2",BackupKey=Key+".backup";
    private const string LegacyKey="STARRIOR_SECRET_KEY_2024";
    public const int DayMaxHintCount=3;
    public static UserData userData;
    public static void Load(int total)
    {
        total=Mathf.Max(1,total);
        userData=Read(PlayerPrefs.GetString(Key,"")) ?? Read(PlayerPrefs.GetString(BackupKey,"")) ?? ReadLegacy() ?? new UserData();
        userData.levelUnlockStatus ??= new List<bool>();
        while(userData.levelUnlockStatus.Count<total) userData.levelUnlockStatus.Add(false);
        userData.currentLevel=Mathf.Clamp(userData.currentLevel,1,total);
        userData.hintCount=Mathf.Clamp(userData.hintCount,0,99);
        RefreshDailyHints();
        Save(userData);
    }
    static UserData Read(string json)
    {
        if(string.IsNullOrWhiteSpace(json)) return null;
        try {
            var data=JsonUtility.FromJson<UserData>(json);
            return data!=null && data.currentLevel>=1 && data.levelUnlockStatus!=null?data:null;
        } catch(Exception) { return null; }
    }
    static UserData ReadLegacy()
    {
        try {
            string path=Path.Combine(Application.persistentDataPath,"user_profile.dat");
            if(!File.Exists(path)) return null;
            string encrypted=Encoding.UTF8.GetString(File.ReadAllBytes(path));
            var decoded=new StringBuilder();
            for(int i=0;i<encrypted.Length;i++) decoded.Append((char)(encrypted[i]^LegacyKey[i%LegacyKey.Length]));
            var data=Read(decoded.ToString());
            if(data!=null && data.saveTime>0 && data.saveTime<=DateTime.MaxValue.Ticks)
                data.hintDay=new DateTime(data.saveTime).ToString("yyyy-MM-dd");
            return data;
        } catch(Exception e) { Debug.LogWarning("Legacy profile import failed: "+e.GetType().Name);return null; }
    }
    static bool RefreshDailyHints()
    {
        if(userData==null) return false;
        string today=DateTime.Now.ToString("yyyy-MM-dd");
        if(string.IsNullOrEmpty(userData.hintDay)) { userData.hintDay=today;return true; }
        if(string.CompareOrdinal(today,userData.hintDay)<=0) return false;
        userData.hintDay=today;
        userData.hintCount=Mathf.Max(userData.hintCount,DayMaxHintCount);
        return true;
    }
    public static void Save(UserData data)
    {
        if(data==null) return;
        try {
            var previous=PlayerPrefs.GetString(Key,"");
            if(Read(previous)!=null) PlayerPrefs.SetString(BackupKey,previous);
            data.saveTime=DateTime.Now.Ticks;
            PlayerPrefs.SetString(Key,JsonUtility.ToJson(data));PlayerPrefs.Save();
        } catch(Exception e) { Debug.LogError("Profile save failed: "+e.GetType().Name); }
    }
    public static int GetCurrentLevel() => userData?.currentLevel ?? 1;
    public static int GetHintCount()
    {
        if(RefreshDailyHints()) Save(userData);
        return userData?.hintCount ?? 0;
    }
    public static void IncHintCount() { RefreshDailyHints();userData.hintCount=Mathf.Min(99,userData.hintCount+1);Save(userData); }
    public static bool ConsumeHintCount()
    {
        if(GetHintCount()<=0) return false;
        userData.hintCount--;Save(userData);return true;
    }
    public static bool IsCompleted(int level) => userData!=null && level>=1 &&
        level<=userData.levelUnlockStatus.Count && userData.levelUnlockStatus[level-1];
    public static bool CompleteLevel(int level)
    {
        if(userData==null || level<1 || level>userData.levelUnlockStatus.Count || level>userData.currentLevel) return false;
        userData.levelUnlockStatus[level-1]=true;
        userData.currentLevel=Mathf.Max(userData.currentLevel,Mathf.Min(level+1,userData.levelUnlockStatus.Count));
        Save(userData);return true;
    }
}

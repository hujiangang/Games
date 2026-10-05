using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class LevelPersistence
{
    private static bool ValidName(string name) => name!=null && Regex.IsMatch(name,@"^Level_[1-9][0-9]*$");
    public static void Save(LevelData data)
    {
#if UNITY_EDITOR
        if(data==null || !ValidName(data.levelName) || !LevelValidator.Validate(data,out _)) { Debug.LogError("Cannot save invalid level.");return; }
        string folder=Path.Combine(Application.dataPath,"Resources/LevelsData");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder,data.levelName+".json"),JsonUtility.ToJson(data,true));
        UnityEditor.AssetDatabase.Refresh();
#else
        Debug.LogWarning("Level authoring is only available in the Unity Editor.");
#endif
    }
    public static LevelData Load(string name)
    {
        if(!ValidName(name)) return null;
        var asset=Resources.Load<TextAsset>("LevelsData/"+name);
        if(!asset) return null;
        try {
            var data=JsonUtility.FromJson<LevelData>(asset.text);
            if(LevelValidator.Validate(data,out var error)) return data;
            Debug.LogError(name+": "+error);return null;
        } catch(Exception e) { Debug.LogError(name+": "+e.GetType().Name);return null; }
    }
    public static string[] GetAvailableLevels()
    {
        var names=new List<string>();
        foreach(var asset in Resources.LoadAll<TextAsset>("LevelsData")) if(ValidName(asset.name)) names.Add(asset.name);
        names.Sort((a,b)=>int.Parse(a.Substring(6)).CompareTo(int.Parse(b.Substring(6))));
        return names.ToArray();
    }
    public static int GetSumLevel()
    {
        var names=GetAvailableLevels();
        for(int i=0;i<names.Length;i++) if(names[i]!="Level_"+(i+1)) { Debug.LogError("Level numbering must be contiguous.");return i; }
        return names.Length;
    }
}

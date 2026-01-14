using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class LevelPersistence {
    // 关卡文件夹路径：项目根目录/Levels
    private static string FolderPath => Application.dataPath + "/LevelsData/";

    public static void Save(LevelData data) {
        if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FolderPath + data.levelName + ".json", json);
        Debug.Log("关卡已存至: " + FolderPath + data.levelName + ".json");
    }

    public static LevelData Load(string levelName) {
        string path = FolderPath + levelName + ".json";
        if (File.Exists(path)) {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<LevelData>(json);
        }
        return null;
    }

    public static string[] GetAvailableLevels()
    {
        if (!Directory.Exists(FolderPath)) return new string[0];
        string[] files = Directory.GetFiles(FolderPath, "*.json");
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        return files;
    }
    
    public static int GetSumLevel()
    {
        return GetAvailableLevels().Length;
    }
}
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class LevelPersistence
{
    // 直接指向Resources/LevelsData目录（保存/加载都用这个路径）
    private static string ResourcesLevelPath => Application.dataPath + "/Resources/LevelsData/";
    private const string ResourcesLoadFolder = "LevelsData"; // Resources加载时的文件夹名

    /// <summary>
    /// 直接保存到Resources/LevelsData目录（全平台通用：编辑器/安卓）.
    /// </summary>
    public static void Save(LevelData data)
    {
        // 安全校验
        if (data == null || string.IsNullOrEmpty(data.levelName))
        {
            Debug.LogError("保存失败：LevelData为空或levelName未设置");
            return;
        }

        // 创建Resources/LevelsData目录（不存在则创建）
        if (!Directory.Exists(ResourcesLevelPath))
        {
            Directory.CreateDirectory(ResourcesLevelPath);
        }

        // 序列化JSON并保存到Resources目录
        string json = JsonUtility.ToJson(data, true);
        string fullPath = ResourcesLevelPath + data.levelName + ".json";
        File.WriteAllText(fullPath, json);

        // 提示刷新资源（编辑器需刷新才能识别新保存的文件）
        Debug.Log($"关卡已直接保存到Resources目录：{fullPath}\n提示：保存后请点击 Unity → Assets → Refresh 刷新资源");
    }

    /// <summary>
    /// 加载关卡数据（全平台通用：编辑器/安卓，直接读Resources）
    /// </summary>
    public static LevelData Load(string levelName)
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogError("加载失败：levelName为空");
            return null;
        }

        // 全平台统一从Resources加载（无需区分）
        string resourcePath = $"{ResourcesLoadFolder}/{levelName}";
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

        if (jsonAsset == null)
        {
            Debug.LogError($"未找到关卡数据：{resourcePath}（请检查Resources/LevelsData目录下是否有{levelName}.json）");
            return null;
        }

        return JsonUtility.FromJson<LevelData>(jsonAsset.text);
    }

    /// <summary>
    /// 获取所有可用关卡列表（全平台通用，直接读Resources）
    /// </summary>
    public static string[] GetAvailableLevels()
    {
        TextAsset[] levelAssets = Resources.LoadAll<TextAsset>(ResourcesLoadFolder);
        List<string> levelList = new List<string>();

        foreach (TextAsset asset in levelAssets)
        {
            if (!string.IsNullOrEmpty(asset.name) && asset.name.StartsWith("Level_"))
            {
                levelList.Add(asset.name);
            }
        }

        // 按关卡数字排序
        levelList.Sort((a, b) =>
        {
            int numA = int.TryParse(a.Replace("Level_", ""), out int aNum) ? aNum : 0;
            int numB = int.TryParse(b.Replace("Level_", ""), out int bNum) ? bNum : 0;
            return numA.CompareTo(numB);
        });

        return levelList.ToArray();
    }

    /// <summary>
    /// 获取总关卡数（全平台通用）
    /// </summary>
    public static int GetSumLevel()
    {
        return GetAvailableLevels().Length;
    }
}
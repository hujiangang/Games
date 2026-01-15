using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


[Serializable]
public class UserData
{
    /// <summary>
    /// 当前玩到的关卡.
    /// </summary>
    public int currentLevel = 1;

    /// <summary>
    /// 剩余看一眼（提示）的次数.
    /// </summary>
    public int hintCount = 5;

    /// <summary>
    /// 关卡解锁列表.
    /// </summary>
    public List<bool> levelUnlockStatus; 

    // 默认构造函数（用于第一次初始化游戏）
    public UserData()
    {
        currentLevel = 1;
        hintCount = 1;
        levelUnlockStatus = new List<bool>();
    }
}

public static class UserDataManager
{
    private static string SavePath = Path.Combine(Application.persistentDataPath, "user_profile.dat");

    /// <summary>
    /// 加密密钥，可以随便改.
    /// </summary>
    private const string EncryptionKey = "STARRIOR_SECRET_KEY_2024";

    /// <summary>
    /// 用户数据.
    /// </summary>
    public static UserData userData;


    // 保存数据
    public static void Save(UserData data)
    {
        try
        {
            // 1. 转为 JSON 字符串
            string json = JsonUtility.ToJson(data);
            
            // 2. 加密（异或算法，简单高效，足以对付普通玩家）
            string encryptedJson = XorEncryptDecrypt(json, EncryptionKey);
            
            // 3. 转为字节并写入文件
            byte[] bytes = Encoding.UTF8.GetBytes(encryptedJson);
            File.WriteAllBytes(SavePath, bytes);
            
            Debug.Log("数据已安全保存至: " + SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError("保存失败: " + e.Message);
        }
    }

    // 读取数据
    public static void Load(int totalLevelsCount)
    {
        if (!File.Exists(SavePath))
        {
            userData = CreateDefaultData(totalLevelsCount);
            return;
        }

        try
        {
            // 1. 读取二进制
            byte[] bytes = File.ReadAllBytes(SavePath);
            string encryptedJson = Encoding.UTF8.GetString(bytes);
            
            // 2. 解密
            string json = XorEncryptDecrypt(encryptedJson, EncryptionKey);
            
            // 3. 反序列化
            UserData data = JsonUtility.FromJson<UserData>(json);

            // 检查列表长度（如果关卡增加了，需要补齐长度）
            while (data.levelUnlockStatus.Count < totalLevelsCount)
            {
                data.levelUnlockStatus.Add(false);
            }

            userData = data;

            // 校正.
            if (userData.currentLevel > userData.levelUnlockStatus.Count)
            {
                userData.currentLevel = userData.levelUnlockStatus.Count;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("读取失败，可能文件损坏: " + e.Message);
            userData = CreateDefaultData(totalLevelsCount);
        }
    }

    // 简单异或加密/解密
    private static string XorEncryptDecrypt(string text, string key)
    {
        StringBuilder result = new();
        for (int i = 0; i < text.Length; i++)
        {
            result.Append((char)(text[i] ^ key[i % key.Length]));
        }
        return result.ToString();
    }

    // 初始默认数据
    private static UserData CreateDefaultData(int count)
    {
        userData = new ()
        {
            hintCount = 1,
            currentLevel = 1
        };
        for (int i = 0; i < count; i++)
        {
            userData.levelUnlockStatus.Add(false);
        }
        Save(userData);
        return userData;
    }


    /// <summary>
    /// 增加提示次数.
    /// </summary>
    public static void IncHintCount()
    {
        userData.hintCount++;
        Save(userData);
    }


    public static int GetHintCount()
    {
        return userData.hintCount;
    }

    public static int GetCurrentLevel()
    {
        return userData.currentLevel;
    }

    /// <summary>
    /// 完成关卡.
    /// </summary>
    /// <param name="level"></param>
    public static bool CompleteLevel(int level)
    {
        Debug.Log($"CompleteLevel: {level}");
        if (level < 0 || level > userData.levelUnlockStatus.Count)
        {
            Debug.LogError("无效的关卡号");
            return false;
        }

        int nextLevel = level + 1;
        bool saveFlag = false;
        if (nextLevel > userData.currentLevel &&
            nextLevel <= userData.levelUnlockStatus.Count)
        {
            userData.currentLevel = nextLevel;
            saveFlag = true;
        }

        int index = level - 1;
        if (!userData.levelUnlockStatus[index])
        {
            userData.levelUnlockStatus[index] = true;
            saveFlag = true;
        }

        if (saveFlag)
        {
            Save(userData);
        }
        return true;
    }


}

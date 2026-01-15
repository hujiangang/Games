using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LevelEditor : MonoBehaviour {
    public TextMeshProUGUI levelNameText; // UI 文本，用于显示当前关卡名

    private int sumLevel = 0; // 总关卡数
    private string currentLevelName; // 当前编辑的关卡名

    void Start() {
        RefreshLevelList();
    }

    // --- 功能 1：新建关卡 ---
    public void CreateNewLevel()
    {

        Cutter cutter = FindObjectOfType<Cutter>();
        foreach (var p in cutter.activePieces) Destroy(p.gameObject);
        cutter.activePieces.Clear();
        cutter.CreateInitialSquare();

        RefreshLevelList();

        sumLevel++;
        currentLevelName = "Level_" + sumLevel;
        levelNameText.text = currentLevelName;
    }

    public void Reset()
    {
        Cutter cutter = FindObjectOfType<Cutter>();
        foreach (var p in cutter.activePieces) Destroy(p.gameObject);
        cutter.activePieces.Clear();
        cutter.CreateInitialSquare();
    }
    
    public void SetCurrentLevelName(int level)
    {
        currentLevelName = "Level_" + level;
        levelNameText.text = currentLevelName;
        Reset();
    }

    // --- 功能 2：保存当前关卡 ---
    public void SaveCurrentLevel() {
        if (string.IsNullOrEmpty(currentLevelName)) {
            Debug.LogError("请输入关卡名称！");
            return;
        }

        LevelData data = new ();
        data.levelName = currentLevelName;
        Cutter cutter = FindObjectOfType<Cutter>();
        foreach (var p in cutter.activePieces) {
            PieceData pd = new ();
            pd.vertices = p.points;
            pd.color = p.GetComponent<MeshRenderer>().material.color;
            data.pieces.Add(pd);
        }

        LevelPersistence.Save(data);
        RefreshLevelList();
    }

    private void RefreshLevelList() {
        string[] levels = LevelPersistence.GetAvailableLevels();
        sumLevel = levels.Length;
    }
}
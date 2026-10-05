using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LevelEditorController : MonoBehaviour {
    public CutterManager cutter; // 引用之前的切割逻辑
    public TMP_Dropdown levelDropdown; // UI 下拉列表，用于选择已有关卡

    public TextMeshProUGUI levelNameText; // UI 文本，用于显示当前关卡名

    private int sumLevel = 0; // 总关卡数
    private string currentLevelName; // 当前编辑的关卡名

    void Start() {
        RefreshLevelList();
    }

    // --- 功能 1：新建关卡 ---
    public void CreateNewLevel() {
        // 清理当前场景
        foreach (var p in cutter.activePieces) Destroy(p.gameObject);
        cutter.activePieces.Clear();

        // 创建初始大正方形
        List<Vector2> initPoints = new List<Vector2> { 
            new Vector2(-CutterManager.cutterLength, CutterManager.cutterLength), new Vector2(CutterManager.cutterLength, CutterManager.cutterLength), 
            new Vector2(CutterManager.cutterLength, -CutterManager.cutterLength), new Vector2(-CutterManager.cutterLength, -CutterManager.cutterLength) 
        };
        cutter.activePieces.Add(cutter.CreatePiece(initPoints));
        sumLevel++;
        currentLevelName = "Level_" + sumLevel;
        levelNameText.text = currentLevelName;
    }

    // --- 功能 2：保存当前关卡 ---
    public void SaveCurrentLevel() {
        if (string.IsNullOrEmpty(currentLevelName)) {
            Debug.LogError("请输入关卡名称！");
            return;
        }

        LevelData data = new LevelData();
        data.levelName = currentLevelName;
        foreach (var p in cutter.activePieces) {
            PieceData pd = new PieceData();
            pd.vertices = p.points;
            pd.color = p.GetComponent<MeshRenderer>().material.color;
            data.pieces.Add(pd);
        }

        LevelPersistence.Save(data);
        RefreshLevelList();
    }

    // --- 功能 3：编辑/加载选中的关卡 ---
    public void EditSelectedLevel() {
        string selectedName = levelDropdown.options[levelDropdown.value].text;
        LevelData data = LevelPersistence.Load(selectedName);

        if (data != null) {
            // 清理并加载
            foreach (var p in cutter.activePieces) Destroy(p.gameObject);
            cutter.activePieces.Clear();

            foreach (var pd in data.pieces) {
                PuzzlePiece p = cutter.CreatePiece(pd.vertices);
                p.GetComponent<MeshRenderer>().material.color = pd.color;
                cutter.activePieces.Add(p);
            }
            Debug.Log("加载关卡进行编辑: " + selectedName);
            levelNameText.text = currentLevelName;
            currentLevelName = selectedName;
        }
    }

    private void RefreshLevelList() {
        string[] levels = LevelPersistence.GetAvailableLevels();
        sumLevel = levels.Length;
        levelDropdown.ClearOptions();
        levelDropdown.AddOptions(new List<string>(levels));
    }
}
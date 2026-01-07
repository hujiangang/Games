using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GamePlay : MonoBehaviour {
    public Material pieceMaterial;
    public string levelToLoad = "Level_1"; // 要玩的关卡名
    
    [Header("区域设置")]
    public Rect trayArea = new Rect(-2.5f, -4.5f, 5f, 2f); // 底部托盘范围

    private List<DraggableComponent> allPieces = new List<DraggableComponent>();

    private bool isLevelFinished = false;

    void Start() {
        DrawTargetFrame();
        LoadAndStartGame();
    }

    void LoadAndStartGame()
    {
        string path = Application.dataPath + "/LevelsData/" + levelToLoad + ".json";
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        LevelData data = JsonUtility.FromJson<LevelData>(json);
        int piecesCount = 0;

        foreach (var pd in data.pieces)
        {
            // 1. 创建碎片物体
            piecesCount++;
            GameObject go = new($"GamePiece_{piecesCount}");
            PuzzlePiece pp = go.AddComponent<PuzzlePiece>(); // 使用你之前的脚本生成 Mesh
            pp.Init(pd.vertices, pieceMaterial, pd.color);

            // 2. 添加游戏逻辑
            DraggableComponent gp = go.AddComponent<DraggableComponent>();
            gp.targetPos = Vector3.zero; // 因为你是对正方形做的切割，中心通常是 0,0

            Vector3 targetFrameOffset = GameObject.Find("TargetFrame").transform.position;
            gp.correctWorldPos = pp.transform.position + targetFrameOffset;

            // 3. 打乱位置到托盘区 (Tray Zone)
            float randomX = Random.Range(trayArea.xMin, trayArea.xMax);
            float randomY = Random.Range(trayArea.yMin, trayArea.yMax);
            go.transform.position = new Vector3(randomX, randomY, 0);

            // 随机旋转增加难度(不需要旋转,本身不能旋转的).
            //go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-30, 30));

            allPieces.Add(gp);
        }
    }
    
    /// <summary>
    /// 画出目标区域框.
    /// </summary>
    void DrawTargetFrame()
    {
        GameObject frame = new GameObject("TargetFrame");
        float offsetY = 2.5f; 
        frame.transform.position = new Vector3(0, offsetY, 0);
        LineRenderer lr = frame.AddComponent<LineRenderer>();

        lr.useWorldSpace = false;
        
        Vector3[] corners = new Vector3[5];
        corners[0] = new Vector3(-CutterManager.cutterLength, CutterManager.cutterLength, 0.1f);
        corners[1] = new Vector3(CutterManager.cutterLength, CutterManager.cutterLength, 0.1f);
        corners[2] = new Vector3(CutterManager.cutterLength, -CutterManager.cutterLength, 0.1f);
        corners[3] = new Vector3(-CutterManager.cutterLength, -CutterManager.cutterLength, 0.1f);
        //corners[4] = new Vector3(-CutterManager.cutterLength, CutterManager.cutterLength, 0.1f); // 闭合

        lr.positionCount = 4;
        lr.SetPositions(corners);
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = pieceMaterial;
        lr.loop = true;

        lr.numCornerVertices = 5; 
        lr.numCapVertices = 5;
    }

    public void CheckWinCondition()
    {

        if (isLevelFinished) return;
        
        bool allSnapped = true;
        foreach (var p in allPieces)
        {
            if (!p.isSnapped)
            {
                allSnapped = false;
                break;
            }
        }

        if (allSnapped)
        {
            isLevelFinished = true;
            DraggableComponent.isGlobalLocked = true;
            Debug.Log("恭喜！拼图完成！");
            // 这里可以弹出胜利 UI
        }
    }



    // 在编辑器里画出托盘区域，方便调试
    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(trayArea.center.x, trayArea.center.y, 0), 
                           new Vector3(trayArea.width, trayArea.height, 0));
    }
}
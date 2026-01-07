using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GamePlay : MonoBehaviour {
    public Material pieceMaterial;
    public string levelToLoad = "Level_1"; // 要玩的关卡名
    
    [Header("区域设置")]
    public Rect trayArea = new(-2.5f, -4.5f, 5f, 2f); // 底部托盘范围

    private List<DraggableComponent> allPieces = new();

    private bool isLevelFinished = false;

    public static bool isGlobalLocked = false;

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

        Vector3 framePos = GameObject.Find("TargetFrame").transform.position;
        float len = CutterManager.cutterLength;
        Rect squareBounds = new(framePos.x - len, framePos.y - len, len * 2, len * 2);

        foreach (var pd in data.pieces)
        {
            // 1. 创建碎片物体
            piecesCount++;
            GameObject go = new($"GamePiece_{piecesCount}");
            PuzzlePiece pp = go.AddComponent<PuzzlePiece>(); // 使用你之前的脚本生成 Mesh
            pp.Init(pd.vertices, pieceMaterial, pd.color);

            // 2. 添加游戏逻辑
            DraggableComponent gp = go.AddComponent<DraggableComponent>();
            gp.Init(squareBounds, pp.transform.position + framePos, framePos);

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
        GameObject frame = new("TargetFrame");
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
            GamePlay.isGlobalLocked = true;
            Debug.Log("恭喜！拼图完成！");
            // 这里可以弹出胜利 UI
        }
    }
    
    public void CheckWinCondition2() {
        GameObject frame = new("TargetFrame");
        // 1. 获取正方形范围
        Bounds b = frame.GetComponent<Collider2D>().bounds;
        
        // 2. 采样检测点 (比如每 0.5 一个点)
        int pointsFilled = 0;
        int totalSamples = 0;

        for (float x = b.min.x + 0.1f; x < b.max.x; x += 0.3f) {
            for (float y = b.min.y + 0.1f; y < b.max.y; y += 0.3f) {
                totalSamples++;
                // 发射极短的射线检测这里是否有碎片
                if (Physics2D.OverlapPoint(new Vector2(x, y))) {
                    pointsFilled++;
                }
            }
        }

        // 3. 如果 98% 的点都被覆盖了，说明拼图完成
        float fillPercent = (float)pointsFilled / totalSamples;
        if (fillPercent > 0.98f) {
          
        }
    }



    // 在编辑器里画出托盘区域，方便调试
    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(trayArea.center.x, trayArea.center.y, 0), 
                           new Vector3(trayArea.width, trayArea.height, 0));
    }
}
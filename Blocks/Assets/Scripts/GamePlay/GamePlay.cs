using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class GamePlay : MonoBehaviour {
    public Material pieceMaterial;
    public string levelToLoad = "Level_1"; // 要玩的关卡名
    
    [Header("托盘区域设置")]
    public Vector3 spawnCenter = new Vector3(0, -5, 0); // 圆心位置（通常在屏幕下方）
    public float spawnRadius = 2.5f;   

    private List<DraggableComponent> allPieces = new();

    private bool isLevelFinished = false;

    public static bool isGlobalLocked = false;

    private Rect targetFrameRect;

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
        targetFrameRect = new(framePos.x - len, framePos.y - len, len * 2, len * 2);

        foreach (var pd in data.pieces)
        {
            // 1. 创建碎片物体
            piecesCount++;
            GameObject go = new($"GamePiece_{piecesCount}");
            PuzzlePiece pp = go.AddComponent<PuzzlePiece>(); // 使用你之前的脚本生成 Mesh
            pp.Init(pd.vertices, pieceMaterial, pd.color);

            // 2. 添加游戏逻辑
            DraggableComponent gp = go.AddComponent<DraggableComponent>();
            gp.Init(targetFrameRect, pp.transform.position + framePos, framePos);

            //以 spawnCenter 为中心，在 spawnRadius 半径内随机取点.
            float minRadius = 0.5f; // 中间留空
            float r = Random.Range(minRadius, spawnRadius);
            Vector2 dir = Random.insideUnitCircle.normalized; // 取一个随机方向
            Vector3 randomPos = spawnCenter + (Vector3)(dir * r);
            gp.transform.position = randomPos;

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
        float lineWidth = 0.1f;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        // 【核心修改】：外扩坐标
        // 为了让碎片的边缘正好对准框的“内边缘”，框的路径点要外扩 半个线宽
        float padding = lineWidth / 2f;
        float L = CutterManager.cutterLength + padding;
        
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(-L, L, 0.1f);
        corners[1] = new Vector3(L, L, 0.1f);
        corners[2] = new Vector3(L, -L, 0.1f);
        corners[3] = new Vector3(-L, -L, 0.1f);
        //corners[4] = new Vector3(-CutterManager.cutterLength, CutterManager.cutterLength, 0.1f); // 闭合

        lr.positionCount = 4;
        lr.SetPositions(corners);

        lr.material = pieceMaterial;
        lr.startColor = lr.endColor = new Color(0.2627451f, 0.2941177f, 0.3372549f, 1);

        lr.loop = true;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
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
        
        // 1. 采样检测点 (比如每 0.5 一个点)
        int pointsFilled = 0;
        int totalSamples = 0;

        for (float x = targetFrameRect.min.x + 0.1f; x < targetFrameRect.max.x; x += 0.3f) {
            for (float y = targetFrameRect.min.y + 0.1f; y < targetFrameRect.max.y; y += 0.3f) {
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
            GamePlay.isGlobalLocked = true;
            Debug.Log("恭喜！拼图完成！");
        }
    }



    // 在编辑器里画出托盘区域，方便调试
    void OnDrawGizmos() {

         // 画出打乱碎片的圆形区域
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }
}
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using Clipper2Lib;

/// <summary>
/// 关卡解锁状态.
/// </summary>
public enum LevelUnlockStatus
{
    Locked,
    Unlocked,
    Current,
}

public class GamePlay : MonoBehaviour
{
    /// <summary>
    /// 是否全局锁定，防止操作.
    /// </summary>
    public static bool isGlobalLocked = false;

    public Material pieceMaterial;

    [Header("托盘区域设置")]
    public Vector3 spawnCenter = new(0, -5, 0);
    public float spawnRadius = 2.5f;

    private readonly List<DraggableComponent> allPieces = new();

    /// <summary>
    /// 缓存外框面积.
    /// </summary>
    private double frameArea;

    /// <summary>
    /// 所有碎片的多边形碰撞器.
    /// </summary>
    readonly List<PolygonCollider2D> allPiecePolys = new();

    Vector2[] framePoints;
    private Rect targetFrameRect;

    //关卡数据.
    public int currentLevel = 0;
    private int sumLevel = 0;
    private int selectLevel = 0;
    private LevelData currLevelData;
    private readonly Dictionary<int, LevelData> levelDataDict = new();

    /// <summary>
    /// 是否静音.
    /// </summary>
    private bool IsMute = false;

    /// <summary>
    /// 是否开始操作.
    /// </summary>
    public static bool IsStartOperation = false;

    // 缓存目标框对象，避免重复Find
    private GameObject targetFrameObj;
    // 目标框边长（缓存）
    private float frameSideLength;

    void Awake()
    {
        sumLevel = LevelPersistence.GetSumLevel();
        UserDataManager.Load(sumLevel);
        currentLevel = UserDataManager.GetCurrentLevel();
        selectLevel = currentLevel;
    }

    void Start()
    {
        DrawTargetFrame();
        LoadAndStartGame();
        ComputeFrameArea();

        StartCoroutine(DelayInit());
    }

    IEnumerator DelayInit()
    {
        yield return new WaitForSeconds(0.5f);
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLevel, currentLevel, sumLevel, LevelUnlockStatus.Current);
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount, UserDataManager.GetHintCount());
    }

    /// <summary>
    /// 获取关卡数据.
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public LevelData GetLevelData(int level)
    {
        // 限制关卡范围
        level = Mathf.Clamp(level, 1, sumLevel);

        // 优先从缓存读取，避免重复加载
        if (levelDataDict.TryGetValue(level, out LevelData data))
        {
            return data;
        }

        Debug.Log($"GetLevelData: 加载关卡 {level}");
        string levelToLoad = $"Level_{level}";
        data = LevelPersistence.Load(levelToLoad);

        if (data == null)
        {
            Debug.LogError($"加载关卡 {level} 失败，请检查关卡数据是否存在");
            return null;
        }

        // 加入缓存，下次直接读取
        levelDataDict[level] = data;
        return data;
    }

    /// <summary>
    /// 清除所有碎片.
    /// </summary>
    private void ClearAllPieces()
    {
        foreach (var piece in allPieces)
        {
            if (piece != null) Destroy(piece.gameObject);
        }
        allPieces.Clear();
        allPiecePolys.Clear();
    }

    /// <summary>
    /// 加载并开始游戏.
    /// </summary>
    void LoadAndStartGame()
    {
        Debug.Log($"LoadAndStartGame: {currentLevel}");
        LevelData data = GetLevelData(currentLevel);
        if (data == null) return;
        int piecesCount = 0;
        currLevelData = data;

        // 容错：目标框为空时使用默认值
        Vector3 framePos = targetFrameObj != null ? targetFrameObj.transform.position : Vector3.zero;
        frameSideLength = CutterManager.cutterLength;
        targetFrameRect = new(framePos.x - frameSideLength, framePos.y - frameSideLength, frameSideLength * 2, frameSideLength * 2);
        int sumPieceCount = data.pieces.Count;

        DraggableComponent.globalTopOrder = sumPieceCount + 1;

        foreach (var pd in data.pieces)
        {
            // 1. 创建碎片物体
            piecesCount++;
            GameObject go = new($"GamePiece_{piecesCount}")
            {
                tag = "PuzzlePiece",
                layer = LayerMask.NameToLayer("PuzzlePiece"),
            };
            // 使用你之前的脚本生成 Mesh.
            PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
            pp.Init(pd.vertices, pieceMaterial, pd.color, piecesCount);

            allPiecePolys.Add(pp.GetComponent<PolygonCollider2D>());
            pp.correctWorldPos = pp.transform.position + framePos;

            // 2. 添加游戏逻辑
            DraggableComponent gp = go.AddComponent<DraggableComponent>();
            gp.Init(targetFrameRect, pp.transform.position + framePos, framePos, piecesCount);

            //以 spawnCenter 为中心，在 spawnRadius 半径内随机取点.
            float minRadius = 0.8f; // 中间留空
            float r = Random.Range(minRadius, spawnRadius);
            Vector2 dir = Random.insideUnitCircle.normalized; // 取一个随机方向
            Vector3 endPos = spawnCenter + (Vector3)(dir * r);

            const float dropHeight = 10f;
            Vector3 startPos = endPos + Vector3.up * dropHeight;
            go.transform.position = startPos;

            // 3. 开始掉落
            FallAndEnableDrag fader = go.AddComponent<FallAndEnableDrag>();
            fader.BeginFall(startPos, endPos);

            allPieces.Add(gp);
        }
    }

    /// <summary>
    /// 画出目标区域框.
    /// </summary>
    void DrawTargetFrame()
    {
        // 先尝试查找，没有则创建，同时赋值给缓存变量
        targetFrameObj = GameObject.Find("TargetFrame");
        if (targetFrameObj == null)
        {
            targetFrameObj = new GameObject("TargetFrame");
            Debug.LogWarning("未找到TargetFrame，自动创建一个");
        }

        float offsetY = 2.5f;
        targetFrameObj.transform.position = new Vector3(0, offsetY, 0);
        LineRenderer lr = targetFrameObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = targetFrameObj.AddComponent<LineRenderer>();
        }

        lr.useWorldSpace = false;
        float lineWidth = 0.1f;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        // 外扩坐标，让碎片边缘对准框的内边缘
        float padding = lineWidth / 1.5f;
        float L = CutterManager.cutterLength + padding;
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(-L, L, 0.1f);
        corners[1] = new Vector3(L, L, 0.1f);
        corners[2] = new Vector3(L, -L, 0.1f);
        corners[3] = new Vector3(-L, -L, 0.1f);

        lr.positionCount = 4;
        lr.SetPositions(corners);

        lr.material = pieceMaterial;
        lr.startColor = lr.endColor = new Color(0.2627451f, 0.2941177f, 0.3372549f, 1);

        lr.loop = true;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
    }

    public void CheckFinish6()
    {
        if (isGlobalLocked) return;

        // 使用较高的 Scale 保证 Clipper 整数运算精度
        const double Scale = 2000.0;
        PuzzlePiece[] pieces = FindObjectsOfType<PuzzlePiece>(); // 确保获取的是碎片组件

        if (pieces == null || pieces.Length == 0) return;

        // 1. 统计吸附情况
        int snappedPieceCount = 0;
        foreach (var p in pieces)
        {
            // 假设你的 DraggableComponent 挂在 PuzzlePiece 同一级或可以通过 GetComponent 获取
            var draggable = p.GetComponent<DraggableComponent>();
            if (draggable != null && draggable.isSnapped)
            {
                snappedPieceCount++;
            }
        }

        // 强制校验：如果吸附数量没达标，直接判错（提高性能，避免复杂几何运算）
        if (snappedPieceCount < pieces.Length)
        {
            Debug.Log($"[拼图检测] ✗ 未完成：吸附进度({snappedPieceCount}/{pieces.Length})");
            return;
        }

        // 2. 收集所有碎片的世界坐标路径并合并
        Paths64 allPaths = new Paths64();
        int piecesInFrame = 0;

        foreach (var p in pieces)
        {
            // 将本地坐标点 points 转换为世界坐标 Path64
            Path64 path = GetPieceWorldPath(p, Scale);
            if (path.Count >= 3)
            {
                allPaths.Add(path);
                
                // 简单的框内检测
                if (targetFrameRect.Contains(p.transform.position))
                {
                    piecesInFrame++;
                }
            }
        }

        // 3. 执行合并（Union）
        // 因为是无缝切割，完美拼合时 Union 后的结果应该只有一个大的多边形
        Paths64 unionPieces = Clipper.Union(allPaths, FillRule.NonZero);

        // 4. 微量膨胀 (Healing)
        // 现在的缝隙几乎为0，膨胀 0.0005 只是为了让那些由于 Transform 坐标精度
        // 导致的“极微小缝隙”（肉眼不可见）在合并时彻底连通。
        double tinyInflate = 0.0005 * Scale; 
        Paths64 healedPieces = Clipper.InflatePaths(unionPieces, tinyInflate, 
            JoinType.Miter, EndType.Polygon, 2.0);

        // 5. 与目标框进行比对
        Paths64 framePaths = CreateTargetFramePath(Scale);
        Paths64 intersection = Clipper.Intersect(healedPieces, framePaths, FillRule.NonZero);

        // 6. 面积计算
        double totalIntersectionArea = 0;
        double maxIslandArea = 0;

        foreach (var path in intersection)
        {
            double a = System.Math.Abs(Clipper.Area(path));
            totalIntersectionArea += a;
            if (a > maxIslandArea) maxIslandArea = a;
        }

        // 反缩放回世界坐标系的面积
        double finalArea = totalIntersectionArea / (Scale * Scale);
        double finalMaxIsland = maxIslandArea / (Scale * Scale);
        double coverageRatio = finalArea / frameArea;
        double mainIslandRatio = finalMaxIsland / frameArea;

        Debug.Log($"[检测反馈] 覆盖率:{coverageRatio:P2}, 最大连通率:{mainIslandRatio:P2}, 框内:{piecesInFrame}/{pieces.Length}");

        // 7. 最终判定条件
        // 因为修复了缝隙，现在的覆盖率要求可以适当提高，更加严谨
        bool isAllInFrame = piecesInFrame >= pieces.Length;
        bool isCoverageHigh = coverageRatio >= 0.99f; // 无缝切割后，99% 是非常安全的
        bool isMainIslandComplete = mainIslandRatio >= 0.98f; // 绝大部分面积必须是连在一起的一块

        if (isCoverageHigh && isMainIslandComplete && isAllInFrame)
        {
            Debug.Log("✓ [拼图完成] 完美契合！");
            DoVictory();
        }
        else
        {
            // 给出具体不通过的原因
            string reason = !isAllInFrame ? "碎片位置偏移" : 
                            (!isCoverageHigh ? "覆盖率不足" : "未完全拼合成整体");
            Debug.Log($"✗ [未完成] 原因: {reason}");
        }
    }

    /// <summary>
    /// 创建目标框的标准化路径（复用现有缓存数据，避免重复计算）
    /// </summary>
    private Paths64 CreateTargetFramePath(double scale)
    {
        Paths64 framePaths = new Paths64();
        Path64 framePath = new Path64();

        if (targetFrameObj == null || frameSideLength <= 0)
        {
            Debug.LogError("目标框对象或尺寸无效");
            return framePaths;
        }

        Vector3 framePos = targetFrameObj.transform.position;
        float halfLength = frameSideLength;

        // 构建目标框四个顶点（顺时针）
        Vector2[] vertices = new Vector2[]
        {
            new(framePos.x - halfLength, framePos.y + halfLength),
            new(framePos.x + halfLength, framePos.y + halfLength),
            new(framePos.x + halfLength, framePos.y - halfLength),
            new(framePos.x - halfLength, framePos.y - halfLength)
        };

        foreach (var v in vertices)
        {
            framePath.Add(new Point64(v.x * scale, v.y * scale));
        }

        framePaths.Add(framePath);
        return framePaths;
    }

    /// <summary>
    /// 计算目标区域的面积（优化版，增加容错）
    /// </summary>
    private void ComputeFrameArea()
    {
        if (targetFrameObj == null)
        {
            Debug.LogError("目标框为空，无法计算面积");
            frameArea = 36; // 默认值（6x6）
            return;
        }

        Vector3 framePos = targetFrameObj.transform.position;
        float L = CutterManager.cutterLength;
        Vector2 worldPos = framePos;

        List<Vector2> polygon = new()
        {
            worldPos + new Vector2(-L,  L),
            worldPos + new Vector2( L,  L),
            worldPos + new Vector2( L, -L),
            worldPos + new Vector2(-L, -L)
        };

        framePoints = polygon.ToArray();
        frameArea = Clipper2CutterHelper.GetPolygonArea(polygon);
        frameSideLength = L * 2;

        Debug.Log($"目标框面积计算完成: {frameArea:F2}, 边长: {frameSideLength}");
    }

    /// <summary>
    /// 胜利逻辑（统一入口）
    /// </summary>
    private void DoVictory()
    {
        // 防止重复触发
        if (isGlobalLocked) return;

        GamePlay.isGlobalLocked = true;
        GameEvents.InvokeBasicEvent(GameBasicEvent.CompleteLevel);
        CompleteLevel();
        Debug.Log("=== 恭喜！拼图完成！===");
    }

    // 保留原有方法，标记为过时
    [System.Obsolete("请使用优化后的CheckFinish6方法")]
    public void CheckFinish()
    {
        double fillArea = Clipper2CutterHelper.GetIntersectionAreaEx(allPiecePolys, framePoints);
        double ratio = fillArea / frameArea;

        if (ratio > 0.95f)
        {
            DoVictory();
        }
        Debug.Log($"[旧方法] 填充区域面积: {fillArea}, 目标面积: {frameArea}, 比例: {ratio:F4}");
    }

    // 在编辑器里画出托盘区域，方便调试
    void OnDrawGizmos()
    {
        // 画出打乱碎片的圆形区域
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);

        // 画出目标框区域
        if (targetFrameObj != null)
        {
            Gizmos.color = Color.cyan;
            float L = CutterManager.cutterLength;
            Vector3 framePos = targetFrameObj.transform.position;
            Gizmos.DrawWireCube(framePos, new Vector3(L * 2, L * 2, 0.1f));
        }
    }

    #region 工具方法

    /// <summary>
    /// 获取碎片的世界坐标路径（封装，解决原有GetWorldPath缺失问题）
    /// </summary>
    private Path64 GetPieceWorldPath(PuzzlePiece piece, double scale)
    {
        Path64 path = new Path64();
        foreach (var localPt in piece.points)
        {
            // 关键：必须 TransformPoint 转为世界坐标
            Vector3 worldPt = piece.transform.TransformPoint(localPt);
            path.Add(new Point64(worldPt.x * scale, worldPt.y * scale));
        }
        return path;
    }

    #endregion

    #region Event_Register_Handler

    private void CompleteLevel()
    {
        UserDataManager.CompleteLevel(currentLevel);
    }

    private void OnLook()
    {
        if (UserDataManager.ConsumeHintCount())
        {
            UIManager.instance.OpenHintWindow(currLevelData);
            GameEvents.InvokeEvent(GameBasicEvent.UpdateLookCount, UserDataManager.GetHintCount());
        }
    }

    private void Play()
    {
        ClearAllPieces();
        currentLevel = selectLevel;
        GamePlay.isGlobalLocked = false;
        LoadAndStartGame();
        UIManager.instance.ClearHintWindow();
        GameEvents.InvokeBasicEvent(GameBasicEvent.ResetUI);
    }

    private void PrevLevel()
    {
        GamePlay.IsStartOperation = false;
        selectLevel--;
        selectLevel = Mathf.Clamp(selectLevel, 1, sumLevel);

        LevelUnlockStatus levelUnlockStatus = LevelUnlockStatus.Current;
        if (selectLevel > UserDataManager.GetCurrentLevel())
        {
            levelUnlockStatus = LevelUnlockStatus.Locked;
        }
        else if (selectLevel < UserDataManager.GetCurrentLevel())
        {
            levelUnlockStatus = LevelUnlockStatus.Unlocked;
        }
        Debug.Log($"Prev Level : select {selectLevel}, status : {levelUnlockStatus.ToString()}");
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLevel, selectLevel, sumLevel, levelUnlockStatus);

        if (selectLevel <= UserDataManager.GetCurrentLevel())
        {
            currentLevel = selectLevel;
            Play();
        }
    }

    private void NextLevel()
    {
        GamePlay.IsStartOperation = false;
        selectLevel++;
        selectLevel = Mathf.Clamp(selectLevel, 1, sumLevel);
        LevelUnlockStatus levelUnlockStatus = LevelUnlockStatus.Current;
        if (selectLevel > UserDataManager.GetCurrentLevel())
        {
            levelUnlockStatus = LevelUnlockStatus.Locked;
        }
        else if (selectLevel < UserDataManager.GetCurrentLevel())
        {
            levelUnlockStatus = LevelUnlockStatus.Unlocked;
        }
        Debug.Log($"Next Level : select {selectLevel}, status : {levelUnlockStatus.ToString()}");
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLevel, selectLevel, sumLevel, levelUnlockStatus);
        if (selectLevel <= UserDataManager.GetCurrentLevel())
        {
            currentLevel = selectLevel;
            Play();
        }
    }

    /// <summary>
    /// 切换音频开关.
    /// </summary>
    private void TurnAudio()
    {
        IsMute = !IsMute;
        GameEvents.InvokeEvent<bool>(GameBasicEvent.UpdateAudio, IsMute);
    }

    /// <summary>
    /// 开始游戏操作.
    /// </summary>
    private void StartGameOprate()
    {
        selectLevel = currentLevel;
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLevel, selectLevel, sumLevel, LevelUnlockStatus.Current);
    }

    public void OnEnable()
    {
        GameEvents.RegisterBasicEvent(GameBasicEvent.Look, OnLook);
        // 注册优化后的检测方法
        GameEvents.RegisterBasicEvent(GameBasicEvent.CheckFinish, CheckFinish6);
        GameEvents.RegisterBasicEvent(GameBasicEvent.PrevLevel, PrevLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.NextLevel, NextLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.TurnAudio, TurnAudio);
        GameEvents.RegisterBasicEvent(GameBasicEvent.StartGameOprate, StartGameOprate);
        GameEvents.RegisterBasicEvent(GameBasicEvent.Play, Play);
    }

    public void OnDisable()
    {
        GameEvents.UnregisterBasicEvent(GameBasicEvent.Look, OnLook);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CheckFinish, CheckFinish6);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PrevLevel, PrevLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.NextLevel, NextLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.TurnAudio, TurnAudio);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.StartGameOprate, StartGameOprate);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.Play, Play);
    }

    #endregion
}
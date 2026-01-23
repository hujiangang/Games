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
            gp.Init(targetFrameRect, pp.transform.position + framePos, framePos);

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

    /// <summary>
    /// 采样点检测完成（备用方法）
    /// </summary>
    public void CheckFinish2()
    {
        // 1. 采样检测点 (比如每 0.5 一个点)
        int pointsFilled = 0;
        int totalSamples = 0;
        float sampleStep = 0.3f;

        for (float x = targetFrameRect.min.x + 0.1f; x < targetFrameRect.max.x; x += sampleStep)
        {
            for (float y = targetFrameRect.min.y + 0.1f; y < targetFrameRect.max.y; y += sampleStep)
            {
                totalSamples++;
                // 发射极短的射线检测这里是否有碎片
                if (Physics2D.OverlapPoint(new Vector2(x, y)))
                {
                    pointsFilled++;
                }
            }
        }

        // 3. 如果 98% 的点都被覆盖了，说明拼图完成
        float fillPercent = (float)pointsFilled / totalSamples;
        if (fillPercent > 0.98f)
        {
            GamePlay.isGlobalLocked = true;
            Debug.Log("恭喜！拼图完成（采样点检测）！");
            DoVictory();
        }
        Debug.Log($"采样点覆盖比例: {fillPercent:P2}, 已覆盖: {pointsFilled}/{totalSamples}");
    }

    public void CheckFinish6()
    {
        if (isGlobalLocked) return;

        const double Scale = 1000.0;
        DraggableComponent[] pieces = FindObjectsOfType<DraggableComponent>();

        if (pieces == null || pieces.Length == 0)
        {
            Debug.Log("[拼图检测] 无有效碎片，检测终止");
            return;
        }

        // ========== 新增：第一步先统计吸附的碎片数量 ==========
        int snappedPieceCount = 0; // 已成功吸附的碎片数
        foreach (var p in pieces)
        {
            if (p == null) continue;
            // 关键：判断碎片是否处于吸附状态（需确保DraggableComponent有IsSnapped属性）
            // 如果你的吸附标记字段不是IsSnapped，替换为你实际的字段名（比如isAttached/snapped）
            if (p.isSnapped)
            {
                snappedPieceCount++;
            }
        }

        // 基础校验：吸附数量不足直接返回（比如要求至少90%的碎片完成吸附）
        float snappedThreshold = 0.9f; // 可调整：比如0.9=90%碎片吸附才判定
        if (snappedPieceCount < pieces.Length * snappedThreshold)
        {
            Debug.Log($"[拼图检测] ✗ 未完成，原因：吸附碎片不足({snappedPieceCount}/{pieces.Length})");
            return;
        }

        // 1. 收集所有碎片的世界坐标路径
        Paths64 allPaths = new Paths64();
        int validPieceCount = 0;
        int piecesInFrame = 0;

        foreach (var p in pieces)
        {
            if (p == null || p.GetComponent<PuzzlePiece>() == null) continue;

            Path64 path = GetPieceWorldPath(p, Scale);
            if (path.Count >= 3)
            {
                allPaths.Add(path);
                validPieceCount++;

                Vector2 pieceCenter = p.transform.position;
                if (targetFrameRect.Contains(pieceCenter))
                {
                    piecesInFrame++;
                }
            }
        }

        // 基础校验：碎片必须大部分在框内
        if (piecesInFrame < pieces.Length * 0.95)
        {
            Debug.Log($"[拼图检测] 碎片不在框内: {piecesInFrame}/{pieces.Length}，未完成");
            return;
        }

        if (validPieceCount < pieces.Length * 0.8)
        {
            Debug.Log($"[拼图检测] 有效碎片不足: {validPieceCount}/{pieces.Length}，未完成");
            return;
        }

        // 2. 合并所有碎片（并集运算）
        Paths64 unionPieces = Clipper.Union(allPaths, FillRule.NonZero);

        // 轻微膨胀弥合缝隙
        double inflateValue = 0.0015 * Scale;
        Paths64 healedPieces = Clipper.InflatePaths(unionPieces, inflateValue,
            JoinType.Miter, EndType.Polygon, 2.0);

        // 3. 创建目标框路径
        Paths64 framePaths = CreateTargetFramePath(Scale);
        if (framePaths == null || framePaths.Count == 0)
        {
            Debug.LogError("[拼图检测] 目标框路径创建失败");
            return;
        }

        // 4. 计算碎片与目标框的交集
        Paths64 intersection = Clipper.Intersect(healedPieces, framePaths, FillRule.NonZero);

        // 5. 计算核心面积指标
        double intersectionArea = 0;
        double maxIslandArea = 0;
        int significantIslands = 0;

        foreach (var path in intersection)
        {
            double area = System.Math.Abs(Clipper.Area(path));
            intersectionArea += area;

            if (area > maxIslandArea) maxIslandArea = area;

            double areaRatio = (area / (Scale * Scale)) / frameArea;
            if (areaRatio > 0.01f)
            {
                significantIslands++;
            }
        }

        intersectionArea /= (Scale * Scale);
        maxIslandArea /= (Scale * Scale);

        double coverageRatio = intersectionArea / frameArea;
        double mainIslandRatio = maxIslandArea / frameArea;

        Debug.Log($"[拼图检测] 总覆盖率: {coverageRatio:P2}, 最大区域覆盖率: {mainIslandRatio:P2}, " +
                  $"显著区域数: {significantIslands}, 总区域数: {intersection.Count}, " +
                  $"框内碎片数: {piecesInFrame}/{pieces.Length}, 吸附碎片数: {snappedPieceCount}/{pieces.Length}");

        // 🌟 核心修改：适配多圈层拼图的判定逻辑（保留原有逻辑）
        bool isMultiLayerPuzzle = significantIslands >= pieces.Length * 0.7;
        bool condition1 = coverageRatio >= 0.96f && piecesInFrame >= pieces.Length;
        bool condition2 = coverageRatio >= 0.98f;
        bool condition3 = !isMultiLayerPuzzle && mainIslandRatio >= 0.95f;

        // 新增：最终判定时也校验吸附数量（确保100%吸附）
        bool isAllSnapped = snappedPieceCount == pieces.Length;
        if ((condition1 || condition2 || condition3) && isAllSnapped)
        {
            Debug.Log($"[拼图检测] ✓ 完成! 条件1={condition1}, 条件2={condition2}, 条件3={condition3}, 吸附数达标={isAllSnapped}");
            DoVictory();
        }
        else
        {
            string reason = "";
            if (!isAllSnapped)
                reason = $"吸附碎片不足({snappedPieceCount}/{pieces.Length})"; // 优先显示吸附不足
            else if (coverageRatio < 0.95)
                reason = $"覆盖率不足({coverageRatio:P2})";
            else if (piecesInFrame < pieces.Length)
                reason = $"碎片不在框内({piecesInFrame}/{pieces.Length})";
            else if (!isMultiLayerPuzzle && mainIslandRatio < 0.95)
                reason = $"最大连续区域不足({mainIslandRatio:P2})";
            else
                reason = "拼图结构特殊，但未满足其他条件";

            Debug.Log($"[拼图检测] ✗ 未完成，原因：{reason}");
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

    [System.Obsolete("请使用优化后的CheckFinish6方法")]
    public void CheckFinish3()
    {
        Debug.LogWarning("CheckFinish3方法已过时，存在依赖isSnapped的bug，请切换到CheckFinish6");
        // 原有逻辑保留，仅做警告
        const double Scale = 1000.0;
        Paths64 allSnappedPaths = new();

        DraggableComponent[] pieces = FindObjectsOfType<DraggableComponent>();
        foreach (var p in pieces)
        {
            if (p.isSnapped)
            {
                Path64 path = GetPieceWorldPath(p, Scale);
                allSnappedPaths.Add(path);
            }
        }

        if (allSnappedPaths.Count < pieces.Length) return;

        Paths64 unionResult = Clipper.Union(allSnappedPaths, FillRule.NonZero);
        double currentArea = 0;
        foreach (var path in unionResult)
        {
            currentArea += System.Math.Abs(Clipper.Area(path));
        }
        currentArea /= (Scale * Scale);

        double ratio = currentArea / frameArea;
        Debug.Log($"[旧方法] 合并后面积: {currentArea}, 比例: {ratio:F4}, 路径数: {unionResult.Count}");

        if (ratio >= 0.95f && ratio <= 1.01f && unionResult.Count == 1)
        {
            DoVictory();
        }
    }

    [System.Obsolete("请使用优化后的CheckFinish6方法")]
    public void CheckFinish4()
    {
        Debug.LogWarning("CheckFinish4方法已过时，存在膨胀参数不合理的bug，请切换到CheckFinish6");
        // 原有逻辑保留，仅做警告
        const double Scale = 1000.0;
        DraggableComponent[] pieces = FindObjectsOfType<DraggableComponent>();

        int snappedCount = 0;
        Paths64 allPaths = new();
        foreach (var p in pieces)
        {
            if (p.isSnapped)
            {
                snappedCount++;
                allPaths.Add(GetPieceWorldPath(p, Scale));
            }
        }

        if (snappedCount < pieces.Length)
        {
            Debug.Log($"[旧方法] 已吸附: {snappedCount}/{pieces.Length}, 未完成");
            return;
        }

        Paths64 combined = Clipper.Union(allPaths, FillRule.NonZero);
        Paths64 healed = Clipper.InflatePaths(combined, 0.005 * Scale, JoinType.Miter, EndType.Polygon);

        double totalFillArea = 0;
        double maxIslandArea = 0;
        foreach (var path in healed)
        {
            double a = System.Math.Abs(Clipper.Area(path));
            totalFillArea += a;
            if (a > maxIslandArea) maxIslandArea = a;
        }

        totalFillArea /= (Scale * Scale);
        maxIslandArea /= (Scale * Scale);

        double totalRatio = totalFillArea / frameArea;
        double mainIslandRatio = maxIslandArea / frameArea;

        Debug.Log($"[旧方法] 总比例: {totalRatio:F4}, 最大岛比例: {mainIslandRatio:F4}, 路径数: {healed.Count}");

        bool condition1 = mainIslandRatio >= 0.95f;
        bool condition2 = healed.Count == 1 && totalRatio >= 0.96f;

        if (condition1 || condition2)
        {
            Debug.Log($"[旧方法] ✓ 完成! 条件1={condition1}, 条件2={condition2}");
            DoVictory();
        }
        else
        {
            Debug.Log($"[旧方法] ✗ 未完成. 条件1={condition1}, 条件2={condition2}");
        }
    }

    [System.Obsolete("请使用优化后的CheckFinish6方法")]
    public void CheckFinish5()
    {
        Debug.LogWarning("CheckFinish5方法已过时，存在膨胀参数和判定阈值不合理的bug，请切换到CheckFinish6");
        // 原有逻辑保留，仅做警告
        const double Scale = 1000.0;
        DraggableComponent[] pieces = FindObjectsOfType<DraggableComponent>();

        Paths64 allPaths = new();
        foreach (var p in pieces)
        {
            allPaths.Add(GetPieceWorldPath(p, Scale));
        }

        Paths64 unionPieces = Clipper.Union(allPaths, FillRule.NonZero);
        Debug.Log($"[旧方法] 合并后: {unionPieces.Count} 个区域");

        Paths64 healed = Clipper.InflatePaths(unionPieces, 0.008 * Scale, JoinType.Miter, EndType.Polygon);
        Debug.Log($"[旧方法] 膨胀后: {healed.Count} 个区域");

        Path64 framePath = new();
        Vector3 framePos = GameObject.Find("TargetFrame").transform.position;
        float L = CutterManager.cutterLength;
        framePath.Add(new Point64((framePos.x - L) * Scale, (framePos.y + L) * Scale));
        framePath.Add(new Point64((framePos.x + L) * Scale, (framePos.y + L) * Scale));
        framePath.Add(new Point64((framePos.x + L) * Scale, (framePos.y - L) * Scale));
        framePath.Add(new Point64((framePos.x - L) * Scale, (framePos.y - L) * Scale));
        Paths64 framePaths = new() { framePath };

        Paths64 intersection = Clipper.Intersect(healed, framePaths, FillRule.NonZero);

        double intersectionArea = 0;
        double maxIslandArea = 0;

        foreach (var path in intersection)
        {
            double area = System.Math.Abs(Clipper.Area(path));
            intersectionArea += area;
            if (area > maxIslandArea) maxIslandArea = area;
        }

        intersectionArea /= (Scale * Scale);
        maxIslandArea /= (Scale * Scale);

        double coverageRatio = intersectionArea / frameArea;
        double mainIslandRatio = maxIslandArea / frameArea;

        Debug.Log($"[旧方法] 交集面积: {intersectionArea:F4}, 覆盖率: {coverageRatio:F4}, 最大岛: {mainIslandRatio:F4}, 路径数: {intersection.Count}");

        bool condition1 = intersection.Count == 1 && coverageRatio >= 0.94f;
        bool condition2 = mainIslandRatio >= 0.93f;

        Debug.Log($"[旧方法] 判定结果: 条件1={condition1}, 条件2={condition2}");

        if (condition1 || condition2)
        {
            Debug.Log($"[旧方法] ✓ 完成! 条件1={condition1}, 条件2={condition2}");
            DoVictory();
        }
        else if (coverageRatio >= 0.93f)
        {
            Debug.Log($"[旧方法] ✗ 覆盖率足够但有 {intersection.Count} 个分离区域，最大岛{mainIslandRatio:P}");
        }
        else
        {
            Debug.Log($"[旧方法] ✗ 覆盖率不足 ({coverageRatio:P})");
        }
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
    private Path64 GetPieceWorldPath(DraggableComponent piece, double scale)
    {
        Path64 path = new Path64();
        PuzzlePiece puzzlePiece = piece.GetComponent<PuzzlePiece>();

        foreach (var v in puzzlePiece.points)
        {
            Vector2 wPos = piece.transform.TransformPoint(v);
            path.Add(new Point64(wPos.x * scale, wPos.y * scale));
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
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

    /// <summary>
    /// 选择关卡.
    /// </summary>
    private int selectLevel = 0;

    /// <summary>
    /// 当前关卡数据.
    /// </summary>
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
            Destroy(piece.gameObject);
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

        Vector3 framePos = GameObject.Find("TargetFrame").transform.position;
        float len = CutterManager.cutterLength;
        targetFrameRect = new(framePos.x - len, framePos.y - len, len * 2, len * 2);
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


    /// <summary>
    /// 检查是否拼图完成
    /// 在拼图区域内均匀采样点，检测是否被碎片覆盖，若覆盖率达到 98% 以上即判定为完成.
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
            Debug.Log("恭喜！拼图完成！");
        }

    }


    private void DoVictory()
    {
        GamePlay.isGlobalLocked = true;
        GameEvents.InvokeBasicEvent(GameBasicEvent.CompleteLevel);
        CompleteLevel();
        Debug.Log("恭喜！拼图完成！");
    }

    /// <summary>
    /// 检查是否拼图完成(面积覆盖法).
    /// </summary>
    public void CheckFinish()
    {
        double fillArea = Clipper2CutterHelper.GetIntersectionAreaEx(allPiecePolys, framePoints);

        double ratio = fillArea / frameArea;

        // 2. 检查是否填充超过 95%.
        if (ratio > 0.95f)
        {
            DoVictory();
        }
        Debug.Log($"填充区域面积: {fillArea}, 目标区域面积: {frameArea}, 填充比例: {ratio}");
    }

    public void CheckFinish3()
    {
        const double Scale = 1000.0;
        Paths64 allSnappedPaths = new();

        // 1. 收集所有【已吸附】碎片的顶点
        DraggableComponent[] pieces = FindObjectsOfType<DraggableComponent>();
        foreach (var p in pieces)
        {
            if (p.isSnapped)
            {
                Path64 path = new();
                // 必须使用变换后的世界坐标
                foreach (var v in p.GetComponent<PuzzlePiece>().points)
                {
                    Vector2 wPos = p.transform.TransformPoint(v);
                    path.Add(new Point64(wPos.x * Scale, wPos.y * Scale));
                }
                allSnappedPaths.Add(path);
            }
        }

        Debug.Log($"已吸附碎片数量: {allSnappedPaths.Count}, 总碎片数量: {pieces.Length}");

        if (allSnappedPaths.Count < pieces.Length) return; // 数量都不够，肯定没完

        // 2. 【核心】执行 Union（并集）运算
        // 这会将重叠的部分合并，产生一个或多个不重叠的大多边形
        Paths64 unionResult = Clipper.Union(allSnappedPaths, FillRule.NonZero);

        // 3. 计算并集后的总面积
        double currentArea = 0;
        foreach (var path in unionResult)
        {
            currentArea += System.Math.Abs(Clipper.Area(path));
        }
        currentArea /= (Scale * Scale); // 还原缩放

        // 4. 与目标框面积对比 (frameArea 是你初始正方形的面积，比如 36)
        double ratio = currentArea / frameArea;

        // 调试：观察合并后的面积
        Debug.Log($"合并后总面积: {currentArea}, 目标面积: {frameArea}, 比例: {ratio:F4}, 并集路径数量: {unionResult.Count}");

        // 5. 判定胜利：
        // 因为是 Union 后的面积，绝对不会超过原始总面积（除非碎片跑到了正方形外面）
        // 所以这里的 ratio 如果在 0.99 到 1.0 之间，就是完美填充
        if (ratio >= 0.95f && ratio <= 1.01f) 
        {
            // 还要加个保险：并集后的结果必须只有一个路径（说明中间没缝，也没散块）
            if (unionResult.Count == 1)
            {
                DoVictory();
            }
        }
    }

    public void CheckFinish4()
    {
        const double Scale = 1000.0;
        DraggableComponent[] pieces = FindObjectsOfType<DraggableComponent>();
        
        // 1. 基础检查：必须所有碎片都已吸附
        int snappedCount = 0;
        Paths64 allPaths = new Paths64();
        foreach (var p in pieces) {
            if (p.isSnapped) {
                snappedCount++;
                // 获取碎片当前世界坐标的路径
                allPaths.Add(p.GetWorldPath(Scale)); 
            }
        }

        if (snappedCount < pieces.Length) return; 

        // 2. 执行并集（Union）并加大膨胀力度进行“缝合”
        // 这里的 0.05 * Scale 是关键。如果你的黑色缝隙很明显，这个值要稍微大一点
        // 它会把碎片边缘向外扩，强制让相邻碎片重叠，从而合并成一个 Count
        Paths64 combined = Clipper.Union(allPaths, FillRule.NonZero);
        Paths64 healed = Clipper.InflatePaths(combined, 0.015 * Scale, JoinType.Miter, EndType.Polygon);
        
        // 3. 计算合并后的总面积
        double totalFillArea = 0;
        double maxIslandArea = 0; // 记录最大的那块碎片的面积

        foreach (var path in healed) {
            double a = System.Math.Abs(Clipper.Area(path));
            totalFillArea += a;
            if (a > maxIslandArea) maxIslandArea = a;
        }
        
        totalFillArea /= (Scale * Scale);
        maxIslandArea /= (Scale * Scale);

        double totalRatio = totalFillArea / frameArea;
        double mainIslandRatio = maxIslandArea / frameArea;

        Debug.Log($"[判定数据] 总比例: {totalRatio:F4}, 最大岛屿比例: {mainIslandRatio:F4}, 路径数量: {healed.Count}");

        // 4. 【核心判定逻辑修改】
        // 满足以下任意一个条件即可判定胜利：
        // 条件 A：最大的一块连续区域已经覆盖了目标框的 95% 以上（无视掉碎屑）
        // 条件 B：总面积覆盖率超过 98% 且位置都已吸附
        if (mainIslandRatio > 0.95f || (totalRatio > 0.98f && healed.Count <= pieces.Length)) 
        {
            DoVictory();
        }
    }


    /// <summary>
    /// 计算目标区域的面积.
    /// </summary>
    private void ComputeFrameArea()
    {
        Vector3 framePos = GameObject.Find("TargetFrame").transform.position;
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
    }


    // 在编辑器里画出托盘区域，方便调试
    void OnDrawGizmos()
    {
        // 画出打乱碎片的圆形区域
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }

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
        Debug.Log($"Next Level : select {selectLevel}, status : {levelUnlockStatus.ToString()}");
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
        GameEvents.RegisterBasicEvent(GameBasicEvent.CheckFinish, CheckFinish4);
        GameEvents.RegisterBasicEvent(GameBasicEvent.PrevLevel, PrevLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.NextLevel, NextLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.TurnAudio, TurnAudio);
        GameEvents.RegisterBasicEvent(GameBasicEvent.StartGameOprate, StartGameOprate);
        GameEvents.RegisterBasicEvent(GameBasicEvent.Play, Play);

    }

    public void OnDisable()
    {
        GameEvents.UnregisterBasicEvent(GameBasicEvent.Look, OnLook);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CheckFinish, CheckFinish4);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PrevLevel, PrevLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.NextLevel, NextLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.TurnAudio, TurnAudio);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.StartGameOprate, StartGameOprate);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.Play, Play);
    }

    #endregion
}
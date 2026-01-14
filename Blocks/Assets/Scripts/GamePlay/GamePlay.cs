using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;


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
    }

    /// <summary>
    /// 获取关卡数据.
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    private LevelData GetLevelData(int level)
    {
        level = Mathf.Clamp(level, 1, sumLevel);
        if (levelDataDict.TryGetValue(level, out LevelData data))
        {
            return data;
        }
        Debug.Log($"GetLevelData: {level}");
        string levelToLoad = $"Level_{level}";
        string path = Application.dataPath + "/LevelsData/" + levelToLoad + ".json";
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<LevelData>(json);
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

        foreach (var pd in data.pieces)
        {
            // 1. 创建碎片物体
            piecesCount++;
            GameObject go = new($"GamePiece_{piecesCount}")
            {
                tag = "PuzzlePiece"
            };
            // 使用你之前的脚本生成 Mesh.
            PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
            pp.Init(pd.vertices, pieceMaterial, pd.color, piecesCount);

            allPiecePolys.Add(pp.GetComponent<PolygonCollider2D>());
            pp.correctWorldPos = pp.transform.position + framePos;

            // 2. 添加游戏逻辑
            DraggableComponent gp = go.AddComponent<DraggableComponent>();
            gp.Init(targetFrameRect, pp.transform.position + framePos, framePos, sumPieceCount + 1);

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
    public void CheckWinCondition2()
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

    /// <summary>
    /// 检查是否拼图完成(面积覆盖法).
    /// </summary>
    public void CheckFinish()
    {
        double fillArea = Clipper2CutterHelper.GetIntersectionArea(allPiecePolys, framePoints);

        double ratio = fillArea / frameArea;

        // 2. 检查是否填充超过 95%.
        if (ratio > 0.95f)
        {
            GamePlay.isGlobalLocked = true;
            GameEvents.InvokeBasicEvent(GameBasicEvent.CompleteLevel);
            CompleteLevel();
            Debug.Log("恭喜！拼图完成！");
        }

        Debug.Log($"填充区域面积: {fillArea}, 目标区域面积: {frameArea}, 填充比例: {ratio}");
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
        currentLevel++;
        UserDataManager.CompleteLevel(currentLevel);
    }

    private void OnLook()
    {
        UIManager.instance.OpenHintWindow(currLevelData);
    }

    private void Play()
    {
        ClearAllPieces();
        if (selectLevel <= currentLevel)
        {
            GamePlay.isGlobalLocked = false;
            LoadAndStartGame();
            UIManager.instance.ClearHintWindow();

            GameEvents.InvokeBasicEvent(GameBasicEvent.Play);
        }
    }

    private void PrevLevel()
    {
        GamePlay.IsStartOperation = false;
        selectLevel--;
        selectLevel = Mathf.Clamp(selectLevel, 1, sumLevel);

        LevelUnlockStatus levelUnlockStatus = LevelUnlockStatus.Current;
        if (selectLevel > currentLevel)
        {
            levelUnlockStatus = LevelUnlockStatus.Locked;
        }
        else if (selectLevel < currentLevel)
        {
            levelUnlockStatus = LevelUnlockStatus.Unlocked;
        }
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLevel, selectLevel, sumLevel, levelUnlockStatus);

        if (selectLevel <= currentLevel)
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
        if (selectLevel > currentLevel)
        {
            levelUnlockStatus = LevelUnlockStatus.Locked;
        }
        else if (selectLevel < currentLevel)
        {
            levelUnlockStatus = LevelUnlockStatus.Unlocked;
        }
        GameEvents.InvokeEvent(GameBasicEvent.UpdateLevel, selectLevel, sumLevel, levelUnlockStatus);
        if (selectLevel <= currentLevel)
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
        GameEvents.RegisterBasicEvent(GameBasicEvent.CheckFinish, CheckFinish);
        GameEvents.RegisterBasicEvent(GameBasicEvent.PrevLevel, PrevLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.NextLevel, NextLevel);
        GameEvents.RegisterBasicEvent(GameBasicEvent.TurnAudio, TurnAudio);
        GameEvents.RegisterBasicEvent(GameBasicEvent.StartGameOprate, StartGameOprate);

    }

    public void OnDisable()
    {
        GameEvents.UnregisterBasicEvent(GameBasicEvent.Look, OnLook);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.CheckFinish, CheckFinish);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.PrevLevel, PrevLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.NextLevel, NextLevel);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.TurnAudio, TurnAudio);
        GameEvents.UnregisterBasicEvent(GameBasicEvent.StartGameOprate, StartGameOprate);
    }

    #endregion
}
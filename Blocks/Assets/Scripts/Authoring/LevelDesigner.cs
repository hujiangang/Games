// LevelDesigner.cs - 关卡设计器
using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelDesigner : MonoBehaviour
{
    public LineDrawer lineDrawer;
    private List<LevelDesignData> allLevels = new List<LevelDesignData>();

    public float AddLength = 0.5f;

    // 关卡数据结构
    [System.Serializable]
    public class LevelDesignData
    {
        public int levelNumber;
        public int gridSize; // 实际不需要，但保留用于难度标记
        public List<CutPath> cutPaths; // 切割路径
        public string description;
        public float difficulty; // 1-5级难度

        [System.Serializable]
        public class CutPath
        {
            public List<Vector2> points; // 切割线的连续点
        }
    }

    void Start()
    {
        GenerateAllLevels();
    }

    // 工具方法：创建切割路径
    private LevelDesignData.CutPath CreateCutPath(params Vector2[] points)
    {
        LevelDesignData.CutPath path = new()
        {
            points = new List<Vector2>(points)
        };

        return path;
    }

     private void AddInfiniteCut(LevelDesignData level, Vector2 dir, float offset, float L)
    {
        Vector2 normal = new Vector2(-dir.y, dir.x).normalized;
        Vector2 center = normal * offset;

        float OUT = 1.3f * L;

        level.cutPaths.Add(CreateCutPath(
            center - dir.normalized * OUT,
            center + dir.normalized * OUT
        ));
    }


    public int currentLevel = 1;

    public void SwitchLevel()
    {
        LoadLevel(currentLevel);
        GetComponent<LevelEditor>().SetCurrentLevelName(currentLevel);
        currentLevel++;
    }

    public void PrevLevel()
    {
        currentLevel--;
        if (currentLevel < 1)
        {
            currentLevel = 1;
        }
        GetComponent<LevelEditor>().SetCurrentLevelName(currentLevel);
        LoadLevel(currentLevel);
    }

    public void NextLevel()
    {
        currentLevel++;
        if (currentLevel > allLevels.Count)
        {
            currentLevel = allLevels.Count;
        }
        GetComponent<LevelEditor>().SetCurrentLevelName(currentLevel);
        LoadLevel(currentLevel);
    }

    // 加载关卡到线绘制器
    public void LoadLevel(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > allLevels.Count)
        {
            Debug.LogError($"关卡 {levelNumber} 不存在");
            return;
        }

        LevelDesignData level = allLevels[levelNumber - 1];
        lineDrawer.ClearLinePrefab();
        lineDrawer.ClearPath();

        Debug.Log($"加载关卡 {level.levelNumber}: {level.description} (难度: {level.difficulty:F1})");

        // 将关卡数据转换为线绘制器可用的格式
        // 这里需要根据你的LineDrawer实际接口调整
        // 假设你可以通过某种方式设置切割路径

        foreach (var cutPath in level.cutPaths)
        {

            Debug.Log($"添加路径: {string.Join(", ", cutPath.points)}");
            if (cutPath.points != null && cutPath.points.Count >= 2)
            {
                // 将路径点转换为 LineDrawer 需要的格式
                List<Vector2> pathPoints = new List<Vector2>();

                // 优化路径点：移除过于接近的点
                for (int i = 0; i < cutPath.points.Count; i++)
                {
                    Vector2 point = cutPath.points[i];

                    // 确保点在有效范围内（可选）
                    float L = CutterManager.cutterLength + AddLength;
                    point.x = Mathf.Clamp(point.x, -L * 1.1f, L * 1.1f);
                    point.y = Mathf.Clamp(point.y, -L * 1.1f, L * 1.1f);

                    // 如果这是第一个点，或者与上一个点的距离足够大，才添加
                    if (i == 0 || Vector2.Distance(point, pathPoints[pathPoints.Count - 1]) > 0.01f)
                    {
                        pathPoints.Add(point);
                    }
                }

                // 如果优化后仍有足够的点，添加到 allPaths
                if (pathPoints.Count >= 2)
                {
                    lineDrawer.allPaths.Add(pathPoints);

                    // 创建可见的线条
                    CreateVisualLine(pathPoints);
                }
            }
        }
    }

    // 辅助方法：创建可见的线条
    private void CreateVisualLine(List<Vector2> pathPoints)
    {
        if (lineDrawer.linePrefab == null)
        {
            Debug.LogWarning("LineDrawer 的 linePrefab 未设置");
            return;
        }

        // 创建静态线条对象
        GameObject staticLine = Instantiate(lineDrawer.linePrefab, Vector3.zero, Quaternion.identity);
        staticLine.tag = "EditorLine"; // 确保使用正确的标签

        LineRenderer slr = staticLine.GetComponent<LineRenderer>();
        if (slr == null)
        {
            Debug.LogWarning("linePrefab 没有 LineRenderer 组件");
            Destroy(staticLine);
            return;
        }

        // 设置线条属性
        lineDrawer.linesortingOrder++;
        slr.sortingOrder = lineDrawer.linesortingOrder;

        // 设置点位置
        slr.positionCount = pathPoints.Count;
        for (int i = 0; i < pathPoints.Count; i++)
        {
            slr.SetPosition(i, new Vector3(pathPoints[i].x, pathPoints[i].y, 0));
        }

        // 设置线条样式
        slr.startWidth = 0.05f;
        slr.endWidth = 0.05f;
        slr.material = lineDrawer.pieceMaterial;

        // 设置线条颜色（可以根据难度调整）
        //Color lineColor = GetLineColorByDifficulty(currentLevel);
        //slr.startColor = lineColor;
        //slr.endColor = lineColor;

        // 对于闭合路径，设置循环
        if (pathPoints.Count >= 3 &&
            Vector2.Distance(pathPoints[0], pathPoints[pathPoints.Count - 1]) < 0.1f)
        {
            //slr.loop = true;
        }
    }

    #region  保存 与 加载
    /// <summary>
    /// 保存关卡数据到JSON.
    /// </summary>
    public void SaveLevelsToJson()
    {
        LevelDataWrapper wrapper = new LevelDataWrapper();
        wrapper.levels = allLevels;
        string json = JsonUtility.ToJson(wrapper, true);
        System.IO.File.WriteAllText(Application.dataPath + "/Resources/Levels.json", json);
        Debug.Log($"已保存 {allLevels.Count} 个关卡到 Levels.json");
    }

    [System.Serializable]
    private class LevelDataWrapper
    {
        public List<LevelDesignData> levels;
    }

    // UI控制方法
    public void LoadNextLevel()
    {
        // 从PlayerPrefs获取当前关卡
        int current = PlayerPrefs.GetInt("CurrentLevel", 1);
        if (current < allLevels.Count)
        {
            LoadLevel(current + 1);
            PlayerPrefs.SetInt("CurrentLevel", current + 1);
        }
    }

    public void LoadPreviousLevel()
    {
        int current = PlayerPrefs.GetInt("CurrentLevel", 1);
        if (current > 1)
        {
            LoadLevel(current - 1);
            PlayerPrefs.SetInt("CurrentLevel", current - 1);
        }
    }

   

    #endregion

    #region  关卡生成
    private void GenerateAllLevels()
    {
        allLevels.Clear();
        float L = CutterManager.cutterLength;

        for (int i = 1; i <= 66; i++)
        {
            LevelDesignData level = new LevelDesignData();
            level.levelNumber = i;
            level.cutPaths = new List<LevelDesignData.CutPath>();
            level.difficulty = Mathf.Lerp(1f, 5f, (i - 1) / 19f);

            GenerateLevel(level, i, L);

            allLevels.Add(level);
        }

        Debug.Log("已生成 20 个主题解谜关卡");
    }

    private void GenerateLevel(LevelDesignData level, int i, float L)
    {
        switch (i)
        {
            case 1:
                GenerateLevel_1(level, i, L);
                break;
            case 2:
                GenerateLevel_2(level, i, L);
                break;
            case 3:
                GenerateLevel_3(level, i, L);
                break;    
            case 4:
                GenerateLevel_4(level, i, L);
                break;
            case 5:
                GenerateLevel_5(level, i, L);
                break;
            case 6:
                GenerateLevel_6(level, i, L);
                break;
            case 7:
                GenerateLevel_7(level, i, L);
                break;
            case 8:
                GenerateLevel_8(level, i, L);
                break;
            case 9:
                GenerateLevel_9(level, i, L);
                break;
            case 10:
                GenerateLevel_10(level, i, L);
                break;
            case 11:
                GenerateLevel_11(level, i, L);
                break;
            case 12:
                GenerateLevel_12(level, i, L);
                break;
            case 13:
                GenerateLevel_13(level, i, L);
                break;
            case 14:
                GenerateLevel_14(level, i, L);
                break;
            case 15:
                GenerateLevel_15(level, i, L);
                break;
            case 16:
                GenerateLevel_16(level, i, L);
                break;
            case 17:
                GenerateLevel_17(level, i, L);
                break;
            case 18:
                GenerateLevel_18(level, i, L);
                break;
            case 19:
                GenerateLevel_19(level, i, L);
                break;
            case 20:
                GenerateLevel_20(level, i, L);
                break;
            case 21:
                break;
            default:
                break;
        }
    }

    private void GenerateLevel_1(LevelDesignData level, int i, float L)
{
    float OUT = 1.3f * L;

    // 主切割：地形脊线（你现在这条）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.1f * L),
        new Vector2(-0.5f * L,  0.25f * L),
        new Vector2(-0.3f * L, -0.15f * L),
        new Vector2( 0.0f * L, -0.25f * L),
        new Vector2( 0.4f * L,  0.2f * L),
        new Vector2( 0.7f * L,  0.0f * L),
        new Vector2( OUT,  0.15f * L)
    ));

    // 次切割 1：斜向贯穿（制造左右分区）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.6f * L),
        new Vector2( OUT,  0.8f * L)
    ));

    // 次切割 2：反向贯穿（制造小碎片）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.7f * L),
        new Vector2( OUT, -0.5f * L)
    ));
}

private void GenerateLevel_2(LevelDesignData level, int i, float L)
{
    float OUT = 1.35f * L;

    // 主断层：S 型大陆撕裂（不变，已经很优秀）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.40f * L),
        new Vector2(-0.7f * L,  0.60f * L),
        new Vector2(-0.3f * L,  0.20f * L),
        new Vector2( 0.0f * L, -0.25f * L),
        new Vector2( 0.45f * L, -0.45f * L),
        new Vector2( 0.9f * L, -0.10f * L),
        new Vector2( OUT,  0.25f * L)
    ));

    // 次断层 1：斜向贯穿（拉开角度，避免锐角）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.60f * L),
        new Vector2(-0.3f * L, -0.20f * L),
        new Vector2( 0.2f * L,  0.30f * L),
        new Vector2( OUT,  0.80f * L)
    ));

    // 次断层 2：上部切割（减少折点，保证块面完整）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.85f * L),
        new Vector2( 0.6f * L,  0.35f * L),
        new Vector2( OUT,  0.50f * L)
    ));

    // 内部裂谷：放大 + 不完全闭合（关键修改）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.35f * L,  0.10f * L),
        new Vector2(-0.10f * L,  0.30f * L),
        new Vector2( 0.30f * L,  0.20f * L),
        new Vector2( 0.25f * L, -0.20f * L),
        new Vector2(-0.20f * L, -0.25f * L)
        // ❌ 不闭合，避免生成极小核心块
    ));
}


private void GenerateLevel_3(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;

    // 1️⃣ 主环裂纹（太阳石盘的时间裂缝）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.3f * L),
        new Vector2(-0.6f * L,  0.5f * L),
        new Vector2(-0.2f * L,  0.3f * L),
        new Vector2( 0.2f * L,  0.4f * L),
        new Vector2( 0.6f * L,  0.2f * L),
        new Vector2( OUT,  0.3f * L)
    ));

    // 2️⃣ 放射断层 A（左下 → 右上）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.6f * L),
        new Vector2(-0.4f * L, -0.2f * L),
        new Vector2( 0.2f * L,  0.2f * L),
        new Vector2( 0.8f * L,  0.6f * L),
        new Vector2( OUT,  0.8f * L)
    ));

    // 3️⃣ 放射断层 B（右下 → 左上，但角度更钝）
    level.cutPaths.Add(CreateCutPath(
        new Vector2( OUT, -0.6f * L),
        new Vector2( 0.6f * L, -0.4f * L),
        new Vector2( 0.0f * L, -0.2f * L),
        new Vector2(-0.6f * L,  0.4f * L),
        new Vector2(-OUT,  0.6f * L)
    ));

    // 4️⃣ 内部偏移裂谷（不闭合，形成“太阳核”）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.3f * L,  0.1f * L),
        new Vector2(-0.1f * L,  0.3f * L),
        new Vector2( 0.3f * L,  0.2f * L),
        new Vector2( 0.4f * L, -0.2f * L),
        new Vector2( 0.1f * L, -0.3f * L),
        new Vector2(-0.3f * L, -0.2f * L)
    ));
}


private void GenerateLevel_4(LevelDesignData level, int i, float L)
{
    float OUT = 1.2f * L; // 定义一个常数，用于确定切割线的延伸长度

    // 1️⃣ 主切割线：从左上到右下，穿过六边形中心
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  OUT * 0.5f),
        new Vector2(0,  OUT),
        new Vector2(OUT,  OUT * 0.5f)
    ));

    // 2️⃣ 次切割线：从右上到左下，与主切割线交叉
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT,  OUT * 0.5f),
        new Vector2(0, -OUT),
        new Vector2(-OUT, -OUT * 0.5f)
    ));

    // 3️⃣ 切割线：从左下到右上，与前两条切割线交叉
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -OUT * 0.5f),
        new Vector2(0,  OUT * 0.3f),
        new Vector2(OUT, -OUT * 0.5f)
    ));

    // 4️⃣ 切割线：水平切割，增加碎片数量
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0),
        new Vector2(OUT, 0)
    ));

    // 5️⃣ 切割线：垂直切割，进一步增加碎片数量
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0, -OUT),
        new Vector2(0, OUT)
    ));
}
private void GenerateLevel_5(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;

    // 1️⃣ 伪垂直脊柱（核心欺骗点）
    // 看起来是直的，其实是 S 型微弯，打破对称感
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.9f * L),
        new Vector2(-0.15f * L,  0.75f * L), // 轻微左偏
        new Vector2(-0.05f * L,  0.0f * L),  // 回正
        new Vector2(-0.20f * L, -0.60f * L), // 再次左偏
        new Vector2(-OUT, -0.9f * L)
    ));

    // 2️⃣ 右上斜切（制造 T 型块）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.40f * L),
        new Vector2( 0.20f * L,  0.20f * L),
        new Vector2( 0.60f * L,  0.50f * L),
        new Vector2( OUT,  0.30f * L)
    ));

    // 3️⃣ 右下斜切（制造 L 型块）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.30f * L),
        new Vector2( 0.30f * L, -0.10f * L),
        new Vector2( 0.50f * L, -0.50f * L),
        new Vector2( OUT, -0.20f * L)
    ));

    // 4️⃣ 顶部横断（封顶，制造边缘块）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.60f * L),
        new Vector2(-0.40f * L,  0.50f * L),
        new Vector2( 0.40f * L,  0.65f * L),
        new Vector2( OUT,  0.55f * L)
    ));
    
    // 5️⃣ 底部横断（封底）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.60f * L),
        new Vector2(-0.30f * L, -0.50f * L),
        new Vector2( 0.50f * L, -0.65f * L),
        new Vector2( OUT, -0.55f * L)
    ));
}

private void GenerateLevel_6(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;

    // 1️⃣ 主脊柱：垂直 Z 字（制造左侧大咬合块）
    // 这条线决定了左边是“凸”还是“凹”
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.9f * L),
        new Vector2(-0.5f * L,  0.9f * L), // 水平延伸
        new Vector2(-0.5f * L,  0.3f * L), // 下
        new Vector2(-0.2f * L,  0.3f * L), // 右
        new Vector2(-0.2f * L, -0.3f * L), // 下
        new Vector2(-0.5f * L, -0.3f * L), // 左
        new Vector2(-0.5f * L, -0.9f * L), // 下
        new Vector2(-OUT, -0.9f * L)
    ));

    // 2️⃣ 副脊柱：垂直反向 Z 字（制造右侧咬合块）
    level.cutPaths.Add(CreateCutPath(
        new Vector2( OUT,  0.9f * L),
        new Vector2( 0.5f * L,  0.9f * L),
        new Vector2( 0.5f * L,  0.3f * L),
        new Vector2( 0.2f * L,  0.3f * L),
        new Vector2( 0.2f * L, -0.3f * L),
        new Vector2( 0.5f * L, -0.3f * L),
        new Vector2( 0.5f * L, -0.9f * L),
        new Vector2( OUT, -0.9f * L)
    ));

    // 3️⃣ 顶部横梁：水平 Z 字（锁死顶部）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.6f * L),
        new Vector2(-0.6f * L,  0.6f * L),
        new Vector2(-0.6f * L,  0.8f * L),
        new Vector2( 0.6f * L,  0.8f * L),
        new Vector2( 0.6f * L,  0.6f * L),
        new Vector2( OUT,  0.6f * L)
    ));

    // 4️⃣ 底部横梁：水平反向 Z 字（锁死底部）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.6f * L),
        new Vector2(-0.6f * L, -0.6f * L),
        new Vector2(-0.6f * L, -0.8f * L),
        new Vector2( 0.6f * L, -0.8f * L),
        new Vector2( 0.6f * L, -0.6f * L),
        new Vector2( OUT, -0.6f * L)
    ));

    // 5️⃣ 中心交叉：打破绝对对称
    // 如果不切这一刀，中间就是个完美的矩形，太简单了
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.2f * L,  0.3f * L),
        new Vector2( 0.2f * L, -0.3f * L)
    ));
}

private void GenerateLevel_7(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;

    // 1️⃣ 主回环：巨大的偏心 C 型
    // 【修改点】：将顶部横向切口从 0.8f 提升到 0.9f，紧贴边界
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.5f * L),
        new Vector2(-0.8f * L,  0.5f * L), // 进
        new Vector2(-0.8f * L, -0.5f * L), // 下
        new Vector2( 0.5f * L, -0.5f * L), // 右
        new Vector2( 0.5f * L,  0.95f * L), // 上（【修改】更接近顶部）
        new Vector2(-0.2f * L,  0.95f * L), // 左（顶部横切【修改】位置上移）
        new Vector2(-0.2f * L,  0.2f * L), // 下（内部凹槽）
        new Vector2( 0.2f * L,  0.2f * L), // 右
        new Vector2( 0.2f * L, -0.2f * L), // 下
        new Vector2(-0.5f * L, -0.2f * L), // 左
        new Vector2(-0.5f * L,  0.2f * L), // 上（回到内部）
        new Vector2(-OUT,  0.2f * L)       // 出
    ));

    // 2️⃣ 断路 A：斜切一刀
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.8f * L),
        new Vector2( 0.8f * L,  0.5f * L)
    ));

    // 3️⃣ 断路 B：反向斜切
    level.cutPaths.Add(CreateCutPath(
        new Vector2( OUT, -0.8f * L),
        new Vector2(-0.8f * L,  0.5f * L)
    ));
    
    // 4️⃣ 顶部封盖：确保顶部碎片不会太小
    // 【修改点】：将终点 Y 轴从 0.7f 降至 0.6f
    // 目的：让这刀斜线更低一点，避开上面那个极窄的区域
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.9f * L),       // 起点保持高位（切角）
        new Vector2( OUT,  0.6f * L)        // 【修改】终点降低，扩大中间区域
    ));
}

// 先在类里添加最小间距检查工具方法（核心防护）
private bool IsDistanceSafe(Vector2 pointA, Vector2 pointB, float minSafeDistance)
{
    return Vector2.Distance(pointA, pointB) >= minSafeDistance;
}

private void InitRandomSeed(int levelNumber)
{
    UnityEngine.Random.InitState(levelNumber * 1000);
}

private void GenerateLevel_8(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    // 初始化随机种子（保证同一关卡切割形状固定）
    UnityEngine.Random.InitState(i * 1000);

    // 1️⃣ 主曲线A：S型贯穿（拉大间距，杜绝小碎片）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.9f * L),
        new Vector2(-0.6f * L,  0.3f * L), // 左中拐点：Y值抬高，拉大纵向间距
        new Vector2(0f,  0.1f * L),        // 中心拐点：Y值上调，避开小碎片区域
        new Vector2(0.6f * L, -0.3f * L),
        new Vector2(OUT, -0.9f * L)
    ));

    // 2️⃣ 主曲线B：反向S型（镜像调整，匹配主曲线A）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.9f * L),
        new Vector2(-0.6f * L, -0.3f * L),
        new Vector2(0f, -0.1f * L),        // 中心拐点：Y值下调，避开小碎片区域
        new Vector2(0.6f * L,  0.3f * L),
        new Vector2(OUT,  0.9f * L)
    ));

    // 3️⃣ 横向波浪线-上层（关键修正：抬高波谷，消除小三角）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.6f * L),
        new Vector2(-0.4f * L,  0.8f * L), // 波峰不变
        new Vector2(0.4f * L,  0.4f * L),  // 【核心修正】波谷从0.3→0.4，抬高0.1L
        new Vector2(OUT,  0.7f * L)
    ));

    // 4️⃣ 横向波浪线-下层（对称调整，保持视觉平衡）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.6f * L),
        new Vector2(-0.4f * L, -0.8f * L),
        new Vector2(0.4f * L, -0.4f * L),  // 对称下调0.1L
        new Vector2(OUT, -0.7f * L)
    ));

    // 5️⃣ 中心交叉线：简化为大斜线，杜绝密集交叉
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.9f * L, -0.2f * L), // 左下端点：向外拉0.1L
        new Vector2(0.9f * L,  0.2f * L)  // 右上端点：向外拉0.1L
    ));
}


// 辅助方法：创建带轻微弧度的曲线点（仅做小偏移，不制造小碎片）
private Vector2 CurveTo(Vector2 target, float curveIntensity)
{
    // 缩小偏移量（从0.5→0.3），避免曲线过度弯曲产生小碎片
    float randomOffsetX = (UnityEngine.Random.value - 0.3f) * curveIntensity;
    float randomOffsetY = (UnityEngine.Random.value - 0.3f) * curveIntensity;
    return new Vector2(target.x + randomOffsetX, target.y + randomOffsetY);
}


private void GenerateLevel_9(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L; // 保证所有碎片都足够大

    // 1️⃣ 主齿轮轮廓：巨大的偏心U型（咬合核心）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.2f * L),
        new Vector2(-0.7f * L,  0.2f * L), // 进入
        new Vector2(-0.7f * L,  0.7f * L), // 上移
        new Vector2( 0.7f * L,  0.7f * L), // 右移
        new Vector2( 0.7f * L, -0.7f * L), // 下移
        new Vector2(-0.7f * L, -0.7f * L), // 左移
        new Vector2(-0.7f * L, -0.2f * L), // 上移
        new Vector2(-OUT, -0.2f * L)       // 出去
    ));

    // 2️⃣ 横向咬合齿：上下两条Z型（制造卡扣）
    // 上层齿
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT,  0.5f * L),
        new Vector2(-0.5f * L,  0.5f * L),
        new Vector2(-0.5f * L,  0.6f * L),
        new Vector2( 0.5f * L,  0.6f * L),
        new Vector2( 0.5f * L,  0.5f * L),
        new Vector2( OUT,  0.5f * L)
    ));
    // 下层齿
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.5f * L),
        new Vector2(-0.5f * L, -0.5f * L),
        new Vector2(-0.5f * L, -0.6f * L),
        new Vector2( 0.5f * L, -0.6f * L),
        new Vector2( 0.5f * L, -0.5f * L),
        new Vector2( OUT, -0.5f * L)
    ));

    // 3️⃣ 纵向咬合齿：左右两条Z型（制造卡扣）
    // 左侧齿
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.5f * L, -OUT),
        new Vector2(-0.5f * L, -0.5f * L),
        new Vector2(-0.6f * L, -0.5f * L),
        new Vector2(-0.6f * L,  0.5f * L),
        new Vector2(-0.5f * L,  0.5f * L),
        new Vector2(-0.5f * L,  OUT)
    ));
    // 右侧齿
    level.cutPaths.Add(CreateCutPath(
        new Vector2( 0.5f * L, -OUT),
        new Vector2( 0.5f * L, -0.5f * L),
        new Vector2( 0.6f * L, -0.5f * L),
        new Vector2( 0.6f * L,  0.5f * L),
        new Vector2( 0.5f * L,  0.5f * L),
        new Vector2( 0.5f * L,  OUT)
    ));

    // 4️⃣ 中心十字：修正版（控制在齿轮内部，杜绝延伸出界）
    // 关键修改1：去掉错误的 *L（OUT已经是1.4f*L）
    // 关键修改2：十字线长度控制在0.8f*L，刚好在齿轮咬合齿内部
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.8f * L, 0f),  // 左端点：齿轮内部
        new Vector2(0.8f * L, 0f)    // 右端点：齿轮内部
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0f, -0.8f * L),  // 下端点：齿轮内部
        new Vector2(0f, 0.8f * L)    // 上端点：齿轮内部
    ));
}

private void GenerateLevel_10(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 主轮廓：梯形底座（不变）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -1.0f * L),
        new Vector2(-0.7f * L, -0.5f * L),
        new Vector2(0.7f * L, -0.5f * L),
        new Vector2(OUT, -1.0f * L),
        new Vector2(-OUT, -1.0f * L)
    ));

    // 2️⃣ 第一层阶梯：带双嵌齿的大梯形（不变）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.7f * L, -0.5f * L),
        new Vector2(-0.8f * L, 0.1f * L),
        new Vector2(-0.6f * L, -0.1f * L),
        new Vector2(0.6f * L, -0.1f * L),
        new Vector2(0.8f * L, 0.1f * L),
        new Vector2(0.7f * L, -0.5f * L)
    ));

    // 3️⃣ 第二层阶梯：带交叉斜撑的三角形（不变）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.8f * L, 0.1f * L),
        new Vector2(0f, 0.6f * L),
        new Vector2(0.8f * L, 0.1f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.6f * L, -0.1f * L),
        new Vector2(0.3f * L, 0.5f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.6f * L, -0.1f * L),
        new Vector2(-0.3f * L, 0.5f * L)
    ));

    // 4️⃣ 顶部符文：双三角嵌套（核心修复：闭合折线）
    // 【关键修改】：把断开的折线改成一个完整的闭合四边形
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.4f * L, 0.6f * L),
        new Vector2(-0.2f * L, 0.8f * L),
        new Vector2(0.2f * L, 0.8f * L),
        new Vector2(0.4f * L, 0.6f * L),
        new Vector2(-0.4f * L, 0.6f * L) // 闭合点
    ));
    // 内三角：保持不变
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.2f * L, 0.7f * L),
        new Vector2(0f, 0.75f * L),
        new Vector2(0.2f * L, 0.7f * L)
    ));

    // 5️⃣ 左右连接纹：不变
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.7f * L, -0.5f * L),
        new Vector2(-0.3f * L, 0.6f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.7f * L, -0.5f * L),
        new Vector2(0.3f * L, 0.6f * L)
    ));
    // 横向斜线：不变
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-1.2f * L, 0.4f * L),
        new Vector2(-0.8f * L, 0.1f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(1.2f * L, 0.4f * L),
        new Vector2(0.8f * L, 0.1f * L)
    ));

    // 6️⃣ 底部拆分：不变
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-1.0f * L, -1.0f * L),
        new Vector2(1.0f * L, -0.4f * L)
    ));
}

private void GenerateLevel_11(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 主干：粗壮的Y型结构（绝对无小碎片）
    // 左侧主枝
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.5f * L),
        new Vector2(-0.6f * L, 0.3f * L),
        new Vector2(-0.4f * L, -0.8f * L),
        new Vector2(-0.2f * L, -OUT)
    ));
    // 右侧主枝
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT, 0.5f * L),
        new Vector2(0.6f * L, 0.3f * L),
        new Vector2(0.4f * L, -0.8f * L),
        new Vector2(0.2f * L, -OUT)
    ));

    // 2️⃣ 树冠：顶部的大轮廓（控制整体形状）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.9f * L),
        new Vector2(-0.5f * L, 0.5f * L),
        new Vector2(0.5f * L, 0.5f * L),
        new Vector2(OUT, 0.9f * L)
    ));

    // 3️⃣ 内部枝丫：大块切割（制造形状，不制造小碎片）
    // 左上大枝
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.6f * L, 0.3f * L),
        new Vector2(-0.8f * L, 0.8f * L),
        new Vector2(-OUT, 0.9f * L)
    ));
    // 右上大枝
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.6f * L, 0.3f * L),
        new Vector2(0.8f * L, 0.8f * L),
        new Vector2(OUT, 0.9f * L)
    ));

    // 4️⃣ 中心交叉：打破对称，增加复杂度
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.5f * L, 0.5f * L),
        new Vector2(0.5f * L, -0.5f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.5f * L, 0.5f * L),
        new Vector2(-0.5f * L, -0.5f * L)
    ));

    // 5️⃣ 底部根须：简单的斜线切割
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.4f * L, -0.8f * L),
        new Vector2(0.4f * L, -0.8f * L)
    ));
}

private void GenerateLevel_12(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 主轮廓：不对称的大弧线（奠定宇宙感）
    // 左侧大弧线
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.8f * L),
        new Vector2(-0.8f * L, -0.4f * L),
        new Vector2(-0.9f * L, 0.4f * L),
        new Vector2(-OUT, 0.8f * L)
    ));
    // 右侧大弧线
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT, -0.8f * L),
        new Vector2(0.8f * L, -0.5f * L),
        new Vector2(0.8f * L, 0.5f * L),
        new Vector2(OUT, 0.8f * L)
    ));

    // 2️⃣ 几何分割：横向与纵向的大切割（制造大板块）
    // 横向分割
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.1f * L),
        new Vector2(OUT, 0.1f * L)
    ));
    // 纵向分割（不对称）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.2f * L, -OUT),
        new Vector2(-0.2f * L, OUT)
    ));

    // 3️⃣ 罗盘指针：斜向大切割（增加复杂度）
    // 左上指针
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.9f * L, 0.4f * L),
        new Vector2(-0.2f * L, 0.1f * L)
    ));
    // 右下指针
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.9f * L, -0.4f * L),
        new Vector2(-0.2f * L, 0.1f * L)
    ));

    // 4️⃣ 内部细节：安全的小分割（杜绝小碎片）
    // 右上内部
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.8f * L, 0.5f * L),
        new Vector2(0.2f * L, 0.8f * L)
    ));
    // 左下内部
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.8f * L, -0.4f * L),
        new Vector2(-0.4f * L, -0.8f * L)
    ));
}

private void GenerateLevel_13(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 主分割：两条巨大的交叉斜线（制造漩涡感）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, OUT),
        new Vector2(OUT, -OUT)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT, OUT),
        new Vector2(-OUT, -OUT)
    ));

    // 2️⃣ 层次分割：两条平行斜线（打破单调，制造层次感）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.5f * L),
        new Vector2(OUT, -0.5f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT, 0.5f * L),
        new Vector2(-OUT, -0.5f * L)
    ));

    // 3️⃣ 边界控制：顶部和底部的弧线（增加柔和感）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.8f * L),
        new Vector2(-0.6f * L, 0.6f * L),
        new Vector2(0.6f * L, 0.6f * L),
        new Vector2(OUT, 0.8f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.8f * L),
        new Vector2(-0.6f * L, -0.6f * L),
        new Vector2(0.6f * L, -0.6f * L),
        new Vector2(OUT, -0.8f * L)
    ));

    // 4️⃣ 内部细节：安全的小分割（杜绝小碎片）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.6f * L, 0.6f * L),
        new Vector2(-0.6f * L, -0.6f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.6f * L, 0.6f * L),
        new Vector2(0.6f * L, -0.6f * L)
    ));
}

private void GenerateLevel_14(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 第一步：切割成4个超大块（间距>0.6L，绝对安全）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, OUT),
        new Vector2(OUT, -OUT)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -OUT),
        new Vector2(OUT, OUT)
    ));

    // 2️⃣ 第二步：安全细分大块（只在大块内部切割，间距>0.3L）
    // 左上大块细分
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.6f * L),
        new Vector2(-0.2f * L, 0.2f * L)
    ));
    // 右上大块细分
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.2f * L, 0.6f * L),
        new Vector2(OUT, 0.2f * L)
    ));
    // 左下大块细分
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.2f * L),
        new Vector2(-0.2f * L, -0.6f * L)
    ));
    // 右下大块细分
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.2f * L, -0.2f * L),
        new Vector2(OUT, -0.6f * L)
    ));

    // 3️⃣ 第三步：柔和边界（弧线处理，避免尖角）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.8f * L),
        new Vector2(-0.4f * L, 0.6f * L),
        new Vector2(0.4f * L, 0.6f * L),
        new Vector2(OUT, 0.8f * L)
    ));
}

private void GenerateLevel_15(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 第一步：切割成3个巨大的水平区块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.5f * L),
        new Vector2(OUT, 0.5f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.2f * L),
        new Vector2(OUT, -0.2f * L)
    ));

    // 2️⃣ 第二步：用平缓斜线制造沙丘轮廓
    // 左沙丘
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.5f * L),
        new Vector2(-0.3f * L, -0.2f * L)
    ));
    // 中沙丘
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.3f * L, 0.5f * L),
        new Vector2(0.3f * L, -0.2f * L)
    ));
    // 右沙丘
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.3f * L, 0.5f * L),
        new Vector2(OUT, -0.2f * L)
    ));

    // 3️⃣ 第三步：安全细分底部区块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.2f * L),
        new Vector2(OUT, -OUT)
    ));
}

private void GenerateLevel_16(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 第一步：切割成4个巨型区块
    // 水平分割
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.3f * L),
        new Vector2(OUT, 0.3f * L)
    ));
    // 纵向分割
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.3f * L, -OUT),
        new Vector2(-0.3f * L, OUT)
    ));

    // 2️⃣ 第二步：制造冰盖的断裂感（安全斜线）
    // 左上冰盖
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, OUT),
        new Vector2(-0.3f * L, 0.3f * L)
    ));
    // 右上冰盖
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT, OUT),
        new Vector2(-0.3f * L, 0.3f * L)
    ));
    // 左下冰盖
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -OUT),
        new Vector2(-0.3f * L, 0.3f * L)
    ));
    // 右下冰盖
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT, -OUT),
        new Vector2(-0.3f * L, 0.3f * L)
    ));

    // 3️⃣ 第三步：安全细节（增加层次，无小碎片）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.7f * L),
        new Vector2(-0.7f * L, 0.3f * L)
    ));
}


private void GenerateLevel_17(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 第一组：陡峭的斜线（制造垂直方向的断层感）
    // 左起第一条
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.2f * L),
        new Vector2(-0.6f * L, -OUT)
    ));
    // 左起第二条（平行错位）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.4f * L, OUT),
        new Vector2(0.2f * L, 0.2f * L)
    ));
    // 左起第三条（平行错位）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.4f * L, OUT),
        new Vector2(OUT, 0.2f * L)
    ));

    // 2️⃣ 第二组：平缓的斜线（制造水平方向的断层感）
    // 这组线与第一组交叉，形成复杂的网格
    // 上起第一条
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.8f * L),
        new Vector2(OUT, -0.4f * L)
    ));
    // 上起第二条（平行错位）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.3f * L),
        new Vector2(OUT, -0.9f * L)
    ));

    // 3️⃣ 第三组：反方向的斜线（打破单调，增加复杂度）
    // 这组线是反向的，用来切割剩余的大空间
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.5f * L),
        new Vector2(0.5f * L, OUT)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.5f * L, -OUT),
        new Vector2(OUT, 0.5f * L)
    ));
}


private void GenerateLevel_18(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 第一步：安全大分割（先分4个巨型区块，间距>0.5L）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, OUT),
        new Vector2(OUT, -OUT)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -OUT),
        new Vector2(OUT, OUT)
    ));

    // 2️⃣ 第二步：可控复杂度（在每个大块内添加斜线，间距>0.3L）
    // 左上区块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.6f * L),
        new Vector2(-0.3f * L, 0.3f * L)
    ));
    // 右上区块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.3f * L, 0.3f * L),
        new Vector2(OUT, 0.6f * L)
    ));
    // 左下区块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.6f * L),
        new Vector2(-0.3f * L, -0.3f * L)
    ));
    // 右下区块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.3f * L, -0.3f * L),
        new Vector2(OUT, -0.6f * L)
    ));

    // 3️⃣ 第三步：制造干扰（添加两条平缓斜线，增加交叉逻辑）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.2f * L),
        new Vector2(OUT, -0.2f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.2f * L),
        new Vector2(OUT, 0.2f * L)
    ));

    // 4️⃣ 第四步：嵌套轮廓（添加两条弧线，保留星环的视觉感）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.8f * L),
        new Vector2(-0.5f * L, 0.6f * L),
        new Vector2(0.5f * L, 0.6f * L),
        new Vector2(OUT, 0.8f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.8f * L),
        new Vector2(-0.5f * L, -0.6f * L),
        new Vector2(0.5f * L, -0.6f * L),
        new Vector2(OUT, -0.8f * L)
    ));
}


private void GenerateLevel_19(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 第一步：切割成4个巨型基础块（间距>0.6L，绝对安全）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, OUT),
        new Vector2(OUT, -OUT)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -OUT),
        new Vector2(OUT, OUT)
    ));

    // 2️⃣ 第二步：安全细分（在每个大块内只切1条线，间距>0.3L）
    // 左上大块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.4f * L),
        new Vector2(-0.3f * L, 0.4f * L)
    ));
    // 右上大块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.3f * L, 0.4f * L),
        new Vector2(OUT, 0.4f * L)
    ));
    // 左下大块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.4f * L),
        new Vector2(-0.3f * L, -0.4f * L)
    ));
    // 右下大块
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.3f * L, -0.4f * L),
        new Vector2(OUT, -0.4f * L)
    ));

    // 3️⃣ 第三步：增加复杂度（添加两条大斜线，交叉点位于大块边缘）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.7f * L),
        new Vector2(0.3f * L, -0.7f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(OUT, 0.7f * L),
        new Vector2(-0.3f * L, -0.7f * L)
    ));
}


private void GenerateLevel_20(LevelDesignData level, int i, float L)
{
    float OUT = 1.4f * L;
    float MIN_SAFE = 0.3f * L;

    // 1️⃣ 头部外轮廓（适度延伸+部分闭合，稳定切割）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, 0.7f * L),
        new Vector2(-0.7f * L, 0.5f * L),
        new Vector2(0.7f * L, 0.5f * L),
        new Vector2(OUT, 0.7f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-OUT, -0.7f * L),
        new Vector2(-0.7f * L, -0.5f * L),
        new Vector2(0.7f * L, -0.5f * L),
        new Vector2(OUT, -0.7f * L)
    ));

    // 2️⃣ 耳朵（闭合三角形，特征明确）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.5f * L, 0.5f * L),
        new Vector2(-0.4f * L, 0.7f * L),
        new Vector2(-0.3f * L, 0.5f * L),
        new Vector2(-0.5f * L, 0.5f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.5f * L, 0.5f * L),
        new Vector2(0.4f * L, 0.7f * L),
        new Vector2(0.3f * L, 0.5f * L),
        new Vector2(0.5f * L, 0.5f * L)
    ));

    // 3️⃣ 眼睛（闭合轮廓，细节丰富）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.5f * L, 0.3f * L),
        new Vector2(-0.4f * L, 0.4f * L),
        new Vector2(-0.2f * L, 0.3f * L),
        new Vector2(-0.4f * L, 0.2f * L),
        new Vector2(-0.5f * L, 0.3f * L)
    ));
    level.cutPaths.Add(CreateCutPath(
        new Vector2(0.5f * L, 0.3f * L),
        new Vector2(0.4f * L, 0.4f * L),
        new Vector2(0.2f * L, 0.3f * L),
        new Vector2(0.4f * L, 0.2f * L),
        new Vector2(0.5f * L, 0.3f * L)
    ));

    // 4️⃣ 喙部（闭合三角形，精准定位）
    level.cutPaths.Add(CreateCutPath(
        new Vector2(-0.2f * L, 0f),
        new Vector2(0.2f * L, 0f),
        new Vector2(0f, -0.2f * L),
        new Vector2(-0.2f * L, 0f)
    ));

    // 5️⃣ 身体分割线（适度延伸，稳定大块）
    level.cutPaths.Add(CreateCutPath(new Vector2(-OUT, -0.2f * L), new Vector2(OUT, -0.2f * L)));
}










    #endregion

}
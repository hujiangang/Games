// LevelDesigner.cs - 关卡设计器
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
        //GenerateAllLevels();
        // 可以选择加载第一个关卡
        // LoadLevel(1);
        DouBao_GenerateAllLevels();
    }
    // 工具方法：创建切割路径
    private LevelDesignData.CutPath CreateCutPath(params Vector2[] points)
    {
        LevelDesignData.CutPath path = new LevelDesignData.CutPath();
        path.points = new List<Vector2>(points);
        return path;
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

    #region  Level_Generator_DeepSeek_太艺术了不行


    public void GenerateAllLevels()
    {
        allLevels.Clear();

        // 关卡1-20：简单切割
        for (int i = 1; i <= 20; i++)
        {
            allLevels.Add(GeneratePuzzleLevelUniversal(i));
        }

        // 关卡21-40：中等切割
        for (int i = 21; i <= 40; i++)
        {
            allLevels.Add(GenerateMediumLevel(i));
        }

        // 关卡41-66：复杂切割
        for (int i = 41; i <= 66; i++)
        {
            allLevels.Add(GenerateComplexLevel(i));
        }

        Debug.Log($"已生成 {allLevels.Count} 个关卡");
    }

    // 修正后的 GenerateSimpleLevel 方法
    private LevelDesignData GenerateSimpleLevel(int levelNum)
    {
        LevelDesignData level = new LevelDesignData();
        level.levelNumber = levelNum;
        level.gridSize = 4;
        level.cutPaths = new List<LevelDesignData.CutPath>();
        level.difficulty = Mathf.Clamp(levelNum * 0.2f, 1f, 5f);

        float L = CutterManager.cutterLength + AddLength;

        switch (levelNum)
        {
            case 1: // 简单十字分割
                level.description = "十字分割（4等分）";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, 0),
                    new Vector2(L, 0)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L),
                    new Vector2(0, -L)
                ));
                break;

            case 2: // 垂直三等分
                level.description = "垂直三等分";
                float third = L * 2 / 3; // 这里定义third
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L + third, L),
                    new Vector2(-L + third, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L + third * 2, L),
                    new Vector2(-L + third * 2, -L)
                ));
                break;

            case 3: // 水平三等分
                level.description = "水平三等分";
                float thirdH = L * 2 / 3; // 重命名避免混淆
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L - thirdH),
                    new Vector2(L, L - thirdH)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L - thirdH * 2),
                    new Vector2(L, L - thirdH * 2)
                ));
                break;

            case 4: // 田字格
                level.description = "田字格分割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L),
                    new Vector2(0, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, 0),
                    new Vector2(L, 0)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(L, L / 2)
                ));
                break;

            case 5: // L形切割
                level.description = "L形切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(L / 2, L / 2),
                    new Vector2(L / 2, -L)
                ));
                break;

            case 6: // 对角线分割
                level.description = "对角线分割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L),
                    new Vector2(L, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, -L),
                    new Vector2(L, L)
                ));
                break;

            case 7: // 井字格
                level.description = "井字格（9宫格）";
                float thirdL = L * 2 / 3;
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L + thirdL, L),
                    new Vector2(-L + thirdL, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L + thirdL * 2, L),
                    new Vector2(-L + thirdL * 2, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L - thirdL),
                    new Vector2(L, L - thirdL)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L - thirdL * 2),
                    new Vector2(L, L - thirdL * 2)
                ));
                break;

            case 8: // 十字加对角线
                level.description = "十字加对角线";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, 0),
                    new Vector2(L, 0)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L),
                    new Vector2(0, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L),
                    new Vector2(L, -L)
                ));
                break;

            case 9: // 阶梯状切割
                level.description = "阶梯切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(-L / 2, L / 2),
                    new Vector2(-L / 2, -L / 2),
                    new Vector2(L / 2, -L / 2),
                    new Vector2(L / 2, -L)
                ));
                break;

            case 10: // 十字偏移
                level.description = "偏移十字";
                float offset = L / 2;
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, offset),
                    new Vector2(L, offset)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(offset, L),
                    new Vector2(offset, -L)
                ));
                break;

            case 11: // Z形切割
                level.description = "Z字形切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(L / 2, L / 2),
                    new Vector2(L / 2, -L / 2),
                    new Vector2(-L, -L / 2)
                ));
                break;

            case 12: // 大十字加小十字
                level.description = "嵌套十字";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, 0),
                    new Vector2(L, 0)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L),
                    new Vector2(0, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L / 2),
                    new Vector2(L / 2, L / 2)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L / 2),
                    new Vector2(0, -L / 2)
                ));
                break;

            case 13: // 四分之一对角线
                level.description = "四分之一对角线";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, 0),
                    new Vector2(0, L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L),
                    new Vector2(L, 0)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(L, 0),
                    new Vector2(0, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, -L),
                    new Vector2(-L, 0)
                ));
                break;

            case 14: // 窗户格
                level.description = "窗户格";
                float quarter = L / 2;
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-quarter, L),
                    new Vector2(-quarter, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(quarter, L),
                    new Vector2(quarter, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, quarter),
                    new Vector2(L, quarter)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, -quarter),
                    new Vector2(L, -quarter)
                ));
                break;

            case 15: // 放射状
                level.description = "简单放射状";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, 0),
                    new Vector2(L, L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, 0),
                    new Vector2(-L, L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, 0),
                    new Vector2(L, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, 0),
                    new Vector2(-L, -L)
                ));
                break;

            case 16: // H形切割
                level.description = "H形切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 3, L),
                    new Vector2(-L / 3, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(L / 3, L),
                    new Vector2(L / 3, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 3, 0),
                    new Vector2(L / 3, 0)
                ));
                break;

            case 17: // 三角形分割
                level.description = "三角形分割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L),
                    new Vector2(L, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, -L),
                    new Vector2(L, L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L),
                    new Vector2(L, L)
                ));
                break;

            case 18: // 平行四边形
                level.description = "平行四边形分割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(L / 2, L / 2),
                    new Vector2(L / 2, -L),
                    new Vector2(-L, -L)
                ));
                break;

            case 19: // 工字形
                level.description = "工字形";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L),
                    new Vector2(L / 2, L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L),
                    new Vector2(0, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, -L),
                    new Vector2(L / 2, -L)
                ));
                break;

            case 20: // 简单迷宫
                level.description = "简单迷宫";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(0, L / 2),
                    new Vector2(0, -L / 2),
                    new Vector2(L, -L / 2)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L),
                    new Vector2(-L / 2, 0)
                ));
                break;

            default:
                // 如果还有更多关卡，使用随机生成
                level.description = $"简单切割 #{levelNum}";
                GenerateRandomSimpleCuts(level, levelNum, L);
                break;
        }

        return level;
    }

    private LevelDesignData GenerateMediumLevel(int levelNum)
    {
        LevelDesignData level = new LevelDesignData();
        level.levelNumber = levelNum;
        level.gridSize = 6;
        level.cutPaths = new List<LevelDesignData.CutPath>();
        level.difficulty = Mathf.Clamp(2 + (levelNum - 21) * 0.15f, 2f, 4f);

        float L = CutterManager.cutterLength + AddLength;

        switch (levelNum)
        {
            case 21: // 窗格切割
                level.description = "窗格切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L),
                    new Vector2(-L / 2, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(L / 2, L),
                    new Vector2(L / 2, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(L, L / 2)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, -L / 2),
                    new Vector2(L, -L / 2)
                ));
                break;

            case 22: // 迷宫式切割
                level.description = "简单迷宫";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(0, L / 2),
                    new Vector2(0, -L / 2),
                    new Vector2(L, -L / 2)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L),
                    new Vector2(-L / 2, 0),
                    new Vector2(L / 2, 0),
                    new Vector2(L / 2, -L)
                ));
                break;

            case 23: // 螺旋切割
                level.description = "螺旋切割";
                float step = L / 4;
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L + step, L),
                    new Vector2(-L + step, -L + step),
                    new Vector2(L - step, -L + step),
                    new Vector2(L - step, L - step * 2),
                    new Vector2(-L + step * 2, L - step * 2),
                    new Vector2(-L + step * 2, -L + step * 3),
                    new Vector2(L - step * 3, -L + step * 3)
                ));
                break;

            case 24: // 锯齿切割
                level.description = "锯齿形切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L / 2),
                    new Vector2(-L / 3, L),
                    new Vector2(L / 3, L / 2),
                    new Vector2(L, L),
                    new Vector2(L, -L / 2)
                ));
                break;

            case 25: // 星形切割
                level.description = "四角星";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, 0),
                    new Vector2(-L / 3, L / 3),
                    new Vector2(0, L),
                    new Vector2(L / 3, L / 3),
                    new Vector2(L, 0),
                    new Vector2(L / 3, -L / 3),
                    new Vector2(0, -L),
                    new Vector2(-L / 3, -L / 3),
                    new Vector2(-L, 0)
                ));
                break;

            case 26: // 字母T切割
                level.description = "T字形切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L),
                    new Vector2(L / 2, L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, L),
                    new Vector2(0, -L)
                ));
                break;

            case 27: // 字母L切割
                level.description = "L字形切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L),
                    new Vector2(-L / 2, -L / 2),
                    new Vector2(L, -L / 2)
                ));
                break;

            case 28: // 字母H切割
                level.description = "H字形切割";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, L),
                    new Vector2(-L / 2, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(L / 2, L),
                    new Vector2(L / 2, -L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L / 2, 0),
                    new Vector2(L / 2, 0)
                ));
                break;

            case 29: // 风车切割
                level.description = "风车叶片";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, 0),
                    new Vector2(0, 0),
                    new Vector2(0, L)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, 0),
                    new Vector2(L, 0)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(0, 0),
                    new Vector2(0, -L)
                ));
                break;

            case 30: // 拼图咬合
                level.description = "拼图咬合";
                GeneratePuzzleCuts(level, L);
                break;

            default:
                // 为31-40关生成更多中等变体
                level.description = $"中等切割 #{levelNum}";
                GenerateRandomMediumCuts(level, levelNum, L);
                break;
        }

        return level;
    }



    private LevelDesignData GenerateComplexLevel(int levelNum)
    {
        LevelDesignData level = new LevelDesignData();
        level.levelNumber = levelNum;
        level.gridSize = 8;
        level.cutPaths = new List<LevelDesignData.CutPath>();
        level.difficulty = Mathf.Clamp(3 + (levelNum - 41) * 0.1f, 3f, 5f);

        float L = CutterManager.cutterLength + AddLength;

        switch (levelNum)
        {
            case 41: // 复杂迷宫
                level.description = "复杂迷宫";
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, L * 0.8f),
                    new Vector2(-L * 0.6f, L * 0.8f),
                    new Vector2(-L * 0.6f, L * 0.2f),
                    new Vector2(-L * 0.2f, L * 0.2f),
                    new Vector2(-L * 0.2f, -L * 0.4f),
                    new Vector2(L * 0.4f, -L * 0.4f),
                    new Vector2(L * 0.4f, L * 0.6f),
                    new Vector2(L, L * 0.6f)
                ));
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L * 0.8f, L),
                    new Vector2(-L * 0.8f, -L * 0.2f),
                    new Vector2(L * 0.2f, -L * 0.2f),
                    new Vector2(L * 0.2f, -L)
                ));
                break;

            case 42: // 雪花分形
                level.description = "雪花分形简化版";
                GenerateSnowflakeCuts(level, L, 2); // 2级分形
                break;

            case 43: // 中国结图案
                level.description = "中国结简化";
                GenerateChineseKnotCuts(level, L);
                break;

            case 44: // 太极图切割
                level.description = "太极图分割";
                GenerateTaijiCuts(level, L);
                break;

            case 45: // 螺旋星系
                level.description = "螺旋星系";
                GenerateSpiralGalaxyCuts(level, L);
                break;

            case 46: // 城市天际线
                level.description = "城市天际线";
                GenerateSkylineCuts(level, L);
                break;

            case 47: // 电路板
                level.description = "电路板走线";
                GenerateCircuitBoardCuts(level, L);
                break;

            case 48: // 树形分叉
                level.description = "树形分叉";
                GenerateTreeCuts(level, L);
                break;

            case 49: // 波浪切割
                level.description = "波浪形";
                GenerateWaveCuts(level, L);
                break;

            case 50: // 蜂巢
                level.description = "蜂巢网格";
                GenerateHoneycombCuts(level, L);
                break;

            case 66: // 最终关卡：复杂艺术品
                level.description = "最终挑战：几何艺术";
                level.difficulty = 5f;
                GenerateFinalArtCuts(level, L);
                break;

            default:
                // 为51-65关生成复杂变体
                level.description = $"复杂切割 #{levelNum}";
                GenerateRandomComplexCuts(level, levelNum, L);
                break;
        }

        return level;
    }

    // 生成随机简单切割（用于填充关卡）
    private void GenerateRandomSimpleCuts(LevelDesignData level, int seed, float L)
    {
        Random.InitState(seed);

        int numCuts = Random.Range(1, 4);
        for (int i = 0; i < numCuts; i++)
        {
            bool isVertical = Random.value > 0.5f;
            if (isVertical)
            {
                float x = Random.Range(-L + 0.5f, L - 0.5f);
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(x, L),
                    new Vector2(x, -L)
                ));
            }
            else
            {
                float y = Random.Range(-L + 0.5f, L - 0.5f);
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-L, y),
                    new Vector2(L, y)
                ));
            }
        }
    }

    // 复杂切割生成函数示例
    private void GenerateSnowflakeCuts(LevelDesignData level, float L, int depth)
    {
        // 简化版科赫雪花
        Vector2 center = Vector2.zero;
        float radius = L * 0.8f;

        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI / 3;
            Vector2 start = center + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            float nextAngle = (i + 1) * Mathf.PI / 3;
            Vector2 end = center + new Vector2(
                Mathf.Cos(nextAngle) * radius,
                Mathf.Sin(nextAngle) * radius
            );

            // 添加分形细节
            List<Vector2> snowflakePoints = new List<Vector2>();
            snowflakePoints.Add(start);

            if (depth > 0)
            {
                Vector2 dir = (end - start).normalized;
                float length = Vector2.Distance(start, end) / 3;

                for (int d = 1; d <= 3; d++)
                {
                    Vector2 segmentStart = start + dir * (length * (d - 1));
                    Vector2 segmentEnd = start + dir * (length * d);

                    if (d % 3 != 0)
                    {
                        snowflakePoints.Add(segmentEnd);
                    }
                    else
                    {
                        // 添加三角形突出
                        Vector2 mid = (segmentStart + segmentEnd) / 2;
                        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
                        Vector2 peak = mid + perpendicular * (length * 0.5f);

                        snowflakePoints.Add(peak);
                        snowflakePoints.Add(segmentEnd);
                    }
                }
            }

            level.cutPaths.Add(CreateCutPath(snowflakePoints.ToArray()));
        }
    }

    private void GenerateChineseKnotCuts(LevelDesignData level, float L)
    {
        // 简化中国结图案
        List<Vector2> path1 = new List<Vector2>();
        List<Vector2> path2 = new List<Vector2>();

        float r = L * 0.7f;

        // 第一个环
        for (int i = 0; i <= 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            float x = Mathf.Cos(angle) * r;
            float y = Mathf.Sin(angle) * r;
            path1.Add(new Vector2(x, y));
        }

        // 第二个交叉环
        for (int i = 0; i <= 8; i++)
        {
            float angle = i * Mathf.PI / 4 + Mathf.PI / 8;
            float x = Mathf.Cos(angle) * r * 0.8f;
            float y = Mathf.Sin(angle) * r * 0.8f;
            path2.Add(new Vector2(x, y));
        }

        level.cutPaths.Add(CreateCutPath(path1.ToArray()));
        level.cutPaths.Add(CreateCutPath(path2.ToArray()));
    }

    private void GenerateTaijiCuts(LevelDesignData level, float L)
    {
        // 太极图分割线
        float radius = L * 0.8f;
        Vector2 center = Vector2.zero;

        // S形曲线
        List<Vector2> sCurve = new List<Vector2>();
        int segments = 20;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI;
            float x = Mathf.Cos(angle) * radius * 0.3f;
            float y = Mathf.Sin(angle) * radius * (1 - t * 0.5f);
            sCurve.Add(center + new Vector2(x, y));
        }

        level.cutPaths.Add(CreateCutPath(sCurve.ToArray()));

        // 两个小圆
        float smallRadius = radius * 0.2f;
        Vector2 topCenter = center + new Vector2(0, radius * 0.5f);
        Vector2 bottomCenter = center - new Vector2(0, radius * 0.5f);

        List<Vector2> topCircle = new List<Vector2>();
        List<Vector2> bottomCircle = new List<Vector2>();

        for (int i = 0; i <= 12; i++)
        {
            float angle = i * Mathf.PI * 2 / 12;
            topCircle.Add(topCenter + new Vector2(
                Mathf.Cos(angle) * smallRadius,
                Mathf.Sin(angle) * smallRadius
            ));
            bottomCircle.Add(bottomCenter + new Vector2(
                Mathf.Cos(angle) * smallRadius,
                Mathf.Sin(angle) * smallRadius
            ));
        }

        level.cutPaths.Add(CreateCutPath(topCircle.ToArray()));
        level.cutPaths.Add(CreateCutPath(bottomCircle.ToArray()));
    }

    // 在LevelDesigner类中添加以下方法

    private void GenerateSpiralGalaxyCuts(LevelDesignData level, float L)
    {
        // 螺旋星系图案
        level.description = "螺旋星系";

        // 主螺旋臂
        List<Vector2> spiral1 = new List<Vector2>();
        List<Vector2> spiral2 = new List<Vector2>();

        int segments = 24;
        float spiralTurns = 1.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2 * spiralTurns;
            float radius = t * L * 0.7f;

            // 第一螺旋臂
            Vector2 point1 = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
            spiral1.Add(point1);

            // 第二螺旋臂（旋转180度）
            Vector2 point2 = new Vector2(
                Mathf.Cos(angle + Mathf.PI) * radius * 0.9f,
                Mathf.Sin(angle + Mathf.PI) * radius * 0.9f
            );
            spiral2.Add(point2);
        }

        level.cutPaths.Add(CreateCutPath(spiral1.ToArray()));
        level.cutPaths.Add(CreateCutPath(spiral2.ToArray()));

        // 中心星系核
        List<Vector2> nucleus = new List<Vector2>();
        int nucleusSegments = 12;
        float nucleusRadius = L * 0.2f;

        for (int i = 0; i <= nucleusSegments; i++)
        {
            float angle = i * Mathf.PI * 2 / nucleusSegments;
            nucleus.Add(new Vector2(
                Mathf.Cos(angle) * nucleusRadius,
                Mathf.Sin(angle) * nucleusRadius
            ));
        }
        level.cutPaths.Add(CreateCutPath(nucleus.ToArray()));
    }

    private void GenerateSkylineCuts(LevelDesignData level, float L)
    {
        // 城市天际线
        level.description = "城市天际线";

        float buildingWidth = L * 0.15f;
        int buildingCount = 8;
        float minHeight = L * 0.2f;
        float maxHeight = L * 0.8f;

        Random.InitState(42); // 固定随机种子

        for (int i = 0; i < buildingCount; i++)
        {
            float xPos = -L + i * (L * 2 / buildingCount) + buildingWidth * 0.5f;
            float height = minHeight + Random.value * (maxHeight - minHeight);

            // 建筑主体
            List<Vector2> building = new List<Vector2>();
            building.Add(new Vector2(xPos - buildingWidth * 0.4f, -L));
            building.Add(new Vector2(xPos - buildingWidth * 0.4f, -L + height));

            // 随机屋顶形状
            float roofType = Random.value;
            if (roofType < 0.3f)
            {
                // 平顶
                building.Add(new Vector2(xPos + buildingWidth * 0.4f, -L + height));
            }
            else if (roofType < 0.6f)
            {
                // 尖顶
                building.Add(new Vector2(xPos, -L + height * 1.2f));
                building.Add(new Vector2(xPos + buildingWidth * 0.4f, -L + height));
            }
            else
            {
                // 圆顶
                building.Add(new Vector2(xPos + buildingWidth * 0.2f, -L + height * 1.1f));
                building.Add(new Vector2(xPos + buildingWidth * 0.4f, -L + height));
            }

            building.Add(new Vector2(xPos + buildingWidth * 0.4f, -L));

            level.cutPaths.Add(CreateCutPath(building.ToArray()));
        }

        // 地面线
        level.cutPaths.Add(CreateCutPath(
            new Vector2(-L, -L),
            new Vector2(L, -L)
        ));
    }

    private void GenerateCircuitBoardCuts(LevelDesignData level, float L)
    {
        // 电路板走线图案
        level.description = "电路板走线";

        Random.InitState(43);

        // 水平走线
        for (int i = 0; i < 5; i++)
        {
            float y = -L + (i + 0.5f) * (L * 2 / 6);
            List<Vector2> track = new List<Vector2>();

            float x = -L;
            track.Add(new Vector2(x, y));

            while (x < L)
            {
                float segmentLength = Random.Range(L * 0.2f, L * 0.4f);
                x = Mathf.Min(x + segmentLength, L);

                if (Random.value > 0.7f && i > 0 && i < 4)
                {
                    // 添加垂直连接
                    float verticalLength = L * 0.3f;
                    float dir = Random.value > 0.5f ? 1 : -1;
                    track.Add(new Vector2(x, y + verticalLength * dir));
                    track.Add(new Vector2(x, y));
                }

                track.Add(new Vector2(x, y));
            }

            level.cutPaths.Add(CreateCutPath(track.ToArray()));
        }

        // 垂直走线
        for (int i = 0; i < 4; i++)
        {
            float x = -L + (i + 1) * (L * 2 / 5);
            List<Vector2> track = new List<Vector2>();

            float y = -L;
            track.Add(new Vector2(x, y));

            while (y < L)
            {
                float segmentLength = Random.Range(L * 0.2f, L * 0.4f);
                y = Mathf.Min(y + segmentLength, L);

                if (Random.value > 0.6f)
                {
                    // 添加水平连接
                    float horizontalLength = L * 0.2f;
                    float dir = Random.value > 0.5f ? 1 : -1;
                    track.Add(new Vector2(x + horizontalLength * dir, y));
                    track.Add(new Vector2(x, y));
                }

                track.Add(new Vector2(x, y));
            }

            level.cutPaths.Add(CreateCutPath(track.ToArray()));
        }

        // 添加一些芯片（矩形）
        for (int i = 0; i < 3; i++)
        {
            float chipSize = L * 0.15f;
            Vector2 center = new Vector2(
                Random.Range(-L + chipSize, L - chipSize),
                Random.Range(-L + chipSize, L - chipSize)
            );

            List<Vector2> chip = new List<Vector2>();
            chip.Add(center + new Vector2(-chipSize, -chipSize));
            chip.Add(center + new Vector2(-chipSize, chipSize));
            chip.Add(center + new Vector2(chipSize, chipSize));
            chip.Add(center + new Vector2(chipSize, -chipSize));
            chip.Add(center + new Vector2(-chipSize, -chipSize));

            level.cutPaths.Add(CreateCutPath(chip.ToArray()));
        }
    }

    private void GenerateTreeCuts(LevelDesignData level, float L)
    {
        // 树形分叉图案（递归）
        level.description = "树形分叉";

        Vector2 trunkBase = new Vector2(0, -L);
        float trunkHeight = L * 0.6f;
        float trunkWidth = L * 0.05f;

        // 树干
        List<Vector2> trunk = new List<Vector2>();
        trunk.Add(trunkBase + new Vector2(-trunkWidth, 0));
        trunk.Add(new Vector2(-trunkWidth, -L + trunkHeight));
        trunk.Add(new Vector2(trunkWidth, -L + trunkHeight));
        trunk.Add(trunkBase + new Vector2(trunkWidth, 0));

        level.cutPaths.Add(CreateCutPath(trunk.ToArray()));

        // 递归生成树枝
        GenerateBranch(level, new Vector2(0, -L + trunkHeight), L * 0.5f, Mathf.PI / 2, 3, L);
    }

    private void GenerateBranch(LevelDesignData level, Vector2 start, float length, float angle, int depth, float L)
    {
        if (depth <= 0 || length < L * 0.05f) return;

        Vector2 end = start + new Vector2(
            Mathf.Cos(angle) * length,
            Mathf.Sin(angle) * length
        );

        // 树枝主干
        List<Vector2> branch = new List<Vector2>();
        float width = length * 0.1f;

        // 计算垂直方向
        Vector2 dir = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);

        branch.Add(start - perpendicular * width * 0.5f);
        branch.Add(end - perpendicular * width * 0.5f);
        branch.Add(end + perpendicular * width * 0.5f);
        branch.Add(start + perpendicular * width * 0.5f);
        branch.Add(start - perpendicular * width * 0.5f);

        level.cutPaths.Add(CreateCutPath(branch.ToArray()));

        // 递归生成子树枝
        if (depth > 1)
        {
            // 左分支
            GenerateBranch(level, end, length * 0.6f, angle + Mathf.PI * 0.3f, depth - 1, L);
            // 右分支
            GenerateBranch(level, end, length * 0.6f, angle - Mathf.PI * 0.3f, depth - 1, L);

            if (Random.value > 0.5f)
            {
                // 随机中间分支
                GenerateBranch(level, end, length * 0.4f, angle, depth - 1, L);
            }
        }
    }

    private void GenerateWaveCuts(LevelDesignData level, float L)
    {
        // 波浪形切割
        level.description = "波浪形图案";

        // 主波浪线
        List<Vector2> wave1 = new List<Vector2>();
        List<Vector2> wave2 = new List<Vector2>();

        int segments = 24;
        float amplitude = L * 0.3f;
        float frequency = 3f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = -L + t * L * 2;
            float y1 = Mathf.Sin(t * Mathf.PI * frequency) * amplitude * 0.8f;
            float y2 = Mathf.Sin(t * Mathf.PI * frequency + Mathf.PI) * amplitude * 0.6f;

            wave1.Add(new Vector2(x, y1));
            wave2.Add(new Vector2(x, y2));
        }

        level.cutPaths.Add(CreateCutPath(wave1.ToArray()));
        level.cutPaths.Add(CreateCutPath(wave2.ToArray()));

        // 垂直波浪线
        List<Vector2> verticalWave = new List<Vector2>();
        frequency = 2f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float y = -L + t * L * 2;
            float x = Mathf.Sin(t * Mathf.PI * frequency) * amplitude * 0.5f;

            verticalWave.Add(new Vector2(x, y));
        }

        level.cutPaths.Add(CreateCutPath(verticalWave.ToArray()));

        // 圆形波浪
        List<Vector2> circularWave = new List<Vector2>();
        int circleSegments = 36;
        float circleRadius = L * 0.4f;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * Mathf.PI * 2 / circleSegments;
            float radiusVariation = Mathf.Sin(angle * 4) * L * 0.1f;
            float radius = circleRadius + radiusVariation;

            circularWave.Add(new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            ));
        }

        level.cutPaths.Add(CreateCutPath(circularWave.ToArray()));
    }

    private void GenerateHoneycombCuts(LevelDesignData level, float L)
    {
        // 蜂巢网格
        level.description = "蜂巢网格";

        float hexSize = L * 0.2f;
        float hexWidth = hexSize * Mathf.Sqrt(3);
        float hexHeight = hexSize * 2;

        int rows = Mathf.FloorToInt(L * 2 / hexHeight * 0.8f);
        int cols = Mathf.FloorToInt(L * 2 / hexWidth * 0.8f);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                float xOffset = (row % 2 == 0) ? 0 : hexWidth * 0.5f;
                float x = -L + col * hexWidth + xOffset + hexWidth * 0.5f;
                float y = -L + row * hexHeight * 0.75f + hexHeight * 0.5f;

                // 跳过超出边界的蜂巢
                if (Mathf.Abs(x) > L * 0.9f || Mathf.Abs(y) > L * 0.9f) continue;

                // 创建六边形
                List<Vector2> hexagon = new List<Vector2>();
                for (int i = 0; i <= 6; i++)
                {
                    float angle = i * Mathf.PI / 3;
                    hexagon.Add(new Vector2(
                        x + Mathf.Cos(angle) * hexSize,
                        y + Mathf.Sin(angle) * hexSize
                    ));
                }

                level.cutPaths.Add(CreateCutPath(hexagon.ToArray()));
            }
        }

        // 随机移除一些蜂巢以创造变化
        Random.InitState(44);
        int removeCount = Mathf.FloorToInt(level.cutPaths.Count * 0.3f);
        for (int i = 0; i < removeCount; i++)
        {
            int index = Random.Range(0, level.cutPaths.Count);
            level.cutPaths.RemoveAt(index);
        }
    }

    private void GenerateFinalArtCuts(LevelDesignData level, float L)
    {
        // 最终艺术品：复杂的几何组合
        level.description = "几何艺术";
        level.difficulty = 5f;

        // 1. 中心曼陀罗
        List<Vector2> mandala = new List<Vector2>();
        int mandalaLayers = 3;
        int mandalaSegments = 24;

        for (int layer = 1; layer <= mandalaLayers; layer++)
        {
            float radius = L * 0.2f * layer;
            float patternFrequency = layer * 4;

            for (int i = 0; i <= mandalaSegments; i++)
            {
                float angle = i * Mathf.PI * 2 / mandalaSegments;
                float radiusVariation = Mathf.Sin(angle * patternFrequency) * radius * 0.2f;
                float currentRadius = radius + radiusVariation;

                mandala.Add(new Vector2(
                    Mathf.Cos(angle) * currentRadius,
                    Mathf.Sin(angle) * currentRadius
                ));
            }

            level.cutPaths.Add(CreateCutPath(mandala.ToArray()));
            mandala.Clear();
        }

        // 2. 放射状线条
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            List<Vector2> ray = new List<Vector2>();

            // 波浪状放射线
            int raySegments = 10;
            for (int j = 0; j <= raySegments; j++)
            {
                float t = j / (float)raySegments;
                float radius = L * 0.8f * t;
                float wave = Mathf.Sin(t * Mathf.PI * 3) * L * 0.05f;

                Vector2 basePoint = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );

                Vector2 perpendicular = new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle));
                Vector2 waveOffset = perpendicular * wave;

                ray.Add(basePoint + waveOffset);
            }

            level.cutPaths.Add(CreateCutPath(ray.ToArray()));
        }

        // 3. 复杂螺旋嵌套
        for (int spiral = 0; spiral < 2; spiral++)
        {
            List<Vector2> nestedSpiral = new List<Vector2>();
            int spiralSegments = 30;
            float spiralTurns = 2 + spiral * 0.5f;
            float spiralDirection = spiral % 2 == 0 ? 1 : -1;

            for (int i = 0; i <= spiralSegments; i++)
            {
                float t = i / (float)spiralSegments;
                float angle = t * Mathf.PI * 2 * spiralTurns * spiralDirection;
                float radius = t * L * 0.6f + L * 0.1f;

                // 添加二级波动
                float wave = Mathf.Sin(t * Mathf.PI * 8) * L * 0.03f;

                nestedSpiral.Add(new Vector2(
                    Mathf.Cos(angle) * (radius + wave),
                    Mathf.Sin(angle) * (radius + wave)
                ));
            }

            level.cutPaths.Add(CreateCutPath(nestedSpiral.ToArray()));
        }

        // 4. 几何碎片图案
        Random.InitState(66);
        int fragments = 12;
        for (int i = 0; i < fragments; i++)
        {
            float angle = i * Mathf.PI * 2 / fragments;
            float radius = L * 0.4f + Random.value * L * 0.2f;

            Vector2 center = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            // 随机不规则多边形
            List<Vector2> fragment = new List<Vector2>();
            int sides = Random.Range(3, 6);
            float fragmentSize = L * 0.1f;

            for (int j = 0; j <= sides; j++)
            {
                float fragmentAngle = j * Mathf.PI * 2 / sides;
                float randomOffset = (Random.value - 0.5f) * fragmentSize * 0.3f;
                float currentSize = fragmentSize + randomOffset;

                fragment.Add(center + new Vector2(
                    Mathf.Cos(fragmentAngle) * currentSize,
                    Mathf.Sin(fragmentAngle) * currentSize
                ));
            }

            level.cutPaths.Add(CreateCutPath(fragment.ToArray()));
        }
    }

    private void GenerateRandomComplexCuts(LevelDesignData level, int seed, float L)
    {
        // 随机复杂切割生成器
        Random.InitState(seed * 100);

        int patternType = seed % 6;

        switch (patternType)
        {
            case 0:
                // 星爆图案
                GenerateStarburstCuts(level, L, 12 + seed % 6);
                level.description = $"星爆图案 #{seed}";
                break;

            case 1:
                // 编织图案
                GenerateWeaveCuts(level, L, 4 + seed % 3);
                level.description = $"编织图案 #{seed}";
                break;

            case 2:
                // 分形树
                GenerateFractalTreeCuts(level, L, 4 + seed % 2);
                level.description = $"分形树 #{seed}";
                break;

            case 3:
                // 光学错觉
                GenerateOpticalIllusionCuts(level, L);
                level.description = $"光学错觉 #{seed}";
                break;

            case 4:
                // 分子结构
                GenerateMolecularCuts(level, L, 8 + seed % 4);
                level.description = $"分子结构 #{seed}";
                break;

            case 5:
                // 抽象艺术
                GenerateAbstractArtCuts(level, L);
                level.description = $"抽象艺术 #{seed}";
                break;
        }
    }

    // 辅助生成函数
    private void GenerateStarburstCuts(LevelDesignData level, float L, int rays)
    {
        for (int i = 0; i < rays; i++)
        {
            float angle = i * Mathf.PI * 2 / rays;
            float rayLength = L * (0.6f + Random.value * 0.3f);

            List<Vector2> ray = new List<Vector2>();
            ray.Add(Vector2.zero);

            // 主射线
            ray.Add(new Vector2(
                Mathf.Cos(angle) * rayLength,
                Mathf.Sin(angle) * rayLength
            ));

            // 侧分支
            if (Random.value > 0.5f)
            {
                float branchAngle = angle + (Random.value - 0.5f) * Mathf.PI * 0.5f;
                float branchLength = rayLength * (0.3f + Random.value * 0.3f);

                ray.Add(new Vector2(
                    Mathf.Cos(branchAngle) * branchLength,
                    Mathf.Sin(branchAngle) * branchLength
                ));
                ray.Add(ray[1]); // 返回主射线端点
            }

            level.cutPaths.Add(CreateCutPath(ray.ToArray()));
        }
    }

    private void GenerateWeaveCuts(LevelDesignData level, float L, int strands)
    {
        // 编织图案
        for (int s = 0; s < strands; s++)
        {
            List<Vector2> strand = new List<Vector2>();
            int segments = 12;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float x = -L + t * L * 2;

                // 正弦波位置
                float baseY = -L + (s + 0.5f) * (L * 2 / strands);
                float wave = Mathf.Sin(t * Mathf.PI * strands + s * Mathf.PI * 2 / strands) * L * 0.2f;

                strand.Add(new Vector2(x, baseY + wave));
            }

            level.cutPaths.Add(CreateCutPath(strand.ToArray()));
        }
    }

    private void GenerateFractalTreeCuts(LevelDesignData level, float L, int maxDepth)
    {
        // 分形树生成
        GenerateFractalBranch(level, new Vector2(0, -L), L * 0.7f, Mathf.PI / 2, maxDepth, 0, L);
    }

    private void GenerateFractalBranch(LevelDesignData level, Vector2 start, float length, float angle, int maxDepth, int currentDepth, float L)
    {
        if (currentDepth >= maxDepth || length < L * 0.02f) return;

        Vector2 end = start + new Vector2(
            Mathf.Cos(angle) * length,
            Mathf.Sin(angle) * length
        );

        // 绘制树枝
        List<Vector2> branch = new List<Vector2>();
        branch.Add(start);
        branch.Add(end);
        level.cutPaths.Add(CreateCutPath(branch.ToArray()));

        // 生成子分支
        int childBranches = 2;
        float angleSpread = Mathf.PI * 0.4f / (currentDepth + 1);

        for (int i = 0; i < childBranches; i++)
        {
            float childAngle = angle - angleSpread / 2 + i * angleSpread;
            float childLength = length * (0.6f + Random.value * 0.2f);

            GenerateFractalBranch(level, end, childLength, childAngle, maxDepth, currentDepth + 1, L);
        }
    }

    // 添加这些辅助方法到LevelDesigner类中

    private void GenerateOpticalIllusionCuts(LevelDesignData level, float L)
    {
        // 光学错觉：同心圆加放射线
        level.description = "光学错觉图案";

        // 同心圆
        for (int i = 1; i <= 3; i++)
        {
            float radius = L * 0.2f * i;
            List<Vector2> circle = new List<Vector2>();
            int segments = 24;

            for (int j = 0; j <= segments; j++)
            {
                float angle = j * Mathf.PI * 2 / segments;
                circle.Add(new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                ));
            }
            level.cutPaths.Add(CreateCutPath(circle.ToArray()));
        }

        // 放射线
        int rays = 12;
        for (int i = 0; i < rays; i++)
        {
            float angle = i * Mathf.PI * 2 / rays;
            level.cutPaths.Add(CreateCutPath(
                Vector2.zero,
                new Vector2(
                    Mathf.Cos(angle) * L * 0.8f,
                    Mathf.Sin(angle) * L * 0.8f
                )
            ));
        }
    }

    private void GenerateMolecularCuts(LevelDesignData level, float L, int atoms)
    {
        // 分子结构图案
        Random.InitState(level.levelNumber);

        // 原子（节点）
        List<Vector2> atomPositions = new List<Vector2>();
        for (int i = 0; i < atoms; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(-L * 0.7f, L * 0.7f),
                Random.Range(-L * 0.7f, L * 0.7f)
            );
            atomPositions.Add(pos);

            // 绘制原子（小圆）
            List<Vector2> atomCircle = new List<Vector2>();
            float atomRadius = L * 0.05f;
            int segments = 12;
            for (int j = 0; j <= segments; j++)
            {
                float angle = j * Mathf.PI * 2 / segments;
                atomCircle.Add(pos + new Vector2(
                    Mathf.Cos(angle) * atomRadius,
                    Mathf.Sin(angle) * atomRadius
                ));
            }
            level.cutPaths.Add(CreateCutPath(atomCircle.ToArray()));
        }

        // 化学键（连接线）
        for (int i = 0; i < atoms; i++)
        {
            for (int j = i + 1; j < atoms; j++)
            {
                if (Vector2.Distance(atomPositions[i], atomPositions[j]) < L * 0.5f)
                {
                    // 单键
                    level.cutPaths.Add(CreateCutPath(
                        atomPositions[i],
                        atomPositions[j]
                    ));

                    // 随机添加双键或三键
                    if (Random.value > 0.7f)
                    {
                        Vector2 dir = (atomPositions[j] - atomPositions[i]).normalized;
                        Vector2 perpendicular = new Vector2(-dir.y, dir.x);
                        float offset = L * 0.02f;

                        level.cutPaths.Add(CreateCutPath(
                            atomPositions[i] + perpendicular * offset,
                            atomPositions[j] + perpendicular * offset
                        ));

                        if (Random.value > 0.8f)
                        {
                            level.cutPaths.Add(CreateCutPath(
                                atomPositions[i] - perpendicular * offset,
                                atomPositions[j] - perpendicular * offset
                            ));
                        }
                    }
                }
            }
        }
    }

    // 在LevelDesigner类中添加这些方法

    private void GeneratePuzzleCuts(LevelDesignData level, float L)
    {
        // 拼图咬合图案
        level.description = "拼图咬合";

        // 创建拼图咬合边缘
        float pieceWidth = L * 0.5f;
        float pieceHeight = L * 0.5f;
        float notchDepth = L * 0.1f;
        float notchWidth = L * 0.05f;

        // 左上角碎片（凸-凸）
        List<Vector2> piece1 = new List<Vector2>();
        piece1.Add(new Vector2(-L, L));
        piece1.Add(new Vector2(-L + pieceWidth, L));

        // 上边缘凸出
        piece1.Add(new Vector2(-L + pieceWidth + notchWidth, L));
        piece1.Add(new Vector2(-L + pieceWidth + notchWidth, L - notchDepth));
        piece1.Add(new Vector2(-L + pieceWidth, L - notchDepth));

        piece1.Add(new Vector2(-L + pieceWidth, L - pieceHeight));
        piece1.Add(new Vector2(-L, L - pieceHeight));
        piece1.Add(new Vector2(-L, L));

        level.cutPaths.Add(CreateCutPath(piece1.ToArray()));

        // 右上角碎片（凹-凸）
        List<Vector2> piece2 = new List<Vector2>();
        piece2.Add(new Vector2(-L + pieceWidth, L - notchDepth));
        piece2.Add(new Vector2(-L + pieceWidth + notchWidth, L - notchDepth));
        piece2.Add(new Vector2(-L + pieceWidth + notchWidth, L));
        piece2.Add(new Vector2(L, L));
        piece2.Add(new Vector2(L, L - pieceHeight));

        // 右边缘凸出
        piece2.Add(new Vector2(L - notchDepth, L - pieceHeight));
        piece2.Add(new Vector2(L - notchDepth, L - pieceHeight + notchWidth));
        piece2.Add(new Vector2(L, L - pieceHeight + notchWidth));

        piece2.Add(new Vector2(L, L - pieceHeight));
        piece2.Add(new Vector2(-L + pieceWidth, L - pieceHeight));
        piece2.Add(new Vector2(-L + pieceWidth, L - notchDepth));

        level.cutPaths.Add(CreateCutPath(piece2.ToArray()));

        // 左下角碎片（凸-凹）
        List<Vector2> piece3 = new List<Vector2>();
        piece3.Add(new Vector2(-L, L - pieceHeight));
        piece3.Add(new Vector2(-L + pieceWidth, L - pieceHeight));

        // 下边缘凹入
        piece3.Add(new Vector2(-L + pieceWidth, -L));
        piece3.Add(new Vector2(-L + pieceWidth + notchWidth, -L));
        piece3.Add(new Vector2(-L + pieceWidth + notchWidth, -L + notchDepth));
        piece3.Add(new Vector2(-L + pieceWidth, -L + notchDepth));

        piece3.Add(new Vector2(-L, -L + notchDepth));
        piece3.Add(new Vector2(-L, L - pieceHeight));

        level.cutPaths.Add(CreateCutPath(piece3.ToArray()));

        // 右下角碎片（凹-凹）
        List<Vector2> piece4 = new List<Vector2>();
        piece4.Add(new Vector2(-L + pieceWidth, L - pieceHeight));
        piece4.Add(new Vector2(L, L - pieceHeight));

        // 右边缘凹入
        piece4.Add(new Vector2(L, -L + notchDepth));
        piece4.Add(new Vector2(L - notchDepth, -L + notchDepth));
        piece4.Add(new Vector2(L - notchDepth, -L + notchDepth + notchWidth));
        piece4.Add(new Vector2(L, -L + notchDepth + notchWidth));

        // 下边缘凸出
        piece4.Add(new Vector2(L, -L));
        piece4.Add(new Vector2(-L + pieceWidth + notchWidth, -L));
        piece4.Add(new Vector2(-L + pieceWidth + notchWidth, -L + notchDepth));
        piece4.Add(new Vector2(-L + pieceWidth, -L + notchDepth));
        piece4.Add(new Vector2(-L + pieceWidth, L - pieceHeight));

        level.cutPaths.Add(CreateCutPath(piece4.ToArray()));
    }

    private void GenerateRandomMediumCuts(LevelDesignData level, int seed, float L)
    {
        // 随机中等难度切割
        Random.InitState(seed * 1000 + 31); // 加上31确保与简单关卡不同

        int patternType = seed % 8;
        level.description = $"中等图案 #{seed}";

        switch (patternType)
        {
            case 0:
                // 网格加对角线
                GenerateGridWithDiagonals(level, L, 3 + seed % 3);
                break;

            case 1:
                // 嵌套矩形
                GenerateNestedRectangles(level, L, 3 + seed % 3);
                break;

            case 2:
                // 星形网格
                GenerateStarGrid(level, L, 4 + seed % 4);
                break;

            case 3:
                // 波浪网格
                GenerateWaveGrid(level, L, 3 + seed % 3);
                break;

            case 4:
                // 交叉阴影
                GenerateCrossHatch(level, L, 4 + seed % 4);
                break;

            case 5:
                // 几何花边
                GenerateGeometricLace(level, L);
                break;

            case 6:
                // 链式连接
                GenerateChainLinks(level, L, 3 + seed % 3);
                break;

            case 7:
                // 模块化图案
                GenerateModularPattern(level, L, 2 + seed % 3);
                break;
        }
    }

    // 中等难度辅助函数

    private void GenerateGridWithDiagonals(LevelDesignData level, float L, int divisions)
    {
        // 网格加对角线
        float cellSize = L * 2 / divisions;

        // 垂直和水平线
        for (int i = 1; i < divisions; i++)
        {
            float x = -L + i * cellSize;
            level.cutPaths.Add(CreateCutPath(
                new Vector2(x, L),
                new Vector2(x, -L)
            ));

            float y = L - i * cellSize;
            level.cutPaths.Add(CreateCutPath(
                new Vector2(-L, y),
                new Vector2(L, y)
            ));
        }

        // 对角线
        for (int i = 0; i < divisions - 1; i++)
        {
            for (int j = 0; j < divisions - 1; j++)
            {
                if ((i + j) % 2 == 0)
                {
                    // 左上到右下
                    Vector2 start = new Vector2(
                        -L + i * cellSize,
                        L - j * cellSize
                    );
                    Vector2 end = new Vector2(
                        -L + (i + 1) * cellSize,
                        L - (j + 1) * cellSize
                    );
                    level.cutPaths.Add(CreateCutPath(start, end));
                }
                else
                {
                    // 右上到左下
                    Vector2 start = new Vector2(
                        -L + (i + 1) * cellSize,
                        L - j * cellSize
                    );
                    Vector2 end = new Vector2(
                        -L + i * cellSize,
                        L - (j + 1) * cellSize
                    );
                    level.cutPaths.Add(CreateCutPath(start, end));
                }
            }
        }
    }

    private void GenerateNestedRectangles(LevelDesignData level, float L, int layers)
    {
        // 嵌套矩形
        float spacing = L * 0.8f / layers;

        for (int i = 0; i < layers; i++)
        {
            float size = L * 0.8f - i * spacing;

            // 矩形四条边
            level.cutPaths.Add(CreateCutPath(
                new Vector2(-size, size),
                new Vector2(size, size)
            ));

            level.cutPaths.Add(CreateCutPath(
                new Vector2(size, size),
                new Vector2(size, -size)
            ));

            level.cutPaths.Add(CreateCutPath(
                new Vector2(size, -size),
                new Vector2(-size, -size)
            ));

            level.cutPaths.Add(CreateCutPath(
                new Vector2(-size, -size),
                new Vector2(-size, size)
            ));

            // 对角线连接
            if (i > 0 && i < layers - 1)
            {
                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-size, size),
                    new Vector2(-(size + spacing), size + spacing)
                ));

                level.cutPaths.Add(CreateCutPath(
                    new Vector2(size, size),
                    new Vector2(size + spacing, size + spacing)
                ));

                level.cutPaths.Add(CreateCutPath(
                    new Vector2(size, -size),
                    new Vector2(size + spacing, -(size + spacing))
                ));

                level.cutPaths.Add(CreateCutPath(
                    new Vector2(-size, -size),
                    new Vector2(-(size + spacing), -(size + spacing))
                ));
            }
        }
    }

    private void GenerateStarGrid(LevelDesignData level, float L, int points)
    {
        // 星形网格
        Vector2 center = Vector2.zero;
        float outerRadius = L * 0.8f;
        float innerRadius = L * 0.4f;

        // 绘制星形
        List<Vector2> star = new List<Vector2>();
        for (int i = 0; i <= points * 2; i++)
        {
            float angle = i * Mathf.PI / points;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;

            star.Add(center + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            ));
        }
        level.cutPaths.Add(CreateCutPath(star.ToArray()));

        // 从中心到星形顶点的连线
        for (int i = 0; i < points * 2; i += 2)
        {
            float angle = i * Mathf.PI / points;
            level.cutPaths.Add(CreateCutPath(
                center,
                center + new Vector2(
                    Mathf.Cos(angle) * outerRadius,
                    Mathf.Sin(angle) * outerRadius
                )
            ));
        }

        // 星形内部连接
        for (int i = 1; i < points * 2; i += 2)
        {
            float angle1 = i * Mathf.PI / points;
            float angle2 = ((i + 2) % (points * 2)) * Mathf.PI / points;

            level.cutPaths.Add(CreateCutPath(
                center + new Vector2(Mathf.Cos(angle1) * innerRadius, Mathf.Sin(angle1) * innerRadius),
                center + new Vector2(Mathf.Cos(angle2) * innerRadius, Mathf.Sin(angle2) * innerRadius)
            ));
        }
    }

    private void GenerateWaveGrid(LevelDesignData level, float L, int waves)
    {
        // 波浪网格
        int verticalWaves = waves;
        int horizontalWaves = waves;
        float amplitude = L * 0.15f;

        // 垂直线（波浪）
        for (int i = 0; i <= verticalWaves; i++)
        {
            float xPos = -L + i * (L * 2 / verticalWaves);
            List<Vector2> waveLine = new List<Vector2>();

            for (int j = 0; j <= 20; j++)
            {
                float t = j / 20f;
                float y = -L + t * L * 2;
                float x = xPos + Mathf.Sin(t * Mathf.PI * horizontalWaves) * amplitude;

                waveLine.Add(new Vector2(x, y));
            }
            level.cutPaths.Add(CreateCutPath(waveLine.ToArray()));
        }

        // 水平线（波浪）
        for (int i = 0; i <= horizontalWaves; i++)
        {
            float yPos = -L + i * (L * 2 / horizontalWaves);
            List<Vector2> waveLine = new List<Vector2>();

            for (int j = 0; j <= 20; j++)
            {
                float t = j / 20f;
                float x = -L + t * L * 2;
                float y = yPos + Mathf.Sin(t * Mathf.PI * verticalWaves) * amplitude;

                waveLine.Add(new Vector2(x, y));
            }
            level.cutPaths.Add(CreateCutPath(waveLine.ToArray()));
        }
    }

    private void GenerateCrossHatch(LevelDesignData level, float L, int density)
    {
        // 交叉阴影线
        float spacing = L * 2 / density;

        // 左上到右下的线
        for (int i = -density; i <= density; i++)
        {
            float offset = i * spacing * 0.5f;
            List<Vector2> diagonal1 = new List<Vector2>();

            // 计算线与边界的交点
            if (offset > -L && offset < L)
            {
                diagonal1.Add(new Vector2(-L, -L + offset));
                diagonal1.Add(new Vector2(L - offset, L));
            }
            else if (offset <= -L)
            {
                diagonal1.Add(new Vector2(-L - offset, -L));
                diagonal1.Add(new Vector2(L, L + offset));
            }

            if (diagonal1.Count >= 2)
                level.cutPaths.Add(CreateCutPath(diagonal1.ToArray()));
        }

        // 右上到左下的线
        for (int i = -density; i <= density; i++)
        {
            float offset = i * spacing * 0.5f;
            List<Vector2> diagonal2 = new List<Vector2>();

            // 计算线与边界的交点
            if (offset > -L && offset < L)
            {
                diagonal2.Add(new Vector2(L, -L + offset));
                diagonal2.Add(new Vector2(-L + offset, L));
            }
            else if (offset <= -L)
            {
                diagonal2.Add(new Vector2(L + offset, -L));
                diagonal2.Add(new Vector2(-L, L + offset));
            }

            if (diagonal2.Count >= 2)
                level.cutPaths.Add(CreateCutPath(diagonal2.ToArray()));
        }
    }

    private void GenerateGeometricLace(LevelDesignData level, float L)
    {
        // 几何花边图案
        float cellSize = L * 0.25f;
        int cells = Mathf.FloorToInt(L * 2 / cellSize);

        for (int row = 0; row < cells; row++)
        {
            for (int col = 0; col < cells; col++)
            {
                float x = -L + col * cellSize + cellSize * 0.5f;
                float y = L - row * cellSize - cellSize * 0.5f;

                // 跳过靠近边界的单元格
                if (Mathf.Abs(x) > L * 0.85f || Mathf.Abs(y) > L * 0.85f)
                    continue;

                // 创建花边图案
                if ((row + col) % 2 == 0)
                {
                    // 圆形花边
                    List<Vector2> circle = new List<Vector2>();
                    float radius = cellSize * 0.4f;
                    int segments = 8;

                    for (int i = 0; i <= segments; i++)
                    {
                        float angle = i * Mathf.PI * 2 / segments;
                        circle.Add(new Vector2(
                            x + Mathf.Cos(angle) * radius,
                            y + Mathf.Sin(angle) * radius
                        ));
                    }
                    level.cutPaths.Add(CreateCutPath(circle.ToArray()));

                    // 连接到相邻单元格
                    if (col > 0)
                    {
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x - radius, y),
                            new Vector2(x - cellSize + radius, y)
                        ));
                    }

                    if (row > 0)
                    {
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x, y + radius),
                            new Vector2(x, y + cellSize - radius)
                        ));
                    }
                }
                else
                {
                    // 方形花边
                    float size = cellSize * 0.3f;
                    List<Vector2> square = new List<Vector2>();
                    square.Add(new Vector2(x - size, y - size));
                    square.Add(new Vector2(x - size, y + size));
                    square.Add(new Vector2(x + size, y + size));
                    square.Add(new Vector2(x + size, y - size));
                    square.Add(new Vector2(x - size, y - size));
                    level.cutPaths.Add(CreateCutPath(square.ToArray()));

                    // 对角线连接
                    level.cutPaths.Add(CreateCutPath(
                        new Vector2(x - size, y - size),
                        new Vector2(x + size, y + size)
                    ));
                    level.cutPaths.Add(CreateCutPath(
                        new Vector2(x - size, y + size),
                        new Vector2(x + size, y - size)
                    ));
                }
            }
        }
    }

    private void GenerateChainLinks(LevelDesignData level, float L, int links)
    {
        // 链式连接
        float linkRadius = L * 0.8f / (links * 2);
        float verticalSpacing = linkRadius * 3;

        for (int row = 0; row < links; row++)
        {
            for (int col = 0; col < links; col++)
            {
                float x = -L + (col * 2 + 1 + (row % 2) * 0.5f) * linkRadius * 1.5f;
                float y = L - (row + 0.5f) * verticalSpacing;

                // 检查是否在边界内
                if (Mathf.Abs(x) > L * 0.9f || Mathf.Abs(y) > L * 0.9f)
                    continue;

                // 绘制链环
                List<Vector2> link = new List<Vector2>();
                int segments = 16;

                // 外圆
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    link.Add(new Vector2(
                        x + Mathf.Cos(angle) * linkRadius,
                        y + Mathf.Sin(angle) * linkRadius
                    ));
                }

                // 内圆（形成环）
                for (int i = segments; i >= 0; i--)
                {
                    float angle = i * Mathf.PI * 2 / segments;
                    link.Add(new Vector2(
                        x + Mathf.Cos(angle) * (linkRadius * 0.6f),
                        y + Mathf.Sin(angle) * (linkRadius * 0.6f)
                    ));
                }

                level.cutPaths.Add(CreateCutPath(link.ToArray()));

                // 连接相邻链环
                if (col > 0 && row % 2 == 0)
                {
                    level.cutPaths.Add(CreateCutPath(
                        new Vector2(x - linkRadius, y),
                        new Vector2(x - linkRadius * 1.5f, y)
                    ));
                }

                if (row > 0)
                {
                    level.cutPaths.Add(CreateCutPath(
                        new Vector2(x, y + linkRadius),
                        new Vector2(x, y + verticalSpacing - linkRadius)
                    ));
                }
            }
        }
    }

    private void GenerateModularPattern(LevelDesignData level, float L, int modules)
    {
        // 模块化图案
        float moduleSize = L * 1.8f / modules;
        Random.InitState(level.levelNumber);

        for (int row = 0; row < modules; row++)
        {
            for (int col = 0; col < modules; col++)
            {
                float x = -L + col * moduleSize + moduleSize * 0.5f;
                float y = L - row * moduleSize - moduleSize * 0.5f;

                // 选择模块类型
                int moduleType = Random.Range(0, 4);

                switch (moduleType)
                {
                    case 0: // X形
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x - moduleSize * 0.3f, y - moduleSize * 0.3f),
                            new Vector2(x + moduleSize * 0.3f, y + moduleSize * 0.3f)
                        ));
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x - moduleSize * 0.3f, y + moduleSize * 0.3f),
                            new Vector2(x + moduleSize * 0.3f, y - moduleSize * 0.3f)
                        ));
                        break;

                    case 1: // 小圆
                        List<Vector2> circle = new List<Vector2>();
                        float radius = moduleSize * 0.2f;
                        int segments = 8;

                        for (int i = 0; i <= segments; i++)
                        {
                            float angle = i * Mathf.PI * 2 / segments;
                            circle.Add(new Vector2(
                                x + Mathf.Cos(angle) * radius,
                                y + Mathf.Sin(angle) * radius
                            ));
                        }
                        level.cutPaths.Add(CreateCutPath(circle.ToArray()));
                        break;

                    case 2: // 正方形
                        float squareSize = moduleSize * 0.25f;
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x - squareSize, y - squareSize),
                            new Vector2(x - squareSize, y + squareSize)
                        ));
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x - squareSize, y + squareSize),
                            new Vector2(x + squareSize, y + squareSize)
                        ));
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x + squareSize, y + squareSize),
                            new Vector2(x + squareSize, y - squareSize)
                        ));
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x + squareSize, y - squareSize),
                            new Vector2(x - squareSize, y - squareSize)
                        ));
                        break;

                    case 3: // 十字
                        float crossSize = moduleSize * 0.2f;
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x - crossSize, y),
                            new Vector2(x + crossSize, y)
                        ));
                        level.cutPaths.Add(CreateCutPath(
                            new Vector2(x, y - crossSize),
                            new Vector2(x, y + crossSize)
                        ));
                        break;
                }
            }
        }
    }


    private void GenerateAbstractArtCuts(LevelDesignData level, float L)
    {
        // 抽象艺术图案
        Random.InitState(level.levelNumber * 10);

        // 随机曲线
        for (int curve = 0; curve < 3; curve++)
        {
            List<Vector2> abstractCurve = new List<Vector2>();
            int segments = 15;

            Vector2 start = new Vector2(
                Random.Range(-L * 0.8f, L * 0.8f),
                Random.Range(-L * 0.8f, L * 0.8f)
            );
            abstractCurve.Add(start);

            Vector2 currentPos = start;
            for (int i = 1; i <= segments; i++)
            {
                float angle = Random.Range(0, Mathf.PI * 2);
                float length = Random.Range(L * 0.1f, L * 0.3f);

                currentPos += new Vector2(
                    Mathf.Cos(angle) * length,
                    Mathf.Sin(angle) * length
                );

                // 确保在边界内
                currentPos.x = Mathf.Clamp(currentPos.x, -L * 0.9f, L * 0.9f);
                currentPos.y = Mathf.Clamp(currentPos.y, -L * 0.9f, L * 0.9f);

                abstractCurve.Add(currentPos);
            }

            level.cutPaths.Add(CreateCutPath(abstractCurve.ToArray()));
        }

        // 随机几何形状
        for (int shape = 0; shape < 4; shape++)
        {
            Vector2 center = new Vector2(
                Random.Range(-L * 0.6f, L * 0.6f),
                Random.Range(-L * 0.6f, L * 0.6f)
            );

            List<Vector2> randomShape = new List<Vector2>();
            int sides = Random.Range(3, 8);
            float size = Random.Range(L * 0.05f, L * 0.2f);

            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                float randomOffset = (Random.value - 0.5f) * size * 0.3f;

                randomShape.Add(center + new Vector2(
                    Mathf.Cos(angle) * (size + randomOffset),
                    Mathf.Sin(angle) * (size + randomOffset)
                ));
            }

            level.cutPaths.Add(CreateCutPath(randomShape.ToArray()));
        }
    }

    #endregion


    #region KIMI

    private LevelDesignData GeneratePuzzleLevelUniversal(int levelNum)
    {
        LevelDesignData level = new LevelDesignData();
        level.levelNumber = levelNum;
        level.gridSize = Mathf.Clamp(4 + levelNum / 10, 4, 8);
        level.cutPaths = GeneratePuzzleLevel(
            levelNum,
            CutterManager.cutterLength + AddLength,
            level.gridSize,
            out _);
        level.difficulty = Mathf.Clamp(1 + levelNum * 0.08f, 1f, 5f);
        level.description = $"拼图 #{levelNum}";
        return level;
    }

    public static List<LevelDesignData.CutPath> GeneratePuzzleLevel(
        int level,                      // 1-66
        float halfLength,               // CutterManager.cutterLength + AddLength
        int gridSize,
        out List<Vector2> pieceSeeds)   // 输出种子，方便以后验证拼图
    {
        pieceSeeds = new List<Vector2>();

        /* ---------- 1. 随关卡递增的参数 ---------- */
        int pieceCount = Mathf.Clamp(4 + level * 3 / 10, 4, 25);
        int mortiseCount = 1 + level / 15;               // 每边榫卯个数
        float mortiseDepth = halfLength * 0.08f;
        float mortiseWidth = halfLength * 0.12f;

        /* ---------- 2. 生成种子点 ---------- */
        Random.InitState(level * 1000 + 1);
        for (int i = 0; i < pieceCount; i++)
        {
            Vector2 p;
            if (level <= 10)
            {   // 均匀网格 + 微扰
                int c = Mathf.FloorToInt(Mathf.Sqrt(pieceCount));
                p = new Vector2(
                    (i % c - (c - 1) * 0.5f) * halfLength * 1.6f / c,
                    (i / c - (c - 1) * 0.5f) * halfLength * 1.6f / c);
                p += Random.insideUnitCircle * halfLength * 0.05f;
            }
            else if (level <= 30)
            {   // 矩形随机
                p = Random.insideUnitCircle * halfLength * 0.9f;
            }
            else
            {   // 密度图：中心更密
                float angle = Random.Range(0, Mathf.PI * 2);
                float r = Mathf.Pow(Random.Range(0f, 1f), 1.4f) * halfLength * 0.8f;
                p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }
            pieceSeeds.Add(p);
        }

        /* ---------- 3. 暴力 Voronoi ---------- */
        var voronoi = new VoronoiDiagram(pieceSeeds, halfLength);

        /* ---------- 4. 加榫卯 ---------- */
        var cuts = new List<LevelDesignData.CutPath>();
        foreach (var edge in voronoi.Edges)
        {
            var shaped = MakeMortiseEdge(edge, mortiseCount, mortiseDepth, mortiseWidth, level);
            cuts.Add(new LevelDesignData.CutPath { points = shaped });
        }
        return cuts;
    }

    /* =================================================================== */
    /* =========================== 私有工具 ================================= */
    /* =================================================================== */
    private static List<Vector2> MakeMortiseEdge(VoronoiEdge e,
        int count, float depth, float width, int seed)
    {
        Random.InitState(seed + e.GetHashCode());   // 让同一条边每次一致
        List<Vector2> pts = new List<Vector2>();
        Vector2 dir = (e.B - e.A).normalized;
        Vector2 n = new Vector2(-dir.y, dir.x);
        float segLen = Vector2.Distance(e.A, e.B) / (count * 2 + 1);

        pts.Add(e.A);
        for (int i = 1; i <= count * 2; i++)
        {
            Vector2 p = Vector2.Lerp(e.A, e.B, i / (float)(count * 2 + 1));
            if (i % 2 == 1)   // 奇数位置做凹凸
            {
                float side = (i & 2) == 0 ? 1 : -1;          // 交替方向
                p += n * (side * depth);
                p += dir * Random.Range(-width, width) * 0.3f;
            }
            pts.Add(p);
        }
        pts.Add(e.B);
        return pts;
    }

    /* 极简 Voronoi：只保留中垂线并裁剪到边界 */
    private class VoronoiDiagram
    {
        public List<VoronoiEdge> Edges = new List<VoronoiEdge>();
        public VoronoiDiagram(List<Vector2> seeds, float bounds)
        {
            for (int i = 0; i < seeds.Count; i++)
                for (int j = i + 1; j < seeds.Count; j++)
                {
                    Vector2 mid = (seeds[i] + seeds[j]) * 0.5f;
                    Vector2 dir = (seeds[j] - seeds[i]).normalized;
                    Vector2 n = new Vector2(-dir.y, dir.x);
                    Vector2 a = mid - n * bounds * 2;
                    Vector2 b = mid + n * bounds * 2;
                    Edges.Add(new VoronoiEdge { A = a, B = b });
                }
        }
    }
    private struct VoronoiEdge
    {
        public Vector2 A, B;
    }

    #endregion


    #region  DOUBAO
    public void DouBao_GenerateAllLevels()
    {
        allLevels.Clear();

        // 关卡1-20：简单拼图切割（规则形状）
        for (int i = 1; i <= 20; i++)
        {
            allLevels.Add(DouBao_GenerateSimpleLevel(i));
        }

        // 关卡21-40：中等拼图切割（不规则形状）
        for (int i = 21; i <= 40; i++)
        {
            allLevels.Add(DouBao_GenerateMediumLevel(i));
        }

        // 关卡41-66：复杂拼图切割（嵌套/异形）
        for (int i = 41; i <= 66; i++)
        {
            allLevels.Add(DouBao_GenerateComplexLevel(i));
        }

        Debug.Log($"已生成 {allLevels.Count} 个拼图关卡");
    }

    // ==================== 简单难度：规则拼图切割（1-20关） ====================
    private LevelDesignData DouBao_GenerateSimpleLevel(int levelNum)
    {
        LevelDesignData level = new LevelDesignData();
        level.levelNumber = levelNum;
        level.gridSize = 4;
        level.cutPaths = new List<LevelDesignData.CutPath>();
        level.difficulty = Mathf.Clamp(levelNum * 0.2f, 1f, 5f);

        float L = CutterManager.cutterLength + AddLength;

        switch (levelNum)
        {
            case 1: // 基础四等分（直边）
                level.description = "四等分直边拼图";
                // 水平中线
                level.cutPaths.Add(DouBao_CreateCutPath(new Vector2(-L, 0), new Vector2(L, 0)));
                // 垂直中线
                level.cutPaths.Add(DouBao_CreateCutPath(new Vector2(0, L), new Vector2(0, -L)));
                break;

            case 2: // 十字咬合（基础榫卯）
                level.description = "十字基础咬合拼图";
                DouBao_GenerateCrossInterlockCut(level, L, 0.15f);
                break;

            case 3: // 2x2拼图（单边咬合）
                level.description = "2x2单边咬合拼图";
                DouBao_Generate2x2SingleInterlock(level, L);
                break;

            case 4: // 田字格（双边简单咬合）
                level.description = "田字格双边咬合";
                DouBao_GenerateFieldGridInterlock(level, L);
                break;

            case 5: // L形拼图（直角咬合）
                level.description = "L形直角咬合拼图";
                DouBao_GenerateLShapedInterlock(level, L);
                break;

            case 6: // 对角线切割（斜向直边）
                level.description = "对角线直边拼图";
                DouBao_GenerateDiagonalStraightCut(level, L);
                break;

            case 7: // 九宫格（规则小拼图）
                level.description = "九宫格规则拼图";
                DouBao_GenerateNineGridSimple(level, L);
                break;

            case 8: // 十字偏移咬合
                level.description = "偏移十字咬合拼图";
                DouBao_GenerateOffsetCrossInterlock(level, L);
                break;

            case 9: // 阶梯状咬合（多级直边）
                level.description = "阶梯状直边咬合";
                DouBao_GenerateStepInterlock(level, L);
                break;

            case 10: // 圆形边缘拼图
                level.description = "圆形边缘基础拼图";
                DouBao_GenerateCircleEdgeSimple(level, L);
                break;

            case 11: // 波浪边基础（单方向）
                level.description = "单向波浪边拼图";
                DouBao_GenerateSingleWaveEdge(level, L);
                break;

            case 12: // 嵌套矩形（同心咬合）
                level.description = "嵌套矩形咬合";
                DouBao_GenerateNestedRectangle(level, L);
                break;

            case 13: // 三角分割（等边咬合）
                level.description = "等边三角形拼图";
                DouBao_GenerateTriangleInterlock(level, L);
                break;

            case 14: // 窗户格（四格对称咬合）
                level.description = "窗户格对称咬合";
                DouBao_GenerateWindowGridInterlock(level, L);
                break;

            case 15: // 放射状基础（6等分）
                level.description = "6等分放射状拼图";
                DouBao_GenerateRadialSixSlice(level, L);
                break;

            case 16: // H形拼图（中间连接咬合）
                level.description = "H形中间连接咬合";
                DouBao_GenerateHShapedInterlock(level, L);
                break;

            case 17: // 三角形组合（3片拼图）
                level.description = "三片三角形组合拼图";
                DouBao_GenerateTriangleCombination(level, L);
                break;

            case 18: // 平行四边形（斜向咬合）
                level.description = "平行四边形斜向咬合";
                DouBao_GenerateParallelogramInterlock(level, L);
                break;

            case 19: // 工字形（两端咬合）
                level.description = "工字形两端咬合拼图";
                DouBao_GenerateIShapedInterlock(level, L);
                break;

            case 20: // 简单迷宫式（路径咬合）
                level.description = "简单路径咬合拼图";
                DouBao_GenerateSimplePathInterlock(level, L);
                break;

            default:
                // 随机简单拼图切割
                level.description = $"简单拼图 #{levelNum}";
                DouBao_GenerateRandomSimplePuzzle(level, levelNum, L);
                break;
        }

        return level;
    }

    // ==================== 中等难度：不规则拼图切割（21-40关） ====================
    private LevelDesignData DouBao_GenerateMediumLevel(int levelNum)
    {
        LevelDesignData level = new LevelDesignData();
        level.levelNumber = levelNum;
        level.gridSize = 6;
        level.cutPaths = new List<LevelDesignData.CutPath>();
        level.difficulty = Mathf.Clamp(2 + (levelNum - 21) * 0.15f, 2f, 4f);

        float L = CutterManager.cutterLength + AddLength;

        switch (levelNum)
        {
            case 21: // 窗格复杂咬合（多方向）
                level.description = "窗格多向复杂咬合";
                DouBao_GenerateWindowGridComplex(level, L);
                break;

            case 22: // 迷宫式拼图（多路径咬合）
                level.description = "多路径迷宫咬合拼图";
                DouBao_GenerateMazePathInterlock(level, L);
                break;

            case 23: // 螺旋切割（曲线咬合）
                level.description = "螺旋曲线咬合拼图";
                DouBao_GenerateSpiralCurveInterlock(level, L);
                break;

            case 24: // 锯齿形咬合（不规则波浪）
                level.description = "不规则锯齿边咬合";
                DouBao_GenerateJaggedEdgeInterlock(level, L);
                break;

            case 25: // 星形拼图（多角咬合）
                level.description = "四角星多角咬合";
                DouBao_GenerateStarShapeInterlock(level, L);
                break;

            case 26: // T字形（不对称咬合）
                level.description = "T字形不对称咬合";
                DouBao_GenerateTShapeAsymmetric(level, L);
                break;

            case 27: // L形复杂（多边咬合）
                level.description = "L形多边复杂咬合";
                DouBao_GenerateLShapedComplex(level, L);
                break;

            case 28: // H形复杂（全边咬合）
                level.description = "H形全边复杂咬合";
                DouBao_GenerateHShapedComplex(level, L);
                break;

            case 29: // 风车拼图（旋转咬合）
                level.description = "风车旋转咬合拼图";
                DouBao_GenerateWindmillInterlock(level, L);
                break;

            case 30: // 拼图全咬合（2x2满咬合）
                level.description = "2x2全边咬合拼图";
                DouBao_Generate2x2FullInterlock(level, L);
                break;

            case 31: // 波浪双边咬合（正交方向）
                level.description = "正交波浪双边咬合";
                DouBao_GenerateDualWaveInterlock(level, L);
                break;

            case 32: // 圆形拼图（多片弧形）
                level.description = "多片弧形圆形拼图";
                DouBao_GenerateMultiSliceCircle(level, L);
                break;

            case 33: // 异形四边形（不规则四边）
                level.description = "异形四边形拼图";
                DouBao_GenerateIrregularQuad(level, L);
                break;

            case 34: // 花瓣形拼图（8瓣）
                level.description = "8瓣花瓣形拼图";
                DouBao_GeneratePetalShape8(level, L);
                break;

            case 35: // 交叉斜线咬合
                level.description = "交叉斜线不规则咬合";
                DouBao_GenerateCrossSlantInterlock(level, L);
                break;

            case 36: // 模块化拼图（4种基础模块）
                level.description = "4模块组合拼图";
                DouBao_GenerateModular4Type(level, L);
                break;

            case 37: // 齿轮状咬合（凹凸齿）
                level.description = "齿轮状凹凸咬合";
                DouBao_GenerateGearInterlock(level, L);
                break;

            case 38: // 菱形拼图（斜向波浪）
                level.description = "菱形斜向波浪咬合";
                DouBao_GenerateDiamondWaveInterlock(level, L);
                break;

            case 39: // 阶梯波浪（混合咬合）
                level.description = "阶梯波浪混合咬合";
                DouBao_GenerateStepWaveHybrid(level, L);
                break;

            case 40: // 随机中等拼图（混合规则）
                level.description = "中等随机混合拼图";
                DouBao_GenerateRandomMediumPuzzle(level, levelNum, L);
                break;

            default:
                level.description = $"中等拼图 #{levelNum}";
                DouBao_GenerateRandomMediumPuzzle(level, levelNum, L);
                break;
        }

        return level;
    }

    // ==================== 复杂难度：异形/嵌套拼图切割（41-66关） ====================
    private LevelDesignData DouBao_GenerateComplexLevel(int levelNum)
    {
        LevelDesignData level = new LevelDesignData();
        level.levelNumber = levelNum;
        level.gridSize = 8;
        level.cutPaths = new List<LevelDesignData.CutPath>();
        level.difficulty = Mathf.Clamp(3 + (levelNum - 41) * 0.1f, 3f, 5f);

        float L = CutterManager.cutterLength + AddLength;

        switch (levelNum)
        {
            case 41: // 复杂迷宫拼图（多路径嵌套）
                level.description = "复杂迷宫嵌套拼图";
                DouBao_GenerateComplexMazePuzzle(level, L);
                break;

            
            case 42: // 雪花形拼图（分形咬合）
                level.description = "雪花形分形咬合拼图";
                DouBao_GenerateSnowflakeFractalPuzzle(level, L);
                break;

            case 43: // 中国结拼图（缠绕咬合）
                level.description = "中国结缠绕咬合拼图";
                DouBao_GenerateChineseKnotPuzzle(level, L);
                break;

            case 44: // 太极图拼图（曲线嵌套）
                level.description = "太极图曲线嵌套拼图";
                DouBao_GenerateTaijiCurvePuzzle(level, L);
                break;

            case 45: // 螺旋星系（多螺旋咬合）
                level.description = "螺旋星系多曲线咬合";
                DouBao_GenerateSpiralGalaxyPuzzle(level, L);
                break;

            case 46: // 城市天际线（异形边缘）
                level.description = "天际线异形边缘拼图";
                DouBao_GenerateSkylineShapePuzzle(level, L);
                break;

            case 47: // 电路板（精密路径咬合）
                level.description = "精密路径咬合拼图";
                DouBao_GenerateCircuitBoardPuzzle(level, L);
                break;

            case 48: // 树形分叉（自然生长咬合）
                level.description = "树形自然咬合拼图";
                DouBao_GenerateTreeBranchPuzzle(level, L);
                break;

            case 49: // 多向波浪（三维感）
                level.description = "多向波浪立体拼图";
                DouBao_GenerateMultiDirectionWave(level, L);
                break;

            case 50: // 蜂巢拼图（六边形咬合）
                level.description = "蜂巢六边形咬合拼图";
                DouBao_GenerateHoneycombPuzzle(level, L);
                break;

            case 51: // 不规则多边形组合
                level.description = "不规则多边形组合拼图";
                DouBao_GenerateIrregularPolygonCombo(level, L);
                break;

            case 52: // 嵌套螺旋（多层咬合）
                level.description = "嵌套螺旋多层咬合";
                DouBao_GenerateNestedSpiralPuzzle(level, L);
                break;

            case 53: // 齿轮组拼图（多齿轮啮合）
                level.description = "多齿轮啮合拼图";
                DouBao_GenerateGearSetPuzzle(level, L);
                break;

            case 54: // 莫比乌斯环（单侧连续咬合）
                level.description = "莫比乌斯环连续咬合";
                DouBao_GenerateMobiusPuzzle(level, L);
                break;

            case 55: // 水滴形拼图（流线型咬合）
                level.description = "水滴形流线咬合拼图";
                DouBao_GenerateWaterDropPuzzle(level, L);
                break;

            case 56: // 星座式拼图（点连接咬合）
                level.description = "星座式点连接拼图";
                DouBao_GenerateConstellationPuzzle(level, L);
                break;

            case 57: // 随机曲线拼图（无规则）
                level.description = "随机曲线无规则拼图";
                DouBao_GenerateRandomCurvePuzzle(level, L);
                break;
                /*

            case 58: // 文字轮廓拼图（汉字基础）
                level.description = "汉字轮廓基础拼图";
                DouBao_GenerateCharacterOutlinePuzzle(level, L);
                break;

            case 59: // 对称异形（镜像咬合）
                level.description = "镜像对称异形咬合";
                DouBao_GenerateMirrorAsymmetricPuzzle(level, L);
                break;

            case 60: // 多层嵌套（3层咬合）
                level.description = "三层嵌套复杂咬合";
                DouBao_GenerateThreeLayerNestedPuzzle(level, L);
                break;

            case 61: // 爆炸形拼图（放射状异形）
                level.description = "爆炸形放射异形拼图";
                DouBao_GenerateExplosionShapePuzzle(level, L);
                break;

            case 62: // 生物细胞（有机形状）
                level.description = "细胞状有机拼图";
                DouBao_GenerateCellShapePuzzle(level, L);
                break;

            case 63: // 折纸式拼图（折痕咬合）
                level.description = "折纸折痕咬合拼图";
                DouBao_GenerateOrigamiPuzzle(level, L);
                break;

            case 64: // 马赛克拼图（小碎片组合）
                level.description = "马赛克小碎片拼图";
                DouBao_GenerateMosaicPuzzle(level, L);
                break;

            case 65: // 渐变难度（混合所有简单/中等）
                level.description = "渐变混合难度拼图";
                DouBao_GenerateGradualHybridPuzzle(level, L);
                break;

            case 66: // 最终关卡：超复杂异形组合
                level.description = "终极挑战：异形组合拼图";
                level.difficulty = 5f;
                DouBao_GenerateFinalBossPuzzle(level, L);
                break;
                */

            default:
                level.description = $"复杂拼图 #{levelNum}";
                DouBao_GenerateRandomComplexPuzzle(level, levelNum, L);
                break;
        }

        return level;
    }

    // ==================== 核心工具函数 ====================
    // 完全适配你的 LevelDesignData.CutPath 类结构
    private LevelDesignData.CutPath DouBao_CreateCutPath(params Vector2[] points)
    {
        LevelDesignData.CutPath cutPath = new LevelDesignData.CutPath();
        cutPath.points = new List<Vector2>(points);
        
        // 自动闭合路径（拼图必备）
        if (cutPath.points.Count > 0 && cutPath.points[0] != cutPath.points[^1])
        {
           // cutPath.points.Add(cutPath.points[0]);
        }
        return cutPath;
    }

    // ==================== 简单难度拼图生成具体实现 ====================
    private void DouBao_GenerateCrossInterlockCut(LevelDesignData level, float L, float depth)
    {
        // 十字基础咬合
        float notchDepth = L * depth;
        float notchWidth = notchDepth * 0.8f;

        // 水平中线（带中心咬合）
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, 0));
        horizontal.Add(new Vector2(-notchWidth, 0));
        horizontal.Add(new Vector2(-notchWidth, notchDepth));
        horizontal.Add(new Vector2(notchWidth, notchDepth));
        horizontal.Add(new Vector2(notchWidth, 0));
        horizontal.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));

        // 垂直中线（带中心咬合）
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(new Vector2(0, L));
        vertical.Add(new Vector2(0, notchWidth));
        vertical.Add(new Vector2(-notchDepth, notchWidth));
        vertical.Add(new Vector2(-notchDepth, -notchWidth));
        vertical.Add(new Vector2(0, -notchWidth));
        vertical.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));
    }

    private void DouBao_Generate2x2SingleInterlock(LevelDesignData level, float L)
    {
        // 2x2单边咬合拼图
        float half = L / 2;
        float notch = L * 0.1f;

        // 水平切割线（下边缘咬合）
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, 0));
        horizontal.Add(new Vector2(-half - notch, 0));
        horizontal.Add(new Vector2(-half - notch, -notch));
        horizontal.Add(new Vector2(-half + notch, -notch));
        horizontal.Add(new Vector2(-half + notch, 0));
        horizontal.Add(new Vector2(half - notch, 0));
        horizontal.Add(new Vector2(half - notch, -notch));
        horizontal.Add(new Vector2(half + notch, -notch));
        horizontal.Add(new Vector2(half + notch, 0));
        horizontal.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));

        // 垂直切割线（右边缘咬合）
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(new Vector2(0, L));
        vertical.Add(new Vector2(0, half - notch));
        vertical.Add(new Vector2(notch, half - notch));
        vertical.Add(new Vector2(notch, half + notch));
        vertical.Add(new Vector2(0, half + notch));
        vertical.Add(new Vector2(0, -half + notch));
        vertical.Add(new Vector2(notch, -half + notch));
        vertical.Add(new Vector2(notch, -half - notch));
        vertical.Add(new Vector2(0, -half - notch));
        vertical.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));
    }

    private void DouBao_GenerateFieldGridInterlock(LevelDesignData level, float L)
    {
        // 田字格双边简单咬合
        float quarter = L / 4;
        float notch = L * 0.08f;

        // 上半水平切割
        List<Vector2> h1 = new List<Vector2>();
        h1.Add(new Vector2(-L, quarter));
        h1.Add(new Vector2(-quarter - notch, quarter));
        h1.Add(new Vector2(-quarter - notch, quarter + notch));
        h1.Add(new Vector2(-quarter + notch, quarter + notch));
        h1.Add(new Vector2(-quarter + notch, quarter));
        h1.Add(new Vector2(quarter - notch, quarter));
        h1.Add(new Vector2(quarter - notch, quarter + notch));
        h1.Add(new Vector2(quarter + notch, quarter + notch));
        h1.Add(new Vector2(quarter + notch, quarter));
        h1.Add(new Vector2(L, quarter));
        level.cutPaths.Add(DouBao_CreateCutPath(h1.ToArray()));

        // 下半水平切割
        List<Vector2> h2 = new List<Vector2>();
        h2.Add(new Vector2(-L, -quarter));
        h2.Add(new Vector2(-quarter - notch, -quarter));
        h2.Add(new Vector2(-quarter - notch, -quarter - notch));
        h2.Add(new Vector2(-quarter + notch, -quarter - notch));
        h2.Add(new Vector2(-quarter + notch, -quarter));
        h2.Add(new Vector2(quarter - notch, -quarter));
        h2.Add(new Vector2(quarter - notch, -quarter - notch));
        h2.Add(new Vector2(quarter + notch, -quarter - notch));
        h2.Add(new Vector2(quarter + notch, -quarter));
        h2.Add(new Vector2(L, -quarter));
        level.cutPaths.Add(DouBao_CreateCutPath(h2.ToArray()));

        // 左半垂直切割
        List<Vector2> v1 = new List<Vector2>();
        v1.Add(new Vector2(-quarter, L));
        v1.Add(new Vector2(-quarter, quarter - notch));
        v1.Add(new Vector2(-quarter - notch, quarter - notch));
        v1.Add(new Vector2(-quarter - notch, quarter + notch));
        v1.Add(new Vector2(-quarter, quarter + notch));
        v1.Add(new Vector2(-quarter, -quarter + notch));
        v1.Add(new Vector2(-quarter - notch, -quarter + notch));
        v1.Add(new Vector2(-quarter - notch, -quarter - notch));
        v1.Add(new Vector2(-quarter, -quarter - notch));
        v1.Add(new Vector2(-quarter, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(v1.ToArray()));

        // 右半垂直切割
        List<Vector2> v2 = new List<Vector2>();
        v2.Add(new Vector2(quarter, L));
        v2.Add(new Vector2(quarter, quarter - notch));
        v2.Add(new Vector2(quarter + notch, quarter - notch));
        v2.Add(new Vector2(quarter + notch, quarter + notch));
        v2.Add(new Vector2(quarter, quarter + notch));
        v2.Add(new Vector2(quarter, -quarter + notch));
        v2.Add(new Vector2(quarter + notch, -quarter + notch));
        v2.Add(new Vector2(quarter + notch, -quarter - notch));
        v2.Add(new Vector2(quarter, -quarter - notch));
        v2.Add(new Vector2(quarter, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(v2.ToArray()));
    }

    private void DouBao_GenerateLShapedInterlock(LevelDesignData level, float L)
    {
        // L形直角咬合拼图
        float half = L / 2;
        float notch = L * 0.12f;

        // L形外框
        List<Vector2> lShape = new List<Vector2>();
        lShape.Add(new Vector2(-L, L));
        lShape.Add(new Vector2(-half, L));
        lShape.Add(new Vector2(-half, half + notch));
        lShape.Add(new Vector2(-half + notch, half + notch));
        lShape.Add(new Vector2(-half + notch, half));
        lShape.Add(new Vector2(L, half));
        lShape.Add(new Vector2(L, -L));
        lShape.Add(new Vector2(half, -L));
        lShape.Add(new Vector2(half, -half - notch));
        lShape.Add(new Vector2(half - notch, -half - notch));
        lShape.Add(new Vector2(half - notch, -half));
        lShape.Add(new Vector2(-L, -half));
        lShape.Add(new Vector2(-L, L));
        level.cutPaths.Add(DouBao_CreateCutPath(lShape.ToArray()));

        // 内部直角咬合
        List<Vector2> innerLock = new List<Vector2>();
        innerLock.Add(new Vector2(-half, half));
        innerLock.Add(new Vector2(-half, half - notch));
        innerLock.Add(new Vector2(-half + notch, half - notch));
        innerLock.Add(new Vector2(-half + notch, -half));
        innerLock.Add(new Vector2(-half + notch + notch, -half));
        innerLock.Add(new Vector2(-half + notch + notch, -half + notch));
        innerLock.Add(new Vector2(-half, -half + notch));
        innerLock.Add(new Vector2(-half, half));
        level.cutPaths.Add(DouBao_CreateCutPath(innerLock.ToArray()));
    }

    private void DouBao_GenerateDiagonalStraightCut(LevelDesignData level, float L)
    {
        // 对角线直边拼图
        List<Vector2> diag1 = new List<Vector2>();
        List<Vector2> diag2 = new List<Vector2>();

        // 主对角线
        diag1.Add(new Vector2(-L, L));
        diag1.Add(new Vector2(L, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(diag1.ToArray()));

        // 副对角线（带中点咬合）
        float midNotch = L * 0.1f;
        diag2.Add(new Vector2(-L, -L));
        diag2.Add(new Vector2(-midNotch, -midNotch));
        diag2.Add(new Vector2(-midNotch, midNotch));
        diag2.Add(new Vector2(midNotch, midNotch));
        diag2.Add(new Vector2(midNotch, -midNotch));
        diag2.Add(new Vector2(L, L));
        level.cutPaths.Add(DouBao_CreateCutPath(diag2.ToArray()));
    }

    private void DouBao_GenerateNineGridSimple(LevelDesignData level, float L)
    {
        // 九宫格规则拼图
        float third = L * 2 / 3;
        float notch = L * 0.05f;

        // 水平切割线（三条）
        for (int i = 1; i <= 2; i++)
        {
            float y = L - i * third;
            List<Vector2> hLine = new List<Vector2>();
            hLine.Add(new Vector2(-L, y));

            for (int j = 0; j < 3; j++)
            {
                float x = -L + j * third;
                hLine.Add(new Vector2(x + notch, y));
                hLine.Add(new Vector2(x + notch, y + (i % 2 == 0 ? notch : -notch)));
                hLine.Add(new Vector2(x + third - notch, y + (i % 2 == 0 ? notch : -notch)));
                hLine.Add(new Vector2(x + third - notch, y));
            }

            hLine.Add(new Vector2(L, y));
            level.cutPaths.Add(DouBao_CreateCutPath(hLine.ToArray()));
        }

        // 垂直切割线（三条）
        for (int i = 1; i <= 2; i++)
        {
            float x = -L + i * third;
            List<Vector2> vLine = new List<Vector2>();
            vLine.Add(new Vector2(x, L));

            for (int j = 0; j < 3; j++)
            {
                float y = L - j * third;
                vLine.Add(new Vector2(x, y - notch));
                vLine.Add(new Vector2(x + (i % 2 == 0 ? notch : -notch), y - notch));
                vLine.Add(new Vector2(x + (i % 2 == 0 ? notch : -notch), y - third + notch));
                vLine.Add(new Vector2(x, y - third + notch));
            }

            vLine.Add(new Vector2(x, -L));
            level.cutPaths.Add(DouBao_CreateCutPath(vLine.ToArray()));
        }
    }

    private void DouBao_GenerateOffsetCrossInterlock(LevelDesignData level, float L)
    {
        // 十字偏移咬合
        float offset = L * 0.2f;
        float notch = L * 0.1f;

        // 偏移水平线
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, offset));
        horizontal.Add(new Vector2(-offset - notch, offset));
        horizontal.Add(new Vector2(-offset - notch, offset + notch));
        horizontal.Add(new Vector2(-offset + notch, offset + notch));
        horizontal.Add(new Vector2(-offset + notch, offset));
        horizontal.Add(new Vector2(offset - notch, offset));
        horizontal.Add(new Vector2(offset - notch, offset - notch));
        horizontal.Add(new Vector2(offset + notch, offset - notch));
        horizontal.Add(new Vector2(offset + notch, offset));
        horizontal.Add(new Vector2(L, offset));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));

        // 偏移垂直线
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(new Vector2(-offset, L));
        vertical.Add(new Vector2(-offset, offset - notch));
        vertical.Add(new Vector2(-offset - notch, offset - notch));
        vertical.Add(new Vector2(-offset - notch, offset + notch));
        vertical.Add(new Vector2(-offset, offset + notch));
        vertical.Add(new Vector2(-offset, -offset + notch));
        vertical.Add(new Vector2(-offset + notch, -offset + notch));
        vertical.Add(new Vector2(-offset + notch, -offset - notch));
        vertical.Add(new Vector2(-offset, -offset - notch));
        vertical.Add(new Vector2(-offset, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));
    }

    private void DouBao_GenerateStepInterlock(LevelDesignData level, float L)
    {
        // 阶梯状直边咬合
        float step = L / 4;
        float notch = L * 0.08f;

        List<Vector2> steps = new List<Vector2>();
        steps.Add(new Vector2(-L, L));

        // 阶梯路径
        for (int i = 0; i < 4; i++)
        {
            float x = -L + i * step;
            float y = L - i * step;

            steps.Add(new Vector2(x + step - notch, y));
            steps.Add(new Vector2(x + step - notch, y - notch));
            steps.Add(new Vector2(x + step, y - notch));
            steps.Add(new Vector2(x + step, y - step + notch));
            steps.Add(new Vector2(x + step - notch, y - step + notch));
            steps.Add(new Vector2(x + step - notch, y - step));
        }

        steps.Add(new Vector2(L, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(steps.ToArray()));
    }

    private void DouBao_GenerateCircleEdgeSimple(LevelDesignData level, float L)
    {
        // 圆形边缘基础拼图
        float radius = L * 0.8f;
        int segments = 16;

        // 外圆
        List<Vector2> outerCircle = new List<Vector2>();
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            outerCircle.Add(new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            ));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(outerCircle.ToArray()));

        // 十字分割（带圆弧咬合）
        float notch = L * 0.1f;
        List<Vector2> hLine = new List<Vector2>();
        hLine.Add(new Vector2(-radius, 0));
        hLine.Add(new Vector2(-notch, 0));
        // 圆弧咬合
        for (int i = 0; i <= 8; i++)
        {
            float angle = Mathf.PI / 2 + i * Mathf.PI / 8;
            hLine.Add(new Vector2(
                Mathf.Cos(angle) * notch,
                Mathf.Sin(angle) * notch
            ));
        }
        hLine.Add(new Vector2(notch, 0));
        hLine.Add(new Vector2(radius, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(hLine.ToArray()));

        List<Vector2> vLine = new List<Vector2>();
        vLine.Add(new Vector2(0, radius));
        vLine.Add(new Vector2(0, notch));
        // 圆弧咬合
        for (int i = 0; i <= 8; i++)
        {
            float angle = Mathf.PI + i * Mathf.PI / 8;
            vLine.Add(new Vector2(
                Mathf.Cos(angle) * notch,
                Mathf.Sin(angle) * notch
            ));
        }
        vLine.Add(new Vector2(0, -notch));
        vLine.Add(new Vector2(0, -radius));
        level.cutPaths.Add(DouBao_CreateCutPath(vLine.ToArray()));
    }

    private void DouBao_GenerateSingleWaveEdge(LevelDesignData level, float L)
    {
        // 单向波浪边拼图
        int segments = 12;
        float amplitude = L * 0.15f;

        List<Vector2> wave = new List<Vector2>();
        wave.Add(new Vector2(-L, 0));

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = -L + t * L * 2;
            float y = Mathf.Sin(t * Mathf.PI * 4) * amplitude;

            wave.Add(new Vector2(x, y));
        }

        wave.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(wave.ToArray()));
    }

    private void DouBao_GenerateNestedRectangle(LevelDesignData level, float L)
    {
        // 嵌套矩形咬合
        float spacing = L * 0.2f;
        float notch = L * 0.05f;

        for (int i = 0; i < 3; i++)
        {
            float size = L * 0.8f - i * spacing;
            List<Vector2> rect = new List<Vector2>();

            // 上边
            rect.Add(new Vector2(-size, size));
            rect.Add(new Vector2(-size + notch, size));
            rect.Add(new Vector2(-size + notch, size - notch));
            rect.Add(new Vector2(size - notch, size - notch));
            rect.Add(new Vector2(size - notch, size));
            rect.Add(new Vector2(size, size));

            // 右边
            rect.Add(new Vector2(size, size - notch));
            rect.Add(new Vector2(size - notch, size - notch));
            rect.Add(new Vector2(size - notch, -size + notch));
            rect.Add(new Vector2(size, -size + notch));
            rect.Add(new Vector2(size, -size));

            // 下边
            rect.Add(new Vector2(size - notch, -size));
            rect.Add(new Vector2(size - notch, -size + notch));
            rect.Add(new Vector2(-size + notch, -size + notch));
            rect.Add(new Vector2(-size + notch, -size));
            rect.Add(new Vector2(-size, -size));

            // 左边
            rect.Add(new Vector2(-size, -size + notch));
            rect.Add(new Vector2(-size + notch, -size + notch));
            rect.Add(new Vector2(-size + notch, size - notch));
            rect.Add(new Vector2(-size, size - notch));
            rect.Add(new Vector2(-size, size));

            level.cutPaths.Add(DouBao_CreateCutPath(rect.ToArray()));
        }
    }

    private void DouBao_GenerateTriangleInterlock(LevelDesignData level, float L)
    {
        // 等边三角形拼图
        float height = L * Mathf.Sqrt(3) / 2;
        float notch = L * 0.1f;

        // 上三角
        List<Vector2> topTri = new List<Vector2>();
        topTri.Add(new Vector2(0, height));
        topTri.Add(new Vector2(-L / 2 + notch, height - L / 2));
        topTri.Add(new Vector2(-L / 2 + notch, height - L / 2 + notch));
        topTri.Add(new Vector2(-notch, height - L / 2 + notch));
        topTri.Add(new Vector2(-notch, height - L / 2));
        topTri.Add(new Vector2(notch, height - L / 2));
        topTri.Add(new Vector2(notch, height - L / 2 + notch));
        topTri.Add(new Vector2(L / 2 - notch, height - L / 2 + notch));
        topTri.Add(new Vector2(L / 2 - notch, height - L / 2));
        topTri.Add(new Vector2(L / 2, height - L / 2));
        topTri.Add(new Vector2(0, height));
        level.cutPaths.Add(DouBao_CreateCutPath(topTri.ToArray()));

        // 下两个三角
        List<Vector2> bottomTri1 = new List<Vector2>();
        bottomTri1.Add(new Vector2(-L / 2, height - L / 2));
        bottomTri1.Add(new Vector2(-notch, -height / 2));
        bottomTri1.Add(new Vector2(-notch, -height / 2 + notch));
        bottomTri1.Add(new Vector2(-L / 2 + notch, -height / 2 + notch));
        bottomTri1.Add(new Vector2(-L / 2 + notch, height - L / 2));
        bottomTri1.Add(new Vector2(-L / 2, height - L / 2));
        level.cutPaths.Add(DouBao_CreateCutPath(bottomTri1.ToArray()));

        List<Vector2> bottomTri2 = new List<Vector2>();
        bottomTri2.Add(new Vector2(L / 2, height - L / 2));
        bottomTri2.Add(new Vector2(notch, -height / 2));
        bottomTri2.Add(new Vector2(notch, -height / 2 + notch));
        bottomTri2.Add(new Vector2(L / 2 - notch, -height / 2 + notch));
        bottomTri2.Add(new Vector2(L / 2 - notch, height - L / 2));
        bottomTri2.Add(new Vector2(L / 2, height - L / 2));
        level.cutPaths.Add(DouBao_CreateCutPath(bottomTri2.ToArray()));
    }

    private void DouBao_GenerateWindowGridInterlock(LevelDesignData level, float L)
    {
        // 窗户格对称咬合
        float half = L / 2;
        float notch = L * 0.1f;

        // 垂直中线
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(new Vector2(0, L));
        vertical.Add(new Vector2(0, half + notch));
        vertical.Add(new Vector2(-notch, half + notch));
        vertical.Add(new Vector2(-notch, half - notch));
        vertical.Add(new Vector2(0, half - notch));
        vertical.Add(new Vector2(0, -half + notch));
        vertical.Add(new Vector2(notch, -half + notch));
        vertical.Add(new Vector2(notch, -half - notch));
        vertical.Add(new Vector2(0, -half - notch));
        vertical.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));

        // 水平中线
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, 0));
        horizontal.Add(new Vector2(-half - notch, 0));
        horizontal.Add(new Vector2(-half - notch, notch));
        horizontal.Add(new Vector2(-half + notch, notch));
        horizontal.Add(new Vector2(-half + notch, 0));
        horizontal.Add(new Vector2(half - notch, 0));
        horizontal.Add(new Vector2(half - notch, -notch));
        horizontal.Add(new Vector2(half + notch, -notch));
        horizontal.Add(new Vector2(half + notch, 0));
        horizontal.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));

        // 四分之一分割
        List<Vector2> q1 = new List<Vector2>();
        q1.Add(new Vector2(-half, half));
        q1.Add(new Vector2(-half + notch, half));
        q1.Add(new Vector2(-half + notch, half - notch));
        q1.Add(new Vector2(-half, half - notch));
        q1.Add(new Vector2(-half, half));
        level.cutPaths.Add(DouBao_CreateCutPath(q1.ToArray()));

        List<Vector2> q2 = new List<Vector2>();
        q2.Add(new Vector2(half, half));
        q2.Add(new Vector2(half - notch, half));
        q2.Add(new Vector2(half - notch, half - notch));
        q2.Add(new Vector2(half, half - notch));
        q2.Add(new Vector2(half, half));
        level.cutPaths.Add(DouBao_CreateCutPath(q2.ToArray()));

        List<Vector2> q3 = new List<Vector2>();
        q3.Add(new Vector2(-half, -half));
        q3.Add(new Vector2(-half + notch, -half));
        q3.Add(new Vector2(-half + notch, -half + notch));
        q3.Add(new Vector2(-half, -half + notch));
        q3.Add(new Vector2(-half, -half));
        level.cutPaths.Add(DouBao_CreateCutPath(q3.ToArray()));

        List<Vector2> q4 = new List<Vector2>();
        q4.Add(new Vector2(half, -half));
        q4.Add(new Vector2(half - notch, -half));
        q4.Add(new Vector2(half - notch, -half + notch));
        q4.Add(new Vector2(half, -half + notch));
        q4.Add(new Vector2(half, -half));
        level.cutPaths.Add(DouBao_CreateCutPath(q4.ToArray()));
    }

    private void DouBao_GenerateRadialSixSlice(LevelDesignData level, float L)
    {
        // 6等分放射状拼图
        float radius = L * 0.8f;
        float notch = L * 0.08f;

        for (int i = 0; i < 6; i++)
        {
            float angle1 = i * Mathf.PI / 3;
            float angle2 = (i + 1) * Mathf.PI / 3;

            List<Vector2> slice = new List<Vector2>();
            slice.Add(Vector2.zero);

            // 外边缘
            slice.Add(new Vector2(
                Mathf.Cos(angle1) * (radius - notch),
                Mathf.Sin(angle1) * (radius - notch)
            ));
            slice.Add(new Vector2(
                Mathf.Cos(angle1) * radius,
                Mathf.Sin(angle1) * radius
            ));
            slice.Add(new Vector2(
                Mathf.Cos((angle1 + angle2) / 2) * radius,
                Mathf.Sin((angle1 + angle2) / 2) * radius
            ));
            slice.Add(new Vector2(
                Mathf.Cos(angle2) * radius,
                Mathf.Sin(angle2) * radius
            ));
            slice.Add(new Vector2(
                Mathf.Cos(angle2) * (radius - notch),
                Mathf.Sin(angle2) * (radius - notch)
            ));

            slice.Add(Vector2.zero);
            level.cutPaths.Add(DouBao_CreateCutPath(slice.ToArray()));
        }
    }

    private void DouBao_GenerateHShapedInterlock(LevelDesignData level, float L)
    {
        // H形中间连接咬合
        float third = L / 3;
        float notch = L * 0.1f;

        // 左竖线
        List<Vector2> left = new List<Vector2>();
        left.Add(new Vector2(-third, L));
        left.Add(new Vector2(-third, third + notch));
        left.Add(new Vector2(-third - notch, third + notch));
        left.Add(new Vector2(-third - notch, third - notch));
        left.Add(new Vector2(-third, third - notch));
        left.Add(new Vector2(-third, -third + notch));
        left.Add(new Vector2(-third - notch, -third + notch));
        left.Add(new Vector2(-third - notch, -third - notch));
        left.Add(new Vector2(-third, -third - notch));
        left.Add(new Vector2(-third, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(left.ToArray()));

        // 右竖线
        List<Vector2> right = new List<Vector2>();
        right.Add(new Vector2(third, L));
        right.Add(new Vector2(third, third + notch));
        right.Add(new Vector2(third + notch, third + notch));
        right.Add(new Vector2(third + notch, third - notch));
        right.Add(new Vector2(third, third - notch));
        right.Add(new Vector2(third, -third + notch));
        right.Add(new Vector2(third + notch, -third + notch));
        right.Add(new Vector2(third + notch, -third - notch));
        right.Add(new Vector2(third, -third - notch));
        right.Add(new Vector2(third, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(right.ToArray()));

        // 中间横线
        List<Vector2> middle = new List<Vector2>();
        middle.Add(new Vector2(-third, 0));
        middle.Add(new Vector2(-third + notch, 0));
        middle.Add(new Vector2(-third + notch, notch));
        middle.Add(new Vector2(third - notch, notch));
        middle.Add(new Vector2(third - notch, 0));
        middle.Add(new Vector2(third, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(middle.ToArray()));
    }

    private void DouBao_GenerateTriangleCombination(LevelDesignData level, float L)
    {
        // 三片三角形组合拼图
        float height = L * Mathf.Sqrt(3) / 2;
        float notch = L * 0.1f;

        // 大三角外框
        List<Vector2> outer = new List<Vector2>();
        outer.Add(new Vector2(-L, -height / 3));
        outer.Add(new Vector2(0, height * 2 / 3));
        outer.Add(new Vector2(L, -height / 3));
        outer.Add(new Vector2(-L, -height / 3));
        level.cutPaths.Add(DouBao_CreateCutPath(outer.ToArray()));

        // 内部分割（带咬合）
        List<Vector2> divide1 = new List<Vector2>();
        divide1.Add(new Vector2(0, height * 2 / 3));
        divide1.Add(new Vector2(L / 2 - notch, -height / 3));
        divide1.Add(new Vector2(L / 2, -height / 3 + notch));
        divide1.Add(new Vector2(L / 2 - notch, -height / 3 + notch));
        divide1.Add(new Vector2(0, height * 2 / 3 - notch));
        divide1.Add(new Vector2(0, height * 2 / 3));
        level.cutPaths.Add(DouBao_CreateCutPath(divide1.ToArray()));

        List<Vector2> divide2 = new List<Vector2>();
        divide2.Add(new Vector2(0, height * 2 / 3));
        divide2.Add(new Vector2(-L / 2 + notch, -height / 3));
        divide2.Add(new Vector2(-L / 2, -height / 3 + notch));
        divide2.Add(new Vector2(-L / 2 + notch, -height / 3 + notch));
        divide2.Add(new Vector2(0, height * 2 / 3 - notch));
        divide2.Add(new Vector2(0, height * 2 / 3));
        level.cutPaths.Add(DouBao_CreateCutPath(divide2.ToArray()));
    }

    private void DouBao_GenerateParallelogramInterlock(LevelDesignData level, float L)
    {
        // 平行四边形斜向咬合
        float offset = L * 0.3f;
        float notch = L * 0.1f;

        List<Vector2> para = new List<Vector2>();
        para.Add(new Vector2(-L, L));
        para.Add(new Vector2(-L + offset, L));
        para.Add(new Vector2(-L + offset - notch, L - notch));
        para.Add(new Vector2(-L - notch, L - notch));
        para.Add(new Vector2(-L, L));

        para.Add(new Vector2(L - offset, -L));
        para.Add(new Vector2(L - offset + notch, -L + notch));
        para.Add(new Vector2(L + notch, -L + notch));
        para.Add(new Vector2(L, -L));
        para.Add(new Vector2(L - offset, -L));

        level.cutPaths.Add(DouBao_CreateCutPath(para.ToArray()));
    }

    private void DouBao_GenerateIShapedInterlock(LevelDesignData level, float L)
    {
        // 工字形两端咬合
        float half = L / 2;
        float notch = L * 0.1f;

        // 上横
        List<Vector2> top = new List<Vector2>();
        top.Add(new Vector2(-half, L));
        top.Add(new Vector2(-half + notch, L));
        top.Add(new Vector2(-half + notch, L - notch));
        top.Add(new Vector2(half - notch, L - notch));
        top.Add(new Vector2(half - notch, L));
        top.Add(new Vector2(half, L));
        level.cutPaths.Add(DouBao_CreateCutPath(top.ToArray()));

        // 中竖
        List<Vector2> middle = new List<Vector2>();
        middle.Add(new Vector2(0, L));
        middle.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(middle.ToArray()));

        // 下横
        List<Vector2> bottom = new List<Vector2>();
        bottom.Add(new Vector2(-half, -L));
        bottom.Add(new Vector2(-half + notch, -L));
        bottom.Add(new Vector2(-half + notch, -L + notch));
        bottom.Add(new Vector2(half - notch, -L + notch));
        bottom.Add(new Vector2(half - notch, -L));
        bottom.Add(new Vector2(half, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(bottom.ToArray()));
    }

    private void DouBao_GenerateSimplePathInterlock(LevelDesignData level, float L)
    {
        // 简单路径咬合拼图
        float quarter = L / 4;
        float half = L / 2;
        float notch = L * 0.08f;

        List<Vector2> path = new List<Vector2>();
        path.Add(new Vector2(-L, quarter));
        path.Add(new Vector2(-quarter, quarter));
        path.Add(new Vector2(-quarter, quarter - notch));
        path.Add(new Vector2(-quarter + notch, quarter - notch));
        path.Add(new Vector2(-quarter + notch, -quarter));
        path.Add(new Vector2(-quarter + notch + notch, -quarter));
        path.Add(new Vector2(-quarter + notch + notch, -quarter + notch));
        path.Add(new Vector2(L, -quarter + notch));
        level.cutPaths.Add(DouBao_CreateCutPath(path.ToArray()));

        List<Vector2> path2 = new List<Vector2>();
        path2.Add(new Vector2(-half, L));
        path2.Add(new Vector2(-half, 0));
        path2.Add(new Vector2(-half + notch, 0));
        path2.Add(new Vector2(-half + notch, -notch));
        path2.Add(new Vector2(-half, -notch));
        path2.Add(new Vector2(-half, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(path2.ToArray()));
    }

    private void DouBao_GenerateRandomSimplePuzzle(LevelDesignData level, int seed, float L)
    {
        // 随机简单拼图切割
        Random.InitState(seed);

        int cuts = Random.Range(2, 4);
        for (int i = 0; i < cuts; i++)
        {
            bool isHorizontal = Random.value > 0.5f;
            float pos = Random.Range(-L * 0.7f, L * 0.7f);
            float notchSize = L * Random.Range(0.05f, 0.12f);

            List<Vector2> cutLine = new List<Vector2>();

            if (isHorizontal)
            {
                cutLine.Add(new Vector2(-L, pos));
                int steps = Random.Range(3, 6);

                for (int j = 0; j < steps; j++)
                {
                    float x = -L + (j + 1) * L * 2 / (steps + 1);
                    bool up = Random.value > 0.5f;

                    cutLine.Add(new Vector2(x - notchSize, pos));
                    cutLine.Add(new Vector2(x - notchSize, pos + (up ? notchSize : -notchSize)));
                    cutLine.Add(new Vector2(x, pos + (up ? notchSize : -notchSize)));
                    cutLine.Add(new Vector2(x, pos));
                }

                cutLine.Add(new Vector2(L, pos));
            }
            else
            {
                cutLine.Add(new Vector2(pos, L));
                int steps = Random.Range(3, 6);

                for (int j = 0; j < steps; j++)
                {
                    float y = L - (j + 1) * L * 2 / (steps + 1);
                    bool right = Random.value > 0.5f;

                    cutLine.Add(new Vector2(pos, y - notchSize));
                    cutLine.Add(new Vector2(pos + (right ? notchSize : -notchSize), y - notchSize));
                    cutLine.Add(new Vector2(pos + (right ? notchSize : -notchSize), y));
                    cutLine.Add(new Vector2(pos, y));
                }

                cutLine.Add(new Vector2(pos, -L));
            }

            level.cutPaths.Add(DouBao_CreateCutPath(cutLine.ToArray()));
        }
    }

    // ==================== 中等难度拼图生成具体实现 ====================
    private void DouBao_GenerateWindowGridComplex(LevelDesignData level, float L)
    {
        // 窗格多向复杂咬合
        float half = L / 2;
        float quarter = L / 4;
        float notch = L * 0.08f;

        // 基础十字（多段咬合）
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(new Vector2(0, L));
        vertical.Add(new Vector2(0, half + quarter));
        vertical.Add(new Vector2(-notch, half + quarter));
        vertical.Add(new Vector2(-notch, half + quarter - notch));
        vertical.Add(new Vector2(notch, half + quarter - notch));
        vertical.Add(new Vector2(notch, half));
        vertical.Add(new Vector2(-notch, half));
        vertical.Add(new Vector2(-notch, half - notch));
        vertical.Add(new Vector2(notch, half - notch));
        vertical.Add(new Vector2(notch, quarter));
        vertical.Add(new Vector2(-notch, quarter));
        vertical.Add(new Vector2(-notch, quarter - notch));
        vertical.Add(new Vector2(notch, quarter - notch));
        vertical.Add(new Vector2(notch, 0));
        vertical.Add(new Vector2(-notch, 0));
        vertical.Add(new Vector2(-notch, -notch));
        vertical.Add(new Vector2(notch, -notch));
        vertical.Add(new Vector2(notch, -quarter));
        vertical.Add(new Vector2(-notch, -quarter));
        vertical.Add(new Vector2(-notch, -quarter + notch));
        vertical.Add(new Vector2(notch, -quarter + notch));
        vertical.Add(new Vector2(notch, -half));
        vertical.Add(new Vector2(-notch, -half));
        vertical.Add(new Vector2(-notch, -half + notch));
        vertical.Add(new Vector2(notch, -half + notch));
        vertical.Add(new Vector2(notch, -half - quarter));
        vertical.Add(new Vector2(-notch, -half - quarter));
        vertical.Add(new Vector2(-notch, -half - quarter + notch));
        vertical.Add(new Vector2(0, -half - quarter + notch));
        vertical.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));

        // 水平方向同理，镜像生成
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, 0));
        horizontal.Add(new Vector2(-half - quarter, 0));
        horizontal.Add(new Vector2(-half - quarter, notch));
        horizontal.Add(new Vector2(-half - quarter + notch, notch));
        horizontal.Add(new Vector2(-half - quarter + notch, -notch));
        horizontal.Add(new Vector2(-half, -notch));
        horizontal.Add(new Vector2(-half, notch));
        horizontal.Add(new Vector2(-half + notch, notch));
        horizontal.Add(new Vector2(-half + notch, -notch));
        horizontal.Add(new Vector2(-quarter, -notch));
        horizontal.Add(new Vector2(-quarter, notch));
        horizontal.Add(new Vector2(-quarter + notch, notch));
        horizontal.Add(new Vector2(-quarter + notch, -notch));
        horizontal.Add(new Vector2(0, -notch));
        horizontal.Add(new Vector2(0, notch));
        horizontal.Add(new Vector2(notch, notch));
        horizontal.Add(new Vector2(notch, -notch));
        horizontal.Add(new Vector2(quarter, -notch));
        horizontal.Add(new Vector2(quarter, notch));
        horizontal.Add(new Vector2(quarter - notch, notch));
        horizontal.Add(new Vector2(quarter - notch, -notch));
        horizontal.Add(new Vector2(half, -notch));
        horizontal.Add(new Vector2(half, notch));
        horizontal.Add(new Vector2(half - notch, notch));
        horizontal.Add(new Vector2(half - notch, -notch));
        horizontal.Add(new Vector2(half + quarter, -notch));
        horizontal.Add(new Vector2(half + quarter, notch));
        horizontal.Add(new Vector2(half + quarter - notch, notch));
        horizontal.Add(new Vector2(half + quarter - notch, 0));
        horizontal.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));
    }

    private void DouBao_GenerateMazePathInterlock(LevelDesignData level, float L)
    {
        // 多路径迷宫咬合拼图
        float step = L / 6;
        float notch = L * 0.05f;

        // 主迷宫路径
        List<Vector2> mainPath = new List<Vector2>();
        mainPath.Add(new Vector2(-L, L));
        mainPath.Add(new Vector2(-L + step, L));
        mainPath.Add(new Vector2(-L + step, L - step));
        mainPath.Add(new Vector2(-L + step + notch, L - step));
        mainPath.Add(new Vector2(-L + step + notch, L - step - notch));
        mainPath.Add(new Vector2(-L + 3 * step, L - step - notch));
        mainPath.Add(new Vector2(-L + 3 * step, L - 2 * step));
        mainPath.Add(new Vector2(-L + 3 * step - notch, L - 2 * step));
        mainPath.Add(new Vector2(-L + 3 * step - notch, L - 2 * step + notch));
        mainPath.Add(new Vector2(-L + 5 * step, L - 2 * step + notch));
        mainPath.Add(new Vector2(-L + 5 * step, -L + 2 * step));
        mainPath.Add(new Vector2(-L + 5 * step + notch, -L + 2 * step));
        mainPath.Add(new Vector2(-L + 5 * step + notch, -L + 2 * step - notch));
        mainPath.Add(new Vector2(-L + 3 * step, -L + 2 * step - notch));
        mainPath.Add(new Vector2(-L + 3 * step, -L + 3 * step));
        mainPath.Add(new Vector2(-L + 3 * step - notch, -L + 3 * step));
        mainPath.Add(new Vector2(-L + 3 * step - notch, -L + 3 * step + notch));
        mainPath.Add(new Vector2(-L + step, -L + 3 * step + notch));
        mainPath.Add(new Vector2(-L + step, -L));
        mainPath.Add(new Vector2(L, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(mainPath.ToArray()));

        // 分支路径
        List<Vector2> branch1 = new List<Vector2>();
        branch1.Add(new Vector2(-L + 2 * step, L - step - notch));
        branch1.Add(new Vector2(-L + 2 * step, -L + 4 * step));
        branch1.Add(new Vector2(-L + 2 * step + notch, -L + 4 * step));
        branch1.Add(new Vector2(-L + 2 * step + notch, -L + 4 * step - notch));
        branch1.Add(new Vector2(L - step, -L + 4 * step - notch));
        branch1.Add(new Vector2(L - step, L - 3 * step));
        branch1.Add(new Vector2(L - step - notch, L - 3 * step));
        branch1.Add(new Vector2(L - step - notch, L - 3 * step + notch));
        branch1.Add(new Vector2(L, L - 3 * step + notch));
        level.cutPaths.Add(DouBao_CreateCutPath(branch1.ToArray()));
    }

    private void DouBao_GenerateSpiralCurveInterlock(LevelDesignData level, float L)
    {
        // 螺旋曲线咬合拼图
        int segments = 24;
        float maxRadius = L * 0.8f;
        float notch = L * 0.05f;

        // 外螺旋
        List<Vector2> outerSpiral = new List<Vector2>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 4; // 两圈螺旋
            float radius = maxRadius * t;

            // 螺旋点
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            // 每4段添加一个咬合齿
            if (i % 4 == 0 && i > 0 && i < segments)
            {
                float toothAngle = angle + Mathf.PI / 8;
                float toothX = Mathf.Cos(toothAngle) * (radius + notch);
                float toothY = Mathf.Sin(toothAngle) * (radius + notch);

                outerSpiral.Add(new Vector2(x, y));
                outerSpiral.Add(new Vector2(toothX, toothY));
                outerSpiral.Add(new Vector2(
                    Mathf.Cos(angle) * (radius + notch),
                    Mathf.Sin(angle) * (radius + notch)
                ));
            }
            else
            {
                outerSpiral.Add(new Vector2(x, y));
            }
        }
        level.cutPaths.Add(DouBao_CreateCutPath(outerSpiral.ToArray()));

        // 内螺旋（反向）
        List<Vector2> innerSpiral = new List<Vector2>();
        for (int i = segments; i >= 0; i--)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 4;
            float radius = maxRadius * 0.5f * t;

            float x = Mathf.Cos(angle + Mathf.PI / 4) * radius;
            float y = Mathf.Sin(angle + Mathf.PI / 4) * radius;

            if (i % 4 == 0 && i > 0 && i < segments)
            {
                float toothAngle = angle - Mathf.PI / 8;
                float toothX = Mathf.Cos(toothAngle) * (radius - notch);
                float toothY = Mathf.Sin(toothAngle) * (radius - notch);

                innerSpiral.Add(new Vector2(x, y));
                innerSpiral.Add(new Vector2(toothX, toothY));
                innerSpiral.Add(new Vector2(
                    Mathf.Cos(angle) * (radius - notch),
                    Mathf.Sin(angle) * (radius - notch)
                ));
            }
            else
            {
                innerSpiral.Add(new Vector2(x, y));
            }
        }
        level.cutPaths.Add(DouBao_CreateCutPath(innerSpiral.ToArray()));
    }

    // ==================== 中等难度缺失函数实现（Case24-Case40） ====================
    private void DouBao_GenerateJaggedEdgeInterlock(LevelDesignData level, float L)
    {
        // 不规则锯齿边咬合（Case24）
        int steps = 16;
        float minNotch = L * 0.08f;
        float maxNotch = L * 0.18f;

        // 水平不规则锯齿线
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, 0));

        Random.InitState(24); // 固定种子保证一致性
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = -L + t * L * 2;
            // 随机锯齿高度和方向
            float notchHeight = Random.Range(minNotch, maxNotch);
            bool up = Random.value > 0.5f;
            float y = up ? notchHeight : -notchHeight;

            // 锯齿顶点（随机偏移增加不规则性）
            float offsetX = Random.Range(-L * 0.02f, L * 0.02f);
            horizontal.Add(new Vector2(x + offsetX, y));
        }

        horizontal.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));

        // 垂直不规则锯齿线（交叉咬合）
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(new Vector2(0, L));

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float y = L - t * L * 2;
            float notchWidth = Random.Range(minNotch, maxNotch);
            bool right = Random.value > 0.5f;
            float x = right ? notchWidth : -notchWidth;

            float offsetY = Random.Range(-L * 0.02f, L * 0.02f);
            vertical.Add(new Vector2(x, y + offsetY));
        }

        vertical.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));
    }

    private void DouBao_GenerateStarShapeInterlock(LevelDesignData level, float L)
    {
        // 四角星多角咬合（Case25）
        float outerRadius = L * 0.8f;
        float innerRadius = L * 0.3f;
        float notch = L * 0.08f;
        int points = 4; // 四角星

        List<Vector2> star = new List<Vector2>();
        for (int i = 0; i < 2 * points; i++)
        {
            float angle = i * Mathf.PI / points;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;

            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            // 每个角添加咬合齿
            if (i % 2 == 0) // 外顶点
            {
                star.Add(new Vector2(
                    Mathf.Cos(angle - Mathf.PI / 16) * (radius - notch),
                    Mathf.Sin(angle - Mathf.PI / 16) * (radius - notch)
                ));
                star.Add(new Vector2(x, y));
                star.Add(new Vector2(
                    Mathf.Cos(angle + Mathf.PI / 16) * (radius - notch),
                    Mathf.Sin(angle + Mathf.PI / 16) * (radius - notch)
                ));
            }
            else // 内凹点
            {
                star.Add(new Vector2(
                    Mathf.Cos(angle - Mathf.PI / 16) * (radius + notch),
                    Mathf.Sin(angle - Mathf.PI / 16) * (radius + notch)
                ));
                star.Add(new Vector2(x, y));
                star.Add(new Vector2(
                    Mathf.Cos(angle + Mathf.PI / 16) * (radius + notch),
                    Mathf.Sin(angle + Mathf.PI / 16) * (radius + notch)
                ));
            }
        }
        // 闭合星形
        star.Add(star[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(star.ToArray()));

        // 星形内部十字咬合分割
        List<Vector2> cross = new List<Vector2>();
        cross.Add(new Vector2(-innerRadius, 0));
        cross.Add(new Vector2(-notch, 0));
        cross.Add(new Vector2(-notch, notch));
        cross.Add(new Vector2(notch, notch));
        cross.Add(new Vector2(notch, 0));
        cross.Add(new Vector2(innerRadius, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(cross.ToArray()));

        List<Vector2> cross2 = new List<Vector2>();
        cross2.Add(new Vector2(0, innerRadius));
        cross2.Add(new Vector2(0, notch));
        cross2.Add(new Vector2(-notch, notch));
        cross2.Add(new Vector2(-notch, -notch));
        cross2.Add(new Vector2(0, -notch));
        cross2.Add(new Vector2(0, -innerRadius));
        level.cutPaths.Add(DouBao_CreateCutPath(cross2.ToArray()));
    }

    private void DouBao_GenerateTShapeAsymmetric(LevelDesignData level, float L)
    {
        // T字形不对称咬合（Case26）
        float stemWidth = L * 0.2f;
        float crossLength = L * 0.8f;
        float notch = L * 0.1f;

        // T字上横（不对称咬合）
        List<Vector2> topCross = new List<Vector2>();
        topCross.Add(new Vector2(-crossLength, L * 0.6f));
        topCross.Add(new Vector2(-crossLength + notch, L * 0.6f));
        topCross.Add(new Vector2(-crossLength + notch, L * 0.6f - notch));
        topCross.Add(new Vector2(-stemWidth - notch, L * 0.6f - notch));
        topCross.Add(new Vector2(-stemWidth - notch, L * 0.6f));
        topCross.Add(new Vector2(-stemWidth, L * 0.6f));
        topCross.Add(new Vector2(-stemWidth, L * 0.6f + notch)); // 左侧额外咬合
        topCross.Add(new Vector2(stemWidth, L * 0.6f)); // 右侧无额外咬合
        topCross.Add(new Vector2(stemWidth + notch, L * 0.6f));
        topCross.Add(new Vector2(stemWidth + notch, L * 0.6f - notch));
        topCross.Add(new Vector2(crossLength - notch, L * 0.6f - notch));
        topCross.Add(new Vector2(crossLength - notch, L * 0.6f));
        topCross.Add(new Vector2(crossLength, L * 0.6f));
        level.cutPaths.Add(DouBao_CreateCutPath(topCross.ToArray()));

        // T字竖杆（不对称咬合）
        List<Vector2> stem = new List<Vector2>();
        stem.Add(new Vector2(-stemWidth, L * 0.6f));
        stem.Add(new Vector2(-stemWidth, -L * 0.4f));
        stem.Add(new Vector2(-stemWidth + notch, -L * 0.4f));
        stem.Add(new Vector2(-stemWidth + notch, -L * 0.4f - notch));
        stem.Add(new Vector2(stemWidth - notch, -L * 0.4f - notch));
        stem.Add(new Vector2(stemWidth - notch, -L * 0.4f));
        stem.Add(new Vector2(stemWidth, -L * 0.4f));
        stem.Add(new Vector2(stemWidth, L * 0.6f));
        // 竖杆中间不对称咬合
        stem.Add(new Vector2(stemWidth, L * 0.2f));
        stem.Add(new Vector2(stemWidth + notch, L * 0.2f)); // 右侧额外咬合
        stem.Add(new Vector2(stemWidth + notch, L * 0.2f - notch));
        stem.Add(new Vector2(stemWidth, L * 0.2f - notch));
        stem.Add(new Vector2(stemWidth, L * 0.6f));
        level.cutPaths.Add(DouBao_CreateCutPath(stem.ToArray()));
    }

    private void DouBao_GenerateLShapedComplex(LevelDesignData level, float L)
    {
        // L形多边复杂咬合（Case27）
        float armLength = L * 0.8f;
        float armWidth = L * 0.3f;
        float notch = L * 0.08f;
        int teethCount = 4; // 每边咬合齿数量

        // L形外框（多段咬合）
        List<Vector2> lOuter = new List<Vector2>();
        // 上边
        lOuter.Add(new Vector2(-armLength, armLength));
        for (int i = 0; i < teethCount; i++)
        {
            float t = (i + 1) / (float)(teethCount + 1);
            float x = -armLength + t * (armLength - armWidth);
            // 交替上下咬合齿
            bool up = i % 2 == 0;
            lOuter.Add(new Vector2(x - notch, armLength));
            lOuter.Add(new Vector2(x - notch, armLength + (up ? notch : -notch)));
            lOuter.Add(new Vector2(x, armLength + (up ? notch : -notch)));
            lOuter.Add(new Vector2(x, armLength));
        }
        lOuter.Add(new Vector2(-armWidth, armLength));

        // 右竖边
        lOuter.Add(new Vector2(-armWidth, armLength - notch));
        for (int i = 0; i < teethCount; i++)
        {
            float t = (i + 1) / (float)(teethCount + 1);
            float y = armLength - notch - t * (armLength - armWidth);
            bool right = i % 2 == 0;
            lOuter.Add(new Vector2(-armWidth, y));
            lOuter.Add(new Vector2(-armWidth + (right ? notch : -notch), y));
            lOuter.Add(new Vector2(-armWidth + (right ? notch : -notch), y - notch));
            lOuter.Add(new Vector2(-armWidth, y - notch));
        }
        lOuter.Add(new Vector2(-armWidth, armWidth));

        // 下边
        lOuter.Add(new Vector2(-armWidth + notch, armWidth));
        for (int i = 0; i < teethCount; i++)
        {
            float t = (i + 1) / (float)(teethCount + 1);
            float x = -armWidth + notch + t * (armLength - armWidth);
            bool down = i % 2 == 0;
            lOuter.Add(new Vector2(x, armWidth));
            lOuter.Add(new Vector2(x, armWidth - (down ? notch : -notch)));
            lOuter.Add(new Vector2(x + notch, armWidth - (down ? notch : -notch)));
            lOuter.Add(new Vector2(x + notch, armWidth));
        }
        lOuter.Add(new Vector2(-armLength, armWidth));

        // 左竖边
        lOuter.Add(new Vector2(-armLength, armWidth + notch));
        for (int i = 0; i < teethCount; i++)
        {
            float t = (i + 1) / (float)(teethCount + 1);
            float y = armWidth + notch + t * (armLength - armWidth);
            bool left = i % 2 == 0;
            lOuter.Add(new Vector2(-armLength, y));
            lOuter.Add(new Vector2(-armLength - (left ? notch : -notch), y));
            lOuter.Add(new Vector2(-armLength - (left ? notch : -notch), y + notch));
            lOuter.Add(new Vector2(-armLength, y + notch));
        }
        lOuter.Add(new Vector2(-armLength, armLength)); // 闭合
        level.cutPaths.Add(DouBao_CreateCutPath(lOuter.ToArray()));

        // L形内部复杂分割
        List<Vector2> innerCut = new List<Vector2>();
        innerCut.Add(new Vector2(-armLength + armWidth, armLength - armWidth));
        innerCut.Add(new Vector2(-armWidth, armLength - armWidth));
        innerCut.Add(new Vector2(-armWidth, armWidth + notch));
        innerCut.Add(new Vector2(-armWidth + notch, armWidth + notch));
        innerCut.Add(new Vector2(-armWidth + notch, armWidth));
        innerCut.Add(new Vector2(-armLength + armWidth, armWidth));
        innerCut.Add(new Vector2(-armLength + armWidth, armLength - armWidth));
        level.cutPaths.Add(DouBao_CreateCutPath(innerCut.ToArray()));
    }

    private void DouBao_GenerateHShapedComplex(LevelDesignData level, float L)
    {
        // H形全边复杂咬合（Case28）
        float barWidth = L * 0.2f;
        float barHeight = L * 0.7f;
        float connectorHeight = L * 0.2f;
        float notch = L * 0.08f;
        int teeth = 5; // 每边咬合齿数量

        // H形左竖杆（全边咬合）
        List<Vector2> leftBar = new List<Vector2>();
        leftBar.Add(new Vector2(-L * 0.5f, barHeight));
        for (int i = 0; i < teeth; i++)
        {
            float t = (i + 1) / (float)(teeth + 1);
            float y = barHeight - t * (barHeight * 2);
            // 左右交替咬合齿
            bool left = i % 2 == 0;
            leftBar.Add(new Vector2(-L * 0.5f, y - notch));
            leftBar.Add(new Vector2(-L * 0.5f - (left ? notch : -notch), y - notch));
            leftBar.Add(new Vector2(-L * 0.5f - (left ? notch : -notch), y));
            leftBar.Add(new Vector2(-L * 0.5f, y));
        }
        leftBar.Add(new Vector2(-L * 0.5f, -barHeight));
        level.cutPaths.Add(DouBao_CreateCutPath(leftBar.ToArray()));

        // H形右竖杆（全边咬合）
        List<Vector2> rightBar = new List<Vector2>();
        rightBar.Add(new Vector2(L * 0.5f, barHeight));
        for (int i = 0; i < teeth; i++)
        {
            float t = (i + 1) / (float)(teeth + 1);
            float y = barHeight - t * (barHeight * 2);
            bool right = i % 2 == 0;
            rightBar.Add(new Vector2(L * 0.5f, y - notch));
            rightBar.Add(new Vector2(L * 0.5f + (right ? notch : -notch), y - notch));
            rightBar.Add(new Vector2(L * 0.5f + (right ? notch : -notch), y));
            rightBar.Add(new Vector2(L * 0.5f, y));
        }
        rightBar.Add(new Vector2(L * 0.5f, -barHeight));
        level.cutPaths.Add(DouBao_CreateCutPath(rightBar.ToArray()));

        // H形上横杠（全边咬合）
        List<Vector2> topConnector = new List<Vector2>();
        topConnector.Add(new Vector2(-L * 0.5f, connectorHeight));
        for (int i = 0; i < teeth; i++)
        {
            float t = (i + 1) / (float)(teeth + 1);
            float x = -L * 0.5f + t * L;
            bool up = i % 2 == 0;
            topConnector.Add(new Vector2(x - notch, connectorHeight));
            topConnector.Add(new Vector2(x - notch, connectorHeight + (up ? notch : -notch)));
            topConnector.Add(new Vector2(x, connectorHeight + (up ? notch : -notch)));
            topConnector.Add(new Vector2(x, connectorHeight));
        }
        topConnector.Add(new Vector2(L * 0.5f, connectorHeight));
        level.cutPaths.Add(DouBao_CreateCutPath(topConnector.ToArray()));

        // H形下横杠（全边咬合）
        List<Vector2> bottomConnector = new List<Vector2>();
        bottomConnector.Add(new Vector2(-L * 0.5f, -connectorHeight));
        for (int i = 0; i < teeth; i++)
        {
            float t = (i + 1) / (float)(teeth + 1);
            float x = -L * 0.5f + t * L;
            bool down = i % 2 == 0;
            bottomConnector.Add(new Vector2(x - notch, -connectorHeight));
            bottomConnector.Add(new Vector2(x - notch, -connectorHeight - (down ? notch : -notch)));
            bottomConnector.Add(new Vector2(x, -connectorHeight - (down ? notch : -notch)));
            bottomConnector.Add(new Vector2(x, -connectorHeight));
        }
        bottomConnector.Add(new Vector2(L * 0.5f, -connectorHeight));
        level.cutPaths.Add(DouBao_CreateCutPath(bottomConnector.ToArray()));

        // H形中间连接（复杂咬合）
        List<Vector2> middle = new List<Vector2>();
        middle.Add(new Vector2(-L * 0.5f, connectorHeight));
        middle.Add(new Vector2(-barWidth, connectorHeight));
        middle.Add(new Vector2(-barWidth, connectorHeight - notch));
        middle.Add(new Vector2(-barWidth + notch, connectorHeight - notch));
        middle.Add(new Vector2(-barWidth + notch, -connectorHeight + notch));
        middle.Add(new Vector2(-barWidth, -connectorHeight + notch));
        middle.Add(new Vector2(-barWidth, -connectorHeight));
        middle.Add(new Vector2(-L * 0.5f, -connectorHeight));

        middle.Add(new Vector2(L * 0.5f, -connectorHeight));
        middle.Add(new Vector2(barWidth, -connectorHeight));
        middle.Add(new Vector2(barWidth, -connectorHeight + notch));
        middle.Add(new Vector2(barWidth - notch, -connectorHeight + notch));
        middle.Add(new Vector2(barWidth - notch, connectorHeight - notch));
        middle.Add(new Vector2(barWidth, connectorHeight - notch));
        middle.Add(new Vector2(barWidth, connectorHeight));
        middle.Add(new Vector2(L * 0.5f, connectorHeight));
        level.cutPaths.Add(DouBao_CreateCutPath(middle.ToArray()));
    }

    private void DouBao_GenerateWindmillInterlock(LevelDesignData level, float L)
    {
        // 风车旋转咬合拼图（Case29）
        float radius = L * 0.8f;
        float notch = L * 0.08f;
        int blades = 4; // 四叶风车

        for (int i = 0; i < blades; i++)
        {
            float angle1 = i * Mathf.PI / 2 - Mathf.PI / 8;
            float angle2 = i * Mathf.PI / 2 + Mathf.PI / 8;
            float angle3 = i * Mathf.PI / 2 + Mathf.PI / 2 - Mathf.PI / 8;
            float angle4 = i * Mathf.PI / 2 + Mathf.PI / 2 + Mathf.PI / 8;

            List<Vector2> blade = new List<Vector2>();
            blade.Add(Vector2.zero);
            // 叶片外边缘（带旋转咬合齿）
            blade.Add(new Vector2(
                Mathf.Cos(angle1) * (radius - notch),
                Mathf.Sin(angle1) * (radius - notch)
            ));
            blade.Add(new Vector2(
                Mathf.Cos(angle1) * radius,
                Mathf.Sin(angle1) * radius
            ));
            blade.Add(new Vector2(
                Mathf.Cos((angle1 + angle2) / 2) * radius,
                Mathf.Sin((angle1 + angle2) / 2) * radius
            ));
            blade.Add(new Vector2(
                Mathf.Cos(angle2) * radius,
                Mathf.Sin(angle2) * radius
            ));
            blade.Add(new Vector2(
                Mathf.Cos(angle2) * (radius - notch),
                Mathf.Sin(angle2) * (radius - notch)
            ));
            // 叶片内边缘（旋转咬合）
            blade.Add(new Vector2(
                Mathf.Cos(angle3) * (radius * 0.3f + notch),
                Mathf.Sin(angle3) * (radius * 0.3f + notch)
            ));
            blade.Add(new Vector2(
                Mathf.Cos(angle3) * (radius * 0.3f),
                Mathf.Sin(angle3) * (radius * 0.3f)
            ));
            blade.Add(new Vector2(
                Mathf.Cos((angle3 + angle4) / 2) * (radius * 0.3f),
                Mathf.Sin((angle3 + angle4) / 2) * (radius * 0.3f)
            ));
            blade.Add(new Vector2(
                Mathf.Cos(angle4) * (radius * 0.3f),
                Mathf.Sin(angle4) * (radius * 0.3f)
            ));
            blade.Add(new Vector2(
                Mathf.Cos(angle4) * (radius * 0.3f + notch),
                Mathf.Sin(angle4) * (radius * 0.3f + notch)
            ));
            blade.Add(Vector2.zero);

            level.cutPaths.Add(DouBao_CreateCutPath(blade.ToArray()));
        }

        // 风车中心旋转咬合结构
        List<Vector2> center = new List<Vector2>();
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            float r = (i % 2 == 0) ? L * 0.1f : L * 0.05f;
            center.Add(new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r));
        }
        center.Add(center[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(center.ToArray()));
    }

    private void DouBao_Generate2x2FullInterlock(LevelDesignData level, float L)
    {
        // 2x2全边咬合拼图（Case30）
        float half = L / 2;
        float notch = L * 0.12f;

        // 水平切割线（全边咬合）
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, 0));
        // 左上-右上咬合
        horizontal.Add(new Vector2(-half - notch, 0));
        horizontal.Add(new Vector2(-half - notch, notch));
        horizontal.Add(new Vector2(-half + notch, notch));
        horizontal.Add(new Vector2(-half + notch, 0));
        // 右上-右下咬合
        horizontal.Add(new Vector2(half - notch, 0));
        horizontal.Add(new Vector2(half - notch, -notch));
        horizontal.Add(new Vector2(half + notch, -notch));
        horizontal.Add(new Vector2(half + notch, 0));
        horizontal.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));

        // 垂直切割线（全边咬合）
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(new Vector2(0, L));
        // 左上-左下咬合
        vertical.Add(new Vector2(0, half - notch));
        vertical.Add(new Vector2(-notch, half - notch));
        vertical.Add(new Vector2(-notch, half + notch));
        vertical.Add(new Vector2(0, half + notch));
        // 左下-右下咬合
        vertical.Add(new Vector2(0, -half + notch));
        vertical.Add(new Vector2(notch, -half + notch));
        vertical.Add(new Vector2(notch, -half - notch));
        vertical.Add(new Vector2(0, -half - notch));
        vertical.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));

        // 四个象限内部额外咬合
        // 左上
        List<Vector2> topLeft = new List<Vector2>();
        topLeft.Add(new Vector2(-half, half));
        topLeft.Add(new Vector2(-half + notch / 2, half));
        topLeft.Add(new Vector2(-half + notch / 2, half - notch / 2));
        topLeft.Add(new Vector2(-half, half - notch / 2));
        topLeft.Add(new Vector2(-half, half));
        level.cutPaths.Add(DouBao_CreateCutPath(topLeft.ToArray()));

        // 右上
        List<Vector2> topRight = new List<Vector2>();
        topRight.Add(new Vector2(half, half));
        topRight.Add(new Vector2(half - notch / 2, half));
        topRight.Add(new Vector2(half - notch / 2, half - notch / 2));
        topRight.Add(new Vector2(half, half - notch / 2));
        topRight.Add(new Vector2(half, half));
        level.cutPaths.Add(DouBao_CreateCutPath(topRight.ToArray()));

        // 左下
        List<Vector2> bottomLeft = new List<Vector2>();
        bottomLeft.Add(new Vector2(-half, -half));
        bottomLeft.Add(new Vector2(-half + notch / 2, -half));
        bottomLeft.Add(new Vector2(-half + notch / 2, -half + notch / 2));
        bottomLeft.Add(new Vector2(-half, -half + notch / 2));
        bottomLeft.Add(new Vector2(-half, -half));
        level.cutPaths.Add(DouBao_CreateCutPath(bottomLeft.ToArray()));

        // 右下
        List<Vector2> bottomRight = new List<Vector2>();
        bottomRight.Add(new Vector2(half, -half));
        bottomRight.Add(new Vector2(half - notch / 2, -half));
        bottomRight.Add(new Vector2(half - notch / 2, -half + notch / 2));
        bottomRight.Add(new Vector2(half, -half + notch / 2));
        bottomRight.Add(new Vector2(half, -half));
        level.cutPaths.Add(DouBao_CreateCutPath(bottomRight.ToArray()));
    }

    private void DouBao_GenerateDualWaveInterlock(LevelDesignData level, float L)
    {
        // 正交波浪双边咬合（Case31）
        int segments = 16;
        float amplitude = L * 0.15f;
        float notch = L * 0.05f;

        // 水平波浪（带咬合齿）
        List<Vector2> horizontalWave = new List<Vector2>();
        horizontalWave.Add(new Vector2(-L, 0));
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = -L + t * L * 2;
            float y = Mathf.Sin(t * Mathf.PI * 4) * amplitude;

            // 波峰波谷添加咬合齿
            if (Mathf.Abs(y) > amplitude * 0.8f)
            {
                float dir = Mathf.Sign(y);
                horizontalWave.Add(new Vector2(x - notch, y));
                horizontalWave.Add(new Vector2(x - notch, y + dir * notch));
                horizontalWave.Add(new Vector2(x, y + dir * notch));
                horizontalWave.Add(new Vector2(x, y));
            }
            else
            {
                horizontalWave.Add(new Vector2(x, y));
            }
        }
        horizontalWave.Add(new Vector2(L, 0));
        level.cutPaths.Add(DouBao_CreateCutPath(horizontalWave.ToArray()));

        // 垂直波浪（正交，带咬合齿）
        List<Vector2> verticalWave = new List<Vector2>();
        verticalWave.Add(new Vector2(0, L));
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float y = L - t * L * 2;
            float x = Mathf.Cos(t * Mathf.PI * 4) * amplitude; // 余弦波（正交）

            if (Mathf.Abs(x) > amplitude * 0.8f)
            {
                float dir = Mathf.Sign(x);
                verticalWave.Add(new Vector2(x, y - notch));
                verticalWave.Add(new Vector2(x + dir * notch, y - notch));
                verticalWave.Add(new Vector2(x + dir * notch, y));
                verticalWave.Add(new Vector2(x, y));
            }
            else
            {
                verticalWave.Add(new Vector2(x, y));
            }
        }
        verticalWave.Add(new Vector2(0, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(verticalWave.ToArray()));
    }

    private void DouBao_GenerateMultiSliceCircle(LevelDesignData level, float L)
    {
        // 多片弧形圆形拼图（Case32）
        float outerRadius = L * 0.8f;
        float innerRadius = L * 0.3f;
        int slices = 8; // 8片弧形
        float notch = L * 0.06f;

        for (int i = 0; i < slices; i++)
        {
            float angle1 = i * Mathf.PI * 2 / slices;
            float angle2 = (i + 1) * Mathf.PI * 2 / slices;

            List<Vector2> slice = new List<Vector2>();
            slice.Add(Vector2.zero);

            // 弧形外边缘（带咬合齿）
            int arcSegments = 8;
            for (int j = 0; j <= arcSegments; j++)
            {
                float t = j / (float)arcSegments;
                float angle = angle1 + t * (angle2 - angle1);
                float radius = outerRadius;

                // 每2段添加咬合齿
                if (j % 2 == 0 && j > 0 && j < arcSegments)
                {
                    slice.Add(new Vector2(
                        Mathf.Cos(angle) * (radius - notch),
                        Mathf.Sin(angle) * (radius - notch)
                    ));
                    slice.Add(new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    ));
                }
                else
                {
                    slice.Add(new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    ));
                }
            }

            // 弧形内边缘（带咬合齿）
            for (int j = arcSegments; j >= 0; j--)
            {
                float t = j / (float)arcSegments;
                float angle = angle1 + t * (angle2 - angle1);
                float radius = innerRadius;

                if (j % 2 == 0 && j > 0 && j < arcSegments)
                {
                    slice.Add(new Vector2(
                        Mathf.Cos(angle) * (radius + notch),
                        Mathf.Sin(angle) * (radius + notch)
                    ));
                    slice.Add(new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    ));
                }
                else
                {
                    slice.Add(new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius
                    ));
                }
            }

            slice.Add(Vector2.zero);
            level.cutPaths.Add(DouBao_CreateCutPath(slice.ToArray()));
        }

        // 中心圆形咬合结构
        List<Vector2> centerCircle = new List<Vector2>();
        for (int i = 0; i <= 16; i++)
        {
            float angle = i * Mathf.PI * 2 / 16;
            centerCircle.Add(new Vector2(
                Mathf.Cos(angle) * innerRadius,
                Mathf.Sin(angle) * innerRadius
            ));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(centerCircle.ToArray()));
    }

    private void DouBao_GenerateIrregularQuad(LevelDesignData level, float L)
    {
        // 异形四边形拼图（Case33）
        Random.InitState(33); // 固定种子
        float offsetRange = L * 0.2f;

        // 生成四个不规则顶点（保证凸四边形）
        Vector2 p1 = new Vector2(-L + Random.Range(0, offsetRange), L - Random.Range(0, offsetRange));
        Vector2 p2 = new Vector2(L - Random.Range(0, offsetRange), L - Random.Range(0, offsetRange));
        Vector2 p3 = new Vector2(L - Random.Range(0, offsetRange), -L + Random.Range(0, offsetRange));
        Vector2 p4 = new Vector2(-L + Random.Range(0, offsetRange), -L + Random.Range(0, offsetRange));

        float notch = L * 0.1f;
        int teethPerSide = 3;

        // 异形四边形外框（带咬合齿）
        List<Vector2> quad = new List<Vector2>();
        // 上边（p1-p2）
        quad.Add(p1);
        for (int i = 1; i <= teethPerSide; i++)
        {
            float t = i / (float)(teethPerSide + 1);
            Vector2 point = Vector2.Lerp(p1, p2, t);
            // 随机咬合方向
            bool up = Random.value > 0.5f;
            Vector2 normal = new Vector2(p2.y - p1.y, p1.x - p2.x).normalized; // 法向量
            quad.Add(point - normal * notch);
            quad.Add(point + (up ? normal : -normal) * notch);
            quad.Add(point + normal * notch);
        }
        quad.Add(p2);

        // 右边（p2-p3）
        for (int i = 1; i <= teethPerSide; i++)
        {
            float t = i / (float)(teethPerSide + 1);
            Vector2 point = Vector2.Lerp(p2, p3, t);
            bool right = Random.value > 0.5f;
            Vector2 normal = new Vector2(p3.y - p2.y, p2.x - p3.x).normalized;
            quad.Add(point - normal * notch);
            quad.Add(point + (right ? normal : -normal) * notch);
            quad.Add(point + normal * notch);
        }
        quad.Add(p3);

        // 下边（p3-p4）
        for (int i = 1; i <= teethPerSide; i++)
        {
            float t = i / (float)(teethPerSide + 1);
            Vector2 point = Vector2.Lerp(p3, p4, t);
            bool down = Random.value > 0.5f;
            Vector2 normal = new Vector2(p4.y - p3.y, p3.x - p4.x).normalized;
            quad.Add(point - normal * notch);
            quad.Add(point + (down ? normal : -normal) * notch);
            quad.Add(point + normal * notch);
        }
        quad.Add(p4);

        // 左边（p4-p1）
        for (int i = 1; i <= teethPerSide; i++)
        {
            float t = i / (float)(teethPerSide + 1);
            Vector2 point = Vector2.Lerp(p4, p1, t);
            bool left = Random.value > 0.5f;
            Vector2 normal = new Vector2(p1.y - p4.y, p4.x - p1.x).normalized;
            quad.Add(point - normal * notch);
            quad.Add(point + (left ? normal : -normal) * notch);
            quad.Add(point + normal * notch);
        }
        quad.Add(p1); // 闭合
        level.cutPaths.Add(DouBao_CreateCutPath(quad.ToArray()));

        // 内部对角线不规则分割
        Vector2 center = (p1 + p2 + p3 + p4) / 4;
        List<Vector2> diag1 = new List<Vector2>();
        diag1.Add(p1 + (center - p1).normalized * L * 0.2f);
        diag1.Add(center);
        diag1.Add(p3 + (center - p3).normalized * L * 0.2f);
        level.cutPaths.Add(DouBao_CreateCutPath(diag1.ToArray()));

        List<Vector2> diag2 = new List<Vector2>();
        diag2.Add(p2 + (center - p2).normalized * L * 0.2f);
        diag2.Add(center);
        diag2.Add(p4 + (center - p4).normalized * L * 0.2f);
        level.cutPaths.Add(DouBao_CreateCutPath(diag2.ToArray()));
    }

    private void DouBao_GeneratePetalShape8(LevelDesignData level, float L)
    {
        // 8瓣花瓣形拼图（Case34）
        float outerRadius = L * 0.8f;
        float innerRadius = L * 0.2f;
        int petals = 8;
        float notch = L * 0.07f;

        // 花瓣外轮廓
        List<Vector2> petalsOuter = new List<Vector2>();
        for (int i = 0; i < 2 * petals; i++)
        {
            float angle = i * Mathf.PI / petals;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;

            Vector2 point = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );

            // 花瓣边缘添加咬合齿
            if (i % 2 == 0) // 花瓣外顶点
            {
                petalsOuter.Add(new Vector2(
                    Mathf.Cos(angle - Mathf.PI / 24) * (radius - notch),
                    Mathf.Sin(angle - Mathf.PI / 24) * (radius - notch)
                ));
                petalsOuter.Add(point);
                petalsOuter.Add(new Vector2(
                    Mathf.Cos(angle + Mathf.PI / 24) * (radius - notch),
                    Mathf.Sin(angle + Mathf.PI / 24) * (radius - notch)
                ));
            }
            else // 花瓣间凹点
            {
                petalsOuter.Add(new Vector2(
                    Mathf.Cos(angle - Mathf.PI / 24) * (radius + notch),
                    Mathf.Sin(angle - Mathf.PI / 24) * (radius + notch)
                ));
                petalsOuter.Add(point);
                petalsOuter.Add(new Vector2(
                    Mathf.Cos(angle + Mathf.PI / 24) * (radius + notch),
                    Mathf.Sin(angle + Mathf.PI / 24) * (radius + notch)
                ));
            }
        }
        petalsOuter.Add(petalsOuter[0]); // 闭合
        level.cutPaths.Add(DouBao_CreateCutPath(petalsOuter.ToArray()));

        // 8个花瓣分割线（带咬合）
        for (int i = 0; i < petals; i++)
        {
            float angle = (i + 0.5f) * Mathf.PI * 2 / petals;
            List<Vector2> petalDivider = new List<Vector2>();

            petalDivider.Add(Vector2.zero);
            petalDivider.Add(new Vector2(
                Mathf.Cos(angle) * (innerRadius + notch),
                Mathf.Sin(angle) * (innerRadius + notch)
            ));
            petalDivider.Add(new Vector2(
                Mathf.Cos(angle) * innerRadius,
                Mathf.Sin(angle) * innerRadius
            ));

            // 花瓣中间咬合齿
            float midAngle = angle;
            float midRadius = (outerRadius + innerRadius) / 2;
            petalDivider.Add(new Vector2(
                Mathf.Cos(midAngle - Mathf.PI / 32) * midRadius,
                Mathf.Sin(midAngle - Mathf.PI / 32) * midRadius
            ));
            petalDivider.Add(new Vector2(
                Mathf.Cos(midAngle) * (midRadius + notch / 2),
                Mathf.Sin(midAngle) * (midRadius + notch / 2)
            ));
            petalDivider.Add(new Vector2(
                Mathf.Cos(midAngle + Mathf.PI / 32) * midRadius,
                Mathf.Sin(midAngle + Mathf.PI / 32) * midRadius
            ));

            petalDivider.Add(new Vector2(
                Mathf.Cos(angle) * (outerRadius - notch),
                Mathf.Sin(angle) * (outerRadius - notch)
            ));
            petalDivider.Add(new Vector2(
                Mathf.Cos(angle) * outerRadius,
                Mathf.Sin(angle) * outerRadius
            ));
            petalDivider.Add(Vector2.zero);

            level.cutPaths.Add(DouBao_CreateCutPath(petalDivider.ToArray()));
        }
    }

    private void DouBao_GenerateCrossSlantInterlock(LevelDesignData level, float L)
    {
        // 交叉斜线不规则咬合（Case35）
        float slope1 = 1.2f; // 非45度斜线
        float slope2 = -0.8f;
        float notch = L * 0.1f;
        int teeth = 5;

        // 第一条斜线（斜率1.2）
        List<Vector2> slant1 = new List<Vector2>();
        slant1.Add(new Vector2(-L, -L * slope1));
        for (int i = 1; i <= teeth; i++)
        {
            float t = i / (float)(teeth + 1);
            float x = -L + t * L * 2;
            float y = x * slope1;

            // 斜线两侧交替咬合齿
            bool up = i % 2 == 0;
            float perpSlope = -1 / slope1; // 垂直斜率
            float len = Mathf.Sqrt(1 + perpSlope * perpSlope);
            float offsetX = notch / len;
            float offsetY = perpSlope * offsetX;

            slant1.Add(new Vector2(x - offsetX, y - offsetY));
            slant1.Add(new Vector2(x + (up ? offsetX : -offsetX), y + (up ? offsetY : -offsetY)));
            slant1.Add(new Vector2(x + offsetX, y + offsetY));
        }
        slant1.Add(new Vector2(L, L * slope1));
        level.cutPaths.Add(DouBao_CreateCutPath(slant1.ToArray()));

        // 第二条斜线（斜率-0.8，交叉）
        List<Vector2> slant2 = new List<Vector2>();
        slant2.Add(new Vector2(-L, L * Mathf.Abs(slope2)));
        for (int i = 1; i <= teeth; i++)
        {
            float t = i / (float)(teeth + 1);
            float x = -L + t * L * 2;
            float y = x * slope2;

            bool right = i % 2 == 0;
            float perpSlope = -1 / slope2;
            float len = Mathf.Sqrt(1 + perpSlope * perpSlope);
            float offsetX = notch / len;
            float offsetY = perpSlope * offsetX;

            slant2.Add(new Vector2(x - offsetX, y - offsetY));
            slant2.Add(new Vector2(x + (right ? offsetX : -offsetX), y + (right ? offsetY : -offsetY)));
            slant2.Add(new Vector2(x + offsetX, y + offsetY));
        }
        slant2.Add(new Vector2(L, L * slope2));
        level.cutPaths.Add(DouBao_CreateCutPath(slant2.ToArray()));

        // 交叉区域额外咬合结构
        Vector2 crossPoint = new Vector2(0, 0); // 交点
        List<Vector2> crossDetail = new List<Vector2>();
        crossDetail.Add(crossPoint + new Vector2(-notch * 2, -notch * 2));
        crossDetail.Add(crossPoint + new Vector2(-notch, -notch));
        crossDetail.Add(crossPoint + new Vector2(-notch, notch));
        crossDetail.Add(crossPoint + new Vector2(notch, notch));
        crossDetail.Add(crossPoint + new Vector2(notch, -notch));
        crossDetail.Add(crossPoint + new Vector2(notch * 2, -notch * 2));
        level.cutPaths.Add(DouBao_CreateCutPath(crossDetail.ToArray()));
    }

    private void DouBao_GenerateModular4Type(LevelDesignData level, float L)
    {
        // 4模块组合拼图（Case36）
        float moduleSize = L * 0.4f;
        float spacing = L * 0.1f;
        float notch = L * 0.08f;

        // 模块1：方形带单边咬合
        List<Vector2> module1 = new List<Vector2>();
        module1.Add(new Vector2(-L / 2 - moduleSize / 2, L / 2 + moduleSize / 2));
        module1.Add(new Vector2(-L / 2 + moduleSize / 2, L / 2 + moduleSize / 2));
        module1.Add(new Vector2(-L / 2 + moduleSize / 2, L / 2 + moduleSize / 2 - notch));
        module1.Add(new Vector2(-L / 2 + moduleSize / 2 - notch, L / 2 + moduleSize / 2 - notch));
        module1.Add(new Vector2(-L / 2 + moduleSize / 2 - notch, L / 2 - moduleSize / 2));
        module1.Add(new Vector2(-L / 2 - moduleSize / 2, L / 2 - moduleSize / 2));
        module1.Add(new Vector2(-L / 2 - moduleSize / 2, L / 2 + moduleSize / 2));
        level.cutPaths.Add(DouBao_CreateCutPath(module1.ToArray()));

        // 模块2：方形带双边咬合
        List<Vector2> module2 = new List<Vector2>();
        module2.Add(new Vector2(L / 2 - moduleSize / 2, L / 2 + moduleSize / 2));
        module2.Add(new Vector2(L / 2 + moduleSize / 2, L / 2 + moduleSize / 2));
        module2.Add(new Vector2(L / 2 + moduleSize / 2, L / 2 + moduleSize / 2 - notch));
        module2.Add(new Vector2(L / 2 + moduleSize / 2 - notch, L / 2 + moduleSize / 2 - notch));
        module2.Add(new Vector2(L / 2 + moduleSize / 2 - notch, L / 2 - moduleSize / 2));
        module2.Add(new Vector2(L / 2 + moduleSize / 2 - notch + notch, L / 2 - moduleSize / 2));
        module2.Add(new Vector2(L / 2 + moduleSize / 2 - notch + notch, L / 2 - moduleSize / 2 + notch));
        module2.Add(new Vector2(L / 2 - moduleSize / 2, L / 2 - moduleSize / 2 + notch));
        module2.Add(new Vector2(L / 2 - moduleSize / 2, L / 2 + moduleSize / 2));
        level.cutPaths.Add(DouBao_CreateCutPath(module2.ToArray()));

        // 模块3：L形模块
        List<Vector2> module3 = new List<Vector2>();
        module3.Add(new Vector2(-L / 2 - moduleSize / 2, -L / 2 - moduleSize / 2));
        module3.Add(new Vector2(-L / 2 + moduleSize / 2, -L / 2 - moduleSize / 2));
        module3.Add(new Vector2(-L / 2 + moduleSize / 2, -L / 2));
        module3.Add(new Vector2(-L / 2, -L / 2));
        module3.Add(new Vector2(-L / 2, -L / 2 + moduleSize / 2));
        module3.Add(new Vector2(-L / 2 - moduleSize / 2, -L / 2 + moduleSize / 2));
        module3.Add(new Vector2(-L / 2 - moduleSize / 2, -L / 2 - moduleSize / 2));
        // L形咬合齿
        module3.Add(new Vector2(-L / 2 - notch, -L / 2 - moduleSize / 2 + notch));
        module3.Add(new Vector2(-L / 2 - notch, -L / 2 - moduleSize / 2 + notch + notch));
        module3.Add(new Vector2(-L / 2 - moduleSize / 2 + notch, -L / 2 - moduleSize / 2 + notch + notch));
        module3.Add(new Vector2(-L / 2 - moduleSize / 2 + notch, -L / 2 - moduleSize / 2 + notch));
        level.cutPaths.Add(DouBao_CreateCutPath(module3.ToArray()));

        // 模块4：T形模块
        List<Vector2> module4 = new List<Vector2>();
        module4.Add(new Vector2(L / 2 - moduleSize / 2, -L / 2 - moduleSize / 2));
        module4.Add(new Vector2(L / 2 + moduleSize / 2, -L / 2 - moduleSize / 2));
        module4.Add(new Vector2(L / 2 + moduleSize / 2, -L / 2 - moduleSize / 2 + notch));
        module4.Add(new Vector2(L / 2, -L / 2 - moduleSize / 2 + notch));
        module4.Add(new Vector2(L / 2, -L / 2 + moduleSize / 2));
        module4.Add(new Vector2(L / 2 - notch, -L / 2 + moduleSize / 2));
        module4.Add(new Vector2(L / 2 - notch, -L / 2 - moduleSize / 2 + notch));
        module4.Add(new Vector2(L / 2 - moduleSize / 2, -L / 2 - moduleSize / 2 + notch));
        module4.Add(new Vector2(L / 2 - moduleSize / 2, -L / 2 - moduleSize / 2));
        level.cutPaths.Add(DouBao_CreateCutPath(module4.ToArray()));

        // 模块间连接咬合
        List<Vector2> connectors = new List<Vector2>();
        connectors.Add(new Vector2(-L / 2 + moduleSize / 2 + spacing, L / 2));
        connectors.Add(new Vector2(L / 2 - moduleSize / 2 - spacing, L / 2));
        connectors.Add(new Vector2(L / 2 - moduleSize / 2 - spacing, L / 2 - notch));
        connectors.Add(new Vector2(-L / 2 + moduleSize / 2 + spacing, L / 2 - notch));
        connectors.Add(new Vector2(-L / 2 + moduleSize / 2 + spacing, L / 2));

        connectors.Add(new Vector2(-L / 2, -L / 2 - moduleSize / 2 - spacing));
        connectors.Add(new Vector2(-L / 2, -L / 2 + moduleSize / 2 + spacing));
        connectors.Add(new Vector2(-L / 2 + notch, -L / 2 + moduleSize / 2 + spacing));
        connectors.Add(new Vector2(-L / 2 + notch, -L / 2 - moduleSize / 2 - spacing));
        connectors.Add(new Vector2(-L / 2, -L / 2 - moduleSize / 2 - spacing));
        level.cutPaths.Add(DouBao_CreateCutPath(connectors.ToArray()));
    }

    private void DouBao_GenerateGearInterlock(LevelDesignData level, float L)
    {
        // 齿轮状凹凸咬合（Case37）
        float outerRadius = L * 0.7f;
        float innerRadius = L * 0.2f;
        int teethCount = 12; // 齿轮齿数
        float toothHeight = L * 0.3f; // 增大齿高
        float toothWidth = Mathf.PI * 2 * outerRadius / (teethCount * 2);

        // 外齿轮轮廓
        List<Vector2> gearOuter = new List<Vector2>();
        for (int i = 0; i < teethCount; i++)
        {
            float angle1 = i * Mathf.PI * 2 / teethCount;
            float angle2 = (i + 1) * Mathf.PI * 2 / teethCount;
            float midAngle = (angle1 + angle2) / 2;

            // 齿根
            gearOuter.Add(new Vector2(
                Mathf.Cos(angle1) * outerRadius,
                Mathf.Sin(angle1) * outerRadius
            ));
            // 齿顶
            gearOuter.Add(new Vector2(
                Mathf.Cos(midAngle) * (outerRadius + toothHeight),
                Mathf.Sin(midAngle) * (outerRadius + toothHeight)
            ));
            // 齿根
            gearOuter.Add(new Vector2(
                Mathf.Cos(angle2) * outerRadius,
                Mathf.Sin(angle2) * outerRadius
            ));
        }
        gearOuter.Add(gearOuter[0]); // 闭合
        level.cutPaths.Add(DouBao_CreateCutPath(gearOuter.ToArray()));

        // 内齿轮轮廓（反向咬合）
        List<Vector2> gearInner = new List<Vector2>();
        for (int i = 0; i < teethCount / 2; i++) // 内齿轮齿数减半
        {
            float angle1 = i * Mathf.PI * 2 / (teethCount / 2);
            float angle2 = (i + 1) * Mathf.PI * 2 / (teethCount / 2);
            float midAngle = (angle1 + angle2) / 2;

            gearInner.Add(new Vector2(
                Mathf.Cos(angle1) * (innerRadius + toothHeight / 2),
                Mathf.Sin(angle1) * (innerRadius + toothHeight / 2)
            ));
            gearInner.Add(new Vector2(
                Mathf.Cos(midAngle) * innerRadius,
                Mathf.Sin(midAngle) * innerRadius
            ));
            gearInner.Add(new Vector2(
                Mathf.Cos(angle2) * (innerRadius + toothHeight / 2),
                Mathf.Sin(angle2) * (innerRadius + toothHeight / 2)
            ));
        }
        gearInner.Add(gearInner[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(gearInner.ToArray()));

        // 齿轮辐条（带咬合）
        for (int i = 0; i < teethCount / 2; i++)
        {
            float angle = (i + 0.5f) * Mathf.PI * 2 / (teethCount / 2);
            List<Vector2> spoke = new List<Vector2>();

            spoke.Add(Vector2.zero);
            spoke.Add(new Vector2(
                Mathf.Cos(angle) * (innerRadius + toothHeight / 2),
                Mathf.Sin(angle) * (innerRadius + toothHeight / 2)
            ));
            // 辐条中间咬合齿
            float midRadius = (outerRadius + innerRadius) / 2;
            float biteHeight = toothHeight * 0.75f; // 增大咬合齿高度
            float biteWidth = toothWidth * 1f; // 增大咬合齿宽度

            spoke.Add(new Vector2(
                Mathf.Cos(angle - biteWidth / outerRadius) * midRadius,
                Mathf.Sin(angle - biteWidth / outerRadius) * midRadius
            ));
            spoke.Add(new Vector2(
                Mathf.Cos(angle) * (midRadius + biteHeight),
                Mathf.Sin(angle) * (midRadius + biteHeight)
            ));
            spoke.Add(new Vector2(
                Mathf.Cos(angle + biteWidth / outerRadius) * midRadius,
                Mathf.Sin(angle + biteWidth / outerRadius) * midRadius
            ));
            spoke.Add(new Vector2(
                Mathf.Cos(angle) * (outerRadius - toothHeight),
                Mathf.Sin(angle) * (outerRadius - toothHeight)
            ));
            spoke.Add(new Vector2(
                Mathf.Cos(angle) * outerRadius,
                Mathf.Sin(angle) * outerRadius
            ));
            spoke.Add(Vector2.zero);

            level.cutPaths.Add(DouBao_CreateCutPath(spoke.ToArray()));
        }
    }

    private void DouBao_GenerateDiamondWaveInterlock(LevelDesignData level, float L)
    {
        // 菱形斜向波浪咬合（Case38）
        float diamondSize = L * 0.8f;
        float rotation = Mathf.PI / 4; // 45度菱形
        int waveSegments = 12;
        float amplitude = L * 0.1f;
        float notch = L * 0.05f;

        // 旋转矩阵
        Matrix4x4 rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation * Mathf.Rad2Deg));

        // 菱形上边（斜向波浪）
        List<Vector2> topEdge = new List<Vector2>();
        for (int i = 0; i <= waveSegments; i++)
        {
            float t = i / (float)waveSegments;
            // 基础菱形点
            Vector2 basePoint = new Vector2(-diamondSize / 2 + t * diamondSize, diamondSize / 2);
            // 应用旋转
            Vector2 rotated = rotMatrix.MultiplyPoint3x4(new Vector3(basePoint.x, basePoint.y, 0));
            // 添加波浪偏移
            rotated.y += Mathf.Sin(t * Mathf.PI * 4) * amplitude;

            // 波浪峰谷添加咬合齿
            if (Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4)) > 0.8f)
            {
                float dir = Mathf.Sign(Mathf.Sin(t * Mathf.PI * 4));
                topEdge.Add(new Vector2(rotated.x - notch, rotated.y));
                topEdge.Add(new Vector2(rotated.x - notch, rotated.y + dir * notch));
                topEdge.Add(new Vector2(rotated.x, rotated.y + dir * notch));
                topEdge.Add(new Vector2(rotated.x, rotated.y));
            }
            else
            {
                topEdge.Add(rotated);
            }
        }
        level.cutPaths.Add(DouBao_CreateCutPath(topEdge.ToArray()));

        // 菱形右边（斜向波浪）
        List<Vector2> rightEdge = new List<Vector2>();
        for (int i = 0; i <= waveSegments; i++)
        {
            float t = i / (float)waveSegments;
            Vector2 basePoint = new Vector2(diamondSize / 2, diamondSize / 2 - t * diamondSize);
            Vector2 rotated = rotMatrix.MultiplyPoint3x4(new Vector3(basePoint.x, basePoint.y, 0));
            rotated.x += Mathf.Cos(t * Mathf.PI * 4) * amplitude;

            if (Mathf.Abs(Mathf.Cos(t * Mathf.PI * 4)) > 0.8f)
            {
                float dir = Mathf.Sign(Mathf.Cos(t * Mathf.PI * 4));
                rightEdge.Add(new Vector2(rotated.x, rotated.y - notch));
                rightEdge.Add(new Vector2(rotated.x + dir * notch, rotated.y - notch));
                rightEdge.Add(new Vector2(rotated.x + dir * notch, rotated.y));
                rightEdge.Add(new Vector2(rotated.x, rotated.y));
            }
            else
            {
                rightEdge.Add(rotated);
            }
        }
        level.cutPaths.Add(DouBao_CreateCutPath(rightEdge.ToArray()));

        // 菱形下边（斜向波浪）
        List<Vector2> bottomEdge = new List<Vector2>();
        for (int i = 0; i <= waveSegments; i++)
        {
            float t = i / (float)waveSegments;
            Vector2 basePoint = new Vector2(diamondSize / 2 - t * diamondSize, -diamondSize / 2);
            Vector2 rotated = rotMatrix.MultiplyPoint3x4(new Vector3(basePoint.x, basePoint.y, 0));
            rotated.y -= Mathf.Sin(t * Mathf.PI * 4) * amplitude;

            if (Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4)) > 0.8f)
            {
                float dir = Mathf.Sign(Mathf.Sin(t * Mathf.PI * 4));
                bottomEdge.Add(new Vector2(rotated.x + notch, rotated.y));
                bottomEdge.Add(new Vector2(rotated.x + notch, rotated.y - dir * notch));
                bottomEdge.Add(new Vector2(rotated.x, rotated.y - dir * notch));
                bottomEdge.Add(new Vector2(rotated.x, rotated.y));
            }
            else
            {
                bottomEdge.Add(rotated);
            }
        }
        level.cutPaths.Add(DouBao_CreateCutPath(bottomEdge.ToArray()));

        // 菱形左边（斜向波浪）
        List<Vector2> leftEdge = new List<Vector2>();
        for (int i = 0; i <= waveSegments; i++)
        {
            float t = i / (float)waveSegments;
            Vector2 basePoint = new Vector2(-diamondSize / 2, -diamondSize / 2 + t * diamondSize);
            Vector2 rotated = rotMatrix.MultiplyPoint3x4(new Vector3(basePoint.x, basePoint.y, 0));
            rotated.x -= Mathf.Cos(t * Mathf.PI * 4) * amplitude;

            if (Mathf.Abs(Mathf.Cos(t * Mathf.PI * 4)) > 0.8f)
            {
                float dir = Mathf.Sign(Mathf.Cos(t * Mathf.PI * 4));
                leftEdge.Add(new Vector2(rotated.x, rotated.y + notch));
                leftEdge.Add(new Vector2(rotated.x - dir * notch, rotated.y + notch));
                leftEdge.Add(new Vector2(rotated.x - dir * notch, rotated.y));
                leftEdge.Add(new Vector2(rotated.x, rotated.y));
            }
            else
            {
                leftEdge.Add(rotated);
            }
        }
        level.cutPaths.Add(DouBao_CreateCutPath(leftEdge.ToArray()));

        // 菱形对角线分割（带咬合）
        List<Vector2> diag1 = new List<Vector2>();
        diag1.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-diamondSize / 2, diamondSize / 2, 0)));
        diag1.Add(Vector2.zero);
        diag1.Add(rotMatrix.MultiplyPoint3x4(new Vector3(diamondSize / 2, -diamondSize / 2, 0)));
        level.cutPaths.Add(DouBao_CreateCutPath(diag1.ToArray()));

        List<Vector2> diag2 = new List<Vector2>();
        diag2.Add(rotMatrix.MultiplyPoint3x4(new Vector3(diamondSize / 2, diamondSize / 2, 0)));
        diag2.Add(Vector2.zero);
        diag2.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-diamondSize / 2, -diamondSize / 2, 0)));
        level.cutPaths.Add(DouBao_CreateCutPath(diag2.ToArray()));
    }

    private void DouBao_GenerateStepWaveHybrid(LevelDesignData level, float L)
    {
        // 阶梯波浪混合咬合（Case39）
        float stepHeight = L * 0.15f;
        int steps = 5;
        float waveAmplitude = L * 0.1f;
        float notch = L * 0.06f;

        // 阶梯+波浪混合线
        List<Vector2> hybridLine = new List<Vector2>();
        hybridLine.Add(new Vector2(-L, L));

        for (int i = 0; i < steps; i++)
        {
            float x = -L + (i + 1) * L * 2 / (steps + 1);
            float baseY = L - (i + 1) * stepHeight * 2;

            // 阶梯部分
            hybridLine.Add(new Vector2(x, baseY + stepHeight));
            hybridLine.Add(new Vector2(x, baseY));

            // 波浪部分（叠加在阶梯上）
            int wavePoints = 4;
            for (int j = 1; j <= wavePoints; j++)
            {
                float t = j / (float)(wavePoints + 1);
                float waveX = x - L * 2 / (steps + 1) + t * L * 2 / (steps + 1);
                float waveY = baseY + Mathf.Sin(t * Mathf.PI * 2) * waveAmplitude;

                // 混合咬合齿
                if (j % 2 == 0)
                {
                    hybridLine.Add(new Vector2(waveX - notch, waveY));
                    hybridLine.Add(new Vector2(waveX - notch, waveY + notch));
                    hybridLine.Add(new Vector2(waveX, waveY + notch));
                    hybridLine.Add(new Vector2(waveX, waveY));
                }
                else
                {
                    hybridLine.Add(new Vector2(waveX, waveY));
                }
            }
        }

        hybridLine.Add(new Vector2(L, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(hybridLine.ToArray()));

        // 正交混合线
        List<Vector2> hybridLine2 = new List<Vector2>();
        hybridLine2.Add(new Vector2(L, L));

        for (int i = 0; i < steps; i++)
        {
            float y = L - (i + 1) * L * 2 / (steps + 1);
            float baseX = -L + (i + 1) * stepHeight * 2;

            hybridLine2.Add(new Vector2(baseX - stepHeight, y));
            hybridLine2.Add(new Vector2(baseX, y));

            int wavePoints = 4;
            for (int j = 1; j <= wavePoints; j++)
            {
                float t = j / (float)(wavePoints + 1);
                float waveY = y - L * 2 / (steps + 1) + t * L * 2 / (steps + 1);
                float waveX = baseX + Mathf.Cos(t * Mathf.PI * 2) * waveAmplitude;

                if (j % 2 == 0)
                {
                    hybridLine2.Add(new Vector2(waveX, waveY - notch));
                    hybridLine2.Add(new Vector2(waveX + notch, waveY - notch));
                    hybridLine2.Add(new Vector2(waveX + notch, waveY));
                    hybridLine2.Add(new Vector2(waveX, waveY));
                }
                else
                {
                    hybridLine2.Add(new Vector2(waveX, waveY));
                }
            }
        }

        hybridLine2.Add(new Vector2(-L, -L));
        level.cutPaths.Add(DouBao_CreateCutPath(hybridLine2.ToArray()));
    }

    private void DouBao_GenerateRandomMediumPuzzle(LevelDesignData level, int levelNum, float L)
    {
        // 中等随机混合拼图生成函数（Case40 & 默认）
        // 固定随机种子，保证每个关卡的随机性一致
        Random.InitState(levelNum * 100 + 40);

        // 定义可选的咬合样式类型
        List<int> styleTypes = new List<int>() { 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 };
        // 随机选择2-3种不同的咬合样式组合
        int styleCount = Random.Range(2, 4);
        List<int> selectedStyles = new List<int>();

        while (selectedStyles.Count < styleCount)
        {
            int randomStyle = styleTypes[Random.Range(0, styleTypes.Count)];
            if (!selectedStyles.Contains(randomStyle))
            {
                selectedStyles.Add(randomStyle);
            }
        }

        // 随机缩放因子（0.8-1.2倍），增加多样性
        float scaleFactor = Random.Range(0.8f, 1.2f);
        float scaledL = L * scaleFactor;

        // 随机旋转角度（0-90度）
        float rotationAngle = Random.Range(0f, 90f) * Mathf.Deg2Rad;
        Matrix4x4 rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotationAngle * Mathf.Rad2Deg));

        // 根据选中的样式生成混合拼图
        foreach (int style in selectedStyles)
        {
            switch (style)
            {
                case 24: // 不规则锯齿边
                    GenerateJaggedEdgeInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 25: // 四角星
                    GenerateStarShapeInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 26: // T字形
                    GenerateTShapeAsymmetric_Mixed(level, scaledL, rotMatrix);
                    break;
                case 27: // L形复杂
                    GenerateLShapedComplex_Mixed(level, scaledL, rotMatrix);
                    break;
                case 28: // H形复杂
                    GenerateHShapedComplex_Mixed(level, scaledL, rotMatrix);
                    break;
                case 29: // 风车
                    GenerateWindmillInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 30: // 2x2全咬合
                    Generate2x2FullInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 31: // 正交波浪
                    GenerateDualWaveInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 32: // 多片圆形
                    GenerateMultiSliceCircle_Mixed(level, scaledL, rotMatrix);
                    break;
                case 33: // 异形四边形
                    GenerateIrregularQuad_Mixed(level, scaledL, rotMatrix);
                    break;
                case 34: // 8瓣花瓣
                    GeneratePetalShape8_Mixed(level, scaledL, rotMatrix);
                    break;
                case 35: // 交叉斜线
                    GenerateCrossSlantInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 36: // 4模块
                    GenerateModular4Type_Mixed(level, scaledL, rotMatrix);
                    break;
                case 37: // 齿轮状
                    GenerateGearInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 38: // 菱形波浪
                    GenerateDiamondWaveInterlock_Mixed(level, scaledL, rotMatrix);
                    break;
                case 39: // 阶梯波浪
                    GenerateStepWaveHybrid_Mixed(level, scaledL, rotMatrix);
                    break;
            }
        }

        // 添加随机偏移的中心咬合结构，增强混合效果
        AddRandomCenterInterlock(level, L);
    }

    // ==================== 混合模式下的样式生成辅助函数 ====================
    private void GenerateJaggedEdgeInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        int steps = 12;
        float minNotch = L * 0.06f;
        float maxNotch = L * 0.15f;

        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(new Vector2(-L, 0));

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = -L + t * L * 2;
            float notchHeight = Random.Range(minNotch, maxNotch);
            bool up = Random.value > 0.5f;
            float y = up ? notchHeight : -notchHeight;
            float offsetX = Random.Range(-L * 0.01f, L * 0.01f);

            Vector2 point = new Vector2(x + offsetX, y);
            // 应用旋转
            point = rotMatrix.MultiplyPoint3x4(new Vector3(point.x, point.y, 0));
            horizontal.Add(point);
        }

        Vector2 endPoint = rotMatrix.MultiplyPoint3x4(new Vector3(L, 0, 0));
        horizontal.Add(endPoint);
        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));
    }

    private void GenerateStarShapeInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float outerRadius = L * 0.7f;
        float innerRadius = L * 0.25f;
        float notch = L * 0.06f;
        int points = 4;

        List<Vector2> star = new List<Vector2>();
        for (int i = 0; i < 2 * points; i++)
        {
            float angle = i * Mathf.PI / points;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;

            Vector2 point = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            // 应用旋转
            point = rotMatrix.MultiplyPoint3x4(new Vector3(point.x, point.y, 0));
            star.Add(point);
        }
        star.Add(star[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(star.ToArray()));
    }

    private void GenerateTShapeAsymmetric_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float stemWidth = L * 0.18f;
        float crossLength = L * 0.7f;
        float notch = L * 0.08f;

        List<Vector2> tShape = new List<Vector2>();
        // T字上横
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-crossLength, L * 0.5f, 0)));
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(crossLength, L * 0.5f, 0)));
        // T字竖杆
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, L * 0.5f, 0)));
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, -L * 0.4f, 0)));

        // 添加咬合齿
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-stemWidth, L * 0.5f, 0)));
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-stemWidth, L * 0.5f + notch, 0)));
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(stemWidth, L * 0.5f + notch, 0)));
        tShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(stemWidth, L * 0.5f, 0)));

        level.cutPaths.Add(DouBao_CreateCutPath(tShape.ToArray()));
    }

    private void GenerateLShapedComplex_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float armLength = L * 0.7f;
        float armWidth = L * 0.25f;

        List<Vector2> lShape = new List<Vector2>();
        lShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-armLength, armLength, 0)));
        lShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-armWidth, armLength, 0)));
        lShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-armWidth, armWidth, 0)));
        lShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-armLength, armWidth, 0)));
        lShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-armLength, armLength, 0)));

        level.cutPaths.Add(DouBao_CreateCutPath(lShape.ToArray()));
    }

    private void GenerateHShapedComplex_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float barWidth = L * 0.15f;
        float barHeight = L * 0.6f;

        List<Vector2> hShape = new List<Vector2>();
        // 左竖杆
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L * 0.4f, barHeight, 0)));
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L * 0.4f, -barHeight, 0)));
        // 右竖杆
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L * 0.4f, barHeight, 0)));
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L * 0.4f, -barHeight, 0)));
        // 上横杠
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L * 0.4f, L * 0.2f, 0)));
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L * 0.4f, L * 0.2f, 0)));
        // 下横杠
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L * 0.4f, -L * 0.2f, 0)));
        hShape.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L * 0.4f, -L * 0.2f, 0)));

        level.cutPaths.Add(DouBao_CreateCutPath(hShape.ToArray()));
    }

    private void GenerateWindmillInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float radius = L * 0.7f;
        int blades = 4;

        for (int i = 0; i < blades; i++)
        {
            float angle = i * Mathf.PI / 2;
            List<Vector2> blade = new List<Vector2>();
            blade.Add(Vector2.zero);
            Vector2 bladeTip = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            bladeTip = rotMatrix.MultiplyPoint3x4(new Vector3(bladeTip.x, bladeTip.y, 0));
            blade.Add(bladeTip);
            level.cutPaths.Add(DouBao_CreateCutPath(blade.ToArray()));
        }
    }

    private void Generate2x2FullInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float half = L / 2;
        float notch = L * 0.1f;

        // 水平切割线
        List<Vector2> horizontal = new List<Vector2>();
        horizontal.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L, 0, 0)));
        horizontal.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-half - notch, 0, 0)));
        horizontal.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-half - notch, notch, 0)));
        horizontal.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-half + notch, notch, 0)));
        horizontal.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-half + notch, 0, 0)));
        horizontal.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L, 0, 0)));

        level.cutPaths.Add(DouBao_CreateCutPath(horizontal.ToArray()));

        // 垂直切割线
        List<Vector2> vertical = new List<Vector2>();
        vertical.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, L, 0)));
        vertical.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, half - notch, 0)));
        vertical.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-notch, half - notch, 0)));
        vertical.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-notch, half + notch, 0)));
        vertical.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, half + notch, 0)));
        vertical.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, -L, 0)));

        level.cutPaths.Add(DouBao_CreateCutPath(vertical.ToArray()));
    }

    private void GenerateDualWaveInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        int segments = 10;
        float amplitude = L * 0.12f;

        List<Vector2> wave = new List<Vector2>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float x = -L + t * L * 2;
            float y = Mathf.Sin(t * Mathf.PI * 3) * amplitude;

            Vector2 point = rotMatrix.MultiplyPoint3x4(new Vector3(x, y, 0));
            wave.Add(point);
        }

        level.cutPaths.Add(DouBao_CreateCutPath(wave.ToArray()));
    }

    private void GenerateMultiSliceCircle_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float outerRadius = L * 0.7f;
        int slices = 6;

        for (int i = 0; i < slices; i++)
        {
            float angle1 = i * Mathf.PI * 2 / slices;
            float angle2 = (i + 1) * Mathf.PI * 2 / slices;

            List<Vector2> slice = new List<Vector2>();
            slice.Add(Vector2.zero);
            slice.Add(rotMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle1) * outerRadius, Mathf.Sin(angle1) * outerRadius, 0)));
            slice.Add(rotMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle2) * outerRadius, Mathf.Sin(angle2) * outerRadius, 0)));
            slice.Add(Vector2.zero);

            level.cutPaths.Add(DouBao_CreateCutPath(slice.ToArray()));
        }
    }

    private void GenerateIrregularQuad_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        // 随机生成凸四边形顶点
        Vector2 p1 = new Vector2(-L + Random.Range(0, L * 0.15f), L - Random.Range(0, L * 0.15f));
        Vector2 p2 = new Vector2(L - Random.Range(0, L * 0.15f), L - Random.Range(0, L * 0.15f));
        Vector2 p3 = new Vector2(L - Random.Range(0, L * 0.15f), -L + Random.Range(0, L * 0.15f));
        Vector2 p4 = new Vector2(-L + Random.Range(0, L * 0.15f), -L + Random.Range(0, L * 0.15f));

        // 应用旋转
        p1 = rotMatrix.MultiplyPoint3x4(new Vector3(p1.x, p1.y, 0));
        p2 = rotMatrix.MultiplyPoint3x4(new Vector3(p2.x, p2.y, 0));
        p3 = rotMatrix.MultiplyPoint3x4(new Vector3(p3.x, p3.y, 0));
        p4 = rotMatrix.MultiplyPoint3x4(new Vector3(p4.x, p4.y, 0));

        List<Vector2> quad = new List<Vector2>() { p1, p2, p3, p4, p1 };
        level.cutPaths.Add(DouBao_CreateCutPath(quad.ToArray()));
    }

    private void GeneratePetalShape8_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        // 1. 修复变量名冲突：将花瓣数量的变量名改为 petalCount
        float outerRadius = L * 0.7f;
        float innerRadius = L * 0.2f;
        int petalCount = 8; // 原 petals → 改为 petalCount，避免和列表名冲突

        // 2. 顶点列表保留原名 petals（语义更贴合）
        List<Vector2> petals = new List<Vector2>();

        // 3. 循环条件同步改为 petalCount
        for (int i = 0; i < 2 * petalCount; i++)
        {
            float angle = i * Mathf.PI / petalCount; // 这里也同步替换
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;

            Vector2 point = rotMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
            petals.Add(point);
        }

        petals.Add(petals[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(petals.ToArray()));
    }

    private void GenerateCrossSlantInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float slope1 = Random.Range(0.8f, 1.5f);
        float slope2 = -Random.Range(0.6f, 1.2f);

        // 第一条斜线
        List<Vector2> slant1 = new List<Vector2>();
        slant1.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L, -L * slope1, 0)));
        slant1.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L, L * slope1, 0)));

        // 第二条斜线
        List<Vector2> slant2 = new List<Vector2>();
        slant2.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L, L * Mathf.Abs(slope2), 0)));
        slant2.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L, L * slope2, 0)));

        level.cutPaths.Add(DouBao_CreateCutPath(slant1.ToArray()));
        level.cutPaths.Add(DouBao_CreateCutPath(slant2.ToArray()));
    }

    private void GenerateModular4Type_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float moduleSize = L * 0.35f;

        // 四个基础模块位置
        Vector2[] moduleCenters = new Vector2[]
        {
        new Vector2(-L/3, L/3),
        new Vector2(L/3, L/3),
        new Vector2(-L/3, -L/3),
        new Vector2(L/3, -L/3)
        };

        foreach (Vector2 center in moduleCenters)
        {
            List<Vector2> module = new List<Vector2>();
            // 方形模块
            module.Add(rotMatrix.MultiplyPoint3x4(new Vector3(center.x - moduleSize / 2, center.y + moduleSize / 2, 0)));
            module.Add(rotMatrix.MultiplyPoint3x4(new Vector3(center.x + moduleSize / 2, center.y + moduleSize / 2, 0)));
            module.Add(rotMatrix.MultiplyPoint3x4(new Vector3(center.x + moduleSize / 2, center.y - moduleSize / 2, 0)));
            module.Add(rotMatrix.MultiplyPoint3x4(new Vector3(center.x - moduleSize / 2, center.y - moduleSize / 2, 0)));
            module.Add(rotMatrix.MultiplyPoint3x4(new Vector3(center.x - moduleSize / 2, center.y + moduleSize / 2, 0)));

            level.cutPaths.Add(DouBao_CreateCutPath(module.ToArray()));
        }
    }

    private void GenerateGearInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float outerRadius = L * 0.6f;
        int teethCount = 10;
        float toothHeight = L * 0.12f;

        List<Vector2> gear = new List<Vector2>();
        for (int i = 0; i < teethCount; i++)
        {
            float angle1 = i * Mathf.PI * 2 / teethCount;
            float angle2 = (i + 1) * Mathf.PI * 2 / teethCount;
            float midAngle = (angle1 + angle2) / 2;

            gear.Add(rotMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle1) * outerRadius, Mathf.Sin(angle1) * outerRadius, 0)));
            gear.Add(rotMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(midAngle) * (outerRadius + toothHeight), Mathf.Sin(midAngle) * (outerRadius + toothHeight), 0)));
            gear.Add(rotMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle2) * outerRadius, Mathf.Sin(angle2) * outerRadius, 0)));
        }
        gear.Add(gear[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(gear.ToArray()));
    }

    private void GenerateDiamondWaveInterlock_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float diamondSize = L * 0.7f;
        int waveSegments = 8;
        float amplitude = L * 0.08f;

        List<Vector2> diamond = new List<Vector2>();
        // 菱形上边（带波浪）
        for (int i = 0; i <= waveSegments; i++)
        {
            float t = i / (float)waveSegments;
            Vector2 basePoint = new Vector2(-diamondSize / 2 + t * diamondSize, diamondSize / 2);
            basePoint.y += Mathf.Sin(t * Mathf.PI * 3) * amplitude;

            diamond.Add(rotMatrix.MultiplyPoint3x4(new Vector3(basePoint.x, basePoint.y, 0)));
        }

        level.cutPaths.Add(DouBao_CreateCutPath(diamond.ToArray()));
    }

    private void GenerateStepWaveHybrid_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float stepHeight = L * 0.12f;
        int steps = 4;
        float waveAmplitude = L * 0.08f;

        List<Vector2> hybrid = new List<Vector2>();
        hybrid.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L, L, 0)));

        for (int i = 0; i < steps; i++)
        {
            float x = -L + (i + 1) * L * 2 / (steps + 1);
            float baseY = L - (i + 1) * stepHeight * 2;
            // 阶梯部分
            hybrid.Add(rotMatrix.MultiplyPoint3x4(new Vector3(x, baseY + stepHeight, 0)));
            hybrid.Add(rotMatrix.MultiplyPoint3x4(new Vector3(x, baseY, 0)));
            // 波浪叠加
            float waveY = baseY + Mathf.Sin(i * Mathf.PI / 2) * waveAmplitude;
            hybrid.Add(rotMatrix.MultiplyPoint3x4(new Vector3(x, waveY, 0)));
        }

        hybrid.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L, -L, 0)));
        level.cutPaths.Add(DouBao_CreateCutPath(hybrid.ToArray()));
    }

    // 随机中心咬合结构辅助函数
    private void AddRandomCenterInterlock(LevelDesignData level, float L)
    {
        float centerRadius = L * 0.15f;
        int centerTeeth = Random.Range(4, 8);

        List<Vector2> center = new List<Vector2>();
        for (int i = 0; i < centerTeeth; i++)
        {
            float angle = i * Mathf.PI * 2 / centerTeeth;
            float r = (i % 2 == 0) ? centerRadius : centerRadius * 0.7f;

            center.Add(new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r));
        }
        center.Add(center[0]);

        level.cutPaths.Add(DouBao_CreateCutPath(center.ToArray()));
    }


    // ==================== Case41：复杂迷宫拼图（核心实现） ====================
    private void DouBao_GenerateComplexMazePuzzle(LevelDesignData level, float L)
    {
        level.description = "复杂迷宫嵌套拼图";
        level.difficulty = 4.5f; // 高难度

        // 迷宫基础参数
        int gridSize = 8; // 迷宫网格大小
        float cellSize = L * 2 / gridSize; // 每个单元格尺寸
        float wallWidth = cellSize * 0.15f; // 墙体宽度（咬合齿基础）
        float notch = wallWidth * 0.6f; // 墙体咬合齿大小

        // 固定随机种子保证迷宫一致性
        Random.InitState(41);

        // 1. 生成迷宫外框（带咬合齿）
        List<Vector2> outerWall = new List<Vector2>();
        // 外框上边
        outerWall.Add(new Vector2(-L, L));
        for (int i = 1; i < gridSize; i++)
        {
            float x = -L + i * cellSize;
            // 交替添加咬合齿
            bool up = i % 2 == 0;
            outerWall.Add(new Vector2(x - notch, L));
            outerWall.Add(new Vector2(x - notch, L + (up ? notch : -notch)));
            outerWall.Add(new Vector2(x, L + (up ? notch : -notch)));
            outerWall.Add(new Vector2(x, L));
        }
        outerWall.Add(new Vector2(L, L));

        // 外框右边
        for (int i = 1; i < gridSize; i++)
        {
            float y = L - i * cellSize;
            bool right = i % 2 == 0;
            outerWall.Add(new Vector2(L, y + notch));
            outerWall.Add(new Vector2(L + (right ? notch : -notch), y + notch));
            outerWall.Add(new Vector2(L + (right ? notch : -notch), y));
            outerWall.Add(new Vector2(L, y));
        }
        outerWall.Add(new Vector2(L, -L));

        // 外框下边
        for (int i = gridSize - 1; i > 0; i--)
        {
            float x = -L + i * cellSize;
            bool down = i % 2 == 0;
            outerWall.Add(new Vector2(x + notch, -L));
            outerWall.Add(new Vector2(x + notch, -L - (down ? notch : -notch)));
            outerWall.Add(new Vector2(x, -L - (down ? notch : -notch)));
            outerWall.Add(new Vector2(x, -L));
        }
        outerWall.Add(new Vector2(-L, -L));

        // 外框左边
        for (int i = gridSize - 1; i > 0; i--)
        {
            float y = L - i * cellSize;
            bool left = i % 2 == 0;
            outerWall.Add(new Vector2(-L, y - notch));
            outerWall.Add(new Vector2(-L - (left ? notch : -notch), y - notch));
            outerWall.Add(new Vector2(-L - (left ? notch : -notch), y));
            outerWall.Add(new Vector2(-L, y));
        }
        outerWall.Add(new Vector2(-L, L)); // 闭合外框
        level.cutPaths.Add(DouBao_CreateCutPath(outerWall.ToArray()));

        // 2. 生成迷宫内部墙体（随机路径+嵌套+咬合）
        // 存储已生成的墙体位置，避免重叠
        HashSet<string> wallPositions = new HashSet<string>();

        // 生成水平墙体
        for (int row = 1; row < gridSize; row += 2) // 隔行生成，保证通道
        {
            float y = L - row * cellSize;
            for (int col = 0; col < gridSize; col++)
            {
                // 随机生成墙体（70%概率生成）
                if (Random.value < 0.7f && !wallPositions.Contains($"H_{row}_{col}"))
                {
                    float x1 = -L + col * cellSize;
                    float x2 = -L + (col + 1) * cellSize;

                    List<Vector2> horzWall = new List<Vector2>();
                    horzWall.Add(new Vector2(x1, y));

                    // 墙体中间添加咬合齿
                    float midX = (x1 + x2) / 2;
                    bool up = Random.value > 0.5f;
                    horzWall.Add(new Vector2(midX - notch / 2, y));
                    horzWall.Add(new Vector2(midX - notch / 2, y + (up ? notch / 2 : -notch / 2)));
                    horzWall.Add(new Vector2(midX + notch / 2, y + (up ? notch / 2 : -notch / 2)));
                    horzWall.Add(new Vector2(midX + notch / 2, y));

                    horzWall.Add(new Vector2(x2, y));
                    level.cutPaths.Add(DouBao_CreateCutPath(horzWall.ToArray()));
                    wallPositions.Add($"H_{row}_{col}");
                }
            }
        }

        // 生成垂直墙体
        for (int col = 1; col < gridSize; col += 2) // 隔列生成
        {
            float x = -L + col * cellSize;
            for (int row = 0; row < gridSize; row++)
            {
                if (Random.value < 0.7f && !wallPositions.Contains($"V_{row}_{col}"))
                {
                    float y1 = L - row * cellSize;
                    float y2 = L - (row + 1) * cellSize;

                    List<Vector2> vertWall = new List<Vector2>();
                    vertWall.Add(new Vector2(x, y1));

                    // 墙体中间添加咬合齿
                    float midY = (y1 + y2) / 2;
                    bool right = Random.value > 0.5f;
                    vertWall.Add(new Vector2(x, midY + notch / 2));
                    vertWall.Add(new Vector2(x + (right ? notch / 2 : -notch / 2), midY + notch / 2));
                    vertWall.Add(new Vector2(x + (right ? notch / 2 : -notch / 2), midY - notch / 2));
                    vertWall.Add(new Vector2(x, midY - notch / 2));

                    vertWall.Add(new Vector2(x, y2));
                    level.cutPaths.Add(DouBao_CreateCutPath(vertWall.ToArray()));
                    wallPositions.Add($"V_{row}_{col}");
                }
            }
        }

        // 3. 生成嵌套迷宫核心（中心区域复杂路径）
        float coreSize = L * 0.4f;
        List<Vector2> coreMaze = new List<Vector2>();
        // 中心螺旋路径
        int spiralTurns = 4;
        float spiralStep = coreSize / (spiralTurns * 4);
        float currentRadius = spiralStep;
        float currentAngle = 0;

        coreMaze.Add(Vector2.zero);
        for (int i = 0; i < spiralTurns * 4; i++)
        {
            currentAngle += Mathf.PI / 2;
            float x = Mathf.Cos(currentAngle) * currentRadius;
            float y = Mathf.Sin(currentAngle) * currentRadius;

            // 螺旋路径添加咬合齿
            if (i % 2 == 0)
            {
                coreMaze.Add(new Vector2(x - spiralStep / 2, y));
                coreMaze.Add(new Vector2(x - spiralStep / 2, y + (i % 4 == 0 ? spiralStep / 2 : -spiralStep / 2)));
                coreMaze.Add(new Vector2(x, y + (i % 4 == 0 ? spiralStep / 2 : -spiralStep / 2)));
                coreMaze.Add(new Vector2(x, y));
            }
            else
            {
                coreMaze.Add(new Vector2(x, y));
            }

            currentRadius += spiralStep;
        }
        coreMaze.Add(Vector2.zero);
        level.cutPaths.Add(DouBao_CreateCutPath(coreMaze.ToArray()));
    }

    // ==================== 随机复杂拼图函数（Default分支） ====================
    private void DouBao_GenerateRandomComplexPuzzle(LevelDesignData level, int levelNum, float L)
    {
        level.description = $"复杂拼图 #{levelNum}";
        level.difficulty = 4f + Random.Range(0f, 1f); // 难度4.0-5.0

        // 固定种子保证每个关卡的随机性一致
        Random.InitState(levelNum * 1000 + 66);

        // 随机选择2-3种高难度样式组合
        List<int> complexStyles = new List<int>() { 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65 };
        int styleCount = Random.Range(2, 4);
        List<int> selectedStyles = new List<int>();

        while (selectedStyles.Count < styleCount)
        {
            int randomStyle = complexStyles[Random.Range(0, complexStyles.Count)];
            if (!selectedStyles.Contains(randomStyle))
            {
                selectedStyles.Add(randomStyle);
            }
        }

        // 随机变换参数
        float scale = Random.Range(0.7f, 1.1f);
        float rotation = Random.Range(0f, 180f) * Mathf.Deg2Rad;
        Matrix4x4 rotMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation));

        // 根据选中的样式生成混合复杂拼图
        foreach (int style in selectedStyles)
        {
            switch (style)
            {
                case 41: // 迷宫
                    GenerateMaze_Mixed(level, L * scale, rotMatrix);
                    break;
                case 42: // 雪花
                    GenerateSnowflake_Mixed(level, L * scale, rotMatrix);
                    break;
                case 43: // 中国结
                    GenerateChineseKnot_Mixed(level, L * scale, rotMatrix);
                    break;
                case 44: // 太极
                    GenerateTaiji_Mixed(level, L * scale, rotMatrix);
                    break;
                case 45: // 螺旋星系
                    GenerateSpiralGalaxy_Mixed(level, L * scale, rotMatrix);
                    break;
                case 50: // 蜂巢
                    GenerateHoneycomb_Mixed(level, L * scale, rotMatrix);
                    break;
                case 53: // 齿轮组
                    GenerateGearSet_Mixed(level, L * scale, rotMatrix);
                    break;
                default: // 其他样式默认用迷宫混合
                    GenerateMaze_Mixed(level, L * scale, rotMatrix);
                    break;
            }
        }

        // 添加随机核心咬合结构
        AddComplexCenterInterlock(level, L);
    }


    // 迷宫混合模式辅助函数
    private void GenerateMaze_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        int gridSize = 6;
        float cellSize = L * 2 / gridSize;

        // 简化迷宫墙体
        for (int row = 1; row < gridSize; row += 2)
        {
            float y = L - row * cellSize;
            List<Vector2> wall = new List<Vector2>();
            wall.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-L, y, 0)));
            wall.Add(rotMatrix.MultiplyPoint3x4(new Vector3(L, y, 0)));
            level.cutPaths.Add(DouBao_CreateCutPath(wall.ToArray()));
        }
    }

    // 雪花混合模式辅助函数
    private void GenerateSnowflake_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float radius = L * 0.7f;
        int branches = 6;

        List<Vector2> snowflake = new List<Vector2>();
        for (int i = 0; i < branches; i++)
        {
            float angle = i * Mathf.PI * 2 / branches;
            Vector2 tip = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            tip = rotMatrix.MultiplyPoint3x4(new Vector3(tip.x, tip.y, 0));
            snowflake.Add(tip);
        }
        snowflake.Add(snowflake[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(snowflake.ToArray()));
    }

    // 中国结混合模式辅助函数
    private void GenerateChineseKnot_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float size = L * 0.5f;
        // 简化中国结轮廓
        List<Vector2> knot = new List<Vector2>();
        // 左上结
        knot.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-size, size, 0)));
        knot.Add(rotMatrix.MultiplyPoint3x4(new Vector3(-size / 2, size + size / 2, 0)));
        knot.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, size, 0)));
        // 右上结
        knot.Add(rotMatrix.MultiplyPoint3x4(new Vector3(size, size, 0)));
        knot.Add(rotMatrix.MultiplyPoint3x4(new Vector3(size / 2, size + size / 2, 0)));
        knot.Add(rotMatrix.MultiplyPoint3x4(new Vector3(0, size, 0)));
        level.cutPaths.Add(DouBao_CreateCutPath(knot.ToArray()));
    }

    // 太极混合模式辅助函数
    private void GenerateTaiji_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float radius = L * 0.6f;
        // 简化太极轮廓
        List<Vector2> taiji = new List<Vector2>();
        // 外圆
        for (int i = 0; i <= 16; i++)
        {
            float angle = i * Mathf.PI * 2 / 16;
            Vector2 point = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            point = rotMatrix.MultiplyPoint3x4(new Vector3(point.x, point.y, 0));
            taiji.Add(point);
        }
        level.cutPaths.Add(DouBao_CreateCutPath(taiji.ToArray()));
    }

    // 螺旋星系混合模式辅助函数
    private void GenerateSpiralGalaxy_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        int segments = 20;
        List<Vector2> spiral = new List<Vector2>();

        spiral.Add(Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 4;
            float radius = t * L * 0.8f;

            Vector2 point = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            point = rotMatrix.MultiplyPoint3x4(new Vector3(point.x, point.y, 0));
            spiral.Add(point);
        }
        level.cutPaths.Add(DouBao_CreateCutPath(spiral.ToArray()));
    }

    // 蜂巢混合模式辅助函数
    private void GenerateHoneycomb_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        float hexSize = L * 0.2f;
        // 基础六边形顶点
        Vector2[] hexPoints = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI / 3;
            hexPoints[i] = new Vector2(Mathf.Cos(angle) * hexSize, Mathf.Sin(angle) * hexSize);
        }

        // 生成蜂巢网格
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                float offsetX = x * hexSize * 1.5f;
                float offsetY = y * hexSize * Mathf.Sqrt(3) + (x % 2 == 0 ? 0 : hexSize * Mathf.Sqrt(3) / 2);

                List<Vector2> hex = new List<Vector2>();
                foreach (Vector2 p in hexPoints)
                {
                    Vector2 point = new Vector2(p.x + offsetX, p.y + offsetY);
                    point = rotMatrix.MultiplyPoint3x4(new Vector3(point.x, point.y, 0));
                    hex.Add(point);
                }
                hex.Add(hex[0]);
                level.cutPaths.Add(DouBao_CreateCutPath(hex.ToArray()));
            }
        }
    }

    // 齿轮组混合模式辅助函数
    private void GenerateGearSet_Mixed(LevelDesignData level, float L, Matrix4x4 rotMatrix)
    {
        // 中心大齿轮
        float bigRadius = L * 0.4f;
        List<Vector2> bigGear = new List<Vector2>();
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2 / 12;
            bigGear.Add(rotMatrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle) * bigRadius, Mathf.Sin(angle) * bigRadius, 0)));
        }
        bigGear.Add(bigGear[0]);
        level.cutPaths.Add(DouBao_CreateCutPath(bigGear.ToArray()));

        // 周围小齿轮
        float smallRadius = L * 0.15f;
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2 / 6;
            Vector2 center = new Vector2(Mathf.Cos(angle) * (bigRadius + smallRadius), Mathf.Sin(angle) * (bigRadius + smallRadius));

            List<Vector2> smallGear = new List<Vector2>();
            for (int j = 0; j < 6; j++)
            {
                float gearAngle = j * Mathf.PI * 2 / 6;
                Vector2 point = new Vector2(
                    center.x + Mathf.Cos(gearAngle) * smallRadius,
                    center.y + Mathf.Sin(gearAngle) * smallRadius
                );
                point = rotMatrix.MultiplyPoint3x4(new Vector3(point.x, point.y, 0));
                smallGear.Add(point);
            }
            smallGear.Add(smallGear[0]);
            level.cutPaths.Add(DouBao_CreateCutPath(smallGear.ToArray()));
        }
    }

    // 复杂中心咬合结构
    private void AddComplexCenterInterlock(LevelDesignData level, float L)
    {
        float radius = L * 0.2f;
        int layers = 3;

        for (int layer = 1; layer <= layers; layer++)
        {
            float layerRadius = radius * layer;
            List<Vector2> center = new List<Vector2>();

            for (int i = 0; i < 8 * layer; i++)
            {
                float angle = i * Mathf.PI * 2 / (8 * layer);
                float r = (i % 2 == 0) ? layerRadius : layerRadius * 0.8f;
                center.Add(new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r));
            }
            center.Add(center[0]);
            level.cutPaths.Add(DouBao_CreateCutPath(center.ToArray()));
        }
    }
    // ==================== Case42：雪花形分形咬合拼图 ====================
    private void DouBao_GenerateSnowflakeFractalPuzzle(LevelDesignData level, float L)
    {
        level.description = "雪花形分形咬合拼图";
        level.difficulty = 4.2f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        // 分形雪花参数
        float baseRadius = L * 0.8f;
        int iterations = 3; // 分形迭代次数（越大越复杂）
        float notch = L * 0.05f; // 咬合齿大小

        // 生成6个主分支（雪花基础结构）
        for (int branch = 0; branch < 6; branch++)
        {
            float mainAngle = branch * Mathf.PI / 3;
            List<Vector2> branchPoints = new List<Vector2>();
            
            // 分形生成分支
            GenerateSnowflakeBranch(branchPoints, Vector2.zero, mainAngle, baseRadius, iterations, notch);
            level.cutPaths.Add(DouBao_CreateCutPath(branchPoints.ToArray()));
        }

        // 生成雪花外轮廓（带咬合齿）
        List<Vector2> outerContour = new List<Vector2>();
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI / 6;
            float radius = (i % 2 == 0) ? baseRadius : baseRadius * 0.7f;
            // 咬合齿：交替凹凸
            float toothOffset = (i % 3 == 0) ? notch : -notch;
            Vector2 point = new Vector2(
                Mathf.Cos(angle) * (radius + toothOffset),
                Mathf.Sin(angle) * (radius + toothOffset)
            );
            outerContour.Add(point);
        }
        level.cutPaths.Add(DouBao_CreateCutPath(outerContour.ToArray()));
    }

    // 雪花分形分支生成辅助函数
    private void GenerateSnowflakeBranch(List<Vector2> points, Vector2 start, float angle, float length, int iterations, float notch)
{
    if (iterations == 0)
    {
        // 递归终止：生成最细分支（带咬合齿）
        Vector2 end = new Vector2(
            start.x + Mathf.Cos(angle) * length,
            start.y + Mathf.Sin(angle) * length
        );
        
        // 1. 左侧咬合齿 → 起点 → 右侧咬合齿 → 终点（完整的分支段）
        points.Add(new Vector2(
            start.x + Mathf.Cos(angle + Mathf.PI/6) * notch,
            start.y + Mathf.Sin(angle + Mathf.PI/6) * notch
        ));
        points.Add(start);
        points.Add(new Vector2(
            start.x + Mathf.Cos(angle - Mathf.PI/6) * notch,
            start.y + Mathf.Sin(angle - Mathf.PI/6) * notch
        ));
        
        // 2. 终点也添加咬合齿（对称），解决end未使用的问题
        points.Add(new Vector2(
            end.x + Mathf.Cos(angle + Mathf.PI/6) * notch,
            end.y + Mathf.Sin(angle + Mathf.PI/6) * notch
        ));
        points.Add(end);
        points.Add(new Vector2(
            end.x + Mathf.Cos(angle - Mathf.PI/6) * notch,
            end.y + Mathf.Sin(angle - Mathf.PI/6) * notch
        ));
        
        return;
    }

    // 分形递归：经典科赫雪花算法（修正坐标计算）
    float newLength = length / 3;
    
    // 主分支上的三个关键点（1/3、2/3处）
    Vector2 p1 = new Vector2( // 主分支1/3处
        start.x + Mathf.Cos(angle) * newLength,
        start.y + Mathf.Sin(angle) * newLength
    );
    Vector2 p2 = new Vector2( // 从p1向60度方向延伸newLength（分形凸起）
        p1.x + Mathf.Cos(angle + Mathf.PI/3) * newLength,
        p1.y + Mathf.Sin(angle + Mathf.PI/3) * newLength
    );
    Vector2 p3 = new Vector2( // 从p2向-60度方向延伸newLength（回到主分支2/3处）
        p2.x + Mathf.Cos(angle - Mathf.PI/3) * newLength,
        p2.y + Mathf.Sin(angle - Mathf.PI/3) * newLength
    );

    // 修正递归调用顺序（经典科赫雪花的4段递归）
    GenerateSnowflakeBranch(points, start, angle, newLength, iterations - 1, notch);       // 第一段：start→p1
    GenerateSnowflakeBranch(points, p1, angle + Mathf.PI/3, newLength, iterations - 1, notch); // 第二段：p1→p2
    GenerateSnowflakeBranch(points, p2, angle - Mathf.PI/3, newLength, iterations - 1, notch); // 第三段：p2→p3
    GenerateSnowflakeBranch(points, p3, angle, newLength, iterations - 1, notch);           // 第四段：p3→end（主分支2/3→终点）
}

    // ==================== Case43：中国结缠绕咬合拼图 ====================
    private void DouBao_GenerateChineseKnotPuzzle(LevelDesignData level, float L)
    {
        level.description = "中国结缠绕咬合拼图";
        level.difficulty = 4.3f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float knotSize = L * 0.6f;
        float strandWidth = L * 0.08f; // 结的线宽
        float notch = strandWidth * 0.7f; // 咬合齿大小

        // 中国结核心缠绕结构（8字形基础）
        List<Vector2> knotCore = new List<Vector2>();
        
        // 左上环
        for (int i = 0; i <= 90; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            float x = -knotSize/2 + Mathf.Cos(angle + Mathf.PI) * knotSize/2;
            float y = knotSize/2 + Mathf.Sin(angle + Mathf.PI) * knotSize/2;
            // 添加咬合齿
            float tooth = (i % 20 == 0) ? notch : -notch;
            knotCore.Add(new Vector2(x + tooth, y));
        }
        
        // 右上环
        for (int i = 90; i <= 180; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            float x = knotSize/2 + Mathf.Cos(angle + Mathf.PI) * knotSize/2;
            float y = knotSize/2 + Mathf.Sin(angle + Mathf.PI) * knotSize/2;
            float tooth = (i % 20 == 0) ? notch : -notch;
            knotCore.Add(new Vector2(x, y + tooth));
        }
        
        // 右下环
        for (int i = 180; i <= 270; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            float x = knotSize/2 + Mathf.Cos(angle + Mathf.PI) * knotSize/2;
            float y = -knotSize/2 + Mathf.Sin(angle + Mathf.PI) * knotSize/2;
            float tooth = (i % 20 == 0) ? notch : -notch;
            knotCore.Add(new Vector2(x - tooth, y));
        }
        
        // 左下环
        for (int i = 270; i <= 360; i += 10)
        {
            float angle = i * Mathf.Deg2Rad;
            float x = -knotSize/2 + Mathf.Cos(angle + Mathf.PI) * knotSize/2;
            float y = -knotSize/2 + Mathf.Sin(angle + Mathf.PI) * knotSize/2;
            float tooth = (i % 20 == 0) ? notch : -notch;
            knotCore.Add(new Vector2(x, y - tooth));
        }
        
        level.cutPaths.Add(DouBao_CreateCutPath(knotCore.ToArray()));

        // 生成缠绕的副结（增加复杂度）
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI/2;
            float smallSize = knotSize * 0.3f;
            List<Vector2> smallKnot = new List<Vector2>();
            
            Vector2 center = new Vector2(
                Mathf.Cos(angle) * knotSize * 0.7f,
                Mathf.Sin(angle) * knotSize * 0.7f
            );
            
            for (int j = 0; j <= 360; j += 30)
            {
                float a = j * Mathf.Deg2Rad;
                smallKnot.Add(new Vector2(
                    center.x + Mathf.Cos(a) * smallSize,
                    center.y + Mathf.Sin(a) * smallSize
                ));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(smallKnot.ToArray()));
        }
    }

    // ==================== Case44：太极图曲线嵌套拼图 ====================
    private void DouBao_GenerateTaijiCurvePuzzle(LevelDesignData level, float L)
    {
        level.description = "太极图曲线嵌套拼图";
        level.difficulty = 4.0f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float radius = L * 0.7f;
        float smallRadius = radius * 0.2f;
        float notch = L * 0.04f; // 曲线咬合齿

        // 太极外圆
        List<Vector2> outerCircle = new List<Vector2>();
        for (int i = 0; i <= 360; i += 15)
        {
            float angle = i * Mathf.Deg2Rad;
            // 外圆添加咬合齿（交替凹凸）
            float tooth = (i % 30 == 0) ? notch : -notch;
            outerCircle.Add(new Vector2(
                Mathf.Cos(angle) * (radius + tooth),
                Mathf.Sin(angle) * (radius + tooth)
            ));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(outerCircle.ToArray()));

        // 太极S形曲线（核心嵌套）
        List<Vector2> sCurve = new List<Vector2>();
        for (int i = 0; i <= 360; i += 5)
        {
            float angle = i * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius * 0.5f;
            float y = Mathf.Cos(angle) * radius * 0.5f;
            // S曲线添加咬合齿
            float tooth = (i % 20 == 0) ? notch : -notch;
            sCurve.Add(new Vector2(x + tooth, y));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(sCurve.ToArray()));

        // 阴阳鱼眼（嵌套小圆）
        List<Vector2> eye1 = new List<Vector2>();
        for (int i = 0; i <= 360; i += 30)
        {
            float angle = i * Mathf.Deg2Rad;
            eye1.Add(new Vector2(
                Mathf.Cos(angle) * smallRadius,
                radius * 0.5f + Mathf.Sin(angle) * smallRadius
            ));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(eye1.ToArray()));

        List<Vector2> eye2 = new List<Vector2>();
        for (int i = 0; i <= 360; i += 30)
        {
            float angle = i * Mathf.Deg2Rad;
            eye2.Add(new Vector2(
                Mathf.Cos(angle) * smallRadius,
                -radius * 0.5f + Mathf.Sin(angle) * smallRadius
            ));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(eye2.ToArray()));
    }

    // ==================== Case45：螺旋星系多曲线咬合拼图 ====================
    private void DouBao_GenerateSpiralGalaxyPuzzle(LevelDesignData level, float L)
    {
        level.description = "螺旋星系多曲线咬合";
        level.difficulty = 4.4f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float maxRadius = L * 0.8f;
        int spiralCount = 4; // 螺旋臂数量
        float notch = L * 0.03f;

        // 星系核心（嵌套圆）
        List<Vector2> core = new List<Vector2>();
        for (int i = 0; i <= 360; i += 20)
        {
            float angle = i * Mathf.Deg2Rad;
            float radius = L * 0.15f + Mathf.Sin(i * Mathf.Deg2Rad * 2) * L * 0.05f;
            core.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(core.ToArray()));

        // 多螺旋臂（带咬合齿）
        for (int spiral = 0; spiral < spiralCount; spiral++)
        {
            float startAngle = spiral * Mathf.PI/2;
            List<Vector2> spiralArm = new List<Vector2>();
            
            spiralArm.Add(Vector2.zero); // 核心起点
            for (int i = 0; i <= 100; i += 2)
            {
                float t = i / 100f;
                float angle = startAngle + t * Mathf.PI * 5; // 5圈螺旋
                float radius = t * maxRadius;
                
                // 螺旋臂添加咬合齿（波浪形）
                float tooth = Mathf.Sin(t * Mathf.PI * 10) * notch;
                spiralArm.Add(new Vector2(
                    Mathf.Cos(angle) * (radius + tooth),
                    Mathf.Sin(angle) * (radius + tooth)
                ));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(spiralArm.ToArray()));
        }

        // 外围星云曲线（增加嵌套感）
        for (int i = 0; i < 2; i++)
        {
            float offset = i * Mathf.PI;
            List<Vector2> nebula = new List<Vector2>();
            for (int j = 0; j <= 360; j += 10)
            {
                float angle = j * Mathf.Deg2Rad + offset;
                float radius = maxRadius * 1.1f + Mathf.Sin(j * Mathf.Deg2Rad * 3) * L * 0.08f;
                nebula.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(nebula.ToArray()));
        }
    }

    // ==================== Case46：天际线异形边缘拼图 ====================
    private void DouBao_GenerateSkylineShapePuzzle(LevelDesignData level, float L)
    {
        level.description = "天际线异形边缘拼图";
        level.difficulty = 4.1f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float groundY = -L * 0.5f;
        int buildingCount = Random.Range(8, 12);
        float notch = L * 0.05f; // 建筑边缘咬合齿

        // 地面基线（带咬合齿）
        List<Vector2> ground = new List<Vector2>();
        ground.Add(new Vector2(-L, groundY));
        for (int i = 0; i <= 20; i++)
        {
            float x = -L + i * L * 2 / 20;
            float tooth = Mathf.Sin(i * Mathf.PI/5) * notch;
            ground.Add(new Vector2(x, groundY + tooth));
        }
        ground.Add(new Vector2(L, groundY));
        level.cutPaths.Add(DouBao_CreateCutPath(ground.ToArray()));

        // 随机生成建筑群（异形边缘+咬合齿）
        float xPos = -L;
        float minWidth = L * 2 / (buildingCount * 1.5f);
        float maxWidth = L * 2 / buildingCount;

        for (int i = 0; i < buildingCount; i++)
        {
            float width = Random.Range(minWidth, maxWidth);
            float height = Random.Range(L * 0.2f, L * 0.9f);
            
            // 建筑轮廓（异形顶部）
            List<Vector2> building = new List<Vector2>();
            building.Add(new Vector2(xPos, groundY));
            
            // 建筑左侧（带咬合齿）
            int teeth = Random.Range(2, 5);
            for (int t = 0; t <= teeth; t++)
            {
                float y = groundY + t * height / teeth;
                float tooth = (t % 2 == 0) ? notch : -notch;
                building.Add(new Vector2(xPos + tooth, y));
            }
            
            // 建筑顶部（异形）
            int topPoints = Random.Range(3, 6);
            for (int t = 0; t <= topPoints; t++)
            {
                float x = xPos + t * width / topPoints;
                float y = groundY + height + Mathf.Sin(t * Mathf.PI/2) * L * 0.1f;
                building.Add(new Vector2(x, y));
            }
            
            // 建筑右侧（带咬合齿）
            for (int t = teeth; t >= 0; t--)
            {
                float y = groundY + t * height / teeth;
                float tooth = (t % 2 == 0) ? notch : -notch;
                building.Add(new Vector2(xPos + width - tooth, y));
            }
            
            building.Add(new Vector2(xPos + width, groundY));
            level.cutPaths.Add(DouBao_CreateCutPath(building.ToArray()));

            xPos += width + Random.Range(0, minWidth * 0.5f);
            if (xPos > L) break;
        }
    }

    // ==================== Case47：精密路径咬合拼图（电路板） ====================
    private void DouBao_GenerateCircuitBoardPuzzle(LevelDesignData level, float L)
    {
        level.description = "精密路径咬合拼图";
        level.difficulty = 4.5f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        int gridSize = 10;
        float cellSize = L * 2 / gridSize;
        float traceWidth = cellSize * 0.1f;
        float notch = traceWidth * 0.8f; // 精密咬合齿

        // 电路板网格（精密基线）
        for (int x = 0; x <= gridSize; x++)
        {
            List<Vector2> vertLine = new List<Vector2>();
            float xPos = -L + x * cellSize;
            for (int y = 0; y <= gridSize; y++)
            {
                float yPos = -L + y * cellSize;
                // 精密咬合齿
                float tooth = (y % 2 == 0) ? notch : -notch;
                vertLine.Add(new Vector2(xPos + tooth, yPos));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(vertLine.ToArray()));
        }

        for (int y = 0; y <= gridSize; y++)
        {
            List<Vector2> horzLine = new List<Vector2>();
            float yPos = -L + y * cellSize;
            for (int x = 0; x <= gridSize; x++)
            {
                float xPos = -L + x * cellSize;
                float tooth = (x % 2 == 0) ? notch : -notch;
                horzLine.Add(new Vector2(xPos, yPos + tooth));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(horzLine.ToArray()));
        }

        // 随机生成电路走线（精密曲线）
        int traceCount = Random.Range(5, 8);
        for (int t = 0; t < traceCount; t++)
        {
            List<Vector2> trace = new List<Vector2>();
            // 随机起点
            Vector2 start = new Vector2(
                Random.Range(-L * 0.8f, L * 0.8f),
                Random.Range(-L * 0.8f, L * 0.8f)
            );
            trace.Add(start);

            // 生成弯曲走线
            int segments = Random.Range(8, 15);
            Vector2 current = start;
            for (int s = 0; s < segments; s++)
            {
                float angle = Random.Range(0, Mathf.PI * 2);
                float length = Random.Range(cellSize * 0.5f, cellSize * 1.5f);
                current += new Vector2(Mathf.Cos(angle) * length, Mathf.Sin(angle) * length);
                
                // 限制在范围内
                current.x = Mathf.Clamp(current.x, -L, L);
                current.y = Mathf.Clamp(current.y, -L, L);
                
                // 精密咬合齿
                float tooth = Mathf.Sin(s * Mathf.PI/4) * notch * 0.5f;
                trace.Add(new Vector2(current.x + tooth, current.y + tooth));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(trace.ToArray()));
        }

        // 电路元件（圆形/方形焊盘）
        int componentCount = Random.Range(6, 10);
        for (int c = 0; c < componentCount; c++)
        {
            Vector2 center = new Vector2(
                Random.Range(-L * 0.7f, L * 0.7f),
                Random.Range(-L * 0.7f, L * 0.7f)
            );
            float size = Random.Range(cellSize * 0.5f, cellSize * 1.2f);

            List<Vector2> component = new List<Vector2>();
            if (Random.value > 0.5f)
            {
                // 圆形焊盘
                for (int i = 0; i <= 360; i += 30)
                {
                    float angle = i * Mathf.Deg2Rad;
                    component.Add(new Vector2(
                        center.x + Mathf.Cos(angle) * size,
                        center.y + Mathf.Sin(angle) * size
                    ));
                }
            }
            else
            {
                // 方形焊盘（带咬合齿）
                component.Add(new Vector2(center.x - size, center.y - size));
                component.Add(new Vector2(center.x + size - notch, center.y - size));
                component.Add(new Vector2(center.x + size - notch, center.y - size + notch));
                component.Add(new Vector2(center.x + size, center.y - size + notch));
                component.Add(new Vector2(center.x + size, center.y + size - notch));
                component.Add(new Vector2(center.x + size - notch, center.y + size - notch));
                component.Add(new Vector2(center.x + size - notch, center.y + size));
                component.Add(new Vector2(center.x - size + notch, center.y + size));
                component.Add(new Vector2(center.x - size + notch, center.y + size - notch));
                component.Add(new Vector2(center.x - size, center.y + size - notch));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(component.ToArray()));
        }
    }

    // ==================== Case48：树形自然咬合拼图 ====================
    private void DouBao_GenerateTreeBranchPuzzle(LevelDesignData level, float L)
{
    level.description = "树形自然咬合拼图";
    level.difficulty = 4.2f;
    level.cutPaths = new List<LevelDesignData.CutPath>();

    Vector2 trunkStart = new Vector2(0, -L);
    float trunkWidth = L * 0.15f;
    float notch = L * 0.04f; // 树枝咬合齿

    // 生成主树干（带自然分叉）
    List<Vector2> trunk = new List<Vector2>();
    GenerateTreeTrunk(trunk, trunkStart, 0, L * 0.8f, trunkWidth, 0, notch);
    level.cutPaths.Add(DouBao_CreateCutPath(trunk.ToArray()));

    // 修复：调用GenerateTreeBranches时，最后一个参数传入L
    GenerateTreeBranches(level, new Vector2(0, L * 0.3f), 0, L * 0.4f, trunkWidth * 0.6f, 1, notch, L);
    GenerateTreeBranches(level, new Vector2(0, L * 0.5f), Mathf.PI/6, L * 0.3f, trunkWidth * 0.5f, 1, notch, L);
    GenerateTreeBranches(level, new Vector2(0, L * 0.5f), -Mathf.PI/6, L * 0.3f, trunkWidth * 0.5f, 1, notch, L);
}

    // 生成树干辅助函数
    private void GenerateTreeTrunk(List<Vector2> points, Vector2 start, float angle, float length, float width, int depth, float notch)
    {
        Vector2 end = new Vector2(
            start.x + Mathf.Cos(angle) * length,
            start.y + Mathf.Sin(angle) * length
        );

        // 树干左侧（带咬合齿）
        points.Add(new Vector2(start.x - width/2 - notch, start.y));
        points.Add(new Vector2(start.x - width/2, start.y));
        
        int segments = 5;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float w = width * (1 - t * 0.7f); // 树干逐渐变细
            float y = start.y + Mathf.Sin(angle) * length * t;
            float x = start.x + Mathf.Cos(angle) * length * t;
            
            // 自然弯曲+咬合齿
            float curve = Mathf.Sin(t * Mathf.PI * 2) * width * 0.2f;
            float tooth = (i % 2 == 0) ? notch : -notch;
            
            points.Add(new Vector2(x - w/2 + curve + tooth, y));
        }
        
        points.Add(new Vector2(end.x - width * 0.3f/2, end.y));
        points.Add(new Vector2(end.x, end.y));
        points.Add(new Vector2(end.x + width * 0.3f/2, end.y));
        
        // 树干右侧
        for (int i = segments; i >= 0; i--)
        {
            float t = i / (float)segments;
            float w = width * (1 - t * 0.7f);
            float y = start.y + Mathf.Sin(angle) * length * t;
            float x = start.x + Mathf.Cos(angle) * length * t;
            
            float curve = Mathf.Sin(t * Mathf.PI * 2) * width * 0.2f;
            float tooth = (i % 2 == 0) ? notch : -notch;
            
            points.Add(new Vector2(x + w/2 + curve - tooth, y));
        }
        
        points.Add(new Vector2(start.x + width/2, start.y));
        points.Add(new Vector2(start.x + width/2 + notch, start.y));
    }

    // 递归生成树枝辅助函数
    // 核心修复：添加 float L 作为参数，让函数能访问到L的值
private void GenerateTreeBranches(LevelDesignData level, Vector2 start, float angle, float length, float width, int depth, float notch, float L)
{
    // 修复：现在L能正常访问，终止条件生效
    if (depth > 4 || length < L * 0.05f) return;

    Vector2 end = new Vector2(
        start.x + Mathf.Cos(angle) * length,
        start.y + Mathf.Sin(angle) * length
    );

    // 生成树枝路径（带咬合齿）
    List<Vector2> branch = new List<Vector2>();
    branch.Add(start);
    
    int teeth = depth + 1;
    for (int t = 0; t <= teeth; t++)
    {
        float tNorm = t / (float)teeth;
        float x = start.x + Mathf.Cos(angle) * length * tNorm;
        float y = start.y + Mathf.Sin(angle) * length * tNorm;
        
        // 自然弯曲+咬合齿
        float curve = Mathf.Sin(tNorm * Mathf.PI * 3) * width * 0.5f;
        float tooth = (t % 2 == 0) ? notch : -notch;
        
        branch.Add(new Vector2(x + curve + tooth, y));
    }
    
    branch.Add(end);
    level.cutPaths.Add(DouBao_CreateCutPath(branch.ToArray()));

    // 递归生成子树枝
    float newLength = length * 0.7f;
    float newWidth = width * 0.7f;
    
    // 修复：递归调用时也要传入L参数
    GenerateTreeBranches(level, end, angle + Random.Range(Mathf.PI/8, Mathf.PI/4), newLength, newWidth, depth + 1, notch, L);
    GenerateTreeBranches(level, end, angle - Random.Range(Mathf.PI/8, Mathf.PI/4), newLength, newWidth, depth + 1, notch, L);
}

    // ==================== Case49：多向波浪立体拼图 ====================
    private void DouBao_GenerateMultiDirectionWave(LevelDesignData level, float L)
    {
        level.description = "多向波浪立体拼图";
        level.difficulty = 4.3f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float amplitude = L * 0.15f;
        int waveCount = 5;
        float notch = L * 0.04f; // 波浪咬合齿

        // 生成多方向波浪（水平、垂直、斜向）
        for (int dir = 0; dir < 4; dir++)
        {
            float angle = dir * Mathf.PI/4;
            List<Vector2> wave = new List<Vector2>();
            
            for (int i = 0; i <= 100; i++)
            {
                float t = i / 100f;
                float x = -L + t * L * 2;
                float y = 0;
                
                // 旋转坐标系
                Vector2 rotated = RotatePoint(new Vector2(x, y), -angle);
                
                // 多频率波浪
                float waveY = Mathf.Sin(t * Mathf.PI * waveCount * 2) * amplitude +
                             Mathf.Sin(t * Mathf.PI * waveCount * 0.8f) * amplitude * 0.5f;
                
                // 添加咬合齿（增强立体感）
                float tooth = Mathf.Sin(t * Mathf.PI * waveCount * 4) * notch;
                rotated.y += waveY + tooth;
                
                // 旋转回原坐标系
                rotated = RotatePoint(rotated, angle);
                wave.Add(rotated);
            }
            
            level.cutPaths.Add(DouBao_CreateCutPath(wave.ToArray()));
        }

        // 生成嵌套波浪环（增强3D感）
        for (int ring = 1; ring <= 3; ring++)
        {
            float radius = L * 0.3f * ring;
            List<Vector2> waveRing = new List<Vector2>();
            
            for (int i = 0; i <= 360; i += 5)
            {
                float angle = i * Mathf.Deg2Rad;
                float wave = Mathf.Sin(angle * 6) * amplitude * 0.3f;
                float tooth = Mathf.Sin(angle * 12) * notch;
                
                waveRing.Add(new Vector2(
                    Mathf.Cos(angle) * (radius + wave + tooth),
                    Mathf.Sin(angle) * (radius + wave + tooth)
                ));
            }
            
            level.cutPaths.Add(DouBao_CreateCutPath(waveRing.ToArray()));
        }
    }

    // 点旋转辅助函数
    private Vector2 RotatePoint(Vector2 point, float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        return new Vector2(
            point.x * cos - point.y * sin,
            point.x * sin + point.y * cos
        );
    }

    // ==================== Case50：蜂巢六边形咬合拼图 ====================
    private void DouBao_GenerateHoneycombPuzzle(LevelDesignData level, float L)
    {
        level.description = "蜂巢六边形咬合拼图";
        level.difficulty = 4.0f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float hexSize = L * 0.15f;
        float notch = hexSize * 0.2f; // 六边形咬合齿
        int gridX = 8;
        int gridY = 6;

        // 生成蜂巢网格
        for (int x = -gridX/2; x < gridX/2; x++)
        {
            for (int y = -gridY/2; y < gridY/2; y++)
            {
                // 计算六边形中心位置（偏移偶数行）
                float offsetX = x * hexSize * 1.5f;
                float offsetY = y * hexSize * Mathf.Sqrt(3) + (x % 2 == 0 ? 0 : hexSize * Mathf.Sqrt(3)/2);
                
                // 限制在范围内
                if (Mathf.Abs(offsetX) > L * 0.9f || Mathf.Abs(offsetY) > L * 0.9f) continue;
                
                // 生成六边形（带咬合齿）
                List<Vector2> hex = new List<Vector2>();
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI/3;
                    float radius = hexSize + (i % 2 == 0 ? notch : -notch);
                    
                    hex.Add(new Vector2(
                        offsetX + Mathf.Cos(angle) * radius,
                        offsetY + Mathf.Sin(angle) * radius
                    ));
                }
                level.cutPaths.Add(DouBao_CreateCutPath(hex.ToArray()));
            }
        }

        // 生成蜂巢核心（大六边形）
        List<Vector2> coreHex = new List<Vector2>();
        float coreSize = hexSize * 2.5f;
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI/3;
            // 核心六边形带多层咬合齿
            float radius1 = coreSize + (i % 2 == 0 ? notch * 2 : -notch * 2);
            float radius2 = coreSize + (i % 2 == 0 ? notch : -notch);
            
            coreHex.Add(new Vector2(Mathf.Cos(angle) * radius1, Mathf.Sin(angle) * radius1));
            coreHex.Add(new Vector2(Mathf.Cos(angle) * radius2, Mathf.Sin(angle) * radius2));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(coreHex.ToArray()));
    }

    // ==================== Case51：不规则多边形组合拼图 ====================
    private void DouBao_GenerateIrregularPolygonCombo(LevelDesignData level, float L)
    {
        level.description = "不规则多边形组合拼图";
        level.difficulty = 4.4f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        int polygonCount = Random.Range(6, 9);
        float notch = L * 0.05f; // 多边形边缘咬合齿

        // 生成多个不规则多边形（随机顶点+咬合齿）
        for (int p = 0; p < polygonCount; p++)
        {
            // 随机中心和大小
            Vector2 center = new Vector2(
                Random.Range(-L * 0.7f, L * 0.7f),
                Random.Range(-L * 0.7f, L * 0.7f)
            );
            float size = Random.Range(L * 0.2f, L * 0.5f);
            int vertices = Random.Range(5, 9); // 5-8边形

            // 生成不规则顶点
            List<Vector2> polygon = new List<Vector2>();
            for (int v = 0; v < vertices; v++)
            {
                float angle = v * Mathf.PI * 2 / vertices + Random.Range(-Mathf.PI/12, Mathf.PI/12);
                float radius = size + Random.Range(-size * 0.2f, size * 0.2f);
                
                // 添加咬合齿
                float tooth = (v % 2 == 0) ? notch : -notch;
                polygon.Add(new Vector2(
                    center.x + Mathf.Cos(angle) * (radius + tooth),
                    center.y + Mathf.Sin(angle) * (radius + tooth)
                ));
            }
            
            level.cutPaths.Add(DouBao_CreateCutPath(polygon.ToArray()));
        }

        // 生成连接多边形的路径（增加咬合）
        for (int i = 0; i < polygonCount - 1; i++)
        {
            if (level.cutPaths[i].points.Count == 0 || level.cutPaths[i+1].points.Count == 0) continue;
            
            // 取两个多边形的随机顶点
            Vector2 p1 = level.cutPaths[i].points[Random.Range(0, level.cutPaths[i].points.Count)];
            Vector2 p2 = level.cutPaths[i+1].points[Random.Range(0, level.cutPaths[i+1].points.Count)];
            
            // 生成连接路径（带咬合齿）
            List<Vector2> connector = new List<Vector2>();
            connector.Add(p1);
            
            int teeth = Random.Range(3, 6);
            for (int t = 1; t < teeth; t++)
            {
                float tNorm = t / (float)teeth;
                Vector2 point = Vector2.Lerp(p1, p2, tNorm);
                // 波浪形咬合齿
                float tooth = Mathf.Sin(tNorm * Mathf.PI * 4) * notch;
                connector.Add(new Vector2(point.x + tooth, point.y + tooth));
            }
            
            connector.Add(p2);
            level.cutPaths.Add(DouBao_CreateCutPath(connector.ToArray()));
        }
    }

    // ==================== Case52：嵌套螺旋多层咬合拼图 ====================
    private void DouBao_GenerateNestedSpiralPuzzle(LevelDesignData level, float L)
    {
        level.description = "嵌套螺旋多层咬合";
        level.difficulty = 4.5f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        int spiralLayers = 4;
        float maxRadius = L * 0.8f;
        float notch = L * 0.03f; // 螺旋咬合齿

        // 生成多层嵌套螺旋
        for (int layer = 1; layer <= spiralLayers; layer++)
        {
            float layerRadius = maxRadius * (layer / (float)spiralLayers);
            float startAngle = layer * Mathf.PI/2; // 错开起始角度
            int turns = 3 + layer; // 层数越多，圈数越多
            
            List<Vector2> spiral = new List<Vector2>();
            spiral.Add(Vector2.zero); // 中心起点
            
            for (int i = 0; i <= 100 * layer; i++)
            {
                float t = i / (100f * layer);
                float angle = startAngle + t * Mathf.PI * 2 * turns;
                float radius = t * layerRadius;
                
                // 多层咬合齿（双波浪）
                float tooth1 = Mathf.Sin(angle * 4) * notch;
                float tooth2 = Mathf.Cos(angle * 6) * notch * 0.5f;
                
                spiral.Add(new Vector2(
                    Mathf.Cos(angle) * (radius + tooth1 + tooth2),
                    Mathf.Sin(angle) * (radius + tooth1 + tooth2)
                ));
            }
            
            level.cutPaths.Add(DouBao_CreateCutPath(spiral.ToArray()));
        }

        // 生成螺旋之间的连接路径（增强咬合）
        for (int i = 0; i < spiralLayers - 1; i++)
        {
            float r1 = maxRadius * (i+1)/spiralLayers * 0.8f;
            float r2 = maxRadius * (i+2)/spiralLayers * 0.8f;
            
            List<Vector2> connector = new List<Vector2>();
            for (int j = 0; j <= 90; j += 5)
            {
                float angle = j * Mathf.Deg2Rad;
                float r = Mathf.Lerp(r1, r2, j/90f);
                
                connector.Add(new Vector2(
                    Mathf.Cos(angle) * (r + Mathf.Sin(j * Mathf.Deg2Rad * 4) * notch),
                    Mathf.Sin(angle) * (r + Mathf.Cos(j * Mathf.Deg2Rad * 4) * notch)
                ));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(connector.ToArray()));
        }
    }

    // ==================== Case53：多齿轮啮合拼图 ====================
    private void DouBao_GenerateGearSetPuzzle(LevelDesignData level, float L)
    {
        level.description = "多齿轮啮合拼图";
        level.difficulty = 4.4f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float mainGearRadius = L * 0.4f;
        int mainGearTeeth = 16;
        float toothHeight = mainGearRadius * 0.15f;
        float notch = toothHeight * 0.5f; // 齿轮咬合齿

        // 生成主齿轮（中心）
        List<Vector2> mainGear = new List<Vector2>();
        GenerateGear(mainGear, Vector2.zero, mainGearRadius, mainGearTeeth, toothHeight, notch);
        level.cutPaths.Add(DouBao_CreateCutPath(mainGear.ToArray()));

        // 生成啮合的副齿轮
        int subGearCount = 6;
        float subGearRadius = mainGearRadius * 0.6f;
        int subGearTeeth = Mathf.RoundToInt(mainGearTeeth * 0.6f);

        for (int i = 0; i < subGearCount; i++)
        {
            float angle = i * Mathf.PI * 2 / subGearCount;
            Vector2 center = new Vector2(
                Mathf.Cos(angle) * (mainGearRadius + subGearRadius),
                Mathf.Sin(angle) * (mainGearRadius + subGearRadius)
            );

            List<Vector2> subGear = new List<Vector2>();
            GenerateGear(subGear, center, subGearRadius, subGearTeeth, toothHeight * 0.6f, notch * 0.6f);
            level.cutPaths.Add(DouBao_CreateCutPath(subGear.ToArray()));

            // 生成齿轮啮合路径（咬合验证）
            List<Vector2> meshPath = new List<Vector2>();
            float startAngle = angle - Mathf.PI/6;
            float endAngle = angle + Mathf.PI/6;
            
            for (int a = 0; a <= 30; a++)
            {
                float t = a / 30f;
                float aRad = Mathf.Lerp(startAngle, endAngle, t);
                float r = Mathf.Lerp(mainGearRadius, mainGearRadius + subGearRadius * 0.9f, t);
                
                meshPath.Add(new Vector2(
                    Mathf.Cos(aRad) * (r + Mathf.Sin(t * Mathf.PI * 4) * notch),
                    Mathf.Sin(aRad) * (r + Mathf.Cos(t * Mathf.PI * 4) * notch)
                ));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(meshPath.ToArray()));
        }

        // 生成外围齿轮（更大）
        float outerGearRadius = mainGearRadius * 1.8f;
        int outerGearTeeth = Mathf.RoundToInt(mainGearTeeth * 1.8f);
        
        List<Vector2> outerGear = new List<Vector2>();
        GenerateGear(outerGear, Vector2.zero, outerGearRadius, outerGearTeeth, toothHeight * 1.2f, notch * 1.2f);
        level.cutPaths.Add(DouBao_CreateCutPath(outerGear.ToArray()));
    }

    // 生成齿轮辅助函数
    private void GenerateGear(List<Vector2> points, Vector2 center, float radius, int teethCount, float toothHeight, float notch)
    {
        for (int i = 0; i < teethCount; i++)
        {
            float angle1 = i * Mathf.PI * 2 / teethCount;
            float angle2 = (i + 1) * Mathf.PI * 2 / teethCount;
            float midAngle = (angle1 + angle2) / 2;

            // 齿根
            points.Add(new Vector2(
                center.x + Mathf.Cos(angle1) * (radius - toothHeight),
                center.y + Mathf.Sin(angle1) * (radius - toothHeight)
            ));
            
            // 齿顶（带咬合齿）
            points.Add(new Vector2(
                center.x + Mathf.Cos(midAngle) * (radius + toothHeight - notch),
                center.y + Mathf.Sin(midAngle) * (radius + toothHeight - notch)
            ));
            points.Add(new Vector2(
                center.x + Mathf.Cos(midAngle) * (radius + toothHeight),
                center.y + Mathf.Sin(midAngle) * (radius + toothHeight)
            ));
            points.Add(new Vector2(
                center.x + Mathf.Cos(midAngle) * (radius + toothHeight - notch),
                center.y + Mathf.Sin(midAngle) * (radius + toothHeight - notch)
            ));
        }
        // 闭合齿轮
        points.Add(points[0]);
    }

    // ==================== Case54：莫比乌斯环连续咬合拼图 ====================
    private void DouBao_GenerateMobiusPuzzle(LevelDesignData level, float L)
    {
        level.description = "莫比乌斯环连续咬合";
        level.difficulty = 4.6f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float radius = L * 0.7f;
        float width = L * 0.15f;
        float notch = L * 0.04f; // 莫比乌斯环咬合齿

        // 生成莫比乌斯环（单侧连续）
        List<Vector2> mobius = new List<Vector2>();
        for (int i = 0; i <= 720; i += 5) // 720度旋转（莫比乌斯特征）
        {
            float angle = i * Mathf.Deg2Rad;
            float twist = angle / 2; // 180度扭转
            
            // 莫比乌斯环中心线
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            // 环的宽度偏移（带扭转）
            float offsetX = Mathf.Cos(angle + twist) * width;
            float offsetY = Mathf.Sin(angle + twist) * width;
            
            // 添加连续咬合齿
            float tooth = Mathf.Sin(angle * 4) * notch;
            mobius.Add(new Vector2(x + offsetX + tooth, y + offsetY + tooth));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(mobius.ToArray()));

        // 生成莫比乌斯环的内侧路径（增强连续感）
        List<Vector2> innerMobius = new List<Vector2>();
        for (int i = 0; i <= 720; i += 5)
        {
            float angle = i * Mathf.Deg2Rad;
            float twist = angle / 2;
            
            float x = Mathf.Cos(angle) * (radius - width * 0.5f);
            float y = Mathf.Sin(angle) * (radius - width * 0.5f);
            
            float offsetX = Mathf.Cos(angle + twist) * width * 0.8f;
            float offsetY = Mathf.Sin(angle + twist) * width * 0.8f;
            
            float tooth = Mathf.Cos(angle * 4) * notch;
            innerMobius.Add(new Vector2(x + offsetX + tooth, y + offsetY + tooth));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(innerMobius.ToArray()));
    }

    // ==================== Case55：水滴形流线咬合拼图 ====================
    private void DouBao_GenerateWaterDropPuzzle(LevelDesignData level, float L)
    {
        level.description = "水滴形流线咬合拼图";
        level.difficulty = 4.1f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        float dropSize = L * 0.6f;
        float notch = L * 0.04f; // 水滴边缘咬合齿
        int dropCount = Random.Range(5, 8);

        // 生成主水滴（中心）
        List<Vector2> mainDrop = new List<Vector2>();
        GenerateWaterDrop(mainDrop, Vector2.zero, dropSize, notch);
        level.cutPaths.Add(DouBao_CreateCutPath(mainDrop.ToArray()));

        // 生成多个小水滴（流线分布）
        for (int i = 0; i < dropCount; i++)
        {
            float angle = i * Mathf.PI * 2 / dropCount + Random.Range(-Mathf.PI/12, Mathf.PI/12);
            float distance = Random.Range(dropSize * 1.2f, dropSize * 2f);
            float size = Random.Range(dropSize * 0.3f, dropSize * 0.7f);
            
            Vector2 center = new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );

            List<Vector2> smallDrop = new List<Vector2>();
            GenerateWaterDrop(smallDrop, center, size, notch * (size/dropSize));
            level.cutPaths.Add(DouBao_CreateCutPath(smallDrop.ToArray()));

            // 生成流线连接路径
            List<Vector2> stream = new List<Vector2>();
            stream.Add(new Vector2(0, dropSize * 0.2f)); // 主水滴出口
            
            int segments = Random.Range(8, 12);
            for (int s = 0; s <= segments; s++)
            {
                float t = s / (float)segments;
                Vector2 point = Vector2.Lerp(new Vector2(0, dropSize * 0.2f), center, t);
                
                // 流线波动+咬合齿
                float wave = Mathf.Sin(t * Mathf.PI * 4) * notch;
                stream.Add(new Vector2(point.x + wave, point.y + wave * 0.5f));
            }
            
            stream.Add(center);
            level.cutPaths.Add(DouBao_CreateCutPath(stream.ToArray()));
        }
    }

    // 生成水滴辅助函数
    private void GenerateWaterDrop(List<Vector2> points, Vector2 center, float size, float notch)
    {
        // 水滴轮廓（上尖下圆）
        points.Add(new Vector2(center.x, center.y + size)); // 顶点
        
        // 右侧轮廓（带咬合齿）
        for (int i = 0; i <= 90; i += 5)
        {
            float angle = (90 - i) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * size * 0.6f;
            float y = Mathf.Cos(angle) * size * 0.8f;
            
            // 流线型+咬合齿
            float tooth = Mathf.Sin(i * Mathf.Deg2Rad * 3) * notch;
            points.Add(new Vector2(
                center.x + x + tooth,
                center.y + y - size * 0.2f + tooth * 0.5f
            ));
        }
        
        // 底部圆弧（带咬合齿）
        for (int i = 0; i <= 180; i += 5)
        {
            float angle = (180 - i) * Mathf.Deg2Rad + Mathf.PI;
            float x = Mathf.Cos(angle) * size * 0.6f;
            float y = Mathf.Sin(angle) * size * 0.4f;
            
            float tooth = Mathf.Sin(i * Mathf.Deg2Rad * 2) * notch;
            points.Add(new Vector2(
                center.x + x + tooth,
                center.y - size * 0.4f + y + tooth
            ));
        }
        
        // 左侧轮廓（带咬合齿）
        for (int i = 90; i >= 0; i -= 5)
        {
            float angle = (90 - i) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * size * 0.6f;
            float y = Mathf.Cos(angle) * size * 0.8f;
            
            float tooth = Mathf.Sin(i * Mathf.Deg2Rad * 3) * notch;
            points.Add(new Vector2(
                center.x - x - tooth,
                center.y + y - size * 0.2f + tooth * 0.5f
            ));
        }
    }

    // ==================== Case56：星座式点连接拼图 ====================
    private void DouBao_GenerateConstellationPuzzle(LevelDesignData level, float L)
    {
        level.description = "星座式点连接拼图";
        level.difficulty = 4.2f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        int starCount = Random.Range(15, 25);
        float maxDistance = L * 0.9f;
        float notch = L * 0.03f; // 连接路径咬合齿

        // 生成随机星点
        List<Vector2> stars = new List<Vector2>();
        for (int i = 0; i < starCount; i++)
        {
            stars.Add(new Vector2(
                Random.Range(-maxDistance, maxDistance),
                Random.Range(-maxDistance, maxDistance)
            ));

            // 星点（小圆形，带咬合齿）
            List<Vector2> star = new List<Vector2>();
            float starSize = Random.Range(L * 0.03f, L * 0.08f);
            for (int a = 0; a <= 360; a += 30)
            {
                float angle = a * Mathf.Deg2Rad;
                float tooth = (a % 60 == 0) ? notch * 0.5f : -notch * 0.5f;
                star.Add(new Vector2(
                    stars[i].x + Mathf.Cos(angle) * (starSize + tooth),
                    stars[i].y + Mathf.Sin(angle) * (starSize + tooth)
                ));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(star.ToArray()));
        }

        // 生成星座连接路径（最近邻连接）
        for (int i = 0; i < starCount; i++)
        {
            // 找到最近的2-3个星点
            List<int> nearest = new List<int>();
            List<float> distances = new List<float>();

            for (int j = 0; j < starCount; j++)
            {
                if (i == j) continue;
                float dist = Vector2.Distance(stars[i], stars[j]);
                if (dist < L * 0.4f) // 只连接近距离星点
                {
                    nearest.Add(j);
                    distances.Add(dist);
                }
            }

            // 排序取最近的2个
            for (int k = 0; k < Mathf.Min(2, nearest.Count); k++)
            {
                int nearestIdx = nearest[k];
                List<Vector2> connection = new List<Vector2>();
                connection.Add(stars[i]);

                // 连接路径（带波浪形咬合齿）
                int segments = Random.Range(5, 8);
                for (int s = 1; s < segments; s++)
                {
                    float t = s / (float)segments;
                    Vector2 point = Vector2.Lerp(stars[i], stars[nearestIdx], t);
                    float wave = Mathf.Sin(t * Mathf.PI * 4) * notch;
                    connection.Add(new Vector2(point.x + wave, point.y + wave * 0.8f));
                }

                connection.Add(stars[nearestIdx]);
                level.cutPaths.Add(DouBao_CreateCutPath(connection.ToArray()));
            }
        }

        // 生成星座外轮廓（大曲线）
        List<Vector2> outerConstellation = new List<Vector2>();
        for (int a = 0; a <= 360; a += 15)
        {
            float angle = a * Mathf.Deg2Rad;
            float radius = maxDistance + Mathf.Sin(angle * 3) * L * 0.1f;
            
            outerConstellation.Add(new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            ));
        }
        level.cutPaths.Add(DouBao_CreateCutPath(outerConstellation.ToArray()));
    }

    // ==================== Case57：随机曲线无规则拼图 ====================
    private void DouBao_GenerateRandomCurvePuzzle(LevelDesignData level, float L)
    {
        level.description = "随机曲线无规则拼图";
        level.difficulty = 4.3f;
        level.cutPaths = new List<LevelDesignData.CutPath>();

        int curveCount = Random.Range(8, 12);
        float notch = L * 0.04f;

        // 生成多条随机曲线
        for (int c = 0; c < curveCount; c++)
        {
            List<Vector2> curve = new List<Vector2>();
            // 随机起点
            Vector2 start = new Vector2(
                Random.Range(-L, L),
                Random.Range(-L, L)
            );
            curve.Add(start);

            // 随机生成曲线段
            int segments = Random.Range(10, 20);
            Vector2 current = start;
            for (int s = 0; s < segments; s++)
            {
                // 随机方向和长度
                float angle = Random.Range(0, Mathf.PI * 2);
                float length = Random.Range(L * 0.05f, L * 0.15f);
                
                current += new Vector2(Mathf.Cos(angle) * length, Mathf.Sin(angle) * length);
                // 限制在范围内
                current.x = Mathf.Clamp(current.x, -L, L);
                current.y = Mathf.Clamp(current.y, -L, L);
                
                // 随机咬合齿
                float tooth = Random.Range(-notch, notch);
                curve.Add(new Vector2(current.x + tooth, current.y + tooth * 0.8f));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(curve.ToArray()));
        }

        // 生成随机闭合曲线
        int closedCurveCount = Random.Range(3, 5);
        for (int c = 0; c < closedCurveCount; c++)
        {
            List<Vector2> closedCurve = new List<Vector2>();
            Vector2 center = new Vector2(
                Random.Range(-L * 0.7f, L * 0.7f),
                Random.Range(-L * 0.7f, L * 0.7f)
            );
            float size = Random.Range(L * 0.15f, L * 0.35f);

            for (int a = 0; a <= 360; a += 10)
            {
                float angle = a * Mathf.Deg2Rad;
                // 随机半径+咬合齿
                float radius = size + Random.Range(-size * 0.3f, size * 0.3f);
                float tooth = Mathf.Sin(angle * 5) * notch;
                
                closedCurve.Add(new Vector2(
                    center.x + Mathf.Cos(angle) * (radius + tooth),
                    center.y + Mathf.Sin(angle) * (radius + tooth)
                ));
            }
            level.cutPaths.Add(DouBao_CreateCutPath(closedCurve.ToArray()));
        }
    }

    #endregion

    // 保存关卡数据到JSON
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
}
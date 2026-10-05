using UnityEngine;
using System.Collections.Generic;

public class Cutter : MonoBehaviour
{
    // 存储当前场景中所有的拼图块
    public List<PuzzlePiece> activePieces = new List<PuzzlePiece>();
    public Material pieceMaterial;

    void Start()
    {
        CreateInitialSquare();
    }


    public void CreateInitialSquare()
    {
        float L = CutterManager.cutterLength;
        List<Vector2> basePoints = new()
        {
            new Vector2(-L, L), new Vector2(L, L), new Vector2(L, -L), new Vector2(-L, -L)
        };
        activePieces.Add(CreateNewPiece(basePoints, true));
    }

    public void ExecuteAllCuts(List<List<Vector2>> allPaths)
    {
        // 每一条画好的折线路径
        foreach (var path in allPaths)
        {
            if (path.Count < 2) continue;

            // 建立一个临时列表，存放这一轮切割后产生的所有新碎片
            List<PuzzlePiece> nextGeneration = new List<PuzzlePiece>();

            // 遍历当前场景中存在的所有碎片（上一轮的结果）
            foreach (var piece in activePieces)
            {
                // 将世界坐标转为该碎片的本地坐标
                List<Vector2> localPath = new List<Vector2>();
                foreach (var p in path) localPath.Add(piece.transform.InverseTransformPoint(p));

                // 执行 Clipper2 切割
                // 注意：这里返回的是 List<List<Vector2>>，可能包含 2块、3块甚至更多
                List<List<Vector2>> results = Clipper2CutterHelper.CutPolygon(piece.points, localPath);

                if (results.Count > 1) 
                {
                    // 【核心修复】：必须遍历 results 里的每一个 List<Vector2>
                    // 如果 results 有 3 项，就必须生成 3 个新物体
                    foreach (var poly in results)
                    {
                        nextGeneration.Add(CreateNewPiece(poly, piece.transform.position));
                    }
                    Destroy(piece.gameObject); // 销毁被切开的旧母体
                }
                else
                {
                    // 如果没被这根线切开，保留原样进入下一轮
                    nextGeneration.Add(piece);
                }
            }
            // 把当前活跃列表更新为最新的一代
            activePieces = nextGeneration;
        }
    }


    PuzzlePiece CreateNewPiece(List<Vector2> pts, Vector3 spawnPos)
    {
        GameObject go = new GameObject("Piece");
        go.transform.position = spawnPos;
        PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
        pp.Init_levelEdit(pts, pieceMaterial, new Color(Random.value, Random.value,Random.value));
        return pp;
    }

    PuzzlePiece CreateNewPiece(List<Vector2> pts, bool initFrame = false)
    {
        GameObject go = new GameObject("Piece");
        PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
        // 注意：Init 内部必须包含“重置轴心点”的逻辑，否则位置会偏
        Color color = new Color(Random.value, Random.value, Random.value);
        if (initFrame)
        {
            color = new Color(0.2f, 0.2f, 0.2f);
        }
        pp.Init_levelEdit(pts, pieceMaterial,color);
        return pp;
    }
}
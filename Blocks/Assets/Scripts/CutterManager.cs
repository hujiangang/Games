using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 切割管理器.
/// </summary>
public class CutterManager : MonoBehaviour
{
    public Material pieceMaterial;
    public LineRenderer previewLine;
    private Vector2 startPos;
    private List<PuzzlePiece> activePieces = new List<PuzzlePiece>();

    void Start()
    {
        // 初始创建一个正方形
        GameObject go = new GameObject("InitialSquare");
        PuzzlePiece p = go.AddComponent<PuzzlePiece>();
        p.Init(new List<Vector2> { new Vector2(-3, 3), new Vector2(3, 3), new Vector2(3, -3), new Vector2(-3, -3) }, pieceMaterial);
        activePieces.Add(p);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            previewLine.gameObject.SetActive(true);
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 currentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            previewLine.SetPosition(0, startPos);
            previewLine.SetPosition(1, currentPos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 endPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            DoSlice(startPos, endPos);
            previewLine.gameObject.SetActive(false);
        }
    }

    void DoSlice(Vector2 lineStart, Vector2 lineEnd)
    {
        List<PuzzlePiece> toAdd = new List<PuzzlePiece>();
        List<PuzzlePiece> toRemove = new List<PuzzlePiece>();

        foreach (var piece in activePieces)
        {
            List<Vector2> leftPoints = new List<Vector2>();
            List<Vector2> rightPoints = new List<Vector2>();

            // 核心分割逻辑
            for (int i = 0; i < piece.points.Count; i++)
            {
                Vector2 p1 = piece.points[i];
                Vector2 p2 = piece.points[(i + 1) % piece.points.Count];

                float side1 = (lineEnd.x - lineStart.x) * (p1.y - lineStart.y) - (lineEnd.y - lineStart.y) * (p1.x - lineStart.x);
                float side2 = (lineEnd.x - lineStart.x) * (p2.y - lineStart.y) - (lineEnd.y - lineStart.y) * (p2.x - lineStart.x);

                if (side1 >= 0) leftPoints.Add(p1);
                if (side1 <= 0) rightPoints.Add(p1);

                if (side1 * side2 < 0)
                {
                    float t = side1 / (side1 - side2);
                    Vector2 intersect = Vector2.Lerp(p1, p2, t);
                    leftPoints.Add(intersect);
                    rightPoints.Add(intersect);
                }
            }

            if (leftPoints.Count > 2 && rightPoints.Count > 2)
            {
                toRemove.Add(piece);
                toAdd.Add(CreatePiece(leftPoints));
                toAdd.Add(CreatePiece(rightPoints));
            }
        }

        foreach (var r in toRemove) { activePieces.Remove(r); Destroy(r.gameObject); }
        activePieces.AddRange(toAdd);
    }

    PuzzlePiece CreatePiece(List<Vector2> pts)
    {
        GameObject go = new GameObject("Piece");
        PuzzlePiece p = go.AddComponent<PuzzlePiece>();
        p.Init(pts, pieceMaterial);
        return p;
    }
}
using UnityEngine;
using System.Collections.Generic;

public class CutterManager : MonoBehaviour {

    public static int cutterLength = 2;

    public Material pieceMaterial;
    public LineRenderer previewLine;
    
    private Vector2 startPos;
    // 必须公开或在面板查看，确保列表里有东西
    public List<PuzzlePiece> activePieces = new List<PuzzlePiece>();

    void Start() {
        // 1. 创建初始正方形
        List<Vector2> initPoints = new List<Vector2> { 
            new Vector2(-cutterLength, cutterLength), new Vector2(cutterLength, cutterLength), 
            new Vector2(cutterLength, -cutterLength), new Vector2(-cutterLength, -cutterLength) 
        };
        
        PuzzlePiece firstPiece = CreatePiece(initPoints);
        activePieces.Add(firstPiece); // 这一行之前漏掉了！
        Debug.Log("初始化完成，当前块数: " + activePieces.Count);
    }

    void Update() {

        // 获取鼠标位置，注意 Z 轴要设为相机的距离
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector2 worldMouse = Camera.main.ScreenToWorldPoint(mousePos);

        if (Input.GetMouseButtonDown(0)) {
            startPos = worldMouse;
            previewLine.gameObject.SetActive(true);
            previewLine.SetPosition(0, startPos);
        }

        if (Input.GetMouseButton(0)) {
            previewLine.SetPosition(1, worldMouse);
        }

        if (Input.GetMouseButtonUp(0)) {
            previewLine.gameObject.SetActive(false);
            if (Vector2.Distance(startPos, worldMouse) > 0.1f) {
                ExecuteSlice(startPos, worldMouse);
            }
        }
    }

    public void ExecuteSlice(Vector2 lineStart, Vector2 lineEnd) {
        List<PuzzlePiece> newGeneration = new List<PuzzlePiece>();
        List<PuzzlePiece> toDestroy = new List<PuzzlePiece>();

        Debug.Log("开始尝试切割，当前碎片总数: " + activePieces.Count);

        foreach (var piece in activePieces) {
            List<Vector2> leftSide = new List<Vector2>();
            List<Vector2> rightSide = new List<Vector2>();
            bool hasIntersection = false;

            for (int i = 0; i < piece.points.Count; i++) {
                Vector2 p1 = piece.points[i];
                Vector2 p2 = piece.points[(i + 1) % piece.points.Count];

                float d1 = GetSide(lineStart, lineEnd, p1);
                float d2 = GetSide(lineStart, lineEnd, p2);

                if (d1 >= 0) leftSide.Add(p1);
                if (d1 <= 0) rightSide.Add(p1);

                // 检测交点
                if (d1 * d2 < -0.0001f) {
                    Vector2 inter = GetIntersection(lineStart, lineEnd, p1, p2);
                    leftSide.Add(inter);
                    rightSide.Add(inter);
                    hasIntersection = true;
                }
            }

            // 只有真正切开（两边都有超过2个点）才处理
            if (hasIntersection && leftSide.Count >= 3 && rightSide.Count >= 3) {
                toDestroy.Add(piece);
                newGeneration.Add(CreatePiece(leftSide));
                newGeneration.Add(CreatePiece(rightSide));
            } else {
                newGeneration.Add(piece); // 没切到的保留
            }
        }

        if (toDestroy.Count > 0) {
            foreach (var old in toDestroy) Destroy(old.gameObject);
            activePieces = newGeneration;
            Debug.Log("切割成功！新碎片总数: " + activePieces.Count);
        } else {
            Debug.Log("未切到任何物体");
        }
    }

    float GetSide(Vector2 lStart, Vector2 lEnd, Vector2 p) {
        return (lEnd.x - lStart.x) * (p.y - lStart.y) - (lEnd.y - lStart.y) * (p.x - lStart.x);
    }

    Vector2 GetIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
        float d = (a2.x - a1.x) * (b2.y - b1.y) - (a2.y - a1.y) * (b2.x - b1.x);
        float u = ((b1.x - a1.x) * (b2.y - b1.y) - (b1.y - a1.y) * (b2.x - b1.x)) / d;
        return a1 + u * (a2 - a1);
    }

    public PuzzlePiece CreatePiece(List<Vector2> pts) {
        GameObject go = new GameObject("Piece");
        PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
        pp.Init_levelEdit(pts, pieceMaterial,new Color(Random.value, Random.value,Random.value));
        return pp;
    }
}
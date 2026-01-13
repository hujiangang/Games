using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HintWindow : BasicUI
{
    public GameObject hintPanel;        // 拖入 HintPanel
    public Transform previewAnchor;     // 拖入 PreviewAnchor
    public Material pieceMaterial;      // 拼图材质
    public float displayTime = 0.5f;    // 显示时长

    private List<GameObject> spawnedPreviews = new ();

    void Start()
    {

        UIManager.instance.RegisterUIWindow(UIWindowType.HintWindow, this);

        hintPanel.SetActive(false); // 初始关闭
    }


    public void ClearPreviews()
    {
        foreach (var obj in spawnedPreviews) Destroy(obj);
        spawnedPreviews.Clear();
    }


    // 点击眼睛按钮调用
    public void OpenHint(LevelData data)
    {
        if (hintPanel.activeSelf) return;

        float uiScale = 200; // 缩放倍率

        if (spawnedPreviews.Count <= 0)
        {
            List<PuzzlePiece> tempPieces = new List<PuzzlePiece>();
            Vector3 sumPos = Vector3.zero;

            // --- 第一步：先生成并初始化所有碎片 ---
            foreach (var pData in data.pieces)
            {
                GameObject preview = new GameObject("PreviewPiece");
                preview.transform.SetParent(previewAnchor, false);
                preview.layer = LayerMask.NameToLayer("UI");

                PuzzlePiece pp = preview.AddComponent<PuzzlePiece>();
                
                // 将 Serializable 顶点转回 Vector2 列表
                List<Vector2> verts = new();
                foreach (var v in pData.vertices) verts.Add(v);

                pp.Init_Preview(verts, pieceMaterial, pData.color);

                // 累加正确位置，稍后算平均中心
                sumPos += pp.correctWorldPos; 
                tempPieces.Add(pp);
                spawnedPreviews.Add(preview);
            }

            // --- 第二步：计算这堆碎片的中心点 ---
            Vector3 totalCenter = sumPos / data.pieces.Count;

            // --- 第三步：根据中心点偏移，把它们挪到 UI 容器中心 ---
            foreach (var pp in tempPieces)
            {
                // 算法：(碎片正确位置 - 整体中心点) * 缩放
                Vector3 relativePos = (pp.correctWorldPos - totalCenter) * uiScale;
                
                // 设置 UI 上的本地坐标
                pp.transform.localPosition = new Vector3(relativePos.x, relativePos.y, 0);
                // 设置缩放
                pp.transform.localScale = Vector3.one * uiScale;

                // 设置层级确保显示在 UI 前面
                MeshRenderer mr = pp.GetComponent<MeshRenderer>();
                mr.sortingLayerName = "UI";
                mr.sortingOrder = 2001;
            }
        }

        hintPanel.SetActive(true);
        StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(displayTime);
        hintPanel.SetActive(false);
    }
}
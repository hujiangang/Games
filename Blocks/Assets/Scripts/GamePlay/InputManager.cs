using UnityEngine;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    private DraggableComponent currentTarget;
    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // --- 1. 获取输入状态 (兼容鼠标和触摸) ---
        bool inputStarted = false;
        bool inputHeld = false;
        bool inputEnded = false;
        Vector2 screenPosition;

        if (Input.touchCount > 0) // 触摸输入
        {
            Touch touch = Input.GetTouch(0);
            screenPosition = touch.position;
            if (touch.phase == TouchPhase.Began) inputStarted = true;
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) inputHeld = true;
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) inputEnded = true;
        }
        else // 鼠标输入
        {
            screenPosition = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) inputStarted = true;
            if (Input.GetMouseButton(0)) inputHeld = true;
            if (Input.GetMouseButtonUp(0)) inputEnded = true;
        }

        // --- 2. 处理逻辑 ---

        // A. 按下瞬间：寻找最高层级的碎片
        if (inputStarted)
        {
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

            DraggableComponent bestPiece = null;
            int maxOrder = -1;

            foreach (var hit in hits)
            {
                DraggableComponent piece = hit.collider.GetComponent<DraggableComponent>();
                if (piece != null)
                {
                    // 核心：比较 Sorting Order，找出视觉上最上面的那一个
                    int pieceOrder = piece.GetComponent<MeshRenderer>().sortingOrder;
                    if (pieceOrder > maxOrder)
                    {
                        maxOrder = pieceOrder;
                        bestPiece = piece;
                    }
                }
            }

            if (bestPiece != null)
            {
                currentTarget = bestPiece;
                currentTarget.StartDragging(worldPos);
            }
        }

        // B. 拖拽中
        if (inputHeld && currentTarget != null)
        {
            Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
            currentTarget.FollowMouse(worldPos);
        }

        // C. 抬起
        if (inputEnded && currentTarget != null)
        {
            currentTarget.StopDragging();
            currentTarget = null;
        }
    }
}
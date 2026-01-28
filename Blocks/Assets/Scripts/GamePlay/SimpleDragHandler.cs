using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleDragHandler : MonoBehaviour
{
    [Header("拖拽配置")]
    public Camera mainCamera; // 2D正交相机（必须赋值）
    public float zDistance = 10f; // 和相机farClipPlane匹配（比如相机far=20，设10）
    public InputActionAsset dragActions; // 拖入SimpleDragActions文件

    private InputAction _dragAction;
    private InputAction _dragPosAction;
    private DraggableComponent _currentTarget;
    private Vector2 _mousePieceOffset; // 鼠标-碎片偏移

    void Awake()
    {
        // 获取动作（名称和配置里完全一致）
        var dragMap = dragActions.FindActionMap("DragMap");
        _dragAction = dragMap.FindAction("Drag");
        _dragPosAction = dragMap.FindAction("DragPosition");
    }

    void OnEnable()
    {
        // 订阅Drag动作的生命周期回调（核心：触摸/鼠标统一处理）
        _dragAction.started += OnDragStarted; // 按下瞬间（鼠标Down/触摸按下）
        _dragAction.canceled += OnDragEnded;  // 松开瞬间（鼠标Up/触摸松开）
    }

    void OnDisable()
    {
        // 必须取消订阅，避免内存泄漏
        _dragAction.started -= OnDragStarted;
        _dragAction.canceled -= OnDragEnded;
    }

    // 按下瞬间：找最上层碎片，计算偏移
    private void OnDragStarted(InputAction.CallbackContext ctx)
    {
        if (_currentTarget != null) return;

        // 获取统一的屏幕位置（鼠标/触摸）
        Vector2 screenPos = _dragPosAction.ReadValue<Vector2>();
        // 2D相机坐标转换（加Z值，解决跟不准问题）
        Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, zDistance);
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosWithZ);

        // 射线检测找最上层碎片（只检测可拖拽Layer）
        int pieceLayer = LayerMask.NameToLayer("PuzzlePiece");
        int layerMask = 1 << pieceLayer;
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero, Mathf.Infinity, layerMask);

        DraggableComponent bestPiece = null;
        int maxOrder = -1;
        foreach (var hit in hits)
        {
            DraggableComponent piece = hit.collider.GetComponent<DraggableComponent>();
            if (piece == null) continue;

            if (piece.sortingOrder > maxOrder)
            {
                maxOrder = piece.sortingOrder;
                bestPiece = piece;
            }
        }

        if (bestPiece != null)
        {
            _currentTarget = bestPiece;
            // 计算偏移：避免点击碎片边缘时瞬移
            _mousePieceOffset = (Vector2)_currentTarget.transform.position - worldPos;
            _currentTarget.StartDragging(_currentTarget.transform.position);
        }
    }


    // 松开瞬间：结束拖拽
    private void OnDragEnded(InputAction.CallbackContext ctx)
    {
        if (_currentTarget == null) return;

        Vector2 screenPos = _dragPosAction.ReadValue<Vector2>();
        Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, zDistance);
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosWithZ);

        _currentTarget.StopDragging(worldPos);
        _currentTarget = null;
        _mousePieceOffset = Vector2.zero;
    }

    void LateUpdate()
    {
        // 按住过程：在LateUpdate里更新位置，保证和画面渲染同步
        if (_currentTarget != null && _dragAction != null && _dragAction.IsPressed())
        {
            UpdateDragPosition();
        }
    }
    
    // 独立的拖拽位置更新方法（解耦，方便调试）
    private void UpdateDragPosition()
    {
        if (_dragPosAction == null) return;

        // 获取统一的屏幕位置（鼠标/触摸）
        Vector2 screenPos = _dragPosAction.ReadValue<Vector2>();
        // 2D相机坐标转换（加Z值，解决跟不准问题）
        Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, zDistance);
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosWithZ);
        
        // 加偏移量，碎片精准跟随点击位置
        _currentTarget.FollowMouse(worldPos + _mousePieceOffset);
    }
}
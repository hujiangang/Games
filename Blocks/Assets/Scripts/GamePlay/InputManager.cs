using System.Collections;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private DraggableComponent currentTarget;
    private Camera mainCamera;

    // 拖拽状态锁，只要开始拖拽就保持，直到真正松开
    private bool isDragging = false;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    #region 携程
    // 协程控制器：用于暂停/停止协程
    private Coroutine _repeatCoroutine;
    // 执行间隔（0.2秒）
    private readonly float _executeInterval = 0.2f;
    // 协程运行状态标记
    private bool _isCoroutineRunning = false;

    public void StartRepeatCoroutine()
    {
        if (_isCoroutineRunning || _repeatCoroutine != null)
        {
            Debug.LogWarning("协程已在运行，无需重复启动");
            return;
        }

        _repeatCoroutine = StartCoroutine(RepeatExecuteCoroutine());
        _isCoroutineRunning = true;
        Debug.Log("0.2秒循环协程已启动");
    }

    /// <summary>
    /// 停止协程
    /// </summary>
    public void StopRepeatCoroutine()
    {
        if (_repeatCoroutine != null && _isCoroutineRunning)
        {
            StopCoroutine(_repeatCoroutine);
            _repeatCoroutine = null;
            _isCoroutineRunning = false;
        }
    }

    /// <summary>
    /// 核心协程：0.2秒执行一次目标逻辑
    /// </summary>
    /// <returns></returns>
    private IEnumerator RepeatExecuteCoroutine()
    {
        while (true)
        {
            try
            {

                GetIsPressing();
            }
            catch (System.Exception e)
            {
                // 异常捕获：避免协程因错误中断
                Debug.LogError($"协程执行逻辑出错：{e.Message}\n{e.StackTrace}");
            }

            yield return new WaitForSeconds(_executeInterval);
        }
    }
    
    void OnDisable()
    {
        StopRepeatCoroutine();
    }

    void OnDestroy()
    {
        StopRepeatCoroutine();
    }


    #endregion

    void Update()
    {
        bool isPressing = GetIsPressing();
        Vector2 screenPos = GetPressScreenPosition();

        // 1. 没有在拖拽：监听按下，拾取碎片
        if (!isDragging)
        {
            if (isPressing)
            {
                Vector2 worldPos = ScreenToWorldPointFixed(screenPos);
                DraggableComponent piece = GetTopDraggable(screenPos);

                if (piece != null)
                {
                    // 开始拖拽，锁死目标
                    currentTarget = piece;
                    isDragging = true;
                    currentTarget.StartDragging(worldPos);
                }
            }
        }
        // 2. 正在拖拽：只要还按住，就持续跟随；松开才结束
        else
        {
            if (isPressing && currentTarget != null)
            {
                // 只要按住，就一直拖，无视系统瞬时的Ended假信号
                Vector2 worldPos = ScreenToWorldPointFixed(screenPos);
                currentTarget.FollowMouse(worldPos);
            }
            else
            {
                // 只有完全松开，才真正结束拖拽
                if (currentTarget != null)
                {
                    Vector2 worldPos = ScreenToWorldPointFixed(screenPos);
                    currentTarget.StopDragging(worldPos);
                }
                // 重置状态
                currentTarget = null;
                isDragging = false;
            }
        }



    }

    /// <summary>
    /// 统一获取：当前是否处于按住状态（鼠标/触摸合一，最可靠）
    /// </summary>
    private bool GetIsPressing()
    {
        if (Input.touchSupported && Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            return t.phase is TouchPhase.Began or TouchPhase.Moved or TouchPhase.Stationary;
        }
        else
        {
            return Input.GetMouseButton(0);
        }
    }

    /// <summary>
    /// 获取当前按住的屏幕坐标
    /// </summary>
    private Vector2 GetPressScreenPosition()
    {
        if (Input.touchSupported && Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        else
        {
            return Input.mousePosition;
        }
    }

    /// <summary>
    /// 修复Z轴错误的ScreenToWorldPoint，2D拖拽必须用这个
    /// </summary>
    private Vector2 ScreenToWorldPointFixed(Vector2 screenPos)
    {
        Vector3 screenPoint = new(screenPos.x, screenPos.y, mainCamera.nearClipPlane);
        return mainCamera.ScreenToWorldPoint(screenPoint);
    }

    /// <summary>
    /// 获取点击位置最上层的拼图碎片
    /// </summary>
    private DraggableComponent GetTopDraggable(Vector2 screenPos)
    {
        Vector2 worldPos = ScreenToWorldPointFixed(screenPos);
        int pieceLayer = LayerMask.NameToLayer("PuzzlePiece");
        int layerMask = 1 << pieceLayer;

        Collider2D[] cols = Physics2D.OverlapPointAll(worldPos, layerMask);

        DraggableComponent best = null;
        int maxOrder = -1;

        foreach (var col in cols)
        {
            DraggableComponent p = col.GetComponent<DraggableComponent>();
            if (p != null && p.sortingOrder > maxOrder)
            {
                maxOrder = p.sortingOrder;
                best = p;
            }
        }
        return best;
    }
}
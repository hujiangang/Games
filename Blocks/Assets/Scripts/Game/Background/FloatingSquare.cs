using UnityEngine;

public class FloatingSquare : MonoBehaviour
{
    private float moveSpeed;
    private float rotationSpeed;
    private float destroyY;

    private FloatingSquarePool floatingSquarePool;

    public void Setup(float speed, float rotSpeed, float size, Color color, float limitY, FloatingSquarePool pool)
    {
        moveSpeed = speed;
        rotationSpeed = rotSpeed;
        destroyY = limitY;

        transform.localScale = Vector3.one * size;
        
        // 设置颜色
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.color = color;
        // 确保背景层级足够低
        sr.sortingOrder = -20; 

        this.floatingSquarePool = pool;
    }

    void Update()
    {
        // 向上移动
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);
        
        // 自旋转
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);

        // 超出屏幕顶部销毁
        if (transform.position.y > destroyY)
        {
            floatingSquarePool.ReturnToPool(this.gameObject);
        }
    }
}
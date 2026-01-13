using UnityEngine;

public class BackgroundComponent : MonoBehaviour
{

    [Header("生成设置")]
    public Sprite squareSprite;      // 拖入一个普通白色正方形图片
    public float spawnInterval = 1f; // 生成间隔
    public float minX = -4f, maxX = 4f; // 生成的横向范围
    public float spawnY = -6f;       // 生成的起始高度（屏幕下方）
    public float destroyY = 8f;      // 销毁的高度（屏幕上方）

    [Header("随机属性范围")]
    public float minSpeed = 0.5f, maxSpeed = 1.5f;
    public float minRot = 30f, maxRot = 100f;
    public float minSize = 0.2f, maxSize = 0.6f;
    public Color[] possibleColors;    // 在面板里设置几种背景颜色

    private float timer;

    public FloatingSquarePool pool;


    void Awake()
    {
        pool = new();
        pool.Init(squareSprite, 20);

        timer = spawnInterval;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnSquare();
            timer = 0;
        }
    }

    void SpawnSquare()
    {
        // 1. 创建物体
        GameObject go = pool.Get();

        go.transform.position = new Vector3(Random.Range(minX, maxX), spawnY, 0);

        if (!go.TryGetComponent<FloatingSquare>(out var fs))
        {
            // 添加行为组件
            fs = go.AddComponent<FloatingSquare>();
        }
        
        // 4. 随机化属性
        float speed = Random.Range(minSpeed, maxSpeed);
        float rot = Random.Range(minRot, maxRot) * (Random.value > 0.5f ? 1 : -1); // 随机左右旋转
        float size = Random.Range(minSize, maxSize);
        Color col = possibleColors.Length > 0 ? possibleColors[Random.Range(0, possibleColors.Length)] : Color.white;
        // 降低背景颜色的亮度或透明度
        //col.a = 0.3f; 

        fs.Setup(speed, rot, size, col, destroyY,pool);
    }
}
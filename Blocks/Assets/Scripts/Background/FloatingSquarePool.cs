using System.Collections.Generic;
using UnityEngine;

public class FloatingSquarePool
{
    private Sprite squareSprite;      // 拖入一个普通白色正方形图片
    public int initialSize = 20;   // 初始生成的数量

    private Queue<GameObject> pool = new();

    public void Init(Sprite squareSprite, int initialSize)
    {
        this.squareSprite = squareSprite;
        this.initialSize = initialSize;
        // 预先生成并隐藏物体
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = GenerateNewObject();
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    private GameObject GenerateNewObject()
    {
        GameObject go = new GameObject("BGSquare");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        return go;
    }


    // 从池子中获取物体
    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            return GenerateNewObject();
        }
    }

    // 将物体归还池子
    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
using UnityEngine;


/// <summary>
/// 碎片从上往下掉落效果.
/// </summary>

public class FallAndEnableDrag : MonoBehaviour
{
    Vector3 start, end;
    PolygonCollider2D col;
    float t;
    const float duration = 0.5f;

    public void BeginFall(Vector3 s, Vector3 e)
    {
        start = s; end = e;
        col = GetComponent<PolygonCollider2D>();
        col.enabled = false;
    }

    void Update()
    {
        t += Time.deltaTime;
        float nt = t / duration;
        float y = Mathf.Lerp(start.y, end.y, nt * nt);
        transform.position = new Vector3(end.x, y, end.z);

        if (nt >= 1)
        {
            transform.position = end;
            // 落地后开碰撞，就能拖了.
            col.enabled = true;
            Destroy(this);
        }
    }
}
using UnityEngine;

public class KeepTilting : MonoBehaviour
{
    [Header("左右最大角度")]
    public float angle = 5f;

    [Header("单程耗时（秒）")]
    public float halfPeriod = 0.3f;   // 越小摆得越快

    private float startZ;

    void Awake()
    {
        startZ = transform.localEulerAngles.z;
    }

    void Update()
    {
        // -angle ~ +angle 来回
        float z = Mathf.PingPong(Time.time, halfPeriod * 2f) / (halfPeriod * 2f) * 2f - 1f;
        transform.localRotation = Quaternion.Euler(0, 0, startZ + z * angle);
    }
}
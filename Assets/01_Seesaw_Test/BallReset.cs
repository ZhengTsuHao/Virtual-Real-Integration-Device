using UnityEngine;

public class BallReset : MonoBehaviour
{
    [Header("巨型板子設定")]
    [Tooltip("板子的中心點 X (請填入板子的 Position X)")]
    public float boardCenterX = 371f; 

    [Tooltip("隨機範圍 (板子長度的一半再扣掉一點點)")]
    public float spawnRange = 150f; 

    [Tooltip("重生高度 (必須比板子的 Y 還要高)")]
    public float respawnHeight = 250f;

    [Header("死亡設定")]
    [Tooltip("掉到多深會重置 (建議設比板子低 50 單位)")]
    public float deadZoneY = 150f;   

    void Update()
    {
        // 檢查是否掉出界線
        if (transform.position.y < deadZoneY) ResetBall();
    }

    // 撞到地板就重置
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Ground") 
        {
            ResetBall();
        }
    }

    void ResetBall()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.velocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero;
        }

        // ⭐ 關鍵數學修正 ⭐
        // 隨機位置 = 板子中心點 (371) + 隨機偏移 (-150 ~ 150)
        float randomOffsetX = Random.Range(-spawnRange, spawnRange);
        float finalX = boardCenterX + randomOffsetX;

        // 瞬移
        transform.position = new Vector3(finalX, respawnHeight, 0);
    }
}
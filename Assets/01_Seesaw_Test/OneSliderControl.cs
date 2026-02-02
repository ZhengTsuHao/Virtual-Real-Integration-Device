using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class OneSliderControl : MonoBehaviour
{
    // ... (前面的變數都不變) ...
    [Header("UI 控制項")]
    [SerializeField] private Slider mainSlider;

    [Header("物理關節")]
    [SerializeField] private HingeJoint joint1;
    [SerializeField] private HingeJoint joint2;

    [Header("安全角度限制")]
    [SerializeField] private float minLimit = -45f;
    [SerializeField] private float maxLimit = 45f;

    [Header("連動參數")]
    [SerializeField] private float arm2Multiplier = 1.2f; 
    [SerializeField] private float smoothTime = 0.05f; 

    [Header("馬達校正")]
    [SerializeField] int offset1 = 0; 
    [SerializeField] int offset2 = 0;
    [SerializeField] bool reverse1 = false;
    [SerializeField] bool reverse2 = false;

    [Header("連線設定")]
    [SerializeField] private string targetIP = "192.168.1.117"; 
    [SerializeField] private int targetPort = 4210;

    // --- 內部變數 ---
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private float currentVelocity; 
    private float currentArm2Angle;
    private float lastSendTime;

    // ⭐ 位置 1：在這裡宣告平滑用的變數 (在所有變數的最下面)
    private float smoothServo1 = 90f; // 預設 90 避免剛開始暴衝
    private float smoothServo2 = 90f;

    private void Start()
    {
        try 
        {
            udpClient = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(targetIP.Trim()), targetPort);
        }
        catch (System.Exception e) { Debug.LogError("IP 錯誤: " + e.Message); }
    }

    private void Update()
    {
        if (mainSlider == null || joint1 == null) return;

        // --- 1. 計算 Slider & 物理目標 ---
        float rawValue = mainSlider.value;
        float targetAngle1 = Mathf.Clamp(rawValue, minLimit, maxLimit);
        
        float targetAngle2 = targetAngle1 * arm2Multiplier;
        currentArm2Angle = Mathf.SmoothDamp(currentArm2Angle, targetAngle2, ref currentVelocity, smoothTime);

        SetJointTarget(joint1, targetAngle1);
        if(joint2) SetJointTarget(joint2, currentArm2Angle);

        // --- 2. 讀取真實物理角度 ---
        float realTimeAngle1 = joint1.angle; 
        float realTimeAngle2 = (joint2 != null) ? joint2.angle : currentArm2Angle; 

        // --- 3. 計算原始目標數值 (還沒平滑) ---
        int rawServo1 = (int)(realTimeAngle1 + 90);
        int rawServo2 = (int)(realTimeAngle2 + 90);

        if (reverse1) rawServo1 = 180 - rawServo1;
        if (reverse2) rawServo2 = 180 - rawServo2;

        rawServo1 += offset1;
        rawServo2 += offset2;

        // 限制範圍
        rawServo1 = Mathf.Clamp(rawServo1, 0, 180);
        rawServo2 = Mathf.Clamp(rawServo2, 0, 180);

        // ==========================================
        // ⭐ 位置 2：插入這裡！(在 SendToESP32 之前)
        // ==========================================
        
        // 使用 Lerp 讓數值「滑」過去，而不是「跳」過去
        // 0.1f = 很滑順但稍慢
        // 0.3f = 反應快一點但稍微有點震動
        smoothServo1 = Mathf.Lerp(smoothServo1, rawServo1, 0.1f);
        smoothServo2 = Mathf.Lerp(smoothServo2, rawServo2, 0.1f);

        // 把平滑後的浮點數轉回整數發送
        SendToESP32((int)smoothServo1, (int)smoothServo2);
        
        // ==========================================
    }

    // ... (SetJointTarget 和 SendToESP32 都不用改) ...
    private void SetJointTarget(HingeJoint joint, float angle)
    {
        var spring = joint.spring;
        spring.targetPosition = angle;
        joint.spring = spring;
    }

    private void SendToESP32(int a1, int a2)
    {
        if (udpClient == null || Time.time - lastSendTime < 0.04f) return;
        try
        {
            string cmd = $"SET:{a1},{a2}";
            byte[] data = Encoding.UTF8.GetBytes(cmd);
            udpClient.Send(data, data.Length, remoteEndPoint);
            lastSendTime = Time.time;
        }
        catch {}
    }
}
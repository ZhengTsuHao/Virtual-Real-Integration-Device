using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO.Ports; // 引入 Serial 函式庫

public class SeesawSync : MonoBehaviour
{
    [Header("模式切換開關")]
    [Tooltip("勾選=有線(穩), 不勾=無線(抖)")]
    public bool useWiredConnection = false;

    [Header("網路設定 (WiFi)")]
    public string esp32_IP = "192.168.1.XXX";
    public int port = 4210;

    [Header("有線設定 (USB)")]
    [Tooltip("Mac通常是 /dev/tty.usb..., Windows是 COM3")]
    public string portName = "/dev/tty.usbserial-0001"; 
    public int baudRate = 115200;

    [Header("連結")]
    public HingeJoint boardJoint;
    public bool reverseDirection = false;
    public int angleOffset = 0;

    // 內部變數
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private SerialPort serialPort; // 序列埠物件
    private float smoothAngle = 90f;
    private float lastSendTime;

    void Start()
    {
        // 初始化 UDP
        try {
            udpClient = new UdpClient();
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(esp32_IP.Trim()), port);
        } catch {}

        // 初始化 Serial (如果不小心勾著開場，就嘗試連線)
        if (useWiredConnection) OpenSerialPort();
    }

    void Update()
    {
        // 即時檢查：如果你突然切換模式
        if (useWiredConnection && (serialPort == null || !serialPort.IsOpen)) {
            OpenSerialPort();
        } else if (!useWiredConnection && serialPort != null && serialPort.IsOpen) {
            CloseSerialPort();
        }

        // --- 物理計算 (跟之前一樣) ---
        if (boardJoint == null) return;
        float currentPhysicsAngle = boardJoint.angle;
        float targetServoAngle = currentPhysicsAngle + 90f;
        if (reverseDirection) targetServoAngle = 180f - targetServoAngle;
        targetServoAngle += angleOffset;
        smoothAngle = Mathf.Lerp(smoothAngle, targetServoAngle, 1.00f);

        // --- 發送訊號 ---
        if (Time.time - lastSendTime > 0.01f) { // 50Hz
            SendToESP32((int)smoothAngle);
            lastSendTime = Time.time;
        }
    }

    void SendToESP32(int angle)
    {
        angle = Mathf.Clamp(angle, 0, 180);
        string cmd = $"SET:{angle}";

        if (useWiredConnection)
        {
            // [有線模式]
            if (serialPort != null && serialPort.IsOpen) {
                try {
                    serialPort.WriteLine(cmd); // WriteLine 會自動加換行符號 \n
                } catch { Debug.LogWarning("Serial 傳送失敗"); }
            }
        }
        else
        {
            // [無線模式]
            try {
                byte[] data = Encoding.UTF8.GetBytes(cmd);
                if (udpClient != null) udpClient.Send(data, data.Length, remoteEndPoint);
            } catch {}
        }
    }

    void OpenSerialPort()
    {
        try {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50;
            serialPort.Open();
            Debug.Log($"✅ 有線連線成功: {portName}");
        } catch (System.Exception e) {
            Debug.LogError($"❌ 無法開啟 Serial Port (請檢查線或 Port 名稱): {e.Message}");
            useWiredConnection = false; // 自動切回無線以免報錯
        }
    }

    void CloseSerialPort()
    {
        if (serialPort != null && serialPort.IsOpen) {
            serialPort.Close();
            Debug.Log("🔌 有線連線已關閉");
        }
    }

    // 關閉遊戲時確保斷線
    void OnApplicationQuit()
    {
        CloseSerialPort();
        if (udpClient != null) udpClient.Close();
    }
}
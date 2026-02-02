using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class WifiMotor : MonoBehaviour
{
    [Header("連線設定")]
    [SerializeField]private string targetIP = "192.168.1.117"; // 【請填入剛剛抄的 IP】
    [SerializeField]private int targetPort = 4210;             // 必須跟 ESP32 一樣

    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;

 // 給按鈕呼叫的函式
    public void SendAngle(float angle)
    {
            angle = Mathf.Clamp(angle, 70f, 110f);
            string command = $"SET:{angle:F0}"; // 格式跟之前一樣
            
            byte[] data = Encoding.UTF8.GetBytes(command);
            udpClient.Send(data, data.Length, remoteEndPoint);
    }
    private void Start()
    {
        // 建立 UDP 發射器
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(targetIP), targetPort);
        Debug.Log($"Wi-Fi 控制準備就緒 -> {targetIP}");
    }
    private void OnDestroy()
    {
        TryQuitConnection();
    }
    private void OnApplicationQuit()
    {
        TryQuitConnection();
    }
    private void TryQuitConnection()
    {
        if (udpClient != null) udpClient.Close();
    }
}
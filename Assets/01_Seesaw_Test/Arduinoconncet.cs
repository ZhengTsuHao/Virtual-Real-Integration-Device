using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class Arduinoconncet : MonoBehaviour
{
    // 1. 檢查這裡！確定你的 Arduino 真的是插在 COM9 嗎？(去 Arduino IDE 看一下)
    public string portName = "COM9";  
    
    // 2. 這裡我幫你改成 9600 了，為了配合上一輪給你的 Arduino 程式
    public int baudRate = 9600; 

    private SerialPort serialPort;

    // 雖然你說不需要感測器，但留著這個定義沒關係
    public delegate void OnDetectEvent(); 
    public event OnDetectEvent OnDetect; 

    void Start()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            
            // 【重要新增】設定超時，防止 Unity 當機
            serialPort.ReadTimeout = 50; 
            
            serialPort.Open();
            Debug.Log("串口已開啟：" + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("無法開啟串口：" + e.Message);
        }
    }

    void Update()
    {
        if (serialPort == null || !serialPort.IsOpen) return;

        try 
        {
            // 檢查是否有訊息
            if (serialPort.BytesToRead > 0)
            {
                string message = serialPort.ReadLine().Trim();
                // 這裡留著也沒關係，只要 Arduino 不傳 "DETECT" 過來，這段就不會執行
                if (message == "DETECT")
                {
                    OnDetect?.Invoke(); 
                }
            }
        }
        catch (System.TimeoutException) 
        { 
            // 這是正常的，讀不到資料就跳過，確保不當機
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("讀取錯誤：" + e.Message);
        }
    }

    public void SendAngle(float angle)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            angle = Mathf.Clamp(angle, 70f, 110f);
            
            // F0 代表整數 (例如 90)，F1 代表小數 (例如 90.5)
            // 只要 Arduino 那邊是用 .toInt() 或 .toFloat() 都可以通
            string command = $"SET:{angle:F0}"; 

            try
            {
                serialPort.WriteLine(command);
                // Debug.Log($"發送: {command}"); // 測試成功後可以把這行註解掉，效能比較好
            }
            catch (System.Exception e)
            {
                Debug.LogError("寫入失敗：" + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
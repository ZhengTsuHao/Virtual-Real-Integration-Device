#include <WiFi.h>
#include <WiFiUdp.h>
#include <ESP32Servo.h>

// ================= 你的 Wi-Fi 設定 =================
const char* ssid = "VitaSpace_Life";     // ✅ 已填入
const char* password = "0903926475";     // ✅ 已填入

const int PIN_SEESAW = 13;
const int UDP_PORT = 4210;
// =================================================

Servo seesawServo;
WiFiUDP udp;
char packetBuffer[255];
int lastMicro = 0; 

void setup() {
  // 高速 Serial
  Serial.begin(500000); 
  Serial.setTimeout(2); // ⚠️ 超時改極短 (2ms)

  seesawServo.setPeriodHertz(50);
  seesawServo.attach(PIN_SEESAW, 600, 2400); 
  seesawServo.write(90);

  Serial.println("正在連線 WiFi...");
  WiFi.begin(ssid, password);
  
  int tryCount = 0;
  while (WiFi.status() != WL_CONNECTED && tryCount < 20) {
    delay(200);
    Serial.print(".");
    tryCount++;
  }
  
  if(WiFi.status() == WL_CONNECTED){
    Serial.println("\n✅ WiFi OK");
    Serial.println(WiFi.localIP());
    udp.begin(UDP_PORT);
  } else {
    Serial.println("\n⚠️ WiFi Fail, using Serial");
  }
}

void loop() {
  String cmd = "";

  // --- 管道 A: WiFi (維持原樣) ---
  int packetSize = udp.parsePacket();
  if (packetSize) {
    int len = udp.read(packetBuffer, 255);
    if (len > 0) packetBuffer[len] = 0;
    cmd = String(packetBuffer);
  }

  // --- 管道 B: USB Serial (⭐ 重大修改：防堆積邏輯) ---
  if (Serial.available()) {
    // 迴圈讀取直到緩衝區空掉，只保留「最後一條」指令
    while (Serial.available() > 0) {
      String temp = Serial.readStringUntil('\n');
      temp.trim();
      if (temp.length() > 0) cmd = temp; // 更新為最新指令
    }
  }

  // --- 執行指令 ---
  if (cmd.startsWith("SET:")) {
    int targetAngle = cmd.substring(4).toInt();
    targetAngle = constrain(targetAngle, 0, 180);
    int targetMicro = map(targetAngle, 0, 180, 600, 2400);
    
    // 死區設為 3，兼顧速度與防抖
    if (abs(targetMicro - lastMicro) > 3) { 
      seesawServo.writeMicroseconds(targetMicro);
      lastMicro = targetMicro;
    }
  }
}

#include <WiFiNINA.h>
#include <ArduinoHttpClient.h>
#include <Arduino_MKRIoTCarrier.h>

MKRIoTCarrier carrier;

// wifi
const char* ssid = "Wifinavn";
const char* password = "passowrd";

// Server
const char* serverAddress = "iknogetendnu.com";
int port = 80;

WiFiClient wifi;
HttpClient client = HttpClient(wifi, serverAddress, port);

int score = 0;
int currentMole = -1;
unsigned long lastMove = 0;
int interval = 1500; // ms

void setup() {
  Serial.begin(9600);
  carrier.begin();

  connectWiFi();
  carrier.display.fillScreen(0);
}

void loop() {
  unsigned long now = millis();

  if (now - lastMove > interval) {
    spawnMole();
    lastMove = now;
  }

  checkHit();
}

void connectWiFi() {
  Serial.print("Connecting to WiFi...");
  while (WiFi.begin(ssid, password) != WL_CONNECTED) {
    delay(2000);
    Serial.print(".");
  }
  Serial.println("Connected!");
}

void spawnMole() {
  carrier.display.fillScreen(0);
  currentMole = random(0, 5);

  carrier.display.setCursor(20 * currentMole, 30);
  carrier.display.print("O");
}

void checkHit() {
  if (carrier.Buttons.onTouchDown(TOUCH0 + currentMole)) {
    score++;
    Serial.print("Score: ");
    Serial.println(score);

    sendScore();

    carrier.display.fillScreen(0);
    delay(300);
    currentMole = -1;
  }
}

void sendScore() {
  String contentType = "application/json";
  String postData = "{\"score\":" + String(score) + "}";

  client.beginRequest();
  client.post("/api/score");
  client.sendHeader("Content-Type", contentType);
  client.sendHeader("Content-Length", postData.length());
  client.beginBody();
  client.print(postData);
  client.endRequest();

  int statusCode = client.responseStatusCode();
  String response = client.responseBody();

  Serial.print("Status: ");
  Serial.println(statusCode);
  Serial.print("Response: ");
  Serial.println(response);
}

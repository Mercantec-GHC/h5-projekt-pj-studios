#include <WiFiNINA.h>
#include <ArduinoHttpClient.h>
#include <Arduino_MKRIoTCarrier.h>

MKRIoTCarrier carrier;

// ---------------- WIFI ----------------
const char* ssid = "Familien.Fischer";
const char* password = "Norregade29";

// Server
const char* serverAddress = "h5-projekt-pj-studios-1.onrender.com";
int port = 443;

WiFiSSLClient wifi;
HttpClient client = HttpClient(wifi, serverAddress, port);

// ---------------- USER ----------------
String email = "Sanne@Sanne.com";
String passwordUser = "Sanne1234!";
String jwtToken = "";

// ---------------- GAME ----------------
int score = 0;
int lives = 3;

int moleIndex = -1;
bool gameRunning = false;

unsigned long lastSpawn = 0;
int moleTime = 1500;

// ---------------- TOUCH ----------------
touchButtons touches[5] = {TOUCH0, TOUCH1, TOUCH2, TOUCH3, TOUCH4};

// ---------------- SEND SCORE ----------------
void sendScore() {
  if (jwtToken == "") return;

  String postData = "{\"score\":" + String(score) + "}";

  client.beginRequest();
  client.post("/api/User/addscore");

  client.sendHeader("Content-Type", "application/json");
  client.sendHeader("Authorization", "Bearer " + jwtToken);
  client.sendHeader("Content-Length", postData.length());

  client.beginBody();
  client.print(postData);
  client.endRequest();
}

// ---------------- LOGIN ----------------
void loginUser() {
  String postData = "{\"email\":\"" + email + "\",\"password\":\"" + passwordUser + "\"}";

  client.beginRequest();
  client.post("/api/User/login");

  client.sendHeader("Content-Type", "application/json");
  client.sendHeader("Content-Length", postData.length());

  client.beginBody();
  client.print(postData);
  client.endRequest();

  String response = client.responseBody();

  int index = response.indexOf("\"token\":\"");
  if (index != -1) {
    int start = index + 9;
    int end = response.indexOf("\"", start);
    jwtToken = response.substring(start, end);
  }
}

// ---------------- GAME OVER ----------------
void gameOver() {
  gameRunning = false;

  carrier.display.fillScreen(0);
  carrier.display.setCursor(40, 50);
  carrier.display.print("Game Over");

  carrier.display.setCursor(40, 80);
  carrier.display.print("Score: ");
  carrier.display.print(score);

  sendScore();
}

// ---------------- SETUP ----------------
void setup() {
  Serial.begin(9600);
  carrier.begin();

  carrier.display.setTextSize(2);

  while (WiFi.begin(ssid, password) != WL_CONNECTED) {
    delay(2000);
  }

  loginUser();

  carrier.display.fillScreen(0);
  carrier.display.setCursor(30, 60);
  carrier.display.print("Tryk TOUCH0");
}

// ---------------- LOOP ----------------
void loop() {

  carrier.Buttons.update();

  if (!gameRunning) {
    if (carrier.Buttons.getTouch(TOUCH0)) {
      gameRunning = true;
      score = 0;
      lives = 3;
    }
    return;
  }

  if (lives <= 0) {
    gameOver();
    return;
  }
}
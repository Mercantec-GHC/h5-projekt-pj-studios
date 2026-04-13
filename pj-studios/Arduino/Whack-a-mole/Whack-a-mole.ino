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

// ---------------- LED CONTROL ----------------
void clearMoleLED() {
  for (int i = 0; i < 5; i++) {
    carrier.leds.setPixelColor(i, 0, 0, 0);
  }
  carrier.leds.show();
}

void showMoleLED(int index) {
  clearMoleLED();

  moleIndex = index;

  // grøn mole
  carrier.leds.setPixelColor(moleIndex, 0, 150, 0);
  carrier.leds.show();

  lastSpawn = millis();
}

// ---------------- DRAW UI ----------------
void drawUI() {
  carrier.display.fillScreen(0);

  carrier.display.setCursor(10, 10);
  carrier.display.print("Score: ");
  carrier.display.print(score);

  carrier.display.setCursor(10, 30);
  carrier.display.print("Liv: ");
  carrier.display.print(lives);
}

// ---------------- SPAWN MOLE ----------------
void spawnMole() {
  drawUI();

  int newIndex = random(0, 5);
  showMoleLED(newIndex);
}

// ---------------- HIT CHECK ----------------
void checkTouch() {
  for (int i = 0; i < 5; i++) {

    if (carrier.Buttons.getTouch(touches[i])) {

      if (i == moleIndex) {
        score++;

        clearMoleLED();
        spawnMole();

      } else {
        lives--;

        if (lives <= 0) {
          gameOver();
        }
      }

      delay(200);
    }
  }
}

// ---------------- GAME OVER ----------------
void gameOver() {
  gameRunning = false;

  clearMoleLED();

  carrier.display.fillScreen(0);

  carrier.display.setCursor(40, 50);
  carrier.display.print("Game Over");

  carrier.display.setCursor(40, 80);
  carrier.display.print("Score: ");
  carrier.display.print(score);

  sendScore();
}

// ---------------- START GAME ----------------
void startGame() {
  score = 0;
  lives = 3;
  gameRunning = true;

  spawnMole();
}

// ---------------- LOOP ----------------
void loop() {

  carrier.Buttons.update();

  if (!gameRunning) {
    if (carrier.Buttons.getTouch(TOUCH0)) {
      startGame();
      delay(300);
    }
    return;
  }

  // miss timeout
  if (millis() - lastSpawn > moleTime) {
    lives--;

    if (lives <= 0) {
      gameOver();
      return;
    }

    spawnMole();
  }

  checkTouch();
}

// ---------------- SETUP ----------------
void setup() {
  Serial.begin(9600);
  carrier.begin();

  carrier.display.setTextSize(2);

  connectWiFi();
  loginUser();

  carrier.display.fillScreen(0);
  carrier.display.setCursor(30, 60);
  carrier.display.print("Tryk TOUCH0");
}

// ---------------- WIFI ----------------
void connectWiFi() {
  while (WiFi.begin(ssid, password) != WL_CONNECTED) {
    delay(2000);
  }
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
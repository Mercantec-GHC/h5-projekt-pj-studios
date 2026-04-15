#include <WiFiNINA.h>
#include <ArduinoHttpClient.h>
#include <Arduino_MKRIoTCarrier.h>

MKRIoTCarrier carrier;

// ---------------- WIFI ----------------
const char* ssid = "Familien.Fischer";
const char* wifiPassword = "Norregade29";

// ---------------- SERVER ----------------
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

// ---------------- LED ----------------
void clearMoleLED() {
  for (int i = 0; i < 5; i++) {
    carrier.leds.setPixelColor(i, 0, 0, 0);
  }
  carrier.leds.show();
}

void showMoleLED(int index) {
  clearMoleLED();

  moleIndex = index;
  carrier.leds.setPixelColor(moleIndex, 0, 150, 0);
  carrier.leds.show();

  lastSpawn = millis();

  Serial.print("MOLE SPAWNED AT: ");
  Serial.println(moleIndex);
}

// ---------------- UI ----------------
void drawUI() {
  carrier.display.fillScreen(0);

  carrier.display.setCursor(10, 10);
  carrier.display.print("Score: ");
  carrier.display.print(score);

  carrier.display.setCursor(10, 30);
  carrier.display.print("Liv: ");
  carrier.display.print(lives);
}

// ---------------- MOLE ----------------
void spawnMole() {
  drawUI();

  int newIndex = random(0, 5);
  showMoleLED(newIndex);
}

// ---------------- TOUCH ----------------
void checkTouch() {

  for (int i = 0; i < 5; i++) {

    if (carrier.Buttons.getTouch(touches[i])) {

      Serial.print("TOUCH: ");
      Serial.println(i);

      Serial.print("MOLE: ");
      Serial.println(moleIndex);

      if (moleIndex == -1) return;

      if (i == moleIndex) {

        Serial.println("HIT!");

        score++;

        clearMoleLED();
        spawnMole();
      }
      else {
        Serial.println("MISS!");

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

  Serial.println("GAME OVER TRIGGERED");

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
  Serial.println("GAME STARTED");

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

  if (millis() - lastSpawn > moleTime) {

    Serial.println("TIMEOUT - MISS");

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
  Serial.println("Connecting WiFi...");

  while (WiFi.begin(ssid, wifiPassword) != WL_CONNECTED) {
    delay(2000);
    Serial.println("Retry WiFi...");
  }

  Serial.println("WiFi CONNECTED!");
}

// ---------------- LOGIN ----------------
void loginUser() {

  Serial.println("Logging in...");

  String postData =
    "{\"email\":\"" + email + "\",\"password\":\"" + passwordUser + "\"}";

  client.beginRequest();
  client.post("/api/User/login");

  client.sendHeader("Content-Type", "application/json");
  client.sendHeader("Content-Length", postData.length());

  client.beginBody();
  client.print(postData);
  client.endRequest();

  int statusCode = client.responseStatusCode();
  String response = client.responseBody();

  Serial.print("Login status: ");
  Serial.println(statusCode);

  Serial.println("Login response:");
  Serial.println(response);

  int index = response.indexOf("\"token\":\"");

  if (index != -1) {
    int start = index + 9;
    int end = response.indexOf("\"", start);

    jwtToken = response.substring(start, end);

    Serial.println("TOKEN OK:");
    Serial.println(jwtToken);
  }
  else {
    Serial.println("TOKEN NOT FOUND!");
  }
}

// ---------------- SEND SCORE ----------------
void sendScore() {

  Serial.println("SENDSCORE CALLED");

  if (jwtToken == "") {
    Serial.println("NO TOKEN - STOP");
    return;
  }

  String postData =
    "{\"email\":\"" + email + "\",\"score\":" + String(score) + "}";

  Serial.println("POST DATA:");
  Serial.println(postData);

  client.beginRequest();
  client.post("/api/User/addscore");

  client.sendHeader("Content-Type", "application/json");
  client.sendHeader("Content-Length", postData.length());

  client.beginBody();
  client.print(postData);
  client.endRequest();

  int status = client.responseStatusCode();
  String response = client.responseBody();

  Serial.print("SCORE STATUS: ");
  Serial.println(status);

  Serial.println("SCORE RESPONSE:");
  Serial.println(response);
}
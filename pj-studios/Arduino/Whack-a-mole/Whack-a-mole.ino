#include <WiFiNINA.h>
#include <ArduinoHttpClient.h>
#include <Arduino_MKRIoTCarrier.h>

MKRIoTCarrier carrier;

// WiFi
const char* ssid = "Wifinavn";
const char* password = "WifiPassword";

// Server
const char* serverAddress = "https://localhost:7087"; //Vent til Hosting
int port = 80;

WiFiClient wifi;
HttpClient client = HttpClient(wifi, serverAddress, port);

// USER
String email = "Jens@Jens.com";      // Din email
String passwordUser = "Jens1234";         // Dit password
String userId = "";                    // Gemmes efter login

// Game
int score = 0;
int lives = 3;
int currentMole = -1;

unsigned long lastMove = 0;
int interval = 1500;

bool gameRunning = false;

// Touch mapping
touchButtons getTouchButton(int index) {
  switch (index) {
    case 0: return TOUCH0;
    case 1: return TOUCH1;
    case 2: return TOUCH2;
    case 3: return TOUCH3;
    case 4: return TOUCH4;
    default: return TOUCH0;
  }
}

// ---------------- SETUP ----------------
void setup() {
  Serial.begin(9600);
  carrier.begin();
  randomSeed(analogRead(0));

  connectWiFi();
  loginUser();
  showStartScreen();
}

// ---------------- LOOP ----------------
void loop() {
  if (!gameRunning) {
    if (carrier.Buttons.onTouchDown(TOUCH0)) {
      startGame();
    }
    return;
  }

  unsigned long now = millis();

  if (now - lastMove > interval) {
    missMole();
    spawnMole();
    lastMove = now;
  }

  checkHit();
}

// ---------------- WIFI ----------------
void connectWiFi() {
  carrier.display.fillScreen(0);
  carrier.display.setCursor(20, 40);
  carrier.display.print("Connecting...");

  while (WiFi.begin(ssid, password) != WL_CONNECTED) {
    delay(2000);
  }

  carrier.display.fillScreen(0);
  carrier.display.setCursor(20, 40);
  carrier.display.print("Connected!");
  delay(1000);
}

// ---------------- LOGIN ----------------
void loginUser() {
  carrier.display.fillScreen(0);
  carrier.display.setCursor(10, 40);
  carrier.display.print("Logging in...");

  String postData = "{\"email\":\"" + email + "\",\"password\":\"" + passwordUser + "\"}";

  client.beginRequest();
  client.post("/api/User/login");
  client.sendHeader("Content-Type", "application/json");
  client.sendHeader("Content-Length", postData.length());
  client.beginBody();
  client.print(postData);
  client.endRequest();

  int statusCode = client.responseStatusCode();
  String response = client.responseBody();

  Serial.print("Login Status: "); Serial.println(statusCode);
  Serial.print("Response: "); Serial.println(response);

  // -------------------------------
  // Simpel parsing for id fra JSON
  // forventer "id":"1234..."
  int index = response.indexOf("\"id\":\"");
  if (index != -1) {
    int start = index + 6; // efter "id":"
    int end = response.indexOf("\"", start);
    if (end != -1) {
      userId = response.substring(start, end);
    }
  }

  if (userId != "") {
    carrier.display.fillScreen(0);
    carrier.display.setCursor(10, 40);
    carrier.display.print("Login success!");
    delay(1000);
  } else {
    carrier.display.fillScreen(0);
    carrier.display.setCursor(10, 40);
    carrier.display.print("Login failed!");
    while (true) { delay(1000); } // Stop spillet
  }
}

// ---------------- UI ----------------
void showStartScreen() {
  carrier.display.fillScreen(0);
  carrier.display.setCursor(10, 40);
  carrier.display.print("Tryk TOUCH0");
  carrier.display.setCursor(10, 60);
  carrier.display.print("for start");
}

void drawHUD() {
  carrier.display.setCursor(0, 0);
  carrier.display.print("Score:");
  carrier.display.print(score);

  carrier.display.setCursor(0, 15);
  carrier.display.print("Liv:");
  carrier.display.print(lives);
}

// ---------------- GAME ----------------
void startGame() {
  score = 0;
  lives = 3;
  interval = 1500;
  gameRunning = true;
  spawnMole();
}

void spawnMole() {
  carrier.display.fillScreen(0);
  currentMole = random(0, 5);
  carrier.display.setCursor(20 * currentMole, 40);
  carrier.display.print("M");
  drawHUD();
}

void checkHit() {
  if (currentMole >= 0 && carrier.Buttons.onTouchDown(getTouchButton(currentMole))) {
    score++;
    if (interval > 500) interval -= 50;
    spawnMole();
  }
}

void missMole() {
  if (currentMole != -1) {
    lives--;
    if (lives <= 0) {
      gameOver();
    }
  }
}

void gameOver() {
  gameRunning = false;
  carrier.display.fillScreen(0);
  carrier.display.setCursor(20, 30);
  carrier.display.print("Game Over");
  carrier.display.setCursor(20, 50);
  carrier.display.print("Score:");
  carrier.display.print(score);
  sendScore();
}

// ---------------- API ----------------
void sendScore() {
  if (userId == "") return; // tjek login

  String postData = "{\"userId\":\"" + userId + "\",\"score\":" + String(score) + "}";

  client.beginRequest();
  client.post("/api/User/addscore"); // ✅ din rigtige route
  client.sendHeader("Content-Type", "application/json");
  client.sendHeader("Content-Length", postData.length());
  client.beginBody();
  client.print(postData);
  client.endRequest();

  int statusCode = client.responseStatusCode();
  String response = client.responseBody();

  Serial.print("Score Status: "); Serial.println(statusCode);
  Serial.print("Response: "); Serial.println(response);
}
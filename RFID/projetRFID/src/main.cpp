#include <SPI.h>
#include <MFRC522.h>

#define RST_PIN 0
#define SS_PIN 5
MFRC522 rfid(SS_PIN, RST_PIN);
MFRC522::MIFARE_Key key;

String mode = "lecture"; // "lecture" ou "ecriture"
String currentRole = "";
String currentID = "";
bool readyToWrite = false;

void setup() {
  Serial.begin(9600);
  SPI.begin();
  rfid.PCD_Init();

  for (byte i = 0; i < 6; i++) key.keyByte[i] = 0xFF;

  Serial.println("MODE PAR DÉFAUT: LECTURE");
  Serial.println("Tapez 'a' (admin) ou 'u' (utilisateur) pour écrire une carte RFID.");
}

String readBlock(byte block) {
  byte buffer[18];
  byte size = sizeof(buffer);

  MFRC522::StatusCode status = rfid.PCD_Authenticate(MFRC522::PICC_CMD_MF_AUTH_KEY_A, block, &key, &(rfid.uid));
  if (status != MFRC522::STATUS_OK) return "";

  status = rfid.MIFARE_Read(block, buffer, &size);
  if (status != MFRC522::STATUS_OK) return "";

  String data = "";
  for (int i = 0; i < 16; i++) {
    if (buffer[i] != 0) data += (char)buffer[i];
  }
  return data;
}

void writeToCard(String id, String role) {
  Serial.println("Carte détectée. Écriture...");

  byte bufferID[16] = {0};
  id.getBytes(bufferID, 16);
  byte bufferRole[16] = {0};
  role.getBytes(bufferRole, 16);

  if (rfid.PCD_Authenticate(MFRC522::PICC_CMD_MF_AUTH_KEY_A, 4, &key, &(rfid.uid)) != MFRC522::STATUS_OK ||
      rfid.MIFARE_Write(4, bufferID, 16) != MFRC522::STATUS_OK) {
    Serial.println("❌ Échec écriture bloc 4.");
    return;
  }

  if (rfid.PCD_Authenticate(MFRC522::PICC_CMD_MF_AUTH_KEY_A, 5, &key, &(rfid.uid)) != MFRC522::STATUS_OK ||
      rfid.MIFARE_Write(5, bufferRole, 16) != MFRC522::STATUS_OK) {
    Serial.println("❌ Échec écriture bloc 5.");
    return;
  }

  Serial.println("✅ Carte écrite: ID=" + id + ", rôle=" + role);
}

void loop() {
  // Entrée utilisateur
  if (Serial.available()) {
    char input = Serial.read();

    if (input == 'a') {
      mode = "ecriture";
      currentRole = "admin";
      currentID = "ADMIN_01";
      readyToWrite = true;
      Serial.println("Mode écriture ADMIN. Approchez une carte...");
    } else if (input == 'u') {
      mode = "ecriture";
      currentRole = "user";
      currentID = "USER_01";
      readyToWrite = true;
      Serial.println("Mode écriture UTILISATEUR. Approchez une carte...");
    }
  }

  // Détection de carte
  if (!rfid.PICC_IsNewCardPresent() || !rfid.PICC_ReadCardSerial()) return;

  if (mode == "ecriture" && readyToWrite) {
    writeToCard(currentID, currentRole);
    readyToWrite = false;
    mode = "lecture";
    Serial.println("Retour en mode lecture...");
    rfid.PICC_HaltA();
    rfid.PCD_StopCrypto1();
    delay(1500);
    return;
  }

  // Lecture normale
  String uid = "";
  for (byte i = 0; i < rfid.uid.size; i++) {
    uid += (rfid.uid.uidByte[i] < 0x10 ? "0" : "");
    uid += String(rfid.uid.uidByte[i], HEX);
  }
  uid.toUpperCase();

  String id = readBlock(4);
  String role = readBlock(5);

  if (id != "" && role != "") {
    Serial.println("{\"card_id\":\"" + id + "\",\"role\":\"" + role + "\"}");
  } else {
    Serial.println("❌ Échec lecture carte.");
  }

  rfid.PICC_HaltA();
  rfid.PCD_StopCrypto1();
  delay(1000);
}

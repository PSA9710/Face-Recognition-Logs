#include <SoftwareSerial.h>

int bluetoothTx = 2;  // TX-O pin of bluetooth mate, Arduino D2
int bluetoothRx = 3;  // RX-I pin of bluetooth mate, Arduino D3

int ledGreen = 10;
int ledYellow = 9;
int ledRed = 8;

int triggerDistance=7;
int echoDistance=6;

SoftwareSerial bluetooth(bluetoothTx, bluetoothRx);

void setup()
{
  Serial.begin(9600);  // Begin the serial monitor at 9600bps

  bluetooth.begin(115200);  // The Bluetooth Mate defaults to 115200bps
  bluetooth.print("$");  // Print three times individually
  bluetooth.print("$");
  bluetooth.print("$");  // Enter command mode
  delay(100);  // Short delay, wait for the Mate to send back CMD
  bluetooth.println("U,9600,N");  // Temporarily Change the baudrate to 9600, no parity
  // 115200 can be too fast at times for NewSoftSerial to relay the data reliably
  bluetooth.begin(9600);  // Start bluetooth serial at 9600

  
  
  pinMode(ledGreen, OUTPUT);
  pinMode(ledYellow, OUTPUT);
  pinMode(ledRed, OUTPUT);
  pinMode(ECHOPIN, INPUT);
  pinMode(TRIGPIN, OUTPUT);

  
}

void loop()
{
  if (bluetooth.available()) // If the bluetooth sent any characters
  {
    char c = (char)bluetooth.read();
    // Send any characters the bluetooth prints to the serial monitor
    Serial.print(c);
    SwitchLedGreen(c);
    SwitchLedYellow(c);
    SwitchLedRed(c);

  }
  if (Serial.available()) // If stuff was typed in the serial monitor
  {
    // Send any characters the Serial monitor prints to the bluetooth
    bluetooth.print((char)Serial.read());
  }

  CalculateDistance()
  // and loop forever and ever!
}


float CalculateDistance()
{
  digitalWrite(TRIGPIN, LOW);
  delayMicroseconds(2);
  digitalWrite(TRIGPIN, HIGH);
  delayMicroseconds(10);
  digitalWrite(TRIGPIN, LOW);
  // Compute distance
  float distance = pulseIn(ECHOPIN, HIGH);
  distance= distance/58;
  Serial.println(distance);
  delay(200);
}


void SwitchLedGreen(char c)
{
  if (c == 'H')
    digitalWrite(ledGreen, HIGH);
  else
    digitalWrite(ledGreen, LOW);
}

void SwitchLedYellow(char c)
{
  if (c == 'H')
    digitalWrite(ledYellow, HIGH);
  else
    digitalWrite(ledYellow, LOW);
}


void SwitchLedRed(char c)
{
  if (c == 'H')
    digitalWrite(ledRed, HIGH);
  else
    digitalWrite(ledRed, LOW);
}



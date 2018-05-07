#include <SoftwareSerial.h>
#define H 'H'
#define L 'L'


int bluetoothTx = 2;  // TX-O pin of bluetooth mate, Arduino D2
int bluetoothRx = 3;  // RX-I pin of bluetooth mate, Arduino D3

int ledGreen = 10;
int ledYellow = 9;
int ledRed = 8;

int triggerDistancePin = 7;
int echoDistancePin = 6;

int threshold = 250; //the maximum distance accepted in mm
float distance = 300;
bool isInRange = false;
bool isConnected = false;

unsigned long startMilliSeconds = 0;


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
  pinMode(echoDistancePin, INPUT);
  pinMode(triggerDistancePin, OUTPUT);
  startMilliSeconds = millis();


}

void loop()
{ String z = "";
  if (bluetooth.available()) // If the bluetooth sent any characters
  {
    z = bluetooth.readString();
    Serial.print(z);
  }

  if (z == "WHO AM I") {
    bluetooth.print("YOU ARE ROOT");
    Serial.println("ROOT");
    delay(1000);
    z=bluetooth.readString();
    if(z=="OK")
    isConnected = true;
   
  }
  if (z == "BYEbye")
  {
    isConnected = false;
    SwitchLedYellow(L);
    SwitchLedRed(L);
    SwitchLedGreen(L);
  }
  if (isConnected)
  {

    CalculateDistance();
    // and loop forever and ever!
  }
}


float CalculateDistance()
{
  bool isInRangeOld = isInRange;
  digitalWrite(triggerDistancePin, LOW);
  delayMicroseconds(2);
  digitalWrite(triggerDistancePin, HIGH);
  delayMicroseconds(10);
  digitalWrite(triggerDistancePin, LOW);
  // Compute distance
  distance = pulseIn(echoDistancePin, HIGH);
  distance = distance / 58;
 // Serial.println(distance);
  if (distance > threshold)
  {
    isInRange = false;
    if (isInRange != isInRangeOld)
    {
      SwitchLedYellow(L);
      SwitchLedRed(H);
     // bluetooth.println("R");
    }
  }
  else
  {
    isInRange = true;
    if (isInRange != isInRangeOld)
    {
      SwitchLedYellow(H);
      SwitchLedRed(L);
     // bluetooth.println("Y");
    }
  }
  SendDistanceAfterTime();
  delay(200);
}

void SendDistanceAfterTime()
{
  unsigned long currentTime = millis();
  unsigned long elapsedMilliSeconds = currentTime - startMilliSeconds;
 // if (elapsedMilliSeconds > 500)
  {
    int dst = (int)distance;
    //bluetooth.print("DST:"); 
    bluetooth.print(dst);
    bluetooth.print("!");   //message divider
    delay(20); //delay to allow message to be transmitted
    //Serial.println("trimis");
    startMilliSeconds = millis();
  }
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



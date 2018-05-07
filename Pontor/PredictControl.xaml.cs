using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using InTheHand.Net.Sockets;
using System.Management;
using System.Threading;

namespace Pontor
{
    /// <summary>
    /// Interaction logic for PredictControl.xaml
    /// </summary>
    public partial class PredictControl : UserControl
    {

        List<Image<Gray, byte>> images = new List<Image<Gray, byte>>();
        public SerialPort serialPort;
        bool isBluetoothConnected = false;
        Dictionary<String, String> bluetoothDevices = new Dictionary<string, string>();
        TextBlock ConsoleOutput;
        ScrollViewer consoleScrollViewer;
        private string bluetoothDeviceName;
        private string bluetoothDevicePort;


        public String message;
        public event EventHandler MessageRecieved;



        public PredictControl(TextBlock textBlock,ScrollViewer scroll)
        {
            InitializeComponent();
            ConsoleOutput = textBlock;
            consoleScrollViewer = scroll;
            WriteToConsole("Bluetooth : Starting devices query");
            Thread t = new Thread(() => { PopulateComboBoxWithSerialPorts(); });
            t.Start();


        }

        private void PopulateComboBoxWithSerialPorts()
        {
            BluetoothClient client = new BluetoothClient();
            var devices = client.DiscoverDevicesInRange();
            int i = 0;
            foreach (BluetoothDeviceInfo d in devices)
            {

                string s = GetBluetoothPort(deviceAddress: d.DeviceAddress.ToString());
                if (s != null)
                {
                    bluetoothDevices.Add(d.DeviceName, s);
                    i++;
                    Dispatcher.Invoke(() => BluetoothDevicesList.Items.Add(d.DeviceName));
                }
            }
            WriteToConsole("Bluetooth : Query finished! Found " + i.ToString() + " devices");
            Dispatcher.Invoke(() => BluetoothDevicesList.IsEnabled = true);
        }


        private void BluetoothDevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BluetoothDevicesList.SelectedIndex != -1)
            {
                bluetoothDeviceName = BluetoothDevicesList.SelectedValue.ToString();
                bluetoothDevicePort = bluetoothDevices[BluetoothDevicesList.SelectedValue.ToString()];
                WriteToConsole("Bluetooth : Selected " + bluetoothDeviceName + " " + bluetoothDevicePort);
                Thread t = new Thread(() => ConnectToComPort(bluetoothDevicePort));
                t.Start();
            }

        }

        private void ConnectToComPort(string bluetoothDevice)
        {
            Thread.Sleep(1000);
            if (serialPort != null && serialPort.IsOpen)
            {
                DisconnectFromBluetooth();
                //serialPort.Close();
            }
            try
            {
                serialPort = new SerialPort(bluetoothDevice, 9600);
                serialPort.DataReceived += new SerialDataReceivedEventHandler(MessageReciever);
                serialPort.NewLine = "\r\n";
                WriteToConsole("Bluetooth : Atempting to connect to " + bluetoothDeviceName + "...");
                serialPort.Open();
                serialPort.Write("WHO AM I");
                WriteToConsole("Bluetooth : Connection opened to " + bluetoothDeviceName);
                isBluetoothConnected = false;
            }
            catch (Exception e)
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                MessageBox.Show(e.ToString());
            }
            finally
            {

            }
        }

        private void MessageReciever(object sender, SerialDataReceivedEventArgs e)
        {
            message += serialPort.ReadExisting();
            ////WriteToConsole(message);
            ////MessageBox.Show(message);
            if (!isBluetoothConnected)
            {
                CheckIfCorrectBluetooth();
            }
            else
                ProcessMessage();

        }

        private void ProcessMessage()
        {
            if (message.Length == 1)
            {
                ////LEDMessage();
            }

            var messages = message.Split('!');
            message = messages[messages.Length - 1];
            messages = messages.Take(messages.Count() - 1).ToArray();
            foreach (String str in messages)
            {
              ////  WriteToConsole(str);
               DistanceMessage(str);
            }
        }

        private void DistanceMessage(string message)
        {
            //message = message.Remove(0, 4);

            ////WriteToConsole(message);
            int distance = Convert.ToInt32(message);
            if (distance < 250)
            {
                ChangeLinearGradientBrushTriangle(distance);
            }
            else
                Dispatcher.Invoke(() =>
                {
                    if (RadarTriangle.Fill != null)
                        RadarTriangle.Fill = null;
                });
        }

        private void ChangeLinearGradientBrushTriangle(int distance)
        {
            // Dispatcher.Invoke(() => { RadarTriangle.Fill = linearGradientBrush; });
            Dispatcher.Invoke(() =>
            {
                ////Polygon radarTriangle = new Polygon
                ////{
                ////    Points = GetTrianglePoints(),
                ////    Stroke=Brushes.Black,
                ////    StrokeThickness = 3,
                ////    Stretch = Stretch.Uniform
                ////};
                Color gradientMiddle = GetColorBasedOnDistance(distance);
                Color gradientBackground = Color.FromArgb(255, 172, 172, 172);
                LinearGradientBrush linearGradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1)
                };
                ////WriteToConsole("dst" + distance.ToString());
                double offset = distance / 250.0;
                ////WriteToConsole(offset.ToString());
                offset = (double)1 - offset;
                double offsetTop = offset - 0.15;
                double offsetBottom = offset + 0.15;
                offsetTop = Clamp(offsetTop, 0, 1);
                offsetBottom = Clamp(offsetBottom, 0, 1);
                linearGradientBrush.GradientStops.Add(new GradientStop(gradientBackground, offsetTop));
                linearGradientBrush.GradientStops.Add(new GradientStop(gradientMiddle, offset));
                linearGradientBrush.GradientStops.Add(new GradientStop(gradientBackground, offsetBottom));
                RadarTriangle.Fill = linearGradientBrush;
            });
        }

        private double Clamp(double val, double min, double max)
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        private Color GetColorBasedOnDistance(int distance)
        {
            float offset = distance / 250;
            byte alpha = 255;
            byte red = 0, blue = 0, green = 0;
            if (distance > 180)
            {
                red = 255;
                green = 255;
            }
            else if (distance > 40)
            {
                green = 255;
            }
            else
            {
                red = 255;
            }
            return Color.FromArgb(alpha, red, green, blue);
        }

        ////private void LEDMessage()
        ////{
        ////    Dispatcher.Invoke(() =>
        ////    {
        ////        if (message == "R")
        ////        {
        ////            LED.Fill = new SolidColorBrush(Colors.Red);
        ////        }
        ////        else if (message == "Y")
        ////        {
        ////            LED.Fill = new SolidColorBrush(Colors.Yellow);
        ////        }
        ////        else if (message == "G")
        ////        {
        ////            LED.Fill = new SolidColorBrush(Colors.Lime);
        ////        }
        ////        if (MessageRecieved != null)
        ////            MessageRecieved(this, EventArgs.Empty);
        ////    });

        ////}

        private void CheckIfCorrectBluetooth()
        {
            //if (message.Contains("ROOT"))
            if (message.Length == 12)
                if (message == "YOU ARE ROOT")
                {
                    WriteToConsole("Succesfully connected to " + bluetoothDeviceName);
                    isBluetoothConnected = true;
                    serialPort.Write("OK");
                    message = null;
                }
                else
                {

                    DisconnectFromBluetooth();

                }
        }

        public void DisconnectFromBluetooth()
        {
            serialPort.Write("BYEbye");
            isBluetoothConnected = false;
            RemoveComPort();
        }

        private void RemoveComPort()
        {
            var s = "Connected to the wrong device...Closing connection to " + bluetoothDeviceName;
            WriteToConsole(s);
            if (serialPort.IsOpen)
                serialPort.Close();


        }


        private string GetBluetoothPort(string deviceAddress)
        {
            const string Win32_SerialPort = "Win32_SerialPort";
            SelectQuery q = new SelectQuery(Win32_SerialPort);
            ManagementObjectSearcher s = new ManagementObjectSearcher(q);
            foreach (object cur in s.Get())
            {
                ManagementObject mo = (ManagementObject)cur;
                string pnpId = mo.GetPropertyValue("PNPDeviceID").ToString();

                if (pnpId.Contains(deviceAddress))
                {
                    object captionObject = mo.GetPropertyValue("Caption");
                    string caption = captionObject.ToString();
                    int index = caption.LastIndexOf("(COM");
                    if (index > 0)
                    {
                        string portString = caption.Substring(index);
                        string comPort = portString.
                                      Replace("(", string.Empty).Replace(")", string.Empty);
                        return comPort;
                    }
                }
            }
            return null;
        }


        private void WriteToConsole(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ConsoleOutput.Text += DateTime.Now.ToString() + " : ";
                ConsoleOutput.Text += message + "\n";
                consoleScrollViewer.ScrollToBottom();
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
              {

                  Thread.Sleep(1000);
                  if (serialPort != null && serialPort.IsOpen)
                  {
                      DisconnectFromBluetooth();
                      //serialPort.Close();
                  }
                  try
                  {
                      serialPort = new SerialPort("COM6", 9600);
                      serialPort.DataReceived += new SerialDataReceivedEventHandler(MessageReciever);
                      serialPort.NewLine = "\r\n";
                      WriteToConsole("Bluetooth : Atempting to connect to " + "...");
                      serialPort.Open();
                      Thread.Sleep(100);
                      serialPort.Write("WHO AM I");
                      WriteToConsole("Bluetooth : Connection opened to ");
                      isBluetoothConnected = false;
                  }
                  catch (Exception ex)
                  {
                      if (serialPort.IsOpen)
                          serialPort.Close();
                      MessageBox.Show(ex.ToString());
                  }
                  finally
                  {

                  }
              }); t.Start();
        }
    }
}

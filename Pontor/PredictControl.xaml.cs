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
        private string bluetoothDeviceName;
        private string bluetoothDevicePort;


        public String message;
        public event EventHandler MessageRecieved;


        


        public PredictControl(TextBlock textBlock)
        {
            InitializeComponent();
            ConsoleOutput = textBlock;

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
            message = serialPort.ReadLine();
            //MessageBox.Show(message);
            if (!isBluetoothConnected)
            {
                CheckIfCorrectBluetooth();
            }
            ProcessMessage();

        }

        private void ProcessMessage()
        {
            if (message.Length == 1)
            {
                LEDMessage();
            }
            if (message.Contains("Distance:"))
            {
                DistanceMessage(message);
            }
        }

        private void DistanceMessage(string message)
        {
            message = message.Remove(0, 9);
            WriteToConsole(message);
            int distance = Convert.ToInt32(message);
            if (distance < 250)
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

                Dispatcher.Invoke(() =>
                {
                    gradientMiddle.Color = Color.FromArgb(alpha, red, green, blue);
                    gradientMiddle.Offset = offset;
                });
            }
        }

        private void LEDMessage()
        {
            Dispatcher.Invoke(() =>
            {
                if (message == "R")
                {
                    LED.Fill = new SolidColorBrush(Colors.Red);
                }
                else if (message == "Y")
                {
                    LED.Fill = new SolidColorBrush(Colors.Yellow);
                }
                else if (message == "G")
                {
                    LED.Fill = new SolidColorBrush(Colors.Lime);
                }
                if (MessageRecieved != null)
                    MessageRecieved(this, EventArgs.Empty);
            });

        }

        private void CheckIfCorrectBluetooth()
        {
            //if (message.Contains("ROOT"))
            if (message == "YOU ARE ROOT")
            {
                WriteToConsole("Succesfully connected to " + bluetoothDeviceName);
                isBluetoothConnected = true;
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
            });
        }
    }
}

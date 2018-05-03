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
        Label ConsoleOutput;
        public PredictControl(Label label)
        {
            InitializeComponent();
            ConsoleOutput = label;
            Thread t = new Thread(() => { PopulateComboBoxWithSerialPorts(); });
            t.Start();

        }

        private void PopulateComboBoxWithSerialPorts()
        {
            BluetoothClient client = new BluetoothClient();
            var devices = client.DiscoverDevicesInRange();
            foreach (BluetoothDeviceInfo d in devices)
            {

                string s = GetBluetoothPort(deviceAddress: d.DeviceAddress.ToString());
                if (s != null)
                {
                    bluetoothDevices.Add(d.DeviceName,s);
                    Dispatcher.Invoke(() => BluetoothDevicesList.Items.Add(d.DeviceName));
                }
            }
            Dispatcher.Invoke(() => BluetoothDevicesList.IsEnabled = true);
        }


        private void BluetoothDevicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BluetoothDevicesList.SelectedIndex != -1)
            {
                var bluetoothDevice = bluetoothDevices[BluetoothDevicesList.SelectedValue.ToString()];
                WriteToConsole("Selected " + bluetoothDevice);
                Thread t = new Thread(() => ConnectToComPort(bluetoothDevice));
                t.Start();
            }

        }

        private void ConnectToComPort(string bluetoothDevice)
        {
            Thread.Sleep(1000);
            if (serialPort != null && serialPort.IsOpen)
                serialPort.Close();
            try
            {
                serialPort = new SerialPort(bluetoothDevice, 9600);
                serialPort.DataReceived += new SerialDataReceivedEventHandler(MessageRecieved);
                serialPort.NewLine = "\r\n";
                WriteToConsole("Atempting to connect...");
                serialPort.Open();
                serialPort.Write("WHO AM I");
                WriteToConsole("Connection opened");
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

        private void MessageRecieved(object sender, SerialDataReceivedEventArgs e)
        {
            var message = serialPort.ReadLine();
            MessageBox.Show(message);
            if (!isBluetoothConnected)
            {
                CheckIfCorrectBluetooth(message);
            }

        }

        private void CheckIfCorrectBluetooth(string message)
        {
            //if (message.Contains("ROOT"))
            if (message == "YOU ARE ROOT")
            {
                WriteToConsole("Connection succesful");
                isBluetoothConnected = true;
            }
            else
            {
                RemoveComPort();
                isBluetoothConnected = false;
            }
        }

        private void RemoveComPort()
        {
            var s = "Connected to the wrong device";
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
                ConsoleOutput.Content += DateTime.Now.ToString() + " : ";
                ConsoleOutput.Content += message + "\n";
            });
        }
    }
}

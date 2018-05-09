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
using System.Drawing;
using System.Windows.Interop;
using System.Timers;

namespace Pontor
{
    /// <summary>
    /// Interaction logic for PredictControl.xaml
    /// </summary>
    public partial class PredictControl : UserControl
    {


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);


        Dictionary<int, Image<Gray, byte>> toBeSaved = new Dictionary<int, Image<Gray, byte>>();


        List<Image<Gray, byte>> images = new List<Image<Gray, byte>>();
        public SerialPort serialPort;
        public bool isBluetoothConnected = false;
        public bool isArduinoEnabled = false;
        Dictionary<String, String> bluetoothDevices = new Dictionary<string, string>();
        TextBlock ConsoleOutput;
        ScrollViewer consoleScrollViewer;
        private string bluetoothDeviceName;
        private string bluetoothDevicePort;


        public String message;
        private bool isPersonInRange = true;

        public event EventHandler MessageRecieved;
        public System.Timers.Timer timerSave = new System.Timers.Timer();


        public PredictControl(TextBlock textBlock, ScrollViewer scroll)
        {
            InitializeComponent();
            ConsoleOutput = textBlock;
            consoleScrollViewer = scroll;
            WriteToConsole("Bluetooth : Starting devices query");
            Thread t = new Thread(() => { PopulateComboBoxWithSerialPorts(); });
            t.Start();


            timerSave.Elapsed += new ElapsedEventHandler(TimeToSave);
            timerSave.Interval = 60000;
            timerSave.Start();


        }

        private void TimeToSave(object state, ElapsedEventArgs e)
        {
            TimeToSaveFired();
            ////throw new NotImplementedException();
        }

        public void TimeToSaveFired()
        {
            try
            {
                if (toBeSaved.Count == 0) { WriteToConsole("Minute Save : No faces were recorded in the last minute"); return; }
                foreach (var reg in toBeSaved)
                {
                    long idLog = SqlManager.SQL_InsertIntoLogs(DateTime.Now.ToString(), reg.Key);
                    saveImageToLogs(reg.Key, idLog);
                }
                toBeSaved.Clear();
                Dispatcher.Invoke(() =>
                {
                    displayDetectedFaces.Children.Clear();
                });
                WriteToConsole("Minute Save : Records Saved");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }
        }

        private void saveImageToLogs(int key, long idLog)
        {
            var location = AppDomain.CurrentDomain.BaseDirectory.ToString();
            location += "/Logs-Pictures/" + idLog.ToString() + ".bmp";
            Image<Gray, byte> image = toBeSaved[key];
            image.Save(location);
            ////throw new NotImplementedException();
        }

        private void PopulateComboBoxWithSerialPorts()
        {
            try
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
            catch (Exception e)
            {
                MessageBox.Show("Bluetooth not open or connected. Please turn it on and restart the app!");
            }
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

                Disconnect.IsEnabled = true;
                Connect.IsEnabled = false;
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
                TimerForBluetoothConnection();
            }
            catch (System.IO.IOException e)
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                MessageBox.Show("Please reconnect the device to the PC");
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
                isPersonInRange = true;
            }
            else
                Dispatcher.Invoke(() =>
                {
                    if (RadarTriangle.Fill != null)
                        RadarTriangle.Fill = null;
                    isPersonInRange = false;
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
                System.Windows.Media.Color gradientMiddle = GetColorBasedOnDistance(distance);
                System.Windows.Media.Color gradientBackground = System.Windows.Media.Color.FromArgb(255, 172, 172, 172);
                LinearGradientBrush linearGradientBrush = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0.5, 0),
                    EndPoint = new System.Windows.Point(0.5, 1)
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

        private System.Windows.Media.Color GetColorBasedOnDistance(int distance)
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
            return System.Windows.Media.Color.FromArgb(alpha, red, green, blue);
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

        private void TimerForBluetoothConnection()
        {
            System.Timers.Timer timerBT = new System.Timers.Timer();
            timerBT.AutoReset = false;
            timerBT.Elapsed += new ElapsedEventHandler(bluetoothCheckConnection);
            timerBT.Interval = 10000;
            timerBT.Start();
        }

        private void bluetoothCheckConnection(object sender, ElapsedEventArgs e)
        {
            if (isBluetoothConnected) return;
            DisconnectFromBluetooth();
        }

        public void DisconnectFromBluetooth()
        {
            try
            {
                serialPort.Write("BYEbye");

            }
            catch (Exception)
            { }
            finally
            {
                isBluetoothConnected = false;
                message = "";
                Dispatcher.Invoke(() => { Connect.IsEnabled = true;
                Disconnect.IsEnabled = false;
            });
                RemoveComPort();
            }
        }

        private void RemoveComPort()
        {
            var s = "Bluetooth : Closing connection to " + bluetoothDeviceName;
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


        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(()=>{ConnectToComPort(bluetoothDevicePort); });
            t.Start();
            Connect.IsEnabled = false;
            Disconnect.IsEnabled = true;
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect.IsEnabled = false;
            Connect.IsEnabled = true;
            if (serialPort != null && serialPort.IsOpen)
            {
                DisconnectFromBluetooth();
                //serialPort.Close();
            }
        }

        List<Image<Gray, byte>> medianDetectedFacesList = new List<Image<Gray, byte>>();
        List<int> medianDetectedIdList = new List<int>();

        public void getMedianFaceRecognition(Image<Gray, byte> image, int id)
        {
            if (!isPersonInRange) return;
            if (id == -1) return;
            if (medianDetectedFacesList.Count == 10 && medianDetectedIdList.Count == 10)
            {
                ProcessTheMedianLists();
                medianDetectedFacesList.Clear();
                medianDetectedIdList.Clear();
            }
            else
            {
                medianDetectedIdList.Add(id);
                medianDetectedFacesList.Add(image.Copy());
            }
        }

        private void ProcessTheMedianLists()
        {
            ////WriteToConsole("proccesthemedianlist");
            int idWithMostOccurences = medianDetectedIdList[0];
            int value = -1;
            var idOccurrences = medianDetectedIdList.GroupBy(i => i).ToDictionary(g => g.Key, g => g.Count());
            foreach (var idOccurrence in idOccurrences)
            {
                if (idOccurrence.Value > value)
                {
                    idWithMostOccurences = idOccurrence.Key;
                    value = idOccurrence.Value;
                }
            }
            if (value >= 8)
            {
                Image<Gray, byte> img = medianDetectedFacesList[medianDetectedIdList.IndexOf(idWithMostOccurences)];
                AddDetectedFaceToDisplay(img);
                AddDetectedFacesToSaveList(img, idWithMostOccurences);
            }
            ////throw new NotImplementedException();
        }

        private void AddDetectedFacesToSaveList(Image<Gray, byte> img, int idWithMostOccurences)
        {
            try
            {
                toBeSaved.Add(idWithMostOccurences, img);
            }
            catch (ArgumentException e)
            { }
            ////throw new NotImplementedException();
        }

        public void AddDetectedFaceToDisplay(Image<Gray, byte> detectedFace)
        {
            if (!isPersonInRange) return;
            Dispatcher.Invoke(() =>
            {
                ImageSource imageSource = ConvertToImageSource(detectedFace.ToBitmap());
                Border border = new Border() { Padding = new Thickness(5) };
                border.Child = new System.Windows.Controls.Image() { Source = imageSource, Width = 95, Height = 95 };
                displayDetectedFaces.Children.Add(border);
            });
            WriteToConsole("Added face to console");
        }

        private ImageSource ConvertToImageSource(Bitmap bmp)
        {

            IntPtr hBitmap = bmp.GetHbitmap();
            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmp.Dispose();
            DeleteObject(hBitmap);
            return wpfBitmap;

        }

        private void ArduinoEnabled_Checked(object sender, RoutedEventArgs e)
        {
            isArduinoEnabled = true;
        }

        private void ArduinoEnabled_Unchecked(object sender, RoutedEventArgs e)
        {
            isArduinoEnabled = false;
        }
    }
}

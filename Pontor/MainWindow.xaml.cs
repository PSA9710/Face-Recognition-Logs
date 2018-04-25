using System;
using System.Collections.Generic;
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
using WebEye.Controls.Wpf;

namespace Pontor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            List<WebCameraId> cameraList = new List<WebCameraId>(WebCam.GetVideoCaptureDevices());
            WebCam.StartCapture(cameraList[0]);

        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (WebCam.IsCapturing)
                WebCam.StopCapture();
        }
    }
}

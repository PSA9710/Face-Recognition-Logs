using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Data.SQLite;
using AForge.Video.DirectShow;

namespace Pontor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


    public partial class MainWindow : Window
    {

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);




        CascadeClassifier cascadeClassifier;
        DispatcherTimer timer;
        //WebCameraControl WebCam;

        VideoCapture WebCam;

        public MainWindow()
        {
            InitializeComponent();
            PopulateStreamOptions();

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(ProcessImage);
            timer.Interval = new TimeSpan(10);
            var locatie = System.AppDomain.CurrentDomain.BaseDirectory;
            cascadeClassifier = new CascadeClassifier(locatie + "/haarcascade_frontalface_alt.xml");
            
            
        }

        private void PopulateStreamOptions()
        {
            FilterInfoCollection x = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            int id = 0;
            foreach(FilterInfo info in x)
            {
                StreamingOptions.Items.Add(id);
                id++;
            }
            StreamingOptions.Items.Add("VIA IP");
        }

        private void WebCam_ImageGrabbed(object sender, EventArgs e)
        {
            Mat m = new Mat();
            WebCam.Retrieve(m);
            this.Dispatcher.Invoke(() =>
            {
                proc(m.Bitmap);
               // ImgViewer.Source = ConvertToImageSource(m.Bitmap);
            });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            //   timer.Start();
            if(StreamingOptions.SelectedIndex==-1)
            {
                MessageBox.Show("Please select streaming Device");
                return;
            }
            if (StreamingOptions.SelectedItem.ToString() == "VIA IP")
            {
                WebCam = new VideoCapture("http://admin:@192.168.0.120:8080/video");
            }
            else
            {
                int id = Convert.ToInt32(StreamingOptions.SelectedItem);
                WebCam = new VideoCapture(id);
            }
            WebCam.ImageGrabbed += WebCam_ImageGrabbed;
            WebCam.Start();

        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            //timer.Stop();
            WebCam.Stop();
           
        }


        private void ProcessImage(object sender, EventArgs e)
        {
            //Bitmap bitmap = WebCam.QueryFrame().Bitmap;
            ////bitmap = DetectFace(bitmap);
           // ImgViewer.Source = ConvertToImageSource(bitmap);
            Image<Bgr, byte> actualImage = WebCam.QueryFrame().ToImage<Bgr, byte>();
            if (actualImage != null)
            {
                Image<Gray, byte> grayImage = actualImage.Convert<Gray, byte>();
                var faces = cascadeClassifier.DetectMultiScale(grayImage, 1.1, 4); //the actual face detection happens here
                foreach (var face in faces)
                {
                    actualImage.Draw(face, new Bgr(255, 0, 0), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them
                    
                }
            }
            ImgViewer.Source = ConvertToImageSource(actualImage.ToBitmap());
           // ImgViewer.Source = ConvertToImageSource(WebCam.QueryFrame().Bitmap);
        }

        private void proc(Bitmap bmp)
        {
            //Bitmap bitmap = WebCam.QueryFrame().Bitmap;
            ////bitmap = DetectFace(bitmap);
            //ImgViewer.Source = ReturnImageAsSource(bitmap);
            Image<Bgr, byte> actualImage = new Image<Bgr, byte>(bmp);
            if (actualImage != null)
            {
                Image<Gray, byte> grayImage = actualImage.Convert<Gray, byte>();
                var faces = cascadeClassifier.DetectMultiScale(grayImage, 1.1, 2); //the actual face detection happens here
                foreach (var face in faces)
                {
                    actualImage.Draw(face, new Bgr(255, 0, 0), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

                }
            }
            ImgViewer.Source = ConvertToImageSource(actualImage.ToBitmap());
            // ImgViewer.Source = ConvertToImageSource(WebCam.QueryFrame().Bitmap);
        }

        private ImageSource ConvertToImageSource(Bitmap bmp)
        {
            
            IntPtr hBitmap = bmp.GetHbitmap();
            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmp.Dispose();
            DeleteObject(hBitmap);
            return wpfBitmap;
            
        }

    }
}

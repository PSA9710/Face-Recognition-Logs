using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Data.SQLite;
using AForge.Video.DirectShow;
using System.IO;
using System.Windows.Controls;

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


        public static int  capturesTaken = 0;
        private static int capturesToBeTaken = 10;
        public static String pathToSavePictures;





        int sizeToBeSaved = 100;//size of the picture wich will be saved

        CascadeClassifier cascadeClassifier;
        DispatcherTimer timer;
        //WebCameraControl WebCam;

        TrainingControl trainingControl;
        PredictControl predictControl;
        VideoCapture WebCam;

        public MainWindow()
        {
            InitializeComponent();
            PopulateStreamOptions();

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(ProcessImage);
            timer.Interval = new TimeSpan(10);
            var location = System.AppDomain.CurrentDomain.BaseDirectory;
            cascadeClassifier = new CascadeClassifier(location + "/haarcascade_frontalface_alt.xml");

            CheckIfDirectoryExists(location);
            SwitchToPredictMode();
            pathToSavePictures = location + "/pictures";
        }

        private void CheckIfDirectoryExists(String location)
        {
            try
            {
                if (Directory.Exists(location + "/pictures"))
                {
                    LoadPictures();
                }
                else
                {
                    Directory.CreateDirectory(location + "/pictures");
                }
                
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void LoadPictures()
        {
            throw new NotImplementedException();
        }

        private void PopulateStreamOptions()
        {
            //get all connected webcams
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
                WebCam = new VideoCapture("http://admin:@10.14.10.37:8080/video");
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
                double scaleFactor = Convert.ToDouble(ScaleFactorValue.Text);
                int minNeigbours = Convert.ToInt32(MinNeigboursValue.Text);
                var faces = cascadeClassifier.DetectMultiScale(grayImage, scaleFactor, minNeigbours); //the actual face detection happens here
                foreach (var face in faces)
                {
                    //get just the detected area(face)
                    var graycopy=actualImage.Copy(face).Convert<Gray, byte>().Resize(sizeToBeSaved,sizeToBeSaved,Inter.Cubic);
                    if (capturesTaken < capturesToBeTaken && ModeSelector.IsChecked == true)
                    {
                        trainingControl.AddPictureToCollection(graycopy);
                        capturesTaken++;
                    }
                    //draw rectangle on detected face
                    actualImage.Draw(face, new Bgr(255, 0, 0), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them


                    //display name over detected face
                    CvInvoke.PutText(actualImage,"Person", new System.Drawing.Point(face.X - 2, face.Y - 2),FontFace.HersheyComplex,1,new Bgr(0,255,0).MCvScalar);
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

        private void ModeSelector_Checked(object sender, RoutedEventArgs e)
        {
            
            try {
                ModeSelector.Content = "Switch to Predict Mode";
                if (predictControl != null)
                    CustomControlContainer.Children.Remove(predictControl);
                SwitchToTrainingMode();
            }
            catch(Exception ex)
            { MessageBox.Show(ex.ToString()); }
        }

        
        
        private void SwitchToPredictMode()
        {
            predictControl = new PredictControl();
            predictControl.Name = "WorkSpace";
           
            CustomControlContainer.Children.Add(predictControl);

        }

        private void SwitchToTrainingMode()
        {
            trainingControl = new TrainingControl();
            trainingControl.Name = "WorkSpace";
            CustomControlContainer.Children.Add(trainingControl);
            
        }

        private void ModelSelector_Unchecked(object sender, RoutedEventArgs e)
        {
            
            try
            {
                ModeSelector.Content = "Switch to Training Mode!";
                if (trainingControl != null)
                    CustomControlContainer.Children.Remove(trainingControl);
                SwitchToPredictMode();
            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString()); }
        }
    }
}

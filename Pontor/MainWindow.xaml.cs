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
using System.Collections.Generic;
using Emgu.CV.Face;
using System.Threading;
using Emgu.CV.Cuda;
using Emgu.CV.UI;

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


        public static int capturesTaken = 0;
        public static int capturesToBeTaken = 100;
        public static String pathToSavePictures;

        bool imagesFound = false;
        Image<Gray, byte>[] trainingImages;
        int[] personID;
        EigenFaceRecognizer faceRecognizer = new EigenFaceRecognizer(90, 2500);


        int sizeToBeSaved = 100;//size of the picture wich will be saved

        CascadeClassifier cascadeClassifier;
        DispatcherTimer timer;
        //WebCameraControl WebCam;
        String cudaClassifierFileName;

        TrainingControl trainingControl = new TrainingControl();
        PredictControl predictControl;
        VideoCapture WebCam;

        bool isPersonInRange = false;
        bool detectFaces = true;
        bool isTraining = false;
        bool isGpuEnabled = false;
        bool isCudaEnabled = false;

        double scaleFactor ;
        int minNeigbours ;

        String appLocation;

        public MainWindow()
        {
            InitializeComponent();
            PopulateStreamOptions();


            predictControl = new PredictControl(ConsoleOutput);
            predictControl.MessageRecieved += new EventHandler(MessageRecieved);



            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(ProcessImage);
            timer.Interval = new TimeSpan(10);
            var location = System.AppDomain.CurrentDomain.BaseDirectory;
            appLocation = location;
            cascadeClassifier = new CascadeClassifier(location + "/haarcascade_frontalface_alt_CPU.xml");
            cudaClassifierFileName = location + "/haarcascade_frontalface_alt.xml";
            CreateDirectory(location, "data");
            CreateDirectory(location, "pictures");


            SwitchToPredictMode();
            pathToSavePictures = location + "/pictures";
            new SqlManager().SQL_CheckforDatabase();


            LoadImages(location);


            CheckIfCudaIsEnabled();

        }

        private void CheckIfCudaIsEnabled()
        {
            if (CudaInvoke.HasCuda)
            {
                isCudaEnabled = true;
            }
            else
            {
                isCudaEnabled = false;
                hardwareSelector.IsEnabled = false;
            }
        }

        private void MessageRecieved(object sender, EventArgs e)
        {
            var message = predictControl.message;
            if (message == "R")
            {
                detectFaces = false;
            }
            else if (message == "Y")
            {
                detectFaces = true;
            }
        }

        public void TrainFaceRecognizer()
        {
            Thread t = new Thread(() =>
            {
                Thread.Sleep(500);
                WriteToConsole("FaceRecognizer : Training...");
                isTraining = true;
                //Dispatcher.Invoke
                faceRecognizer.Train(trainingImages, personID);
                WriteToConsole("FaceRecognizer : Finished Training");
                isTraining = false;
            });
            t.Start();
            //faceRecognizer.Write("/data/ceva");
            //throw new NotImplementedException();
        }

        public void LoadImages(String location)
        {
            Thread t = new Thread(() =>
              {
                  location += "/pictures";
                  int count = Directory.GetFiles(location).Length;
                  if (count > 0)
                  {
                      WriteToConsole("FaceRecognizer : Found " + count.ToString() + " images.");
                      WriteToConsole("FaceRecognizer : Loading Images...");
                  }
                  trainingImages = new Image<Gray, byte>[count];
                  personID = new int[count];
                  int i = 0;
                  foreach (string file in Directory.EnumerateFiles(location, "*.bmp"))
                  {
                      trainingImages[i] = new Image<Gray, byte>(file);
                      string filename = Path.GetFileName(file);
                      var fileSplit = filename.Split('_');
                      int personid = Convert.ToInt32(fileSplit[0]);
                      personID[i] = personid;
                      i++;
                      imagesFound = true;
                  }
                  if (!imagesFound)
                  {
                      MessageBox.Show("No pictures were found, please register a person", "Data not available", MessageBoxButton.OK, MessageBoxImage.Warning);
                      Dispatcher.Invoke(() => { ModeSelector.IsChecked = true; });
                  }
                  if (imagesFound)
                  {
                      WriteToConsole("FaceRecognizer : Images Loaded succesfully");
                      TrainFaceRecognizer();
                  }
              });
            t.Start();

        }

        private void CreateDirectory(string location, string folder)
        {
            Directory.CreateDirectory(location + "/" + folder);
        }


        private void PopulateStreamOptions()
        {
            //get all connected webcams
            FilterInfoCollection x = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            int id = 0;
            foreach (FilterInfo info in x)
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

            if (m != null)
                this.Dispatcher.Invoke(() =>
                {
                    proc(m);
                    // ImgViewer.Source = ConvertToImageSource(m.Bitmap);
                });
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (StreamingOptions.SelectedIndex == -1)
            {
                MessageBox.Show("Please select streaming Device");
                return;
            }
            if (StreamingOptions.SelectedItem.ToString() == "VIA IP")
            {
                string url = "http://";
                url += UsernameStream.Text + ":";
                url += PasswordStream.Text + "@";
                url += IP1.Text + ".";
                url += IP2.Text + ".";
                url += IP3.Text + ".";
                url += IP4.Text;
                url += ":8080/video";

                WebCam = new VideoCapture(url);
                WriteToConsole("Camera : Connected to external camera");
            }
            else
            {
                int id = Convert.ToInt32(StreamingOptions.SelectedItem);
                WebCam = new VideoCapture(id);
                WriteToConsole("Camera : Connected to internal camera");
            }
            WebCam.ImageGrabbed += WebCam_ImageGrabbed;
            WebCam.Start();

        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
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
            //ImgViewer.Source = ConvertToImageSource(actualImage.ToBitmap());
            // ImgViewer.Source = ConvertToImageSource(WebCam.QueryFrame().Bitmap);
        }



        private void proc(Mat bmp)
        {
            //Bitmap bitmap = WebCam.QueryFrame().Bitmap;
            ////bitmap = DetectFace(bitmap);
            //ImgViewer.Source = ReturnImageAsSource(bitmap);
            if (isCudaEnabled && isGpuEnabled)
            {
                ProcessWithGPU(bmp);
            }
            else
            {
                ProcessWithCPU(bmp);
            }
            // ImgViewer.Source = ConvertToImageSource(WebCam.QueryFrame().Bitmap);
        }

        private void ProcessWithGPU(Mat bmp)
        {
            using (CudaImage<Bgr, byte> cudaCapturedImage = new CudaImage<Bgr, byte>(bmp.ToImage<Bgr, byte>()))
            {
                using (CudaImage<Gray, byte> cudaGrayImage = cudaCapturedImage.Convert<Gray, byte>())
                {
                    Rectangle[] faces = FindFacesUsingGPU(cudaGrayImage);
                    using (Image<Bgr, byte> capturedImage = cudaCapturedImage.ToImage())
                    {
                        foreach (Rectangle face in faces)
                        {
                            capturedImage.Draw(face, new Bgr(255, 0, 0), 3);
                        }
                        imageDisplay.Image = capturedImage;
                    }
                }
            }
        }

        private Rectangle[] FindFacesUsingGPU(CudaImage<Gray, byte> cudaCapturedImage)
        {
            using (CudaCascadeClassifier face = new CudaCascadeClassifier(cudaClassifierFileName))
            using (GpuMat faceRegionMat = new GpuMat())
            {
                face.ScaleFactor = scaleFactor;
                face.MinNeighbors = minNeigbours;
                face.DetectMultiScale(cudaCapturedImage, faceRegionMat);
                Rectangle[] faceRegion = face.Convert(faceRegionMat);
                return faceRegion;
            }
        }


        //private void ProcessWithCPU(Mat bmp)
        //{
        //    using (Image<Bgr, byte> capturedImage = new Image<Bgr, byte>(bmp.Bitmap))
        //    {
        //        using (Image<Gray, byte> grayCapturedImage = capturedImage.Convert<Gray, byte>())
        //        {

        //        }
        //    }
        //}

        private void ProcessWithCPU(Mat bmp)
        {
            Image<Bgr, byte> actualImage = new Image<Bgr, byte>(bmp.Bitmap);
            if (actualImage != null && detectFaces)
            {
                Image<Gray, byte> grayImage = actualImage.Convert<Gray, byte>();
                double scaleFactor = Convert.ToDouble(ScaleFactorValue.Text);
                int minNeigbours = Convert.ToInt32(MinNeigboursValue.Text);
                var faces = cascadeClassifier.DetectMultiScale(grayImage, scaleFactor, minNeigbours); //the actual face detection happens here
                foreach (var face in faces)
                {
                    //get just the detected area(face)
                    var graycopy = actualImage.Copy(face).Convert<Gray, byte>().Resize(sizeToBeSaved, sizeToBeSaved, Inter.Cubic);
                    if (capturesTaken < capturesToBeTaken && ModeSelector.IsChecked == true)
                    {
                        trainingControl.AddPictureToCollection(graycopy);
                        capturesTaken++;
                    }
                    //draw rectangle on detected face
                    actualImage.Draw(face, new Bgr(255, 0, 0), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

                    string personName = "UNKNOWN";
                    if (imagesFound)
                    {
                        if (!isTraining)
                        {
                            FaceRecognizer.PredictionResult result = faceRecognizer.Predict(graycopy);
                            personName = new SqlManager().SQL_GetPersonName(result.Label.ToString());
                        }
                        else
                        {
                            personName = "In Training";
                        }
                    }
                    //display name over detected face
                    CvInvoke.PutText(actualImage, personName, new System.Drawing.Point(face.X - 2, face.Y - 2), FontFace.HersheyComplex, 1, new Bgr(0, 255, 0).MCvScalar);
                }
            }
            imageDisplay.Image = actualImage;
            // ImgViewer.Source = ConvertToImageSource(actualImage.ToBitmap());
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

            try
            {
                ModeSelector.Content = "Switch to Predict Mode";
                if (predictControl != null)
                    CustomControlContainer.Children.Remove(predictControl);
                SwitchToTrainingMode();
            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString()); }
        }



        private void SwitchToPredictMode()
        {
            CustomControlContainer.Children.Add(predictControl);

        }

        private void SwitchToTrainingMode()
        {

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
                LoadImages(System.AppDomain.CurrentDomain.BaseDirectory);

            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString()); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (predictControl.serialPort != null && predictControl.serialPort.IsOpen)
            {
                predictControl.serialPort.Close();
            }
            if (WebCam != null)
                WebCam.Stop();
        }

        private void WriteToConsole(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ConsoleOutput.Text += DateTime.Now.ToString() + " : ";
                ConsoleOutput.Text += message + "\n";
            });
        }

        private void hardwareSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (hardwareSelector.SelectedIndex == 0)
            {
                isGpuEnabled = false;
            }
            else
            {
                isGpuEnabled = true;
            }
        }

        private void ParameterChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                TextBox textBox = (TextBox)sender;
                if (textBox.Name == "ScaleFactorValue")
                {
                    scaleFactor = Convert.ToDouble(textBox.Text);
                }
                else
                    if (textBox.Name == "MinNeigboursValue")
                {
                    minNeigbours = Convert.ToInt32(textBox.Text);
                }
            }
            catch (Exception) { }
        }

    }
}

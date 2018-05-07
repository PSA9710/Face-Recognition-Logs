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
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
        public static int capturesToBeTaken = 20;
        public static String pathToSavePictures;

        bool imagesFound = false;
        Image<Gray, byte>[] trainingImages;
        int[] personID;
        EigenFaceRecognizer faceRecognizer = new EigenFaceRecognizer(90, 2500);


        int sizeToBeSaved = 100;//size of the picture wich will be saved

        //WebCameraControl WebCam;


        TrainingControl trainingControl = new TrainingControl();
        PredictControl predictControl;
        VideoCapture WebCam;

        bool isPersonInRange = false;
        bool detectFaces = true;
        bool isTraining = false;
        bool isGpuEnabled = false;
        bool isCudaEnabled = false;
        bool isRegistering = false;

        double scaleFactor;
        int minNeigbours;

        String appLocation;
        String cpuClassifierFileName;
        String cudaClassifierFileName;

        private CascadeClassifier cpuClassifier;
        CudaCascadeClassifier cudaClassifier;
        public MainWindow()
        {
            InitializeComponent();
            PopulateStreamOptions();


            predictControl = new PredictControl(ConsoleOutput,ConsoleScrollBar);
            predictControl.MessageRecieved += new EventHandler(MessageRecieved);
            trainingControl.writeToConsole += new EventHandler(trainingControlWriteToConsole);


            var location = System.AppDomain.CurrentDomain.BaseDirectory;
            appLocation = location;
            cpuClassifierFileName = location + "/haarcascade_frontalface_alt_CPU.xml";
            cudaClassifierFileName = location + "/haarcascade_frontalface_alt_GPU.xml";
            CreateDirectory(location, "data");
            CreateDirectory(location, "pictures");


            SwitchToPredictMode();
            pathToSavePictures = location + "/pictures";
            new SqlManager().SQL_CheckforDatabase();

            //loads model otherwise trains it
            if (!CheckForModel())
                LoadImages(location);


            CheckIfCudaIsEnabled();


            //test
            cpuClassifier = new CascadeClassifier(cpuClassifierFileName);
        }

        private void trainingControlWriteToConsole(object sender, EventArgs e)
        {
            String message = trainingControl.messageForConsole;
            if (message != null)
            {
                WriteToConsole(message);
                trainingControl.messageForConsole = null;
            }
        }


        //////private void tests(object sender,EventArgs arfs)
        //////{
        //////    if(WebCam!=null)
        //////    if (isCudaEnabled && isGpuEnabled)
        //////    {
        //////        Mat gm = new Mat();
        //////        gm = WebCam.QueryFrame();
        //////            if(gm!=null)
        //////        ProcessWithGPU(gm);
        //////    }
        //////    else
        //////    {
        //////        Mat m = new Mat();
        //////        m = WebCam.QueryFrame();
        //////            if(m!=null)
        //////        ProcessWithCPU(m);
        //////    }
        //////}

        private bool CheckForModel()
        {
            if (File.Exists(appLocation + "/data/faceRecognizerModel.cv"))
            {
                WriteToConsole("FaceRecognizer : Model found. Loaded and skiped training");
                faceRecognizer.Read(appLocation + "/data/faceRecognizerModel.cv");
                return true;
            }
            return false;
        }

        private void CheckIfCudaIsEnabled()
        {
            if (CudaInvoke.HasCuda)
            {
                isCudaEnabled = true;
                cudaClassifier = new CudaCascadeClassifier(cudaClassifierFileName);

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

        #region FACERECOGNIZER
        public void TrainFaceRecognizer()
        {
            Thread t = new Thread(() =>
            {
                Thread.Sleep(500);
                WriteToConsole("FaceRecognizer : Training...");

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
            isTraining = true;
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
        #endregion

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
            try
            {
                Mat m = new Mat();
                WebCam.Retrieve(m);
                if (m != null)
                    if (isCudaEnabled && isGpuEnabled)
                    {

                        ProcessWithGPU(m);
                    }
                    else
                    {

                        ProcessWithCPU(m);
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }


        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (StreamingOptions.SelectedIndex == -1)
            {
                MessageBox.Show("Please select streaming Device");
                return;
            }
            string option = StreamingOptions.SelectedItem.ToString();
            string url = "http://";
            // url += UsernameStream.Text + ":";
            // url += PasswordStream.Text + "@";
            url += IP1.Text + ".";
            url += IP2.Text + ".";
            url += IP3.Text + ".";
            url += IP4.Text;
            url += ":8080/video";
            Thread t = new Thread(() =>
              {
                  if (option == "VIA IP")
                  {
                      WebCam = new VideoCapture(url);
                      WriteToConsole("Camera : Connected to external camera");
                  }
                  else
                  {
                     // int id = Convert.ToInt32(StreamingOptions.SelectedItem);
                      WebCam = new VideoCapture(Convert.ToInt32(option));
                      WriteToConsole("Camera : Connected to internal camera");
                  }
                  WebCam.ImageGrabbed += WebCam_ImageGrabbed;
                  //WebCam.SetCaptureProperty(CapProp.Buffersuze, 3);
                  WebCam.Start();
              });
            t.Start();
            startCameraFeed.IsEnabled = false;
            stopCameraFeed.IsEnabled = true;
            imageDisplayBorder.Visibility = Visibility.Visible;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (WebCam != null)
            {
                WriteToConsole("Camera : Camera stopped");
                WebCam.ImageGrabbed -= WebCam_ImageGrabbed;
                WebCam.Stop();
                WebCam.Dispose();
            }
            startCameraFeed.IsEnabled = true;
            stopCameraFeed.IsEnabled = false;
        }



        #region GPU PROCESSING

        private void ProcessWithGPU(Mat bmp)
        {
            using (Image<Bgr, byte> capturedImage = bmp.ToImage<Bgr, byte>())
            {
                using (CudaImage<Bgr, byte> cudaCapturedImage = new CudaImage<Bgr, byte>(bmp))
                {
                    using (CudaImage<Gray, byte> cudaGrayImage = cudaCapturedImage.Convert<Gray, byte>())
                    {
                        Rectangle[] faces = FindFacesUsingGPU(cudaGrayImage);
                        foreach (Rectangle face in faces)
                        {
                            using (var graycopy = capturedImage.Convert<Gray, byte>().Copy(face).Resize(sizeToBeSaved, sizeToBeSaved, Inter.Cubic))
                            {
                                capturedImage.Draw(face, new Bgr(255, 0, 0), 3);  //draw a rectangle around the detected face
                                if (isRegistering)
                                {
                                    Dispatcher.Invoke(() => { AddPicturesToCollection(graycopy); });
                                }
                                else
                                {
                                    var personName = PredictFace(graycopy);
                                    //place name of the person on the image
                                    CvInvoke.PutText(capturedImage, personName, new System.Drawing.Point(face.X - 2, face.Y - 2), FontFace.HersheyComplex, 1, new Bgr(0, 255, 0).MCvScalar);
                                }
                            }
                            //imageDisplay.Image = capturedImage;

                        }
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    imageDisplay.Source = ConvertToImageSource(capturedImage.ToBitmap());
                });
            }
        }

        private Rectangle[] FindFacesUsingGPU(CudaImage<Gray, byte> cudaCapturedImage)
        {
            //using (CudaCascadeClassifier face = new CudaCascadeClassifier(cudaClassifierFileName))
            using (GpuMat faceRegionMat = new GpuMat())
            {
                cudaClassifier.ScaleFactor = scaleFactor;
                cudaClassifier.MinNeighbors = minNeigbours;
                cudaClassifier.DetectMultiScale(cudaCapturedImage, faceRegionMat);
                Rectangle[] faceRegion = cudaClassifier.Convert(faceRegionMat);
                return faceRegion;
            }
        }

        #endregion
        #region CPU PROCESSING
        private void ProcessWithCPU(Mat bmp)
        {
            using (Image<Bgr, byte> capturedImage = new Image<Bgr, byte>(bmp.Bitmap))
            {
                using (Image<Gray, byte> grayCapturedImage = capturedImage.Convert<Gray, byte>())
                {
                    Rectangle[] faces = FindFacesUsingCPU(grayCapturedImage);
                    foreach (Rectangle face in faces)
                    {
                        using (var graycopy = grayCapturedImage.Copy(face).Resize(sizeToBeSaved, sizeToBeSaved, Inter.Cubic))
                        {
                            capturedImage.Draw(face, new Bgr(255, 0, 0), 3);  //draw a rectangle around the detected face
                            if (isRegistering)
                            {
                                Dispatcher.Invoke(() => { AddPicturesToCollection(graycopy); });
                            }
                            else
                            {
                                var personName = PredictFace(graycopy);
                                //place name of the person on the image
                                CvInvoke.PutText(capturedImage, personName, new System.Drawing.Point(face.X - 2, face.Y - 2), FontFace.HersheyComplex, 1, new Bgr(0, 255, 0).MCvScalar);
                            }
                        }
                    }
                    //imageDisplay.Image = capturedImage;
                    Dispatcher.Invoke(() =>
                        { imageDisplay.Source = ConvertToImageSource(capturedImage.ToBitmap()); });
                }
            }
        }


        private Rectangle[] FindFacesUsingCPU(Image<Gray, byte> grayCapturedImage)
        {
            // using (CascadeClassifier face = new CascadeClassifier(cpuClassifierFileName))
            {
                var faces = cpuClassifier.DetectMultiScale(grayCapturedImage, scaleFactor, minNeigbours);
                return faces;
            }
        }
        #endregion

        private void AddPicturesToCollection(Image<Gray, byte> graycopy)
        {
            if (trainingControl.isWaitingForImage)
                if (capturesTaken < capturesToBeTaken)
                {
                    trainingControl.AddPictureToCollection(graycopy);
                }
        }

        private String PredictFace(Image<Gray, byte> image)
        {
            String personName;
            if (!isTraining)
            {
                var result = faceRecognizer.Predict(image);
                personName = new SqlManager().SQL_GetPersonName(result.Label.ToString());
            }
            else
            {
                personName = "IN-TRAINING";
            }
            return personName;
        }


        //private void ProcessWithCPU(Mat bmp)
        //{
        //    Image<Bgr, byte> actualImage = new Image<Bgr, byte>(bmp.Bitmap);
        //    if (actualImage != null && detectFaces)
        //    {
        //        Image<Gray, byte> grayImage = actualImage.Convert<Gray, byte>();
        //        double scaleFactor = Convert.ToDouble(ScaleFactorValue.Text);
        //        int minNeigbours = Convert.ToInt32(MinNeigboursValue.Text);
        //        var faces = cascadeClassifier.DetectMultiScale(grayImage, scaleFactor, minNeigbours); //the actual face detection happens here
        //        foreach (var face in faces)
        //        {
        //            //get just the detected area(face)
        //            var graycopy = actualImage.Copy(face).Convert<Gray, byte>().Resize(sizeToBeSaved, sizeToBeSaved, Inter.Cubic);
        //            if (capturesTaken < capturesToBeTaken && ModeSelector.IsChecked == true)
        //            {
        //                trainingControl.AddPictureToCollection(graycopy);
        //                capturesTaken++;
        //            }
        //            //draw rectangle on detected face
        //            actualImage.Draw(face, new Bgr(255, 0, 0), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

        //            string personName = "UNKNOWN";
        //            if (imagesFound)
        //            {
        //                if (!isTraining)
        //                {
        //                    FaceRecognizer.PredictionResult result = faceRecognizer.Predict(graycopy);
        //                    personName = new SqlManager().SQL_GetPersonName(result.Label.ToString());
        //                }
        //                else
        //                {
        //                    personName = "In Training";
        //                }
        //            }
        //            //display name over detected face
        //            CvInvoke.PutText(actualImage, personName, new System.Drawing.Point(face.X - 2, face.Y - 2), FontFace.HersheyComplex, 1, new Bgr(0, 255, 0).MCvScalar);
        //        }
        //    }
        //    imageDisplay.Image = actualImage;
        //    // ImgViewer.Source = ConvertToImageSource(actualImage.ToBitmap());
        //}

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
            isRegistering = false;

        }

        private void SwitchToTrainingMode()
        {

            CustomControlContainer.Children.Add(trainingControl);
            isRegistering = true;

        }

        private void ModelSelector_Unchecked(object sender, RoutedEventArgs e)
        {

            try
            {
                ModeSelector.Content = "Switch to Training Mode!";
                if (trainingControl != null)
                    CustomControlContainer.Children.Remove(trainingControl);
                SwitchToPredictMode();
                if (trainingControl.hasSaved)
                {
                    LoadImages(System.AppDomain.CurrentDomain.BaseDirectory);
                }
            }
            catch (Exception ex)
            { MessageBox.Show(ex.ToString()); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isTraining)
            {
                MessageBoxResult result = MessageBox.Show("Face recognition in training. Do you want to quit?", "Possible data loss", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            if (capturesTaken > 0)
            {
                MessageBoxResult result = MessageBox.Show("You have unsaved pictures. Do you want to quit?", "Possible data loss", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            if (predictControl.serialPort != null && predictControl.serialPort.IsOpen)
            {
                predictControl.DisconnectFromBluetooth();
            }
            if (WebCam != null)
            {
                WebCam.Stop();
                WebCam.Dispose();
            }
            if (faceRecognizer != null && !isTraining)
            {
                WriteToConsole("Saving Model");
                faceRecognizer.Write(appLocation + "/data/faceRecognizerModel.cv");
            }
            Environment.Exit(0);
        }

        private void WriteToConsole(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ConsoleOutput.Text += DateTime.Now.ToString() + " @ ";
                ConsoleOutput.Text += message + "\n";
                ConsoleScrollBar.ScrollToBottom();
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

        private void StreamingOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StreamingOptions.SelectedIndex == 0)
            {
                webcameraCredentials.IsEnabled = false;
            }
            else if (StreamingOptions.SelectedIndex == 1)
            {
                webcameraCredentials.IsEnabled = true;
            }
            if (!stopCameraFeed.IsEnabled)
            {
                startCameraFeed.IsEnabled = true;
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            WindowAspectRatio.Register((Window)sender);
        }

        internal class WindowAspectRatio
        {
            private double _ratio;

            private WindowAspectRatio(Window window)
            {
                _ratio = window.Width / window.Height;
                ((HwndSource)HwndSource.FromVisual(window)).AddHook(DragHook);
            }

            public static void Register(Window window)
            {
                new WindowAspectRatio(window);
            }

            internal enum WM
            {
                WINDOWPOSCHANGING = 0x0046,
            }

            [Flags()]
            public enum SWP
            {
                NoMove = 0x2,
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct WINDOWPOS
            {
                public IntPtr hwnd;
                public IntPtr hwndInsertAfter;
                public int x;
                public int y;
                public int cx;
                public int cy;
                public int flags;
            }

            private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handeled)
            {
                if ((WM)msg == WM.WINDOWPOSCHANGING)
                {
                    WINDOWPOS position = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                    if ((position.flags & (int)SWP.NoMove) != 0 ||
                        HwndSource.FromHwnd(hwnd).RootVisual == null) return IntPtr.Zero;

                    position.cx = (int)(position.cy * _ratio);

                    Marshal.StructureToPtr(position, lParam, true);
                    handeled = true;
                }

                return IntPtr.Zero;
            }
        }

    }
}

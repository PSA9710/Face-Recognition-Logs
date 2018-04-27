using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
using Emgu.CV.Structure;
using Microsoft.Win32;

namespace Pontor
{
    /// <summary>
    /// Interaction logic for TrainingControl.xaml
    /// </summary>
    public partial class TrainingControl : UserControl
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        List<Image<Gray,byte>> images = new List<Image<Gray,byte >>();


        public TrainingControl()
        {
            InitializeComponent();
        }


        public void AddPictureToCollection(Image<Gray,byte> image )
        {
            images.Add(image);
            ImageSource img = ConvertToImageSource(image.Bitmap);
            CapturesDisplay.Children.Add(new System.Windows.Controls.Image() { Source=img,Width=50,Height=50});
        }

        private ImageSource ConvertToImageSource(Bitmap bmp)
        {

            IntPtr hBitmap = bmp.GetHbitmap();
            ImageSource wpfBitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            bmp.Dispose();
            DeleteObject(hBitmap);
            return wpfBitmap;

        }

        private void RetakeDataSet_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.capturesTaken = 0; 
            CapturesDisplay.Children.Clear();
        }

        private void SaveDataSet_Click(object sender, RoutedEventArgs e)
        {
            int id = 1;
            int piccount = 0;
            var location = MainWindow.pathToSavePictures+"/";
            try
            {
                
                foreach (object image in images)
                {
                    SaveImage(image,id,piccount);
                    piccount++;
                    
                }
            }
            catch(Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void SaveImage(object image,int id, int piccount)
        {
            Image<Gray, byte> img = image as Image<Gray,byte>;
            //test.Source = ConvertToImageSource(img.Bitmap);
            Bitmap bmp = img.Bitmap;
            String filePath = "pictures/" + id.ToString();
            filePath += "_" + piccount.ToString() + ".bmp";
            bmp.Save(filePath);
            bmp.Dispose();
        }



    }
}

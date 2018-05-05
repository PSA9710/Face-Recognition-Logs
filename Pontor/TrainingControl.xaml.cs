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

        List<Image<Gray, byte>> images = new List<Image<Gray, byte>>();

        

        public TrainingControl()
        {
            InitializeComponent();
        }


        public void AddPictureToCollection(Image<Gray, byte> image)
        {
            Image<Gray, byte> img1 = new Image<Gray, byte>(image.ToBitmap());
            images.Add(img1);
            ImageSource img = ConvertToImageSource(image.Bitmap);
            CapturesDisplay.Children.Add(new System.Windows.Controls.Image() { Source = img, Width = 50, Height = 50 });
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
            images.Clear();
        }

        private void SaveDataSet_Click(object sender, RoutedEventArgs e)
        {
            String firstName = FirstNameTextBox.Text;
            String lastName = LastNameTextBox.Text;
            String CNP = CNPTextBox.Text;
            if (images.Count != MainWindow.capturesToBeTaken)
            {
                MessageBox.Show("Witchcraft!!! There should be " + MainWindow.capturesToBeTaken.ToString() + "" +
                    " pictures taken","WIZZARD DETECTED",MessageBoxButton.OK,MessageBoxImage.Warning);
                MessageBox.Show(images.Count.ToString());
                return;
            }
            if (SaveInDatabase(firstName, lastName, CNP))
            {

                int id = new SqlManager().SQL_GetPersonId(CNP);
                if (id == -1)
                {
                    MessageBox.Show("IMPOSIBLE! The ID is negative! Bring holy water!");
                    return;
                }
                int piccount = 0;
                var location = MainWindow.pathToSavePictures + "/";
                try
                {

                    foreach (var image in images)
                    {
                        SaveImage(image, id, piccount);
                        piccount++;

                    }
                    ResetAllFields();
                    images.Clear();
                    
                    MessageBox.Show("Save succesful");

                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
            }
        }

        private void ResetAllFields()
        {
            FirstNameTextBox.Text = "";
            LastNameTextBox.Text = "";
            CNPTextBox.Text = "";
            CapturesDisplay.Children.Clear();
        }

        private bool SaveInDatabase(string firstName, string lastName, string CNP)
        {
            if (CheckForEmptyFields(firstName, lastName, CNP))
            {
                try
                {
                    new SqlManager().SQL_InsertIntoPersons(firstName, lastName, CNP);
                    return true;
                }
                catch(IndexOutOfRangeException e)
                {
                    MessageBox.Show("There is 1 Person with same CNP. Please check your information or contact database administrator",
                     "CNP IN USE", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return false;
        }

        private bool CheckForEmptyFields(string firstName, string lastName, string CNP)
        {
            if (String.IsNullOrEmpty(firstName))
            {
                MessageBox.Show("Field First Name can not be empty");
                FirstNameTextBox.Focus();
                return false;
            }
            else if (String.IsNullOrEmpty(lastName))
            {
                MessageBox.Show("Field First Name can not be empty");
                LastNameTextBox.Focus();
                return false;
            }
            else if (String.IsNullOrEmpty(CNP))
            {
                MessageBox.Show("Field CNP can not be empty");
                CNPTextBox.Focus();
                return false;
            }
            return true;
        }

        private void SaveImage(Image<Gray,byte> image, int id, int piccount)
        {
            Bitmap bmp = image.ToBitmap();
            String filePath = "pictures/" + id.ToString();
            filePath += "_" + piccount.ToString() + ".bmp";
            bmp.Save(filePath);
            bmp.Dispose();
        }



    }
}

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
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;


namespace Pontor
{
    /// <summary>
    /// Interaction logic for PredictControl.xaml
    /// </summary>
    public partial class PredictControl : UserControl
    {

        List<Image<Gray, byte>> images = new List<Image<Gray, byte>>();

        public PredictControl()
        {
            InitializeComponent();
            this.images = images;
        }

        public string Predict()
        {
            
            return null;
        }
    }
}

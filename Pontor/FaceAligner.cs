using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Pontor
{
    public static class FaceAligner
    {
        static String location = AppDomain.CurrentDomain.BaseDirectory;
        private static CascadeClassifier cpuEyeClassifier = new CascadeClassifier(location + "/haarcascade_eye_tree_eyeglasses_CPU.xml");

        public static Rectangle[] AlignFace(Image<Gray, byte> imageToAlign, out Image<Gray, byte> allignedImage)
        {
            Rectangle[] eyes;
            int height = (int)(imageToAlign.Height / 1.5);
            using (Image<Gray, byte> upperFace = imageToAlign.GetSubRect(new Rectangle(0, 0, imageToAlign.Width, height)))
            //using (Image<Gray, byte> upperFace = imageToAlign.Clone())
            {
                eyes = cpuEyeClassifier.DetectMultiScale(upperFace, 1.1, 10, minSize: Size.Empty,maxSize:new Size(50,50));
            }
            allignedImage = imageToAlign;
            return eyes;
        }

    }
}

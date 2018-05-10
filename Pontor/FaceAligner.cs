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

        public static Rectangle[] AlignFace(Image<Gray, byte> imageToAlign, out double degreesToRotateImage)
        {
            Rectangle[] eyes;
            int height = (int)(imageToAlign.Height / 1.5);
            using (Image<Gray, byte> upperFace = imageToAlign.GetSubRect(new Rectangle(0, 0, imageToAlign.Width, height)))
            //using (Image<Gray, byte> upperFace = imageToAlign.Clone())
            {
                eyes = cpuEyeClassifier.DetectMultiScale(upperFace, 1.1, 10, minSize: Size.Empty, maxSize: new Size(50, 50));
            }

            degreesToRotateImage = GetDegrees(eyes);
            return eyes;
        }

        private static double GetDegrees( Rectangle[] eyes)
        {
            if (eyes.Count() != 2) return 0;
            Rectangle lEye, rEye;
            if(eyes[0].X > eyes[1].X)
            {
                rEye = eyes[0];
                lEye = eyes[1];
            }
            else
            {
                lEye = eyes[0];
                rEye = eyes[1];
            }
            double deltaY = (lEye.Y + lEye.Height / 2) - (rEye.Y + rEye.Height / 2);
            double deltaX = (lEye.X + lEye.Width / 2) - (rEye.X + rEye.Width / 2);
            var degrees = Math.Atan2(deltaY, deltaX) * 180 / Math.PI;
            degrees = 180 - degrees;
            return degrees;
        }
    }
}

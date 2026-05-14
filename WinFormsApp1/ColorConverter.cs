using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;

public class ColorConverter
{
        ////////////////////////////////////////////////////////////////////////////
        public static void RgbToCmyk(string path)
        {
            Bitmap rgbImage = new Bitmap(path);

            for (int i = 0; i < rgbImage.Height; i++)
            {
                for (int j = 0; j < rgbImage.Width; j++)
                {
                    Color pixelColor = rgbImage.GetPixel(j, i);
                    int c = 255 - pixelColor.R;
                    int m = 255 - pixelColor.G;
                    int y = 255 - pixelColor.B;

                    Color cmyColor = Color.FromArgb(c, m, y);
                    rgbImage.SetPixel(j, i, cmyColor);
                }
            }
            Console.WriteLine("image has been converted to cmyk");
            rgbImage.Save("C:\\Users\\Rama Alwanni\\Desktop\\cmyk_image.jpg");
        }
        //////////////////////////////////////////////////////////////////////////
        public static void RgbToHsv(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat hsvImage = new Mat();
            CvInvoke.CvtColor(rgbImage, hsvImage, ColorConversion.Bgr2Hsv);

            Console.WriteLine("image has been converted to HSV");
            hsvImage.Save("C:\\Users\\Rama Alwanni\\Desktop\\Hsv_image.jpg");
        }

        //RGB --> YCbCr
        public static Mat ConvertToYcbcr(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat ycbcrImage = new Mat();
            CvInvoke.CvtColor(rgbImage, ycbcrImage, ColorConversion.Bgr2YCrCb);
            //For test 
           ycbcrImage.Save("C:\\Users\\dell\\Documents\\4th\\ForStudy\\Lectures\\2S\\MultiMedia\\Practical\\Lec\\IT-Ycbcr.jpg");

           Console.WriteLine("image has been converted to Ycbcr");

           return ycbcrImage;
        }
        //RGB --> YUV 
        public static Mat ConvertToYUV(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat yuvImage = new Mat();
            CvInvoke.CvtColor(rgbImage, yuvImage,ColorConversion.Bgr2Yuv);
            return yuvImage;
        }

        //RGB --> L*a*b
        public static Mat ConvertToLAB(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat labImage = new Mat();
            CvInvoke.CvtColor(rgbImage, labImage,ColorConversion.Bgr2Lab);
            return labImage;
        }

    }


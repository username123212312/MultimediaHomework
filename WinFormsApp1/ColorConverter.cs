using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;

public static class ColorConverter
{
        ////////////////////////////////////////////////////////////////////////////
        public static Bitmap RgbToCmyk(string path)
        {
            Bitmap rgbImage = new Bitmap(path);

            for (int x = 0; x < rgbImage.Width; x++)
            {
                for (int y = 0; y < rgbImage.Height; y++)
                {
                    Color c = rgbImage.GetPixel(x, y);

                    double r = c.R / 255.0;
                    double g = c.G / 255.0;
                    double b = c.B / 255.0;

                    double k = 1 - Math.Max(r, Math.Max(g, b));

                    int cyan = 0, magenta = 0, yellow = 0;

                    if (k < 1)
                    {
                        cyan = (int)(((1 - r - k) / (1 - k)) * 255);
                        magenta = (int)(((1 - g - k) / (1 - k)) * 255);
                        yellow = (int)(((1 - b - k) / (1 - k)) * 255);
                    }
                    rgbImage.SetPixel(x, y, Color.FromArgb(cyan, magenta, yellow));
                }
            }
            Console.WriteLine("image has been converted to cmyk");
            rgbImage.Save("C:\\Users\\Rama Alwanni\\Desktop\\cmyk_image.jpg");
            return rgbImage;
        }

        //////////////////////////////////////////////////////////////////////////
        public static Mat RgbToHsv(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat hsvImage = new Mat();
            CvInvoke.CvtColor(rgbImage, hsvImage, ColorConversion.Bgr2Hsv);

            Console.WriteLine("image has been converted to HSV");
            hsvImage.Save("C:\\Users\\Rama Alwanni\\Desktop\\Hsv_image.jpg");

            return hsvImage;
        }

        //RGB --> YCbCr
        public static Mat ConvertToYcbcr(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat ycbcrImage = new Mat();
            CvInvoke.CvtColor(rgbImage, ycbcrImage, ColorConversion.Bgr2YCrCb);
            //For test 
           //ycbcrImage.Save("C:\\Users\\dell\\Documents\\4th\\ForStudy\\Lectures\\2S\\MultiMedia\\Practical\\Lec\\IT-Ycbcr.jpg");

           Console.WriteLine("image has been converted to Ycbcr");

        ycbcrImage.Save("C:\\Users\\Rama Alwanni\\Desktop\\ycbcrImage.jpg");


        return ycbcrImage;
        }
        //RGB --> YUV 
        public static Mat ConvertToYUV(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat yuvImage = new Mat();
            CvInvoke.CvtColor(rgbImage, yuvImage,ColorConversion.Bgr2Yuv);
        yuvImage.Save("C:\\Users\\Rama Alwanni\\Desktop\\yuvImage.jpg");

        return yuvImage;
        }

        //RGB --> L*a*b
        public static Mat ConvertToLAB(string path)
        {
            Mat rgbImage = CvInvoke.Imread(path);

            Mat labImage = new Mat();
            CvInvoke.CvtColor(rgbImage, labImage,ColorConversion.Bgr2Lab);
        labImage.Save("C:\\Users\\Rama Alwanni\\Desktop\\labImage.jpg");

        return labImage;
        }

}


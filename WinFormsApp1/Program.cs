using System;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WinFormsApp1
{
    internal static class Program
    {
        ////////////////////////////////////////////////////////////////////////////
        public static void RgbToCmyk(string path)
        {
             Bitmap rgbImage = new Bitmap(path);

            for (int i = 0; i<rgbImage.Height; i++)
            {
                    for (int j = 0; j<rgbImage.Width; j++)
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
        /////////////////////////////////////////////////////////////////////////

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //************************************************
            //Enter any path image for convert it 
            RgbToCmyk("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            RgbToHsv("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            //************************************************

            Application.Run(new PixelLabMainForm());

            




        }


    }
}
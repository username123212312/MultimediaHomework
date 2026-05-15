using System;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WinFormsApp1
{
    internal static class Program
    {

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //************************************************
            //Enter any path image for convert it 
            ColorConverter.RgbToCmyk("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            ColorConverter.RgbToHsv("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            ColorConverter.ConvertToYcbcr("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            ColorConverter.ConvertToYUV("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            ColorConverter.ConvertToLAB("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");

            //************************************************

            Application.Run(new PixelLabMainForm());

            




        }


    }
}

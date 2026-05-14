using System;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WinFormsApp1
{
    internal static class Program
    {
           /////////////////////////////////////////////////////////////////////////

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //************************************************
            //Enter any path image for convert it 
            //RgbToCmyk("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            //RgbToHsv("C:\\Users\\Rama Alwanni\\Desktop\\image.jpg");
            //************************************************
            ConvertToYcbcr("C:\\Users\\dell\\Documents\\4th\\ForStudy\\Lectures\\2S\\MultiMedia\\Practical\\Lec\\IT.jpg");

            Application.Run(new PixelLabMainForm());

            




        }


    }
}
using System;
using System.Windows.Forms;
using WinFormsApp1.Lectures;

namespace WinFormsApp1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            string imagePath = "C:\\Users\\Yousef Razzouk\\Desktop\\pic.png";
            string outputPath = "C:\\Users\\Yousef Razzouk\\Desktop\\pic";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            //var lectureForm = Lecture2.CreateForm(imagePath, outputPath);
            Lecture4.run();

            //Application.Run(lectureForm);
        }
    }
}
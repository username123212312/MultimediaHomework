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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Lecture6.run();
            Application.Run(new PixelLabMainForm());
            //Application.Run(new Lecture6Form());
        }
    }
}

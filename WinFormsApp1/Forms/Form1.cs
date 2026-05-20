//using System;
//using System.Drawing;
//using System.Windows.Forms;

//namespace WinFormsApp1
//{
//    public partial class Form1 : Form
//    {
//        // =================================================================
//        // 1. Shared variables and data structures for mouse and color tracking
//        // =================================================================
//        private bool isRotating = false;
//        private Point lastMousePosition;
//        private float rotateX = 30f;
//        private float rotateY = -45f;
//        private float zoomScale = 1.0f;

//        // Structure to store HSV color components
//        public struct HSVColor
//        {
//            public double H; // Hue (0 - 360 degrees)
//            public double S; // Saturation (0 - 100%)
//            public double V; // Value/Brightness (0 - 100%)
//        }

//        // Structure to store CMYK color components
//        public struct CMYKColor
//        {
//            public double C; // Cyan %
//            public double M; // Magenta %
//            public double Y; // Yellow %
//            public double K; // Key/Black %
//        }

//        // Form Constructor
//        //public Form1()
//        //{
//        //    InitializeComponent();
//        //}

//        // =================================================================
//        // 2. Mouse and UI Control Events
//        // =================================================================

//        // Triggers when the user clicks inside the color space visualizer
//        private void colorSpaceVisualizer1_MouseClick(object sender, MouseEventArgs e)
//        {
//            // 1. Thread-safety Check: Ensure execution happens on the main UI thread
//            if (this.InvokeRequired)
//            {
//                this.Invoke(new Action(() => colorSpaceVisualizer1_MouseClick(sender, e)));
//                return;
//            }

//            // 2. Execute only on Left Mouse Button click
//            if (e.Button == MouseButtons.Left)
//            {
//                try
//                {
//                    Point mousePos = e.Location;

//                    // Create a 1x1 bitmap to capture the exact pixel color under the mouse safely
//                    using (Bitmap bmp = new Bitmap(1, 1))
//                    {
//                        using (Graphics g = Graphics.FromImage(bmp))
//                        {
//                            // Convert control relative coordinates to screen coordinates for precision
//                            Point screenPos = ColorSpaceVisualizer.PointToScreen(mousePos);
//                            g.CopyFromScreen(screenPos.X, screenPos.Y, 0, 0, new Size(1, 1));
//                        }

//                        // Extract the ARGB color of the captured pixel
//                        Color pickedColor = bmp.GetPixel(0, 0);

//                        // Send the RGB values to the conversion and synchronization method
//                        UpdateAndSyncColorOutputs(pickedColor.R, pickedColor.G, pickedColor.B);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    // Print error to the Output window instead of crashing the application
//                    System.Diagnostics.Debug.WriteLine($"[Color Picker Error]: {ex.Message}");
//                }
//            }
//        }

//        // Triggers when the color space visualizer control is resized (Fixes overlapping layout)
//        private void colorSpaceVisualizer1_Resize(object sender, EventArgs e)
//        {
//            int width = colorSpaceVisualizer1.Width;
//            int height = colorSpaceVisualizer1.Height;

//            if (height == 0) height = 1; // Prevent division-by-zero error

//            double aspectRatio = (double)width / height;

//            // Pass the updated aspect ratio to your 3D engine if necessary
//            colorSpaceVisualizer1.Invalidate(); // Force the control to redraw itself immediately
//        }


//        // =================================================================
//        // 3. Mathematical Algorithms for Color Transformations & UI Sync
//        // =================================================================

//        // Algorithm to convert RGB space values into HSV space
//        private HSVColor ConvertRGBtoHSV(int r, int g, int b)
//        {
//            HSVColor hsv = new HSVColor();
//            double rNorm = r / 255.0;
//            double gNorm = g / 255.0;
//            double bNorm = b / 255.0;

//            double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
//            double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
//            double delta = max - min;

//            // Calculate Value (Brightness)
//            hsv.V = Math.Round(max * 100);

//            // Calculate Saturation
//            if (max == 0) hsv.S = 0;
//            else hsv.S = Math.Round((delta / max) * 100);

//            // Calculate Hue angle in degrees
//            if (delta == 0)
//            {
//                hsv.H = 0;
//            }
//            else
//            {
//                if (max == rNorm) hsv.H = 60 * (((gNorm - bNorm) / delta) % 6);
//                else if (max == gNorm) hsv.H = 60 * (((bNorm - rNorm) / delta) + 2);
//                else if (max == bNorm) hsv.H = 60 * (((rNorm - gNorm) / delta) + 4);

//                if (hsv.H < 0) hsv.H += 360;
//            }
//            hsv.H = Math.Round(hsv.H);
//            return hsv;
//        }

//        // Algorithm to convert RGB space values into CMYK space
//        private CMYKColor ConvertRGBtoCMYK(int r, int g, int b)
//        {
//            CMYKColor cmyk = new CMYKColor();
//            double rNorm = r / 255.0;
//            double gNorm = g / 255.0;
//            double bNorm = b / 255.0;

//            double k = 1.0 - Math.Max(rNorm, Math.Max(gNorm, bNorm));

//            if (k == 1.0) // Pure black exception
//            {
//                cmyk.C = 0; cmyk.M = 0; cmyk.Y = 0; cmyk.K = 100;
//                return cmyk;
//            }

//            cmyk.C = Math.Round(((1.0 - rNorm - k) / (1.0 - k)) * 100);
//            cmyk.M = Math.Round(((1.0 - gNorm - k) / (1.0 - k)) * 100);
//            cmyk.Y = Math.Round(((1.0 - bNorm - k) / (1.0 - k)) * 100);
//            cmyk.K = Math.Round(k * 100);

//            return cmyk;
//        }

//        // Prints and synchronizes calculated values back onto the Form UI labels
//        // Prints and synchronizes calculated values back onto the Form UI labels safely
//        private void UpdateAndSyncColorOutputs(int r, int g, int b)
//        {
//            if (this.InvokeRequired)
//            {
//                this.Invoke(new Action(() => UpdateAndSyncColorOutputs(r, g, b)));
//                return;
//            }

//            // Mathematical conversions
//            HSVColor hsv = ConvertRGBtoHSV(r, g, b);

//            // Print values directly to the Visual Studio Output/Console window
//            System.Diagnostics.Debug.WriteLine($"\n>>> COLOR PICKED EVENT <<<");
//            System.Diagnostics.Debug.WriteLine($"RGB → ({r}, {g}, {b})");
//            System.Diagnostics.Debug.WriteLine($"HSV → ({hsv.H}°, {hsv.S}%, {hsv.V}%)");
//            System.Diagnostics.Debug.WriteLine($">>>>>>>>>><<<<<<<<<<<<<<<<\n");

//            // Print to UI Labels if they exist in this Form context
//            if (lblRGBResult != null) lblRGBResult.Text = $"RGB → ({r}, {g}, {b})";
//            if (lblHSVResult != null) lblHSVResult.Text = $"HSV → ({hsv.H}°, {hsv.S}%, {hsv.V}%)";
//        }
//    }
//}
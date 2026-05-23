using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class ColorSpaceVisualizer : UserControl
    {
        private double angleX = 30 * Math.PI / 180;
        private double angleY = -45 * Math.PI / 180;
        private float zoomScale = 1.0f;
        private Point lastMousePosition;
        private Bitmap currentBitmap;

        private string currentSystem = "RGB";
        public event EventHandler<Color>? ColorPicked;
        public ColorSpaceVisualizer()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(50, 50, 50);

            this.MouseDown += Visualizer_MouseDown;
            this.MouseMove += Visualizer_MouseMove;
            this.MouseWheel += Visualizer_MouseWheel;
        }

        public void SetImage(Bitmap bmp)
        {
            this.currentBitmap = bmp;
            this.Refresh();
        }

        public void SetColorSystem(string system)
        {
            this.currentSystem = system.ToUpper();
            this.Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (currentBitmap == null)
            {
                return;
            }

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            int centerX = this.Width / 2;
            int centerY = this.Height / 2;

            BitmapData bmpData = currentBitmap.LockBits(
                new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int bytes = Math.Abs(bmpData.Stride) * currentBitmap.Height;
            byte[] pixelBuffer = new byte[bytes];
            Marshal.Copy(bmpData.Scan0, pixelBuffer, 0, bytes);
            currentBitmap.UnlockBits(bmpData);

            int step = 2;

            for (int y = 0; y < currentBitmap.Height; y += step)
            {
                int rowOffset = y * bmpData.Stride;
                for (int x = 0; x < currentBitmap.Width; x += step)
                {
                    int byteIndex = rowOffset + x * 4;

                    byte b = pixelBuffer[byteIndex + 0];
                    byte gChan = pixelBuffer[byteIndex + 1];
                    byte r = pixelBuffer[byteIndex + 2];

                    double pointX, pointY, pointZ;

                    if (currentSystem == "HSV")
                    {
                        ColorToHsv(r, gChan, b, out double h, out double s, out double v);

                        double angle = h * Math.PI / 180.0;
                        double radius = s * 128.0; 
                        pointX = radius * Math.Cos(angle);
                        pointZ = radius * Math.Sin(angle);
                        pointY = (v * 255.0) - 128.0; 
                    }
                    else if (currentSystem == "YCbCr" || currentSystem == "YUV")
                    {
                        double Y = 0.299 * r + 0.587 * gChan + 0.114 * b;
                        double Cb = -0.1687 * r - 0.3313 * gChan + 0.5 * b + 128;
                        double Cr = 0.5 * r - 0.4187 * gChan - 0.0813 * b + 128;

                        pointX = Cb - 128;
                        pointY = Cr - 128;
                        pointZ = 0; 
                    }
                    else if (currentSystem == "CMYK")
                    {
                        double c = 1.0 - (r / 255.0);
                        double m = 1.0 - (gChan / 255.0);
                        double yColor = 1.0 - (b / 255.0);
                        double k = Math.Min(c, Math.Min(m, yColor));

                        pointX = ((c - k) * 255.0) - 128;
                        pointY = ((m - k) * 255.0) - 128;
                        pointZ = ((yColor - k) * 255.0) - 128;
                    }
                    else if (currentSystem == "LAB")
                    {
                        RgbToLab(r, gChan, b, out double l, out double a, out double bl);
                        pointX = a * 1.5;
                        pointY = (l - 50) * 1.5;
                        pointZ = bl * 1.5;
                    }
                    else 
                    {
                        pointX = r - 128;
                        pointY = gChan - 128;
                        pointZ = b - 128;
                    }

                    double rotatedY1 = pointY * Math.Cos(angleX) - pointZ * Math.Sin(angleX);
                    double rotatedZ1 = pointY * Math.Sin(angleX) + pointZ * Math.Cos(angleX);
                    double rotatedX2 = pointX * Math.Cos(angleY) + rotatedZ1 * Math.Sin(angleY);

                    float finalScreenX = (float)(centerX + (rotatedX2 * zoomScale));
                    float finalScreenY = (float)(centerY - (rotatedY1 * zoomScale));

                    float size = 3.0f + (float)(rotatedZ1 * 0.005);
                    if (size < 1.0f) size = 1.0f; 

                    using (var brush = new SolidBrush(Color.FromArgb(120, r, gChan, b)))
                    {
                        g.FillRectangle(brush, finalScreenX, finalScreenY, size, size);
                    }
                }
            }

            if (currentSystem == "HSV")
            {
                DrawCylinderOutline(g, centerX, centerY);
            }
            else if (currentSystem == "YCbCr" || currentSystem == "YUV")
            {
                Draw2DPlanesOutline(g, centerX, centerY); 
            }
            else if (currentSystem == "LAB")
            {

                DrawSphereOutline(g, centerX, centerY);
            }
            else
            {
                DrawCubeOutline(g, centerX, centerY);
            }
        }
        private void Draw2DPlanesOutline(Graphics g, int centerX, int centerY)
        {
            using (Pen pen = new Pen(Color.FromArgb(100, Color.Gray), 1.5f))
            {
                PointF[] points = new PointF[4]
                {
            ProjectPoint(-128, -128, 0, centerX, centerY),
            ProjectPoint(128, -128, 0, centerX, centerY),
            ProjectPoint(128, 128, 0, centerX, centerY),
            ProjectPoint(-128, 128, 0, centerX, centerY)
                };

                g.DrawPolygon(pen, points);

                PointF top = ProjectPoint(0, -128, 0, centerX, centerY);
                PointF bottom = ProjectPoint(0, 128, 0, centerX, centerY);
                PointF left = ProjectPoint(-128, 0, 0, centerX, centerY);
                PointF right = ProjectPoint(128, 0, 0, centerX, centerY);

                g.DrawLine(pen, top, bottom);
                g.DrawLine(pen, left, right);
            }
        }

        private PointF ProjectPoint(double x, double y, double z, int cx, int cy)
        {
            double rY1 = y * Math.Cos(angleX) - z * Math.Sin(angleX);
            double rZ1 = y * Math.Sin(angleX) + z * Math.Cos(angleX);
            double rX2 = x * Math.Cos(angleY) + rZ1 * Math.Sin(angleY);

            return new PointF(
                (float)(cx + (rX2 * zoomScale)),
                (float)(cy - (rY1 * zoomScale))
            );
        }
        private void DrawCubeOutline(Graphics g, int cx, int cy)
        {
            int[][] cubeEdges = new int[][] {
                new int[] {-128, -128, -128}, new int[] {127, -128, -128},
                new int[] {127, 127, -128},   new int[] {-128, 127, -128},
                new int[] {-128, -128, 127},  new int[] {127, -128, 127},
                new int[] {127, 127, 127},    new int[] {-128, 127, 127}
            };

            PointF[] projectedEdges = new PointF[8];

            for (int i = 0; i < 8; i++)
            {
                double x = cubeEdges[i][0]; double y = cubeEdges[i][1]; double z = cubeEdges[i][2];
                double y1 = y * Math.Cos(angleX) - z * Math.Sin(angleX);
                double z1 = y * Math.Sin(angleX) + z * Math.Cos(angleX);
                double x2 = x * Math.Cos(angleY) + z1 * Math.Sin(angleY);
                projectedEdges[i] = new PointF((float)(cx + (x2 * zoomScale)), (float)(cy - (y1 * zoomScale)));
            }

            using (Pen p = new Pen(Color.FromArgb(100, Color.White), 1))
            {
                int[][] lines = new int[][] {
                    new int[]{0,1}, new int[]{1,2}, new int[]{2,3}, new int[]{3,0},
                    new int[]{4,5}, new int[]{5,6}, new int[]{6,7}, new int[]{7,4},
                    new int[]{0,4}, new int[]{1,5}, new int[]{2,6}, new int[]{3,7}
                };
                foreach (var line in lines) g.DrawLine(p, projectedEdges[line[0]], projectedEdges[line[1]]);
            }
        }

        private void DrawCylinderOutline(Graphics g, int cx, int cy)
        {
            using (Pen p = new Pen(Color.FromArgb(100, Color.White), 1))
            {
                int segments = 16;
                PointF[] topPoints = new PointF[segments];
                PointF[] bottomPoints = new PointF[segments];

                for (int i = 0; i < segments; i++)
                {
                    double angle = (i * 2 * Math.PI) / segments;
                    double r = 128.0; 

                    double x = r * Math.Cos(angle);
                    double z = r * Math.Sin(angle);

                    double yTop = 127;
                    double yBottom = -128;

                    double y1 = yTop * Math.Cos(angleX) - z * Math.Sin(angleX);
                    double z1 = yTop * Math.Sin(angleX) + z * Math.Cos(angleX);
                    double x2 = x * Math.Cos(angleY) + z1 * Math.Sin(angleY);
                    topPoints[i] = new PointF((float)(cx + (x2 * zoomScale)), (float)(cy - (y1 * zoomScale)));

                    y1 = yBottom * Math.Cos(angleX) - z * Math.Sin(angleX);
                    z1 = yBottom * Math.Sin(angleX) + z * Math.Cos(angleX);
                    x2 = x * Math.Cos(angleY) + z1 * Math.Sin(angleY);
                    bottomPoints[i] = new PointF((float)(cx + (x2 * zoomScale)), (float)(cy - (y1 * zoomScale)));
                }

                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    g.DrawLine(p, topPoints[i], topPoints[next]);
                    g.DrawLine(p, bottomPoints[i], bottomPoints[next]);

                    if (i % 4 == 0) g.DrawLine(p, topPoints[i], bottomPoints[i]);
                }
            }
        }

        private void ColorToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
        {
            double min = Math.Min(r, Math.Min(g, b));
            double max = Math.Max(r, Math.Max(g, b));
            double delta = max - min;
            v = max / 255.0;
            s = (max == 0) ? 0 : delta / max;
            if (s == 0) h = 0;
            else
            {
                if (r == max) h = (g - b) / delta;
                else if (g == max) h = 2.0 + (b - r) / delta;
                else h = 4.0 + (r - g) / delta;
                h *= 60.0;
                if (h < 0) h += 360.0;
            }
        }

        private void Visualizer_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                lastMousePosition = e.Location;

                try
                {
                    Point screenPos = this.PointToScreen(e.Location);
                    using (Bitmap bmp = new Bitmap(1, 1))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(screenPos.X, screenPos.Y, 0, 0, new Size(1, 1));
                        }
                        Color pickedColor = bmp.GetPixel(0, 0);

                        ColorPicked?.Invoke(this, pickedColor);
                    }
                }
                catch { /* حماية لتفادي أي خطأ خارج نطاق الشاشة */ }
            }
        }

        private void Visualizer_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int deltaX = e.X - lastMousePosition.X;
                int deltaY = e.Y - lastMousePosition.Y;
                angleY += deltaX * 0.005;
                angleX += deltaY * 0.005;
                lastMousePosition = e.Location;
                this.Refresh();
            }
        }

        private void Visualizer_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0) zoomScale += 0.05f;
            else if (e.Delta < 0 && zoomScale > 0.1f) zoomScale -= 0.05f;
            this.Refresh();
        }

        private void RgbToLab(byte r, byte g, byte b, out double l, out double a, out double bl)
        {
            double var_R = (r / 255.0);
            double var_G = (g / 255.0);
            double var_B = (b / 255.0);

            var_R = (var_R > 0.04045) ? Math.Pow((var_R + 0.055) / 1.055, 2.4) : var_R / 12.92;
            var_G = (var_G > 0.04045) ? Math.Pow((var_G + 0.055) / 1.055, 2.4) : var_G / 12.92;
            var_B = (var_B > 0.04045) ? Math.Pow((var_B + 0.055) / 1.055, 2.4) : var_B / 12.92;

            double X = (var_R * 0.4124 + var_G * 0.3576 + var_B * 0.1805) * 100;
            double Y = (var_R * 0.2126 + var_G * 0.7152 + var_B * 0.0722) * 100;
            double Z = (var_R * 0.0193 + var_G * 0.1192 + var_B * 0.9505) * 100;

            double refX = 95.047; double refY = 100.000; double refZ = 108.883;
            double x = X / refX; double y = Y / refY; double z = Z / refZ;

            x = (x > 0.008856) ? Math.Pow(x, 1.0 / 3.0) : (7.787 * x) + (16.0 / 116.0);
            y = (y > 0.008856) ? Math.Pow(y, 1.0 / 3.0) : (7.787 * y) + (16.0 / 116.0);
            z = (z > 0.008856) ? Math.Pow(z, 1.0 / 3.0) : (7.787 * z) + (16.0 / 116.0);

            l = (116 * y) - 16;
            a = 500 * (x - y);
            bl = 200 * (y - z);
        }
        private void DrawSphereOutline(Graphics g, int cx, int cy)
        {
            using (Pen p = new Pen(Color.FromArgb(100, Color.White), 1))
            {
                int segments = 16;
                int rings = 8; 
                int radius = 128; 

                for (int j = -rings; j <= rings; j++)
                {
                    double latAngle = (j * Math.PI) / (rings * 2);
                    double currentY = radius * Math.Sin(latAngle);
                    double currentR = radius * Math.Cos(latAngle);

                    PointF[] points = new PointF[segments];
                    for (int i = 0; i < segments; i++)
                    {
                        double longAngle = (i * 2 * Math.PI) / segments;
                        double x = currentR * Math.Cos(longAngle);
                        double z = currentR * Math.Sin(longAngle);
                        points[i] = ProjectPoint(x, currentY, z, cx, cy);
                    }

                    for (int i = 0; i < segments; i++)
                        g.DrawLine(p, points[i], points[(i + 1) % segments]);
                }
            }
        }
    }
}
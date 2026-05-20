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
        // c202508
        // Constructor: Initializes the control, enables double buffering, and hooks up mouse events.
        public ColorSpaceVisualizer()
        {
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(25, 25, 25);

            this.MouseDown += Visualizer_MouseDown;
            this.MouseMove += Visualizer_MouseMove;
            this.MouseWheel += Visualizer_MouseWheel;
        }

        // Sets the bitmap image to be visualized and triggers a redraw of the control.
        public void SetImage(Bitmap bmp)
        {
            this.currentBitmap = bmp;
            this.Refresh();
        }

        // Handles the painting logic: processes image pixels using fast memory locking and draws the 3D color points.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (currentBitmap == null)
            {
                e.Graphics.DrawString("الرجاء تحميل صورة لعرض الفضاء اللوني ثلاثي الأبعاد", this.Font, Brushes.Gray, 10, 10);
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

            int step = 8;

            for (int y = 0; y < currentBitmap.Height; y += step)
            {
                int rowOffset = y * bmpData.Stride;
                for (int x = 0; x < currentBitmap.Width; x += step)
                {
                    int byteIndex = rowOffset + x * 4;

                    byte b = pixelBuffer[byteIndex + 0];
                    byte gChan = pixelBuffer[byteIndex + 1];
                    byte r = pixelBuffer[byteIndex + 2];

                    double pointX = r - 128;
                    double pointY = gChan - 128;
                    double pointZ = b - 128;

                    double rotatedY1 = pointY * Math.Cos(angleX) - pointZ * Math.Sin(angleX);
                    double rotatedZ1 = pointY * Math.Sin(angleX) + pointZ * Math.Cos(angleX);
                    double rotatedX2 = pointX * Math.Cos(angleY) + rotatedZ1 * Math.Sin(angleY);

                    float finalScreenX = (float)(centerX + (rotatedX2 * zoomScale));
                    float finalScreenY = (float)(centerY - (rotatedY1 * zoomScale));

                    using (var brush = new SolidBrush(Color.FromArgb(r, gChan, b)))
                    {
                        g.FillRectangle(brush, finalScreenX, finalScreenY, 2, 2);
                    }
                }
            }

            DrawCubeOutline(g, centerX, centerY);
        }

        // Projects and draws the wireframe outline of the 3D cube surrounding the color space.
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
                double x = cubeEdges[i][0];
                double y = cubeEdges[i][1];
                double z = cubeEdges[i][2];

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

                foreach (var line in lines)
                {
                    g.DrawLine(p, projectedEdges[line[0]], projectedEdges[line[1]]);
                }
            }
        }

        // Stores the initial mouse cursor coordinates when the user presses the left mouse button.
        private void Visualizer_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                lastMousePosition = e.Location;
        }

        // Calculates mouse movement deltas to dynamically rotate the 3D view when dragging.
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

        // Modifies the scale multiplier based on mouse wheel scrolling to zoom the 3D plot in or out.
        private void Visualizer_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0) zoomScale += 0.05f;
            else if (e.Delta < 0 && zoomScale > 0.1f) zoomScale -= 0.05f;

            this.Refresh();
        }
    }
}
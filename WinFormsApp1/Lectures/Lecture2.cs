using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1.Lectures
{
    public static class Lecture2
    {
        public static Form CreateForm(string imagePath, string outputPath)
        {
            Bitmap rgbImage = new Bitmap(imagePath);

            var form = new Form
            {
                Text = "Lecture 2 - YCbCr TrackBar",
                StartPosition = FormStartPosition.CenterScreen,
                ClientSize = new Size(rgbImage.Width + 20, rgbImage.Height + 160)
            };

            var pictureBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(rgbImage.Width, rgbImage.Height),
                Image = new Bitmap(rgbImage)
            };
            form.Controls.Add(pictureBox);

            var trackY = new TrackBar { Minimum = -100, Maximum = 100, Value = 0, Width = rgbImage.Width - 100, Location = new Point(10, rgbImage.Height + 10) };
            form.Controls.Add(trackY);
            var labelY = new Label { Text = "Y: 0", Location = new Point(10 + trackY.Width + 8, rgbImage.Height + 10), AutoSize = true };
            form.Controls.Add(labelY);

            var trackCb = new TrackBar { Minimum = -100, Maximum = 100, Value = 0, Width = rgbImage.Width - 100, Location = new Point(10, rgbImage.Height + 40) };
            form.Controls.Add(trackCb);
            var labelCb = new Label { Text = "Cb: 0", Location = new Point(10 + trackCb.Width + 8, rgbImage.Height + 40), AutoSize = true };
            form.Controls.Add(labelCb);

            var trackCr = new TrackBar { Minimum = -100, Maximum = 100, Value = 0, Width = rgbImage.Width - 100, Location = new Point(10, rgbImage.Height + 70) };
            form.Controls.Add(trackCr);
            var labelCr = new Label { Text = "Cr: 0", Location = new Point(10 + trackCr.Width + 8, rgbImage.Height + 70), AutoSize = true };
            form.Controls.Add(labelCr);

            var saveButton = new Button { Text = "Save", Location = new Point(rgbImage.Width - 70, rgbImage.Height + 100), Size = new Size(60, 24) };
            form.Controls.Add(saveButton);

            void UpdateImage()
            {
                int shiftY = trackY.Value;
                int shiftCb = trackCb.Value;
                int shiftCr = trackCr.Value;

                labelY.Text = "Y: " + shiftY;
                labelCb.Text = "Cb: " + shiftCb;
                labelCr.Text = "Cr: " + shiftCr;

                var newBmp = new Bitmap(rgbImage.Width, rgbImage.Height);
                for (int i = 0; i < rgbImage.Height; i++)
                {
                    for (int j = 0; j < rgbImage.Width; j++)
                    {
                        Color colorPixel = rgbImage.GetPixel(j, i);

                        int r0 = colorPixel.R;
                        int g0 = colorPixel.G;
                        int b0 = colorPixel.B;

                        double y = (0.299 * r0) + (0.587 * g0) + (0.114 * b0);
                        double cb = (0.168736 * r0) - (0.331264 * g0) + (0.5 * b0) + 128;
                        double cr = (0.5 * r0) - (0.418688 * g0) - (0.081312 * b0) + 128;

                        y += shiftY;
                        cb += shiftCb;
                        cr += shiftCr;

                        int yInt = Math.Clamp((int)Math.Round(y), 0, 255);
                        int cbInt = Math.Clamp((int)Math.Round(cb), 0, 255);
                        int crInt = Math.Clamp((int)Math.Round(cr), 0, 255);

                        double Y = yInt;
                        double Cb = cbInt - 128;
                        double Cr = crInt - 128;

                        int r = Math.Clamp((int)Math.Round(Y + 1.402 * Cr), 0, 255);
                        int g = Math.Clamp((int)Math.Round(Y - 0.344136 * Cb - 0.714136 * Cr), 0, 255);
                        int b = Math.Clamp((int)Math.Round(Y + 1.772 * Cb), 0, 255);

                        newBmp.SetPixel(j, i, Color.FromArgb(r, g, b));
                    }
                }

                var old = pictureBox.Image as Bitmap;
                pictureBox.Image = newBmp;
                old?.Dispose();
            }

            trackY.Scroll += (s, e) => UpdateImage();
            trackCb.Scroll += (s, e) => UpdateImage();
            trackCr.Scroll += (s, e) => UpdateImage();

            saveButton.Click += (s, e) =>
            {
                try
                {
                    var bmp = pictureBox.Image as Bitmap;
                    bmp?.Save(outputPath + "1.png");
                    MessageBox.Show("Saved to: " + outputPath + "1.png");
                }
                catch (Exception ex) { MessageBox.Show("Save failed:\n" + ex.Message); }
            };
            form.FormClosed += (s, e) =>
            {
                rgbImage.Dispose();
                (pictureBox.Image as Bitmap)?.Dispose();
            };

            return form;
        }
    }
}
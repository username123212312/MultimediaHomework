using Emgu.CV;
using Emgu.CV.CvEnum;
using OxyPlot;
using OxyPlot.Series;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using Rectangle = System.Drawing.Rectangle;

namespace WinFormsApp1.Lectures
{
    public static class Lecture3
    {
        public static void run()
        {
            external_excercise();
        }

        private static void external_excercise()
        {
            Form form = new Form();
            form.Text = "8-bit Indexed Conversion - External Exercise";
            form.ClientSize = new System.Drawing.Size(1100, 700);
            form.StartPosition = FormStartPosition.CenterScreen;

            PictureBox pbOriginal = new PictureBox
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(540, 560),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            PictureBox pbConverted = new PictureBox
            {
                Location = new System.Drawing.Point(560, 10),
                Size = new System.Drawing.Size(540, 560),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            Button btnLoad = new Button
            {
                Text = "Load Image",
                Location = new System.Drawing.Point(10, 580),
                Size = new System.Drawing.Size(100, 30)
            };

            Button btnConvert = new Button
            {
                Text = "Convert -> 8bpp",
                Location = new System.Drawing.Point(120, 580),
                Size = new System.Drawing.Size(120, 30),
                Enabled = false
            };

            TrackBar trackEnhance = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                LargeChange = 10,
                SmallChange = 1,
                TickFrequency = 10,
                Location = new System.Drawing.Point(260, 580),
                Size = new System.Drawing.Size(300, 45)
            };

            Label lblEnhance = new Label
            {
                Text = "Enhancement (contrast) : 0",
                Location = new System.Drawing.Point(260, 610),
                Size = new System.Drawing.Size(300, 20)
            };

            Button btnSave = new Button
            {
                Text = "Save Converted",
                Location = new System.Drawing.Point(570, 580),
                Size = new System.Drawing.Size(120, 30),
                Enabled = false
            };

            form.Controls.Add(pbOriginal);
            form.Controls.Add(pbConverted);
            form.Controls.Add(btnLoad);
            form.Controls.Add(btnConvert);
            form.Controls.Add(trackEnhance);
            form.Controls.Add(lblEnhance);
            form.Controls.Add(btnSave);

            Bitmap? loaded = null;
            Bitmap? converted = null;

            trackEnhance.Scroll += (s, e) =>
            {
                lblEnhance.Text = $"Enhancement (contrast) : {trackEnhance.Value}";
            };

            btnLoad.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Open Image";
                    ofd.Filter = "Images|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tiff|All files|*.*";
                    if (ofd.ShowDialog() != DialogResult.OK) return;
                    loaded?.Dispose();
                    converted?.Dispose();
                    loaded = new Bitmap(ofd.FileName);
                    pbOriginal.Image = (Bitmap)loaded.Clone();
                    pbConverted.Image = null;
                    btnConvert.Enabled = true;
                    btnSave.Enabled = false;
                }
            };

            btnConvert.Click += (s, e) =>
            {
                if (loaded == null) return;
                btnConvert.Enabled = false;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    converted?.Dispose();
                    converted = ConvertTo8bppIndexed(loaded, trackEnhance.Value);
                    pbConverted.Image = (Bitmap)converted.Clone();
                    btnSave.Enabled = true;
                    string outDir = AppDomain.CurrentDomain.BaseDirectory;
                    string outPath = Path.Combine(outDir, $"converted_8bit_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    converted.Save(outPath, ImageFormat.Png);
                    MessageBox.Show($"Converted image saved to:\n{outPath}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Conversion failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                    btnConvert.Enabled = true;
                }
            };

            btnSave.Click += (s, e) =>
            {
                if (converted == null) return;
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "PNG Image|*.png|Bitmap|*.bmp";
                    sfd.FileName = "converted_8bit.png";
                    if (sfd.ShowDialog() != DialogResult.OK) return;
                    converted.Save(sfd.FileName, ImageFormat.Png);
                    MessageBox.Show($"Saved to:\n{sfd.FileName}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Application.Run(form);

            loaded?.Dispose();
            converted?.Dispose();
        }

        /// <summary>
        /// Convert a 24-bit (or any RGB) Bitmap to an 8-bit indexed Bitmap using a simple 3-3-2 quantization.
        /// An optional enhancement level (0-100) increases contrast before quantization.
        /// </summary>
        private static Bitmap ConvertTo8bppIndexed(Bitmap src, int enhancementLevel)
        {
            Bitmap src24;
            if (src.PixelFormat != PixelFormat.Format24bppRgb)
            {
                src24 = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(src24))
                    g.DrawImage(src, 0, 0, src.Width, src.Height);
            }
            else
            {
                src24 = src;
            }

            int width = src24.Width;
            int height = src24.Height;

            Bitmap dest = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            // Build palette: 3 bits red, 3 bits green, 2 bits blue => 256 entries
            ColorPalette palette = dest.Palette;
            for (int i = 0; i < 256; i++)
            {
                int r3 = (i >> 5) & 0x07; // top 3 bits
                int g3 = (i >> 2) & 0x07; // middle 3 bits
                int b2 = i & 0x03;        // bottom 2 bits

                int r = (int)((r3 * 255.0) / 7.0);
                int g = (int)((g3 * 255.0) / 7.0);
                int b = (int)((b2 * 255.0) / 3.0);

                palette.Entries[i] = System.Drawing.Color.FromArgb(r, g, b);
            }
            dest.Palette = palette;

            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData srcData = src24.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dstData = dest.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

            try
            {
                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;

                int srcBytes = Math.Abs(srcStride) * height;
                int dstBytes = Math.Abs(dstStride) * height;

                byte[] srcBuffer = new byte[srcBytes];
                byte[] dstBuffer = new byte[dstBytes];

                Marshal.Copy(srcData.Scan0, srcBuffer, 0, srcBytes);

                // Enhancement: simple contrast tweak around mid (128)
                double factor = 1.0 + (enhancementLevel / 100.0); // 0->1; 100->2
                bool doEnhance = enhancementLevel > 0;

                for (int y = 0; y < height; y++)
                {
                    int srcRow = y * srcStride;
                    int dstRow = y * dstStride;
                    for (int x = 0; x < width; x++)
                    {
                        int srcIndex = srcRow + x * 3;
                        byte b = srcBuffer[srcIndex + 0];
                        byte g = srcBuffer[srcIndex + 1];
                        byte r = srcBuffer[srcIndex + 2];

                        if (doEnhance)
                        {
                            r = ApplyContrast(r, factor);
                            g = ApplyContrast(g, factor);
                            b = ApplyContrast(b, factor);
                        }

                        // 3-3-2 quantization formula
                        int r3 = r >> 5; // 0..7
                        int g3 = g >> 5; // 0..7
                        int b2 = b >> 6; // 0..3

                        byte index = (byte)((r3 << 5) | (g3 << 2) | b2);
                        dstBuffer[dstRow + x] = index;
                    }
                }

                // Copy destBuffer back to destination bitmap
                Marshal.Copy(dstBuffer, 0, dstData.Scan0, dstBytes);
            }
            finally
            {
                src24.UnlockBits(srcData);
                dest.UnlockBits(dstData);

                // If we created a converted temporary src24, dispose it
                if (!ReferenceEquals(src24, src))
                {
                    src24.Dispose();
                }
            }

            return dest;
        }

        private static byte ApplyContrast(byte c, double factor)
        {
            // Contrast around 128 (midpoint). factor 1.0 => no change, >1 increases contrast.
            double centered = c - 128.0;
            double adjusted = 128.0 + centered * factor;
            if (adjusted < 0) adjusted = 0;
            if (adjusted > 255) adjusted = 255;
            return (byte)adjusted;
        }

        private static void excercise3()
        {
            string imgPath = "C:\\Users\\Yousef Razzouk\\image\\repos\\WinFormsApp1\\WinFormsApp1\\pics\\kim_r_hunter-aircraft-5611528.jpg";
            string outPath = "C:\\Users\\Yousef Razzouk\\image\\repos\\WinFormsApp1\\WinFormsApp1\\pics\\8bitpic.png";

            using (Bitmap src = new Bitmap(imgPath))
            using (Bitmap dest = new Bitmap(src.Width, src.Height, PixelFormat.Format8bppIndexed))
            {

            }
        }

        private static void excercise2()
        {
            // Define quality values
            int[] qualityValues = { 50, 25, 15, 5, 1 };

            // Initialize lists to store compression ratios and corresponding qualities
            List<int> qualities = new List<int>();
            List<double> compressionRatios = new List<double>();

            string inputPath = "C:\\Users\\Yousef Razzouk\\source\\repos\\WinFormsApp1\\WinFormsApp1\\pics\\nature-27.jpg";

            using (FileStream inputStream = File.OpenRead(inputPath))
            using (Image<Rgb24> image = SixLabors.ImageSharp.Image.Load<Rgb24>(inputStream))
            {
                // Get the size of the original image
                long originalSize = new FileInfo(inputPath).Length;
                foreach (int q in qualityValues)
                {
                    string outputPath = $"output_image_q{q}.jpg";
                    using (FileStream outputStream = File.OpenWrite(outputPath))
                    {
                        image.SaveAsJpeg(outputStream, new SixLabors.
                            ImageSharp.Formats.Jpeg.JpegEncoder()
                        { Quality = q });
                    }
                    // Get the size of the compressed image
                    long compressedSize = new FileInfo(outputPath).Length;

                    // Calculate compression ratio (guard against zero)
                    double compressionRatio = compressedSize > 0 ? (double)originalSize / compressedSize : 0.0;

                    // Add quality and compression ratio to the lists
                    qualities.Add(q);
                    compressionRatios.Add(compressionRatio);
                }
            }

            // Print results
            for (int i = 0; i < qualities.Count; i++)
            {
                Console.WriteLine($"Quality {qualities[i]} -> Compression ratio: {compressionRatios[i]:F2}");
            }

            // Create a plot model
            var plotModel = new PlotModel { Title = "Quality vs Compression Ratio" };

            // Create a line series
            var lineSeries = new LineSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.White
            };
            // Add data points to the line series
            for (int i = 0; i < qualities.Count; i++)
            {
                lineSeries.Points.Add(new DataPoint(qualities[i], compressionRatios[i]));
            }
            // Add the line series to the plot model
            plotModel.Series.Add(lineSeries);

            // Export the plot to a file
            var exporter = new OxyPlot.SvgExporter { Width = 600, Height = 400 };
            using (var stream = File.Create("C:\\Users\\Yousef Razzouk\\source\\repos\\WinFormsApp1\\WinFormsApp1\\plots\\plot.svg"))
            {
                exporter.Export(plotModel, stream);
            }
        }

        private static void excercise1()
        {
            string imgPath = "C:\\Users\\Yousef Razzouk\\source\\repos\\WinFormsApp1\\WinFormsApp1\\pics\\nature-27.jpg";
            Bitmap img = new(imgPath);
            ShowImage(img);
            string outputPath = "C:\\Users\\Yousef Razzouk\\source\\repos\\WinFormsApp1\\WinFormsApp1\\pics\\new.jpg";
            SaveImage(img, outputPath);
            ShowImageInfo(img);
            Console.WriteLine($"Image Size : {GetImageSize(img)}");
        }

        static long GetImageSize(Bitmap img)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, img.RawFormat);
                return ms.Length;
            }
        }

        static void ShowImageInfo(Bitmap img)
        {
            Console.WriteLine($"Image Width : {img.Width}");
            Console.WriteLine($"Image Height : {img.Height}");
            Console.WriteLine($"Image Pixel Depth : " +
                $"{System.Drawing.Image.GetPixelFormatSize(img.PixelFormat)} bits per pixel");
            Console.WriteLine($"Image Width : {img.PixelFormat}");
        }
        static void SaveImage(Bitmap img, string outputPath)
        {
            if (img != null)
            {
                img.Save(outputPath);

            }
        }
        static void ShowImage(Bitmap img)
        {
            using (Form form = new Form())
            {
                form.Text = "Image Viewer";
                form.ClientSize = new System.Drawing.Size(img.Width, img.Height);
                PictureBox pictureBox = new PictureBox();
                pictureBox.Dock = DockStyle.Fill;
                pictureBox.Image = img;
                form.Controls.Add(pictureBox);
                Application.Run(form);
            }
        }
    }
}


//BitmapData bitmapData = image.LockBits(new System.Drawing.Rectangle(0, 0,
//                image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);

//IntPtr ptr = bitmapData.Scan0;
//// Declare an array to hold the bytes of the bitmap.
//int bytes = Math.Abs(bitmapData.Stride) * bitmapData.Height;
//byte[] rgbValues = new byte[bytes];

//// Copy the RGB values into the array.
//System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

//image.UnlockBits(bitmapData);
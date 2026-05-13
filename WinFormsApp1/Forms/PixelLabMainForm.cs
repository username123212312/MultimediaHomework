using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class PixelLabMainForm : Form
    {
        private readonly PictureBox pictureBox;
        private readonly Label dragDropLabel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;
        private readonly MenuStrip menuStrip;

        private string? currentImagePath;
        private Image? originalImage;

        public PixelLabMainForm()
        {
            Text = "PixelLab - Image Viewer";
            Width = 1000;
            Height = 700;
            AllowDrop = true;

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(24, 24, 30) 
            };

            dragDropLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Drag & drop an image here\nor File → Open",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point),
                BackColor = Color.Transparent,
                AutoSize = false
            };

            pictureBox.Controls.Add(dragDropLabel);

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel { Text = "No image loaded" };
            statusStrip.Items.Add(statusLabel);

            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var openItem = new ToolStripMenuItem("Open...", null, OpenItem_Click);
            var resetItem = new ToolStripMenuItem("Reset", null, ResetItem_Click) { Enabled = false };
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Close());
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openItem, resetItem, new ToolStripSeparator(), exitItem });
            menuStrip.Items.Add(fileMenu);
            MainMenuStrip = menuStrip;

            Controls.Add(pictureBox);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);

            DragEnter += PixelLabMainForm_DragEnter;
            DragDrop += PixelLabMainForm_DragDrop;
            pictureBox.MouseMove += PictureBox_MouseMove;
        }

        private void OpenItem_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tiff;*.webp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                LoadImage(ofd.FileName);
            }
        }

        private void ResetItem_Click(object? sender, EventArgs e)
        {
            if (originalImage != null)
            {
                SetPictureImage((Image)originalImage.Clone());
                statusLabel.Text = $"Restored: {Path.GetFileName(currentImagePath ?? string.Empty)}";
            }
        }

        private void PixelLabMainForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0 && IsImageFile(files[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void PixelLabMainForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                if (files.Length > 0 && IsImageFile(files[0]))
                {
                    LoadImage(files[0]);
                }
            }
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (pictureBox.Image == null)
            {
                dragDropLabel.Visible = true;
                statusLabel.Text = "No image loaded";
                return;
            }

            dragDropLabel.Visible = false;

            var img = pictureBox.Image;
            var imgRect = GetImageRectangle(pictureBox);
            if (!imgRect.Contains(e.Location))
            {
                statusLabel.Text = $"{Path.GetFileName(currentImagePath ?? string.Empty)} - {img.Width}x{img.Height}";
                return;
            }

            var ix = (int)((e.X - imgRect.X) * (double)img.Width / imgRect.Width);
            var iy = (int)((e.Y - imgRect.Y) * (double)img.Height / imgRect.Height);
            if (ix >= 0 && iy >= 0 && ix < img.Width && iy < img.Height)
            {
                if (img is Bitmap bmp)
                {
                    var color = bmp.GetPixel(ix, iy);
                    statusLabel.Text = $"{Path.GetFileName(currentImagePath ?? string.Empty)} - {img.Width}x{img.Height} | ({ix},{iy}) R:{color.R} G:{color.G} B:{color.B}";
                }
            }
        }

        private static Rectangle GetImageRectangle(PictureBox pb)
        {
            if (pb.Image == null) return Rectangle.Empty;
            var img = pb.Image;
            var imgRatio = (double)img.Width / img.Height;
            var pbRatio = (double)pb.ClientSize.Width / pb.ClientSize.Height;
            if (pbRatio > imgRatio)
            {
                var height = pb.ClientSize.Height;
                var width = (int)(height * imgRatio);
                var x = (pb.ClientSize.Width - width) / 2;
                return new Rectangle(x, 0, width, height);
            }
            else
            {
                var width = pb.ClientSize.Width;
                var height = (int)(width / imgRatio);
                var y = (pb.ClientSize.Height - height) / 2;
                return new Rectangle(0, y, width, height);
            }
        }

        private static bool IsImageFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".bmp" or ".png" or ".jpg" or ".jpeg" or ".gif" or ".tiff" or ".webp";
        }

        private void LoadImage(string path)
        {
            try
            {
                // Read bytes then create image from stream to avoid file lock
                var bytes = File.ReadAllBytes(path);
                using var ms = new MemoryStream(bytes);
                var img = Image.FromStream(ms);

                originalImage?.Dispose();
                originalImage = (Image)img.Clone();
                currentImagePath = path;

                SetPictureImage(img);
                statusLabel.Text = $"{Path.GetFileName(path)} - {img.Width}x{img.Height}";

                // enable Reset menu item
                foreach (ToolStripMenuItem menu in menuStrip.Items)
                {
                    if (menu.Text == "File")
                    {
                        foreach (ToolStripItem item in menu.DropDownItems)
                        {
                            if (item.Text == "Reset")
                                item.Enabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetPictureImage(Image img)
        {
            var old = pictureBox.Image;
            pictureBox.Image = img;
            old?.Dispose();

            // hide overlay when an image is present
            dragDropLabel.Visible = pictureBox.Image == null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox.Image?.Dispose();
                originalImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
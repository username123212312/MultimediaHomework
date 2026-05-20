using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

        // New UI controls for color-system selection and component controls
        private readonly Panel topPanel;
        private readonly ComboBox comboColorSystem;
        private readonly FlowLayoutPanel trackPanel;
        private readonly CheckBox chkR;
        private readonly CheckBox chkG;
        private readonly CheckBox chkB;
        private readonly NumericUpDown numWidthPct;
        private readonly NumericUpDown numHeightPct;

        private CancellationTokenSource? applyCts;

        private string? currentImagePath;
        private ColorSpaceVisualizer colorSpaceVisualizer;
        private Image? originalImage;

        private NumericUpDown numColorCount;
        private Label imageInfoLabel;
        private bool _isReducingColors = false;
        private string? _originalFilePath; 

        public PixelLabMainForm()
        {
            Text = "PixelLab - Image Viewer";
            Width = 1000;
            Height = 700;
            AllowDrop = true;

            colorSpaceVisualizer = new ColorSpaceVisualizer();
            colorSpaceVisualizer.BackColor = Color.FromArgb(25, 25, 25);
            colorSpaceVisualizer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            colorSpaceVisualizer.Location = new Point(this.Width - 340, 100);
            colorSpaceVisualizer.Size = new Size(320, 320);
            colorSpaceVisualizer.Name = "colorSpaceVisualizer";

            // Top panel: color system selector + dynamic trackbars
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 95,
                Padding = new Padding(6),
                BackColor = Color.FromArgb(32, 32, 36)
            };

            // Left container ensures combo has reserved space and prevents overlap.
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                Padding = new Padding(6),
                BackColor = Color.Transparent
            };

            comboColorSystem = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            comboColorSystem.Items.AddRange(new object[] { "RGB", "HSV", "CMYK", "YCbCr", "YUV", "LAB" });
            comboColorSystem.SelectedIndexChanged += ComboColorSystem_SelectedIndexChanged;
            leftPanel.Controls.Add(comboColorSystem);

            trackPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoScroll = true,
                Padding = new Padding(12, 6, 6, 6),
                WrapContents = false,
                Height = 44
            };

            // Right options panel: channel toggles + reshape controls
            var rightPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 500,
                Padding = new Padding(6),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                AutoScroll = true,

            };

            var colorReductionGroup = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Margin = new Padding(0, 5, 0, 5),
                BackColor = Color.FromArgb(45, 45, 48)
            };

            var lblColorReduction = new Label
            {
                Text = "(تقليل الألوان (لحظي",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Dock = DockStyle.Top,
                Padding = new Padding(6, 6, 6, 0),
                Height = 28
            };

            var colorControlPanel = new Panel { Dock = DockStyle.Fill };

            var lblColorCount = new Label
            {
                Text = "عدد الألوان:",
                ForeColor = Color.White,
                Location = new Point(6, 12),
                Size = new Size(70, 23)
            };

            numColorCount = new NumericUpDown
            {
                Minimum = 2,
                Maximum = 256,
                Value = 256,
                Width = 80,
                Location = new Point(85, 10),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White
            };

            numColorCount.ValueChanged += (s, e) =>
            {
                if (originalImage != null && !_isReducingColors && pictureBox.Image != null)
                {
                    _isReducingColors = true;
                    try
                    {
                        int colorCount = (int)numColorCount.Value;
                        var reduced = ColorConverter.ReduceColors((Bitmap)originalImage, colorCount);
                        var old = pictureBox.Image;
                        pictureBox.Image = reduced;
                        old?.Dispose();
                        colorSpaceVisualizer.SetImage(reduced);
                        UpdateCurrentImageInfo(reduced);
                        statusLabel.Text = $" تقليل الألوان إلى {colorCount} لون";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                    finally
                    {
                        _isReducingColors = false;
                    }
                }
            };

            colorControlPanel.Controls.Add(lblColorCount);
            colorControlPanel.Controls.Add(numColorCount);
            colorReductionGroup.Controls.Add(colorControlPanel);
            colorReductionGroup.Controls.Add(lblColorReduction);


            var infoTitle = new Label
            {
                Text = "معلومات الصورة:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(6, 12, 6, 0)
            };

            imageInfoLabel = new Label
            {
                Text = "لا توجد صورة",
                ForeColor = Color.LightGray,
                AutoSize = false,
                Width = 180,
                Height = 120,
                Font = new Font("Segoe UI", 8),
                Margin = new Padding(6, 3, 6, 8)
            };

            var chkPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0)
            };

            chkR = new CheckBox { Text = "R", Checked = true, ForeColor = Color.White, AutoSize = true, Margin = new Padding(6) };
            chkG = new CheckBox { Text = "G", Checked = true, ForeColor = Color.White, AutoSize = true, Margin = new Padding(6) };
            chkB = new CheckBox { Text = "B", Checked = true, ForeColor = Color.White, AutoSize = true, Margin = new Padding(6) };
            chkR.CheckedChanged += (s, e) => ScheduleApplyChanges();
            chkG.CheckedChanged += (s, e) => ScheduleApplyChanges();
            chkB.CheckedChanged += (s, e) => ScheduleApplyChanges();

            chkPanel.Controls.Add(chkR);
            chkPanel.Controls.Add(chkG);
            chkPanel.Controls.Add(chkB);

            var lblW = new Label { Text = "Width %", ForeColor = Color.White, AutoSize = true, Margin = new Padding(6, 8, 6, 0) };
            numWidthPct = new NumericUpDown { Minimum = 10, Maximum = 400, Value = 100, Width = 80, Margin = new Padding(6, 4, 6, 6) };
            numWidthPct.ValueChanged += (s, e) => ScheduleApplyChanges();

            var lblH = new Label { Text = "Height %", ForeColor = Color.White, AutoSize = true, Margin = new Padding(6, 6, 6, 0) };
            numHeightPct = new NumericUpDown { Minimum = 10, Maximum = 400, Value = 100, Width = 80, Margin = new Padding(6, 4, 6, 6) };
            numHeightPct.ValueChanged += (s, e) => ScheduleApplyChanges();

            rightPanel.Controls.Add(chkPanel);
            rightPanel.Controls.Add(lblW);
            rightPanel.Controls.Add(numWidthPct);
            rightPanel.Controls.Add(lblH);
            rightPanel.Controls.Add(numHeightPct);

            rightPanel.Controls.Add(colorReductionGroup);
            rightPanel.Controls.Add(infoTitle);
            rightPanel.Controls.Add(imageInfoLabel);


            // Add controls in order: trackPanel (fill), leftPanel (left dock), rightPanel (right dock).
            topPanel.Controls.Add(trackPanel);
            topPanel.Controls.Add(leftPanel);
            topPanel.Controls.Add(rightPanel);

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
            var resetItem = new ToolStripMenuItem("Reset", null, ResetItem_Click) { Enabled = true };
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Close());
            var saveItem = new ToolStripMenuItem("Save As...", null, SaveItem_Click) { Enabled = false };  
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openItem, resetItem, saveItem , new ToolStripSeparator(), exitItem });
            menuStrip.Items.Add(fileMenu);
            MainMenuStrip = menuStrip;

            Controls.Add(pictureBox);
            Controls.Add(colorSpaceVisualizer);
            Controls.Add(topPanel);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);

            colorSpaceVisualizer.BringToFront();
            DragEnter += PixelLabMainForm_DragEnter;
            DragDrop += PixelLabMainForm_DragDrop;
            pictureBox.MouseMove += PictureBox_MouseMove;

            comboColorSystem.SelectedIndex = 0;
        }

        // small typed container to hold captured UI state (read on UI thread)
        public struct UiState
        {
            public string System { get; }
            public TrackBar[] Sliders { get; }
            public bool KeepR { get; }
            public bool KeepG { get; }
            public bool KeepB { get; }
            public int WidthPct { get; }
            public int HeightPct { get; }

            public UiState(string system, TrackBar[] sliders, bool keepR, bool keepG, bool keepB, int widthPct, int heightPct)
            {
                System = system;
                Sliders = sliders;
                KeepR = keepR;
                KeepG = keepG;
                KeepB = keepB;
                WidthPct = widthPct;
                HeightPct = heightPct;
            }
        }
        // Capture all UI state on UI thread in one place
        private UiState CaptureUiState()
        {
            if (InvokeRequired)
            {
                return (UiState)Invoke((Func<UiState>)(() => CaptureUiStateCore()));
            }
            return CaptureUiStateCore();
        }

        private UiState CaptureUiStateCore()
        {
            var system = comboColorSystem.SelectedItem?.ToString() ?? "RGB";
            var sliders = trackPanel.Controls
                .OfType<Panel>()
                .Select(p => p.Controls.OfType<TrackBar>().FirstOrDefault())
                .Where(tb => tb != null)
                .Cast<TrackBar>()
                .ToArray();
            bool keepR = chkR.Checked, keepG = chkG.Checked, keepB = chkB.Checked;
            int widthPct = (int)numWidthPct.Value, heightPct = (int)numHeightPct.Value;
            return new UiState(system, sliders, keepR, keepG, keepB, widthPct, heightPct);
        }

        // helper to create a labelled TrackBar panel and add to trackPanel
        private TrackBar AddLabeledTrack(string labelText, int min, int max, int value)
        {
            var container = new Panel { Width = 200, Height = 44, Margin = new Padding(6) };
            var lbl = new Label { Text = labelText, Dock = DockStyle.Top, Height = 14, ForeColor = Color.White };
            var tb = new TrackBar
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                TickStyle = TickStyle.None,
                Dock = DockStyle.Bottom,
                Tag = labelText
            };
            tb.ValueChanged += TrackBar_ValueChanged;
            container.Controls.Add(tb);
            container.Controls.Add(lbl);
            trackPanel.Controls.Add(container);
            return tb;
        }

        private void ComboColorSystem_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var system = comboColorSystem.SelectedItem?.ToString() ?? "RGB";
            BuildTrackBarsForSystem(system);
            if (originalImage != null)
                ScheduleApplyChanges();
        }

        private void BuildTrackBarsForSystem(string system)
        {
            trackPanel.Controls.Clear();

            switch (system)
            {
                case "RGB":
                    AddLabeledTrack("R (%)", 0, 200, 100);
                    AddLabeledTrack("G (%)", 0, 200, 100);
                    AddLabeledTrack("B (%)", 0, 200, 100);
                    break;
                case "HSV":
                    AddLabeledTrack("H (deg)", -180, 180, 0);
                    AddLabeledTrack("S (%)", 0, 200, 100);
                    AddLabeledTrack("V (%)", 0, 200, 100);
                    break;
                case "CMYK":
                    AddLabeledTrack("C (%)", 0, 200, 100);
                    AddLabeledTrack("M (%)", 0, 200, 100);
                    AddLabeledTrack("Y (%)", 0, 200, 100);
                    AddLabeledTrack("K (%)", 0, 200, 100);
                    break;
                case "YCbCr":
                    AddLabeledTrack("Y (shift)", -100, 100, 0);
                    AddLabeledTrack("Cb (shift)", -128, 128, 0);
                    AddLabeledTrack("Cr (shift)", -128, 128, 0);
                    break;
                case "YUV":
                    AddLabeledTrack("Y (shift)", -100, 100, 0);
                    AddLabeledTrack("U (shift)", -128, 128, 0);
                    AddLabeledTrack("V (shift)", -128, 128, 0);
                    break;
                case "LAB":
                    AddLabeledTrack("L (shift)", -100, 100, 0);
                    AddLabeledTrack("a (shift)", -128, 128, 0);
                    AddLabeledTrack("b (shift)", -128, 128, 0);
                    break;
                default:
                    break;
            }
        }

        private void TrackBar_ValueChanged(object? sender, EventArgs e) => ScheduleApplyChanges();

        private void ScheduleApplyChanges()
        {
            applyCts?.Cancel();
            applyCts = new CancellationTokenSource();
            var token = applyCts.Token;
            _ = Task.Run(async () =>
            {
                try { await Task.Delay(120, token).ConfigureAwait(false); }
                catch (TaskCanceledException) { return; }

                if (token.IsCancellationRequested) return;
                await ApplyChangesAsync(token).ConfigureAwait(false);
            }, token);
        }

        private async Task ApplyChangesAsync(CancellationToken token)
        {
            if (_isReducingColors) return;

            if (originalImage == null) return;

            Bitmap srcBmp;
            lock (this)
            {
                srcBmp = new Bitmap(originalImage);
            }

            // Capture UI state once
            var state = CaptureUiState();

            if (token.IsCancellationRequested) { srcBmp.Dispose(); return; }

            Bitmap resultBmp = srcBmp;
            try
            {
                switch (state.System)
                {
                    case "RGB":
                        {
                            double rScale = state.Sliders.ElementAtOrDefault(0)?.Value / 100.0 ?? 1.0;
                            double gScale = state.Sliders.ElementAtOrDefault(1)?.Value / 100.0 ?? 1.0;
                            double bScale = state.Sliders.ElementAtOrDefault(2)?.Value / 100.0 ?? 1.0;
                            resultBmp = await Task.Run(() => ColorConverter.ApplyRgbChannelMultipliers(srcBmp, rScale, gScale, bScale), token).ConfigureAwait(false);
                            break;
                        }
                    case "HSV":
                        {
                            int hShift = state.Sliders.ElementAtOrDefault(0)?.Value ?? 0;
                            double sScale = (state.Sliders.ElementAtOrDefault(1)?.Value ?? 100) / 100.0;
                            double vScale = (state.Sliders.ElementAtOrDefault(2)?.Value ?? 100) / 100.0;
                            resultBmp = await Task.Run(() => ColorConverter.ApplyHsvAdjustments(srcBmp, hShift, sScale, vScale), token).ConfigureAwait(false);
                            break;
                        }
                    case "CMYK":
                        {
                            double c = (state.Sliders.ElementAtOrDefault(0)?.Value ?? 100) / 100.0;
                            double m = (state.Sliders.ElementAtOrDefault(1)?.Value ?? 100) / 100.0;
                            double y = (state.Sliders.ElementAtOrDefault(2)?.Value ?? 100) / 100.0;
                            double k = (state.Sliders.ElementAtOrDefault(3)?.Value ?? 100) / 100.0;
                            resultBmp = await Task.Run(() => ColorConverter.ApplyCmykAdjustments(srcBmp, c, m, y, k), token).ConfigureAwait(false);
                            break;
                        }
                    case "YCbCr":
                        {
                            int yShift = state.Sliders.ElementAtOrDefault(0)?.Value ?? 0;
                            int cbShift = state.Sliders.ElementAtOrDefault(1)?.Value ?? 0;
                            int crShift = state.Sliders.ElementAtOrDefault(2)?.Value ?? 0;
                            resultBmp = await Task.Run(() => ColorConverter.ApplyYcbcrAdjustments(srcBmp, yShift, cbShift, crShift), token).ConfigureAwait(false);
                            break;
                        }
                    case "YUV":
                        {
                            int yShift = state.Sliders.ElementAtOrDefault(0)?.Value ?? 0;
                            int uShift = state.Sliders.ElementAtOrDefault(1)?.Value ?? 0;
                            int vShift = state.Sliders.ElementAtOrDefault(2)?.Value ?? 0;
                            resultBmp = await Task.Run(() => ColorConverter.ApplyYuvAdjustments(srcBmp, yShift, uShift, vShift), token).ConfigureAwait(false);
                            break;
                        }
                    case "LAB":
                        {
                            int lShift = state.Sliders.ElementAtOrDefault(0)?.Value ?? 0;
                            int aShift = state.Sliders.ElementAtOrDefault(1)?.Value ?? 0;
                            int bShift = state.Sliders.ElementAtOrDefault(2)?.Value ?? 0;
                            resultBmp = await Task.Run(() => ColorConverter.ApplyLabAdjustments(srcBmp, lShift, aShift, bShift), token).ConfigureAwait(false);
                            break;
                        }
                    default:
                        resultBmp = new Bitmap(srcBmp);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                srcBmp.Dispose();
                return;
            }
            catch (Exception ex)
            {
                srcBmp.Dispose();
                BeginInvoke((Action)(() => MessageBox.Show(this, $"Conversion failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                return;
            }
            finally
            {
                if (!ReferenceEquals(resultBmp, srcBmp))
                    srcBmp.Dispose();
            }

            if (token.IsCancellationRequested)
            {
                resultBmp.Dispose();
                return;
            }

            // Apply channel mask if any channel disabled
            Bitmap masked = resultBmp;
            if (!(state.KeepR && state.KeepG && state.KeepB))
            {
                try
                {
                    var tmp = await Task.Run(() => ColorConverter.ApplyRgbChannelMask(resultBmp, state.KeepR, state.KeepG, state.KeepB), token).ConfigureAwait(false);
                    if (!ReferenceEquals(tmp, resultBmp))
                    {
                        masked = tmp;
                        resultBmp.Dispose();
                    }
                }
                catch (OperationCanceledException) { resultBmp.Dispose(); return; }
            }

            // Apply resizing if requested (percent not 100)
            Bitmap finalBmp = masked;
            if (state.WidthPct != 100 || state.HeightPct != 100)
            {
                try
                {
                    var tmp = await Task.Run(() => ColorConverter.ResizeBitmap(masked, state.WidthPct, state.HeightPct), token).ConfigureAwait(false);
                    if (!ReferenceEquals(tmp, masked))
                    {
                        finalBmp = tmp;
                        masked.Dispose();
                    }
                }
                catch (OperationCanceledException) { masked.Dispose(); return; }
            }

            if (token.IsCancellationRequested)
            {
                finalBmp.Dispose();
                return;
            }

            BeginInvoke((Action)(() =>
            {
                var old = pictureBox.Image;
                pictureBox.Image = finalBmp;
                old?.Dispose();
                statusLabel.Text = $"{Path.GetFileName(currentImagePath ?? string.Empty)} - {finalBmp.Width}x{finalBmp.Height}";
                dragDropLabel.Visible = pictureBox.Image == null;

                if (finalBmp != null)
                {
                    colorSpaceVisualizer.SetImage(finalBmp);
                    UpdateCurrentImageInfo(finalBmp);
                }
            }));
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
                applyCts?.Cancel();
                SetPictureImage((Image)originalImage.Clone());
                UpdateCurrentImageInfo((Image)originalImage.Clone());
                statusLabel.Text = $"Restored: {Path.GetFileName(currentImagePath ?? string.Empty)}";
            }
        }

        private void SaveItem_Click(object? sender, EventArgs e)
        {
            if (pictureBox.Image == null)
            {
                MessageBox.Show("لا توجد صورة للحفظ!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Title = "حفظ الصورة المعدلة";
            sfd.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|BMP Image (*.bmp)|*.bmp";
            sfd.FilterIndex = 1;
            sfd.FileName = Path.GetFileNameWithoutExtension(currentImagePath ?? "image") + "_edited";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Bitmap bmp = new Bitmap(pictureBox.Image.Width, pictureBox.Image.Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(pictureBox.Image, 0, 0, bmp.Width, bmp.Height);
                    }

                    ImageFormat format = sfd.FilterIndex switch
                    {
                        1 => ImageFormat.Png,
                        2 => ImageFormat.Jpeg,
                        3 => ImageFormat.Bmp,
                        _ => ImageFormat.Png
                    };

                    bmp.Save(sfd.FileName, format);
                    bmp.Dispose();

                    MessageBox.Show($"تم حفظ الصورة بنجاح!", "تم الحفظ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    statusLabel.Text = $"Saved: {Path.GetFileName(sfd.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"فشل حفظ الصورة: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                var bytes = File.ReadAllBytes(path);
                using var ms = new MemoryStream(bytes);
                var img = Image.FromStream(ms);

                originalImage?.Dispose();
                originalImage = (Image)img.Clone();
                currentImagePath = path;
                _originalFilePath = path;

                SetPictureImage(img);
                UpdateCurrentImageInfo(img);
                statusLabel.Text = $"{Path.GetFileName(path)} - {img.Width}x{img.Height}";

                foreach (ToolStripMenuItem menu in menuStrip.Items)
                {
                    if (menu.Text == "File")
                    {
                        foreach (ToolStripItem item in menu.DropDownItems)
                        {
                            if (item.Text == "Reset")
                                item.Enabled = true;

                            if (item.Text == "Save As...")
                                item.Enabled = true;
                        }
                    }
                }

                ScheduleApplyChanges();
                colorSpaceVisualizer.SetImage((Bitmap)img);
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
            dragDropLabel.Visible = pictureBox.Image == null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                applyCts?.Cancel();
                pictureBox.Image?.Dispose();
                originalImage?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.colorSpaceVisualizer = new WinFormsApp1.ColorSpaceVisualizer();
            this.SuspendLayout();
            // 
            // colorSpaceVisualizer
            // 
            this.colorSpaceVisualizer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(25)))), ((int)(((byte)(25)))));
            this.colorSpaceVisualizer.Location = new System.Drawing.Point(599, 73);
            this.colorSpaceVisualizer.Name = "colorSpaceVisualizer";
            this.colorSpaceVisualizer.Size = new System.Drawing.Size(320, 320);
            this.colorSpaceVisualizer.TabIndex = 0;
            this.colorSpaceVisualizer.Load += new System.EventHandler(this.colorSpaceVisualizer1_Load);
            this.colorSpaceVisualizer.Resize += new System.EventHandler(this.colorSpaceVisualizer_Resize);
            // 
            // PixelLabMainForm
            // 
            this.ClientSize = new System.Drawing.Size(672, 330);
            this.Controls.Add(this.colorSpaceVisualizer);
            this.Name = "PixelLabMainForm";
            this.ResumeLayout(false);

        }

        private void colorSpaceVisualizer1_Load(object sender, EventArgs e)
        {

        }

        private void colorSpaceVisualizer_Resize(object sender, EventArgs e)
        {
            int width = colorSpaceVisualizer.Width;
            int height = colorSpaceVisualizer.Height;

            if (height == 0) height = 1; 

            double aspectRatio = (double)width / height;


            colorSpaceVisualizer.Invalidate();
        }

        private void UpdateCurrentImageInfo(Image img)
        {
            if (img == null)
            {
                imageInfoLabel.Text = "لا توجد صورة";
                return;
            }

            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(_originalFilePath))
            {
                var fileInfo = new FileInfo(_originalFilePath);
                sb.AppendLine($"{Path.GetFileName(_originalFilePath)}");
                sb.AppendLine($"{img.Width} x {img.Height}");
                sb.AppendLine($"{fileInfo.Length / 1024} KB");
            }
            else
            {
                sb.AppendLine($"{img.Width} x {img.Height}");
            }

            sb.AppendLine($"{img.PixelFormat}");
            sb.AppendLine($"بكسل: {img.Width * img.Height:N0}");

            imageInfoLabel.Text = sb.ToString();
        }
    }
}
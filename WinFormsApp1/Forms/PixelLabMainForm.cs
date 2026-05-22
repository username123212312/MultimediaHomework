using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WinFormsApp1
{
    public class PixelLabMainForm : Form
    {
        private readonly PictureBox pictureBox;
        private readonly Label dragDropLabel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;
        private readonly MenuStrip menuStrip;

        private readonly Panel topPanel;
        private readonly ComboBox comboColorSystem;
        private readonly FlowLayoutPanel trackPanel;
        private readonly CheckBox chkR;
        private readonly CheckBox chkG;
        private readonly CheckBox chkB;
        private readonly CheckBox chkK; // new for CMYK
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

        private Button btnSwitchSystem;
        private Label lblRGBResult;
        private Label lblHSVResult;
        private ToolStripMenuItem? saveMenuItem;


        public struct HSVColor
        {
            public double H; public double S; public double V;
        }

        public struct CMYKColor
        {
            public double C; public double M; public double Y; public double K;
        }

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

            colorSpaceVisualizer.MouseClick += colorSpaceVisualizer_MouseClick;

            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 95,
                Padding = new Padding(6),
                BackColor = Color.FromArgb(32, 32, 36)
            };

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
            comboColorSystem.SelectedIndexChanged += comboColorSystem_SelectedIndexChanged;
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

            var rightPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 500,
                Padding = new Padding(6),
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
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
            // new CMYK K checkbox
            chkK = new CheckBox { Text = "K", Checked = true, ForeColor = Color.White, AutoSize = true, Margin = new Padding(6) };

            chkR.CheckedChanged += (s, e) => ScheduleApplyChanges();
            chkG.CheckedChanged += (s, e) => ScheduleApplyChanges();
            chkB.CheckedChanged += (s, e) => ScheduleApplyChanges();
            chkK.CheckedChanged += (s, e) => ScheduleApplyChanges();

            chkPanel.Controls.Add(chkR);
            chkPanel.Controls.Add(chkG);
            chkPanel.Controls.Add(chkB);
            chkPanel.Controls.Add(chkK);

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

            saveMenuItem = new ToolStripMenuItem("Save As...", null, SaveItem_Click) { Enabled = false };

            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openItem, resetItem, saveMenuItem, new ToolStripSeparator(), exitItem });
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

            InitializeCustomPlacementComponents();
            lblRGBResult.Visible = false;
            lblHSVResult.Visible = false;


        }


        private void InitializeCustomPlacementComponents()
        {
            lblRGBResult = new Label();
            lblRGBResult.Text = "RGB → (0, 0, 0)";
            lblRGBResult.ForeColor = Color.LightGreen;
            lblRGBResult.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblRGBResult.Size = new Size(320, 25);
            lblRGBResult.Location = new Point(this.Width - 340, 440);
            lblRGBResult.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            lblHSVResult = new Label();
            lblHSVResult.Text = "HSV → (0°, 0%, 0%)";
            lblHSVResult.ForeColor = Color.LightCyan;
            lblHSVResult.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblHSVResult.Size = new Size(320, 25);
            lblHSVResult.Location = new Point(this.Width - 340, 470);
            lblHSVResult.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            btnSwitchSystem = new Button();
            btnSwitchSystem.Text = "Color Space 3D Visualizer";
            btnSwitchSystem.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSwitchSystem.Size = new Size(220, 45);
            btnSwitchSystem.BackColor = Color.FromArgb(50, 50, 55);
            btnSwitchSystem.ForeColor = Color.White;
            btnSwitchSystem.FlatStyle = FlatStyle.Flat;
            btnSwitchSystem.Cursor = Cursors.Hand;

            btnSwitchSystem.Location = new Point(
                this.ClientSize.Width - btnSwitchSystem.Width - 20,
                this.ClientSize.Height - btnSwitchSystem.Height - 40
            );
            btnSwitchSystem.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            btnSwitchSystem.Click += btnOpenVisualizerForm_Click;

            this.Controls.Add(lblRGBResult);
            this.Controls.Add(lblHSVResult);
            this.Controls.Add(btnSwitchSystem);

            lblRGBResult.BringToFront();
            lblHSVResult.BringToFront();
            btnSwitchSystem.BringToFront();
        }
        //private void btnOpenVisualizerForm_Click(object? sender, EventArgs e)
        //{
        //    ...
        //}
        private void btnOpenVisualizerForm_Click(object? sender, EventArgs e)
        {
            if (originalImage == null)
            {
                MessageBox.Show("Please load an image first!", "No Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Form visualizerForm = new Form
            {
                Text = "3D Color Space Explorer",
                Width = 500,
                Height = 750,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox = false,
                BackColor = Color.FromArgb(25, 25, 25)
            };

            string currentSystem = comboColorSystem.SelectedItem?.ToString() ?? "RGB";

            colorSpaceVisualizer.Dock = DockStyle.Top;
            colorSpaceVisualizer.Height = 400;
            visualizerForm.Controls.Add(colorSpaceVisualizer);
            colorSpaceVisualizer.SetColorSystem(currentSystem);

            Panel infoPanel = new Panel { Dock = DockStyle.Bottom, Height = 300, Padding = new Padding(20), BackColor = Color.FromArgb(32, 32, 36) };

            // تعريف مصفوفة Labels لعرض الـ 6 أنظمة بوضوح
            Label[] colorLabels = new Label[6];
            string[] titles = { "RGB", "HSV", "CMYK", "YUV", "LAB", "YCbCr" };

            for (int i = 0; i < 6; i++)
            {
                colorLabels[i] = new Label { Text = $"{titles[i]} → ...", ForeColor = Color.Cyan, Font = new Font("Consolas", 11F, FontStyle.Bold), Dock = DockStyle.Top, Height = 35 };
                infoPanel.Controls.Add(colorLabels[i]);
            }
            visualizerForm.Controls.Add(infoPanel);

            EventHandler<Color> colorPickedHandler = (s_sender, pickedColor) =>
            {
                int r = pickedColor.R, g = pickedColor.G, b = pickedColor.B;
                double rN = r / 255.0, gN = g / 255.0, bN = b / 255.0;
                double max = Math.Max(rN, Math.Max(gN, bN)), min = Math.Min(rN, Math.Min(gN, bN)), delta = max - min;
                double v = Math.Round(max * 100), se = max == 0 ? 0 : Math.Round((delta / max) * 100);

                double h = 0;
                if (delta != 0)
                {
                    if (max == rN) h = 60 * (((gN - bN) / delta) % 6);
                    else if (max == gN) h = 60 * (((bN - rN) / delta) + 2);
                    else if (max == bN) h = 60 * (((rN - gN) / delta) + 4);
                    if (h < 0) h += 360;
                }
                h = Math.Round(h);

                double k = 1 - max;
                double c = k == 1 ? 0 : (1 - rN - k) / (1 - k);
                double m = k == 1 ? 0 : (1 - gN - k) / (1 - k);
                double y = k == 1 ? 0 : (1 - bN - k) / (1 - k);

                double Y_u = 0.299 * r + 0.587 * g + 0.114 * b;
                double U_u = -0.14713 * r - 0.28886 * g + 0.436 * b;
                double V_u = 0.615 * r - 0.51499 * g - 0.10001 * b;

                double Y_c = 16 + (65.481 * rN + 128.553 * gN + 24.966 * bN);
                double Cb = 128 + (-37.797 * rN - 74.203 * gN + 112.0 * bN);
                double Cr = 128 + (112.0 * rN - 93.786 * gN - 18.214 * bN);

                double L = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 2.55;
                double a_lab = r - g, b_lab = g - b;

                Action updateUI = () =>
                {
                    colorLabels[0].Text = $"RGB   → ({r}, {g}, {b})";
                    colorLabels[1].Text = $"HSV   → ({h}°, {se}%, {v}%)";
                    colorLabels[2].Text = $"CMYK  → ({Math.Round(c * 100)}%, {Math.Round(m * 100)}%, {Math.Round(y * 100)}%, {Math.Round(k * 100)}%)";
                    colorLabels[3].Text = $"YUV   → ({Math.Round(Y_u, 1)}, {Math.Round(U_u, 1)}, {Math.Round(V_u, 1)})";
                    colorLabels[4].Text = $"LAB   → ({Math.Round(L, 1)}, {Math.Round(a_lab, 1)}, {Math.Round(b_lab, 1)})";
                    colorLabels[5].Text = $"YCbCr → ({Math.Round(Y_c)}, {Math.Round(Cb)}, {Math.Round(Cr)})";
                    foreach (var lbl in colorLabels) lbl.Refresh();
                };

                if (infoPanel.InvokeRequired) infoPanel.BeginInvoke(updateUI);
                else updateUI();
            };

            colorSpaceVisualizer.ColorPicked += colorPickedHandler;
            visualizerForm.FormClosing += (s, ev) => { colorSpaceVisualizer.ColorPicked -= colorPickedHandler; };
            visualizerForm.ShowDialog();
        }

        private void colorSpaceVisualizer_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    if (sender is Control visualizerControl)
                    {
                        using (Bitmap bmp = new Bitmap(1, 1))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                Point screenPos = visualizerControl.PointToScreen(e.Location);
                                g.CopyFromScreen(screenPos.X, screenPos.Y, 0, 0, new Size(1, 1));
                            }
                            Color pickedColor = bmp.GetPixel(0, 0);
                            UpdateAndSyncColorOutputs(pickedColor.R, pickedColor.G, pickedColor.B);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Color Pick Error]: {ex.Message}");
                }
            }
        }

        private void UpdateAndSyncColorOutputs(int r, int g, int b)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateAndSyncColorOutputs(r, g, b)));
                return;
            }

            double rNorm = r / 255.0; double gNorm = g / 255.0; double bNorm = b / 255.0;
            double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
            double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
            double delta = max - min;

            double v = Math.Round(max * 100);
            double s = max == 0 ? 0 : Math.Round((delta / max) * 100);
            double h = 0;

            if (delta != 0)
            {
                if (max == rNorm) h = 60 * (((gNorm - bNorm) / delta) % 6);
                else if (max == gNorm) h = 60 * (((bNorm - rNorm) / delta) + 2);
                else if (max == bNorm) h = 60 * (((rNorm - gNorm) / delta) + 4);
                if (h < 0) h += 360;
            }
            h = Math.Round(h);


        }


        public struct UiState
        {
            public string System { get; }
            public TrackBar[] Sliders { get; }
            public bool KeepR { get; }
            public bool KeepG { get; }
            public bool KeepB { get; }
            public bool KeepK { get; } // new
            public int WidthPct { get; }
            public int HeightPct { get; }

            public UiState(string system, TrackBar[] sliders, bool keepR, bool keepG, bool keepB, bool keepK, int widthPct, int heightPct)
            {
                System = system;
                Sliders = sliders;
                KeepR = keepR;
                KeepG = keepG;
                KeepB = keepB;
                KeepK = keepK;
                WidthPct = widthPct;
                HeightPct = heightPct;
            }
        }

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
            bool keepK = chkK.Checked;
            int widthPct = (int)numWidthPct.Value, heightPct = (int)numHeightPct.Value;
            return new UiState(system, sliders, keepR, keepG, keepB, keepK, widthPct, heightPct);
        }

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

        private void comboColorSystem_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (comboColorSystem.SelectedItem == null) return;

            string currentSystem = comboColorSystem.SelectedItem.ToString() ?? "RGB";

            // Update channel labels (visual only)
            if (currentSystem.Equals("CMYK", StringComparison.OrdinalIgnoreCase))
            {
                chkR.Text = "C";
                chkG.Text = "M";
                chkB.Text = "Y";
                chkK.Text = "K";
                chkK.Visible = true;
            }
            else if (currentSystem.Equals("YCbCr", StringComparison.OrdinalIgnoreCase))
            {
                // Explicit labels: Y, Cb, Cr
                chkR.Text = "Y";
                chkG.Text = "Cb";
                chkB.Text = "Cr";
                chkK.Visible = false;
            }
            else
            {
                // fallback: first three characters (e.g. RGB, HSV, YUV, LAB)
                if (currentSystem.Length >= 3)
                {
                    chkR.Text = currentSystem[0].ToString();
                    chkG.Text = currentSystem[1].ToString();
                    chkB.Text = currentSystem[2].ToString();
                }
                chkK.Visible = false; // only show K for CMYK
            }

            // Ensure trackbars are rebuilt for the newly selected system
            BuildTrackBarsForSystem(currentSystem);

            // Tell visualizer and reapply changes
            colorSpaceVisualizer?.SetColorSystem(currentSystem);
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
            if (_isReducingColors || originalImage == null) return;

            Bitmap srcBmp;
            lock (this)
            {
                srcBmp = new Bitmap(originalImage);
            }

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

            // Apply per-model channel mask when user disables channels (uses the visible checkbox meanings)
            Bitmap masked = resultBmp;
            if (!(state.KeepR && state.KeepG && state.KeepB && state.KeepK))
            {
                try
                {
                    var tmp = await Task.Run(() => ApplyModelChannelMask(resultBmp, state.System, state.KeepR, state.KeepG, state.KeepB, state.KeepK), token).ConfigureAwait(false);
                    if (!ReferenceEquals(tmp, resultBmp))
                    {
                        masked = tmp;
                        resultBmp.Dispose();
                    }
                }
                catch (OperationCanceledException) { resultBmp.Dispose(); return; }
            }

            // Apply resizing if requested
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

                colorSpaceVisualizer.SetImage(finalBmp);
                UpdateCurrentImageInfo(finalBmp);
            }));
        }
        private void OpenItem_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.bmp;*.png;*.jpg;*.jpeg;";
            if (ofd.ShowDialog() == DialogResult.OK) { LoadImage(ofd.FileName); }
        }

        private void LoadImage(string filePath)
        {
            try
            {
                _originalFilePath = filePath;
                currentImagePath = filePath;

                // Load via memory stream to avoid locking the file on disk
                byte[] bytes = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(bytes);
                using var tmp = Image.FromStream(ms);

                // store a dedicated clone as originalImage (dispose previous)
                lock (this)
                {
                    originalImage?.Dispose();
                    originalImage = new Bitmap(tmp);
                }

                BeginInvoke((Action)(() =>
                {
                    // replace displayed image with a fresh copy
                    var old = pictureBox.Image;
                    pictureBox.Image = new Bitmap((Bitmap)originalImage);
                    old?.Dispose();

                    dragDropLabel.Visible = false;
                    colorSpaceVisualizer.SetImage((Bitmap)pictureBox.Image);
                    UpdateCurrentImageInfo((Bitmap)pictureBox.Image);
                    if (saveMenuItem != null) saveMenuItem.Enabled = true;

                    // ensure the current UI state is applied to the newly loaded image
                    ScheduleApplyChanges();
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image: {ex.Message}");
            }
        }

        private void UpdateCurrentImageInfo(Bitmap bmp)
        {
            if (bmp == null)
            {
                imageInfoLabel.Text = "لا توجد صورة";
                return;
            }

            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(_originalFilePath))
            {
                var fileInfo = new FileInfo(_originalFilePath);
                sb.AppendLine($"{Path.GetFileName(_originalFilePath)}");
                sb.AppendLine($"{bmp.Width} x {bmp.Height}");
                sb.AppendLine($"{fileInfo.Length / 1024} KB");
            }
            else
            {
                sb.AppendLine($"{bmp.Width} x {bmp.Height}");
            }

            sb.AppendLine($"{bmp.PixelFormat}");
            sb.AppendLine($"بكسل: {bmp.Width * bmp.Height:N0}");

            imageInfoLabel.Text = sb.ToString();
        }

        private void ResetItem_Click(object? sender, EventArgs e)
        {
            if (originalImage != null)
            {
                applyCts?.Cancel();
                pictureBox.Image = new Bitmap(originalImage);
                colorSpaceVisualizer.SetImage((Bitmap)pictureBox.Image);
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
        private void PixelLabMainForm_DragEnter(object? sender, DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; }
        private void PixelLabMainForm_DragDrop(object? sender, DragEventArgs e) { var files = (string[])e.Data.GetData(DataFormats.FileDrop); if (files.Length > 0) LoadImage(files[0]); }
        private void PictureBox_MouseMove(object? sender, MouseEventArgs e) { }
        // Per-model masking helper: keepA/keepB/keepC/keepD = components for the current system
        private static Bitmap ApplyModelChannelMask(Bitmap src, string system, bool keepA, bool keepB, bool keepC, bool keepD)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, src.Width, src.Height);
            var srcData = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var dstData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                int bytes = Math.Abs(srcData.Stride) * src.Height;
                var buffer = new byte[bytes];
                Marshal.Copy(srcData.Scan0, buffer, 0, bytes);
                var outBuf = new byte[bytes];

                for (int y = 0; y < src.Height; y++)
                {
                    int row = y * srcData.Stride;
                    for (int x = 0; x < src.Width; x++)
                    {
                        int i = row + x * 4;
                        byte b = buffer[i + 0];
                        byte g = buffer[i + 1];
                        byte r = buffer[i + 2];
                        byte a = buffer[i + 3];

                        byte nr = r, ng = g, nb = b;

                        if (string.Equals(system, "RGB", StringComparison.OrdinalIgnoreCase))
                        {
                            nr = keepA ? r : (byte)0;
                            ng = keepB ? g : (byte)0;
                            nb = keepC ? b : (byte)0;
                        }
                        else if (string.Equals(system, "HSV", StringComparison.OrdinalIgnoreCase))
                        {
                            // RGB -> HSV
                            double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
                            double max = Math.Max(rd, Math.Max(gd, bd));
                            double min = Math.Min(rd, Math.Min(gd, bd));
                            double delta = max - min;
                            double h = 0, s = 0, v = max;
                            if (delta != 0)
                            {
                                if (Math.Abs(max - rd) < 1e-9) h = 60 * (((gd - bd) / delta) % 6);
                                else if (Math.Abs(max - gd) < 1e-9) h = 60 * (((bd - rd) / delta) + 2);
                                else h = 60 * (((rd - gd) / delta) + 4);
                                if (h < 0) h += 360;
                                s = max == 0 ? 0 : delta / max;
                            }

                            if (!keepA) h = 0;
                            if (!keepB) s = 0;
                            if (!keepC) v = 0;

                            // HSV -> RGB
                            double C = v * s;
                            double X = C * (1 - Math.Abs(((h / 60.0) % 2) - 1));
                            double m = v - C;
                            double rt = 0, gt = 0, bt = 0;
                            if (h < 60) { rt = C; gt = X; bt = 0; }
                            else if (h < 120) { rt = X; gt = C; bt = 0; }
                            else if (h < 180) { rt = 0; gt = C; bt = X; }
                            else if (h < 240) { rt = 0; gt = X; bt = C; }
                            else if (h < 300) { rt = X; gt = 0; bt = C; }
                            else { rt = C; gt = 0; bt = X; }

                            nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round((rt + m) * 255)));
                            ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round((gt + m) * 255)));
                            nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round((bt + m) * 255)));
                        }
                        else if (string.Equals(system, "CMYK", StringComparison.OrdinalIgnoreCase))
                        {
                            // RGB -> CMYK
                            double R = r / 255.0, G = g / 255.0, B = b / 255.0;
                            double K = 1 - Math.Max(R, Math.Max(G, B));
                            double C = 0, M = 0, Y = 0;
                            if (K < 1.0 - 1e-9)
                            {
                                C = (1 - R - K) / (1 - K);
                                M = (1 - G - K) / (1 - K);
                                Y = (1 - B - K) / (1 - K);
                            }

                            if (!keepA) C = 0;
                            if (!keepB) M = 0;
                            if (!keepC) Y = 0;
                            if (!keepD) K = 0; // if K unchecked -> remove black

                            double rOut = 1 - Math.Min(1.0, C * (1 - K) + K);
                            double gOut = 1 - Math.Min(1.0, M * (1 - K) + K);
                            double bOut = 1 - Math.Min(1.0, Y * (1 - K) + K);

                            nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rOut * 255)));
                            ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gOut * 255)));
                            nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bOut * 255)));
                        }
                        else if (string.Equals(system, "YCbCr", StringComparison.OrdinalIgnoreCase))
                        {
                            // RGB -> YCbCr (ITU-R BT.601)
                            double R = r, G = g, B = b;
                            double Y = 0.299 * R + 0.587 * G + 0.114 * B;
                            double Cb = 128 + (-0.168736 * R - 0.331264 * G + 0.5 * B);
                            double Cr = 128 + (0.5 * R - 0.418688 * G - 0.081312 * B);

                            if (!keepA) Y = 0;
                            if (!keepB) Cb = 128;
                            if (!keepC) Cr = 128;

                            double Cb_d = Cb - 128;
                            double Cr_d = Cr - 128;

                            double rOut = Y + 1.402 * Cr_d;
                            double gOut = Y - 0.344136 * Cb_d - 0.714136 * Cr_d;
                            double bOut = Y + 1.772 * Cb_d;

                            nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rOut)));
                            ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gOut)));
                            nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bOut)));
                        }
                        else if (string.Equals(system, "YUV", StringComparison.OrdinalIgnoreCase))
                        {
                            double R = r, G = g, B = b;
                            double Y = 0.299 * R + 0.587 * G + 0.114 * B;
                            double U = -0.14713 * R - 0.288862 * G + 0.436 * B + 128;
                            double V = 0.615 * R - 0.51498 * G - 0.10001 * B + 128;

                            if (!keepA) Y = 0;
                            if (!keepB) U = 128;
                            if (!keepC) V = 128;

                            double U_d = U - 128;
                            double V_d = V - 128;

                            double rOut = Y + 1.13983 * V_d;
                            double gOut = Y - 0.39465 * U_d - 0.58060 * V_d;
                            double bOut = Y + 2.03211 * U_d;

                            nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rOut)));
                            ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gOut)));
                            nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bOut)));
                        }
                        else if (string.Equals(system, "LAB", StringComparison.OrdinalIgnoreCase))
                        {
                            // Convert RGB -> Lab, mask, Lab -> RGB (approximation using same conversions as ColorConverter)
                            static double PivotRgb(double v)
                            {
                                v = v / 255.0;
                                return (v <= 0.04045) ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
                            }
                            static double InvPivotRgb(double v)
                            {
                                return (v <= 0.0031308) ? 12.92 * v : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
                            }
                            static double PivotXyzToLab(double t)
                            {
                                return t > 0.008856 ? Math.Pow(t, 1.0 / 3.0) : (7.787037 * t + 16.0 / 116.0);
                            }
                            static double InvPivotLab(double t)
                            {
                                double t3 = t * t * t;
                                return t3 > 0.008856 ? t3 : (t - 16.0 / 116.0) / 7.787037;
                            }

                            double Rlin = PivotRgb(r);
                            double Glin = PivotRgb(g);
                            double Blin = PivotRgb(b);

                            double X = Rlin * 0.4124564 + Glin * 0.3575761 + Blin * 0.1804375;
                            double Yv = Rlin * 0.2126729 + Glin * 0.7151522 + Blin * 0.0721750;
                            double Z = Rlin * 0.0193339 + Glin * 0.1191920 + Blin * 0.9503041;

                            double Xr = 0.95047, Yr = 1.0, Zr = 1.08883;
                            double fx = PivotXyzToLab(X / Xr);
                            double fy = PivotXyzToLab(Yv / Yr);
                            double fz = PivotXyzToLab(Z / Zr);

                            double L = Math.Max(0.0, 116.0 * fy - 16.0);
                            double a_lab = 500.0 * (fx - fy);
                            double b_lab = 200.0 * (fy - fz);

                            if (!keepA) L = 0;     // if L disabled -> black
                            if (!keepB) a_lab = 0;
                            if (!keepC) b_lab = 0;

                            double fy2 = (L + 16.0) / 116.0;
                            double fx2 = fy2 + (a_lab / 500.0);
                            double fz2 = fy2 - (b_lab / 200.0);

                            double xr = InvPivotLab(fx2);
                            double yr2 = InvPivotLab(fy2);
                            double zr = InvPivotLab(fz2);

                            double X2 = xr * Xr;
                            double Y2 = yr2 * Yr;
                            double Z2 = zr * Zr;

                            double rLinOut = 3.2406 * X2 - 1.5372 * Y2 - 0.4986 * Z2;
                            double gLinOut = -0.9689 * X2 + 1.8758 * Y2 + 0.0415 * Z2;
                            double bLinOut = 0.0557 * X2 - 0.2040 * Y2 + 1.0570 * Z2;

                            double rFinal = InvPivotRgb(rLinOut);
                            double gFinal = InvPivotRgb(gLinOut);
                            double bFinal = InvPivotRgb(bLinOut);

                            nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rFinal * 255.0)));
                            ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gFinal * 255.0)));
                            nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bFinal * 255.0)));
                        }

                        outBuf[i + 0] = nb;
                        outBuf[i + 1] = ng;
                        outBuf[i + 2] = nr;
                        outBuf[i + 3] = a;
                    }
                }

                Marshal.Copy(outBuf, 0, dstData.Scan0, bytes);
            }
            finally
            {
                src.UnlockBits(srcData);
                dst.UnlockBits(dstData);
            }

            return dst;
        }
    }
}
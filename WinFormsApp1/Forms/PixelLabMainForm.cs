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

        private Button btnSwitchSystem;
        private Label lblRGBResult;
        private Label lblHSVResult;


        
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
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openItem, resetItem, saveItem, new ToolStripSeparator(), exitItem });
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
        //    if (originalImage == null)
        //    {
        //        MessageBox.Show("Please load an image first!", "No Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        return;
        //    }

        //    Form visualizerForm = new Form
        //    {
        //        Text = "3D Color Space Explorer",
        //        Width = 500,
        //        Height = 680,
        //        StartPosition = FormStartPosition.CenterScreen,
        //        FormBorderStyle = FormBorderStyle.FixedSingle,
        //        MaximizeBox = false,
        //        BackColor = Color.FromArgb(25, 25, 25)
        //    };

        //    string currentSystem = comboColorSystem.SelectedItem?.ToString() ?? "RGB";

        //    colorSpaceVisualizer.Dock = DockStyle.Top;
        //    colorSpaceVisualizer.Height = 400;
        //    visualizerForm.Controls.Add(colorSpaceVisualizer);

        //    colorSpaceVisualizer.SetColorSystem(currentSystem);

        //    Panel infoPanel = new Panel
        //    {
        //        Dock = DockStyle.Bottom,
        //        Height = 240,
        //        Padding = new Padding(20, 10, 20, 10),
        //        BackColor = Color.FromArgb(32, 32, 36)
        //    };

        //    Label lblTitle = new Label
        //    {
        //        Text = $"Current Coordinates ({currentSystem} Model)",
        //        ForeColor = Color.White,
        //        Font = new Font("Segoe UI", 11F, FontStyle.Bold),
        //        Dock = DockStyle.Top,
        //        Height = 30
        //    };
        //    infoPanel.Controls.Add(lblTitle);

        //    Label lblAllSystems = new Label
        //    {
        //        Text = "Click on the visualizer to pick a color...",
        //        ForeColor = Color.LightGray,
        //        Font = new Font("Consolas", 10.5F, FontStyle.Regular),
        //        Dock = DockStyle.Fill,
        //        Location = new Point(20, 40)
        //    };
        //    infoPanel.Controls.Add(lblAllSystems);
        //    visualizerForm.Controls.Add(infoPanel);

        //    EventHandler<Color> colorPickedHandler = (s_sender, pickedColor) =>
        //    {
        //        int r = pickedColor.R; int g = pickedColor.G; int b = pickedColor.B;

        //        double rN = r / 255.0, gN = g / 255.0, bN = b / 255.0;
        //        double max = Math.Max(rN, Math.Max(gN, bN)), min = Math.Min(rN, Math.Min(gN, bN)), delta = max - min;
        //        double v = Math.Round(max * 100);

        //        double se = max == 0 ? 0 : Math.Round((delta / max) * 100);
        //        double h = 0;
        //        if (delta != 0)
        //        {
        //            if (max == rN) h = 60 * (((gN - bN) / delta) % 6);
        //            else if (max == gN) h = 60 * (((bN - rN) / delta) + 2);
        //            else if (max == bN) h = 60 * (((rN - gN) / delta) + 4);
        //            if (h < 0) h += 360;
        //        }
        //        h = Math.Round(h);

        //        double k = 1 - max;
        //        double c = k == 1 ? 0 : (1 - rN - k) / (1 - k);
        //        double m = k == 1 ? 0 : (1 - gN - k) / (1 - k);
        //        double y = k == 1 ? 0 : (1 - bN - k) / (1 - k);

        //        double Y_u = 0.299 * r + 0.587 * g + 0.114 * b;
        //        double U_u = -0.14713 * r - 0.28886 * g + 0.436 * b;
        //        double V_u = 0.615 * r - 0.51499 * g - 0.10001 * b;

        //        double Y_c = 16 + (65.481 * rN + 128.553 * gN + 24.966 * bN);
        //        double Cb = 128 + (-37.797 * rN - 74.203 * gN + 112.0 * bN);
        //        double Cr = 128 + (112.0 * rN - 93.786 * gN - 18.214 * bN);

        //        double L = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 2.55;
        //        double a_lab = r - g;
        //        double b_lab = g - b;

        //        Action updateUI = () => {
        //            lblAllSystems.Text =
        //                  $"RGB   → ({r}, {g}, {b})\n" +
        //                  $"HSV   → ({h}°, {se}%, {v}%)\n" +
        //                  $"CMYK  → ({Math.Round(c * 100)}%, {Math.Round(m * 100)}%, {Math.Round(y * 100)}%, {Math.Round(k * 100)}%)\n" +
        //                  $"YUV   → ({Math.Round(Y_u, 1)}, {Math.Round(U_u, 1)}, {Math.Round(V_u, 1)})\n" +
        //                  $"LAB   → ({Math.Round(L, 1)}, {Math.Round(a_lab, 1)}, {Math.Round(b_lab, 1)})\n" +
        //                  $"YCbCr → ({Math.Round(Y_c)}, {Math.Round(Cb)}, {Math.Round(Cr)})"; 

        //            lblAllSystems.Refresh();
        //        };

        //        if (lblAllSystems.InvokeRequired)
        //        {
        //            lblAllSystems.BeginInvoke(new Action(updateUI));
        //        }
        //        else
        //        {
        //            updateUI();
        //        }

        //        UpdateAndSyncColorOutputs(r, g, b);
        //    };

        //    colorSpaceVisualizer.ColorPicked += colorPickedHandler;

        //    visualizerForm.FormClosing += (senderForm, ev) =>
        //    {
        //        colorSpaceVisualizer.ColorPicked -= colorPickedHandler;

        //        colorSpaceVisualizer.Dock = DockStyle.None;
        //        colorSpaceVisualizer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        //        colorSpaceVisualizer.Location = new Point(this.Width - 340, 100);
        //        colorSpaceVisualizer.Size = new Size(320, 320);

        //        this.Controls.Add(colorSpaceVisualizer);
        //        colorSpaceVisualizer.BringToFront();
        //    };

        //    visualizerForm.ShowDialog();
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

                Action updateUI = () => {
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

            if (currentSystem.Length >= 3)
            {
                chkR.Text = currentSystem[0].ToString();
                chkG.Text = currentSystem[1].ToString();
                chkB.Text = currentSystem[2].ToString();
            }

            if (colorSpaceVisualizer != null)
            {
                colorSpaceVisualizer.SetColorSystem(currentSystem);
            }

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
            lock (this) { srcBmp = new Bitmap(originalImage); }
            var state = CaptureUiState();
            if (token.IsCancellationRequested) { srcBmp.Dispose(); return; }

            Bitmap resultBmp = srcBmp;
            try
            {
                switch (state.System)
                {
                    case "RGB":
                        double rScale = state.Sliders.ElementAtOrDefault(0)?.Value / 100.0 ?? 1.0;
                        double gScale = state.Sliders.ElementAtOrDefault(1)?.Value / 100.0 ?? 1.0;
                        double bScale = state.Sliders.ElementAtOrDefault(2)?.Value / 100.0 ?? 1.0;
                        resultBmp = await Task.Run(() => ColorConverter.ApplyRgbChannelMultipliers(srcBmp, rScale, gScale, bScale), token).ConfigureAwait(false);
                        break;
                    case "HSV":
                        int hShift = state.Sliders.ElementAtOrDefault(0)?.Value ?? 0;
                        double sScale = (state.Sliders.ElementAtOrDefault(1)?.Value ?? 100) / 100.0;
                        double vScale = (state.Sliders.ElementAtOrDefault(2)?.Value ?? 100) / 100.0;
                        resultBmp = await Task.Run(() => ColorConverter.ApplyHsvAdjustments(srcBmp, hShift, sScale, vScale), token).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception) { srcBmp.Dispose(); return; }

            BeginInvoke((Action)(() =>
            {
                var old = pictureBox.Image;
                pictureBox.Image = resultBmp;
                old?.Dispose();
                colorSpaceVisualizer.SetImage(resultBmp);
                UpdateCurrentImageInfo(resultBmp);
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
                var img = Image.FromFile(filePath);
                lock (this) { originalImage = img; }
                BeginInvoke((Action)(() =>
                {
                    pictureBox.Image = new Bitmap(img);
                    dragDropLabel.Visible = false;
                    colorSpaceVisualizer.SetImage((Bitmap)pictureBox.Image);
                    UpdateCurrentImageInfo((Bitmap)pictureBox.Image);
                }));
            }
            catch (Exception ex) { MessageBox.Show($"Failed to load image: {ex.Message}"); }
        }

        private void UpdateCurrentImageInfo(Bitmap bmp)
        {
            imageInfoLabel.Text = $"Dimensions: {bmp.Width} x {bmp.Height}\nFormat: {bmp.PixelFormat}";
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

        private void SaveItem_Click(object? sender, EventArgs e) { }
        private void PixelLabMainForm_DragEnter(object? sender, DragEventArgs e) { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; }
        private void PixelLabMainForm_DragDrop(object? sender, DragEventArgs e) { var files = (string[])e.Data.GetData(DataFormats.FileDrop); if (files.Length > 0) LoadImage(files[0]); }
        private void PictureBox_MouseMove(object? sender, MouseEventArgs e) { }
    }
}
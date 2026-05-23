using System.IO.Compression;


namespace WinFormsApp1.Lectures
{
    internal class Lecture6
    {
        public static void run()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Lecture6Form());
        }

        private class Lecture6Form : Form
        {
            private Button selectFilesButton;
            private Button compressButton;
            private ListView filesListView;
            private ColumnHeader colName;
            private ColumnHeader colSize;

            public Lecture6Form()
            {
                InitializeComponents();
            }

            private void InitializeComponents()
            {
                Text = "Lecture6 - ZIP Example";
                Size = new Size(640, 420);
                StartPosition = FormStartPosition.CenterScreen;

                selectFilesButton = new Button
                {
                    Text = "Select Files",
                    Location = new Point(12, 12),
                    Size = new Size(120, 30)
                };
                selectFilesButton.Click += SelectFilesButton_Click;

                compressButton = new Button
                {
                    Text = "Compress to ZIP",
                    Location = new Point(140, 12),
                    Size = new Size(120, 30)
                };
                compressButton.Click += CompressButton_Click;

                filesListView = new ListView
                {
                    Location = new Point(12, 50),
                    Size = new Size(600, 320),
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    MultiSelect = true
                };

                colName = new ColumnHeader { Text = "File name", Width = 420 };
                colSize = new ColumnHeader { Text = "Size (bytes)", Width = 160, TextAlign = HorizontalAlignment.Right };

                filesListView.Columns.AddRange(new ColumnHeader[] { colName, colSize });

                Controls.Add(selectFilesButton);
                Controls.Add(compressButton);
                Controls.Add(filesListView);
            }

            private void SelectFilesButton_Click(object? sender, EventArgs e)
            {
                using var ofd = new OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "All files (*.*)|*.*",
                    Title = "Select files to add"
                };

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                filesListView.Items.Clear();

                foreach (var path in ofd.FileNames)
                {
                    try
                    {
                        var fi = new FileInfo(path);
                        var item = new ListViewItem(fi.Name) { Tag = fi.FullName };
                        item.SubItems.Add(fi.Length.ToString());
                        filesListView.Items.Add(item);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Failed to add file '{path}': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            private void CompressButton_Click(object? sender, EventArgs e)
            {
                if (filesListView.Items.Count == 0)
                {
                    MessageBox.Show(this, "No files to compress. Use 'Select Files' first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    FileName = "output.zip",
                    Filter = "ZIP archive (*.zip)|*.zip",
                    Title = "Save ZIP file"
                };

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                var outputPath = sfd.FileName;

                try
                {
                    long originalTotal = 0;
                    foreach (ListViewItem item in filesListView.Items)
                    {
                        var fullPath = item.Tag as string;
                        if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                        {
                            originalTotal += new FileInfo(fullPath).Length;
                        }
                    }

                    if (File.Exists(outputPath))
                        File.Delete(outputPath);

                    using (var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
                    {
                        foreach (ListViewItem item in filesListView.Items)
                        {
                            var fullPath = item.Tag as string;
                            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                                continue;

                            var entryName = Path.GetFileName(fullPath);
                            archive.CreateEntryFromFile(fullPath, entryName, CompressionLevel.Optimal);
                        }
                    }

                    var compressedSize = new FileInfo(outputPath).Length;

                    var reduction = originalTotal - compressedSize;
                    double percent = originalTotal == 0 ? 0.0 : (double)reduction / originalTotal * 100.0;

                    var message = $"Files compressed to: {outputPath}\n\n" +
                                  $"Original total: {FormatBytes(originalTotal)}\n" +
                                  $"Compressed size: {FormatBytes(compressedSize)}\n" +
                                  $"Reduction: {FormatBytes(Math.Abs(reduction))} ({Math.Abs(percent):0.00}%)\n\n";

                    if (reduction > 0)
                        message += "Compression reduced the size.";
                    else if (reduction < 0)
                        message += "Archive is larger than original data (this can happen with already-compressed files or small files due to ZIP headers and metadata).";
                    else
                        message += "No size change.";

                    MessageBox.Show(this, message, "Compression Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Compression failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            private static string FormatBytes(long bytes)
            {
                if (bytes < 1024) return $"{bytes} B";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.00} KB";
                if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):0.00} MB";
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.00} GB";
            }
        }
    }
}

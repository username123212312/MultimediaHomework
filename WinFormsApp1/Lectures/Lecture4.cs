using NAudio.Gui;
using NAudio.Mixer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WinFormsApp1.Lectures
{
    internal class Lecture4
    {
        private static string outputPath = "C:\\Users\\Yousef Razzouk\\source\\repos\\WinFormsApp1\\WinFormsApp1\\audio";
        private static TimeSpan duration;
        static WaveOutEvent? waveOut;
        static AudioFileReader? audioReader;
        private static string? currentFile;

        public static void run()
        {
            excercise4();
        }

        private static void excercise1()
        {
            string audioFile = $"{outputPath}\\voice-telephony-8khz.wav";
            WaveViewer waveViewer = new WaveViewer();
            waveViewer.Dock = DockStyle.Fill;
            waveViewer.SamplesPerPixel = 400;
            waveViewer.StartPosition = 10000;
            // Load the audio file and set it as the WaveStream for the WaveViewer
            WaveFileReader waveFileReader = new WaveFileReader(audioFile);
            waveViewer.WaveStream = waveFileReader;
            // Create and configure the form
            Form form = new Form();
            form.Text = "WaveViewer Example";
            form.Controls.Add(waveViewer);
            form.ClientSize = new System.Drawing.Size(800, 600); // Set the form size
            form.ShowDialog();
        }

        private static void excercise2()
        {
            using (var waveIn = new WaveInEvent())
            {
                waveIn.WaveFormat = new WaveFormat(44100, 1); // 44100 Hz, mono
                WaveFileWriter waveFileWriter = null;
                waveIn.DataAvailable += (sender, e) =>
                {
                    // Initialize the WaveFileWriter on the first call to the DataAvailable event
                    if (waveFileWriter == null)
                    {
                        waveFileWriter = new WaveFileWriter($"{outputPath}\\new.wav", waveIn.WaveFormat);
                    }
                    // Write the recorded audio data to the WAV file
                    waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
                };

                waveIn.StartRecording();
                Console.WriteLine("Recording. Press any key to stop ... ");
                Console.ReadKey();
                waveIn.StopRecording();
                // Close the WaveFileWriter after recording is stopped
                waveFileWriter?.Dispose();
                Console.WriteLine("Recording stopped. Audio saved to: " + "C:\\Users\\HP\\Desktop\\recordtest1.wav");
            }
        }

        private static void excercise3()
        {
            string audioFile1 = $"{outputPath}\\new.wav";
            string audioFile2 = $"{outputPath}\\voice-telephony-8khz.wav";
            //var audioFileReader = new AudioFileReader(audioFile);

            using (var reader1 = new AudioFileReader(audioFile1))
            using (var reader2 = new AudioFileReader(audioFile2))
            {
                var mixer = new MixingSampleProvider(new[] { reader1, reader2 });
                WaveFileWriter.CreateWaveFile16($"{outputPath}\\merged.wav", mixer);
            }
        }

        private static void excercise4()
        {
            Form form = new Form();
            form.Text = "Stereo to Mono Converter";
            form.Size = new System.Drawing.Size(500, 150);

            Button btnLoad = new Button { Text = "Load WAV", Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(100, 40) };
            Button btnPlay = new Button { Text = "Play", Location = new System.Drawing.Point(130, 20), Size = new System.Drawing.Size(100, 40) };
            Button btnStop = new Button { Text = "Stop", Location = new System.Drawing.Point(240, 20), Size = new System.Drawing.Size(100, 40) };
            Button btnSave = new Button { Text = "Save as Mono", Location = new System.Drawing.Point(350, 20), Size = new System.Drawing.Size(120, 40) };

            form.Controls.Add(btnLoad);
            form.Controls.Add(btnPlay);
            form.Controls.Add(btnStop);
            form.Controls.Add(btnSave);
            btnLoad.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "WAV Files|*.wav";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        currentFile = ofd.FileName;
                        MessageBox.Show("File loaded!");
                    }
                }
            };

            btnPlay.Click += (s, e) =>
            {
                if (currentFile == null) { MessageBox.Show("Load a file first!"); return; }
                if (waveOut != null) { waveOut.Stop(); waveOut.Dispose(); }
                audioReader = new AudioFileReader(currentFile);
                waveOut = new WaveOutEvent();
                waveOut.Init(audioReader);
                waveOut.Play();
            };

            btnStop.Click += (s, e) =>
            {
                if (waveOut != null) { waveOut.Stop(); waveOut.Dispose(); waveOut = null; }
            };

            btnSave.Click += (s, e) =>
            {
                if (currentFile == null) { MessageBox.Show("Load a file first!"); return; }
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "WAV Files|*.wav";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var reader = new AudioFileReader(currentFile))
                    {
                        Console.WriteLine(reader.WaveFormat.Channels);
                        var mono = new StereoToMonoSampleProvider(reader);
                        WaveFileWriter.CreateWaveFile16(sfd.FileName, mono);
                        Console.WriteLine(mono.WaveFormat.Channels);
                    }
                    MessageBox.Show("Mono file saved!");
                }
            };

            Application.Run(form);

        }
    }
}

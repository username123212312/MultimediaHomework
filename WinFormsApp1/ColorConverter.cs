using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WinFormsApp1
{
    // Pure System.Drawing based conversion helpers suitable for UI wiring.
    // - No Emgu dependency
    // - Methods accept Bitmap and return a new Bitmap (caller must Dispose results)
    public static class ColorConverter
    {
        // Helper: copy pixels using LockBits and apply per-pixel transform (BGRA byte order)
        private static Bitmap ProcessBitmap(Bitmap src, Func<byte, byte, byte, (byte r, byte g, byte b)> transform)
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

                        var (nr, ng, nb) = transform(b, g, r);

                        outBuf[i + 0] = nb;
                        outBuf[i + 1] = ng;
                        outBuf[i + 2] = nr;
                        outBuf[i + 3] = a; // preserve alpha
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

        // RGB channel multipliers (rScale,gScale,bScale are multipliers, 1.0 = no change)
        public static Bitmap ApplyRgbChannelMultipliers(Bitmap src, double rScale, double gScale, double bScale)
        {
            return ProcessBitmap(src, (b, g, r) =>
            {
                int nr = (int)Math.Round(r * rScale);
                int ng = (int)Math.Round(g * gScale);
                int nb = (int)Math.Round(b * bScale);
                nr = Math.Min(255, Math.Max(0, nr));
                ng = Math.Min(255, Math.Max(0, ng));
                nb = Math.Min(255, Math.Max(0, nb));
                return ((byte)nr, (byte)ng, (byte)nb);
            });
        }

        // Apply an RGB channel mask: keep channel when enabled, otherwise set to 0.
        public static Bitmap ApplyRgbChannelMask(Bitmap src, bool keepR, bool keepG, bool keepB)
        {
            return ProcessBitmap(src, (b, g, r) =>
            {
                byte nr = keepR ? r : (byte)0;
                byte ng = keepG ? g : (byte)0;
                byte nb = keepB ? b : (byte)0;
                return (nr, ng, nb);
            });
        }

        // Resize with high-quality settings, percentages (100 = same)
        public static Bitmap ResizeBitmap(Bitmap src, int widthPercent, int heightPercent)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (widthPercent <= 0) widthPercent = 1;
            if (heightPercent <= 0) heightPercent = 1;

            int newW = Math.Max(1, src.Width * widthPercent / 100);
            int newH = Math.Max(1, src.Height * heightPercent / 100);

            var dst = new Bitmap(newW, newH, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, 0, 0, newW, newH);
            }
            return dst;
        }

        // HSV adjustments using per-pixel conversion (hue shift in degrees, satScale,valScale multipliers)
        public static Bitmap ApplyHsvAdjustments(Bitmap src, int hueShiftDegrees, double satScale, double valScale)
        {
            // convert hue degrees to range used in our conversion (0..360)
            return ProcessBitmap(src, (b, g, r) =>
            {
                // convert RGB [0..255] to [0..1]
                double rd = r / 255.0;
                double gd = g / 255.0;
                double bd = b / 255.0;

                double max = Math.Max(rd, Math.Max(gd, bd));
                double min = Math.Min(rd, Math.Min(gd, bd));
                double delta = max - min;

                double h = 0;
                if (delta > 0)
                {
                    if (Math.Abs(max - rd) < 1e-9) h = 60 * (((gd - bd) / delta) % 6);
                    else if (Math.Abs(max - gd) < 1e-9) h = 60 * (((bd - rd) / delta) + 2);
                    else h = 60 * (((rd - gd) / delta) + 4);
                }
                if (h < 0) h += 360;

                double s = max == 0 ? 0 : delta / max;
                double v = max;

                // apply adjustments
                h = (h + hueShiftDegrees) % 360;
                if (h < 0) h += 360;
                s = Math.Min(1.0, Math.Max(0.0, s * satScale));
                v = Math.Min(1.0, Math.Max(0.0, v * valScale));

                // HSV -> RGB
                double c = v * s;
                double x = c * (1 - Math.Abs(((h / 60.0) % 2) - 1));
                double m = v - c;

                double rt = 0, gt = 0, bt = 0;
                if (h < 60) { rt = c; gt = x; bt = 0; }
                else if (h < 120) { rt = x; gt = c; bt = 0; }
                else if (h < 180) { rt = 0; gt = c; bt = x; }
                else if (h < 240) { rt = 0; gt = x; bt = c; }
                else if (h < 300) { rt = x; gt = 0; bt = c; }
                else { rt = c; gt = 0; bt = x; }

                byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round((rt + m) * 255)));
                byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round((gt + m) * 255)));
                byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round((bt + m) * 255)));

                return (nr, ng, nb);
            });
        }

        // CMYK adjustments (produce an RGB preview reflecting CMYK modifications)
        // cScale,mScale,yScale,kScale multipliers (1.0 = no change)
        public static Bitmap ApplyCmykAdjustments(Bitmap src, double cScale, double mScale, double yScale, double kScale)
        {
            return ProcessBitmap(src, (b, g, r) =>
            {
                double R = r / 255.0;
                double G = g / 255.0;
                double B = b / 255.0;

                double K = 1 - Math.Max(R, Math.Max(G, B));
                double C = 0, M = 0, Y = 0;
                if (K < 1.0 - 1e-9)
                {
                    C = (1 - R - K) / (1 - K);
                    M = (1 - G - K) / (1 - K);
                    Y = (1 - B - K) / (1 - K);
                }

                C = Math.Min(1.0, Math.Max(0.0, C * cScale));
                M = Math.Min(1.0, Math.Max(0.0, M * mScale));
                Y = Math.Min(1.0, Math.Max(0.0, Y * yScale));
                K = Math.Min(1.0, Math.Max(0.0, K * kScale));

                double rOut = 1 - Math.Min(1.0, C * (1 - K) + K);
                double gOut = 1 - Math.Min(1.0, M * (1 - K) + K);
                double bOut = 1 - Math.Min(1.0, Y * (1 - K) + K);

                byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rOut * 255)));
                byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gOut * 255)));
                byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bOut * 255)));

                return (nr, ng, nb);
            });
        }

        // YCbCr adjustments: per-channel shifts (Y, Cb, Cr)
        public static Bitmap ApplyYcbcrAdjustments(Bitmap src, int yShift, int cbShift, int crShift)
        {
            return ProcessBitmap(src, (b, g, r) =>
            {
                double R = r;
                double G = g;
                double B = b;

                // forward
                double Y = 0.299 * R + 0.587 * G + 0.114 * B;
                double Cb = 128 + (-0.168736 * R - 0.331264 * G + 0.5 * B);
                double Cr = 128 + (0.5 * R - 0.418688 * G - 0.081312 * B);

                Y += yShift;
                Cb += cbShift;
                Cr += crShift;

                // inverse
                double Cb_d = Cb - 128;
                double Cr_d = Cr - 128;

                double rOut = Y + 1.402 * Cr_d;
                double gOut = Y - 0.344136 * Cb_d - 0.714136 * Cr_d;
                double bOut = Y + 1.772 * Cb_d;

                byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rOut)));
                byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gOut)));
                byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bOut)));

                return (nr, ng, nb);
            });
        }

        // YUV adjustments: per-channel shifts (Y, U, V)
        public static Bitmap ApplyYuvAdjustments(Bitmap src, int yShift, int uShift, int vShift)
        {
            return ProcessBitmap(src, (b, g, r) =>
            {
                double R = r;
                double G = g;
                double B = b;

                double Y = 0.299 * R + 0.587 * G + 0.114 * B;
                double U = -0.14713 * R - 0.288862 * G + 0.436 * B + 128;
                double V = 0.615 * R - 0.51498 * G - 0.10001 * B + 128;

                Y += yShift;
                U += uShift;
                V += vShift;

                double U_d = U - 128;
                double V_d = V - 128;

                double rOut = Y + 1.13983 * V_d;
                double gOut = Y - 0.39465 * U_d - 0.58060 * V_d;
                double bOut = Y + 2.03211 * U_d;

                byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rOut)));
                byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gOut)));
                byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bOut)));

                return (nr, ng, nb);
            });
        }

        // LAB adjustments: shift L,a,b channels and convert back (approximate)
        public static Bitmap ApplyLabAdjustments(Bitmap src, int lShift, int aShift, int bShift)
        {
            // helpers
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

            return ProcessBitmap(src, (b, g, r) =>
            {
                // sRGB -> linear
                double R = PivotRgb(r);
                double G = PivotRgb(g);
                double B = PivotRgb(b);

                // linear RGB -> XYZ (D65)
                double X = R * 0.4124564 + G * 0.3575761 + B * 0.1804375;
                double Y = R * 0.2126729 + G * 0.7151522 + B * 0.0721750;
                double Z = R * 0.0193339 + G * 0.1191920 + B * 0.9503041;

                // normalize by reference white D65
                double Xr = 0.95047;
                double Yr = 1.00000;
                double Zr = 1.08883;

                double fx = PivotXyzToLab(X / Xr);
                double fy = PivotXyzToLab(Y / Yr);
                double fz = PivotXyzToLab(Z / Zr);

                double L = Math.Max(0.0, 116.0 * fy - 16.0); // 0..100
                double a = 500.0 * (fx - fy);
                double bLab = 200.0 * (fy - fz);

                // apply shifts
                L = Math.Min(100.0, Math.Max(0.0, L + lShift));
                a = Math.Min(127.0, Math.Max(-128.0, a + aShift));
                bLab = Math.Min(127.0, Math.Max(-128.0, bLab + bShift));

                // Lab -> XYZ
                double fy2 = (L + 16.0) / 116.0;
                double fx2 = fy2 + (a / 500.0);
                double fz2 = fy2 - (bLab / 200.0);

                double xr = InvPivotLab(fx2);
                double yr = InvPivotLab(fy2);
                double zr = InvPivotLab(fz2);

                double X2 = xr * Xr;
                double Y2 = yr * Yr;
                double Z2 = zr * Zr;

                // XYZ -> linear RGB
                double rLin =  3.2406 * X2 - 1.5372 * Y2 - 0.4986 * Z2;
                double gLin = -0.9689 * X2 + 1.8758 * Y2 + 0.0415 * Z2;
                double bLin =  0.0557 * X2 - 0.2040 * Y2 + 1.0570 * Z2;

                // gamma correction
                double rOutD = InvPivotRgb(rLin);
                double gOutD = InvPivotRgb(gLin);
                double bOutD = InvPivotRgb(bLin);

                byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(rOutD * 255.0)));
                byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(gOutD * 255.0)));
                byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bOutD * 255.0)));

                return (nr, ng, nb);
            });
        }

        // Local helper versions to keep this file tidy
        private static double PivotRgbLocal(double v) { v = v / 255.0; return (v <= 0.04045) ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
        private static double InvPivotRgbLocal(double v) { return (v <= 0.0031308) ? 12.92 * v : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055; }
        private static double PivotXyzLocal(double t) { return t > 0.008856 ? Math.Pow(t, 1.0 / 3.0) : (7.787037 * t + 16.0 / 116.0); }
        private static double InvPivotLabLocal(double t) { double t3 = t * t * t; return t3 > 0.008856 ? t3 : (t - 16.0 / 116.0) / 7.787037; }

        // Convenience legacy wrappers (path-based)
        public static Bitmap RgbToCmyk(string path) { using var bmp = new Bitmap(path); return ApplyCmykAdjustments(bmp, 1.0, 1.0, 1.0, 1.0); }
        public static Bitmap RgbToHsv(string path) { using var bmp = new Bitmap(path); return ApplyHsvAdjustments(bmp, 0, 1.0, 1.0); }
        public static Bitmap ConvertToYcbcr(string path) { using var bmp = new Bitmap(path); return ConvertRgbToYcbcrBitmap(bmp); }
        public static Bitmap ConvertToYUV(string path) { using var bmp = new Bitmap(path); return ConvertRgbToYuvBitmap(bmp); }
        public static Bitmap ConvertToLAB(string path) { using var bmp = new Bitmap(path); return ConvertRgbToLabBitmap(bmp); }

        // Visualization helpers (kept)
        public static Bitmap ConvertRgbToYcbcrBitmap(Bitmap src) => ProcessBitmap(src, (b, g, r) =>
        {
            double R = r, G = g, B = b;
            double Y = 0.299 * R + 0.587 * G + 0.114 * B;
            double Cb = 128 + (-0.168736 * R - 0.331264 * G + 0.5 * B);
            double Cr = 128 + (0.5 * R - 0.418688 * G - 0.081312 * B);
            byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(Y)));
            byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(Cb)));
            byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(Cr)));
            return (nr, ng, nb);
        });

        public static Bitmap ConvertRgbToYuvBitmap(Bitmap src) => ProcessBitmap(src, (b, g, r) =>
        {
            double R = r, G = g, B = b;
            double Y = 0.299 * R + 0.587 * G + 0.114 * B;
            double U = -0.14713 * R - 0.288862 * G + 0.436 * B + 128;
            double V = 0.615 * R - 0.51498 * G - 0.10001 * B + 128;
            byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(Y)));
            byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(U)));
            byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(V)));
            return (nr, ng, nb);
        });

        public static Bitmap ConvertRgbToLabBitmap(Bitmap src) => ProcessBitmap(src, (b, g, r) =>
        {
            static double PivotRgb(double v) { v = v / 255.0; return (v <= 0.04045) ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }
            static double PivotXyz(double t) { return t > 0.008856 ? Math.Pow(t, 1.0 / 3.0) : (7.787037 * t + 16.0 / 116.0); }
            double R = PivotRgb(r), G = PivotRgb(g), B = PivotRgb(b);
            double X = R * 0.4124564 + G * 0.3575761 + B * 0.1804375;
            double Y = R * 0.2126729 + G * 0.7151522 + B * 0.0721750;
            double Z = R * 0.0193339 + G * 0.1191920 + B * 0.9503041;
            double Xr = 0.95047, Yr = 1.0, Zr = 1.08883;
            double fx = PivotXyz(X / Xr), fy = PivotXyz(Y / Yr), fz = PivotXyz(Z / Zr);
            double L = Math.Max(0.0, 116.0 * fy - 16.0);
            double a = 500.0 * (fx - fy);
            double bLab = 200.0 * (fy - fz);
            byte nr = (byte)Math.Min(255, Math.Max(0, (int)Math.Round((L / 100.0) * 255.0)));
            byte ng = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(a + 128.0)));
            byte nb = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(bLab + 128.0)));
            return (nr, ng, nb);
        });

        public static Bitmap ReduceColors(Bitmap src, int colorCount)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (colorCount < 1) colorCount = 1;
            if (colorCount > 256) colorCount = 256;

            int colorsPerChannel = (int)Math.Pow(colorCount, 1.0 / 3.0);
            if (colorsPerChannel < 1) colorsPerChannel = 1;

            int step = 256 / colorsPerChannel;

            return ProcessBitmap(src, (b, g, r) =>
            {
                byte nr = (byte)((r / step) * step);
                byte ng = (byte)((g / step) * step);
                byte nb = (byte)((b / step) * step);
                return (nr, ng, nb);
            });
        }
    }
}

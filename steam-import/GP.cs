using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace steam_import
{
    public static class GP
    {
        public static Bitmap Convert(Bitmap bmp, string cat)
        {
            int apercent = bmp.Height / 100 * 30;
            int bpercent = bmp.Width / 100 * 30;
            //bmp = CropBottom(bmp, apercent);
            bmp = CropLeft(bmp, bpercent);

            //decimal aspect = 460 / 215;
            //bmp.Save("1.png", ImageFormat.Png);
            decimal factor = (decimal)bmp.Height / (decimal)215;
            bmp = ResizeImage((Image)bmp, (int)(bmp.Width / factor), 215);
            //bmp.Save("2.png", ImageFormat.Png);

            Size newSize2 = new Size(bmp.Width + (bpercent), bmp.Height);
            bmp = new Bitmap(bmp, newSize2);

            decimal wfactor = (decimal)bmp.Width / (decimal)480;
            Bitmap tmp = new Bitmap(bmp.Width, (int)(215 * wfactor));

            //Random r = new Random();
            //int rnd = r.Next(0, bmp.Height - tmp.Height);
            int rnd = (bmp.Height / 2) - (tmp.Height / 2);

            using (Graphics g = Graphics.FromImage(tmp))
            {
                g.DrawImage(bmp, new Rectangle(0, 0, tmp.Width, tmp.Height),
                                 new Rectangle(0, rnd, tmp.Width, tmp.Height),
                                 GraphicsUnit.Pixel);
            }

            //ZOOMIN
            Size newSize = new Size(480, 215);
            tmp = new Bitmap(tmp, newSize);

            Bitmap blurred = ZoomIn(tmp, 1.5);
            blurred = Convolve(blurred, GaussianBlur(12, 12));

            //CENTER
            Rectangle cloneRect = new Rectangle((blurred.Width - 480) / 2, (blurred.Height - 215) / 2, 480, 215);
            System.Drawing.Imaging.PixelFormat format =
                blurred.PixelFormat;
            Bitmap cloneBitmap = blurred.Clone(cloneRect, format);
            //cloneBitmap.Save("centered.png", ImageFormat.Png);
            //blurred.Save("r.png", ImageFormat.Png);

            using (Graphics g = Graphics.FromImage(cloneBitmap))
            {
                g.DrawImage(bmp, new Rectangle((cloneBitmap.Width / 2) - (bmp.Width / 2), 0, bmp.Width, bmp.Height),
                                 new Rectangle(0, 0, bmp.Width, bmp.Height),
                                 GraphicsUnit.Pixel);
            }
            string overlay_file = null;
            if (cat == "GB") overlay_file = "gb_overlay.png";
            if (cat == "GBC") overlay_file = "gbc_overlay.png";
            if (cat == "GBA") overlay_file = "gba_overlay.png";

            Bitmap oly = (Bitmap)Bitmap.FromFile(overlay_file);

            using (Graphics g = Graphics.FromImage(cloneBitmap))
            {
                g.DrawImage(oly, new Rectangle(0, 0, 480, 215),
                                 new Rectangle(0, 0, 480, 215),
                                 GraphicsUnit.Pixel);
            }



            //int space = 480 - bmp.Width;
            //bmp = AddSides(bmp, space);
            return cloneBitmap;
        }

        public static Bitmap ZoomIn(Bitmap bmp, double factor)
        {
            int width = bmp.Width;
            int height = bmp.Height;

            //ZOOMIN
            Size newSize = new Size((int)(bmp.Width * factor), (int)(bmp.Height * factor));
            bmp = new Bitmap(bmp, newSize);

            ////CENTER
            //Rectangle cloneRect = new Rectangle((bmp.Width - width) / 2, (bmp.Height - height) / 2, width, height);
            //System.Drawing.Imaging.PixelFormat format =
            //    bmp.PixelFormat;
            //Bitmap cloneBitmap = bmp.Clone(cloneRect, format);

            return bmp;
        }

        public static Bitmap CropLeft(Bitmap b, int size)
        {
            Bitmap target = new Bitmap(b.Width - size, b.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(b, new Rectangle(0, 0, target.Width, target.Height),
                                 new Rectangle(size, 0, b.Width - size, b.Height),
                                 GraphicsUnit.Pixel);
            }
            return target;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Bitmap Convolve(Bitmap srcImage, double[,] kernel)
        {
            int width = srcImage.Width;
            int height = srcImage.Height;
            BitmapData srcData = srcImage.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int bytes = srcData.Stride * srcData.Height;
            byte[] buffer = new byte[bytes];
            byte[] result = new byte[bytes];
            Marshal.Copy(srcData.Scan0, buffer, 0, bytes);
            srcImage.UnlockBits(srcData);
            int colorChannels = 3;
            double[] rgb = new double[colorChannels];
            int foff = (kernel.GetLength(0) - 1) / 2;
            int kcenter = 0;
            int kpixel = 0;
            for (int y = foff; y < height - foff; y++)
            {
                for (int x = foff; x < width - foff; x++)
                {
                    for (int c = 0; c < colorChannels; c++)
                    {
                        rgb[c] = 0.0;
                    }
                    kcenter = y * srcData.Stride + x * 4;
                    for (int fy = -foff; fy <= foff; fy++)
                    {
                        for (int fx = -foff; fx <= foff; fx++)
                        {
                            kpixel = kcenter + fy * srcData.Stride + fx * 4;
                            for (int c = 0; c < colorChannels; c++)
                            {
                                rgb[c] += (double)(buffer[kpixel + c]) * kernel[fy + foff, fx + foff];
                            }
                        }
                    }
                    for (int c = 0; c < colorChannels; c++)
                    {
                        if (rgb[c] > 255)
                        {
                            rgb[c] = 255;
                        }
                        else if (rgb[c] < 0)
                        {
                            rgb[c] = 0;
                        }
                    }
                    for (int c = 0; c < colorChannels; c++)
                    {
                        result[kcenter + c] = (byte)rgb[c];
                    }
                    result[kcenter + 3] = 255;
                }
            }
            Bitmap resultImage = new Bitmap(width, height);
            BitmapData resultData = resultImage.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(result, 0, resultData.Scan0, bytes);
            resultImage.UnlockBits(resultData);
            return resultImage;
        }



        public static double[,] GaussianBlur(int lenght, double weight)
        {
            double[,] kernel = new double[lenght, lenght];
            double kernelSum = 0;
            int foff = (lenght - 1) / 2;
            double distance = 0;
            double constant = 1d / (2 * Math.PI * weight * weight);
            for (int y = -foff; y <= foff; y++)
            {
                for (int x = -foff; x <= foff; x++)
                {
                    distance = ((y * y) + (x * x)) / (2 * weight * weight);
                    kernel[y + foff, x + foff] = constant * Math.Exp(-distance);
                    kernelSum += kernel[y + foff, x + foff];
                }
            }
            for (int y = 0; y < lenght; y++)
            {
                for (int x = 0; x < lenght; x++)
                {
                    kernel[y, x] = kernel[y, x] * 1d / kernelSum;
                }
            }
            return kernel;
        }
    }
}

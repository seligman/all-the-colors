using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MergeAnimate
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"..\..\..\bin\Release\anims\";
            var frame = 0;

            Bitmap img1 = (Bitmap)Bitmap.FromFile(path + "final_a.png");
            Bitmap img2 = (Bitmap)Bitmap.FromFile(path + "final_b.png");
            BitmapData data1 = img1.LockBits(new Rectangle(0, 0, img1.Width, img1.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData data2 = img2.LockBits(new Rectangle(0, 0, img1.Width, img1.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] bits1 = new int[data1.Stride / 4 * data1.Height];
            int[] bits2 = new int[data2.Stride / 4 * data2.Height];
            Marshal.Copy(data1.Scan0, bits1, 0, bits1.Length);
            Marshal.Copy(data2.Scan0, bits2, 0, bits2.Length);
            int width = img1.Width;
            int height = img2.Height;
            int stride = data1.Stride;
            int[] bits = new int[data2.Stride / 4 * data2.Height];
            Bitmap img = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            int steps = 30;
            int[,] step = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    step[x, y] = 0;
                }
            }

            while (true)
            {
                var fn = path + "output_newpix_" + frame.ToString("0000") + ".png";
                if (!File.Exists(fn))
                {
                    break;
                }
                Bitmap temp = (Bitmap)Bitmap.FromFile(fn);
                BitmapData dataTemp = temp.LockBits(new Rectangle(0, 0, temp.Width, temp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                int[] bitsTemp = new int[dataTemp.Stride / 4 * dataTemp.Height];
                Marshal.Copy(dataTemp.Scan0, bitsTemp, 0, bitsTemp.Length);
                temp.UnlockBits(dataTemp);
                temp.Dispose();

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (step[x, y] > 0)
                        {
                            step[x, y]--;
                        }
                        if ((bitsTemp[x + y * stride / 4] & 0xFFFFFF) > 0)
                        {
                            step[x, y] = steps;
                        }

                        if (step[x, y] == 0)
                        {
                            bits[x + y * stride / 4] = bits1[x + y * stride / 4];
                            // bits[x + y * stride / 4] = Color.Red.ToArgb();
                        }
                        else if (step[x, y] == steps)
                        {
                            bits[x + y * stride / 4] = bits2[x + y * stride / 4];
                            // bits[x + y * stride / 4] = Color.Blue.ToArgb();
                        }
                        else
                        {
                            var a = Color.FromArgb(bits2[x + y * stride / 4]);
                            var b = Color.FromArgb(bits1[x + y * stride / 4]);
                            double scale = ((double)step[x, y]) / ((double)steps);
                            var c = Color.FromArgb(
                                (int)((((double)a.R) * scale) + (((double)b.R) * (1.0 - scale))),
                                (int)((((double)a.G) * scale) + (((double)b.G) * (1.0 - scale))),
                                (int)((((double)a.B) * scale) + (((double)b.B) * (1.0 - scale)))
                            );
                            //var c = Color.FromArgb(
                            //    (int)((((double)255) * scale) + (((double)0) * (1.0 - scale))),
                            //    (int)((((double)255) * scale) + (((double)0) * (1.0 - scale))),
                            //    (int)((((double)255) * scale) + (((double)0) * (1.0 - scale)))
                            //);
                            bits[x + y * stride / 4] = c.ToArgb();
                        }
                    }
                }

                BitmapData imgData = img.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(bits, 0, imgData.Scan0, bits.Length);
                img.UnlockBits(imgData);
                Console.WriteLine("merged_" + frame.ToString("0000") + ".png");
                img.Save("merged_" + frame.ToString("0000") + ".png");
                frame++;
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Version_2
{
    // A simple 3D point, with a color backing it
    class PointXYZ
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public double Hue { get; private set; }

        // The list of pixels this point represents
        public List<PointXY> Points;

        static void RGBtoOKLab(double r, double g, double b, out double l, out double m, out double s)
        {
            l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
            m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
            s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

            l = Math.Pow(l, 1.0 / 3.0);
            m = Math.Pow(m, 1.0 / 3.0);
            s = Math.Pow(s, 1.0 / 3.0);

            l = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
            m = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
            s = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;

            l = (l + 1.0) / 2.0;
            m = (m + 1.0) / 2.0;
            s = (s + 1.0) / 2.0;
        }

        static void RGBtoRGB(double r, double g, double b, out double r1, out double g1, out double b1)
        {
            r1 = r;
            g1 = g;
            b1 = b;
        }

        delegate void PixelHandlerDelegate(double r, double g, double b, out double r1, out double g1, out double b1);
        static PixelHandlerDelegate s_PixelHandler;

        public static void SetupPixelHandler()
        {
            switch (Settings.COLOR_MODE)
            {
                case "rgb":
                    s_PixelHandler = RGBtoRGB;
                    break;
                case "okl":
                    s_PixelHandler = RGBtoOKLab;
                    break;
                default:
                    throw new Exception("Unknown color mode");
            }
        }

        public PointXYZ(PixelColor rgb)
        {
            s_PixelHandler(rgb.R, rgb.G, rgb.B, out double x, out double y, out double z);
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Hue = rgb.Hue;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is PointXYZ)
            {
                PointXYZ other = (PointXYZ)obj;

                return other.X == X && other.Y == Y && other.Z == Z;
            }
            else
            {
                return false;
            }
        }
    }
}

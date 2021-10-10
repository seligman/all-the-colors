using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Version_2
{
    // A floating point color
    class ColorRGB
    {
        // Each RGB element should be in the range from 0.0 to 1.0
        public double R { get; private set; }
        public double G { get; private set; }
        public double B { get; private set; }

        public double Hue { get; private set; }
        public double Sat { get; private set; }
        public double Val { get; private set; }

        public ColorRGB()
        {
            // Nothing to do
        }

        public ColorRGB(double R, double G, double B)
        {
            this.R = R;
            this.G = G;
            this.B = B;

            double hue = 0;
            double sat = 0;
            double val = 0;

            GetHSV(out hue, out sat, out val);

            this.Hue = hue;
            this.Sat = sat;
            this.Val = val;
        }

        // Turn the color into a standard Color struct
        public Color AsColor()
        {
#pragma warning disable 162
            if (Settings.AVG_GRAY)
            {
                if (Settings.GAMMA)
                {
                    return Color.FromArgb(
                        (int)((Math.Pow((R + 0.15) / 2.0, 1.0 / 2.2) * 255.0) + 0.5),
                        (int)((Math.Pow((G + 0.15) / 2.0, 1.0 / 2.2) * 255.0) + 0.5),
                        (int)((Math.Pow((B + 0.15) / 2.0, 1.0 / 2.2) * 255.0) + 0.5));
                }
                else
                {
                    return Color.FromArgb(
                        (int)(((R + 0.15) / 2.0 * 255.0) + 0.5),
                        (int)(((G + 0.15) / 2.0 * 255.0) + 0.5),
                        (int)(((B + 0.15) / 2.0 * 255.0) + 0.5));
                }
            }
            else
            {
                if (Settings.GAMMA)
                {
                    return Color.FromArgb(
                        (int)((Math.Pow(R, 1.0 / 2.2) * 255.0) + 0.5),
                        (int)((Math.Pow(G, 1.0 / 2.2) * 255.0) + 0.5),
                        (int)((Math.Pow(B, 1.0 / 2.2) * 255.0) + 0.5));
                }
                else
                {
                    return Color.FromArgb(
                        (int)((R * 255.0) + 0.5),
                        (int)((G * 255.0) + 0.5),
                        (int)((B * 255.0) + 0.5));
                }
            }
#pragma warning restore 162
        }

        public double GetPerceivedBrightness()
        {
            return Math.Sqrt(0.299 * (R * R) + 0.587 * (G * G) + 0.114 * (B * B));
        }

        // Get the HSV values for this color
        public void GetHSV(out double hue, out double sat, out double val)
        {
            GetHSV(R, G, B, out hue, out sat, out val);
        }

        public static void GetRGBPastel(double hue, double saturation, double brightness, out double red, out double green, out double blue)
        {
            if (brightness <= 0.0)
            {
                red = 0.0;
                green = 0.0;
                blue = 0.0;
            }
            else
            {
                if (saturation <= 0.0)
                {
                    red = 1.0;
                    green = 1.0;
                    blue = 1.0;
                }
                else
                {
                    red = 0.47450980392156861 + 0.20392156862745098 * Math.Cos(0.017453292519943295 * hue);
                    green = 0.52352941176470591 - 0.0803921568627451 * Math.Cos(0.017453292519943295 * hue);
                    blue = 0.47254901960784312 + 0.19411764705882353 * Math.Cos(0.017453292519943295 * hue + 1.8849555921538759);

                    if (saturation < 1.0)
                    {
                        red = red * saturation + 1.0 - saturation;
                        green = green * saturation + 1.0 - saturation;
                        blue = blue * saturation + 1.0 - saturation;
                    }
                }

                if (brightness < 1.0)
                {
                    red = red * brightness;
                    green = green * brightness;
                    blue = blue * brightness;
                }
            }
        }

        // Helper to turn RGB into HSV values
        public static void GetRGB(double hue, double sat, double val, out double R, out double G, out double B)
        {
            R = 0;
            G = 0;
            B = 0;

            double c = val * sat;
            double x = c * (1.0 - Math.Abs((hue / 60.0) % 2.0 - 1.0));
            double m = val - c;

            switch ((int)(hue / 60))
            {
                case 0:
                    R = c;
                    G = x;
                    break;
                case 1:
                    R = x;
                    G = c;
                    break;
                case 2:
                    G = c;
                    B = x;
                    break;
                case 3:
                    G = x;
                    B = c;
                    break;
                case 4:
                    R = x;
                    B = c;
                    break;
                case 5:
                    R = c;
                    B = x;
                    break;
            }

            R += m;
            G += m;
            B += m;
        }

        // Helper to turn RGB to HSV values
        public static void GetHSV(double R, double G, double B, out double hue, out double sat, out double val)
        {
            hue = 0;
            sat = 0;
            val = 0;

            double minRGB = Math.Min(R, Math.Min(G, B));
            double maxRGB = Math.Max(R, Math.Max(G, B));

            if (minRGB == maxRGB)
            {
                val = minRGB;
            }
            else
            {
                double d = (R == minRGB) ? G - B : ((B == minRGB) ? R - G : B - R);
                double h = (R == minRGB) ? 3 : ((B == minRGB) ? 1 : 5);
                hue = 60.0 * (h - d / (maxRGB - minRGB));
                sat = (maxRGB - minRGB) / maxRGB;
                val = maxRGB;
            }
        }
    }
}

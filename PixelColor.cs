using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Version_2
{
    // The final pixel to display
    class PixelColor
    {
        // The full and complete color for this pixel
        public double R { get; private set; }
        public double G { get; private set; }
        public double B { get; private set; }

        public double Hue { get; private set; }

        // The Rgba32 color
        public int Color { get; private set; }

        public PixelColor(ColorRGB from)
        {
            this.R = from.R;
            this.G = from.G;
            this.B = from.B;
            this.Hue = from.Hue;
            this.Color = from.AsColor().ToArgb();
        }

        public PixelColor(Color from)
        {
            this.Color = from.ToArgb();
        }

        public void SetIntColor(ColorRGB source)
        {
            this.Color = source.AsColor().ToArgb();
        }
    }
}

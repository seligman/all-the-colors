using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Version_2
{
    // A simple 2d point
    struct PointXY
    {
        public int X;
        public int Y;

        public void Set(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public PointXY(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is PointXY)
            {
                var other = (PointXY)obj;

                return this.X == other.X && this.Y == other.Y;
            }
            else
            {
                return false;
            }
        }
    }
}

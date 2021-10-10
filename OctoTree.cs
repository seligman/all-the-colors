using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Version_2
{
    // A three dimension b tree to store points in
    class OctoTree
    {
        // The bounds of this box
        public double X1 { get; private set; }
        public double Y1 { get; private set; }
        public double Z1 { get; private set; }

        public double X2 { get; private set; }
        public double Y2 { get; private set; }
        public double Z2 { get; private set; }

        // Is this box a leaf containing a point?
        public bool Leaf { get; private set; }

        // The data for the point if Leaf is true
        public PointXYZ Point { get; private set; }

        // The child nodes, will contain 8 boxes
        public OctoTree[] Nodes { get; private set; }

        // The total count of points, only valid for the base node
        public int Count { get; private set; }

        // Construct a new box
        public OctoTree(double X1, double Y1, double Z1, double X2, double Y2, double Z2)
        {
            this.Leaf = true;
            this.Point = null;
            this.Nodes = null;
            this.X1 = X1;
            this.Y1 = Y1;
            this.Z1 = Z1;
            this.X2 = X2;
            this.Y2 = Y2;
            this.Z2 = Z2;
            this.Count = 0;
        }

        // Return the index for a given point inside this quad
        public int PointIndex(double x, double y, double z)
        {
            return
                ((2 * x > X1 + X2) ? 1 : 0) +
                ((2 * y > Y1 + Y2) ? 2 : 0) +
                ((2 * z > Z1 + Z2) ? 4 : 0);
        }

        // Remove a point from wherever it is in the tree, PointXYZ must be in the tree
        public void RemovePoint(PointXYZ pt)
        {
            LinkedList<OctoTree> stack = new LinkedList<OctoTree>();

            Count--;

            // Create a stack of quads down to the target point
            OctoTree cur = this;
            stack.AddLast(cur);
            while (!cur.Leaf)
            {
                cur = cur.Nodes[cur.PointIndex(pt.X, pt.Y, pt.Z)];
                stack.AddLast(cur);

                if (cur.Leaf)
                {
                    cur.Point = null;
                }
            }

            while (stack.Count > 2)
            {
                stack.RemoveLast();

                int leaves = 0;
                PointXYZ upward = null;
                int count = 0;

                // Count how many leaves are on this level
                for (int i = 0; i < 8; i++)
                {
                    if (stack.Last.Value.Nodes[i].Leaf)
                    {
                        leaves++;
                        if (stack.Last.Value.Nodes[i].Point != null)
                        {
                            upward = stack.Last.Value.Nodes[i].Point;
                            count++;
                        }
                    }
                }

                if (leaves == 8 && count == 0)
                {
                    // This means this quad is nothing but empty leaves, go ahead and turn it into it's own leaf
                    stack.Last.Value.Nodes = null;
                    stack.Last.Value.Leaf = true;
                }
                else if (leaves == 8 && count == 1)
                {
                    // This means this quad is nothing but leaves with one of them containing a point, 
                    // trim it to it's own leaf, carrying the point upwards
                    stack.Last.Value.Nodes = null;
                    stack.Last.Value.Leaf = true;
                    stack.Last.Value.Point = upward;
                }
                else
                {
                    // This quad contains less than all leaves, or two or more points, it's irreducibly complex
                    break;
                }
            }
        }

        // Add a new point to the tree
        public void AddPoint(PointXYZ pt)
        {
            OctoTree cur = this;

            Count++;

            while (true)
            {
                // Find the best leaf for this node
                while (!cur.Leaf)
                {
                    cur = cur.Nodes[cur.PointIndex(pt.X, pt.Y, pt.Z)];
                }

                // Turn the quad into a bunch of leaves
                cur.Nodes = new OctoTree[8];

                cur.Nodes[0] = new OctoTree(cur.X1, cur.Y1, cur.Z1, (cur.X1 + cur.X2) / 2, (cur.Y1 + cur.Y2) / 2, (cur.Z1 + cur.Z2) / 2);
                cur.Nodes[1] = new OctoTree((cur.X1 + cur.X2) / 2, cur.Y1, cur.Z1, cur.X2, (cur.Y1 + cur.Y2) / 2, (cur.Z1 + cur.Z2) / 2);
                cur.Nodes[2] = new OctoTree(cur.X1, (cur.Y1 + cur.Y2) / 2, cur.Z1, (cur.X1 + cur.X2) / 2, cur.Y2, (cur.Z1 + cur.Z2) / 2);
                cur.Nodes[3] = new OctoTree((cur.X1 + cur.X2) / 2, (cur.Y1 + cur.Y2) / 2, cur.Z1, cur.X2, cur.Y2, (cur.Z1 + cur.Z2) / 2);

                cur.Nodes[4] = new OctoTree(cur.X1, cur.Y1, (cur.Z1 + cur.Z2) / 2, (cur.X1 + cur.X2) / 2, (cur.Y1 + cur.Y2) / 2, cur.Z2);
                cur.Nodes[5] = new OctoTree((cur.X1 + cur.X2) / 2, cur.Y1, (cur.Z1 + cur.Z2) / 2, cur.X2, (cur.Y1 + cur.Y2) / 2, cur.Z2);
                cur.Nodes[6] = new OctoTree(cur.X1, (cur.Y1 + cur.Y2) / 2, (cur.Z1 + cur.Z2) / 2, (cur.X1 + cur.X2) / 2, cur.Y2, cur.Z2);
                cur.Nodes[7] = new OctoTree((cur.X1 + cur.X2) / 2, (cur.Y1 + cur.Y2) / 2, (cur.Z1 + cur.Z2) / 2, cur.X2, cur.Y2, cur.Z2);

                if (cur.Point != null)
                {
                    // Need to carry a point down from this quad to the proper child quad
                    cur.Nodes[cur.PointIndex(cur.Point.X, cur.Point.Y, cur.Point.Z)].Point = cur.Point;
                }

                // This quad is no longer a leaf
                cur.Point = null;
                cur.Leaf = false;

                int index = cur.PointIndex(pt.X, pt.Y, pt.Z);

                if (cur.Nodes[index].Point == null)
                {
                    // This leaf is empty, place the point there
                    cur.Nodes[index].Point = pt;
                    cur.Nodes[index].Leaf = true;

                    break;
                }
            }
        }

        // Find the nearest point to a target point.  The target point need not exist
        public PointXYZ NearestPoint(double x, double y, double z, ref double bestDist)
        {
            PointXYZ bestPoint = null;
            bestDist = (X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1) + (Z2 - Z1) * (Z2 - Z1);

            NearestMatch(x, y, z, ref bestPoint, ref bestDist, this);

            return bestPoint;
        }

        // Find the nearest point to a target point.  The target point need not exist
        public PointXYZ NearestPoint(double x, double y, double z)
        {
            PointXYZ bestPoint = null;
            double bestDist = 0;
            bestDist = (X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1) + (Z2 - Z1) * (Z2 - Z1);

            NearestMatch(x, y, z, ref bestPoint, ref bestDist, this);

            return bestPoint;
        }

        // Internal data object used by nearest match.  This only exists
        // to prevent pressure on the heap, stored in TLS to make NearestMatch 
        // and NearestPoint thread safe for pure readers

        static ThreadLocal<Queue<OctoTree>> m_queue = new ThreadLocal<Queue<OctoTree>>();

        // Internal helper to find the nearest point
        static void NearestMatch(double x, double y, double z, ref PointXYZ bestPoint, ref double bestDist, OctoTree node)
        {
            // Pull the queue object out of TLS, or create it the first time
            Queue<OctoTree> queue;
            if (!m_queue.IsValueCreated)
            {
                queue = new Queue<OctoTree>();
                m_queue.Value = queue;
            }
            else
            {
                queue = m_queue.Value;
            }

            // A queue of boxes to search, starts off with the base node
            queue.Enqueue(node);
            int left = 1;

            // Keep searching till we run out of nodes
            while (left > 0)
            {
                node = queue.Dequeue();
                left--;

                // Exclude the node if the point is further away than the current best distance
                // in any axis
                if (x < node.X1 - bestDist || x > node.X2 + bestDist ||
                    y < node.Y1 - bestDist || y > node.Y2 + bestDist ||
                    z < node.Z1 - bestDist || z > node.Z2 + bestDist)
                {
                    continue;
                }

                // If there is a point in this node, test it
                if (node.Point != null)
                {
                    double dx = node.Point.X - x;
                    double dy = node.Point.Y - y;
                    double dz = node.Point.Z - z;

                    double d = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (d < bestDist)
                    {
                        // This is better than our current distance, so call it the best match
                        bestDist = d;
                        bestPoint = node.Point;
                    }
                }

                // And check all children nodes
                if (node.Nodes != null)
                {
                    left += 8;

                    // Start at the most likely node
                    int start = node.PointIndex(x, y, z);
                    for (int i = 0; i < 8; i++)
                    {
                        queue.Enqueue(node.Nodes[(start + i) % 8]);
                    }
                }
            }
        }
    }
}

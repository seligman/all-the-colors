using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace Version_2
{
    class Worker
    {
        // Delegate that's called to save the current state.  Final is true
        // for the last call, and extra contains frame or checkpoint information
        public delegate void CheckPointDel(Bitmap bitmap, bool final, string extra, bool ui);
        public static CheckPointDel CheckPoint = null;

        // Delegate that's called often with a string to present to the user
        public delegate void MessageDel(string value);
        public static MessageDel Message = null;

        // Bool that's true to keep the worker thread running
        public static bool Working = true;

        static int Width = 0;
        static int Height = 0;

        // Return all neighbors for a given pixel, assuming a square layout
        static void GetNeighborsSquare(PointXY xy, PointXY[] list, ref int count)
        {
            count = 0;

            // This is a fairly inner loop function, so unroll the loop here and return the
            // neighbors directly.

            // These values are pulled out to prevent accessors from being called more than
            // once
            int x = xy.X;
            int y = xy.Y;

            // These checks are needed more than once in this function, so only do the check
            // itself once
            bool YGreater = y > 0;
            bool YLess = y < Height - 1;

            if (x > 0)
            {
                if (YGreater)
                {
                    list[count].Set(x - 1, y - 1);
                    count++;
                }

                list[count].Set(x - 1, y);
                count++;

                if (YLess)
                {
                    list[count].Set(x - 1, y + 1);
                    count++;
                }
            }

            if (x < Width - 1)
            {
                if (YGreater)
                {
                    list[count].Set(x + 1, y - 1);
                    count++;
                }

                list[count].Set(x + 1, y);
                count++;

                if (YLess)
                {
                    list[count].Set(x + 1, y + 1);
                    count++;
                }
            }

            if (YGreater)
            {
                list[count].Set(x, y - 1);
                count++;
            }

            if (YLess)
            {
                list[count].Set(x, y + 1);
                count++;
            }
        }

        // Return all neighbors for a given pixel, assuming a hexagon layout
        static void GetNeighborsHexagon(PointXY xy, PointXY[] list, ref int count)
        {
            count = 0;

            // This is a fairly inner loop function, so unroll the loop here and return the
            // neighbors directly.

            // These values are pulled out to prevent accessors from being called more than
            // once
            int x = xy.X;
            int y = xy.Y;

            // These checks are needed more than once in this function, so only do the check
            // itself once
            bool YGreater = y > 0;
            bool YLess = y < Height - 1;

            if (x > 0)
            {
                if (YGreater)
                {
                    list[count].Set(x - 1, y - 1);
                    count++;
                }

                list[count].Set(x - 1, y);
                count++;

                if (YLess)
                {
                    list[count].Set(x - 1, y + 1);
                    count++;
                }
            }

            if (x < Width - 1)
            {
                if (YGreater)
                {
                    list[count].Set(x + 1, y - 1);
                    count++;
                }

                list[count].Set(x + 1, y);
                count++;

                if (YLess)
                {
                    list[count].Set(x + 1, y + 1);
                    count++;
                }
            }

            if (YGreater)
            {
                list[count].Set(x, y - 1);
                count++;
            }

            if (YLess)
            {
                list[count].Set(x, y + 1);
                count++;
            }
        }

        // Create enough colors for each pixel of display, returns a list
        // sorted by hue
        public static List<ColorRGB> SetupColors()
        {
            SortedDictionary<double, List<ColorRGB>> colors = new SortedDictionary<double, List<ColorRGB>>();
            int num = (int)(Math.Pow(Width * Height, 1.0 / 3.0) + 2.0);
            int count = 0;

            DateTime nextMessage = DateTime.Now.AddMilliseconds(200);

            if (Settings.DARKER_IMAGE)
            {
#pragma warning disable 162
                num = (int)(num * 1.5);
#pragma warning restore 162
            }

            // Pick the colors by RGB
            for (int r = 0; r < num && Working; r++)
            {
                if (DateTime.Now >= nextMessage)
                {
                    nextMessage = DateTime.Now.AddMilliseconds(200);
                    Message(string.Format("Colors, Pass 1/3: {0:P}", ((double)r) / ((double)num)));
                }

                for (int g = 0; g < num; g++)
                {
                    for (int b = 0; b < num; b++)
                    {
                        ColorRGB rgb = new ColorRGB(
                            ((double)r) / ((double)(num - 1)),
                            ((double)g) / ((double)(num - 1)),
                            ((double)b) / ((double)(num - 1)));

                        // Don't use colors that are almost completly desaturated
#pragma warning disable 429
                        if ((Settings.DARKER_IMAGE && (rgb.Sat > 0.05 && rgb.R >= 0.2 && rgb.G >= 0.2 && rgb.B >= 0.2)) ||
                           (!Settings.DARKER_IMAGE && (rgb.Sat > 0.05)))
#pragma warning restore 429
                        {
                            // Rotate the hue a bit so we start with blue
                            double shiftedHue = rgb.Hue + 180.0;

                            if (shiftedHue > 360.0)
                            {
                                shiftedHue -= 360.0;
                            }

                            // The array is a list of hues with the matching colors
                            if (!colors.ContainsKey(shiftedHue))
                            {
                                colors.Add(shiftedHue, new List<ColorRGB>());
                            }
                            colors[shiftedHue].Add(rgb);
                            count++;
                        }
                    }
                }
            }

            if (!Working)
            {
                return null;
            }

            // Pick a list of indexes to trim so we end up with just the number of colors we need
            int left = count;
            HashSet<int> toRemove = new HashSet<int>();
            Random rnd = new Random(42);

            if (left < Width * Height)
            {
                // This is bad
                throw new Exception("Not enough colors!");
            }

            int curRemove = 0;
            int totalRemove = left - (Width * Height);

            while (Width * Height < left)
            {
                if (DateTime.Now >= nextMessage)
                {
                    nextMessage = DateTime.Now.AddMilliseconds(200);
                    Message(string.Format("Colors, Pass 2/3: {0:P}", ((double)curRemove) / ((double)totalRemove)));
                }

                // Just build a list of indexes to remove
                int temp = rnd.Next(0, count);
                if (!toRemove.Contains(temp))
                {
                    toRemove.Add(temp);
                    left--;
                    curRemove++;
                }
            }

            if (!Working)
            {
                return null;
            }

            // We now have a list of lists, and a list of indexes to not add to the final list,
            // so construct that final list
            List<ColorRGB> returnList = new List<ColorRGB>();
            int index = 0;
            foreach (var list in colors.Values)
            {
                // Before working on each sub-list, go ahead and shuffle it up
                List<Tuple<double, ColorRGB>> random = new List<Tuple<double, ColorRGB>>();
                foreach (var cur in list)
                {
                    random.Add(new Tuple<double, ColorRGB>(rnd.NextDouble(), cur));
                }

                random.Sort((a, b) =>
                {
                    return a.Item1.CompareTo(b.Item1);
                });

                // Now, for each item in this shuffled sub-list, add the color to the
                // final list to return, but only if it's not one of the indexes we 
                // just decided to toss out
                foreach (var cur in random)
                {
                    if (DateTime.Now >= nextMessage)
                    {
                        nextMessage = DateTime.Now.AddMilliseconds(200);
                        Message(string.Format("Colors, Pass 3/3: {0:P}", ((double)index) / ((double)count)));
                    }

                    if (!toRemove.Contains(index))
                    {
                        returnList.Add(cur.Item2);
                    }
                    index++;
                }
            }

            return returnList;
        }

        // Construct the list of pixels
        public static object[,] CreateArray()
        {
            // Null represents an empty pixel, so this is simple

            // Note: This list is Height x Width to make it slightly
            // quicker when dumping to a bitmap

            return new object[Height, Width];
        }

        // Create a list of checkpoints
        static Queue<int> CreateCheckpoints(int count, int max, bool frames)
        {
            Queue<int> ret = new Queue<int>();

            // Use Int64 to prevent an int overflow during the math
            for (Int64 i = 0; i <= max; i++)
            {
                if (frames && i == 0)
                {
                    for (int j = 0; j < Settings.INITIAL_FRAMES; j++)
                    {
                        ret.Enqueue((int)(i * count / max));
                    }
                }
                else if (frames && i == max)
                {
                    for (int j = 0; j < Settings.FINAL_FRAMES; j++)
                    {
                        ret.Enqueue((int)(i * count / max));
                    }
                }
                else
                {
                    ret.Enqueue((int)(i * count / max));
                }
            }

            ret.Enqueue(int.MaxValue);

            return ret;
        }

        // Construct a checkpoint image and send it off
        static void RunCheckPoint(object[,] pixels, bool final, string extra, bool ui)
        {
            // Create a bitmap and get the bitmap bits
            Bitmap image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            BitmapData data = image.LockBits(new Rectangle(0, 0, Width, Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int stride = data.Stride / 4;

            int[] bits = new int[stride * data.Height];

            // Setup the background color values
            int background1 = 0;
            int background2 = 0;

#pragma warning disable 162
            if (Settings.CHECKERED_BACKGROUND)
            {
                background1 = Color.FromArgb(204, 204, 204).ToArgb();
                background2 = Color.FromArgb(255, 255, 255).ToArgb();
            }
            else
            {
                background1 = Color.FromArgb(0, 0, 0).ToArgb();
                background2 = Color.FromArgb(0, 0, 0).ToArgb();
            }
#pragma warning restore 162

            // Use red to highlight available pixels
            int avail = Color.FromArgb(192, 0, 0).ToArgb();

            // See how much to scale the checkered background
            int scale = 1;
            int width = Width;
            int height = Height;

            while (width > 1280 || height > 720)
            {
                width /= 2;
                height /= 2;
                scale *= 2;
            }

            // Run through every pixel
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (pixels[y, x] == null)
                    {
                        // A truly empty pixel, either mark it black or checkered
                        // depending on settings
                        if ((x % (16 * scale) < (8 * scale)) ^ (y % (16 * scale) < (8 * scale)))
                        {
                            bits[x + y * stride] = background1;
                        }
                        else
                        {
                            bits[x + y * stride] = background2;
                        }
                    }
                    else if (pixels[y, x] is PixelEmpty)
                    {
                        bits[x + y * stride] = avail;
                    }
                    else
                    {
                        bits[x + y * stride] = ((PixelColor)pixels[y, x]).Color;
                    }
                }
            }

            // Copy our bits into the BitmapData structure
            Marshal.Copy(bits, 0, data.Scan0, bits.Length);
            image.UnlockBits(data);

            // Call the delegate to do something with the bitmap
            CheckPoint(image, final, extra, ui);
        }

        // The main worker of All The Colors
        public static void MainWorker()
        {
            // Name this thread so I can find it in the debugger
            Thread.CurrentThread.Name = "All The Colors";

            // Store these values locally to make lookup faster
            Width = Settings.SIZE.Width;
            Height = Settings.SIZE.Height;

            // Setup the color conversion logic
            PointXYZ.SetupPixelHandler();

            // Use a hard coded seed for the random number generator so I 
            // can recreate images
            Random rnd = new Random(42);

            // Setup the color data
            Message("Setting up colors");
            List<ColorRGB> colors = SetupColors();
            if (!Working)
            {
                return;
            }

            // Create the array of pixels
            Message("Creating final array");
            object[,] pixels = CreateArray();
            if (!Working)
            {
                return;
            }

            // Create the array of pixels
            Message("Creating new array");
            object[,] pixelsNew = null;
            if (Settings.SAVE_SET_PIXELS)
            {
                pixelsNew = CreateArray();
            }
            if (!Working)
            {
                return;
            }

            // Make the list of available pixels, this is initially empty
            Message("Make tree of nodes to check");
            OctoTree available = new OctoTree(0, 0, 0, 1, 1, 1);
            if (!Working)
            {
                return;
            }

            // Create the various states to checkpoint data
            Message("Setting up checkpoints");
            Queue<int> checkpoints = CreateCheckpoints(colors.Count, 100, false);
            Queue<int> saves = CreateCheckpoints(colors.Count, 10, false);
            Queue<int> frames = null;
            int frameNumber = 0;
            if (Settings.SAVE_FRAMES)
            {
#pragma warning disable 162
                frames = CreateCheckpoints(colors.Count, Settings.FRAME_COUNT, true);
#pragma warning restore 162
            }

            // A list of neighbors, create the array here to 
            // prevent us from putting too much pressure on the heap
            PointXY[] neighbors = new PointXY[9];
            // Next time we display info to the user
            DateTime nextInfo = DateTime.Now.AddSeconds(1);
            // What time did we start work?
            DateTime start = DateTime.Now;
            // Time of next checkpoint for passes > 0
            DateTime nextCheckPoint = DateTime.Now.AddSeconds(30);
            // How many pixels have we drawn?
            int drawn = 0;

            // And, finally, start the work
            Message("Begin work");
            for (int pass = 0; Working; pass++)
            {
                // Multiple passes.  First pass is the main one, the remaining passes
                // fill in any holes that are left
                double maxHue = 5;
                double maxRGB = 1.0 / ((double)(Width * Height)) * 10000.0;

                // The index currently in the the color array
                int i = -1;

                // Loop through all the colors we want to place
                foreach (var color in colors)
                {
                    i++;

                    if (DateTime.Now >= nextInfo)
                    {
                        nextInfo = DateTime.Now.AddSeconds(1);
                        Message(string.Format("Pass {3} : {0:p} : queue {1:#,##0} : time {2:%h\\:mm\\:ss}",
                            (double)i / Width / Height,
                            available.Count,
                            (DateTime.Now - start),
                            pass));
                    }

                    PointXY best;
                    double dist = 0;
                    PointXYZ match = null;

                    if (available.Count == 0)
                    {
                        // Just use the starting point
                        best = new PointXY(Settings.START.X, Settings.START.Y);
                    }
                    else
                    {
                        // Find the "nearest" color in the RGB space from the target color
                        // to the list of available colors
                        // Filter this color through PointXYZ to force it to convert to the
                        // right colorspace, if that needs to happen
                        var filtered = new PointXYZ(new PixelColor(color));
                        match = available.NearestPoint(filtered.X, filtered.Y, filtered.Z, ref dist);
                        int index = rnd.Next(0, match.Points.Count);
                        best = match.Points[index];
                    }

                    bool skip = false;
                    if (dist > maxRGB)
                    {
                        // This color appears to be far away, is it really according to the hue?
                        double diff = color.Hue - match.Hue;

                        if (diff < 0.0)
                        {
                            diff *= -1f;
                        }

                        if (diff > 180.0)
                        {
                            diff = 360.0 - diff;
                        }

                        if (diff > maxHue)
                        {
                            // Yep, it's really far away, just skip it
                            skip = true;
                        }
                    }

                    if (!skip)
                    {
                        // Put color in the pixel array
                        drawn++;
                        PixelEmpty toRemove = (PixelEmpty)pixels[best.Y, best.X];
                        PixelColor newPixel = new PixelColor(color);

                        if (Settings.PASTEL)
                        {
#pragma warning disable 162
                            double h;
                            double s;
                            double v;

                            ColorRGB.GetHSV(newPixel.R, newPixel.G, newPixel.B, out h, out s, out v);

                            double r;
                            double g;
                            double b;

                            ColorRGB.GetRGBPastel(h, s, v, out r, out g, out b);
                            ColorRGB pastel = new ColorRGB(r, g, b);

                            newPixel.SetIntColor(pastel);
#pragma warning restore 162
                        }

                        pixels[best.Y, best.X] = newPixel;
                        if (pixelsNew != null)
                        {
                            pixelsNew[best.Y, best.X] = new PixelColor(Color.White);
                        }

                        if (Settings.DARKER_IMAGE)
                        {
#pragma warning disable 162
                            ColorRGB darker = new ColorRGB(
                                color.R * .9 + .5 * .1,
                                color.G * .9 + .5 * .1,
                                color.B * .9 + .5 * .1);

                            ((PixelColor)pixels[best.Y, best.X]).SetIntColor(darker);
#pragma warning restore 162
                        }

                        // Remove the match from all the empties
                        if (toRemove != null)
                        {
                            int max = toRemove.BackRefs.Count;

                            for (int j = 0; j < max; j++)
                            {
                                PointXYZ temp = toRemove.BackRefs[j];
                                temp.Points.Remove(best);

                                if (temp.Points.Count == 0)
                                {
                                    // This pixel is no longer available to be used at
                                    // all, remove it from the available array
                                    available.RemovePoint(temp);
                                }
                            }
                        }

                        // Now mark the surronding pixels as available
                        PointXYZ newColor = null;
                        int count = 0;

#pragma warning disable 162
                        if (Settings.HEXAGON_LAYOUT)
                        {
                            GetNeighborsHexagon(best, neighbors, ref count);
                        }
                        else
                        {
                            GetNeighborsSquare(best, neighbors, ref count);
                        }
#pragma warning restore 162

                        for (int j = 0; j < count; j++)
                        {
                            int x = neighbors[j].X;
                            int y = neighbors[j].Y;
                            if (pixels[y, x] == null || pixels[y, x] is PixelEmpty)
                            {
                                if (newColor == null)
                                {
                                    // Get this color from the array, or if it's not in the array
                                    // already, add it
                                    newColor = GetPoint(available, (PixelColor)pixels[best.Y, best.X]);
                                }

                                if (pixels[y, x] == null)
                                {
                                    // This pixel is empty, turn it into an available pixel
                                    pixels[y, x] = new PixelEmpty();
                                }

                                // Add the color to the list of possible colors for this empty pixel
                                ((PixelEmpty)pixels[y, x]).BackRefs.Add(newColor);

                                // Add the available neighbors to this color
                                newColor.Points.Add(neighbors[j]);
                            }
                        }

                        if (Settings.SAVE_FRAMES)
                        {
#pragma warning disable 162
                            // See if this is a frame for the animation, if so, save it
                            while (drawn >= frames.Peek())
                            {
                                frames.Dequeue();

                                RunCheckPoint(pixels, false, "frame_" + frameNumber.ToString("0000"), true);
                                if (pixelsNew != null)
                                {
                                    RunCheckPoint(pixelsNew, false, "newpix_" + frameNumber.ToString("0000"), false);
                                    for (int y = 0; y < Height; y++)
                                    {
                                        for (int x = 0; x < Width; x++)
                                        {
                                            pixelsNew[y, x] = null;
                                        }
                                    }
                                }

                                frameNumber++;
                            }
#pragma warning restore 162
                        }

                        // Check to see if it's time for a checkpoint image
                        if (pass == 0)
                        {
                            if (i >= checkpoints.Peek())
                            {
                                // Time for a normal check point
                                checkpoints.Dequeue();

                                RunCheckPoint(pixels, false, null, true);
                            }

                            if (i >= saves.Peek())
                            {
                                // Time for a save of progress
                                saves.Dequeue();

                                RunCheckPoint(pixels, false, "save_" + (11 - saves.Count).ToString("00"), true);
                            }
                        }
                        else
                        {
                            // After the first pass, just save a check point every
                            // two minutes
                            if (DateTime.Now >= nextCheckPoint)
                            {
                                nextCheckPoint = DateTime.Now.AddMinutes(2);

                                RunCheckPoint(pixels, false, null, true);
                            }
                        }
                    }

                    if (available.Count == 0 || !Working)
                    {
                        // Woo hoo!  We either finished or were told to give up
                        break;
                    }
                }

                if (available.Count == 0 || !Working)
                {
                    // Woo hoo!  We either finished or were told to give up
                    break;
                }

                // Save a check point for this pass
                RunCheckPoint(pixels, false, "pass_" + pass.ToString("00"), true);
            }

            if (Working)
            {
                // And the final check point image
                RunCheckPoint(pixels, true, null, true);

                Message(string.Format("All done. Duration: {0:%h\\:mm\\:ss}.", DateTime.Now - start));

                // All done, so set this to false
                Working = false;
            }
        }


        // Get a known point from the array, if it's not in the array already
        // add it.
        static PointXYZ GetPoint(OctoTree tree, PixelColor color)
        {
            PointXYZ pt = new PointXYZ(color);
            PointXYZ best = tree.NearestPoint(pt.X, pt.Y, pt.Z);

            if (best != null && best.X == pt.X && best.Y == pt.Y && best.Z == pt.Z)
            {
                return best;
            }
            else
            {
                pt.Points = new List<PointXY>();
                tree.AddPoint(pt);
                return pt;
            }
        }
    }
}

using ScottsUtils.Equation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Version_2
{
    // Some settings that control the final display
    class Settings
    {
        // The size of the image to create
        public static Size SIZE = new Size(250 * 2, 250 * 2);

        // Start position for the first pixel
        public static Point START = new Point(SIZE.Width / 3, SIZE.Height / 3);

        // Used for animation
        public static int FRAME_COUNT = 1800;
        public static int INITIAL_FRAMES = 6;
        public static int FINAL_FRAMES = 60;

        // Save frames to create an animation
        public static bool SAVE_FRAMES = false;
        // Empty pixels have a checkered background
        public static bool CHECKERED_BACKGROUND = true;
        // Small preview for development (changes width and height)
        public static bool SMALL_PREVIEW = false;
        // Pick colors that lead to a slightly darker image
        public static bool DARKER_IMAGE = false;
        // Color mode to find near colors
        public static string COLOR_MODE = "rgb";
        // Use pastel colors for the final image
        public static bool PASTEL = true;
        // Use gamma correction for a smoother color gradient
        public static bool GAMMA = false;
        // Average colors to gray a bit
        public static bool AVG_GRAY = false;
        // Save a smaller version of the PNG file as well
        public static bool SAVE_SMALLER = true;
        // Layout the pixels in a hexagon grid
        public static bool HEXAGON_LAYOUT = true;
        // Save the set pixels during animation
        public static bool SAVE_SET_PIXELS = false;

        // Dummy value to force Setup() to run
        public static bool IS_SETUP = Setup();

        public static bool Setup()
        {
            // Shortcut to change the size at runtime
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                if (File.Exists(Environment.GetCommandLineArgs()[1]))
                {
                    try
                    {
                        LoadFromFile(Environment.GetCommandLineArgs()[1]);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Error parsing file: " + e.Message);
                    }
                }
                else
                {
                    SaveToFile(Environment.GetCommandLineArgs()[1]);
                    MessageBox.Show("Sample data file created");
                }
            }

            START = new Point(SIZE.Width / 3, SIZE.Height / 3);
            return true;
        }


        static void SaveToFile(string filename)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# The size of the image to create");
            sb.AppendLine("SIZE_WIDTH = " + SIZE.Width);
            sb.AppendLine("SIZE_HEIGHT = " + SIZE.Height);
            sb.AppendLine();

            sb.AppendLine("# Used for animation");
            sb.AppendLine("FRAME_COUNT = " + FRAME_COUNT);
            sb.AppendLine("INITIAL_FRAMES = " + INITIAL_FRAMES);
            sb.AppendLine("FINAL_FRAMES = " + FINAL_FRAMES);
            sb.AppendLine();

            sb.AppendLine("# Save frames to create an animation");
            sb.AppendLine("SAVE_FRAMES = " + (SAVE_FRAMES ? "true" : "false"));
            sb.AppendLine("# Empty pixels have a checkered background");
            sb.AppendLine("CHECKERED_BACKGROUND = " + (CHECKERED_BACKGROUND ? "true" : "false"));
            sb.AppendLine("# Small preview for development (changes width and height)");
            sb.AppendLine("SMALL_PREVIEW = " + (SMALL_PREVIEW ? "true" : "false"));
            sb.AppendLine("# Pick colors that lead to a slightly darker image");
            sb.AppendLine("DARKER_IMAGE = " + (DARKER_IMAGE ? "true" : "false"));
            sb.AppendLine("# Color mode to use to pick near colors, 'rgb' or 'okl'");
            sb.AppendLine("COLOR_MODE = " + COLOR_MODE);
            sb.AppendLine("# Use pastel colors for the final image");
            sb.AppendLine("PASTEL = " + (PASTEL ? "true" : "false"));
            sb.AppendLine("# Use gamma correction for a smoother color gradient");
            sb.AppendLine("GAMMA = " + (GAMMA ? "true" : "false"));
            sb.AppendLine("# Average colors to gray a bit");
            sb.AppendLine("AVG_GRAY = " + (AVG_GRAY ? "true" : "false"));
            sb.AppendLine("# Save a smaller version of the PNG file as well");
            sb.AppendLine("SAVE_SMALLER = " + (SAVE_SMALLER ? "true" : "false"));
            sb.AppendLine("# Layout the pixels in a hexagon grid");
            sb.AppendLine("HEXAGON_LAYOUT = " + (HEXAGON_LAYOUT ? "true" : "false"));
            sb.AppendLine("# Save animation of which pixels are set");
            sb.AppendLine("SAVE_SET_PIXELS = " + (SAVE_SET_PIXELS ? "true" : "false"));

            File.WriteAllText(filename, sb.ToString());
        }

        static void LoadFromFile(string filename)
        {
            foreach (var line in File.ReadLines(filename))
            {
                if (!line.StartsWith("#"))
                {
                    var temp = line.Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length == 2)
                    {
                        switch (temp[0].ToUpper())
                        {
                            case "SIZE_WIDTH": SIZE = new Size(ParseInt(temp[1]), SIZE.Height); break;
                            case "SIZE_HEIGHT": SIZE = new Size(SIZE.Width, ParseInt(temp[1])); break;
                            case "FRAME_COUNT": FRAME_COUNT = ParseInt(temp[1]); break;
                            case "INITIAL_FRAMES": INITIAL_FRAMES = ParseInt(temp[1]); break;
                            case "FINAL_FRAMES": FINAL_FRAMES = ParseInt(temp[1]); break;
                            case "SAVE_FRAMES": SAVE_FRAMES = ParseBool(temp[1]); break;
                            case "CHECKERED_BACKGROUND": CHECKERED_BACKGROUND = ParseBool(temp[1]); break;
                            case "SMALL_PREVIEW": SMALL_PREVIEW = ParseBool(temp[1]); break;
                            case "DARKER_IMAGE": DARKER_IMAGE = ParseBool(temp[1]); break;
                            case "COLOR_MODE": COLOR_MODE = temp[1]; break;
                            case "PASTEL": PASTEL = ParseBool(temp[1]); break;
                            case "GAMMA": GAMMA = ParseBool(temp[1]); break;
                            case "AVG_GRAY": AVG_GRAY = ParseBool(temp[1]); break;
                            case "SAVE_SMALLER": SAVE_SMALLER = ParseBool(temp[1]); break;
                            case "HEXAGON_LAYOUT": HEXAGON_LAYOUT = ParseBool(temp[1]); break;
                            case "SAVE_SET_PIXELS": SAVE_SET_PIXELS = ParseBool(temp[1]); break;
                            default:
                                throw new Exception("Unknown setting: " + line);
                        }
                    }
                }
            }
        }

        static int ParseInt(string value)
        {
            var ret = Equation<double>.Static.Evaluate(value);
            return (int)ret;
        }

        static bool ParseBool(string value)
        {
            if (value.ToLower() == "true")
            {
                return true;
            }
            else if (value.ToLower() == "false")
            {
                return false;
            }
            else
            {
                throw new Exception("Unknown bool type: " + value);
            }
        }
    }
}

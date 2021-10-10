using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Version_2
{
    public partial class PreviewForm : Form
    {
        Bitmap m_bitmap = null;
        Thread m_thread = null;
        Size m_preview;

        public PreviewForm()
        {
            InitializeComponent();

            int width = Settings.SIZE.Width;
            int height = Settings.SIZE.Height;

            // Shrink things down for big displays
            while (width > 1280 || height > 720)
            {
                width /= 2;
                height /= 2;
            }

            // Set the size of the form
            ClientSize = new Size(width, height);
            m_preview = new Size(width, height);

            try
            {
                Icon = Version_2.Properties.Resources.Blues;
            }
            catch { }
        }

        void PreviewForm_Load(object sender, EventArgs e)
        {
            if (Settings.IS_SETUP)
            {
                // Kick off the worker thread
                m_thread = new Thread(WorkerThread);
                m_thread.Start();
            }
        }

        void WorkerThread()
        {
            Worker.CheckPoint = CheckPoint;
            Worker.Message = ShowMessage;
            Worker.MainWorker();
        }

        void ShowMessage(string value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    ShowMessage(value);
                }));
                return;
            }

            // Just display the message
            Text = value;
        }

        void CheckPoint(Bitmap bitmap, bool final, string extra, bool ui)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    CheckPoint(bitmap, final, extra, ui);
                }));
                return;
            }

            lock(this)
            {
                Bitmap backup = null;
                if (!ui)
                {
                    backup = m_bitmap;
                }
                // Save the image locally and to disk
                m_bitmap = bitmap;
                
                string name = "Final, 1 Pass, " + Settings.SIZE.Width + "x" + Settings.SIZE.Height + ".png";

                if (extra != null)
                {
                    name = "output_" + extra + ".png";
                }

                // Save to a temp file
                string temp = "temp.png";
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
                m_bitmap.Save("temp.png");

                // Rename it in place
                if (File.Exists(name))
                {
                    File.Delete(name);
                }
                File.Move(temp, name);

#pragma warning disable 162
                if (Settings.SAVE_SMALLER)
                {
                    // And save a scaled down version as well
                    using (Bitmap copy = new Bitmap(bitmap.Width / 2, bitmap.Height / 2, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(copy))
                        {
                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.CompositingQuality = CompositingQuality.HighQuality;
                            g.DrawImage(bitmap,
                                new Rectangle(0, 0, copy.Width, copy.Height),
                                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                GraphicsUnit.Pixel);
                        }


                        name = "Final, 2 Pass, " + copy.Width + "x" + copy.Height + ".png";

                        if (extra != null)
                        {
                            name = "output_" + extra + "_small.png";
                        }

                        // Save to a temp file
                        temp = "temp.png";
                        if (File.Exists(temp))
                        {
                            File.Delete(temp);
                        }
                        copy.Save("temp.png");

                        // Rename it in place
                        if (File.Exists(name))
                        {
                            File.Delete(name);
                        }
                        File.Move(temp, name);
                    }
                }

                if (!ui)
                {
                    m_bitmap = backup;
                }
#pragma warning restore 162
            }

            Invalidate();
        }

        void PreviewForm_Paint(object sender, PaintEventArgs e)
        {
            lock (this)
            {
                // Clear the screen
                e.Graphics.Clear(Color.Gray);

                int x = 0;
                int y = 0;

                // Center the image if the form is bigger
                if (ClientSize.Width > m_preview.Width)
                {
                    x = (ClientSize.Width - m_preview.Width) / 2;
                }

                if (ClientSize.Height > m_preview.Height)
                {
                    y = (ClientSize.Height - m_preview.Height) / 2;
                }

                // Draw the background
                e.Graphics.FillRectangle(Brushes.Black, new Rectangle(x, y, m_preview.Width, m_preview.Height));

                if (m_bitmap != null)
                {
                    // Draw the image itself
                    e.Graphics.DrawImage(m_bitmap,
                        new Rectangle(x, y, m_preview.Width, m_preview.Height),
                        new Rectangle(0, 0, Settings.SIZE.Width, Settings.SIZE.Height), 
                        GraphicsUnit.Pixel);
                }
            }
        }

        void PreviewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // A last chance to abort
            if (Worker.Working)
            {
                if (MessageBox.Show("Are you sure you want to exit?", "All The Colors", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Tell the worker to stop
            Worker.Working = false;

            if (m_thread != null)
            {
                // Wait for the woker to actually stop
                m_thread.Join();
            }
        }

        void PreviewForm_Resize(object sender, EventArgs e)
        {
            // Redraw the display
            Invalidate();
        }
    }
}

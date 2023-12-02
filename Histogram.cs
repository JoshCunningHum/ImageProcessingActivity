using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageProcessingActivity
{
    public class Histogram
    {
        public static int[] Grey(Bitmap bmp)
        {
            int[] result = new int[256];

            BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            IntPtr Ptr = data.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Ptr;
                for(int i = 0; i < bmp.Width; i++)
                {
                    for(int j = 0; j < bmp.Height; j++)
                    {
                        // Exclude all transparent pixels
                        if (p[3] == 0)
                        {
                            p += 4;
                            continue;
                        }

                        int g = Util.Clip.Integer((int)(.299 * p[2] + .587 * p[1] + .114 * p[0]), 0, 255);
                        result[g]++;
                        p += 4;
                    }
                }
            }

            bmp.UnlockBits(data);

            return result;
        }

        public static void Draw(Graphics g, Size s, int[] data, Color? BarColor = null)
        {
            if(BarColor == null) BarColor = Color.Gray;
            Brush BarBrush = new SolidBrush((Color)BarColor);

            int gap = 0;

            float BarWidth = Math.Max(((float)s.Width / (float)256) - gap, 1);
            int MaxData = 1;

            // Get Max Data
            for (int i = 0; i < 256; i++) if (data[i] > MaxData) MaxData = data[i];

/*
            Console.WriteLine("Max: " + MaxData);
            Console.Write("[");
            for (int i = 0; i < 256; i++)
            {
                Console.Write(data[i]);
                if (i < 255) Console.Write(", ");
            }
            Console.Write("]");*/

            // Clear the graphics
            g.Clear(Control.DefaultBackColor);

            for(int i = 0; i < 256; i++)
            {
                float BarHeight = ((float)s.Height * (float)((float) data[i] / (float)MaxData));
                PointF Location = new PointF(i * (BarWidth + gap), s.Height - BarHeight);
                SizeF Size = new SizeF(BarWidth, BarHeight);
                RectangleF Bounds = new RectangleF(Location, Size);
                g.FillRectangle(BarBrush, Bounds);
            }

        }
    }
}

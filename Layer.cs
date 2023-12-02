using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessingActivity
{
    public class LayerFilterOptions
    {
        public static float SepiaR = 1f;
        public static float SepiaG = 0.95f;
        public static float SepiaB = 0.82f;

        public static Color SubtractColor = Color.Green;
        public static float SubtractTolerance = 0.25f;
    }
    public class LayerFilter
    {
        // Layer Filter Options

        // Uses Visitor Pattern-Like Structure to apply filters

        public static Action<Layer> Normal = l =>
        {
            
            // Just copy the image to the processed
            l.processed = new Bitmap(l.image);
        };

        public static Action<Layer> Inverted = l =>
        {
            // Do a basic copy first
            Normal(l);

            BitmapData procesed = l.GetProcessedImageData();
            BitmapData og = l.GetImageData();

            IntPtr pStart = procesed.Scan0;
            IntPtr oStart = og.Scan0;
                
            unsafe
            {
                byte * s = (byte *)(void *)oStart;
                byte * d = (byte *)(void *)pStart;

                for(int i = 0; i < procesed.Height;  i++)
                {
                    for(int j = 0; j < procesed.Width; j++)
                    {
                        d[0] = (byte)(255 - s[0]);
                        d[1] = (byte)(255 - s[2]);
                        d[2] = (byte)(255 - s[2]);

                        d += 4;
                        s += 4;
                    }
                }
            }

            l.UnlockImageData(og);
            l.UnlockProcessedImageData(procesed);
        };

        public static Action<Layer> Greyscale = l =>
        {
            Normal(l);


            BitmapData procesed = l.GetProcessedImageData();
            BitmapData og = l.GetImageData();

            IntPtr pStart = procesed.Scan0;
            IntPtr oStart = og.Scan0;

            unsafe
            {
                byte* s = (byte*)(void*)oStart;
                byte* d = (byte*)(void*)pStart;

                for (int i = 0; i < procesed.Height; i++)
                {
                    for (int j = 0; j < procesed.Width; j++)
                    {
                        d[0] = d[1] = d[2] = (byte)(.299 * s[2] + .587 * s[1] + .114 * s[0]);

                        s += 4;
                        d += 4;
                    }
                }
            }

            l.UnlockImageData(og);
            l.UnlockProcessedImageData(procesed);
        };


        public static Action<Layer> Sepia = l =>
        {
            Normal(l);

            BitmapData procesed = l.GetProcessedImageData();
            BitmapData og = l.GetImageData();

            IntPtr pStart = procesed.Scan0;
            IntPtr oStart = og.Scan0;

            float R = LayerFilterOptions.SepiaR;
            float G = LayerFilterOptions.SepiaG;
            float B = LayerFilterOptions.SepiaB;

            unsafe
            {
                byte* s = (byte*)(void*)oStart;
                byte* d = (byte*)(void*)pStart;

                for (int i = 0; i < procesed.Height; i++)
                {
                    for (int j = 0; j < procesed.Width; j++)
                    {

                        d[0] = d[1] = d[2] = (byte)(.299 * s[2] + .587 * s[1] + .114 * s[0]);

                        d[0] = (byte)(d[0] * B);
                        d[1] = (byte)(d[1] * G);
                        d[2] = (byte)(d[2] * R);

                        s += 4;
                        d += 4;
                    }
                }
            }

            l.UnlockImageData(og);
            l.UnlockProcessedImageData(procesed);
        };

        // Sir, this is the method for doing the subtraction
        // Since this is designed where adding a method here automatically list it as a filter type
        // This is the only thing needed for the subtraction filter
        public static Action<Layer> Subtract = l =>
        {
            Normal(l);

            BitmapData procesed = l.GetProcessedImageData();
            BitmapData og = l.GetImageData();

            IntPtr pStart = procesed.Scan0;
            IntPtr oStart = og.Scan0;

            float tolerance = LayerFilterOptions.SubtractTolerance;

            Color minC = Color.FromArgb((int)(tolerance * 255), 0, 0, 0);
            Color maxC = Color.FromArgb((int)(tolerance * 255), 255, 255, 255);

            Color min = Util.Draw.BlendColors(LayerFilterOptions.SubtractColor, minC);
            Color max = Util.Draw.BlendColors(LayerFilterOptions.SubtractColor, maxC);

            int minR = min.R, minG = min.G, minB = min.B;
            int maxR = max.R, maxG = max.G, maxB = max.B;

            unsafe
            {
                byte* s = (byte*)(void*)oStart;
                byte* d = (byte*)(void*)pStart;

                for (int i = 0; i < procesed.Height; i++)
                {
                    for (int j = 0; j < procesed.Width; j++)
                    {
                        if (s[0] >= minB && s[0] <= maxB &&
                            s[1] >= minG && s[1] <= maxG &&
                            s[2] >= minR && s[2] <= maxR)
                        {
                            d[3] = 0;
                        }
                        else d[3] = s[3];

                        d[0] = s[0];
                        d[1] = s[1];
                        d[2] = s[2];

                        s += 4;
                        d += 4;
                    }
                }
            }

            l.UnlockImageData(og);
            l.UnlockProcessedImageData(procesed);
        };

    }

    public class Layer
    {
        public static Pen DebugPen = new Pen(Color.Yellow, 1);

        public static Color SelectedBorderColor = Color.Red;
        public static Pen SelectedBorderPen = new Pen(SelectedBorderColor, 1);

        public static List<Layer> layers = new List<Layer>();
        private string id;

        // Used for getting the histogram of overall image
        public static Bitmap FlatImage(Size size)
        {
            int width = size.Width;
            int height = size.Height;

            Bitmap result = new Bitmap(width, height);

            for(int i = 0; i < width; i++)
            {
                for(int j = 0; j < height; j++)
                {
                    Color Pixel = Color.Empty;
                    layers.ForEach(l => Pixel = Util.Draw.BlendColors(Pixel, l.GetProcessedPixelAbs(i, j)));
                    result.SetPixel(i, j, Pixel);
                }
            }

            return result;
        }

        public int index => layers.IndexOf(this);

        public string name;
        public Point offset;

        public Bitmap processed = null; // will hold the original image, to be processed when changing filters
        public Bitmap image = null; // will also hold the size of this whole layer

        public Action<Layer> Filter = LayerFilter.Normal;

        // Reference to the app
        private Main m;

        public Layer(Size size, Main m)
        {
            image = new Bitmap(size.Width, size.Height);
            init();
            this.m = m;
        }

        public Layer(Image img, Main m)
        {
            image = (Bitmap) img;
            init();
            this.m = m;
        }

        private void init()
        {
            name = string.Format("Layer {0}", layers.Count);
            offset = new Point(0, 0);
            id = Util.Rand.RandomGuidString(8);
            layers.Add(this);
            ApplyFilter();
        }

        public void Draw(Graphics g, RectangleF CanvasBounds)
        {
            PointF Location = m.ImageOffset + Util.Cast.ToSizeF(Util.Scale.PointF(offset, m.ScaleRatio));
            SizeF Size = Util.Scale.SizeF(processed.Size, m.ScaleRatio);

            RectangleF Bounds = new RectangleF(Location, Size);
            RectangleF ClippedBounds = Util.Clip.RectangleF(Bounds, CanvasBounds, true);
            RectangleF Clipping = Util.Clip.Rectangle(new Rectangle(offset, image.Size), new Rectangle(Point.Empty, Main.CanvasSize), true);

            Clipping.Offset(Util.Draw.NegPoint(offset));

            // g.DrawImage(image, Bounds);
            g.DrawImage(processed, ClippedBounds, Clipping, GraphicsUnit.Pixel);
            // g.DrawRectangle(DebugPen, Util.Cast.Rectangle(ClippedBounds));
        }

        public void DrawBorder(Graphics g)
        {
            PointF Location = m.ImageOffset + Util.Cast.ToSizeF(Util.Scale.PointF(offset, m.ScaleRatio));
            SizeF Size = Util.Scale.SizeF(image.Size, m.ScaleRatio);

            RectangleF Bounds = new RectangleF(Location, Size);
            g.DrawRectangle(SelectedBorderPen, Util.Cast.Rectangle(Bounds));
        }

        // Make sure to unlock the image again
        public BitmapData GetProcessedImageData()
        {
            return processed.LockBits(new Rectangle(0, 0, processed.Width, processed.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        }

        public void UnlockProcessedImageData(BitmapData data)
        {
            processed.UnlockBits(data);
        }

        public BitmapData GetImageData()
        {
            return image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        }
        
        public void UnlockImageData(BitmapData data)
        {
            image.UnlockBits(data);
        }

        public void InsertRowsAndColumns(int rows, int columns)
        {
            Bitmap NewImg = new Bitmap(image.Width + Math.Abs(columns), image.Height + Math.Abs(rows));

            using (Graphics g = Graphics.FromImage(NewImg))
            {
                // Draw the image on the new Img
                int offsetY = Math.Max(-rows, 0);
                int offsetX = Math.Max(-columns, 0);
                g.DrawImageUnscaled(image, new Point(offsetX, offsetY));

                offset.X += -offsetX;
                offset.Y += -offsetY;
            }
            image.Dispose();
            image = NewImg;
        }

        // Negative parameter means put rows in the top
        public void InsertRows(int rows = 1)
        {
            if (rows == 0) return;
            InsertRowsAndColumns(rows, 0);
        }

        // Negative parameter means put cols in the left
        public void InsertColumns(int columns = 1)
        {
            if (columns == 0) return;
            InsertRowsAndColumns(0, columns);
        }

        public Color GetProcessedPixelAbs(int x, int y)
        {
            x -= offset.X;
            y -= offset.Y;
            if (x < 0 || y < 0 || x >= processed.Width || y >= processed.Height) return Color.Empty;
            return processed.GetPixel(x, y);
        }

        public Color GetPixelAbs(int x, int y)
        {
            return GetPixel(x - offset.X, y - offset.Y);
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return Color.Empty;
            return image.GetPixel(x, y);
        }

        public void SetPixelAbs(int x, int y, Color color)
        {
            SetPixel(x - offset.X, y - offset.Y, color);
        }

        public void SetPixel(int x, int y, Color color)
        {
            // TODO: If we are setting pixel in negative coordinate, we expand the image and then set the pixel
            int addedColumns = x < 0 ? x : x >= image.Width ? x - image.Width + 1 : 0;
            int addedRows = y < 0 ? y : y >= image.Height ? y - image.Height + 1 : 0;

            if(addedRows != 0 || addedColumns != 0) InsertRowsAndColumns(addedRows, addedColumns);

            if (x < 0) x = 0;
            if (y < 0) y = 0;

            image.SetPixel(x, y, color);
            // We apply filter everytime we edit the image
            ApplyFilter();
        }

        public void ApplyFilter()
        {
            Filter(this);
            if(m != null) m.RefreshHistogram();
        }

        public void SetFilter(Action<Layer> f)
        {
            Filter = f;
            ApplyFilter();
        }

        public void Rasterize()
        {
            // Copy processed data to image itself
            image = new Bitmap(processed);
            Filter = LayerFilter.Normal;
        }

        public Point RelatePoint(Point point)
        {
            return new Point(point.X - offset.X, point.Y - offset.Y);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Layer)) return false;
            Layer other = obj as Layer;
            return other.id == id;
        }
    }
}

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace ImageProcessingActivity
{
    public class Util
    {
        public static Point Clone(Point p)
        {
            return new Point(p.X, p.Y);
        }

        public static PointF Clone(PointF p)
        {
            return new PointF(p.X, p.Y);
        }

        public static Size Clone(Size s)
        {
            return new Size(s.Width, s.Height);
        }

        public static class Draw
        {
            public static void DrawRotated(Graphics g, Point offset, float degrees, Action act)
            {
                g.TranslateTransform(offset.X, offset.Y);
                g.RotateTransform(-degrees);
                act();
                g.RotateTransform(degrees);
                g.TranslateTransform(-offset.X, -offset.Y);
            }

            public static void EmptyPoint(ref Point p)
            {
                p.X = 0;
                p.Y = 0;
            }

            public static Point NegPoint(Point p)
            {
                p.X *= -1;
                p.Y *= -1;
                return p;
            }

            public static PointF Sub(PointF a, PointF b)
            {
                return new PointF(a.X - b.X, a.Y - b.Y);
            }

            public static PointF Add(PointF a, PointF b)
            {
                return new PointF(a.X + b.X, a.Y  + b.Y);
            }

            public static int GetMax(Size s)
            {
                return Mat.Max(s.Width, s.Height);
            }

            public static float GetMax(SizeF s)
            {
                return Mat.Max(s.Width, s.Height);
            }

            public static Color AddColor(Color a, Color b, bool PreMultiplied = false)
            {
                if (PreMultiplied)
                {
                    a = PostMultiplyColor(a);
                    b = PostMultiplyColor(b);
                }

                byte R = (byte)Clip.Integer(a.R + b.R, 0, 255);
                byte G = (byte)Clip.Integer(a.G + b.G, 0, 255);
                byte B = (byte)Clip.Integer(a.B + b.B, 0, 255);

                return Color.FromArgb(R, G, B);
            }

            public static Color BlendColors(Color a, Color b)
            {
                float A = (float)b.A / (float)255;

                byte R = (byte)Clip.Integer((int)((1 - A) * a.R + A * b.R), 0 , 255);
                byte G = (byte)Clip.Integer((int)((1 - A) * a.G + A * b.G), 0 , 255);
                byte B = (byte)Clip.Integer((int)((1 - A) * a.B + A * b.B), 0 , 255);

                return Color.FromArgb(R, G, B);
            }

            public static Color PostMultiplyColor(Color c)
            {
                byte R = (byte)(c.R * (c.A / 255));
                byte G = (byte)(c.G * (c.A / 255));
                byte B = (byte)(c.B * (c.A / 255));

                return Color.FromArgb(R, G, B);
            }
        }

        public static class Cast
        {
            public static Rectangle Rectangle(Size size)
            {
                return new Rectangle(Point.Empty, size);
            }

            public static Rectangle Rectangle(RectangleF rect)
            {
                return new Rectangle(ToPoint(rect.Location), ToSize(rect.Size));
            }

            public static Rectangle RectangleFrom(Point a, Point b, bool SwapIfNeeded = true)
            {
                Point topLeft = new Point(a.X, a.Y),
                    botRight = new Point(b.X, b.Y);

                if (SwapIfNeeded)
                {
                    topLeft.Y = Mat.Min(a.Y, b.Y);
                    topLeft.X = Mat.Min(a.X, b.X);
                    botRight.Y = b.Y == topLeft.Y ? a.Y : b.Y;
                    botRight.X = b.X == topLeft.X ? a.X : b.X;
                }

                return new Rectangle(topLeft, (Size)botRight - (Size) topLeft);
            }

            public static Point ToPoint(PointF p)
            {
                return new Point((int)(p.X), (int)(p.Y));
            }
            public static Point ToPointRound(PointF p)
            {
                return new Point((int)Math.Round(p.X), (int)Math.Round(p.Y));
            }

            public static Point ToPointRoundDown(PointF p)
            {
                return new Point((int)Math.Floor(p.X), (int)Math.Floor(p.Y));
            }

            public static SizeF ToSizeF(PointF p)
            {
                return new SizeF(p.X, p.Y);
            }

            public static Size ToSize(SizeF size)
            {
                return new Size((int)(size.Width), (int)(size.Height));
            }
            public static Size ToSizeRound(SizeF size)
            {
                return new Size((int)Math.Round(size.Width), (int)Math.Round(size.Height));
            }
        }

        public static class Interpolate
        {
            public static int Integer(int from, int to, float n)
            {
                float diff = (float)(to - from) * n;
                return from + (int)diff;
            }

            public static float Float(float from, float to, float n)
            {
                float diff = (to - from) * n;
                return from + diff;
            }
        }

        public static class Clip
        {
            public static int Integer(int n, int? min, int? max)
            {
                if (min != null && n < min) n = (int)min;
                if(max != null && n > max) n = (int)max;
                return n;
            }

            public static float Float(float n, float? min, float? max)
            {
                if (min != null && n < min) n = (float)min;
                if (max != null && n > max) n = (float)max;
                return n;
            }

            public static Rectangle Rectangle(Rectangle rect, Rectangle basis, bool fullBounds = false)
            {
                if (!fullBounds)
                {
                    rect.Width = Integer(rect.Width, null, basis.Width);
                    rect.Height = Integer(rect.Height, null, basis.Height);
                }
                else
                {
                    int MinX = basis.Left, MinY = basis.Top;
                    int MaxX = Mat.Min(basis.Right, rect.Right),
                        MaxY = Mat.Min(basis.Bottom, rect.Bottom);

                    // Adjust offset
                    if (rect.Top < MinY) rect.Offset(0, MinY - rect.Top);
                    if (rect.Left < MinX) rect.Offset(MinX - rect.Left, 0);
                    // Adjust size
                    if (rect.Right > MaxX) rect.Width -= rect.Right - MaxX;
                    if (rect.Bottom > MaxY) rect.Height -= rect.Bottom - MaxY;
                }

                return rect;
            }

            public static RectangleF RectangleF(RectangleF rect, RectangleF basis, bool fullBounds = false)
            {

                if (!fullBounds)
                {
                    rect.Width = Float(rect.Width, null, basis.Width);
                    rect.Height = Float(rect.Height, null, basis.Height);
                }
                else
                {
                    float MinX = basis.Left, MinY = basis.Top;
                    float MaxX = Mat.Min(basis.Right, rect.Right), 
                        MaxY = Mat.Min(basis.Bottom, rect.Bottom);

                    // Adjust offset
                    if (rect.Top < MinY) rect.Offset(0, MinY - rect.Top);
                    if (rect.Left < MinX) rect.Offset(MinX - rect.Left, 0);
                    // Adjust size
                    if (rect.Right > MaxX) rect.Width -= rect.Right - MaxX;
                    if (rect.Bottom > MaxY) rect.Height -= rect.Bottom - MaxY;
                }

                return rect;
            }

        }
        public static class Mat
        {
            public static int Factorial(int n)
            {
                int total = 1;
                for (int i = n; i > 1; i--) total *= i;
                return total;
            }

            public static float RoundTo(float number, float magnitude)
            {
                return (int)(number / magnitude + 1) * magnitude;
            }

            public static int Min(params int[] values)
            {
                return (int)Array.Reduce(Array.From(values), (acc, v) =>
                {
                    int element = (int)v, smallest = (int)acc;
                    return element < smallest ? element : smallest;
                }, values[0]);
            }

            public static float Min(params float[] values)
            {
                return (float)Array.Reduce(Array.From(values), (acc, v) =>
                {
                    float element = (float)v, smallest = (float)acc;
                    return element < smallest ? element : smallest;
                }, values[0]);
            }

            // Supports multiple values
            public static int Max(params int[] values)
            {
                return (int)Array.Reduce(Array.From(values), (acc, v) =>
                {
                    int element = (int)v, largest = (int)acc;
                    return element > largest ? element : largest;
                }, values[0]);
            }

            // Supports multiple values
            public static float Max(params float[] values)
            {
                return (float)Array.Reduce(Array.From(values), (acc, v) =>
                {
                    float element = (float)v, largest = (float)acc;
                    return element > largest ? element : largest;
                }, values[0]);
            }
        }

        public static class Control
        {
            public static int ValidateInt(TextBoxBase control, int defaultValue = 0)
            {
                string input = control.Text;
                int value;
                try
                {
                    value = Convert.ToInt32(input);
                }catch (Exception)
                {
                    value = defaultValue;
                }

                return value;
            }

            public static void OnlyAcceptNumbers(KeyEventArgs e)
            {
                if(e.KeyData != Keys.Back) e.SuppressKeyPress = !int.TryParse(Convert.ToString((char)e.KeyData), out int _);
            }
        }

        public static class Scale
        {
            public static Size Size(Size size, float scale)
            {
                return new Size((int)(size.Width * scale), (int)(size.Height * scale));
            }

            public static SizeF SizeF(Size size, float scale)
            {
                return new SizeF((float)size.Width * scale, (float)size.Height * scale);
            }

            public static SizeF SizeF(SizeF size, float scale)
            {
                return new SizeF(size.Width * scale, size.Height * scale);
            }

            public static Rectangle Rectangle(Rectangle rect, float scale)
            {
                return new Rectangle(rect.Location, Size(rect.Size, scale));
            }

            public static Point Point(Point p, float scale)
            {
                return new Point((Size)(Point)Util.Scale.Size((Size)p, scale));
            }

            public static PointF PointF(PointF p, float scale)
            {
                return new PointF(p.X * scale, p.Y * scale);
            }

            public static PointF PointF(Point p, float scale)
            {
                return new PointF((float)p.X * scale, (float)p.Y * scale);
            }
        }

        public static class Rand
        {
            public static Random RNG = new Random();

            public static int ChooseFromProbabilities(ArrayList probabilities)
            {
                int total = (int) Array.Reduce(probabilities, (acc, current) =>
                {
                    return (int)acc + (int)current;
                }, 0);

                int chosen = RNG.Next(total), accumulator = 0;

                for(int i = 0; i < probabilities.Count; i++)
                {
                    accumulator += (int) probabilities[i];
                    if (chosen < accumulator) return i;
                }

                // Will not happen
                return -1;
            }

            public static string RandomGuidString(int length) => Guid.NewGuid().ToString("N").Substring(0, length);
        }

        public static class Array
        {
            public static ArrayList From(String str)
            {
                ArrayList array = new ArrayList();
                foreach (char c in str)
                {
                    array.Add(c);
                }
                return array;
            }

            public static ArrayList From(int[] values)
            {
                ArrayList array = new ArrayList();
                array.AddRange(values);
                return array;
            }
            public static ArrayList From(float[] values)
            {
                ArrayList array = new ArrayList();
                array.AddRange(values);
                return array;
            }

            public static object Reduce(ArrayList array, Func<object, object, object> callback, object initial)
            {
                return Reduce(array, (accumulator, current, index) =>
                {
                    return callback(accumulator, current);
                }, initial);
            }

            public static object Reduce(ArrayList array, Func<object, object, int, object> callback, object initial)
            {
                for(int i = 0; i <  array.Count; i++)
                {
                    object element = array[i];
                    initial = callback(initial, element, i);
                }
                return initial;
            }

            public static object Get(ArrayList array, int index, bool Looping = false)
            {
                if (index >= array.Count && !Looping) return null;
                else if (index >= array.Count) index %= array.Count;
                if (index < 0) index = array.Count - index;
                return array[index];
            }
        }

        public static class FileDialog
        {
            public static string ImageFilter()
            {
                return
                    "All Files (*.*)|*.*" +
                    "|All Pictures (*.emf;*.wmf;*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif;*.emz;*.wmz;*.tif;*.tiff;*.svg;*.ico)" +
                        "|*.emf;*.wmf;*.jpg;*.jpeg;*.jfif;*.jpe;*.png;*.bmp;*.dib;*.rle;*.gif;*.emz;*.wmz;*.tif;*.tiff;*.svg;*.ico" +
                    "|Windows Enhanced Metafile (*.emf)|*.emf" +
                    "|Windows Metafile (*.wmf)|*.wmf" +
                    "|JPEG File Interchange Format (*.jpg;*.jpeg;*.jfif;*.jpe)|*.jpg;*.jpeg;*.jfif;*.jpe" +
                    "|Portable Network Graphics (*.png)|*.png" +
                    "|Bitmap Image File (*.bmp;*.dib;*.rle)|*.bmp;*.dib;*.rle" +
                    "|Compressed Windows Enhanced Metafile (*.emz)|*.emz" +
                    "|Compressed Windows MetaFile (*.wmz)|*.wmz" +
                    "|Tag Image File Format (*.tif;*.tiff)|*.tif;*.tiff" +
                    "|Scalable Vector Graphics (*.svg)|*.svg" +
                    "|Icon (*.ico)|*.ico";
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace ImageProcessingActivity
{
    internal interface Drawable
    {
        void Draw(Graphics g, Renderer r);
    }
    internal class Renderer
    {
        public static FontFamily _debugff = new FontFamily("Times New Roman");
        public static Font debugFont = new Font(_debugff, 11, FontStyle.Regular, GraphicsUnit.Pixel);
        public static Brush debugBrush = new SolidBrush(Color.Red);

        private System.Threading.Timer ticker;
        private readonly Control target;
        private readonly Form form;

        private readonly List<Action<Graphics>> beforeTick = new List<Action<Graphics>>();
        private readonly List<Action<Graphics>> afterTick = new List<Action<Graphics>>();

        private readonly ArrayList Drawables = new ArrayList();

        public int fps = 90;
        public Point Mouse = new Point(0, 0);

        public Point ClientOffset = new Point(0, 0);

        public bool FastMode = false;
        public int Scale = 1;
        public Point Offset = new Point(0, 0);

        // Some Useful Flags for Interaction
        public int ClickInstance = 0;
        public bool onLeftClick
        {
            get
            {
                return ClickInstance > 0;
            }
            set 
            {
                ClickInstance = value ? ClickInstance+1 : 0;
            }
        }
        public bool onDrag
        {
            get { return ClickInstance > 1;}
        }
        public bool onTop = false;

        public Renderer(Control target, Form form, bool FastMode = false)
        {
            this.form = form;
            this.target = target;
            target.Paint += new PaintEventHandler(this.Update);

            target.MouseDown += (Object sender, MouseEventArgs e) => onLeftClick = true;
            target.MouseUp += (Object sender, MouseEventArgs e) => onLeftClick = false;
            target.MouseEnter += (Object sender, EventArgs e) => onTop = true;
            target.MouseLeave += (Object sender, EventArgs e) => onTop = false;

            form.Resize += (Object sender, EventArgs e) => RefreshClientOffset();
            form.LocationChanged += (Object sender, EventArgs e) => RefreshClientOffset();

            this.FastMode = FastMode;
        }

        public void Add(params object[] d)
        {
            foreach (Drawable item in d)
            {
                Drawables.Add(item);
            }
        }

        public void AddMultiple(object[] d)
        {
            foreach (Drawable item in d)
            {
                Drawables.Add(item);
            }
        }

        public void Remove(Drawable d)
        {
            Drawables.Remove(d);
        }

        public void Clear()
        {
            Drawables.Clear();
        }

        public void BeforeTick(Action<Graphics> action)
        {
            beforeTick.Add(action);
        }

        public void AfterTick(Action<Graphics> action)
        {
            afterTick.Add(action);
        }

        public void Start()
        {
            ticker = new System.Threading.Timer(Tick, null, 0, 1000 / fps);
            RefreshClientOffset();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Stop()
        {
            ticker?.Dispose();
        }

        private void Update(object sender, PaintEventArgs e)
        {
            // Draw drawables found in the renderer
            Graphics g = e.Graphics;

            if (FastMode)
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            }

            // Apply Offset
            g.TranslateTransform(Offset.X, Offset.Y);


            beforeTick.ForEach(a => a.Invoke(g));

            foreach (Drawable d in Drawables)
            {
                d.Draw(g, this);
            }

            afterTick.ForEach(a => a.Invoke(g));

            // Activate Flags
            if (onLeftClick) ClickInstance++;
        }

        public double Deltatime => 1000 / fps;

        public void Tick(object obj)
        {
            // Find current mouse coordinates
            Point AbsoluteMouseCoordinates = Control.MousePosition;
            Size Offset = (Size)ClientOffset;
            Mouse = AbsoluteMouseCoordinates - Offset;

            target.Invalidate();
        }

        public void RefreshClientOffset()
        {
            ClientOffset = target.PointToScreen(Point.Empty);
        }
    }
}

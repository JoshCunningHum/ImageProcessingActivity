using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageProcessingActivity
{
    public class OptimizePanel : System.Windows.Forms.Panel
    {
        readonly List<Action<KeyEventArgs>> KeyUpHandlers = new List<Action<KeyEventArgs>>();
        readonly List<Action<KeyEventArgs>> KeyDownHandlers = new List<Action<KeyEventArgs>>();

        public OptimizePanel()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            TabStop = true;
            this.AutoSize = true;
            this.Dock = DockStyle.Fill;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Focus();
            base.OnMouseEnter(e);
        }


        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Control) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            foreach (Action<KeyEventArgs> action in KeyUpHandlers) action(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            foreach (Action<KeyEventArgs> action in KeyDownHandlers) action(e);
        }

        public void AddKeyDownEvent(Action<KeyEventArgs> a)
        {
            KeyDownHandlers.Add(a);
        }

        public void AddKeyUpEvent(Action<KeyEventArgs> a)
        {
            KeyUpHandlers.Add(a);
        }

    }
}

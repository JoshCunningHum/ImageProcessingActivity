using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageProcessingActivity
{
    internal class ViewTool : Tool
    {

        public ViewTool(int index, Main m) : base(index, "CTRL+Scroll to Zoom, SPACE+Drag to Span, Drag to Move a Layer")
        {
            bool IsSpanning = false;

            m.pnlImage.AddKeyDownEvent(ev =>
            {
                // Add shortcut for this tool
                if (ev.KeyCode == Keys.V) m.SelectTool(index);
                if (ev.KeyCode == Keys.Space) IsSpanning = true;
                m.pnlImage.Focus();
            });
            m.pnlImage.AddKeyUpEvent(ev =>
            {
                if (ev.KeyCode == Keys.Space) IsSpanning = false;
            });

            PointF SavedOffset = new PointF();
            Point SavedOffsetLayer = new Point();

            Dictionary<ToolEvent, Action<Graphics>> events = new Dictionary<ToolEvent, Action<Graphics>>()
            {
                {ToolEvent.Click, g =>
                {
                    if (IsSpanning)
                    {
                        SavedOffset = Util.Clone(m.AppliedImageOffset);
                    }
                    else
                    {
                        Layer selected = m.SelectedLayer;
                        if(selected == null) return;

                        SavedOffsetLayer = Util.Clone(selected.offset);
                    }
                }},
                {ToolEvent.ClickHover, g =>
                {
                    if (IsSpanning)
                    {
                        m.AppliedImageOffset = SavedOffset + (Size) m.PointChange;
                    }
                    else
                    {
                        Layer selected = m.SelectedLayer;
                        if(selected == null) return;

                        selected.offset = SavedOffsetLayer + (Size) m.PointChangeReScaled;
                    }
                } },
            };

            this.events = events;
        }
    }
}

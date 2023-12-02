using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageProcessingActivity
{
    internal class MoveTool : Tool
    {
        public MoveTool(int index, Main m) : base (index, "Move")
        {
            Point SavedOffset = new Point();

            // Add shortcut for this tool
            m.pnlImage.AddKeyDownEvent(ev =>
            {
                if (ev.KeyCode == Keys.T) m.SelectTool(index);
                m.pnlImage.Focus();
            });

            Dictionary<ToolEvent, Action<Graphics>> events = new Dictionary<ToolEvent, Action<Graphics>>()
            {
                {ToolEvent.Click, g =>
                {
                    // Check if we are selecting a layer
                    Layer selected = Layer.layers[m.SelectedLayerIndex];

                    if(selected == null) return;

                    SavedOffset = Util.Clone(selected.offset);
                }},
                {ToolEvent.ClickHover, g =>
                {
                    // Move the offset of that layer
                    // m.AppliedImageOffset = SavedOffset + (Size) m.PointChange;
                    Layer selected = Layer.layers[m.SelectedLayerIndex];
                    if(selected == null) return;

                    selected.offset = SavedOffset + (Size) m.PointChangeReScaled;
                } },
            };

            this.events = events;
        }
    }
}

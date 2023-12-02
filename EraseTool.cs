using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;

namespace ImageProcessingActivity
{
    internal class EraseTool : Tool
    {
        public EraseTool(int index, Main m) : base(index, "Erase")
        {

            m.pnlImage.AddKeyDownEvent(ev =>
            {
                // Add shortcut for this tool
                if (ev.KeyCode == Keys.E) m.SelectTool(index);
                m.pnlImage.Focus();
            });

            Pen HoveringPixelBorderPen = new Pen(Color.Black, 1);
            HoveringPixelBorderPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            Point PixelCoord = m.CursorCoordinate;
            bool IsNotWithinCanvas = false;

            Dictionary<ToolEvent, Action<Graphics>> events = new Dictionary<ToolEvent, Action<Graphics>>()
            {
                {ToolEvent.Hover, g=>
                {
                    // Draw Boxes to the pixel we are hovering
                    if(m.ScaleRatio < 4) return; // Only hover when a pixel (in image) is greater than set pixel (in desktop)

                    Size ImageSize = Main.CanvasSize;

                    IsNotWithinCanvas = PixelCoord.X < 0 ||
                    PixelCoord.Y < 0 ||
                    PixelCoord.X >= ImageSize.Width ||
                    PixelCoord.Y >= ImageSize.Height;

                    if(IsNotWithinCanvas) return;

                    PointF CursorLocationF = m.ScaledCursorCoordinate;

                    PointF Location = CursorLocationF + new SizeF(m.ImageOffset);
                    SizeF Size = new SizeF(m.ScaleRatio, m.ScaleRatio);
                    RectangleF Bounds = new RectangleF(Location, Size);

                    g.DrawRectangle(HoveringPixelBorderPen, Util.Cast.Rectangle(Bounds));
                }},
                {ToolEvent.ClickHover, g =>
                {
                    PixelCoord = m.CursorCoordinate;
                    if(IsNotWithinCanvas) return;

                    Layer SelectedLayer = m.SelectedLayer;
                    if(SelectedLayer == null) return;

                    SelectedLayer.SetPixelAbs(PixelCoord.X, PixelCoord.Y, Color.Empty);
                }},
            };

            this.events = events;
        }
    }
}

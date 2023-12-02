using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using static System.Net.Mime.MediaTypeNames;

namespace ImageProcessingActivity
{
    public partial class 
        Main : Form
    {
        // Make it static so that other classes can easily access the size of the whole canvas
        public static Size CanvasSize = new Size(30, 30);

        public OptimizePanel pnlImage;
        public OptimizePanel pnlHistogramVisual;

        // Colors
        public Color RulerBG = Control.DefaultBackColor;
        public Color RulerFG = Color.Black;
        public Color OutsideImage = Color.DarkGray;
        public Color RulerCursorBG = Color.FromArgb(150, 0, 120, 215);
        public Color RulerGridFG = Color.Black;
        public Brush OutsideImageBrush;
        public Brush OpacityBrush;
        public Brush RulerBGBrush;
        public Brush RulerFGBrush;
        public Brush RulerCursorBrush;
        public Pen RulerFGPen;
        public Pen RulerGridPen;

        // Image Scale Render (handles zooms) 
        // How many pixels (in screen) is 1 pixel in the Canvas
        public float BaseScaleRatio = 1; // (Changes when the client width/size changes)
        // Multiplied to above to apply the zoom level 
        public float ModifiedScaleRatio = .75f; // (Changes when user changes the zoom level)
        public float ScaleRatio => BaseScaleRatio * ModifiedScaleRatio;
        public float MinZoom => Math.Min(CanvasSize.Width, CanvasSize.Height) / 5;
        public float MaxZoom => 0.2f;

        // Image Offset (for panning)
        public PointF CenteredImageOffset = new PointF(0, 0); // The BaseOffset for Centering the Canvas Size
        public PointF AppliedImageOffset = new PointF(0, 0); // Offset applied by the user when using the Select Tool
        public PointF ImageOffset => CenteredImageOffset + Util.Cast.ToSizeF(AppliedImageOffset);
        public PointF CalculatedOffset => Util.Scale.PointF(ImageOffset, 1 / ScaleRatio);

        // App stuff
        public float[] TrackBarZoomLevels = new float[] { .25f, .5f, .75f, 1f, 1.5f, 2f, 3f, 4f };
        public int toolIndex = 1;
        public int SelectedLayerIndex = 0;
        public Layer SelectedLayer
        {
            get
            {
                try { return Layer.layers[SelectedLayerIndex]; }
                catch { return null; }
            }
        }
        Tool[] Tools;
        List<Action<Layer>> Filters = new List<Action<Layer>>();

        // Cameruh
        FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        // Draggin Feature
        public Point PointOrigin = new Point(0, 0);
        public Point PointEnd = new Point(0, 0);
        public Point PointChange => PointEnd - (Size)PointOrigin;
        public Point PointChangeReScaled => Util.Cast.ToPoint(Util.Scale.PointF(PointChange, 1 / ScaleRatio));

        public Point CursorCoordinate = new Point(0, 0);
        public PointF ScaledCursorCoordinate => Util.Scale.PointF(CursorCoordinate, ScaleRatio);

        // Settings
        public bool ShowGrid = false;
        public bool MaintainRatio = false;

        public 
            Main()
        {
            InitializeComponent();

            DoubleBuffered = true;

            // Set Pens Brushes
            RulerFGPen = new Pen(RulerFG);
            RulerBGBrush = new SolidBrush(RulerBG);
            RulerFGBrush = new SolidBrush(RulerFG);
            OutsideImageBrush = new SolidBrush(OutsideImage);
            RulerCursorBrush = new SolidBrush(RulerCursorBG);
            RulerGridPen = new Pen(RulerGridFG);
            RulerGridPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            OpacityBrush = new TextureBrush(Properties.Resources.Transparent);

            // Set File Dialog Image filters
            fileDialog.Filter = Util.FileDialog.ImageFilter();
            KeyPreview = true;

            // Initialize all possible Filters
            Type type = typeof(LayerFilter);
            foreach (var p in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
            {
                var v = p.GetValue(null);
                cbLayerFilters.Items.Add(p.Name);
            }
            Filters = typeof(LayerFilter).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).Select(f => (Action<Layer>)f.GetValue(null)).ToList();

            cbLayerFilters.SelectedIndex = 0;
        }   

        private void Main_Load(object sender, EventArgs e)
        {
            // Optimize panel
            pnlImage = new OptimizePanel();
            pnlImageContainer.Controls.Add(pnlImage);

            pnlHistogramVisual = new OptimizePanel();
            pnlHistogramVisual.Paint += pnlHistogramVisual_Paint;
            pnlHistogramContainer.Controls.Add(pnlHistogramVisual);

            ComputeImageOffsetAndScaleRatio();
            // Create an empty base layer
            new Layer(CanvasSize, this);
            RefreshLayerList();
            lbLayers.SelectedIndex = 0;
            cbHistogramTarget.SelectedIndex = 0;
            RefreshHistogram();

            Renderer r = new Renderer(pnlImage, this, true);
            r.Start();

            // Detect Events to change flags
            Resize += (Object formSender, EventArgs formE) => ComputeImageOffsetAndScaleRatio();

            // Tool Buttons
            int i = 0;
            foreach (var item in new List<Button>()
            {
                btnView,
                null,
                btnBrush,
                btnErase
            })
            {
                if (item == null)
                {
                    i++;
                    continue;
                }
                int assignIndex = i;
                item.Click += (Object btnSender, EventArgs btnE) => SelectTool(assignIndex);
                i++;
            };

            // Default Event Handlers
            void HoverDefault(Graphics g)
            {
                CursorCoordinate = Util.Cast.ToPointRoundDown(Util.Scale.PointF(r.Mouse, 1 / ScaleRatio) - Util.Cast.ToSizeF(CalculatedOffset));
                SetLblCursor(CursorCoordinate);
            }

                // Dragging

            void ClickDefault(Graphics g)
            {
                PointOrigin = r.Mouse;
            }

            void MouseDownDefault(Graphics g)
            {
                PointEnd = r.Mouse;
            }

            // Tools
            Tools = new Tool[]
            {
                new ViewTool(0, this),
                null,
                new BrushTool(2, this),
                new EraseTool(3, this)
            };
            SelectTool(0);

            // Zooming using scroll feature

            bool PressingControl = false;
            pnlImage.AddKeyDownEvent(ev =>
            {
                if(ev.KeyCode == Keys.ControlKey) PressingControl = true;
            });

            pnlImage.AddKeyUpEvent(ev =>
            {
                if(ev.KeyCode == Keys.ControlKey) PressingControl = false;
            });



            pnlImage.MouseWheel += (Object pnlSender, MouseEventArgs pnlE) =>
            {
                bool isScrollUp = pnlE.Delta < 0;

                // Handle Scroll Behaviors
                if (!PressingControl) return;

                float PreviousScale = ScaleRatio;
                PointF PreviousCursorOffset = ScaledCursorCoordinate;

                /*float Step = (MinZoom - MaxZoom) / Math.Max(((ModifiedScaleRatio - MaxZoom) * MinZoom), (MinZoom - MaxZoom) / 5);
*/
                float Step = ModifiedScaleRatio / 5;
                
                float NextScaleRatio = ModifiedScaleRatio + Step * (isScrollUp ? -1 : 1);
                if (NextScaleRatio < MaxZoom) NextScaleRatio = MaxZoom;
                else if (NextScaleRatio > MinZoom) NextScaleRatio = MinZoom;
                SetZoom(NextScaleRatio, false);

                PointF NewCursorOffset = ScaledCursorCoordinate;

                // Get difference between two points then add that to applied image offset
                AppliedImageOffset = Util.Draw.Add(Util.Draw.Sub(PreviousCursorOffset, NewCursorOffset), AppliedImageOffset);
            };

            r.BeforeTick(g =>
            {

                // Default Events
                if (r.onTop) HoverDefault(g);

                Tool ActiveTool = Tools[toolIndex];

                if (r.onLeftClick && r.ClickInstance == 1)
                {
                    ClickDefault(g);
                    ActiveTool.RunIfExists(ToolEvent.Click, g);
                }
                else if(r.onDrag)
                {
                    MouseDownDefault(g);
                    ActiveTool.RunIfExists(ToolEvent.ClickHover, g);
                }
                else
                {
                    // Release
                    Util.Draw.EmptyPoint(ref PointOrigin);
                    Util.Draw.EmptyPoint(ref PointEnd);
                    ActiveTool.RunIfExists(ToolEvent.EndClickHover, g);
                }

                // Draw OutsideImage Background
                g.FillRectangle(OutsideImageBrush, 0, 0, pnlImage.Width, pnlImage.Height);

                // Compute Canvas Bounds
                RectangleF CanvasBounds = new RectangleF(Util.Cast.ToPoint(ImageOffset), Util.Scale.SizeF(CanvasSize, ScaleRatio));
                g.FillRectangle(OpacityBrush, CanvasBounds);

                // Draw Layers
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half; // Fixes the wrong offsetting of pixels when drawing
                Layer.layers.ForEach(l => l.Draw(g, CanvasBounds));
/*
                for (int layerIndex = Layer.layers.Count - 1; layerIndex >= 0; layerIndex--)
                {
                    Layer l = Layer.layers[layerIndex];
                    l.Draw(g, CanvasBounds);
                }
*/
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

                // Draw OutsideImage Background
                /* g.FillRectangle(OutsideImageBrush, Util.Cast.Rectangle(new Size((int)CanvasBounds.Left, pnlImage.Height)));
                 g.FillRectangle(OutsideImageBrush, Util.Cast.Rectangle(new Size(pnlImage.Width, (int)CanvasBounds.Top)));
                 g.FillRectangle(OutsideImageBrush, new Rectangle(0, (int)CanvasBounds.Bottom, pnlImage.Width, pnlImage.Height));
                 g.FillRectangle(OutsideImageBrush, new Rectangle((int)CanvasBounds.Right, 0, pnlImage.Width, pnlImage.Height));*/

                // Hover Effects on ActiveTool
                if (r.onTop) ActiveTool.RunIfExists(ToolEvent.Hover, g);

                // Draw selected layer's border
                SelectedLayer?.DrawBorder(g);

                // Draw Rulers
                DrawRulers(g);

                // Misc
                SetLblImageSize();
            });
        }

        // Helper Methods
        private void ComputeImageOffsetAndScaleRatio()
        {
            int MaxCanvasDim = Math.Max(CanvasSize.Width, CanvasSize.Height);
            int MinPageDim = Math.Min(pnlImage.Width, pnlImage.Height);
            BaseScaleRatio = (float) MinPageDim / (float) MaxCanvasDim;
            ComputeCenterImageOffset();
        }

        private void ComputeCenterImageOffset()
        {
            CenteredImageOffset.X = ((float)pnlImage.Width - (float)CanvasSize.Width * ScaleRatio) / 2;
            CenteredImageOffset.Y = ((float)pnlImage.Height - (float)CanvasSize.Height * ScaleRatio) / 2;
        }

        private void DrawRulers(Graphics g)
        {
            const int RULER_WIDTH = 30, MAX_TICKS = 100;
            const int MINGAPX = 50, MINGAPY = 50, MINTERVAL = 1;


            Rectangle HorizontalRulerBounds = new Rectangle(new Point(RULER_WIDTH, 0), new Size(pnlImage.Width - RULER_WIDTH, RULER_WIDTH));
            Rectangle VerticalRulerBounds = new Rectangle(new Point(0, RULER_WIDTH), new Size(RULER_WIDTH, pnlImage.Height - RULER_WIDTH));

            // Actual Width of the Ruler in Pixels
            float PanelWidth = (float)pnlImage.Width;
            float PanelHeight = (float)pnlImage.Height;

            // Calculate the width/height of the panel scaled with ScaleRatio
            float WidthRange = PanelWidth / ScaleRatio;
            float HeightRange = PanelHeight / ScaleRatio;

            // Max number of ticks we can display
            // float MaxTicksWidth = PanelWidth / MINGAPX;
            // float MaxTicksHeight = PanelHeight / MINGAPY;

            // Minimum range between two labels
            float MinRangeGapX = Util.Mat.RoundTo(WidthRange * MINGAPX / PanelWidth, MINTERVAL);
            float MinRangeGapY = Util.Mat.RoundTo(HeightRange * MINGAPY / PanelHeight, MINTERVAL);

            // Space between ticks
            float GapX = MinRangeGapX / WidthRange * PanelWidth;
            float GapY = MinRangeGapY / HeightRange * PanelHeight;

            // Number of ticks
            float XTicks = PanelWidth / GapX;
            float YTicks = PanelHeight / GapY;

            // For offsetting
            float WidthOffset = (ImageOffset.X % GapX);
            float HeightOffset = (ImageOffset.Y % GapY);


            // DrawBGS
            g.FillRectangle(RulerBGBrush, HorizontalRulerBounds);
            g.FillRectangle(RulerBGBrush, VerticalRulerBounds);


            // Horizontal Ruler (Top)
            for (int i = 0; i < XTicks; i++)
            {
                float X = WidthOffset + i * GapX;
                float ImageX = ((X - ImageOffset.X) / ScaleRatio);

                if (X < RULER_WIDTH) continue;
                // Draw Ticks
                g.DrawLine(RulerFGPen, new Point((int) X, 0), new Point((int)X, RULER_WIDTH));

                // Write Text
                g.DrawString(Math.Round(ImageX).ToString(), Control.DefaultFont, RulerFGBrush,  new PointF(X + 1, 2));

                // Draw Grid
                if (!ShowGrid || ImageX < 0 || ImageX > CanvasSize.Width) continue;
                g.DrawLine(RulerGridPen, X, ImageOffset.Y, X, ImageOffset.Y + CanvasSize.Height * ScaleRatio);
            }
            g.DrawRectangle(RulerFGPen, HorizontalRulerBounds);

            // Vertical Ruler
            for (int i = 0; i < YTicks; i++)
            {
                float Y = HeightOffset + i * GapY;
                float ImageY = ((Y - ImageOffset.Y) / ScaleRatio);

                if (Y < RULER_WIDTH) continue;
                // Draw Ticks
                g.DrawLine(RulerFGPen, new Point(0, (int) Y), new Point(RULER_WIDTH, (int)Y));
                // Write Text
                g.DrawString(Math.Round(ImageY).ToString(), DefaultFont, RulerFGBrush, new PointF(2, Y + 1));

                // Draw Grid
                if (!ShowGrid || ImageY < 0 || ImageY > CanvasSize.Height) continue;
                g.DrawLine(RulerGridPen, ImageOffset.X, Y, ImageOffset.X + CanvasSize.Width * ScaleRatio, Y);
            }
            g.DrawRectangle(RulerFGPen, VerticalRulerBounds);

            // Draw Cursor Location in the ruler
            float PixelDim = Math.Max(ScaleRatio, 1);
            float PixelX = (CursorCoordinate.X * ScaleRatio + ImageOffset.X);
            float PixelY = (CursorCoordinate.Y * ScaleRatio + ImageOffset.Y);
            g.FillRectangle(RulerCursorBrush, PixelX, 0, PixelDim, RULER_WIDTH);
            g.FillRectangle(RulerCursorBrush, 0, PixelY, RULER_WIDTH, PixelDim);

            // Fill that hole in your heart
            // Jk, fill the hole in the top right
            g.FillRectangle(RulerBGBrush, new Rectangle(Point.Empty, (Size)new Point(RULER_WIDTH, RULER_WIDTH)));
        }

        private void SetZoom(float zoom, bool ComputeCenterImage = true)
        {
            lblZoomLevel.Text = zoom * 100 + "%";
            ModifiedScaleRatio = zoom;

            int tbIndex = 0;
            for(int i = 0; i < TrackBarZoomLevels.Length; i++)
            {
                float zoomLevel = TrackBarZoomLevels[i];
                if (zoom > zoomLevel) tbIndex = i;
            }
            tbZoomLevel.Value = tbIndex + 1;

            if(ComputeCenterImage) ComputeCenterImageOffset();
        }

        private void RefreshLayerList()
        {
            lbLayers.Items.Clear();
            Layer.layers.ForEach(l => lbLayers.Items.Add(l.name));
            lbLayers.SelectedIndex = 0;
            ComputeImageOffsetAndScaleRatio();

            // Refresh Histogram Selectables
            int targetHistogram = cbHistogramTarget.SelectedIndex;
            cbHistogramTarget.Items.Clear();
            Layer.layers.ForEach(l => cbHistogramTarget.Items.Add(l.name));
            cbHistogramTarget.SelectedIndex = targetHistogram < cbHistogramTarget.Items.Count ? targetHistogram : 0;
        }

        public void RefreshHistogram()
        {
            // If we are not selecting histogram tab, do not update
            if (tabOperations.SelectedTab != tpHistogram) return;
            pnlHistogramVisual.Invalidate();
        }

        private void AddLayer(System.Drawing.Image img = null)
        {
            if (img == null) new Layer(CanvasSize, this);
            else new Layer(img, this);
            RefreshLayerList();
        }

        private void OpenImage(object sender, EventArgs e)
        {
            DialogResult result = fileDialog.ShowDialog();
            System.Drawing.Image loaded = System.Drawing.Image.FromFile(fileDialog.FileName);

            bool isGreaterWidth = loaded.Width > CanvasSize.Width;
            bool isGreaterHeight = loaded.Height > CanvasSize.Height;

            if (isGreaterWidth || isGreaterHeight)
            {
                DialogResult resizeDialogResult = MessageBox.Show("Image is bigger than the canvas, resize canvas?", "Confirm", MessageBoxButtons.YesNo);

                if (resizeDialogResult == DialogResult.Yes)
                {
                    if (isGreaterWidth) CanvasSize.Width = loaded.Width;
                    if (isGreaterHeight) CanvasSize.Height = loaded.Height;
                }
            }
            AddLayer(loaded);
        }

        private void SaveImage()
        {
            SaveFileDialog dialog = new SaveFileDialog();

            if(dialog.ShowDialog() == DialogResult.OK)
            {
                new Thread(() => System.Windows.Forms.MessageBox.Show("Saving data please wait...")).Start();
                Layer.FlatImage(CanvasSize).Save(dialog.FileName, ImageFormat.Png);
            }
        }

        public void SelectTool(int index)
        {
            toolIndex = index;
            // Change the selected ToolOption Tab
            tcToolOptions.SelectedIndex = index;
            // Change tool description
            lblToolUsed.Text = Tools[index].name;
        }

        public void SelectLayer(int index)
        {
            SelectedLayerIndex = index;
            lbLayers.SelectedIndex = index;
            tbLayerName.Text = SelectedLayer?.name;
            // Update Filter Combobox
            cbLayerFilters.SelectedIndex = Filters.IndexOf(SelectedLayer.Filter);
            // Show/Hide the device combobox
            cbDeviceList.Visible = (SelectedLayer is DynamicLayer);
        }

        // Label Methods
        private void SetLblCursor(Point p)
        {
            lblCursorPosition.Text = string.Format("({0}, {1})", p.X, p.Y);
        }

        private void SetLblImageSize()  
        {
            lblImageSize.Text = string.Format("({0}, {1})", CanvasSize.Width, CanvasSize.Height);
        }

        // Events added through the UI

        private void btnAddLayer_Click(object sender, EventArgs e)
        {
            AddLayer();
        }

        private void btnViewToOrigin_Click(object sender, EventArgs e)
        {
            AppliedImageOffset.X = 0;
            AppliedImageOffset.Y = 0;
            SetZoom(0.75f);
        }

        private void lbLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectLayer(lbLayers.SelectedIndex);
        }

        private void tbZoomLevel_Scroll(object sender, EventArgs e)
        {
            SetZoom(TrackBarZoomLevels[tbZoomLevel.Value - 1], false);
        }

        private void tbLayerName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (SelectedLayer != null) return;
            SelectedLayer.name = tbLayerName.Text;
            RefreshLayerList();
        }

        private void cbLayerFilters_SelectedIndexChanged(object sender, EventArgs e)
        {
            int FilterIndex = cbLayerFilters.SelectedIndex;
           
            SelectedLayer?.SetFilter(Filters[FilterIndex]);
            switch (cbLayerFilters.SelectedItem.ToString())
            {
                case "Subtract":
                    tcLayerModeOperation.SelectedIndex = 1;
                    break;
                default:
                    tcLayerModeOperation.SelectedIndex = 0;
                    break;
            }
        }

        private void saveImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImage();
        }

        private void pnlHistogramVisual_Paint(object sender, PaintEventArgs e)
        {
            int SelectedLayer = cbHistogramTarget.SelectedIndex;
            int[] data;

            if (SelectedLayer == -1) return;
            else if (false) // TODO: Add a whole layer histogram
            {
                // Set to all
                data = Histogram.Grey(Layer.FlatImage(CanvasSize));
            }
            else
            {
                // Set to specific layer
                if (SelectedLayer >= Layer.layers.Count) return;
                data = Histogram.Grey(Layer.layers[SelectedLayer]?.processed);
            }

            Histogram.Draw(e.Graphics, pnlHistogramVisual.Size, data);
        }

        private void cbHistogramTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlHistogramVisual.Invalidate();
        }

        private void btnBrushColor_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog.ShowDialog();
            BrushTool.BrushColor = colorDialog.Color;
            btnBrushColor.BackColor = colorDialog.Color;
        }

        private void btnArrangeUp_Click(object sender, EventArgs e)
        {
            if(SelectedLayerIndex == 0) return;
            Layer layer = SelectedLayer;
            Layer.layers.RemoveAt(SelectedLayerIndex--);
            Layer.layers.Insert(SelectedLayerIndex, layer);
            RefreshLayerList();
        }

        private void btnArrangeDown_Click(object sender, EventArgs e)
        {
            if (SelectedLayerIndex == Layer.layers.Count) return;
            Layer layer = SelectedLayer;
            Layer.layers.RemoveAt(SelectedLayerIndex++);
            Layer.layers.Insert(SelectedLayerIndex, layer);
            RefreshLayerList();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (Layer.layers.Count <= 1) return;

            Layer l = Layer.layers[SelectedLayerIndex];

            if (l == null) return;
            if (l is DynamicLayer) hasCamera = false;

            Layer.layers.RemoveAt(SelectedLayerIndex--);
            SelectedLayerIndex = lbLayers.Items.Count - 1;

            RefreshLayerList();
        }

        private void inpWidth_KeyDown(object sender, KeyEventArgs e)
        {
            Util.Control.OnlyAcceptNumbers(e);;
        }

        private void inpHeight_KeyDown(object sender, KeyEventArgs e)
        {
            Util.Control.OnlyAcceptNumbers(e);
        }

        private void btnMaintainRatio_MouseDown(object sender, MouseEventArgs e)
        {
            MaintainRatio = !MaintainRatio;
            btnMaintainRatio.Checked = MaintainRatio;
        }

        bool MRChange = false;

        private void inpWidth_TextChanged(object sender, EventArgs e)
        {
            string t = inpWidth.Text == "" ? "0" : inpWidth.Text;


            int value = Util.Clip.Integer(int.Parse(t), 1, null);

            float ratio = (float)CanvasSize.Height / (float)CanvasSize.Width;

            CanvasSize.Width = value;

            if (MaintainRatio && !MRChange)
            {
                MRChange = true;
                inpHeight.Text = ((int)((float)value / ratio)).ToString();
            }
            MRChange = false;

            if (inpWidth.Text == value.ToString()) return;
            inpWidth.Text = value.ToString();
        }

        private void inpHeight_TextChanged(object sender, EventArgs e)
        {
            string t = inpHeight.Text == "" ? "0" : inpHeight.Text;

            int value = Util.Clip.Integer(int.Parse(t), 1, null);

            float ratio = (float)CanvasSize.Height / (float)CanvasSize.Width;

            CanvasSize.Height = value;

            if (MaintainRatio && !MRChange)
            {
                MRChange = true;
                inpWidth.Text = ((int)((float)value * ratio)).ToString();
            }
            MRChange = false;

            if (inpHeight.Text == value.ToString()) return;
            inpHeight.Text = value.ToString();
        }

        private void toolStrip5_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        // Code for changing the subtraction color
        private void btnSubtractColor_Click(object sender, EventArgs e)
        {
            DialogResult result = subtractColorDialog.ShowDialog();
            LayerFilterOptions.SubtractColor = subtractColorDialog.Color;
            btnSubtractColor.BackColor = subtractColorDialog.Color;

            // Re apply filter
            int FilterIndex = cbLayerFilters.SelectedIndex;
            SelectedLayer?.SetFilter(Filters[FilterIndex]);
        }

        private void tbTolerance_KeyDown(object sender, KeyEventArgs e)
        {
            Util.Control.OnlyAcceptNumbers(e);
        }

        // Code fore changing the tolerance value of the subtraction filter
        private void tbTolerance_TextChanged(object sender, EventArgs e)
        {

            string t = tbTolerance.Text == "" ? "0" : tbTolerance.Text;

            int value = Util.Clip.Integer(int.Parse(t), 0, 100);

            LayerFilterOptions.SubtractTolerance = (float)value / 100f;

            // Re apply filter
            int FilterIndex = cbLayerFilters.SelectedIndex;
            SelectedLayer?.SetFilter(Filters[FilterIndex]);

            if (tbTolerance.Text == value.ToString()) return;
            tbTolerance.Text = value.ToString();
        }

        Boolean hasCamera = false;
        private void btnAddDynamic_Click(object sender, EventArgs e)
        {
            if (hasCamera) return;

            new DynamicLayer(this);
            RefreshLayerList();

            btnAddDynamic.Enabled = false;
        }

        public void RefreshDeviceList()
        {

            // Refresh Video Device List
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                cbDeviceList.Items.Clear();
                if (videoDevices.Count == 0) return;

                foreach (FilterInfo d in videoDevices)  cbDeviceList.Items.Add(d.Name);
            }
            catch (ApplicationException)
            {
                // haha nothing
            }
        }

        private void cbDeviceList_Click(object sender, EventArgs e)
        {
            RefreshDeviceList();
        }
    }
}

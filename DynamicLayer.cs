using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;

namespace ImageProcessingActivity
{
    public class DynamicLayer : Layer
    {
        public bool isImageCloned = false;

        public DynamicLayer(Main m) : base(Main.CanvasSize, m)
        {
            name = "Camera Layer";
        }

        public void setDevice(VideoCaptureDevice videoSource)
        {

            if(videoSource == null)
            {
                Console.WriteLine("Video source passed is null");
                return;
            }

            try
            {

                videoSource.NewFrame += new NewFrameEventHandler(syncWithImage);
                Console.WriteLine("Successfully added handling camera event");
                videoSource.Start();
                Console.WriteLine("Successfully started video source capture");
            }
            catch
            {

            }
        }

        public void syncWithImage(object sender, NewFrameEventArgs e)
        {
            if (isImageUsed) return;

            // Set image
            //if (image != null) image.Dispose();
            isImageCloned = true;
            image = (Bitmap)e.Frame.Clone();
            isImageCloned = false;
        }


        public override void Draw(Graphics g, RectangleF CanvasBounds)
        {
            // Apply filter first
            ApplyFilter(true);
            base.Draw(g, CanvasBounds);
        }

        public override void ApplyFilter(bool fromDraw = false)
        {
            if (isImageCloned) return;
            if (!fromDraw) return;
            base.ApplyFilter(false);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Video.DirectShow;

namespace ImageProcessingActivity
{
    internal class DynamicLayer : Layer
    {
        VideoCapabilities videoSource = null;

        public DynamicLayer(Main m) : base(Main.CanvasSize, m)
        {
            name = "Camera Layer";
        }
    }
}

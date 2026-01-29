using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PureGate.Property;
using OpenCvSharp.Extensions;
using System.Runtime.InteropServices;
using PureGate.Teach;

namespace PureGate.Core
{
    public class PreviewImage
    {
        private Mat _orignalImage = null;
        private Mat _previewImage = null;

        private InspWindow _inspWindow = null;
        private bool _usePreview = true;

        public void SetImage(Mat image)
        {
            _orignalImage = image;
            _previewImage = new Mat();
        }

        public void SetInspWindow(InspWindow inspwindow)
        {
            _inspWindow = inspwindow;
        }
    }
}

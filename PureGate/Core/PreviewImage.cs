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

        public void SetBinary(int lowerValue, int upperValue, bool invert, ShowBinaryMode showBinMode)
        {
            if (_usePreview == false)
                return;
            if (_orignalImage == null)
                return;

            var cameraForm = MainForm.GetDockForm<CameraForm>();
            if (cameraForm == null)
                return;

            Bitmap bmpImage;
            if (showBinMode == ShowBinaryMode.ShowBinaryNone)
            {
                bmpImage = BitmapConverter.ToBitmap(_orignalImage);
                cameraForm.UpdateDisplay(bmpImage);
                return;
            }

            Rect windowArea = new Rect(0, 0, _orignalImage.Width, _orignalImage.Height);

            if(_inspWindow != null)
            {
                windowArea = _inspWindow.WindowArea;
            }

            Mat orgRoi = _orignalImage[windowArea];

            Mat grayImage = new Mat();
            if (orgRoi.Type() == MatType.CV_8UC3)
                Cv2.CvtColor(orgRoi, grayImage, ColorConversionCodes.BGR2GRAY);
            else
                grayImage = orgRoi;

            Mat binaryMask = new Mat();
            Cv2.InRange(grayImage, lowerValue, upperValue, binaryMask);

            if (invert)
                binaryMask = ~binaryMask;

            Mat fullBinaryMask = Mat.Zeros(_orignalImage.Size(), MatType.CV_8UC1);
            binaryMask.CopyTo(new Mat(fullBinaryMask, windowArea));

            if (showBinMode == ShowBinaryMode.ShowBinaryOnly)
            {
                if (orgRoi.Type() == MatType.CV_8UC3)
                {
                    Mat colorBinary = new Mat();
                    Cv2.CvtColor(binaryMask, colorBinary, ColorConversionCodes.GRAY2BGR);
                    _previewImage = _orignalImage.Clone();
                    colorBinary.CopyTo(new Mat(_previewImage, windowArea));
                }
                else
                {
                    _previewImage = _orignalImage.Clone();
                    binaryMask.CopyTo(new Mat(_previewImage, windowArea));
                }

                bmpImage = BitmapConverter.ToBitmap(_previewImage);
                cameraForm.UpdateDisplay(bmpImage);
                return;
            }

            Scalar highlightColor;
            if (showBinMode == ShowBinaryMode.ShowBinaryHighlightRed)
                highlightColor = new Scalar(0, 0, 255);
            else if (showBinMode == ShowBinaryMode.ShowBinaryHighlightGreen)
                highlightColor = new Scalar(0, 255, 0);
            else
                highlightColor = new Scalar(255, 0, 0);

            Mat overlayImage;
            if (_orignalImage.Type() == MatType.CV_8UC1)
            {
                overlayImage = new Mat();
                Cv2.CvtColor(_orignalImage, overlayImage, ColorConversionCodes.GRAY2BGR);

                Mat colorOrignal = overlayImage.Clone();

                overlayImage.SetTo(highlightColor, fullBinaryMask);

                Cv2.AddWeighted(colorOrignal, 0.7, overlayImage, 0.3, 0, _previewImage);
            }
            else
            {
                overlayImage = _orignalImage.Clone();
                overlayImage.SetTo(highlightColor, fullBinaryMask);

                Cv2.AddWeighted(_orignalImage, 0.7, overlayImage, 0.3, 0, _previewImage);
            }

            bmpImage = BitmapConverter.ToBitmap(_previewImage);
            cameraForm.UpdateDisplay(bmpImage);
        }
    }
}

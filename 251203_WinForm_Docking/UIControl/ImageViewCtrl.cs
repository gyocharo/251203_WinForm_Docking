using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using _251203_WinForm_Docking.Algorithm;
using _251203_WinForm_Docking.Core;

namespace _251203_WinForm_Docking.UIControl
{
    public partial class ImageViewCtrl : UserControl
    {
        private Bitmap _bitmapImage = null;

        private Bitmap Canvas = null;

        private RectangleF ImageRect = new RectangleF(0, 0, 0, 0);

        //현재 줌 배율
        private float _curZoom = 1.0f;
        // 줌 배율 변경 시 확대/축소 단위
        private float _zoomFactor = 1.1f;

        //줌 최소/최대값 제한
        private float MinZoom = 1.0f;
        private const float MaxZoom = 100.0f;

        private bool _isInitialized = false;

        private List<DrawInspectInfo> _rectInfos = new List<DrawInspectInfo>();

        public ImageViewCtrl()
        {
            InitializeComponent();
            InitializeCanvas();

            MouseWheel += new MouseEventHandler(ImageViewCtrl_MouseWheel);
        }

        // 더블버퍼링
        private void InitializeCanvas()
        {
            ResizeCanvas();

            DoubleBuffered = true;
        }

        private void ResizeCanvas()
        {
            if (Width <= 0 || Height <= 0 || _bitmapImage == null)
                return;

            Canvas = new Bitmap(Width, Height);
            if (Canvas == null)
                return;

            float virtualWidth = _bitmapImage.Width * _curZoom;
            float virtualHeight = _bitmapImage.Height * _curZoom;

            float offsetX = virtualWidth < Width ? (Width - virtualWidth) / 2f : 0f;
            float offsetY = virtualHeight < Height ? (Height - virtualHeight) / 2f : 0f;

            ImageRect = new RectangleF(offsetX, offsetY, virtualWidth, virtualHeight);
        }

        private void FitImageToScreen()
        {
            RecalcZoomRatio();

            float NewWidth = _bitmapImage.Width * _curZoom;
            float NewHeight = _bitmapImage.Height * _curZoom;

            ImageRect = new RectangleF((Width - NewWidth) / 2, (Height - NewHeight) / 2, NewWidth, NewHeight);
        }

        private void RecalcZoomRatio()
        {
            if (_bitmapImage == null || Width <= 0 || Height <= 0)
                return;

            Size imageSize = new Size(_bitmapImage.Width, _bitmapImage.Height);

            float aspectRatio = (float)imageSize.Height / (float)imageSize.Width;
            float clinetAspect = (float)Height / (float)Width;

            float ratio;
            if (aspectRatio <= clinetAspect)
                ratio = (float)Width / (float)imageSize.Width;
            else
                ratio = (float)Height / (float)imageSize.Height;

            float minZoom = ratio;

            MinZoom = minZoom;

            _curZoom = Math.Max(MinZoom, Math.Min(MaxZoom, ratio));

            Invalidate();
        }

        public void LoadBitmap(Bitmap bitmap)
        {
            if (_bitmapImage != null)
            {
                if (_bitmapImage.Width == bitmap.Width && _bitmapImage.Height == bitmap.Height)
                {
                    _bitmapImage = bitmap;
                    Invalidate();
                    return;
                }

                _bitmapImage.Dispose();
                _bitmapImage = null;
            }

            _bitmapImage = bitmap;

            if (_isInitialized == false)
            {
                _isInitialized = true;
                ResizeCanvas();
            }

            FitImageToScreen();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_bitmapImage != null && Canvas != null)
            {
                using (Graphics g = Graphics.FromImage(Canvas))
                {
                    g.Clear(Color.Transparent);

                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImage(_bitmapImage, ImageRect);

                    DrawDiagram(g);

                    e.Graphics.DrawImage(Canvas, 0, 0);
                }
            }
        }

        private void DrawDiagram(Graphics g)
        {
            if (_rectInfos != null)
            {
                foreach (DrawInspectInfo rectInfo in _rectInfos)
                {
                    Color lineColor = Color.LightCoral;
                    if (rectInfo.decision == DecisionType.Defect)
                        lineColor = Color.Red;
                    else if (rectInfo.decision == DecisionType.Good)
                        lineColor = Color.LightGreen;

                    Rectangle rect = new Rectangle(rectInfo.rect.X, rectInfo.rect.Y, rectInfo.rect.Width, rectInfo.rect.Height);
                    Rectangle screenRect = VirtualToScreen(rect);

                    using (Pen pen = new Pen(lineColor, 2))
                    {
                        if (rectInfo.UseRotatedRect)
                        {
                            PointF[] screenPoints = rectInfo.rotatedPoints
                                                    .Select(p => VirtualToScreen(new PointF(p.X, p.Y)))
                                                    .ToArray();

                            if (screenPoints.Length == 4)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    g.DrawLine(pen, screenPoints[i], screenPoints[(i + 1) % 4]);
                                }
                            }
                        }
                        else
                        {
                            g.DrawRectangle(pen, screenRect);
                        }
                    }

                    if (rectInfo.info != "")
                    {
                        float baseFontSize = 20.0f;

                        if (rectInfo.decision == DecisionType.Info)
                        {
                            baseFontSize = 3.0f;
                            lineColor = Color.LightBlue;
                        }

                        float fontSize = baseFontSize * _curZoom;

                        string infoText = rectInfo.info;
                        PointF textPos = new PointF(screenRect.Left, screenRect.Top);

                        if (rectInfo.inspectType == InspectType.InspBinary
                            && rectInfo.decision != DecisionType.Info)
                        {
                            textPos.Y = screenRect.Bottom - fontSize;
                        }

                        DrawText(g, infoText, textPos, fontSize, lineColor);
                    }
                }
            }
        }

        private void DrawText(Graphics g, string text, PointF position, float fontSize, Color color)
        {
            using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
            using (Brush outlineBrush = new SolidBrush(Color.Black))
            using (Brush textBrush = new SolidBrush(color))
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        PointF borderPos = new PointF(position.X + dx, position.Y + dy);
                        g.DrawString(text, font, outlineBrush, borderPos);
                    }
                }

                g.DrawString(text, font, textBrush, position);
            }
        }

        private void ZoomMove(float zoom, Point zoomOrigin)
        {
            PointF virtualOrigin = ScreenToVirtual(new PointF(zoomOrigin.X, zoomOrigin.Y));

            _curZoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
            if (_curZoom <= MinZoom)
                return;

            PointF zoomedOrigin = VirtualToScreen(virtualOrigin);

            float dx = zoomedOrigin.X - zoomOrigin.X;
            float dy = zoomedOrigin.Y - zoomOrigin.Y;

            ImageRect.X -= dx;
            ImageRect.Y -= dy;
        }

        private void ImageViewCtrl_DoubleClick(object sender, EventArgs e)
        {
            FitImageToScreen();
        }

        private void ImageViewCtrl_Resize(object sender, EventArgs e)
        {
            ResizeCanvas();
            Invalidate();
        }

        private PointF GetScreenOffset()
        {
            return new PointF(ImageRect.X, ImageRect.Y);
        }

        private Rectangle ScreenToVirtual(Rectangle screenRect)
        {
            PointF offset = GetScreenOffset();
            return new Rectangle(
                (int)((screenRect.X - offset.X) / _curZoom + 0.5f),
                (int)((screenRect.Y - offset.Y) / _curZoom + 0.5f),
                (int)(screenRect.Width / _curZoom + 0.5f),
                (int)(screenRect.Height / _curZoom + 0.5f));
        }

        private Rectangle VirtualToScreen(Rectangle virtualRect)
        {
            PointF offset = GetScreenOffset();
            return new Rectangle(
                (int)(virtualRect.X * _curZoom + offset.X + 0.5f),
                (int)(virtualRect.Y * _curZoom + offset.Y + 0.5f),
                (int)(virtualRect.Width * _curZoom + 0.5f),
                (int)(virtualRect.Height * _curZoom + 0.5f));
        }

        private PointF ScreenToVirtual(PointF screenPos)
        {
            PointF offset = GetScreenOffset();
            return new PointF(
                (screenPos.X - offset.X) / _curZoom,
                (screenPos.Y - offset.Y) / _curZoom);
        }

        private PointF VirtualToScreen(PointF virtualPos)
        {
            PointF offset = GetScreenOffset();
            return new PointF(
                virtualPos.X * _curZoom + offset.X,
                virtualPos.Y * _curZoom + offset.Y);
        }

        private void ImageViewCtrl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta < 0)
                ZoomMove(_curZoom / _zoomFactor, e.Location);
            else
                ZoomMove(_curZoom * _zoomFactor, e.Location);

            if (_bitmapImage != null)
            {
                ImageRect.Width = _bitmapImage.Width * _curZoom;
                ImageRect.Height = _bitmapImage.Height * _curZoom;
            }
            Invalidate();
        }
        public Bitmap GetCurBitmap()
        {
            return _bitmapImage;
        }

        public void AddRect(List<DrawInspectInfo> rectInfos)
        {
            _rectInfos.AddRange(rectInfos);
            Invalidate();
        }

        public void ResetEntity()
        {
            _rectInfos.Clear();
            Invalidate();
        }
    }
}

using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PureGate.Util
{
    public static class BitmapConverter
    {
        /// <summary>
        /// OpenCvSharp Mat -> System.Drawing.Bitmap (Deep Copy)
        /// 지원: CV_8U 1/3/4채널
        /// </summary>
        public static Bitmap ToBitmap(Mat src)
        {
            if (src == null) throw new ArgumentNullException(nameof(src));
            if (src.Empty()) throw new ArgumentException("src is empty.", nameof(src));

            Mat mat = src;

            // Depth가 8U가 아니면 표시용으로 8U로 변환(정규화)
            Mat tmpDepth = null;
            if (mat.Depth() != MatType.CV_8U)
            {
                tmpDepth = new Mat();
                using (var normalized = new Mat())
                {
                    Cv2.Normalize(mat, normalized, 0, 255, NormTypes.MinMax);
                    normalized.ConvertTo(tmpDepth, MatType.CV_8U);
                }
                mat = tmpDepth;
            }

            // 채널 처리: 1/3/4만 지원, 그 외는 변환 시도
            Mat tmpCh = null;
            int ch = mat.Channels();

            if (ch == 2)
            {
                // 2채널이면 첫 채널만 뽑아 1채널로
                tmpCh = new Mat();
                Cv2.ExtractChannel(mat, tmpCh, 0);
                mat = tmpCh;
                ch = 1;
            }
            else if (ch > 4)
            {
                // 5채널 이상이면 앞 3채널만 병합해 3채널(BGR)로
                Mat c0 = new Mat(); Mat c1 = new Mat(); Mat c2 = new Mat();
                tmpCh = new Mat();
                try
                {
                    Cv2.ExtractChannel(mat, c0, 0);
                    Cv2.ExtractChannel(mat, c1, 1);
                    Cv2.ExtractChannel(mat, c2, 2);
                    Cv2.Merge(new[] { c0, c1, c2 }, tmpCh);
                    mat = tmpCh;
                    ch = 3;
                }
                finally
                {
                    c0.Dispose(); c1.Dispose(); c2.Dispose();
                }
            }

            PixelFormat pf = PixelFormat.Format24bppRgb;

            // Bitmap 생성
            Bitmap bmp = new Bitmap(mat.Width, mat.Height, pf);

            // 8bpp 팔레트(그레이)
            if (pf == PixelFormat.Format8bppIndexed)
            {
                var pal = bmp.Palette;
                for (int i = 0; i < 256; i++)
                    pal.Entries[i] = Color.FromArgb(i, i, i);
                bmp.Palette = pal;
            }

            // 연속 메모리 보장
            Mat tmpCont = null;
            if (!mat.IsContinuous())
            {
                tmpCont = mat.Clone();
                mat = tmpCont;
            }

            BitmapData bd = null;
            try
            {
                bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                  ImageLockMode.WriteOnly, pf);

                int bytesPerPixel = (pf == PixelFormat.Format8bppIndexed) ? 1 :
                                    (pf == PixelFormat.Format24bppRgb) ? 3 : 4;

                int srcStride = (int)mat.Step();
                int dstStride = bd.Stride;
                int rowBytes = mat.Width * bytesPerPixel;

                // 행 단위 복사(Stride 차이 안전)
                byte[] row = new byte[rowBytes];
                for (int y = 0; y < mat.Height; y++)
                {
                    IntPtr srcPtr = mat.Data + y * srcStride;
                    IntPtr dstPtr = bd.Scan0 + y * dstStride;

                    Marshal.Copy(srcPtr, row, 0, rowBytes);
                    Marshal.Copy(row, 0, dstPtr, rowBytes);
                }

                return bmp;
            }
            catch
            {
                bmp.Dispose();
                throw;
            }
            finally
            {
                if (bd != null) bmp.UnlockBits(bd);
                tmpCont?.Dispose();
                tmpCh?.Dispose();
                tmpDepth?.Dispose();
            }
        }

        public static Mat ToMat(Bitmap bmp)
        {
            if (bmp == null)
                throw new ArgumentNullException(nameof(bmp));

            // Bitmap이 인덱스/팔레트 등 특이 포맷이면 변환 후 처리
            PixelFormat pf = bmp.PixelFormat;

            // 처리 가능한 포맷으로 맞추기 (안전)
            if (pf != PixelFormat.Format8bppIndexed &&
                pf != PixelFormat.Format24bppRgb &&
                pf != PixelFormat.Format32bppArgb &&
                pf != PixelFormat.Format32bppRgb &&
                pf != PixelFormat.Format32bppPArgb)
            {
                var converted = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(converted))
                {
                    g.DrawImage(bmp, new Rectangle(0, 0, converted.Width, converted.Height));
                }
                return ToMat(converted);
            }

            // 8bpp Indexed는 팔레트 기반이라 그대로 읽되 1채널로 간주
            int channels =
                pf == PixelFormat.Format8bppIndexed ? 1 :
                pf == PixelFormat.Format24bppRgb ? 3 : 4;

            var mat = new Mat(bmp.Height, bmp.Width, channels == 1 ? MatType.CV_8UC1 :
                                               channels == 3 ? MatType.CV_8UC3 :
                                                               MatType.CV_8UC4);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bd = null;

            try
            {
                bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, pf);

                int srcStride = bd.Stride;
                int dstStride = (int)mat.Step();
                int bytesPerPixel = channels; // 1,3,4
                int rowBytes = bmp.Width * bytesPerPixel;

                // 행 단위로 안전 복사 (Stride 차이 고려)
                byte[] row = new byte[rowBytes];

                for (int y = 0; y < bmp.Height; y++)
                {
                    IntPtr srcPtr = bd.Scan0 + y * srcStride;
                    IntPtr dstPtr = mat.Data + y * dstStride;

                    Marshal.Copy(srcPtr, row, 0, rowBytes);
                    Marshal.Copy(row, 0, dstPtr, rowBytes);
                }
            }
            finally
            {
                if (bd != null)
                    bmp.UnlockBits(bd);
            }

            // (중요) 32bppPArgb는 미리 곱(Premultiplied) 알파일 수 있음
            // 표시/연산에서 문제되면 여기서 별도 보정 필요.
            // 대부분의 비전 처리에서는 BGRA로 그대로 써도 큰 문제는 없음.

            return mat;
        }
    }
}

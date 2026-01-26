using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureGate.Util;

namespace PureGate.UIControl
{
    public partial class RecentNGimages : UserControl
    {
        private FlowLayoutPanel flowThumbnails;
        private Label lblTitle;
        private System.Windows.Forms.Timer timerRefresh;
        private const int MAX_THUMBNAILS = 10;
        private const int THUMBNAIL_SIZE = 80;
        private const string NG_ROOT_PATH = @"D:\NG";

        public RecentNGimages()
        {
            InitializeComponent();
            InitializeNGThumbnailUI();
            StartAutoRefresh();
        }

        private void InitializeNGThumbnailUI()
        {
            // 상단 제목 레이블
            lblTitle = new Label
            {
                Text = "최근 불량 (NG) 이미지",
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.Red,
                AutoSize = false,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Dock = DockStyle.Top
            };

            // 썸네일을 표시할 FlowLayoutPanel
            flowThumbnails = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(5),
                WrapContents = true
            };

            // 컨트롤 추가
            this.Controls.Add(flowThumbnails);
            this.Controls.Add(lblTitle);

            // 초기 로드
            LoadRecentNGImages();
        }

        private void StartAutoRefresh()
        {
            // 5초마다 자동 갱신
            timerRefresh = new System.Windows.Forms.Timer
            {
                Interval = 5000
            };
            timerRefresh.Tick += (s, e) => LoadRecentNGImages();
            timerRefresh.Start();
        }

        private void LoadRecentNGImages()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(LoadRecentNGImages));
                return;
            }

            try
            {
                // 기존 썸네일 제거
                foreach (Control ctrl in flowThumbnails.Controls)
                {
                    if (ctrl is PictureBox pb && pb.Image != null)
                    {
                        pb.Image.Dispose();
                    }
                    ctrl.Dispose();
                }
                flowThumbnails.Controls.Clear();

                // NG 폴더가 없으면 종료
                if (!Directory.Exists(NG_ROOT_PATH))
                {
                    AddNoImageLabel();
                    return;
                }

                // 모든 NG 이미지 파일 수집 (최신순)
                var imageFiles = Directory.GetFiles(NG_ROOT_PATH, "*.jpg", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .Take(MAX_THUMBNAILS)
                    .ToList();

                if (imageFiles.Count == 0)
                {
                    AddNoImageLabel();
                    return;
                }

                // 썸네일 생성
                foreach (var fileInfo in imageFiles)
                {
                    AddThumbnail(fileInfo);
                }
            }
            catch (Exception ex)
            {
                SLogger.Write($"[RecentNGimages] NG 이미지 로드 실패: {ex.Message}", SLogger.LogType.Error);
            }
        }

        private void AddNoImageLabel()
        {
            Label lblNoImage = new Label
            {
                Text = "불량 이미지가 없습니다.",
                ForeColor = Color.Gray,
                AutoSize = true,
                Font = new Font("맑은 고딕", 9F),
                Padding = new Padding(10)
            };
            flowThumbnails.Controls.Add(lblNoImage);
        }

        private void AddThumbnail(FileInfo fileInfo)
        {
            Panel thumbnailPanel = new Panel
            {
                Width = THUMBNAIL_SIZE + 10,
                Height = THUMBNAIL_SIZE + 35,
                Margin = new Padding(5),
                BackColor = Color.FromArgb(60, 60, 65),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 썸네일 이미지
            PictureBox pictureBox = new PictureBox
            {
                Width = THUMBNAIL_SIZE,
                Height = THUMBNAIL_SIZE,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(5, 5),
                Cursor = Cursors.Hand,
                BackColor = Color.Black
            };

            // 시간 정보 레이블
            Label lblTime = new Label
            {
                Text = fileInfo.LastWriteTime.ToString("HH:mm:ss"),
                ForeColor = Color.White,
                AutoSize = false,
                Width = THUMBNAIL_SIZE,
                Height = 20,
                Location = new Point(5, THUMBNAIL_SIZE + 8),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 7F)
            };

            // 이미지 로드 (비동기)
            Task.Run(() =>
            {
                try
                {
                    using (var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Image img = Image.FromStream(fs);
                        Image thumbnail = CreateThumbnail(img, THUMBNAIL_SIZE, THUMBNAIL_SIZE);
                        img.Dispose();

                        if (pictureBox.IsDisposed) return;

                        pictureBox.Invoke(new Action(() =>
                        {
                            if (!pictureBox.IsDisposed)
                            {
                                pictureBox.Image = thumbnail;
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    SLogger.Write($"[RecentNGimages] 썸네일 로드 실패: {ex.Message}", SLogger.LogType.Error);
                }
            });

            // 클릭 시 원본 이미지 표시
            pictureBox.Click += (s, e) => ShowFullImage(fileInfo.FullName);
            pictureBox.Tag = fileInfo.FullName; // 전체 경로 저장

            // ToolTip 추가
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(pictureBox, $"{Path.GetFileName(fileInfo.FullName)}\n{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

            thumbnailPanel.Controls.Add(pictureBox);
            thumbnailPanel.Controls.Add(lblTime);
            flowThumbnails.Controls.Add(thumbnailPanel);
        }

        private Image CreateThumbnail(Image original, int width, int height)
        {
            Bitmap thumbnail = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(thumbnail))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, width, height);
            }
            return thumbnail;
        }

        private void ShowFullImage(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    MessageBox.Show("이미지 파일을 찾을 수 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 새 창에서 원본 이미지 표시
                Form imageForm = new Form
                {
                    Text = $"NG 이미지 - {Path.GetFileName(imagePath)}",
                    Width = 800,
                    Height = 600,
                    StartPosition = FormStartPosition.CenterScreen
                };

                PictureBox pbFull = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Black
                };

                using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    pbFull.Image = Image.FromStream(fs);
                }

                imageForm.Controls.Add(pbFull);
                imageForm.FormClosed += (s, e) =>
                {
                    if (pbFull.Image != null)
                    {
                        pbFull.Image.Dispose();
                    }
                };

                imageForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"이미지를 표시할 수 없습니다.\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanupResources()
        {
            if (timerRefresh != null)
            {
                timerRefresh.Stop();
                timerRefresh.Dispose();
                timerRefresh = null;
            }

            // 모든 썸네일 이미지 해제
            if (flowThumbnails != null)
            {
                foreach (Control ctrl in flowThumbnails.Controls)
                {
                    if (ctrl is PictureBox pb && pb.Image != null)
                    {
                        pb.Image.Dispose();
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupResources();
            }
            base.Dispose(disposing);
        }
    }
}

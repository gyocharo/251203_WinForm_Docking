using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace PureGate
{
    public partial class TitleForm : DockContent
    {
        public TitleForm()
        {
            InitializeComponent();
            SetupQuickRangeUI();
        }

        private void ShowResult(DateTime start, DateTime end)
        {
            string project = "PureGate"; 
            SummaryForm resultForm = new SummaryForm(project, start, end);
            resultForm.ShowDialog();
        }

        private void lblToday_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today, DateTime.Today);
        }

        private void lbl1Week_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today.AddDays(-6), DateTime.Today);
        }

        private void lbl1Month_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today.AddMonths(-1).AddDays(1), DateTime.Today);
        }

        private void lbl1Year_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today.AddYears(-1).AddDays(1), DateTime.Today);
        }

        private void btnShowRange_Click(object sender, EventArgs e)
        {
            // 1. 시작/끝 날짜 가져오기
            DateTime start = dtpStart.Value.Date;
            DateTime end = dtpEnd.Value.Date;

            // 2. 날짜 범위 유효성 체크 (끝날짜가 시작날짜보다 빠르면 경고)
            if (end < start)
            {
                MessageBox.Show("끝 날짜는 시작 날짜보다 빠를 수 없습니다.", "날짜 범위 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. 프로젝트 선택값 가져오기 (ComboBox가 있다면)
            string project = "PureGate";

            // 4. SummaryForm 생성 후 보여주기
            SummaryForm summaryForm = new SummaryForm(project, start, end);
            summaryForm.Show();
        }

        private void SetupQuickRangeUI()
        {
            // ===== Color Set (모던 블루) =====
            Color border = Color.FromArgb(220, 225, 235);
            Color normalBg = Color.White;
            Color hoverBg = Color.FromArgb(235, 240, 250);
            Color selectedBg = Color.FromArgb(220, 230, 250);
            Color text = Color.FromArgb(50, 60, 80);

            tlpLinklabels.SuspendLayout();

            // 전체 박스 스타일
            tlpLinklabels.BackColor = border;     // 테두리/구분선 색
            tlpLinklabels.Padding = new Padding(1);
            tlpLinklabels.ColumnCount = 2;
            tlpLinklabels.RowCount = 2;

            tlpLinklabels.ColumnStyles.Clear();
            tlpLinklabels.RowStyles.Clear();
            tlpLinklabels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpLinklabels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpLinklabels.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpLinklabels.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // 링크들 다시 배치
            tlpLinklabels.Controls.Clear();
            tlpLinklabels.Controls.Add(lblToday, 0, 0);
            tlpLinklabels.Controls.Add(lbl1Week, 1, 0);
            tlpLinklabels.Controls.Add(lbl1Month, 0, 1);
            tlpLinklabels.Controls.Add(lbl1Year, 1, 1);

            // 각 링크를 '버튼'처럼 꾸미기
            StyleQuickLink(lblToday);
            StyleQuickLink(lbl1Week);
            StyleQuickLink(lbl1Month);
            StyleQuickLink(lbl1Year);

            // 셀 사이 구분선(가운데 십자) : 1px Panel 2개
            var v = MakeLinePanel(); // vertical
            var h = MakeLinePanel(); // horizontal
            tlpLinklabels.Controls.Add(v);
            tlpLinklabels.Controls.Add(h);
            v.BringToFront();
            h.BringToFront();

            void RepositionLines()
            {
                int innerW = tlpLinklabels.ClientSize.Width - tlpLinklabels.Padding.Horizontal;
                int innerH = tlpLinklabels.ClientSize.Height - tlpLinklabels.Padding.Vertical;

                int xMid = tlpLinklabels.Padding.Left + innerW / 2;
                int yMid = tlpLinklabels.Padding.Top + innerH / 2;

                v.Bounds = new Rectangle(xMid, tlpLinklabels.Padding.Top, 1, innerH);
                h.Bounds = new Rectangle(tlpLinklabels.Padding.Left, yMid, innerW, 1);
            }

            tlpLinklabels.SizeChanged += (s, e) => RepositionLines();
            RepositionLines();

            // 기본 선택
            SetSelected(lblToday);

            // 클릭 시 선택 표시 변경 (기존 LinkClicked 핸들러는 그대로 실행됨)
            lblToday.LinkClicked += (s, e) => SetSelected(lblToday);
            lbl1Week.LinkClicked += (s, e) => SetSelected(lbl1Week);
            lbl1Month.LinkClicked += (s, e) => SetSelected(lbl1Month);
            lbl1Year.LinkClicked += (s, e) => SetSelected(lbl1Year);

            tlpLinklabels.ResumeLayout(true);

            // ---- local helpers ----
            Panel MakeLinePanel()
            {
                return new Panel
                {
                    BackColor = border
                };
            }

            void StyleQuickLink(LinkLabel ll)
            {
                ll.AutoSize = false;
                ll.UseCompatibleTextRendering = true;
                ll.Dock = DockStyle.Fill;
                ll.Margin = new Padding(0);
                ll.Padding = new Padding(6, 2, 6, 2);
                ll.TextAlign = ContentAlignment.MiddleCenter;
                ll.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);

                // 링크 밑줄 제거 + 텍스트 컬러 통일
                ll.LinkBehavior = LinkBehavior.NeverUnderline;
                ll.LinkVisited = false;
                ll.ActiveLinkColor = text;
                ll.LinkColor = text;
                ll.VisitedLinkColor = text;

                ll.BackColor = normalBg;

                if (ll == lbl1Month)
                    ll.Padding = new Padding(2, 2, 2, 2);
                else
                    ll.Padding = new Padding(6, 2, 6, 2);

                // Hover 효과
                ll.MouseEnter += (s, e) =>
                {
                    if (!Equals(ll.Tag, "selected"))
                        ll.BackColor = hoverBg;
                };
                ll.MouseLeave += (s, e) =>
                {
                    if (!Equals(ll.Tag, "selected"))
                        ll.BackColor = normalBg;
                };
            }

            void SetSelected(LinkLabel selected)
            {
                LinkLabel[] all = { lblToday, lbl1Week, lbl1Month, lbl1Year };
                foreach (var ll in all)
                {
                    ll.Tag = null;
                    ll.Font = new Font(ll.Font.FontFamily, ll.Font.Size, FontStyle.Regular);
                    ll.BackColor = normalBg;

                    ll.LinkVisited = false;
                    ll.ActiveLinkColor = text;
                    ll.LinkColor = text;
                    ll.VisitedLinkColor = text;
                }

                selected.Tag = "selected";
                selected.Font = new Font(selected.Font.FontFamily, selected.Font.Size, FontStyle.Bold);
                selected.BackColor = selectedBg;

                selected.LinkVisited = false;
                selected.ActiveLinkColor = text;
                selected.LinkColor = text;
                selected.VisitedLinkColor = text;
            }
        }
    }
}

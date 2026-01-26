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
using PureGate.Inspect;
using System.Windows.Forms.DataVisualization.Charting;

namespace PureGate
{
    public partial class SummaryForm : DockContent
    {
        private string _project;
        private DateTime _from;
        private DateTime _to;

        // UI 컨트롤
        private Label lblInfo;
        private Label lblTotal;
        private Label lblOk;
        private Label lblNg;
        private Label lblNgTitle;
        private DataGridView dgvNgByClass;
        private TableLayoutPanel root;
        private SplitContainer split;
        private FlowLayoutPanel pnlSummary;

        // 생성자 수정: 프로젝트명 + 날짜 범위
        public SummaryForm(string project, DateTime from, DateTime to)
        {
            _project = project;
            _from = from;
            _to = to;

            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(500, 500);   // ← 원하는 크기
            InitializeUI();
            LoadSummaryData();
        }

        private void InitializeUI()
        {
            this.Text = "Inspection Summary";
            this.DockAreas = DockAreas.Document;

            // 기존 컨트롤(디자이너가 넣어둔 ResultChart 포함) 레이아웃 정리
            // ResultChart는 디자이너에서 생성되므로 여기서 Dock/부모만 재배치
            NGResultChart.Dock = DockStyle.Fill;

            // ===== Root Layout (4 rows) =====
            root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10),
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // info
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // summary
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // title
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // content
            this.Controls.Add(root);

            // 1) 상단 정보 라벨
            lblInfo = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(6, 0, 0, 0)
            };
            root.Controls.Add(lblInfo, 0, 0);

            // 2) 합계 패널
            pnlSummary = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 6, 0, 6)
            };

            lblTotal = CreateSummaryLabel("Total: 0");
            lblOk = CreateSummaryLabel("OK: 0");
            lblNg = CreateSummaryLabel("NG: 0");

            pnlSummary.Controls.Add(lblTotal);
            pnlSummary.Controls.Add(lblOk);
            pnlSummary.Controls.Add(lblNg);

            root.Controls.Add(pnlSummary, 0, 1);

            // 3) 섹션 제목 라벨
            lblNgTitle = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(6, 0, 0, 0),
                BackColor = Color.FromArgb(245, 245, 245)
            };
            lblNgTitle.Text = "NG 클래스 종류와 개수";
            root.Controls.Add(lblNgTitle, 0, 2);

            // 4) 콘텐츠 영역: 위 Grid / 아래 Chart
            dgvNgByClass = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // SplitContainer
            split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                SplitterDistance = 220, // 위쪽(Grid) 높이
            };

            split.Panel1.Padding = new Padding(0, 6, 0, 6);
            split.Panel2.Padding = new Padding(0, 6, 0, 0);

            split.Panel1.Controls.Add(dgvNgByClass);

            // 차트 2개를 좌/우로 배치할 컨테이너
            var chartRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0),
            };
            chartRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f)); // OK/NG
            chartRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f)); // NG 분포

            // 디자이너 차트 도킹
            OkNgChart.Dock = DockStyle.Fill;
            NGResultChart.Dock = DockStyle.Fill;

            // Panel2에 추가
            chartRow.Controls.Add(OkNgChart, 0, 0);
            chartRow.Controls.Add(NGResultChart, 1, 0);

            split.Panel2.Controls.Add(chartRow);

            root.Controls.Add(split, 0, 3);

            // 레이아웃이 다 끝난 "다음 틱"에 비율 적용 (덮어쓰기 방지)
            this.Shown += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    // 아래(차트)에 더 투자: 위(Grid)를 줄임
                    split.SplitterDistance = 150;   // 원하는 값(예: 120~180)
                }));
            };
        }

        private Label CreateSummaryLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(6, 2, 12, 2),
                Padding = new Padding(10, 6, 10, 6),
                ForeColor = Color.White,              // 기본 글자색
                BackColor = Color.Gray,               // 기본 배경 (나중에 덮어씀)
                TextAlign = ContentAlignment.MiddleCenter,
                Text = text
            };
        }

        // NGClass 정규화 함수 추가 (공백/줄바꿈 제거 + 소문자 통일)
        private string NormalizeNgClass(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return s.Trim()
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Replace("\t", "")
                    .ToLowerInvariant();
        }

        private void LoadSummaryData()
        {
            // 상단 정보
            lblInfo.Text = $"프로젝트: {_project} / 조회 기간: {_from:yyyy-MM-dd} ~ {_to:yyyy-MM-dd}";

            // 필터 없이 기간 로드
            var list = InspHistoryRepo.LoadRange(_from, _to, "");

            // 합계 계산
            int total = list.Sum(x => x.Total);
            int ok = list.Sum(x => x.Ok);
            int ng = list.Sum(x => x.Ng);

            lblTotal.Text = $"Total: {total}";
            lblOk.Text = $"OK: {ok}";
            lblOk.BackColor = Color.FromArgb(76, 175, 80);    // 그린
            lblOk.ForeColor = Color.White;
            lblNg.Text = $"NG: {ng}";
            lblNg.BackColor = Color.FromArgb(244, 67, 54);    // 레드
            lblNg.ForeColor = Color.White;

            // NG 클래스별 집계 (정규화해서 GroupBy)
            var ngByClass = list
                .Where(x => x.Ng == 1 && !string.IsNullOrWhiteSpace(x.NgClass))
                .GroupBy(x => NormalizeNgClass(x.NgClass))
                .Where(g => g.Key != "")
                .Select(g => new NgClassCount
                {
                    ClassName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Grid 갱신 
            dgvNgByClass.AutoGenerateColumns = true;
            dgvNgByClass.Columns.Clear();

            dgvNgByClass.DataSource = null;
            dgvNgByClass.DataSource = ngByClass;

            UpdateOkNgChart(ok, ng);
            UpdateNGResultChart(ngByClass);

            dgvNgByClass.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNgByClass.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvNgByClass.ScrollBars = ScrollBars.Both;

            dgvNgByClass.Refresh();
        }

        private void UpdateNGResultChart(List<NgClassCount> ngByClass)
        {
            NGResultChart.Series.Clear();

            var series = new Series("NG")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,

                // 도넛 안에는 퍼센트만
                Label = "#PERCENT{P1}",

                // 범례(오른쪽)에는 클래스명
                LegendText = "#VALX"
            };

            series["DoughnutRadius"] = "60";

            foreach (var item in ngByClass)
            {
                series.Points.AddXY(item.ClassName, item.Count);
            }

            NGResultChart.Series.Add(series);

            if (NGResultChart.Legends.Count > 0)
                NGResultChart.Legends[0].Docking = Docking.Right;

            // 아래(차트 밖)에 4개 클래스 개수 전부 표시
            NGResultChart.Titles.Clear();

            // 4개니까 전부 넣되, 보기 좋게 정렬만
            var all = ngByClass
                .OrderByDescending(x => x.Count)
                .Select(x => $"{x.ClassName}:{x.Count}")
                .ToList();

            var t = new Title
            {
                Text = string.Join("   ", all),   // 예: misplaced:12   cut_lead:10 ...
                Docking = Docking.Bottom,
                Alignment = ContentAlignment.BottomRight,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Black,
                IsDockedInsideChartArea = false,
            };

            NGResultChart.Titles.Add(t);
            NGResultChart.Invalidate();
        }

        private void UpdateOkNgChart(int ok, int ng)
        {
            OkNgChart.Series.Clear();
            OkNgChart.ChartAreas.Clear();
            OkNgChart.Legends.Clear();

            OkNgChart.ChartAreas.Add(new ChartArea("Main"));
            OkNgChart.Legends.Add(new Legend("Legend") { Docking = Docking.Right });

            var s = new Series("OKNG")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                Label = "#PERCENT{P2}",
                LegendText = "#VALX"
            };

            s["DoughnutRadius"] = "60";

            int idxOk = s.Points.AddXY("OK", ok);
            int idxNg = s.Points.AddXY("NG", ng);

            // 색 지정은 인덱스로 접근해서 설정
            s.Points[idxOk].Color = Color.FromArgb(76, 175, 80);
            s.Points[idxNg].Color = Color.FromArgb(244, 67, 54);

            OkNgChart.Series.Add(s);
            // ===== 퍼센트 텍스트 계산 =====
            double total = ok + ng;
            double okPct = total > 0 ? ok * 100.0 / total : 0;
            double ngPct = total > 0 ? ng * 100.0 / total : 0;

            // 오른쪽 아래 텍스트를 Title로 표시 (가장 안정적)
            OkNgChart.Titles.Clear();

            var t = new Title
            {
                Text = $"OK : {ok}   NG : {ng}",
                Docking = Docking.Bottom,
                Alignment = ContentAlignment.BottomRight,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Black,
                IsDockedInsideChartArea = false, // 차트 바깥(아래)에 붙임 (잘 보임)
            };

            OkNgChart.Titles.Add(t);
            OkNgChart.Invalidate();
        }
    }
}

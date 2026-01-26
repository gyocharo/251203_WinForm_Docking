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
            InitializeUI();
            LoadSummaryData();
        }

        private void InitializeUI()
        {
            this.Text = "Inspection Summary";
            this.DockAreas = DockAreas.Document;

            // 기존 컨트롤(디자이너가 넣어둔 ResultChart 포함) 레이아웃 정리
            // ResultChart는 디자이너에서 생성되므로 여기서 Dock/부모만 재배치
            ResultChart.Dock = DockStyle.Fill;

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
                Height = 28,
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
                Height = 26,
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
                SplitterDistance = 220 // 위쪽(Grid) 높이
            };

            split.Panel1.Padding = new Padding(0, 6, 0, 6);
            split.Panel2.Padding = new Padding(0, 6, 0, 0);

            split.Panel1.Controls.Add(dgvNgByClass);

            // ResultChart는 디자이너에서 생성된 컨트롤을 여기로 옮겨 붙임
            split.Panel2.Controls.Add(ResultChart);

            root.Controls.Add(split, 0, 3);
        }

        private Label CreateSummaryLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(10, 10, 10, 10),
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
            lblNg.Text = $"NG: {ng}";

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

            UpdateResultChart(ngByClass);

            dgvNgByClass.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNgByClass.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvNgByClass.ScrollBars = ScrollBars.Both;

            dgvNgByClass.Refresh();
        }

        private void UpdateResultChart(List<NgClassCount> ngByClass)
        {
            // 기존 데이터 초기화
            ResultChart.Series.Clear();

            var series = new Series("NG")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                Label = "#VALX : #VAL",
                LegendText = "#VALX"
            };

            // ✅ 도넛 모양 핵심
            series["DoughnutRadius"] = "60"; // 0~100

            foreach (var item in ngByClass)
            {
                series.Points.AddXY(item.ClassName, item.Count);
            }

            ResultChart.Series.Add(series);

            // 범례 위치(선택)
            if (ResultChart.Legends.Count > 0)
                ResultChart.Legends[0].Docking = Docking.Right;
        }
    }
}

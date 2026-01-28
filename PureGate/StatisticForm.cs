using PureGate.Inspect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WeifenLuo.WinFormsUI.Docking;

namespace PureGate
{
    public partial class StatisticForm : DockContent
    {
        private Chart chart;
        private Label lblSummary; // 하단 개수 표시용

        public StatisticForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Statistics";

            // 1. 레이아웃 설정 (표 삭제, 차트와 라벨만 배치)
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 85f)); // 차트 비중 확대
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // 하단 텍스트 영역
            this.Controls.Add(layout);

            // 2. 차트 설정
            chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            ChartArea chartArea = new ChartArea("MainArea");
            chart.ChartAreas.Add(chartArea);

            Series series = new Series("StatSeries")
            {
                ChartType = SeriesChartType.Doughnut,
                Font = new Font("Arial", 9f, FontStyle.Bold)
            };
            series["DoughnutRadius"] = "65";
            series["PieLabelStyle"] = "Inside"; // 라벨을 안쪽으로
            // ⭐ 퍼센트(%) 표시 설정
            series.Label = "#PERCENT{P0}";
            chart.Series.Add(series);

            if (chart.Legends.Count == 0)
            {
                chart.Legends.Add(new Legend("DefaultLegend"));
            }

            layout.Controls.Add(chart, 0, 0);

            // 3. 하단 OK/NG 개수 표시 라벨 (표 대신 사용)
            lblSummary = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                Text = "OK: 0 / NG: 0"
            };
            layout.Controls.Add(lblSummary, 0, 1);

            UpdateStatistics(0, 0);
        }

        public void UpdateStatistics(int okCount, int ngCount, List<NgClassCount> ngClassDetails = null)
        {
            System.Diagnostics.Debug.WriteLine($"넘어온 NG 종류 개수: {ngClassDetails?.Count ?? 0}");
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatistics(okCount, ngCount, ngClassDetails)));
                return;
            }

            Series series = chart.Series["StatSeries"];
            series.Points.Clear();

            if (okCount == 0 && ngCount == 0)
            {
                int idx0 = series.Points.AddXY("NoData", 1);
                series.Points[idx0].Color = Color.LightGray;
                series.Points[idx0].Label = "";        // % 라벨 숨김
                series.Points[idx0].LegendText = "";   // 범례 숨김
                chart.Invalidate();
                return;
            }

            // 1. 하단 라벨 즉시 갱신
            lblSummary.Text = $"TOTAL: {okCount + ngCount} ( OK: {okCount} / NG: {ngCount} )";

            // 2. OK 데이터 추가
            if (okCount > 0)
            {
                int idx = series.Points.AddXY("OK", okCount);
                series.Points[idx].Color = Color.DodgerBlue;
                series.Points[idx].LegendText = $"OK ({okCount})";
            }

            System.Diagnostics.Debug.WriteLine("[NG_RAW] " + string.Join(", ", ngClassDetails.Select(x => x.ClassName)));
            // 3. ⭐ NG 데이터 분할 (가장 중요한 부분)
            if (ngClassDetails != null && ngClassDetails.Any(d => d.Count > 0))
            {
                // ✅ OK 계열 키 제거 + 공백 제거 + 대소문자 무시 그룹핑
                var stats = ngClassDetails
                    .Where(d => d != null && d.Count > 0)
                    .Select(d => new { Name = (d.ClassName ?? "").Trim(), d.Count })
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        !x.Name.Equals("OK", StringComparison.OrdinalIgnoreCase) &&
                        !x.Name.Equals("GOOD", StringComparison.OrdinalIgnoreCase) &&
                        !x.Name.Equals("NG", StringComparison.OrdinalIgnoreCase) &&
                        !x.Name.Equals("NoData", StringComparison.OrdinalIgnoreCase)
                    )
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new { Name = g.Key, Total = g.Sum(v => v.Count) });

                Color[] palette = { Color.Red, Color.Orange, Color.Magenta, Color.Brown, Color.Gold };
                int i = 0;

                foreach (var item in stats)
                {
                    int idx = series.Points.AddXY(item.Name, item.Total);
                    series.Points[idx].Color = palette[i % palette.Length];
                    series.Points[idx].LegendText = $"{item.Name} ({item.Total})";
                    i++;
                }
            }
            else if (ngCount > 0)
            {
                int idx = series.Points.AddXY("NG", ngCount);
                series.Points[idx].Color = Color.Red;
                series.Points[idx].LegendText = $"NG ({ngCount})";
            }

            chart.Invalidate(); // 차트 강제 다시 그리기
        }

    }
}
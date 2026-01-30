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
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatistics(okCount, ngCount, ngClassDetails)));
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[StatisticForm] ok={okCount}, ng={ngCount}, detailCount={ngClassDetails?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine("[NG_RAW] " + string.Join(", ", (ngClassDetails ?? new List<NgClassCount>())
                .Where(x => x != null)
                .Select(x => $"{x.ClassName}:{x.Count}")));

            Series series = chart.Series["StatSeries"];
            series.Points.Clear();

            if (okCount == 0 && ngCount == 0)
            {
                int idx0 = series.Points.AddXY("NoData", 1);
                series.Points[idx0].Color = Color.LightGray;
                series.Points[idx0].Label = "";
                series.Points[idx0].LegendText = "";
                lblSummary.Text = "OK: 0 / NG: 0";
                chart.Invalidate();
                return;
            }

            // 하단 라벨
            lblSummary.Text = $"TOTAL: {okCount + ngCount} ( OK: {okCount} / NG: {ngCount} )";

            // ✅ 1) 유효한 NG 클래스 목록 만들기
            // - OK/GOOD/NG/NoData/Unknown 같은 값은 제거
            // - 공백 제거 + 대소문자 무시로 그룹핑 + 합산
            var statsList = (ngClassDetails ?? new List<NgClassCount>())
                .Where(d => d != null && d.Count > 0)
                .Select(d => new { Name = (d.ClassName ?? "").Trim(), d.Count })
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Name) &&
                    !x.Name.Equals("OK", StringComparison.OrdinalIgnoreCase) &&
                    !x.Name.Equals("GOOD", StringComparison.OrdinalIgnoreCase) &&
                    !x.Name.Equals("NG", StringComparison.OrdinalIgnoreCase) &&
                    !x.Name.Equals("NoData", StringComparison.OrdinalIgnoreCase) &&
                    !x.Name.Equals("Unknown", StringComparison.OrdinalIgnoreCase)
                )
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Name = g.Key, Total = g.Sum(v => v.Count) })
                .OrderByDescending(x => x.Total)
                .ToList();

            int detailNgSum = statsList.Sum(x => x.Total);
            if (ngCount > 0 && detailNgSum > 0 && detailNgSum != ngCount)
            {
                // ✅ detail 합이 ngCount랑 다르면 어디서 중복/누락이 생긴 거라 디버그 로그로 바로 보이게
                System.Diagnostics.Debug.WriteLine($"[StatisticForm][WARN] NG mismatch: ngCount={ngCount}, detailSum={detailNgSum} (check producer side)");
            }

            // ✅ 2) 도넛 구성 원칙
            // - OK는 항상 OK로 1조각
            // - NG는 statsList가 있으면 "클래스별"로 쪼개서 표시
            // - statsList가 없으면 NG 합계 1조각으로 표시

            // OK 조각
            if (okCount > 0)
            {
                int idx = series.Points.AddXY("OK", okCount);
                series.Points[idx].Color = Color.DodgerBlue;
                series.Points[idx].LegendText = $"OK ({okCount})";
            }

            // NG 조각들
            if (ngCount > 0)
            {
                if (statsList.Count > 0)
                {
                    // 클래스별 NG 표시
                    Color[] palette = { Color.Red, Color.Orange, Color.Magenta, Color.Brown, Color.Gold, Color.DarkRed, Color.DarkOrange };
                    int i = 0;

                    foreach (var item in statsList)
                    {
                        int idx = series.Points.AddXY(item.Name, item.Total);
                        series.Points[idx].Color = palette[i % palette.Length];
                        series.Points[idx].LegendText = $"{item.Name} ({item.Total})";
                        i++;
                    }

                    // ✅ statsList 합이 ngCount보다 작으면 남는 NG는 기타로 표시(원하면 삭제 가능)
                    int remain = ngCount - detailNgSum;
                    if (remain > 0)
                    {
                        int idx = series.Points.AddXY("NG(etc)", remain);
                        series.Points[idx].Color = Color.Gray;
                        series.Points[idx].LegendText = $"NG(etc) ({remain})";
                    }
                }
                else
                {
                    // 유효 클래스가 없으면 NG 합계 1조각만
                    int idx = series.Points.AddXY("NG", ngCount);
                    series.Points[idx].Color = Color.Red;
                    series.Points[idx].LegendText = $"NG ({ngCount})";
                }
            }

            chart.Invalidate();
        }

    }
}
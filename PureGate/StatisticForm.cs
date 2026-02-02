using PureGate.Inspect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // ✅ UPH 오버레이용
        private Label lblUph;
        private Panel pnlChartHost;

        // ✅ UPH 계산(StatisticForm 내부에서만)
        private readonly object _uphLock = new object();
        private readonly Stopwatch _uphSw = new Stopwatch();
        private readonly Queue<DateTime> _uphDoneTimesUtc = new Queue<DateTime>(4096);

        private bool _uphStarted = false;
        private int _uphLastTotal = 0;

        private int _lastOk = 0;
        private int _lastNg = 0;

        private Timer _uphTimer;

        public StatisticForm()
        {
            InitializeComponent();
            InitializeUI();

            // ✅ 1초마다 UPH 라벨 갱신(검사 이벤트 없을 때도 숫자 유지)
            _uphTimer = new Timer { Interval = 1000 };
            _uphTimer.Tick += (s, e) => UpdateUphLabelFromCounts(_lastOk, _lastNg, isCountUpdate: false);
            _uphTimer.Start();

            this.FormClosed += (s, e) =>
            {
                try
                {
                    _uphTimer?.Stop();
                    _uphTimer?.Dispose();
                }
                catch { }
            };
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

            // ✅ 2. 차트 Host 패널 (차트 + UPH 라벨 오버레이)
            pnlChartHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

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

            // ✅ UPH 라벨 (좌상단 오버레이)
            lblUph = new Label
            {
                AutoSize = true,
                Text = "UPH(평균): -",
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(6, 3, 6, 3),
                Location = new Point(8, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            pnlChartHost.Controls.Add(chart);
            pnlChartHost.Controls.Add(lblUph);
            lblUph.BringToFront();

            layout.Controls.Add(pnlChartHost, 0, 0);

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

            _lastOk = okCount;
            _lastNg = ngCount;

            System.Diagnostics.Debug.WriteLine($"[StatisticForm] ok={okCount}, ng={ngCount}, detailCount={ngClassDetails?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine("[NG_RAW] " + string.Join(", ", (ngClassDetails ?? new List<NgClassCount>())
                .Where(x => x != null)
                .Select(x => $"{x.ClassName}:{x.Count}")));

            // ✅ UPH 라벨 갱신(카운트 변화 시점에만 큐에 기록)
            UpdateUphLabelFromCounts(okCount, ngCount, isCountUpdate: true);

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
                System.Diagnostics.Debug.WriteLine($"[StatisticForm][WARN] NG mismatch: ngCount={ngCount}, detailSum={detailNgSum} (check producer side)");
            }

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
                    Color[] palette = { Color.Red, Color.Orange, Color.Magenta, Color.Brown, Color.Gold, Color.DarkRed, Color.DarkOrange };
                    int i = 0;

                    foreach (var item in statsList)
                    {
                        int idx = series.Points.AddXY(item.Name, item.Total);
                        series.Points[idx].Color = palette[i % palette.Length];
                        series.Points[idx].LegendText = $"{item.Name} ({item.Total})";
                        i++;
                    }

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
                    int idx = series.Points.AddXY("NG", ngCount);
                    series.Points[idx].Color = Color.Red;
                    series.Points[idx].LegendText = $"NG ({ngCount})";
                }
            }

            chart.Invalidate();
        }

        private void UpdateUphLabelFromCounts(int okCount, int ngCount, bool isCountUpdate)
        {
            if (lblUph == null) return;

            int total = okCount + ngCount;

            lock (_uphLock)
            {
                // 카운트가 0이면 세션 초기화
                if (total <= 0)
                {
                    _uphStarted = false;
                    _uphLastTotal = 0;
                    _uphDoneTimesUtc.Clear();
                    _uphSw.Reset();
                    lblUph.Text = "UPH(평균): -";
                    return;
                }

                // 최초 시작
                if (!_uphStarted)
                {
                    _uphStarted = true;
                    _uphLastTotal = 0;
                    _uphDoneTimesUtc.Clear();
                    _uphSw.Reset();
                    _uphSw.Start();
                }

                // 카운트가 리셋(감소)된 경우 재시작
                if (isCountUpdate && total < _uphLastTotal)
                {
                    _uphLastTotal = 0;
                    _uphDoneTimesUtc.Clear();
                    _uphSw.Reset();
                    _uphSw.Start();
                }

                // 증가분 기록(현재는 avg만 쓰지만 로직은 유지)
                if (isCountUpdate)
                {
                    int delta = total - _uphLastTotal;
                    if (delta > 0)
                    {
                        var nowUtc = DateTime.UtcNow;
                        for (int i = 0; i < delta; i++)
                            _uphDoneTimesUtc.Enqueue(nowUtc);

                        _uphLastTotal = total;
                    }
                }

                double hours = _uphSw.Elapsed.TotalHours;
                if (hours <= 1e-9)
                {
                    lblUph.Text = "UPH(평균): -";
                    return;
                }

                double avgUph = total / hours;

                // ✅ 소수점 없이 표시
                lblUph.Text = $"UPH(평균): {avgUph:0}";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WeifenLuo.WinFormsUI.Docking;

namespace PureGate
{
    public partial class StatisticForm : DockContent
    {
        private Chart chart;

        public StatisticForm()
        {
            InitializeComponent();
            InitializeChart();
        }

        private void InitializeChart()
        {
            // 차트 객체 생성 및 기본 설정
            chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Name = "ResultChart" // 이름 명시
            };

            // 중요: 폼의 컨트롤 목록에 추가하고 맨 앞으로 보냄
            this.Controls.Add(chart);
            chart.BringToFront();

            ChartArea chartArea = new ChartArea("MainArea")
            {
                BackColor = Color.Transparent
            };
            chart.ChartAreas.Add(chartArea);

            Series series = new Series("StatSeries")
            {
                ChartType = SeriesChartType.Doughnut,
                Font = new Font("맑은 고딕", 12f, FontStyle.Bold)
            };

            // 도넛 스타일 설정
            series["PieDrawingStyle"] = "SoftEdge";
            series["DoughnutRadius"] = "50";
            series["PieLabelStyle"] = "Inside"; // 라벨이 안쪽에 나오게

            chart.Series.Add(series);

            // 범례 설정
            Legend legend = new Legend
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                BackColor = Color.Transparent
            };
            chart.Legends.Add(legend);

            // 초기 상태에서 "검사 대기" 상태라도 그리게 함
            UpdateStatistics(0, 0);
        }

        /// <summary>
        /// 외부(MainForm 등)에서 OK/NG 개수를 받아 차트를 갱신하는 메서드
        /// </summary>
        public void UpdateStatistics(int okCount, int ngCount)
        {
            if (chart.InvokeRequired)
            {
                chart.Invoke(new Action(() => UpdateStatistics(okCount, ngCount)));
                return;
            }

            Series series = chart.Series["StatSeries"];
            series.Points.Clear();

            int total = okCount + ngCount;

            // total이 0이면 Ready 표시하고 종료
            if (total == 0)
            {
                int idx = series.Points.AddXY("Waiting", 1);
                series.Points[idx].Label = "Ready (0%)";
                series.Points[idx].Color = Color.LightGray;
                return;
            }

            // 🔴 수치가 변하게 하는 핵심 로직
            double okRate = (double)okCount / total * 100;
            double ngRate = (double)ngCount / total * 100;

            // OK 추가
            int oIdx = series.Points.AddXY("OK", okCount);
            series.Points[oIdx].Label = $"OK {okRate:F0}%"; // 예: OK 95%
            series.Points[oIdx].Color = Color.DodgerBlue;
            series.Points[oIdx].LegendText = $"OK ({okCount})";

            // NG 추가
            int nIdx = series.Points.AddXY("NG", ngCount);
            series.Points[nIdx].Label = $"NG {ngRate:F0}%"; // 예: NG 5%
            series.Points[nIdx].Color = Color.OrangeRed;
            series.Points[nIdx].LegendText = $"NG ({ngCount})";

            // 즉시 반영
            chart.Invalidate();
            chart.Update();
        }


    }
}
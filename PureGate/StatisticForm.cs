using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // 차트 컨트롤 생성
            chart = new Chart
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(chart);

            // 차트 영역 추가
            ChartArea chartArea = new ChartArea("MainArea");
            chart.ChartAreas.Add(chartArea);

            // Series 생성 및 Doughnut 타입
            Series series = new Series("Data")
            {
                ChartType = SeriesChartType.Doughnut
            };

            // 최소 데이터 포인트 추가 (실행 시 보여주기 위함)
            series.Points.AddXY("A", 30);
            series.Points.AddXY("B", 70);

            chart.Series.Add(series);

            // 레전드 추가 (선택 사항)
            Legend legend = new Legend();
            chart.Legends.Add(legend);
        }
    }
}

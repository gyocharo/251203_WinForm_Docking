namespace PureGate
{
    partial class SummaryForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.NGResultChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.OkNgChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.btnOpenSaveFolder = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.NGResultChart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.OkNgChart)).BeginInit();
            this.SuspendLayout();
            // 
            // NGResultChart
            // 
            chartArea1.Name = "ChartArea1";
            this.NGResultChart.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.NGResultChart.Legends.Add(legend1);
            this.NGResultChart.Location = new System.Drawing.Point(406, 239);
            this.NGResultChart.Name = "NGResultChart";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Doughnut;
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.NGResultChart.Series.Add(series1);
            this.NGResultChart.Size = new System.Drawing.Size(394, 211);
            this.NGResultChart.TabIndex = 0;
            this.NGResultChart.TabStop = false;
            this.NGResultChart.Text = "chart1";
            // 
            // OkNgChart
            // 
            chartArea2.Name = "ChartArea1";
            this.OkNgChart.ChartAreas.Add(chartArea2);
            legend2.Name = "Legend1";
            this.OkNgChart.Legends.Add(legend2);
            this.OkNgChart.Location = new System.Drawing.Point(6, 239);
            this.OkNgChart.Name = "OkNgChart";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Doughnut;
            series2.Legend = "Legend1";
            series2.Name = "Series1";
            this.OkNgChart.Series.Add(series2);
            this.OkNgChart.Size = new System.Drawing.Size(394, 211);
            this.OkNgChart.TabIndex = 1;
            this.OkNgChart.Text = "chart1";
            // 
            // btnOpenSaveFolder
            // 
            this.btnOpenSaveFolder.Font = new System.Drawing.Font("굴림", 7F);
            this.btnOpenSaveFolder.Location = new System.Drawing.Point(623, 171);
            this.btnOpenSaveFolder.Name = "btnOpenSaveFolder";
            this.btnOpenSaveFolder.Size = new System.Drawing.Size(159, 40);
            this.btnOpenSaveFolder.TabIndex = 2;
            this.btnOpenSaveFolder.TabStop = false;
            this.btnOpenSaveFolder.Text = "통계 저장 폴더 열기";
            this.btnOpenSaveFolder.UseVisualStyleBackColor = true;
            this.btnOpenSaveFolder.Click += new System.EventHandler(this.btnOpenSaveFolder_Click);
            // 
            // SummaryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(806, 450);
            this.Controls.Add(this.btnOpenSaveFolder);
            this.Controls.Add(this.OkNgChart);
            this.Controls.Add(this.NGResultChart);
            this.Name = "SummaryForm";
            this.Text = "SummaryForm";
            ((System.ComponentModel.ISupportInitialize)(this.NGResultChart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.OkNgChart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart NGResultChart;
        private System.Windows.Forms.DataVisualization.Charting.Chart OkNgChart;
        private System.Windows.Forms.Button btnOpenSaveFolder;
    }
}
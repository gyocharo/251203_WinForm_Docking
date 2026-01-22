namespace PureGate
{
    partial class TitleForm
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
            this.dockPanel1 = new WeifenLuo.WinFormsUI.Docking.DockPanel();
            this.btnShowRange = new System.Windows.Forms.Button();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.PJName = new System.Windows.Forms.Label();
            this.lblToday = new System.Windows.Forms.LinkLabel();
            this.lbl1Week = new System.Windows.Forms.LinkLabel();
            this.lbl1Month = new System.Windows.Forms.LinkLabel();
            this.lbl1Year = new System.Windows.Forms.LinkLabel();
            this.tlpLinklabels = new System.Windows.Forms.TableLayoutPanel();
            this.tlpDateTimePickers = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tlpLinklabels.SuspendLayout();
            this.tlpDateTimePickers.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // dockPanel1
            // 
            this.dockPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dockPanel1.Location = new System.Drawing.Point(0, 0);
            this.dockPanel1.Name = "dockPanel1";
            this.dockPanel1.Size = new System.Drawing.Size(1106, 78);
            this.dockPanel1.TabIndex = 0;
            // 
            // btnShowRange
            // 
            this.btnShowRange.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnShowRange.Location = new System.Drawing.Point(524, 24);
            this.btnShowRange.Name = "btnShowRange";
            this.btnShowRange.Size = new System.Drawing.Size(96, 30);
            this.btnShowRange.TabIndex = 1;
            this.btnShowRange.Text = "보기";
            this.btnShowRange.UseVisualStyleBackColor = true;
            this.btnShowRange.Click += new System.EventHandler(this.btnShowRange_Click);
            // 
            // dtpStart
            // 
            this.dtpStart.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dtpStart.Location = new System.Drawing.Point(6, 25);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(236, 28);
            this.dtpStart.TabIndex = 2;
            // 
            // dtpEnd
            // 
            this.dtpEnd.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.dtpEnd.Location = new System.Drawing.Point(279, 25);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(236, 28);
            this.dtpEnd.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(251, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(19, 18);
            this.label1.TabIndex = 4;
            this.label1.Text = "~";
            // 
            // PJName
            // 
            this.PJName.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.PJName.Font = new System.Drawing.Font("굴림", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.PJName.Location = new System.Drawing.Point(3, 4);
            this.PJName.Name = "PJName";
            this.PJName.Size = new System.Drawing.Size(227, 69);
            this.PJName.TabIndex = 5;
            this.PJName.Text = "PureGate";
            // 
            // lblToday
            // 
            this.lblToday.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblToday.AutoSize = true;
            this.lblToday.Location = new System.Drawing.Point(14, 10);
            this.lblToday.Name = "lblToday";
            this.lblToday.Size = new System.Drawing.Size(59, 18);
            this.lblToday.TabIndex = 6;
            this.lblToday.TabStop = true;
            this.lblToday.Text = "Today";
            this.lblToday.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblToday_LinkClicked);
            // 
            // lbl1Week
            // 
            this.lbl1Week.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl1Week.AutoSize = true;
            this.lbl1Week.Location = new System.Drawing.Point(98, 10);
            this.lbl1Week.Name = "lbl1Week";
            this.lbl1Week.Size = new System.Drawing.Size(68, 18);
            this.lbl1Week.TabIndex = 7;
            this.lbl1Week.TabStop = true;
            this.lbl1Week.Text = "1 Week";
            this.lbl1Week.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lbl1Week_LinkClicked);
            // 
            // lbl1Month
            // 
            this.lbl1Month.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl1Month.AutoSize = true;
            this.lbl1Month.Location = new System.Drawing.Point(6, 49);
            this.lbl1Month.Name = "lbl1Month";
            this.lbl1Month.Size = new System.Drawing.Size(75, 18);
            this.lbl1Month.TabIndex = 8;
            this.lbl1Month.TabStop = true;
            this.lbl1Month.Text = "1 Month";
            this.lbl1Month.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lbl1Month_LinkClicked);
            // 
            // lbl1Year
            // 
            this.lbl1Year.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl1Year.AutoSize = true;
            this.lbl1Year.Location = new System.Drawing.Point(101, 49);
            this.lbl1Year.Name = "lbl1Year";
            this.lbl1Year.Size = new System.Drawing.Size(61, 18);
            this.lbl1Year.TabIndex = 9;
            this.lbl1Year.TabStop = true;
            this.lbl1Year.Text = "1 Year";
            this.lbl1Year.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lbl1Year_LinkClicked);
            // 
            // tlpLinklabels
            // 
            this.tlpLinklabels.ColumnCount = 2;
            this.tlpLinklabels.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpLinklabels.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpLinklabels.Controls.Add(this.lbl1Week, 1, 0);
            this.tlpLinklabels.Controls.Add(this.lbl1Month, 0, 1);
            this.tlpLinklabels.Controls.Add(this.lbl1Year, 1, 1);
            this.tlpLinklabels.Controls.Add(this.lblToday, 0, 0);
            this.tlpLinklabels.Dock = System.Windows.Forms.DockStyle.Right;
            this.tlpLinklabels.Location = new System.Drawing.Point(930, 0);
            this.tlpLinklabels.Name = "tlpLinklabels";
            this.tlpLinklabels.RowCount = 2;
            this.tlpLinklabels.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpLinklabels.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlpLinklabels.Size = new System.Drawing.Size(176, 78);
            this.tlpLinklabels.TabIndex = 10;
            // 
            // tlpDateTimePickers
            // 
            this.tlpDateTimePickers.ColumnCount = 4;
            this.tlpDateTimePickers.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 39.8577F));
            this.tlpDateTimePickers.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4.152751F));
            this.tlpDateTimePickers.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 39.8577F));
            this.tlpDateTimePickers.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.13184F));
            this.tlpDateTimePickers.Controls.Add(this.dtpStart, 0, 0);
            this.tlpDateTimePickers.Controls.Add(this.dtpEnd, 2, 0);
            this.tlpDateTimePickers.Controls.Add(this.label1, 1, 0);
            this.tlpDateTimePickers.Controls.Add(this.btnShowRange, 3, 0);
            this.tlpDateTimePickers.Dock = System.Windows.Forms.DockStyle.Right;
            this.tlpDateTimePickers.Location = new System.Drawing.Point(307, 0);
            this.tlpDateTimePickers.Name = "tlpDateTimePickers";
            this.tlpDateTimePickers.RowCount = 1;
            this.tlpDateTimePickers.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpDateTimePickers.Size = new System.Drawing.Size(623, 78);
            this.tlpDateTimePickers.TabIndex = 10;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.PJName, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Left;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(233, 78);
            this.tableLayoutPanel3.TabIndex = 11;
            // 
            // TitleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1106, 78);
            this.Controls.Add(this.tableLayoutPanel3);
            this.Controls.Add(this.tlpDateTimePickers);
            this.Controls.Add(this.tlpLinklabels);
            this.Controls.Add(this.dockPanel1);
            this.Name = "TitleForm";
            this.Text = "TitleForm";
            this.tlpLinklabels.ResumeLayout(false);
            this.tlpLinklabels.PerformLayout();
            this.tlpDateTimePickers.ResumeLayout(false);
            this.tlpDateTimePickers.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private WeifenLuo.WinFormsUI.Docking.DockPanel dockPanel1;
        private System.Windows.Forms.Button btnShowRange;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label PJName;
        private System.Windows.Forms.LinkLabel lblToday;
        private System.Windows.Forms.LinkLabel lbl1Week;
        private System.Windows.Forms.LinkLabel lbl1Month;
        private System.Windows.Forms.LinkLabel lbl1Year;
        private System.Windows.Forms.TableLayoutPanel tlpLinklabels;
        private System.Windows.Forms.TableLayoutPanel tlpDateTimePickers;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
    }
}
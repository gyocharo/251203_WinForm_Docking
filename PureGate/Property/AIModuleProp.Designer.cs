namespace PureGate.Property
{
    partial class AIModuleProp
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel6 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.lbx_ResultDetail = new System.Windows.Forms.ListBox();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lv_Result = new System.Windows.Forms.ListView();
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label9 = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dasda = new System.Windows.Forms.Label();
            this.lblAreaFilter = new System.Windows.Forms.Label();
            this.txtMinArea = new System.Windows.Forms.TextBox();
            this.txtMaxArea = new System.Windows.Forms.TextBox();
            this.cbAIModelType = new System.Windows.Forms.ComboBox();
            this.btnInspAI = new System.Windows.Forms.Button();
            this.txtAIModelPath = new System.Windows.Forms.TextBox();
            this.btnLoadModel = new System.Windows.Forms.Button();
            this.btnSelAIModel = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.Txt_ModuleInfo = new System.Windows.Forms.TextBox();
            this.Lbl_ModuleInfo = new System.Windows.Forms.Label();
            this.lv_ClassInfos = new System.Windows.Forms.ListView();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.lbx_ModelInformation = new System.Windows.Forms.ListBox();
            this.label24 = new System.Windows.Forms.Label();
            this.panel6.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel3.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.panel4);
            this.panel6.Location = new System.Drawing.Point(374, 300);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(267, 292);
            this.panel6.TabIndex = 20;
            // 
            // panel4
            // 
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.label1);
            this.panel4.Controls.Add(this.lbx_ResultDetail);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(0, 0);
            this.panel4.Margin = new System.Windows.Forms.Padding(4);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(267, 292);
            this.panel4.TabIndex = 15;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(84, 12);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 18);
            this.label1.TabIndex = 14;
            this.label1.Text = "ResultDetail";
            // 
            // lbx_ResultDetail
            // 
            this.lbx_ResultDetail.FormattingEnabled = true;
            this.lbx_ResultDetail.ItemHeight = 18;
            this.lbx_ResultDetail.Location = new System.Drawing.Point(-1, 34);
            this.lbx_ResultDetail.Margin = new System.Windows.Forms.Padding(4);
            this.lbx_ResultDetail.Name = "lbx_ResultDetail";
            this.lbx_ResultDetail.Size = new System.Drawing.Size(267, 274);
            this.lbx_ResultDetail.TabIndex = 1;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.panel3);
            this.panel5.Location = new System.Drawing.Point(374, 8);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(267, 285);
            this.panel5.TabIndex = 21;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.lv_Result);
            this.panel3.Controls.Add(this.label9);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(4);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(267, 285);
            this.panel3.TabIndex = 14;
            // 
            // lv_Result
            // 
            this.lv_Result.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader6,
            this.columnHeader7});
            this.lv_Result.GridLines = true;
            this.lv_Result.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lv_Result.HideSelection = false;
            this.lv_Result.Location = new System.Drawing.Point(-1, 34);
            this.lv_Result.Margin = new System.Windows.Forms.Padding(4);
            this.lv_Result.MultiSelect = false;
            this.lv_Result.Name = "lv_Result";
            this.lv_Result.Size = new System.Drawing.Size(267, 250);
            this.lv_Result.TabIndex = 17;
            this.lv_Result.UseCompatibleStateImageBehavior = false;
            this.lv_Result.View = System.Windows.Forms.View.Details;
            this.lv_Result.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.Lv_Result_ItemSelectionChanged);
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "FileName";
            this.columnHeader6.Width = 140;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Result";
            this.columnHeader7.Width = 70;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(104, 12);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(57, 18);
            this.label9.TabIndex = 14;
            this.label9.Text = "Result";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.panel2, 0, 1);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(4, 4);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 43.15068F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 56.84932F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(367, 588);
            this.tableLayoutPanel2.TabIndex = 19;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.dasda);
            this.panel1.Controls.Add(this.lblAreaFilter);
            this.panel1.Controls.Add(this.txtMinArea);
            this.panel1.Controls.Add(this.txtMaxArea);
            this.panel1.Controls.Add(this.cbAIModelType);
            this.panel1.Controls.Add(this.btnInspAI);
            this.panel1.Controls.Add(this.txtAIModelPath);
            this.panel1.Controls.Add(this.btnLoadModel);
            this.panel1.Controls.Add(this.btnSelAIModel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(4, 4);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(359, 245);
            this.panel1.TabIndex = 10;
            // 
            // dasda
            // 
            this.dasda.AutoSize = true;
            this.dasda.Location = new System.Drawing.Point(90, 185);
            this.dasda.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.dasda.Name = "dasda";
            this.dasda.Size = new System.Drawing.Size(22, 18);
            this.dasda.TabIndex = 17;
            this.dasda.Text = "~";
            // 
            // lblAreaFilter
            // 
            this.lblAreaFilter.AutoSize = true;
            this.lblAreaFilter.Location = new System.Drawing.Point(4, 214);
            this.lblAreaFilter.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAreaFilter.Name = "lblAreaFilter";
            this.lblAreaFilter.Size = new System.Drawing.Size(195, 18);
            this.lblAreaFilter.TabIndex = 16;
            this.lblAreaFilter.Text = "Area Filter (Min ~ Max)";
            // 
            // txtMinArea
            // 
            this.txtMinArea.Location = new System.Drawing.Point(18, 182);
            this.txtMinArea.Margin = new System.Windows.Forms.Padding(4);
            this.txtMinArea.Name = "txtMinArea";
            this.txtMinArea.Size = new System.Drawing.Size(64, 28);
            this.txtMinArea.TabIndex = 14;
            // 
            // txtMaxArea
            // 
            this.txtMaxArea.Location = new System.Drawing.Point(120, 182);
            this.txtMaxArea.Margin = new System.Windows.Forms.Padding(4);
            this.txtMaxArea.Name = "txtMaxArea";
            this.txtMaxArea.Size = new System.Drawing.Size(64, 28);
            this.txtMaxArea.TabIndex = 15;
            // 
            // cbAIModelType
            // 
            this.cbAIModelType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAIModelType.FormattingEnabled = true;
            this.cbAIModelType.Location = new System.Drawing.Point(108, 41);
            this.cbAIModelType.Margin = new System.Windows.Forms.Padding(4);
            this.cbAIModelType.Name = "cbAIModelType";
            this.cbAIModelType.Size = new System.Drawing.Size(233, 26);
            this.cbAIModelType.TabIndex = 5;
            this.cbAIModelType.SelectedIndexChanged += new System.EventHandler(this.cbAIModelType_SelectedIndexChanged);
            // 
            // btnInspAI
            // 
            this.btnInspAI.Location = new System.Drawing.Point(215, 176);
            this.btnInspAI.Margin = new System.Windows.Forms.Padding(4);
            this.btnInspAI.Name = "btnInspAI";
            this.btnInspAI.Size = new System.Drawing.Size(126, 39);
            this.btnInspAI.TabIndex = 9;
            this.btnInspAI.Text = "AI 검사";
            this.btnInspAI.UseVisualStyleBackColor = true;
            this.btnInspAI.Click += new System.EventHandler(this.btnInspAI_Click);
            // 
            // txtAIModelPath
            // 
            this.txtAIModelPath.Location = new System.Drawing.Point(7, 8);
            this.txtAIModelPath.Margin = new System.Windows.Forms.Padding(4);
            this.txtAIModelPath.Name = "txtAIModelPath";
            this.txtAIModelPath.ReadOnly = true;
            this.txtAIModelPath.Size = new System.Drawing.Size(337, 28);
            this.txtAIModelPath.TabIndex = 6;
            // 
            // btnLoadModel
            // 
            this.btnLoadModel.Location = new System.Drawing.Point(215, 124);
            this.btnLoadModel.Margin = new System.Windows.Forms.Padding(4);
            this.btnLoadModel.Name = "btnLoadModel";
            this.btnLoadModel.Size = new System.Drawing.Size(126, 44);
            this.btnLoadModel.TabIndex = 8;
            this.btnLoadModel.Text = "모델 로딩";
            this.btnLoadModel.UseVisualStyleBackColor = true;
            this.btnLoadModel.Click += new System.EventHandler(this.btnLoadModel_Click);
            // 
            // btnSelAIModel
            // 
            this.btnSelAIModel.Location = new System.Drawing.Point(215, 74);
            this.btnSelAIModel.Margin = new System.Windows.Forms.Padding(4);
            this.btnSelAIModel.Name = "btnSelAIModel";
            this.btnSelAIModel.Size = new System.Drawing.Size(126, 42);
            this.btnSelAIModel.TabIndex = 7;
            this.btnSelAIModel.Text = "AI모델 선택";
            this.btnSelAIModel.UseVisualStyleBackColor = true;
            this.btnSelAIModel.Click += new System.EventHandler(this.btnSelAIModel_Click);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.Txt_ModuleInfo);
            this.panel2.Controls.Add(this.Lbl_ModuleInfo);
            this.panel2.Controls.Add(this.lv_ClassInfos);
            this.panel2.Controls.Add(this.lbx_ModelInformation);
            this.panel2.Controls.Add(this.label24);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(4, 257);
            this.panel2.Margin = new System.Windows.Forms.Padding(4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(359, 327);
            this.panel2.TabIndex = 21;
            // 
            // Txt_ModuleInfo
            // 
            this.Txt_ModuleInfo.Location = new System.Drawing.Point(4, 290);
            this.Txt_ModuleInfo.Margin = new System.Windows.Forms.Padding(4);
            this.Txt_ModuleInfo.Multiline = true;
            this.Txt_ModuleInfo.Name = "Txt_ModuleInfo";
            this.Txt_ModuleInfo.Size = new System.Drawing.Size(347, 23);
            this.Txt_ModuleInfo.TabIndex = 27;
            // 
            // Lbl_ModuleInfo
            // 
            this.Lbl_ModuleInfo.AutoSize = true;
            this.Lbl_ModuleInfo.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.Lbl_ModuleInfo.Location = new System.Drawing.Point(4, 268);
            this.Lbl_ModuleInfo.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Lbl_ModuleInfo.Name = "Lbl_ModuleInfo";
            this.Lbl_ModuleInfo.Size = new System.Drawing.Size(180, 18);
            this.Lbl_ModuleInfo.TabIndex = 26;
            this.Lbl_ModuleInfo.Text = "Module Information";
            // 
            // lv_ClassInfos
            // 
            this.lv_ClassInfos.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5});
            this.lv_ClassInfos.GridLines = true;
            this.lv_ClassInfos.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lv_ClassInfos.HideSelection = false;
            this.lv_ClassInfos.Location = new System.Drawing.Point(4, 124);
            this.lv_ClassInfos.Margin = new System.Windows.Forms.Padding(4);
            this.lv_ClassInfos.MultiSelect = false;
            this.lv_ClassInfos.Name = "lv_ClassInfos";
            this.lv_ClassInfos.Size = new System.Drawing.Size(347, 140);
            this.lv_ClassInfos.TabIndex = 25;
            this.lv_ClassInfos.UseCompatibleStateImageBehavior = false;
            this.lv_ClassInfos.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Name";
            this.columnHeader3.Width = 110;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Color";
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "IsNG";
            // 
            // lbx_ModelInformation
            // 
            this.lbx_ModelInformation.FormattingEnabled = true;
            this.lbx_ModelInformation.ItemHeight = 18;
            this.lbx_ModelInformation.Location = new System.Drawing.Point(4, 22);
            this.lbx_ModelInformation.Margin = new System.Windows.Forms.Padding(4);
            this.lbx_ModelInformation.Name = "lbx_ModelInformation";
            this.lbx_ModelInformation.Size = new System.Drawing.Size(347, 94);
            this.lbx_ModelInformation.TabIndex = 24;
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label24.Location = new System.Drawing.Point(4, 0);
            this.label24.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(169, 18);
            this.label24.TabIndex = 20;
            this.label24.Text = "Model Information";
            // 
            // AIModuleProp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "AIModuleProp";
            this.Size = new System.Drawing.Size(655, 620);
            this.panel6.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lbx_ResultDetail;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.ListView lv_Result;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label dasda;
        private System.Windows.Forms.Label lblAreaFilter;
        private System.Windows.Forms.TextBox txtMinArea;
        private System.Windows.Forms.TextBox txtMaxArea;
        private System.Windows.Forms.ComboBox cbAIModelType;
        private System.Windows.Forms.Button btnInspAI;
        private System.Windows.Forms.TextBox txtAIModelPath;
        private System.Windows.Forms.Button btnLoadModel;
        private System.Windows.Forms.Button btnSelAIModel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox Txt_ModuleInfo;
        private System.Windows.Forms.Label Lbl_ModuleInfo;
        private System.Windows.Forms.ListView lv_ClassInfos;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ListBox lbx_ModelInformation;
        private System.Windows.Forms.Label label24;
    }
}

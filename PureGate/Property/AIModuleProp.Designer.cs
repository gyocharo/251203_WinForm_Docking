using PureGate.Core;

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
            if (disposing)
            {
                // [ADD] 이벤트 구독 해제
                try { Global.Inst.InspStage.InspectionCompleted -= InspStage_InspectionCompleted; }
                catch { }

                if (components != null)
                {
                    components.Dispose();
                }
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
            this.txtAIModelPath = new System.Windows.Forms.TextBox();
            this.btnLoadModel = new System.Windows.Forms.Button();
            this.btnSelAIModel = new System.Windows.Forms.Button();
            this.btnInspAI = new System.Windows.Forms.Button();
            this.Txt_ModuleInfo = new System.Windows.Forms.TextBox();
            this.lv_ClassInfos = new System.Windows.Forms.ListView();
            this.lbx_ModelInformation = new System.Windows.Forms.ListBox();
            this.Lbl_ModuleInfo = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.cbAIModelType = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblAreaFilter = new System.Windows.Forms.Label();
            this.txtMaxArea = new System.Windows.Forms.TextBox();
            this.txtMinArea = new System.Windows.Forms.TextBox();
            this.dasda = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnInspAI
            // 
            this.btnInspAI.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnInspAI.Location = new System.Drawing.Point(442, 162);
            this.btnInspAI.Name = "btnInspAI";
            this.btnInspAI.Size = new System.Drawing.Size(94, 42);
            this.btnInspAI.TabIndex = 9;
            this.btnInspAI.Text = "AI 검사";
            this.btnInspAI.UseVisualStyleBackColor = true;
            this.btnInspAI.Click += new System.EventHandler(this.btnInspAI_Click);
            // 
            // txtAIModelPath
            // 
            this.txtAIModelPath.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtAIModelPath.Location = new System.Drawing.Point(3, 3);
            this.txtAIModelPath.Name = "txtAIModelPath";
            this.txtAIModelPath.ReadOnly = true;
            this.txtAIModelPath.Size = new System.Drawing.Size(533, 25);
            this.txtAIModelPath.TabIndex = 6;
            // 
            // btnLoadModel
            // 
            this.btnLoadModel.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnLoadModel.Location = new System.Drawing.Point(272, 179);
            this.btnLoadModel.Margin = new System.Windows.Forms.Padding(4);
            this.btnLoadModel.Location = new System.Drawing.Point(442, 113);
            this.btnLoadModel.Name = "btnLoadModel";
            this.btnLoadModel.Size = new System.Drawing.Size(118, 64);
            this.btnLoadModel.TabIndex = 8;
            this.btnLoadModel.Text = "모델 로딩";
            this.btnLoadModel.UseVisualStyleBackColor = true;
            this.btnLoadModel.Click += new System.EventHandler(this.btnLoadModel_Click);
            // 
            // btnSelAIModel
            // 
            this.btnSelAIModel.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnSelAIModel.Location = new System.Drawing.Point(272, 104);
            this.btnSelAIModel.Margin = new System.Windows.Forms.Padding(4);
            this.btnSelAIModel.Name = "btnSelAIModel";
            this.btnSelAIModel.Size = new System.Drawing.Size(94, 43);
            this.btnSelAIModel.TabIndex = 7;
            this.btnSelAIModel.Text = "AI모델 선택";
            this.btnSelAIModel.UseVisualStyleBackColor = true;
            this.btnSelAIModel.Click += new System.EventHandler(this.btnSelAIModel_Click);
            // 
            // Txt_ModuleInfo
            // 
            this.Txt_ModuleInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Txt_ModuleInfo.Location = new System.Drawing.Point(4, 275);
            this.Txt_ModuleInfo.Margin = new System.Windows.Forms.Padding(4);
            this.Txt_ModuleInfo.Multiline = true;
            this.Txt_ModuleInfo.Name = "Txt_ModuleInfo";
            this.Txt_ModuleInfo.Size = new System.Drawing.Size(533, 17);
            this.Txt_ModuleInfo.TabIndex = 27;
            // 
            // lv_ClassInfos
            // 
            this.lv_ClassInfos.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lv_ClassInfos.GridLines = true;
            this.lv_ClassInfos.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lv_ClassInfos.HideSelection = false;
            this.lv_ClassInfos.Location = new System.Drawing.Point(3, 90);
            this.lv_ClassInfos.MultiSelect = false;
            this.lv_ClassInfos.Name = "lv_ClassInfos";
            this.lv_ClassInfos.Size = new System.Drawing.Size(533, 117);
            this.lv_ClassInfos.TabIndex = 25;
            this.lv_ClassInfos.UseCompatibleStateImageBehavior = false;
            this.lv_ClassInfos.View = System.Windows.Forms.View.Details;
            // 
            // lbx_ModelInformation
            // 
            this.lbx_ModelInformation.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbx_ModelInformation.FormattingEnabled = true;
            this.lbx_ModelInformation.ItemHeight = 15;
            this.lbx_ModelInformation.Location = new System.Drawing.Point(3, 19);
            this.lbx_ModelInformation.Name = "lbx_ModelInformation";
            this.lbx_ModelInformation.Size = new System.Drawing.Size(533, 65);
            this.lbx_ModelInformation.TabIndex = 24;
            // 
            // Lbl_ModuleInfo
            // 
            this.Lbl_ModuleInfo.AutoSize = true;
            this.Lbl_ModuleInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Lbl_ModuleInfo.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.Lbl_ModuleInfo.Location = new System.Drawing.Point(3, 210);
            this.Lbl_ModuleInfo.Name = "Lbl_ModuleInfo";
            this.Lbl_ModuleInfo.Size = new System.Drawing.Size(386, 19);
            this.Lbl_ModuleInfo.TabIndex = 26;
            this.Lbl_ModuleInfo.Text = "모듈 정보";
            // 
            // label24
            // 
            this.label24.AutoSize = true;
            this.label24.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label24.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label24.Location = new System.Drawing.Point(3, 0);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(386, 19);
            this.label24.TabIndex = 20;
            this.label24.Text = "모델 정보";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Controls.Add(this.Txt_ModuleInfo, 0, 4);
            this.tableLayoutPanel3.Controls.Add(this.label24, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.Lbl_ModuleInfo, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.lv_ClassInfos, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.lbx_ModelInformation, 0, 1);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 5;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18.24324F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 81.75676F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 123F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(539, 255);
            this.tableLayoutPanel3.TabIndex = 19;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.txtAIModelPath, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.cbAIModelType, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.btnSelAIModel, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.btnLoadModel, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.panel1, 0, 4);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 5;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52.58855F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 47.41145F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(539, 275);
            this.tableLayoutPanel2.TabIndex = 20;
            // 
            // cbAIModelType
            // 
            this.cbAIModelType.Dock = System.Windows.Forms.DockStyle.Right;
            this.cbAIModelType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAIModelType.FormattingEnabled = true;
            this.cbAIModelType.Location = new System.Drawing.Point(4, 57);
            this.cbAIModelType.Margin = new System.Windows.Forms.Padding(4);
            this.cbAIModelType.Name = "cbAIModelType";
            this.cbAIModelType.Size = new System.Drawing.Size(386, 26);
            this.cbAIModelType.TabIndex = 5;
            this.cbAIModelType.SelectedIndexChanged += new System.EventHandler(this.cbAIModelType_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.lblAreaFilter);
            this.panel1.Controls.Add(this.txtMaxArea);
            this.panel1.Controls.Add(this.txtMinArea);
            this.panel1.Controls.Add(this.dasda);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(2, 209);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(535, 64);
            this.panel1.TabIndex = 23;
            // 
            // lblAreaFilter
            // 
            this.lblAreaFilter.AutoSize = true;
            this.lblAreaFilter.Location = new System.Drawing.Point(12, 38);
            this.lblAreaFilter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblAreaFilter.Name = "lblAreaFilter";
            this.lblAreaFilter.Size = new System.Drawing.Size(161, 15);
            this.lblAreaFilter.TabIndex = 24;
            this.lblAreaFilter.Text = "영역 필터 (최소값 ~ 최대값)";
            // 
            // txtMaxArea
            // 
            this.txtMaxArea.Location = new System.Drawing.Point(4, 3);
            this.txtMaxArea.Name = "txtMaxArea";
            this.txtMaxArea.Size = new System.Drawing.Size(70, 25);
            this.txtMaxArea.TabIndex = 14;
            // 
            // txtMinArea
            // 
            this.txtMinArea.Location = new System.Drawing.Point(103, 3);
            this.txtMinArea.Name = "txtMinArea";
            this.txtMinArea.Size = new System.Drawing.Size(70, 25);
            this.txtMinArea.TabIndex = 15;
            // 
            // dasda
            // 
            this.dasda.AutoSize = true;
            this.dasda.Location = new System.Drawing.Point(79, 6);
            this.dasda.Name = "dasda";
            this.dasda.Size = new System.Drawing.Size(18, 15);
            this.dasda.TabIndex = 17;
            this.dasda.Text = "~";
            // 
            // cbAIModelType
            // 
            this.cbAIModelType.Dock = System.Windows.Forms.DockStyle.Right;
            this.cbAIModelType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAIModelType.FormattingEnabled = true;
            this.cbAIModelType.Location = new System.Drawing.Point(223, 35);
            this.cbAIModelType.Name = "cbAIModelType";
            this.cbAIModelType.Size = new System.Drawing.Size(313, 23);
            this.cbAIModelType.TabIndex = 5;
            this.cbAIModelType.SelectedIndexChanged += new System.EventHandler(this.cbAIModelType_SelectedIndexChanged);
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.tableLayoutPanel3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 416);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(541, 257);
            this.panel2.TabIndex = 21;
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.tableLayoutPanel2);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Margin = new System.Windows.Forms.Padding(2);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(541, 277);
            this.panel3.TabIndex = 22;
            // 
            // AIModuleProp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Name = "AIModuleProp";
            this.Size = new System.Drawing.Size(541, 673);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TextBox txtAIModelPath;
        private System.Windows.Forms.Button btnLoadModel;
        private System.Windows.Forms.Button btnInspAI;
        private System.Windows.Forms.Button btnSelAIModel;
        private System.Windows.Forms.TextBox Txt_ModuleInfo;
        private System.Windows.Forms.Label Lbl_ModuleInfo;
        private System.Windows.Forms.ListView lv_ClassInfos;
        private System.Windows.Forms.ListBox lbx_ModelInformation;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox txtMinArea;
        private System.Windows.Forms.Label dasda;
        private System.Windows.Forms.TextBox txtMaxArea;
        private System.Windows.Forms.ComboBox cbAIModelType;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblAreaFilter;
    }
}

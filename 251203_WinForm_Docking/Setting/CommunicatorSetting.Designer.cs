namespace _251203_WinForm_Docking.Setting
{
    partial class CommunicatorSetting
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.txtMachine = new System.Windows.Forms.TextBox();
            this.txtIpAddr = new System.Windows.Forms.TextBox();
            this.cmbCommType = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "설비명";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "통신타입";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 18);
            this.label3.TabIndex = 2;
            this.label3.Text = "IP주소";
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(256, 139);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(100, 43);
            this.btnApply.TabIndex = 3;
            this.btnApply.Text = "적용";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // txtMachine
            // 
            this.txtMachine.Location = new System.Drawing.Point(108, 19);
            this.txtMachine.Name = "txtMachine";
            this.txtMachine.Size = new System.Drawing.Size(248, 28);
            this.txtMachine.TabIndex = 4;
            // 
            // txtIpAddr
            // 
            this.txtIpAddr.Location = new System.Drawing.Point(108, 99);
            this.txtIpAddr.Name = "txtIpAddr";
            this.txtIpAddr.Size = new System.Drawing.Size(248, 28);
            this.txtIpAddr.TabIndex = 6;
            // 
            // cmbCommType
            // 
            this.cmbCommType.FormattingEnabled = true;
            this.cmbCommType.Location = new System.Drawing.Point(108, 59);
            this.cmbCommType.Name = "cmbCommType";
            this.cmbCommType.Size = new System.Drawing.Size(248, 26);
            this.cmbCommType.TabIndex = 7;
            // 
            // CommunicatorSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmbCommType);
            this.Controls.Add(this.txtIpAddr);
            this.Controls.Add(this.txtMachine);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "CommunicatorSetting";
            this.Size = new System.Drawing.Size(370, 196);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.TextBox txtMachine;
        private System.Windows.Forms.TextBox txtIpAddr;
        private System.Windows.Forms.ComboBox cmbCommType;
    }
}

namespace _251203_WinForm_Docking.Property
{
    partial class MatchInspProp
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
                txtExtendX.Leave -= OnUpdateValue;
                txtExtendY.Leave -= OnUpdateValue;
                txtScore.Leave -= OnUpdateValue;

                patternImageEditor.ButtonChanged -= PatternImage_ButtonChanged;
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
            this.chkUse = new System.Windows.Forms.CheckBox();
            this.grpMatch = new System.Windows.Forms.GroupBox();
            this.txtExtendX = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtScore = new System.Windows.Forms.TextBox();
            this.txtExtendY = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkInvertResult = new System.Windows.Forms.CheckBox();
            this.patternImageEditor = new _251203_WinForm_Docking.UIControl.PatternImageEditor();
            this.grpMatch.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkUse
            // 
            this.chkUse.AutoSize = true;
            this.chkUse.Location = new System.Drawing.Point(18, 17);
            this.chkUse.Name = "chkUse";
            this.chkUse.Size = new System.Drawing.Size(70, 22);
            this.chkUse.TabIndex = 0;
            this.chkUse.Text = "검사";
            this.chkUse.UseVisualStyleBackColor = true;
            this.chkUse.CheckedChanged += new System.EventHandler(this.chkUse_CheckedChanged);
            // 
            // grpMatch
            // 
            this.grpMatch.Controls.Add(this.chkInvertResult);
            this.grpMatch.Controls.Add(this.label3);
            this.grpMatch.Controls.Add(this.txtExtendY);
            this.grpMatch.Controls.Add(this.txtScore);
            this.grpMatch.Controls.Add(this.txtExtendX);
            this.grpMatch.Controls.Add(this.label2);
            this.grpMatch.Controls.Add(this.label1);
            this.grpMatch.Location = new System.Drawing.Point(4, 46);
            this.grpMatch.Name = "grpMatch";
            this.grpMatch.Size = new System.Drawing.Size(436, 169);
            this.grpMatch.TabIndex = 1;
            this.grpMatch.TabStop = false;
            // 
            // txtExtendX
            // 
            this.txtExtendX.Location = new System.Drawing.Point(125, 25);
            this.txtExtendX.Name = "txtExtendX";
            this.txtExtendX.Size = new System.Drawing.Size(77, 28);
            this.txtExtendX.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "매칭스코어";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "확장영역";
            // 
            // txtScore
            // 
            this.txtScore.Location = new System.Drawing.Point(125, 69);
            this.txtScore.Name = "txtScore";
            this.txtScore.Size = new System.Drawing.Size(77, 28);
            this.txtScore.TabIndex = 3;
            // 
            // txtExtendY
            // 
            this.txtExtendY.Location = new System.Drawing.Point(233, 25);
            this.txtExtendY.Name = "txtExtendY";
            this.txtExtendY.Size = new System.Drawing.Size(77, 28);
            this.txtExtendY.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(208, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(19, 18);
            this.label3.TabIndex = 5;
            this.label3.Text = "X";
            // 
            // chkInvertResult
            // 
            this.chkInvertResult.AutoSize = true;
            this.chkInvertResult.Location = new System.Drawing.Point(17, 115);
            this.chkInvertResult.Name = "chkInvertResult";
            this.chkInvertResult.Size = new System.Drawing.Size(112, 22);
            this.chkInvertResult.TabIndex = 6;
            this.chkInvertResult.Text = "결과 반전";
            this.chkInvertResult.UseVisualStyleBackColor = true;
            this.chkInvertResult.CheckedChanged += new System.EventHandler(this.chkInvertResult_CheckedChanged);
            // 
            // patternImageEditor
            // 
            this.patternImageEditor.Location = new System.Drawing.Point(4, 234);
            this.patternImageEditor.Name = "patternImageEditor";
            this.patternImageEditor.Size = new System.Drawing.Size(436, 181);
            this.patternImageEditor.TabIndex = 2;
            // 
            // MatchInspProp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.patternImageEditor);
            this.Controls.Add(this.grpMatch);
            this.Controls.Add(this.chkUse);
            this.Name = "MatchInspProp";
            this.Size = new System.Drawing.Size(447, 438);
            this.grpMatch.ResumeLayout(false);
            this.grpMatch.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkUse;
        private System.Windows.Forms.GroupBox grpMatch;
        private System.Windows.Forms.TextBox txtExtendX;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtExtendY;
        private System.Windows.Forms.TextBox txtScore;
        private System.Windows.Forms.CheckBox chkInvertResult;
        private UIControl.PatternImageEditor patternImageEditor;
    }
}

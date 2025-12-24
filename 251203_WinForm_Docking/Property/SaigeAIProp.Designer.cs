namespace _251203_WinForm_Docking.Property
{
    partial class SaigeAIProp
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
            this.cmb_Model = new System.Windows.Forms.ComboBox();
            this.btn_Apply = new System.Windows.Forms.Button();
            this.txt_Model_Path = new System.Windows.Forms.TextBox();
            this.btn_Model = new System.Windows.Forms.Button();
            this.btn_Model_Load = new System.Windows.Forms.Button();
            this.txt_Area = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cmb_Model
            // 
            this.cmb_Model.FormattingEnabled = true;
            this.cmb_Model.Items.AddRange(new object[] {
            "SEG",
            "DET",
            "IAD"});
            this.cmb_Model.Location = new System.Drawing.Point(17, 21);
            this.cmb_Model.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmb_Model.Name = "cmb_Model";
            this.cmb_Model.Size = new System.Drawing.Size(171, 26);
            this.cmb_Model.TabIndex = 0;
            this.cmb_Model.SelectedIndexChanged += new System.EventHandler(this.cmb_Model_SelectedIndexChanged);
            // 
            // btn_Apply
            // 
            this.btn_Apply.Location = new System.Drawing.Point(179, 270);
            this.btn_Apply.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btn_Apply.Name = "btn_Apply";
            this.btn_Apply.Size = new System.Drawing.Size(139, 60);
            this.btn_Apply.TabIndex = 1;
            this.btn_Apply.Text = "적용";
            this.btn_Apply.UseVisualStyleBackColor = true;
            this.btn_Apply.Click += new System.EventHandler(this.btn_Apply_Click);
            // 
            // txt_Model_Path
            // 
            this.txt_Model_Path.Location = new System.Drawing.Point(17, 60);
            this.txt_Model_Path.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txt_Model_Path.Name = "txt_Model_Path";
            this.txt_Model_Path.ReadOnly = true;
            this.txt_Model_Path.Size = new System.Drawing.Size(298, 28);
            this.txt_Model_Path.TabIndex = 2;
            // 
            // btn_Model
            // 
            this.btn_Model.Location = new System.Drawing.Point(179, 134);
            this.btn_Model.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btn_Model.Name = "btn_Model";
            this.btn_Model.Size = new System.Drawing.Size(139, 60);
            this.btn_Model.TabIndex = 3;
            this.btn_Model.Text = "모델 선택";
            this.btn_Model.UseVisualStyleBackColor = true;
            this.btn_Model.Click += new System.EventHandler(this.btn_Model_Click);
            // 
            // btn_Model_Load
            // 
            this.btn_Model_Load.Location = new System.Drawing.Point(179, 202);
            this.btn_Model_Load.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Model_Load.Name = "btn_Model_Load";
            this.btn_Model_Load.Size = new System.Drawing.Size(139, 60);
            this.btn_Model_Load.TabIndex = 4;
            this.btn_Model_Load.Text = "모델 로드";
            this.btn_Model_Load.UseVisualStyleBackColor = true;
            this.btn_Model_Load.Click += new System.EventHandler(this.btn_Model_Load_Click);
            // 
            // txt_Area
            // 
            this.txt_Area.Location = new System.Drawing.Point(17, 134);
            this.txt_Area.Name = "txt_Area";
            this.txt_Area.Size = new System.Drawing.Size(124, 28);
            this.txt_Area.TabIndex = 5;
            // 
            // SaigeAIProp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txt_Area);
            this.Controls.Add(this.btn_Model_Load);
            this.Controls.Add(this.btn_Model);
            this.Controls.Add(this.txt_Model_Path);
            this.Controls.Add(this.btn_Apply);
            this.Controls.Add(this.cmb_Model);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "SaigeAIProp";
            this.Size = new System.Drawing.Size(609, 687);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmb_Model;
        private System.Windows.Forms.Button btn_Apply;
        private System.Windows.Forms.TextBox txt_Model_Path;
        private System.Windows.Forms.Button btn_Model;
        private System.Windows.Forms.Button btn_Model_Load;
        public System.Windows.Forms.TextBox txt_Area;
    }
}

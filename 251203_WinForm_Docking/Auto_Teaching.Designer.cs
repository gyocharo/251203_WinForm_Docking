namespace _251203_WinForm_Docking
{
    partial class Auto_Teaching
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
            this.btn_Apply = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.Num_Entity = new System.Windows.Forms.NumericUpDown();
            this.Num_Score = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.Num_Entity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Num_Score)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_Apply
            // 
            this.btn_Apply.Location = new System.Drawing.Point(437, 228);
            this.btn_Apply.Name = "btn_Apply";
            this.btn_Apply.Size = new System.Drawing.Size(117, 65);
            this.btn_Apply.TabIndex = 0;
            this.btn_Apply.Text = "적용";
            this.btn_Apply.UseVisualStyleBackColor = true;
            this.btn_Apply.Click += new System.EventHandler(this.btn_Apply_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(76, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "개수";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(292, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "스코어";
            // 
            // Num_Entity
            // 
            this.Num_Entity.Location = new System.Drawing.Point(126, 118);
            this.Num_Entity.Name = "Num_Entity";
            this.Num_Entity.Size = new System.Drawing.Size(120, 28);
            this.Num_Entity.TabIndex = 3;
            // 
            // Num_Score
            // 
            this.Num_Score.Location = new System.Drawing.Point(360, 118);
            this.Num_Score.Name = "Num_Score";
            this.Num_Score.Size = new System.Drawing.Size(120, 28);
            this.Num_Score.TabIndex = 4;
            // 
            // Auto_Teaching
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(587, 327);
            this.Controls.Add(this.Num_Score);
            this.Controls.Add(this.Num_Entity);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Apply);
            this.Name = "Auto_Teaching";
            this.Text = "Auto_Teaching";
            ((System.ComponentModel.ISupportInitialize)(this.Num_Entity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Num_Score)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Apply;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown Num_Entity;
        private System.Windows.Forms.NumericUpDown Num_Score;
    }
}
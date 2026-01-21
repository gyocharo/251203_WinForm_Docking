namespace PureGate
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericCount = new System.Windows.Forms.NumericUpDown();
            this.numericScore = new System.Windows.Forms.NumericUpDown();
            this.btnApply = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericCount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericScore)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(57, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Count";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(265, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "Score";
            // 
            // numericCount
            // 
            this.numericCount.Location = new System.Drawing.Point(119, 58);
            this.numericCount.Name = "numericCount";
            this.numericCount.Size = new System.Drawing.Size(120, 28);
            this.numericCount.TabIndex = 2;
            // 
            // numericScore
            // 
            this.numericScore.Location = new System.Drawing.Point(327, 58);
            this.numericScore.Name = "numericScore";
            this.numericScore.Size = new System.Drawing.Size(120, 28);
            this.numericScore.TabIndex = 3;
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(404, 215);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(112, 53);
            this.btnApply.TabIndex = 4;
            this.btnApply.Text = "적용";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // Auto_Teaching
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(543, 295);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.numericScore);
            this.Controls.Add(this.numericCount);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Auto_Teaching";
            this.Text = "Auto_Teaching";
            ((System.ComponentModel.ISupportInitialize)(this.numericCount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericScore)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericCount;
        private System.Windows.Forms.NumericUpDown numericScore;
        private System.Windows.Forms.Button btnApply;
    }
}
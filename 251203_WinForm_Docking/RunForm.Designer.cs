namespace _251203_WinForm_Docking
{
    partial class RunForm
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
            this.btn_Grab = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnLive = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Grab
            // 
            this.btn_Grab.Location = new System.Drawing.Point(12, 12);
            this.btn_Grab.Name = "btn_Grab";
            this.btn_Grab.Size = new System.Drawing.Size(155, 71);
            this.btn_Grab.TabIndex = 0;
            this.btn_Grab.Text = "촬영";
            this.btn_Grab.UseVisualStyleBackColor = true;
            this.btn_Grab.Click += new System.EventHandler(this.btn_Grab_Click);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(173, 12);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(155, 71);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "검사";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnLive
            // 
            this.btnLive.Location = new System.Drawing.Point(334, 12);
            this.btnLive.Name = "btnLive";
            this.btnLive.Size = new System.Drawing.Size(155, 71);
            this.btnLive.TabIndex = 2;
            this.btnLive.Text = "LIVE";
            this.btnLive.UseVisualStyleBackColor = true;
            this.btnLive.Click += new System.EventHandler(this.btnLive_Click);
            // 
            // RunForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnLive);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btn_Grab);
            this.Name = "RunForm";
            this.Text = "RunForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_Grab;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnLive;
    }
}
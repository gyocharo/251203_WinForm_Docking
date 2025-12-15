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
            this.btn＿Grab = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn＿Grab
            // 
            this.btn＿Grab.Location = new System.Drawing.Point(12, 12);
            this.btn＿Grab.Name = "btn＿Grab";
            this.btn＿Grab.Size = new System.Drawing.Size(155, 71);
            this.btn＿Grab.TabIndex = 0;
            this.btn＿Grab.Text = "촬영";
            this.btn＿Grab.UseVisualStyleBackColor = true;
            // 
            // RunForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btn＿Grab);
            this.Name = "RunForm";
            this.Text = "RunForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn＿Grab;
    }
}
namespace _251203_WinForm_Docking
{
    partial class CameraForm
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
            this.imageViewer = new _251203_WinForm_Docking.UIControl.ImageViewCtrl();
            this.mainViewToolbar = new JidamVision4.UIControl.MainViewToolbar();
            this.SuspendLayout();
            // 
            // imageViewer
            // 
            this.imageViewer.Dock = System.Windows.Forms.DockStyle.Left;
            this.imageViewer.Location = new System.Drawing.Point(0, 0);
            this.imageViewer.Name = "imageViewer";
            this.imageViewer.Size = new System.Drawing.Size(800, 450);
            this.imageViewer.TabIndex = 0;
            this.imageViewer.WorkingState = "";
            // 
            // mainViewToolbar
            // 
            this.mainViewToolbar.Dock = System.Windows.Forms.DockStyle.Right;
            this.mainViewToolbar.Location = new System.Drawing.Point(724, 0);
            this.mainViewToolbar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.mainViewToolbar.Name = "mainViewToolbar";
            this.mainViewToolbar.Size = new System.Drawing.Size(76, 450);
            this.mainViewToolbar.TabIndex = 1;
            // 
            // CameraForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.mainViewToolbar);
            this.Controls.Add(this.imageViewer);
            this.Name = "CameraForm";
            this.Text = "CameraForm";
            this.Resize += new System.EventHandler(this.CameraForm_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private UIControl.ImageViewCtrl imageViewer;
        private JidamVision4.UIControl.MainViewToolbar mainViewToolbar;
    }
}
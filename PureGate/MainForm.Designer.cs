namespace PureGate
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.SideMenu = new System.Windows.Forms.Panel();
            this.btnCycleMode = new System.Windows.Forms.Button();
            this.btnSetUp = new System.Windows.Forms.Button();
            this.btnSetROI = new System.Windows.Forms.Button();
            this.btnImage = new System.Windows.Forms.Button();
            this.btnModel = new System.Windows.Forms.Button();
            this.checkBoxHide = new System.Windows.Forms.CheckBox();
            this.btnOverview = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.timerSliding = new System.Windows.Forms.Timer(this.components);
            this.SideMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // SideMenu
            // 
            this.SideMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.SideMenu.Controls.Add(this.btnCycleMode);
            this.SideMenu.Controls.Add(this.btnSetUp);
            this.SideMenu.Controls.Add(this.btnSetROI);
            this.SideMenu.Controls.Add(this.btnImage);
            this.SideMenu.Controls.Add(this.btnModel);
            this.SideMenu.Controls.Add(this.checkBoxHide);
            this.SideMenu.Controls.Add(this.btnOverview);
            this.SideMenu.Controls.Add(this.pictureBox1);
            this.SideMenu.Location = new System.Drawing.Point(0, 0);
            this.SideMenu.Name = "SideMenu";
            this.SideMenu.Size = new System.Drawing.Size(261, 1444);
            this.SideMenu.TabIndex = 3;
            // 
            // btnCycleMode
            // 
            this.btnCycleMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCycleMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCycleMode.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnCycleMode.ForeColor = System.Drawing.Color.Black;
            this.btnCycleMode.Location = new System.Drawing.Point(0, 373);
            this.btnCycleMode.Name = "btnCycleMode";
            this.btnCycleMode.Size = new System.Drawing.Size(261, 67);
            this.btnCycleMode.TabIndex = 11;
            this.btnCycleMode.Text = "Cycle Mode";
            this.btnCycleMode.UseVisualStyleBackColor = true;
            this.btnCycleMode.Click += new System.EventHandler(this.btnCycleMode_Click);
            // 
            // btnSetUp
            // 
            this.btnSetUp.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnSetUp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetUp.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnSetUp.ForeColor = System.Drawing.Color.Black;
            this.btnSetUp.Location = new System.Drawing.Point(0, 1344);
            this.btnSetUp.Name = "btnSetUp";
            this.btnSetUp.Size = new System.Drawing.Size(261, 67);
            this.btnSetUp.TabIndex = 10;
            this.btnSetUp.Text = "SetUp";
            this.btnSetUp.UseVisualStyleBackColor = true;
            this.btnSetUp.Click += new System.EventHandler(this.btnSetUp_Click);
            // 
            // btnSetROI
            // 
            this.btnSetROI.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnSetROI.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetROI.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnSetROI.ForeColor = System.Drawing.Color.Black;
            this.btnSetROI.Location = new System.Drawing.Point(0, 306);
            this.btnSetROI.Name = "btnSetROI";
            this.btnSetROI.Size = new System.Drawing.Size(261, 67);
            this.btnSetROI.TabIndex = 9;
            this.btnSetROI.Text = "Set ROI";
            this.btnSetROI.UseVisualStyleBackColor = true;
            this.btnSetROI.Click += new System.EventHandler(this.btnSetROI_Click);
            // 
            // btnImage
            // 
            this.btnImage.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnImage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImage.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnImage.ForeColor = System.Drawing.Color.Black;
            this.btnImage.Location = new System.Drawing.Point(0, 239);
            this.btnImage.Name = "btnImage";
            this.btnImage.Size = new System.Drawing.Size(261, 67);
            this.btnImage.TabIndex = 13;
            this.btnImage.Text = "Image";
            this.btnImage.UseVisualStyleBackColor = true;
            this.btnImage.Click += new System.EventHandler(this.btnImage_Click);
            // 
            // btnModel
            // 
            this.btnModel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.btnModel.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnModel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnModel.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnModel.ForeColor = System.Drawing.Color.Black;
            this.btnModel.Location = new System.Drawing.Point(0, 172);
            this.btnModel.Name = "btnModel";
            this.btnModel.Size = new System.Drawing.Size(261, 67);
            this.btnModel.TabIndex = 8;
            this.btnModel.Text = "Model";
            this.btnModel.UseVisualStyleBackColor = false;
            this.btnModel.Click += new System.EventHandler(this.btnModel_Click);
            // 
            // checkBoxHide
            // 
            this.checkBoxHide.Appearance = System.Windows.Forms.Appearance.Button;
            this.checkBoxHide.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.checkBoxHide.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.checkBoxHide.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.checkBoxHide.Font = new System.Drawing.Font("굴림", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.checkBoxHide.ForeColor = System.Drawing.Color.Black;
            this.checkBoxHide.Location = new System.Drawing.Point(0, 1411);
            this.checkBoxHide.Name = "checkBoxHide";
            this.checkBoxHide.Size = new System.Drawing.Size(261, 33);
            this.checkBoxHide.TabIndex = 6;
            this.checkBoxHide.Text = "<";
            this.checkBoxHide.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBoxHide.UseVisualStyleBackColor = false;
            this.checkBoxHide.Click += new System.EventHandler(this.checkBoxHide_CheckedChanged);
            // 
            // btnOverview
            // 
            this.btnOverview.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnOverview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOverview.Font = new System.Drawing.Font("굴림", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnOverview.ForeColor = System.Drawing.Color.Black;
            this.btnOverview.Location = new System.Drawing.Point(0, 105);
            this.btnOverview.Name = "btnOverview";
            this.btnOverview.Size = new System.Drawing.Size(261, 67);
            this.btnOverview.TabIndex = 2;
            this.btnOverview.Text = "Overview";
            this.btnOverview.UseVisualStyleBackColor = true;
            this.btnOverview.Click += new System.EventHandler(this.btnOverview_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox1.Image = global::PureGate.Properties.Resources.이미지;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(261, 105);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 12;
            this.pictureBox1.TabStop = false;
            // 
            // timerSliding
            // 
            this.timerSliding.Interval = 10;
            this.timerSliding.Tick += new System.EventHandler(this.timerSliding_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1978, 1444);
            this.Name = "MainForm";
            this.Text = "PureGate";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.SideMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel SideMenu;
        private System.Windows.Forms.Button btnCycleMode;
        private System.Windows.Forms.Button btnSetUp;
        private System.Windows.Forms.Button btnSetROI;
        private System.Windows.Forms.Button btnModel;
        private System.Windows.Forms.CheckBox checkBoxHide;
        private System.Windows.Forms.Button btnOverview;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Timer timerSliding;
        private System.Windows.Forms.Button btnImage;
    }
}
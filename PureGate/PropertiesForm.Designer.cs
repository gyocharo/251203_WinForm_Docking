namespace PureGate
{
    partial class PropertiesForm
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
            this.tabPropControl1 = new System.Windows.Forms.TabControl();
            this.SuspendLayout();
            // 
            // tabPropControl1
            // 
            this.tabPropControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabPropControl1.Location = new System.Drawing.Point(0, 0);
            this.tabPropControl1.Name = "tabPropControl1";
            this.tabPropControl1.SelectedIndex = 0;
            this.tabPropControl1.Size = new System.Drawing.Size(800, 450);
            this.tabPropControl1.TabIndex = 0;
            // 
            // PropertiesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tabPropControl1);
            this.Name = "PropertiesForm";
            this.Text = "PropertiesForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabPropControl1;
    }
}
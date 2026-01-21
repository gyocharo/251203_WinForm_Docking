namespace PureGate
{
    partial class ModelTreeForm
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
            this.tvModelTree = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // tvModelTree
            // 
            this.tvModelTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvModelTree.Location = new System.Drawing.Point(0, 0);
            this.tvModelTree.Name = "tvModelTree";
            this.tvModelTree.Size = new System.Drawing.Size(800, 450);
            this.tvModelTree.TabIndex = 0;
            this.tvModelTree.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tvModelTree_MouseDown_1);
            // 
            // ModelTreeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tvModelTree);
            this.Name = "ModelTreeForm";
            this.Text = "ModelTreeForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tvModelTree;
    }
}
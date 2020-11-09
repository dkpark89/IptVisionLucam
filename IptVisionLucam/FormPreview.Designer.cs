namespace Ipt
{
	partial class FormPreview
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPreview));
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBoxIpl1 = new OpenCvSharp.UserInterface.PictureBoxIpl();
            this.labelROI = new System.Windows.Forms.Label();
            this.checkBoxViewROI = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIpl1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.labelROI);
            this.panel1.Controls.Add(this.checkBoxViewROI);
            this.panel1.Controls.Add(this.pictureBoxIpl1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(716, 400);
            this.panel1.TabIndex = 0;
            // 
            // pictureBoxIpl1
            // 
            this.pictureBoxIpl1.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxIpl1.Name = "pictureBoxIpl1";
            this.pictureBoxIpl1.Size = new System.Drawing.Size(628, 361);
            this.pictureBoxIpl1.TabIndex = 1;
            this.pictureBoxIpl1.TabStop = false;
            this.pictureBoxIpl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBoxIpl1_MouseClick);
            this.pictureBoxIpl1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxIpl1_MouseDown);
            this.pictureBoxIpl1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBoxIpl1_MouseMove);
            this.pictureBoxIpl1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBoxIpl1_MouseUp);
            // 
            // labelROI
            // 
            this.labelROI.BackColor = System.Drawing.Color.Cyan;
            this.labelROI.Location = new System.Drawing.Point(227, 162);
            this.labelROI.Name = "labelROI";
            this.labelROI.Size = new System.Drawing.Size(100, 63);
            this.labelROI.TabIndex = 15;
            this.labelROI.Text = "0";
            this.labelROI.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelROI.Visible = false;
            this.labelROI.MouseDown += new System.Windows.Forms.MouseEventHandler(this.labelROI_MouseDown);
            this.labelROI.MouseMove += new System.Windows.Forms.MouseEventHandler(this.labelROI_MouseMove);
            this.labelROI.MouseUp += new System.Windows.Forms.MouseEventHandler(this.labelROI_MouseUp);
            // 
            // checkBoxViewROI
            // 
            this.checkBoxViewROI.AutoSize = true;
            this.checkBoxViewROI.Location = new System.Drawing.Point(533, 5);
            this.checkBoxViewROI.Name = "checkBoxViewROI";
            this.checkBoxViewROI.Size = new System.Drawing.Size(76, 16);
            this.checkBoxViewROI.TabIndex = 14;
            this.checkBoxViewROI.Text = "View ROI";
            this.checkBoxViewROI.UseVisualStyleBackColor = true;
            this.checkBoxViewROI.CheckedChanged += new System.EventHandler(this.checkBoxViewROI_CheckedChanged);
            // 
            // FormPreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(716, 400);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormPreview";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormPreview";
            this.TopMost = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.FormPreview_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIpl1)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

        public System.Windows.Forms.Panel panel1;
        private OpenCvSharp.UserInterface.PictureBoxIpl pictureBoxIpl1;
        private System.Windows.Forms.Label labelROI;
        private System.Windows.Forms.CheckBox checkBoxViewROI;
    }
}
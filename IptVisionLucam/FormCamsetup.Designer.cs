namespace IptVisionLucam
{
    partial class FormCamsetup
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
            this.textBoxExposure = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonApply = new System.Windows.Forms.Button();
            this.textBoxGain = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxExposure
            // 
            this.textBoxExposure.Location = new System.Drawing.Point(80, 12);
            this.textBoxExposure.Name = "textBoxExposure";
            this.textBoxExposure.Size = new System.Drawing.Size(60, 21);
            this.textBoxExposure.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(146, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(23, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "ms";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(21, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "노출시간";
            // 
            // buttonApply
            // 
            this.buttonApply.Location = new System.Drawing.Point(12, 69);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(186, 45);
            this.buttonApply.TabIndex = 2;
            this.buttonApply.Text = "적용";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
            // 
            // textBoxGain
            // 
            this.textBoxGain.Location = new System.Drawing.Point(80, 39);
            this.textBoxGain.Name = "textBoxGain";
            this.textBoxGain.Size = new System.Drawing.Size(60, 21);
            this.textBoxGain.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(40, 42);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "GAIN";
            // 
            // FormCamsetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(209, 126);
            this.Controls.Add(this.buttonApply);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxGain);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxExposure);
            this.Name = "FormCamsetup";
            this.Text = "카메라 설정";
            this.Load += new System.EventHandler(this.FormCamsetup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxExposure;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.TextBox textBoxGain;
        private System.Windows.Forms.Label label4;
    }
}
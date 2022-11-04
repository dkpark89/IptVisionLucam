namespace Ipt
{
    partial class FormCheckLotNo
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
            this.textBoxPsCd = new System.Windows.Forms.TextBox();
            this.labelName = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonGet = new System.Windows.Forms.Button();
            this.labelResult = new System.Windows.Forms.Label();
            this.labelLotNumber = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBoxPsCd
            // 
            this.textBoxPsCd.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.textBoxPsCd.Location = new System.Drawing.Point(152, 56);
            this.textBoxPsCd.Name = "textBoxPsCd";
            this.textBoxPsCd.Size = new System.Drawing.Size(145, 29);
            this.textBoxPsCd.TabIndex = 16;
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelName.Location = new System.Drawing.Point(313, 59);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(30, 19);
            this.labelName.TabIndex = 11;
            this.labelName.Text = "( )";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label2.Location = new System.Drawing.Point(83, 59);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 19);
            this.label2.TabIndex = 14;
            this.label2.Text = "사번 :";
            // 
            // buttonGet
            // 
            this.buttonGet.BackColor = System.Drawing.Color.Orange;
            this.buttonGet.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.buttonGet.Location = new System.Drawing.Point(36, 107);
            this.buttonGet.Name = "buttonGet";
            this.buttonGet.Size = new System.Drawing.Size(352, 87);
            this.buttonGet.TabIndex = 10;
            this.buttonGet.Text = "사번을 입력하고 생산지시 읽기";
            this.buttonGet.UseVisualStyleBackColor = false;
            this.buttonGet.Click += new System.EventHandler(this.buttonGet_Click);
            // 
            // labelResult
            // 
            this.labelResult.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelResult.Location = new System.Drawing.Point(32, 221);
            this.labelResult.Name = "labelResult";
            this.labelResult.Size = new System.Drawing.Size(356, 116);
            this.labelResult.TabIndex = 8;
            this.labelResult.Text = ".";
            // 
            // labelLotNumber
            // 
            this.labelLotNumber.AutoSize = true;
            this.labelLotNumber.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelLotNumber.Location = new System.Drawing.Point(43, 25);
            this.labelLotNumber.Name = "labelLotNumber";
            this.labelLotNumber.Size = new System.Drawing.Size(103, 19);
            this.labelLotNumber.TabIndex = 9;
            this.labelLotNumber.Text = "로트넘버 :";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Enabled = false;
            this.buttonCancel.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.buttonCancel.Location = new System.Drawing.Point(36, 352);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(127, 37);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "취소";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Enabled = false;
            this.buttonOK.Font = new System.Drawing.Font("Gulim", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.buttonOK.Location = new System.Drawing.Point(261, 352);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(127, 37);
            this.buttonOK.TabIndex = 7;
            this.buttonOK.Text = "승인";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // FormCheckLotNo
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(422, 442);
            this.Controls.Add(this.textBoxPsCd);
            this.Controls.Add(this.labelName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonGet);
            this.Controls.Add(this.labelResult);
            this.Controls.Add(this.labelLotNumber);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Name = "FormCheckLotNo";
            this.Text = "FormCheckLotNo";
            this.Load += new System.EventHandler(this.FormCheckLotNo_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textBoxPsCd;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonGet;
        private System.Windows.Forms.Label labelResult;
        private System.Windows.Forms.Label labelLotNumber;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;

    }
}
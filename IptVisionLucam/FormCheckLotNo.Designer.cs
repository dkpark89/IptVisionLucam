namespace IptVision
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
            this.comboBoxGroup = new System.Windows.Forms.ComboBox();
            this.textBoxHeater = new System.Windows.Forms.TextBox();
            this.textBoxPsCd = new System.Windows.Forms.TextBox();
            this.labelName = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonGet = new System.Windows.Forms.Button();
            this.labelResult = new System.Windows.Forms.Label();
            this.labelLotNumber = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // comboBoxGroup
            // 
            this.comboBoxGroup.FormattingEnabled = true;
            this.comboBoxGroup.Items.AddRange(new object[] {
            "A조",
            "B조",
            "C조"});
            this.comboBoxGroup.Location = new System.Drawing.Point(285, 21);
            this.comboBoxGroup.Name = "comboBoxGroup";
            this.comboBoxGroup.Size = new System.Drawing.Size(68, 20);
            this.comboBoxGroup.TabIndex = 17;
            this.comboBoxGroup.SelectedIndexChanged += new System.EventHandler(this.comboBoxGroup_SelectedIndexChanged);
            // 
            // textBoxHeater
            // 
            this.textBoxHeater.Location = new System.Drawing.Point(670, 21);
            this.textBoxHeater.Name = "textBoxHeater";
            this.textBoxHeater.Size = new System.Drawing.Size(56, 21);
            this.textBoxHeater.TabIndex = 15;
            // 
            // textBoxPsCd
            // 
            this.textBoxPsCd.Location = new System.Drawing.Point(427, 21);
            this.textBoxPsCd.Name = "textBoxPsCd";
            this.textBoxPsCd.Size = new System.Drawing.Size(63, 21);
            this.textBoxPsCd.TabIndex = 16;
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(498, 25);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(19, 12);
            this.labelName.TabIndex = 11;
            this.labelName.Text = "( )";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(230, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 12);
            this.label3.TabIndex = 12;
            this.label3.Text = "작업조 :";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(604, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 12);
            this.label1.TabIndex = 13;
            this.label1.Text = "히터온도 :";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(381, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 12);
            this.label2.TabIndex = 14;
            this.label2.Text = "사번 :";
            // 
            // buttonGet
            // 
            this.buttonGet.BackColor = System.Drawing.Color.Red;
            this.buttonGet.Location = new System.Drawing.Point(756, 17);
            this.buttonGet.Name = "buttonGet";
            this.buttonGet.Size = new System.Drawing.Size(123, 29);
            this.buttonGet.TabIndex = 10;
            this.buttonGet.Text = "생산지시 읽기";
            this.buttonGet.UseVisualStyleBackColor = false;
            this.buttonGet.Click += new System.EventHandler(this.buttonGet_Click);
            // 
            // labelResult
            // 
            this.labelResult.Font = new System.Drawing.Font("굴림", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.labelResult.Location = new System.Drawing.Point(41, 67);
            this.labelResult.Name = "labelResult";
            this.labelResult.Size = new System.Drawing.Size(838, 379);
            this.labelResult.TabIndex = 8;
            this.labelResult.Text = ".";
            // 
            // labelLotNumber
            // 
            this.labelLotNumber.AutoSize = true;
            this.labelLotNumber.Location = new System.Drawing.Point(41, 25);
            this.labelLotNumber.Name = "labelLotNumber";
            this.labelLotNumber.Size = new System.Drawing.Size(65, 12);
            this.labelLotNumber.TabIndex = 9;
            this.labelLotNumber.Text = "로트넘버 : ";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(655, 480);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(71, 37);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "취소";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Enabled = false;
            this.buttonOK.Location = new System.Drawing.Point(752, 480);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(127, 37);
            this.buttonOK.TabIndex = 7;
            this.buttonOK.Text = "승인";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // FormCheckLotNo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(921, 535);
            this.Controls.Add(this.comboBoxGroup);
            this.Controls.Add(this.textBoxHeater);
            this.Controls.Add(this.textBoxPsCd);
            this.Controls.Add(this.labelName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
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

        private System.Windows.Forms.ComboBox comboBoxGroup;
        private System.Windows.Forms.TextBox textBoxHeater;
        private System.Windows.Forms.TextBox textBoxPsCd;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonGet;
        private System.Windows.Forms.Label labelResult;
        private System.Windows.Forms.Label labelLotNumber;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;

    }
}
namespace IptVisionLucam
{
    partial class UserControlStatus
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dataSetData = new System.Data.DataSet();
            this.tagData = new System.Data.DataTable();
            this.variable = new System.Data.DataTable();
            this.dataColumn1 = new System.Data.DataColumn();
            this.dataColumn2 = new System.Data.DataColumn();
            this.dataColumn3 = new System.Data.DataColumn();
            this.dataColumn4 = new System.Data.DataColumn();
            this.dataColumn5 = new System.Data.DataColumn();
            this.dataColumn6 = new System.Data.DataColumn();
            this.dataColumn7 = new System.Data.DataColumn();
            this.dataColumn8 = new System.Data.DataColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataSetData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tagData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.variable)).BeginInit();
            this.SuspendLayout();
            // 
            // dataSetData
            // 
            this.dataSetData.DataSetName = "NewDataSet";
            this.dataSetData.Tables.AddRange(new System.Data.DataTable[] {
            this.tagData,
            this.variable});
            // 
            // tagData
            // 
            this.tagData.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumn1,
            this.dataColumn2,
            this.dataColumn3,
            this.dataColumn4,
            this.dataColumn5,
            this.dataColumn6});
            this.tagData.TableName = "m_data";
            // 
            // variable
            // 
            this.variable.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumn7,
            this.dataColumn8});
            this.variable.TableName = "variable";
            // 
            // dataColumn1
            // 
            this.dataColumn1.ColumnName = "CenterState";
            this.dataColumn1.DataType = typeof(int);
            this.dataColumn1.DefaultValue = 0;
            // 
            // dataColumn2
            // 
            this.dataColumn2.ColumnName = "BoundaryState";
            this.dataColumn2.DataType = typeof(int);
            this.dataColumn2.DefaultValue = 0;
            // 
            // dataColumn3
            // 
            this.dataColumn3.ColumnName = "okIndex";
            this.dataColumn3.DataType = typeof(int);
            this.dataColumn3.DefaultValue = -1;
            // 
            // dataColumn4
            // 
            this.dataColumn4.ColumnName = "palIndex";
            this.dataColumn4.DataType = typeof(int);
            this.dataColumn4.DefaultValue = 0;
            // 
            // dataColumn5
            // 
            this.dataColumn5.ColumnName = "LenzId";
            this.dataColumn5.DataType = typeof(int);
            this.dataColumn5.DefaultValue = -1;
            // 
            // dataColumn6
            // 
            this.dataColumn6.ColumnName = "StageIndex";
            this.dataColumn6.DataType = typeof(int);
            this.dataColumn6.DefaultValue = 0;
            // 
            // dataColumn7
            // 
            this.dataColumn7.ColumnName = "m_okIndex";
            this.dataColumn7.DataType = typeof(int);
            this.dataColumn7.DefaultValue = 0;
            // 
            // dataColumn8
            // 
            this.dataColumn8.ColumnName = "m_palIndex";
            this.dataColumn8.DataType = typeof(int);
            this.dataColumn8.DefaultValue = 0;
            // 
            // UserControlStatus
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.Name = "UserControlStatus";
            this.Size = new System.Drawing.Size(239, 197);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.UserControlStatus_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.dataSetData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tagData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.variable)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Data.DataSet dataSetData;
        private System.Data.DataTable tagData;
        private System.Data.DataTable variable;
        private System.Data.DataColumn dataColumn1;
        private System.Data.DataColumn dataColumn2;
        private System.Data.DataColumn dataColumn3;
        private System.Data.DataColumn dataColumn4;
        private System.Data.DataColumn dataColumn5;
        private System.Data.DataColumn dataColumn6;
        private System.Data.DataColumn dataColumn7;
        private System.Data.DataColumn dataColumn8;
    }
}

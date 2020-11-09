using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IptVisionLucam
{
    public partial class FormTableView : Form
    {
        public FormTableView(ref DataSet ds)
        {
            InitializeComponent();
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView2.DataSource = ds.Tables[1];
            dataGridView3.DataSource = ds.Tables[2];
        }
    }
}

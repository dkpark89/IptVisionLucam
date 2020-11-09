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
    public partial class FormCamsetup : Form
    {
        private float gain;
        private float exposure;
        public float Gain
        {
            set { gain = value; }
            get { return gain; }
        }
        public float Exposure
        {
            set { exposure = value; }
            get { return exposure; }
        }
        public FormCamsetup()
        {
            InitializeComponent();
        }

        private void FormCamsetup_Load(object sender, EventArgs e)
        {
            textBoxExposure.Text = exposure.ToString();
            textBoxGain.Text = gain.ToString();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            try
            {
                exposure = float.Parse(textBoxExposure.Text);
            }
            catch
            {
                MessageBox.Show("노출시간 값이 잘못되었습니다.");
                return;
            }
            try
            {
                gain = float.Parse(textBoxGain.Text);
            }
            catch
            {
                MessageBox.Show("GAIN 값이 잘못되었습니다.");
                return;
            }
            Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IptVision
{
    public partial class FormPassword : Form
    {
        private string password = "";
        private string userString = "";
        private string returnPassword = "";
        public string ReturnPassword
        {
            get { return returnPassword; }
        }
        //private DataSet ds = null;
        private bool bPass = false;
        public bool Pass
        {
            get { return bPass; }
        }
        public FormPassword(string password)
        {
            this.password = password;
            InitializeComponent();
        }
        private void FormPassword_Load(object sender, EventArgs e)
        {
            bPass = false;
            if (password.Length == 0)
            {
                bPass = true;
                Close();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            userString += "1";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
                Close();
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            userString += "2";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            userString += "3";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
                Close();
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            userString += "4";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            userString += "5";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
                Close();
            }
        }
        private void button6_Click(object sender, EventArgs e)
        {
            userString += "6";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
                Close();
            }
        }
        private void button7_Click(object sender, EventArgs e)
        {
            userString += "7";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
                Close();
            }
        }
        private void button8_Click(object sender, EventArgs e)
        {
            userString += "8";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
                Close();
            }
        }
        private void button9_Click(object sender, EventArgs e)
        {
            userString += "9";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
            }
        }
        private void button0_Click(object sender, EventArgs e)
        {
            userString += "0";
            textBoxLcd.Text = userString;
            if (password.Equals(userString))
            {
                bPass = true;
                Close();
            }
        }
        private void buttonClr_Click(object sender, EventArgs e)
        {
            userString = "";
            textBoxLcd.Text = userString;
            Close();
        }

        private void FormPassword_KeyDown(object sender, KeyEventArgs e)
        {
            bPass = false;
            switch (e.KeyCode)
            {
                case Keys.D0:
                    button0_Click(null, null);
                    break;
                case Keys.D1:
                    button1_Click(null, null);
                    break;
                case Keys.D2:
                    button2_Click(null, null);
                    break;
                case Keys.D3:
                    button3_Click(null, null);
                    break;
                case Keys.D4:
                    button4_Click(null, null);
                    break;
                case Keys.D5:
                    button5_Click(null, null);
                    break;
                case Keys.D6:
                    button6_Click(null, null);
                    break;
                case Keys.D7:
                    button7_Click(null, null);
                    break;
                case Keys.D8:
                    button8_Click(null, null);
                    break;
                case Keys.D9:
                    button9_Click(null, null);
                    break;
                case Keys.Escape:
                    buttonClr_Click(null, null);
                    break;
            }
        }
    }
}

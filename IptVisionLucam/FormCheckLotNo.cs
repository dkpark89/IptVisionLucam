using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Ipt
{
    public partial class FormCheckLotNo : Form
    {
        private bool bOk = false;
        public bool OK
        {
            get { return bOk; }
        }
        private string psCd = "";
        public string PsCd
        {
            set { psCd = value; }
            get { return psCd; }
        }
        private string strConnectERP = "";
        public string StringConnectERP
        {
            set { strConnectERP = value; }
        }
        private string strConnectEES = "";
        public string StringConnectEES
        {
            set { strConnectEES = value; }
        }
        private string lotNumber = "";
        public string LotNumber
        {
            set { lotNumber = value; }
        }
        public FormCheckLotNo()
        {
            InitializeComponent();
        }

        private void FormCheckLotNo_Load(object sender, EventArgs e)
        {
            labelLotNumber.Text = "로트넘버 : " + lotNumber;
            textBoxPsCd.Text = psCd;
        }
        private void buttonGet_Click(object sender, EventArgs e)
        {
            psCd = textBoxPsCd.Text;
            string psNm = GetPsNm(psCd);
            labelName.Text = "(" + psNm + ")";
            buttonOK.Enabled = (psNm.Length > 0);
            buttonCancel.Enabled = true;
            buttonGet.BackColor = buttonOK.Enabled ? Color.Green : Color.Red;
        }
        private string GetPsNm(string psCd)
        {
            string ret = "";
            try
            {
#if TEST
                ret = "TEST";
                labelResult.Text = "TEST";
#else
                string strQuery = "select a.emp_nm from VIEW_ERP_TB_HRA100 a where a.emp_no='" + psCd + "'";
                Class_ERPDB db = new Class_ERPDB();
                db.ConnectDB(strConnectEES);
                DataTable dt = db.GetDBtable(strQuery);
                db.CloseDB();
                if (dt.Rows.Count > 0)
                {
                    ret = dt.Rows[0][0].ToString();
                    labelResult.Text = "";
                }
                else
                {
                    labelResult.Text = "없음";
                }
#endif

            }
            catch(Exception ex)
            {
                labelResult.Text = ex.Message;
            }
            return ret;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            bOk = true;
            Close();
        }
    }
}

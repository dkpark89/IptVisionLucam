using Ipt;
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
    public partial class FormCheckLotNo : Form
    {
        private bool bOk = false;
        public bool OK
        {
            get { return bOk; }
        }
        private int group = 0;
        public int Group
        {
            set { group = value; }
            get { return comboBoxGroup.SelectedIndex; }
        }
        private string psCd = "";
        public string PsCd
        {
            set { psCd = value; }
            get { return psCd; }
        }
        private string lotNumber = "";
        public string LotNumber
        {
            set { lotNumber = value; }
        }
        private string mcCd = "error";
        public string McCd
        {
            set { mcCd = value; }
        }
        private string heater = "0";
        public string Heater
        {
            set { heater = value; }
        }
        public FormCheckLotNo()
        {
            InitializeComponent();
        }

        private void FormCheckLotNo_Load(object sender, EventArgs e)
        {
            labelLotNumber.Text = "로트넘버 : " + lotNumber;
            textBoxPsCd.Text = psCd;
            comboBoxGroup.SelectedIndex = group;
            textBoxHeater.Text = heater;
        }
        private void OnRecieveMessage(object sender, SendMessageArgs e)
        {
            string lotnumber = "";
            string gdNm = "";
            string psNm = "";
            string spec = "";
            string job_qty = "";
            string[] cmd_seperators = new string[1] { "||" };
            string[] q_seperators = new string[1] { "=" };
            string[] seperators = new string[1] { "|" };
            string time = "";
            try
            {
                foreach (string querys in e.Msg.Split(seperators, StringSplitOptions.None))
                {
                    string[] query = querys.Split(q_seperators, StringSplitOptions.None);
                    switch (query[0])
                    {
                        case "lotnumber":
                            lotnumber = query[1];
                            break;
                        case "gd_nm":
                            gdNm = query[1];
                            break;
                        case "spec":
                            spec = query[1];
                            break;
                        case "job_qty":
                            job_qty = query[1];
                            break;
                        case "ps_nm":
                            psNm = query[1];
                            break;
                        case "time":
                            time = query[1];
                            break;
                    }
                }
                labelName.Text = "(" + psNm + ")";
                if (psNm.Length > 0 && lotNumber.Length > 0)
                {
                    buttonGet.BackColor = Color.Transparent;
                    buttonOK.Enabled = true;
                }
                heater = textBoxHeater.Text;
                if (time.Length > 0)
                {
                    if (lotnumber.Length > 0)
                    {
                        labelResult.Text = "작업호기 : " + mcCd + "\n\n히터온도 : " + heater + "\n\n로트넘버 : " + lotnumber + "\n\n제품명 : " + gdNm + "\n\nSPEC : " + spec + "\n\n지시수량 : " + job_qty + "\n\n작업조 : " + comboBoxGroup.Text + "\n\n작업자 : " + psNm + "\n\n시작 시각 : " + time;
                    }
                    else
                    {
                        labelResult.Text = "작업지시가 없습니다.";
                    }
                }
                else
                {
                    labelResult.Text = e.Msg;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void buttonGet_Click(object sender, EventArgs e)
        {
            psCd = textBoxPsCd.Text;
            TCPClient client = new TCPClient();
            client.SendMessage += new TCPClient.SendMessageNotify(OnRecieveMessage);
            client.Send("?get_data||lotnumber=" + lotNumber + "|ps_cd=" + psCd + "||lotnumber|gd_nm|spec|job_qty|ps_nm|time");
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            // TODO 여기에 최종적으로 서버에 확인 한번 더.
            TCPClient client = new TCPClient();
            client.SendMessage += new TCPClient.SendMessageNotify(OnRecieveMessage);
            client.Send("?set_start||lotnumber=" + lotNumber + "|ps_cd=" + psCd + "|heater=" + heater + "|mc_cd=" + mcCd + "|group=" + group.ToString());
            bOk = true;
            Close();
        }

        private void comboBoxGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            group = comboBoxGroup.SelectedIndex;
        }
    }
}

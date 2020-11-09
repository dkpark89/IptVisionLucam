using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace IptVisionLucam
{
    public partial class UserControlStatus : UserControl
    {
        private Pen redPen = new Pen(Color.Red, 2.0f);
        private Pen bluePen = new Pen(Color.Blue, 2.0f);
        private Brush redBrush = new SolidBrush(Color.Pink);
        private Brush blueBrush = new SolidBrush(Color.SkyBlue);
        private Brush blackBrush = new SolidBrush(Color.Black);
        private Pen blackPen = new Pen(Color.Black, 1.0f);

        enum RESULT{ NONE = 0, OK, NG };
        //class tagData
        //{
        //    tagData()
        //    {
        //        CenterState = RESULT.NONE;
        //        BoundaryState = RESULT.NONE;
        //        okIndex = -1;
        //        palIndex = 0;
        //        LenzId = -1;
        //        StageIndex = 0;
        //    }
        //    public RESULT CenterState, BoundaryState;
        //    public int okIndex;
        //    public int palIndex;
        //    public int LenzId;
        //    public int StageIndex;
        //}
        //tagData[] m_data = new tagData[8];
        //int m_okIndex;
        //int m_palIndex;
        bool m_bPalReset;

        public UserControlStatus()
        {
            InitializeComponent();
            Init();
        }
        DataRowCollection m_data;
        DataRow drIndex;
        public int OkIndex
        {
            get { return (int)drIndex["m_okIndex"]; }
        }
        public int PalIndex
        {
            get { return (int)drIndex["m_palIndex"]; }
        }
        public void Init()
        {
            dataSetData.Clear();
            for (int i = 0; i < 8; i++)
            {
                DataRow dr = dataSetData.Tables["m_data"].NewRow();
                dr["okIndex"] = -1;
                dr["BoundaryState"] = RESULT.NONE;
                dr["CenterState"] = RESULT.NONE;
                dataSetData.Tables["m_data"].Rows.Add(dr);
            }
            {
                DataRow dr = dataSetData.Tables["variable"].NewRow();
                dr["m_okIndex"] = 0;
                dr["m_palIndex"] = 1;
                dataSetData.Tables["variable"].Rows.Add(dr);
            }
            m_data = dataSetData.Tables["m_data"].Rows;
            drIndex = dataSetData.Tables["variable"].Rows[0];
            m_bPalReset = false;
            this.InvokeIfNeeded(() => Invalidate());
        }
        void DrawLenz(Graphics G, int x, int y, RESULT eCenter, RESULT eBoundary, int okIndex)
        {
            string strOkIndex = okIndex.ToString();
            switch (eCenter)
            {
                case RESULT.OK:
                    G.FillEllipse(blueBrush, x - 8, y - 8, 16, 16);
                    break;
                case RESULT.NG:
                    G.FillEllipse(redBrush, x - 8, y - 8, 16, 16);
                    break;
            }
            switch (eBoundary)
            {
                case RESULT.OK:
                    G.DrawEllipse(bluePen, x - 10, y - 10, 20, 20);
                    break;
                case RESULT.NG:
                    G.DrawEllipse(redPen, x - 10, y - 10, 20, 20);
                    break;
            }
            if (eCenter == RESULT.OK && eBoundary == RESULT.OK)
                G.DrawString(strOkIndex, arial, blackBrush, (x - 8), (y - 5));
        }
        private Font arial = new Font("Arial", 8, System.Drawing.FontStyle.Regular);
        private void UserControlStatus_Paint(object sender, PaintEventArgs e)
        {
            Graphics G = this.CreateGraphics();
            G.Clear(Color.LightYellow);
            DrawLenz(G, 180, 180, (RESULT)m_data[0]["CenterState"], (RESULT)m_data[0]["BoundaryState"], (int)m_data[0]["okIndex"]);   //0 CAM1
            DrawLenz(G, 152, 186, (RESULT)m_data[1]["CenterState"], (RESULT)m_data[1]["BoundaryState"], (int)m_data[1]["okIndex"]);   //1
            DrawLenz(G, 122, 176, (RESULT)m_data[2]["CenterState"], (RESULT)m_data[2]["BoundaryState"], (int)m_data[2]["okIndex"]);   //2 CAM2
            DrawLenz(G, 99, 155, (RESULT)m_data[3]["CenterState"], (RESULT)m_data[3]["BoundaryState"], (int)m_data[3]["okIndex"]);        //3
            if ((RESULT)m_data[4]["CenterState"] != RESULT.OK || (RESULT)m_data[4]["BoundaryState"] != RESULT.OK)
                DrawLenz(G, 88, 123, (RESULT)m_data[4]["CenterState"], (RESULT)m_data[4]["BoundaryState"], (int)m_data[4]["okIndex"]);        //4 SEL
            if ((RESULT)m_data[5]["CenterState"] != RESULT.OK || (RESULT)m_data[5]["BoundaryState"] != RESULT.OK)
                DrawLenz(G, 96, 92, (RESULT)m_data[5]["CenterState"], (RESULT)m_data[5]["BoundaryState"], (int)m_data[5]["okIndex"]);     //5 DEL
            if ((RESULT)m_data[6]["CenterState"] != RESULT.OK || (RESULT)m_data[6]["BoundaryState"] != RESULT.OK)
                DrawLenz(G, 117, 69, (RESULT)m_data[6]["CenterState"], (RESULT)m_data[6]["BoundaryState"], (int)m_data[6]["okIndex"]);        //6 DEL2
            if ((RESULT)m_data[7]["CenterState"] == RESULT.OK && (RESULT)m_data[7]["BoundaryState"] == RESULT.OK)
                DrawLenz(G, 43, 123, (RESULT)m_data[7]["CenterState"], (RESULT)m_data[7]["BoundaryState"], (int)m_data[7]["okIndex"]);        //7 PAL
            G.DrawRectangle(blackPen, new Rectangle(27, 107, 82, 31));
            G.DrawString("CAM1", arial, blackBrush, 160, 196);
            G.DrawString("CAM2", arial, blackBrush, 100, 190);
            if (m_bPalReset)
            {
                G.DrawString("팔레트 이송", new Font("Arial", 1f), redBrush, 20, 220);
                m_bPalReset = false;
            }
        }
        public void CheckResult(out bool pbCenter, out bool pbBoundary)
        {
            pbCenter = ((RESULT)m_data[2]["CenterState"] == RESULT.OK);
            pbBoundary = ((RESULT)m_data[2]["BoundaryState"] == RESULT.OK);
        }
        public void SetIndex(int OkIndex, int PalIndex)
        {
            m_data[7]["okIndex"] = OkIndex;
            m_data[7]["palIndex"] = PalIndex;
            for (int i = 3; i >= 0; i--)
            {
                if ((RESULT)m_data[i]["BoundaryState"] == RESULT.OK && (RESULT)m_data[i]["CenterState"] == RESULT.OK)
                {
                    OkIndex++;
                    if (OkIndex > 50)
                    {
                        OkIndex = 1;
                        PalIndex++;
                    }
                    m_data[i]["okIndex"] = OkIndex;
                    m_data[i]["palIndex"] = PalIndex;
                }
            }
            drIndex["m_okIndex"] = OkIndex;
            drIndex["m_palIndex"] = PalIndex;
            this.InvokeIfNeeded(() => Invalidate());
        }
        public bool SetCamResult(bool bCenter, int CenterStageIndex, bool bBoundary, int LenzId, out int pOkIndex, out int pPalIndex, ref int pIndex, ref int pStageId)
        {
            bool ret = false;
            m_data.RemoveAt(6);
            m_data.InsertAt(dataSetData.Tables["m_data"].NewRow(), 0);
            m_data[0]["LenzId"] = LenzId;
            m_data[0]["CenterState"] = bCenter ? RESULT.OK : RESULT.NG;
            m_data[0]["StageIndex"] = CenterStageIndex;
            m_data[2]["BoundaryState"] = bBoundary ? RESULT.OK : RESULT.NG;
            if ((RESULT)m_data[2]["CenterState"] == RESULT.OK && (RESULT)m_data[2]["BoundaryState"] == RESULT.OK)
            {
                int okIndex = (int)drIndex["m_okIndex"] + 1;
                drIndex["m_okIndex"] = okIndex;
                if (okIndex > 50)
                {
                    drIndex["m_okIndex"] = 1;
                    int palIndex = (int)drIndex["m_palIndex"] + 1;
                    drIndex["m_palIndex"] = palIndex;
                }
                m_data[2]["okIndex"] = drIndex["m_okIndex"];
                m_data[2]["palIndex"] = drIndex["m_palIndex"];
            }
            if ((RESULT)m_data[4]["BoundaryState"] == RESULT.OK && (RESULT)m_data[4]["CenterState"] == RESULT.OK)
            {
                m_data[7]["okIndex"] = m_data[4]["okIndex"];
                m_data[7]["palIndex"] = m_data[4]["palIndex"];
                m_data[7]["LenzId"] = m_data[4]["LenzId"];
                m_data[7]["StageIndex"] = m_data[4]["StageIndex"];
                m_data[7]["BoundaryState"] = m_data[4]["BoundaryState"];
                m_data[7]["CenterState"] = m_data[4]["CenterState"];
                pIndex = (int)m_data[7]["LenzId"];
                pStageId = (int)m_data[7]["StageIndex"];
                ret = true;
            }
            pOkIndex = (int)m_data[7]["okIndex"];
            pPalIndex = (int)m_data[7]["palIndex"];
            this.InvokeIfNeeded(() => Invalidate());
            return ret;
        }

        public void RcvPalResetSignal()
        {
            m_bPalReset = true;
            this.InvokeIfNeeded(() => Invalidate());
        }

        public void SaveData(string filename)
        {
            dataSetData.WriteXml(filename, XmlWriteMode.WriteSchema);
            //for (int i = 0; i < 8; i++)
            //{
            //    fwrite(&m_data[i], sizeof(TAG_DATA), 1, pf);
            //}
            //fwrite(&m_okIndex, sizeof(int), 1, pf);
            //fwrite(&m_palIndex, sizeof(int), 1, pf);
        }

        public void LoadData(string filename)
        {
            dataSetData.Clear();
            try
            {
                dataSetData.ReadXml(filename, XmlReadMode.Auto);
                m_data = dataSetData.Tables["m_data"].Rows;
                drIndex = dataSetData.Tables["variable"].Rows[0];
            }
            catch
            {
                Init();
            }
            //for (int i = 0; i < 8; i++)
            //{
            //    fread(&m_data[i], sizeof(TAG_DATA), 1, pf);
            //}
            //fread(&m_okIndex, sizeof(int), 1, pf);
            //fread(&m_palIndex, sizeof(int), 1, pf);
        }
    }
}

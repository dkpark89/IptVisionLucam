using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Ipt;
using IptVision;
using LUCAMAPICOMLib;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace IptVisionLucam
{
    public partial class FormMain : Form
    {
        #region "importDll"
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(Int32 vKey);
        #endregion "importDll"

        #region "variables"
        enum DISPLAY_FLAG { INIT = 0, SIL, DK, DEFEACT, BUBBLE, NONE };
        private FormPreview openedPreview = null;
        private ImageUtil iu = null;
        private bool bDebug = false;
        private bool bNoCamera = false;
        private bool bClosing = false;
        private string m_settingDir = "";
        private string m_resultDir = "";
        private string m_tempDir = "";
        private string m_uploadDir = "";
        private string m_errorDir = "";
        //private int cntAll = 0;
        //private int cntOk = 0;
        //private int cntNg = 0;
        //private int cntNoLenz1 = 0;
        //private int cntNoLenz2 = 0;
        //private int cntOK1 = 0;
        //private int cntOK2 = 0;
        //private int cntBubble = 0;
        //private int cntDefeat1 = 0;
        //private int cntDefeat2 = 0;
        //private int cntPaint = 0;
        //private int cntEdge = 0;
        //private int cntSil = 0;
        //private int cntDK = 0;
        //private int cntTop = 0;
        private int[] curNG = new int[2] { Constants.NG_NO_LENZ, Constants.NG_NO_LENZ };
        private int[] prevNG = new int[2] { Constants.NG_NO_LENZ, Constants.NG_NO_LENZ };
        //private int cntDisplayNG_Bubble = 0;
        //private int cntDisplayNG_Defeat = 0;
        //private int cntDisplayNG_DK = 0;
        //private int cntDisplayNG_Sil = 0;
        //private int cntDisplayNG_None = 0;

        //private int cntTotalUpload = 0;
        private int selectViewResult = 0;
        private int selectViewResultIndex = 0;
        private int[] cnt = new int[2] { 0, 0 };
        private long m_startTick = 0;
        private IplImage[] cvBufferCam = new IplImage[2] { null, null };
        private ClassCenter[] centerTest = null;
        private ClassBoundary[] boundaryTest = null;
        private enum eResultStatus { READY = 0, OK, NG, FA };
        private static eResultStatus[] eStatusCam = new eResultStatus[2] { eResultStatus.READY, eResultStatus.READY };
        private static eResultStatus[] eStatusPrevMaster = new eResultStatus[2] { eResultStatus.READY, eResultStatus.READY };
        private bool bFinishSignal = false;
        private bool bUpload = false;
        private object lockObject = new object();
        private string previousLotNumber = "";
        private int m_okIndex = 0;
        private int m_palIndex = 0;
        private bool[] bPreview = new bool[2] { false, false };
        //        private int cntPreview = 0;
        private CvFont font = new CvFont(FontFace.Vector0, 0.5, 0.5);
        private CvColor blackColor = new CvColor(0, 0, 0);
        private CvColor whiteColor = new CvColor(255, 255, 255);
        private bool bForceExit = false;
        private bool Connected;
        private LUCAMAPICOMLib.lucamCOMClass LucamCamera = null;
        private LUCAMAPICOMLib.LUCAM_FRAME_FORMAT_COM[] FrameFormat;
        private LUCAMAPICOMLib.LUCAM_SNAPSHOT_COM[] LucamSnapshot = new LUCAM_SNAPSHOT_COM[2];
        private int[] hCamera = new int[2];
        private float[] FrameRate = new float[2];
        private float[] Exposure = new float[2];
        private float[] Gain = new float[2];
        private float[] Brightness = new float[2];
        private float[] Contrast = new float[2];
        private float[] Gamma = new float[2];
        private int[] iSnapshotCallbackId = new int[2] { -1, -1 };
        private long snapshotLum;
        public long SnapshotLum
        {
            set { snapshotLum = value; }
        }
        private IntPtr[] FrameBuffer = new IntPtr[2] { IntPtr.Zero, IntPtr.Zero };

        bool bAdvIO = false;
        bool bResizing = false;
        private Automation.BDaq.InstantDoCtrl instantDoCtrl1;
        private Automation.BDaq.InstantDiCtrl instantDiCtrl1;
        DataRowCollection drcRoi = null;
        DataRow drSystem = null;
        DataRow drCounter = null;
        private string CurrentFileName = "";
        private bool bJobChange = false;
        private bool bWorkDone = false;
        bool bUseNetwork = false;
        bool bInFinish = false;
        #endregion "variables"



        const string strConnectionERP = @"Data Source=192.168.2.5;Initial Catalog=ERP_2;Persist Security Info=True;User ID=sa;Password=inter07@";
        //const string strConnectionEES = @"Data Source=192.168.2.203;Initial Catalog=InterojoSmartEES;Persist Security Info=True;User ID=sa;Password=fntps!250";
        const string strConnectionEES = @"Data Source=192.168.2.203;Initial Catalog=InterojoSmartEES;Persist Security Info=True;User ID=micube;Password=EES#%DB!@";
        public UdpClient[] clientUDP = new UdpClient[2] { null, null };
        private IPEndPoint[] plcEndPoint = new IPEndPoint[2] { null, null };
        private Thread[] threadQuery = new Thread[2] { null, null };
        bool[] bConnection = new bool[2] { true, true };
        //ConcurrentQueue<byte[]>[] testQ = new ConcurrentQueue<byte[]>[2] { new ConcurrentQueue<byte[]>(), new ConcurrentQueue<byte[]>() };
        private void connections()
        {
            int plcPortnumber = (int)drSystem["plcPortnumber"];
            labelPlc.InvokeIfNeeded(() => labelPlc.BackColor = Color.Red);
            for (int ch = 0; ch < clientUDP.Length; ch++)
            {
                try
                {
                    plcEndPoint[ch] = new IPEndPoint(IPAddress.Parse(drSystem["plcAddress1"].ToString()), plcPortnumber + ch);
                    clientUDP[ch] = new UdpClient(plcPortnumber + ch);
                    clientUDP[ch].Client.SendTimeout = 1000;
                    clientUDP[ch].Client.ReceiveTimeout = 1000;
                    //threadQuery[ch] = new Thread(new ParameterizedThreadStart(ThreadPlcReadQuery));
                    //threadQuery[ch].Start(ch);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            labelPlc.InvokeIfNeeded(() => labelPlc.BackColor = Color.Green);
            //SendCheckSheetNo();
        }
        private void closeConnection()
        {
            for (int ch = 0; ch < clientUDP.Length; ch++)
            {
                try
                {
                    if (threadQuery[ch] != null) threadQuery[ch].Abort();
                }
                catch { }
            }
            Thread.Sleep(300);
            for (int ch = 0; ch < clientUDP.Length; ch++)
            {
                try
                {
                    if (clientUDP[ch] != null) clientUDP[ch].Close();
                }
                catch { }
            }
        }
        private void SendLotStart()
        {
            SendM(1, 1993, true);
        }
        private void SendLotEnd()
        {
            SendM(1, 1994, true);
        }
        private void SendCheckSheetNo()
        {
            SendStringD(1, 4500, drSystem["lotNumber"].ToString());//Check Sheet No.1
        }
        void SendM(int ch, int address, bool value)
        {
            byte[] data = MitubishPLC.BatchWriteBitTypeA1E("M" + address, value);
            try
            {
                byte[] sendData = data;
                //testQ[ch].TryDequeue(out sendData);
                //testQ[ch].TryDequeue(out byte[] sendData);
                try
                {
                    clientUDP[ch].Send(sendData, sendData.Length, plcEndPoint[ch]);
                    byte[] buffer = clientUDP[ch].Receive(ref plcEndPoint[ch]);
                }
                catch (Exception ex)
                {
                    //testQ[ch].Enqueue(sendData);
                    bConnection[ch] = false;
                    if (labelPlc.BackColor != Color.Red)
                    {
                        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(s3)", ex));
                        labelPlc.InvokeIfNeeded(() => labelPlc.BackColor = Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                bConnection[ch] = false;
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", ex));
            }
            SaveSendToPlc("M" + address, value ? 1 : 0);
        }
        void SendStringD(int ch, int address, string value)
        {
            byte[] data = MitubishPLC.WriteStringWord_TypeA1E("D" + address, value, 10);
            try
            {
                byte[] sendData = data;
                //testQ[ch].TryDequeue(out sendData);
                //testQ[ch].TryDequeue(out byte[] sendData);
                try
                {
                    clientUDP[ch].Send(sendData, sendData.Length, plcEndPoint[ch]);
                    byte[] buffer = clientUDP[ch].Receive(ref plcEndPoint[ch]);
                }
                catch (Exception ex)
                {
                    //testQ[ch].Enqueue(sendData);
                    bConnection[ch] = false;
                    if (labelPlc.BackColor != Color.Red)
                    {
                        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(s3)", ex));
                        labelPlc.InvokeIfNeeded(() => labelPlc.BackColor = Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                bConnection[ch] = false;
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", ex));
            }
            SaveSendToPlc("D" + address, value);
        }
        object objSendToPlc = new object();
        private void SaveSendToPlc(string address, int value)
        {
            if (false == (bool)drSystem["bLogSendToPlc"]) return;
            DateTime now = DateTime.Now;
            string lotNumber = textBoxLotNumber.Text;
            string path = m_settingDir + @"\logSendToPlc\log-" + DateTime.Now.ToString("yyyy-MM");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filename = path + @"\sendToPlc-" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";
            bool bExistFile = File.Exists(filename);
            using (FileStream fs = File.Open(filename, FileMode.Append))
            {
                using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                {
                    DataColumnCollection dcc = dataSetFinish.Tables[0].Columns;
                    lock (objSendToPlc)
                    {
                        if (bExistFile == false)
                        {
                            string header = "time,LotNumber,address,data(int), data(bin)";
                            w.WriteLine(header);
                        }
                        {
                            string data = now.ToString("HH:mm:ss.fff") + "," + lotNumber + "," + address + "," + value + ","
                                + "0b" + Convert.ToString(value, 2).PadLeft(16, '0');
                            w.WriteLine(data);
                        }
                    }
                }
            }
        }
        private void SaveSendToPlc(string address, string value)
        {
            if (false == (bool)drSystem["bLogSendToPlc"]) return;
            DateTime now = DateTime.Now;
            string lotNumber = textBoxLotNumber.Text;
            string path = m_settingDir + @"\logSendToPlc\log-" + DateTime.Now.ToString("yyyy-MM");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filename = path + @"\sendToPlc-" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";
            bool bExistFile = File.Exists(filename);
            using (FileStream fs = File.Open(filename, FileMode.Append))
            {
                using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                {
                    DataColumnCollection dcc = dataSetFinish.Tables[0].Columns;
                    lock (objSendToPlc)
                    {
                        if (bExistFile == false)
                        {
                            string header = "time,LotNumber,address,data(int), data(bin)";
                            w.WriteLine(header);
                        }
                        {
                            string data = now.ToString("HH:mm:ss.fff") + "," + lotNumber + "," + address + "," + value + ",is string";
                            w.WriteLine(data);
                        }
                    }
                }
            }
        }

        #region "ThreadCode"
        Queue<Dictionary<string, object>> upLoadQueue = new Queue<Dictionary<string, object>>();
        Thread wCheckResult = null;
        Thread wResultList = null;
        Thread wUpload = null;
        Thread[] wCheckCamera = new Thread[2] { null, null };
        private int checkCameraCenter(ref ClassCenter centerTest, ref IplImage cvImage, ref DataSet ds, ref eResultStatus eStatus)
        {
            try
            {
                int result = -1;
                result = centerTest.processing(cvImage, ref ds, checkBoxColor.Checked, true);
                if (bClosing) return 0;
                if (result == 0)
                    eStatus = eResultStatus.OK;
                else
                    eStatus = eResultStatus.NG;
                return result;
            }
            catch (ThreadInterruptedException)
            {
                eStatus = eResultStatus.FA;
                return Constants.NG_NO_LENZ;
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
                eStatus = eResultStatus.FA;
                return Constants.NG_NO_LENZ;
            }
        }
        private int checkCameraBoundary(ref ClassBoundary boundaryTest, IplImage cvImage, ref DataSet ds, ref eResultStatus eStatus)
        {
            try
            {
                int result = -1;
                result = boundaryTest.processing(cvImage, ref ds, checkBoxBanung.Checked, checkBoxColor.Checked);
                if (bClosing) return 0;
                if (result == 0)
                    eStatus = eResultStatus.OK;
                else
                    eStatus = eResultStatus.NG;
                return result;
            }
            catch (ThreadInterruptedException)
            {
                eStatus = eResultStatus.FA;
                return Constants.NG_NO_LENZ;
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
                eStatus = eResultStatus.FA;
                return Constants.NG_NO_LENZ;
            }
        }
        private void memToIplImage(IntPtr src, IplImage des)
        {
            unsafe
            {
                byte* ch1 = des.ImageDataPtr;
                byte* pData = (byte*)src;
                for (int j = 0; j < des.Height; j++)
                {
                    for (int i = 0; i < des.Width; i++)
                    {
                        int index = i + j * des.Width;
                        ch1[index] = (byte)pData[index];
                    }
                }
            }
        }
        public void wtCheckCamera1()
        {
            try
            {
                int id = 0;
                try
                {
                    if (!bDemo)
                    {
                        if (hCamera[id] > 0)
                        {
                            if (cvBufferCam[id] != null) cvBufferCam[id].Dispose();
                            if (LucamCamera.TakeFastFrame(hCamera[id], FrameBuffer[id].ToInt32()))
                            {
                                cvBufferCam[id] = new IplImage(LucamSnapshot[id].format.width, LucamSnapshot[id].format.height, BitDepth.U8, 1);
                                memToIplImage(FrameBuffer[id], cvBufferCam[id]);
                            }
                            else
                            {
                                cvBufferCam[id].Zero();
                            }
                        }
                        else
                        {
                            if (cvBufferCam[id] != null) cvBufferCam[id].Zero();
                        }
                    }
                }
                catch
                {
                    if (cvBufferCam[id] != null) cvBufferCam[id].Zero();
                }
                if (bResizing)
                {
                    IplImage tmp = new IplImage(1600, 1200, BitDepth.U8, 1);
                    cvBufferCam[id].Resize(tmp, Interpolation.Area);
                    cvBufferCam[id] = tmp;
                }
                if (buttonTestCapture.Enabled == false)
                {
                    eStatusCam[id] = eResultStatus.NG;
                    pictureBoxCam1.InvokeIfNeeded(() => pictureBoxCam1.Image = iu.resizeImage(cvBufferCam[id], pictureBoxCam1.Size));
                    labelCount1.InvokeIfNeeded(() => labelCount1.Text = "<OK>");
                }
                else
                {
                    curNG[id] = checkCameraCenter(ref centerTest[id], ref cvBufferCam[id], ref dataSetBackup, ref eStatusCam[id]);
                    if (!bDemo)
                    {
                        cnt[id]++;
                        lock (lockObject)
                        {
                            if (curNG[id] == 0)
                            {
                                drCounter["OK1"] = (int)drCounter["OK1"] + 1;
                            }
                            else if (curNG[id] == Constants.NG_NO_LENZ)
                            {
                                drCounter["NoLenz1"] = (int)drCounter["NoLenz1"] + 1;
                            }
                            else
                            {
                                if ((curNG[id] & Constants.NG_BUBBLE) != 0) drCounter["Bubble"] = (int)drCounter["Bubble"] + 1;
                                if ((curNG[id] & Constants.NG_DEFEAT) != 0) drCounter["Defeat1"] = (int)drCounter["Defeat1"] + 1;
                                if ((curNG[id] & Constants.NG_PAINT) != 0) drCounter["Paint"] = (int)drCounter["Paint"] + 1;
                                if ((curNG[id] & Constants.NG_EDGE) != 0) drCounter["Edge"] = (int)drCounter["Edge"] + 1;
                                if ((curNG[id] & Constants.NG_CT) != 0) drCounter["Ct"] = (int)drCounter["Ct"] + 1;
                            }
                        }
                    }
                    else
                    {
                        string filename = m_tempDir + @"\" + Path.GetFileNameWithoutExtension(demoFilename1) + ".demo";

                        try
                        {
                            centerTest[id].ResultBitmap.Save(filename);
                        }
                        catch
                        {
                            IplImage tmp = cvBufferCam[id].EmptyClone();
                            tmp.SaveImage(filename);
                        }
                    }
                    //labelSen1.InvokeIfNeeded(() => labelSen1.Text = "자외선차이=" + centerTest[id].UvDiff1.ToString() + "/" + centerTest[id].UvDiff2.ToString() + " [" + centerTest[id].C1 + "/" + centerTest[id].C2 + "/" + centerTest[id].C3 + "]");
                    string str = "(" + StageIndex + "/12)";
                    labelSen1.InvokeIfNeeded(() => labelSen1.Text = str + " [" + centerTest[id].C1 + "/" + centerTest[id].C2 + "/" + centerTest[id].C3 + "]");
                    PutText(ref cvBufferCam[id], "M#=" + dataSetBackup.Tables["fixedBackup"].Rows[0]["machineCode"].ToString() + ", id=" + (id + 1).ToString() + ", cnt=" + drCounter["count"].ToString() + ", status=" + eStatusCam[id].ToString(), true, 30, 30);
                    DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
                    PutText(ref cvBufferCam[id], "diffTh=" + dr["diffTh"].ToString() + "/colorIn=" + dr["colorInner"].ToString() + "/colorOut" + dr["colorOuter"].ToString(), false, 30, 60);
                    PutText(ref cvBufferCam[id], dr["colorUnderprint"].ToString() + "/" + dr["lumDiffTh"].ToString() + "/" + dr["devTh"].ToString(), false, 30, 90);
                    PutText(ref cvBufferCam[id], dr["s1Start"].ToString() + "/" + dr["s1Count"].ToString() + "/" + dr["s2Start"].ToString() + "/" + dr["s2Count"].ToString() + "/" + dr["s3Start"].ToString() + "/" + dr["s3Count"].ToString(), false, 30, 120);
                    PutText(ref cvBufferCam[id], "UV=" + centerTest[id].UvDiff1.ToString() + "/" + centerTest[id].UvDiff2.ToString(), false, 30, 150);
                    PutText(ref cvBufferCam[id], centerTest[id].C1.ToString() + "/" + centerTest[id].C2.ToString() + "/" + centerTest[id].C3.ToString(), false, 30, 180);
                    PutText(ref cvBufferCam[id], Properties.Resources.VERSION, false, 30, 210);
                    if (!bDemo)
                    {
                        cvBufferCam[id].SaveImage(m_tempDir + @"\Org1-" + ((int)drCounter["count"]).ToString("000000") + ".jpg");
                        try
                        {
                            centerTest[id].ResultBitmap.Save(m_tempDir + @"\Res1-" + ((int)drCounter["count"]).ToString("000000") + ".jpg");
                        }
                        catch (InvalidOperationException)
                        {
                            Thread.Sleep(100);
                            centerTest[id].ResultBitmap.Save(m_tempDir + @"\Res1-" + ((int)drCounter["count"]).ToString("000000") + ".jpg");
                        }
                        pictureBoxCam1.InvokeIfNeeded(() => pictureBoxCam1.Image = iu.resizeImage(cvBufferCam[id], pictureBoxCam1.Size));
                        pictureBoxResult1.Tag = (int)drCounter["count"];
                    }
                    else
                    {
                        string filename1 = m_tempDir + @"\" + Path.GetFileNameWithoutExtension(demoFilename1) + ".demo";
                        pictureBoxResult1.Tag = filename1 + "|" + demoFilename1;
                    }
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        string demoFilename1 = "";
        string demoFilename2 = "";
        public void wtCheckCamera2()
        {
            try
            {
                int id = 1;
                try
                {
                    if (!bDemo)
                    {
                        if (hCamera[id] > 0)
                        {
                            if (cvBufferCam[id] != null) cvBufferCam[id].Dispose();
                            if (LucamCamera.TakeFastFrame(hCamera[id], FrameBuffer[id].ToInt32()))
                            {
                                cvBufferCam[id] = new IplImage(LucamSnapshot[id].format.width, LucamSnapshot[id].format.height, BitDepth.U8, 1);
                                memToIplImage(FrameBuffer[id], cvBufferCam[id]);
                            }
                            else
                            {
                                cvBufferCam[id].Zero();
                            }
                        }
                        else
                        {
                            if (cvBufferCam[id] != null) cvBufferCam[id].Zero();
                        }
                    }
                }
                catch
                {
                    if (cvBufferCam[id] != null) cvBufferCam[id].Zero();
                }
                if (bResizing)
                {
                    IplImage tmp = new IplImage(1600, 1200, BitDepth.U8, 1);
                    cvBufferCam[id].Resize(tmp, Interpolation.Area);
                    cvBufferCam[id] = tmp;
                }
                if (buttonTestCapture.Enabled == false)
                {
                    eStatusCam[id] = eResultStatus.NG;
                    pictureBoxCam2.InvokeIfNeeded(() => pictureBoxCam2.Image = iu.resizeImage(cvBufferCam[id], pictureBoxCam2.Size));
                    labelCount2.InvokeIfNeeded(() => labelCount2.Text = "<OK>");
                }
                else
                {
                    if ((int)drCounter["count"] > 2 || bDemo)
                    {
                        curNG[id] = checkCameraBoundary(ref boundaryTest[id - 1], cvBufferCam[id], ref dataSetBackup, ref eStatusCam[id]);
                        if (!bDemo)
                        {
                            cnt[id]++;
                            lock (lockObject)
                            {
                                if (curNG[id] == 0)
                                {
                                    drCounter["OK2"] = (int)drCounter["OK2"] + 1;
                                }
                                else if (curNG[id] == Constants.NG_NO_LENZ)
                                {
                                    drCounter["NoLenz2"] = (int)drCounter["NoLenz2"] + 1;
                                }
                                else
                                {
                                    if ((curNG[id] & Constants.NG_DEFEAT) != 0) drCounter["Defeat2"] = (int)drCounter["Defeat2"] + 1;
                                    if ((curNG[id] & Constants.NG_DK) != 0) drCounter["DK"] = (int)drCounter["DK"] + 1;
                                    if ((curNG[id] & Constants.NG_SIL) != 0) drCounter["Sil"] = (int)drCounter["Sil"] + 1;
                                    if ((curNG[id] & Constants.NG_TOP) != 0) drCounter["TOP"] = (int)drCounter["TOP"] + 1;
                                }
                            }
                        }
                        else
                        {
                            string filename = m_tempDir + @"\" + Path.GetFileNameWithoutExtension(demoFilename2) + ".demo";

                            try
                            {
                                boundaryTest[id - 1].ResultBitmap.Save(filename);
                            }
                            catch
                            {
                                IplImage tmp = cvBufferCam[id].EmptyClone();
                                tmp.SaveImage(filename);
                            }
                        }
                        int index = (StageIndex == 2) ? 12 : (StageIndex == 1) ? 11 : StageIndex - 2;
                        string str = "(" + index + "/12)";
                        //labelSen2.InvokeIfNeeded(() => labelSen2.Text = "[" + boundaryTest[id - 1].C1 + "/" + boundaryTest[id - 1].C2 + "/" + boundaryTest[id - 1].C3 + "][" + boundaryTest[id - 1].RoundDiff.ToString("0") + "]");
                        labelSen2.InvokeIfNeeded(() => labelSen2.Text = str + "[" + boundaryTest[id - 1].C1 + "/" + boundaryTest[id - 1].C2 + "/" + boundaryTest[id - 1].C3 + "][" + boundaryTest[id - 1].RoundDiff.ToString("0") + "]");
                        PutText(ref cvBufferCam[id], "M#=" + dataSetBackup.Tables["fixedBackup"].Rows[0]["machineCode"].ToString() + ", id=" + (id + 1).ToString() + ", cnt=" + drCounter["count"].ToString() + ", status=" + eStatusCam[id].ToString(), true, 30, 30);

                        DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
                        PutText(ref cvBufferCam[id], dr["cannyParameter1"].ToString() + "/" + dr["cannyParameter2"].ToString() + "/" + dr["diffTh"].ToString(), false, 30, 60);
                        PutText(ref cvBufferCam[id], dr["s1Start"].ToString() + "/" + dr["s1Count"].ToString() + "/" + dr["s2Start"].ToString() + "/" + dr["s2Count"].ToString() + "/" + dr["s3Start"].ToString() + "/" + dr["s3Count"].ToString(), false, 30, 90);
                        PutText(ref cvBufferCam[id], boundaryTest[id - 1].C1.ToString() + "/" + boundaryTest[id - 1].C2.ToString() + "/" + boundaryTest[id - 1].C3.ToString(), false, 30, 120);
                        PutText(ref cvBufferCam[id], Properties.Resources.VERSION, false, 30, 150);
                        if (!bDemo)
                        {
                            cvBufferCam[id].SaveImage(m_tempDir + @"\Org2-" + ((int)drCounter["count"] - 2).ToString("000000") + ".jpg");
                            try
                            {
                                boundaryTest[id - 1].ResultBitmap.Save(m_tempDir + @"\Res2-" + ((int)drCounter["count"] - 2).ToString("000000") + ".jpg");
                            }
                            catch (InvalidOperationException)
                            {
                                Thread.Sleep(100);
                                boundaryTest[id - 1].ResultBitmap.Save(m_tempDir + @"\Res2-" + ((int)drCounter["count"] - 2).ToString("000000") + ".jpg");
                            }
                            pictureBoxResult2.Tag = (int)((int)drCounter["count"] - 2);
                        }
                        else
                        {
                            string filename2 = m_tempDir + @"\" + Path.GetFileNameWithoutExtension(demoFilename2) + ".demo";
                            pictureBoxResult2.Tag = filename2 + "|" + demoFilename2;
                        }
                        //}
                        //    else
                        //    {
                        //        cvBufferCam[id].SaveImage(m_tempDir + @"\DEMO-Org2.jpg");
                        //        try
                        //        {
                        //            boundaryTest[id - 1].ResultBitmap.Save(m_tempDir + @"\DEMO-Res2.jpg");
                        //        }
                        //        catch (InvalidOperationException)
                        //        {
                        //            Thread.Sleep(100);
                        //            boundaryTest[id - 1].ResultBitmap.Save(m_tempDir + @"\DEMO-Res2.jpg");
                        //        }
                        //    }
                    }
                    else
                    {
                        eStatusCam[id] = eResultStatus.NG;
                    }
                    pictureBoxCam2.InvokeIfNeeded(() => pictureBoxCam2.Image = iu.resizeImage(cvBufferCam[id], pictureBoxCam2.Size));
                    //pictureBoxResult2.Tag = (int)(cntAll - 2);
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        string code1 = "";
        public void wtCheckResult()
        {
            double timeValue = 0;
            if (!bDemo)
            {
                try
                {
#if DEBUG
                    int overTime = int.MaxValue;
#else
                    int overTime = 40000000;
#endif
                    if (bNoCamera)
                    {
                        overTime = 1000000000;
                    }
                    long startTick = DateTime.Now.Ticks;
                    for (int i = 0; i < 2; i++)
                    {
                        while (eStatusCam[i].Equals(eResultStatus.READY))
                        {
                            if (bClosing) return;
                            if ((DateTime.Now.Ticks - startTick) > overTime)
                            {
                                try
                                {
                                    if (wCheckCamera[i] != null)
                                    {
                                        wCheckCamera[i].Interrupt();
                                        wCheckCamera[i].Join();
                                    }
                                }
                                catch
                                {
                                }
                                logPrint(MethodBase.GetCurrentMethod().Name + "(1)", "[카메라" + (i + 1).ToString() + " 측정시간 초과]");
                                statusStrip1.InvokeIfNeeded(() => labelMessage.Text += (" [카메라" + (i + 1).ToString() + " 측정시간 초과]"));
                                //cnt[i]++;
                                eStatusCam[i] = eResultStatus.FA;
                                break;
                            }
                            Thread.Sleep(10);
                        }
                        wCheckCamera[i] = null;
                    }
                    timeValue = (DateTime.Now.Ticks - m_startTick) / 10000000.0;
                    if (bClosing) return;
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
            }
            if (buttonTestCapture.Enabled == false)
            {
                buttonTestCapture.InvokeIfNeeded(() => buttonTestCapture.Enabled = true);
                statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[테스트 카메라 완료]");
            }
            else
            {
                if (!bDemo)
                {
                    try
                    {
                        if (checkBoxForce1.Checked)
                        {
                            eStatusCam[0] = eResultStatus.OK;
                        }
                        if (checkBoxForce2.Checked)
                        {
                            eStatusCam[1] = eResultStatus.OK;
                        }
                        code1 = (eStatusCam[0].Equals(eResultStatus.OK) ? "10" : "01")
                            + (eStatusCam[1].Equals(eResultStatus.OK) ? "10" : "01");
                        SafeNativeMethods.SendMessage(mainHandle, (uint)UWM.serialPortPLC, IntPtr.Zero, IntPtr.Zero);
                        //timer500msOneShot.Enabled = true;
                        //bProcessing = false;
                    }
                    catch (Exception ex)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name + "(2)", ex.ToString());
                    }
                    try
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            if (!eStatusCam[i].Equals(eResultStatus.FA))
                            {
                                if (centerTest[i].ResultBitmap != null)
                                {
                                    pictureBoxResult1.InvokeIfNeeded(() => pictureBoxResult1.Image = centerTest[i].ResultBitmapThumb);
                                }
                            }
                            else
                            {
                                pictureBoxResult1.InvokeIfNeeded(() => pictureBoxResult1.Image = null);
                            }
                        }
                        for (int i = 1; i < 2; i++)
                        {
                            if (!eStatusCam[i].Equals(eResultStatus.FA))
                            {
                                if (boundaryTest[i - 1].ResultBitmap != null)
                                {
                                    pictureBoxResult2.InvokeIfNeeded(() => pictureBoxResult2.Image = boundaryTest[i - 1].ResultBitmapThumb);
                                }
                                else
                                {
                                    pictureBoxResult2.InvokeIfNeeded(() => pictureBoxResult2.Image = null);
                                }
                            }
                        }
                        if (checkBoxColor.Checked && checkBoxAutoUV.Checked)
                        {
                            UpdateUvTh();
                        }
                        if (wResultList != null)
                        {
                            wResultList.Interrupt();
                            wResultList.Join();
                        }
                        wResultList = new Thread(new ThreadStart(wtResultList));
                        wResultList.Start();
                    }
                    catch (Exception ex)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name + "(3)", ex.ToString());
                    }
                }
                try
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Label labelResult = null;
                        switch (i)
                        {
                            case 0: labelResult = labelResult1; break;
                            case 1: labelResult = labelResult2; break;
                        }
                        string ngMessage = "";
                        if (i < 1)
                        {
                            if (bDemo && nDemo == 2) continue;
                            if (curNG[i] == 0)
                            {
                                ngMessage += "양품          ";
                            }
                            else if (curNG[i] == Constants.NG_NO_LENZ)
                            {
                                ngMessage += "없음          ";
                            }
                            else
                            {
                                if ((curNG[i] & Constants.NG_BUBBLE) != 0)
                                {
                                    ngMessage += "기포 ";
                                }
                                else
                                {
                                    ngMessage += "     ";
                                }
                                if ((curNG[i] & Constants.NG_DEFEAT) != 0)
                                {
                                    ngMessage += "파손 ";
                                }
                                else
                                {
                                    ngMessage += "     ";
                                }
                                if ((curNG[i] & Constants.NG_PAINT) != 0)
                                {
                                    ngMessage += "인쇄 ";
                                }
                                else
                                {
                                    ngMessage += "    ";
                                }
                                if ((curNG[i] & Constants.NG_EDGE) != 0)
                                {
                                    ngMessage += "에지";
                                }
                                else
                                {
                                    ngMessage += "    ";
                                }
                            }
                        }
                        else
                        {
                            if (bDemo && nDemo == 1) continue;
                            if (curNG[i] == 0)
                            {
                                ngMessage += "양품           ";
                            }
                            else if (curNG[i] == Constants.NG_NO_LENZ)
                            {
                                ngMessage += "없음           ";
                            }
                            else
                            {
                                if ((curNG[i] & Constants.NG_DEFEAT) != 0)
                                {
                                    ngMessage += "파손 ";
                                }
                                else
                                {
                                    ngMessage += "     ";
                                }
                                if ((curNG[i] & Constants.NG_SIL) != 0)
                                {
                                    ngMessage += "실 ";
                                }
                                else
                                {
                                    ngMessage += "   ";
                                }
                                if ((curNG[i] & Constants.NG_DK) != 0)
                                {
                                    ngMessage += "뜯김 ";
                                }
                                else
                                {
                                    ngMessage += "     ";
                                }
                                if ((curNG[i] & Constants.NG_TOP) != 0)
                                {
                                    ngMessage += "탑";
                                }
                                else
                                {
                                    ngMessage += "  ";
                                }
                            }
                        }
                        if (eStatusCam[i].Equals(eResultStatus.OK))
                        {
                            labelResult.InvokeIfNeeded(() => labelResult.Text = ngMessage);
                            labelResult.InvokeIfNeeded(() => labelResult.BackColor = Color.LightBlue);
                        }
                        else
                        {
                            labelResult.InvokeIfNeeded(() => labelResult.Text = ngMessage);
                            labelResult.InvokeIfNeeded(() => labelResult.BackColor = Color.Red);
                        }
                    }
                    buttonCapture.InvokeIfNeeded(() => buttonCapture.Enabled = true);
                    labelTime.InvokeIfNeeded(() => labelTime.Text = timeValue.ToString("0.###") + " sec");
                    labelCount1.InvokeIfNeeded(() => labelCount1.Text = cnt[0].ToString());
                    labelCount2.InvokeIfNeeded(() => labelCount2.Text = cnt[1].ToString());
                    statusStrip1.InvokeIfNeeded(() => labelMessage.Text += " [측정 종료]");
                }
                catch (Exception ex)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name + "(4)", ex.ToString());
                }
            }
            GC.Collect();
        }
        public void wtResultList()
        {
            try
            {
                string strCode = "";
                int cntAllIndex = (int)drCounter["count"];
                try
                {
                    int p1 = 0, p2 = 0, aa, bb;
                    userControlStatus1.SetCamResult(eStatusCam[0].Equals(eResultStatus.OK), 1, eStatusCam[1].Equals(eResultStatus.OK), 1, out aa, out bb, ref p1, ref p2);
                    if (eStatusPrevMaster[1] == eResultStatus.OK)
                    {
                        if (eStatusCam[1] == eResultStatus.OK)
                        {
                            strCode = "OK OK";
                        }
                        else
                        {
                            strCode = "OK NG";
                        }
                    }
                    else
                    {
                        if (eStatusCam[1] == eResultStatus.OK)
                        {
                            strCode = "NG OK";
                        }
                        else
                        {
                            strCode = "NG NG";
                        }
                    }
                    eStatusPrevMaster[1] = eStatusPrevMaster[0];
                    eStatusPrevMaster[0] = eStatusCam[0];
                }
                catch { }
                //                pictureBoxPalette.InputBufferMove();
                try
                {
                    if (cntAllIndex > 2)
                    {
                        int localStageIndex = (StageIndex - 2) > 0 ? StageIndex - 2 : StageIndex + 10;
                        if (localStageIndex < 1) localStageIndex = 1;
                        if (localStageIndex > 12) localStageIndex = 12;

                        DataGridViewRow dgvr = new DataGridViewRow();
                        DataGridViewCell cell;
                        int index = cntAllIndex - 2;
                        if (strCode.Equals("OK OK"))
                        {
                            DataRow dr = dataSetOkNg.Tables["ok"].NewRow();
                            drCounter["cntOk"] = (int)drCounter["cntOk"] + 1;
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = "양품-" + drCounter["cntOk"].ToString();
                            dr[0] = cell.Value;
                            dgvr.Cells.Add(cell);
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = index.ToString();
                            dr[1] = cell.Value;
                            dgvr.Cells.Add(cell);
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = localStageIndex.ToString();
                            dr[2] = cell.Value;
                            dgvr.Cells.Add(cell);
                            StageNgCnt[localStageIndex - 1] = 0;
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = strCode;
                            dr[3] = cell.Value;
                            dgvr.Cells.Add(cell);
                            dataSetOkNg.Tables["ok"].Rows.Add(dr);
                            dataGridViewOK.InvokeIfNeeded(() => dataGridViewOK.Rows.Insert(0, dgvr));
                            //pictureBoxPalette.InsertOk(index);
                            {
                                if (m_palIndex == 0) m_palIndex = 1;
                                m_okIndex++;
                                okProcess(index, m_palIndex, m_okIndex, 1);
                                if (m_okIndex >= 50)
                                {
                                    m_okIndex = 0;
                                    m_palIndex++;
                                }
                            }
                        }
                        else
                        {
                            drCounter["cntNg"] = (int)drCounter["cntNg"] + 1;
                            DISPLAY_FLAG DisplayFlag = DISPLAY_FLAG.INIT;
                            DataRow dr = dataSetOkNg.Tables["ng"].NewRow();
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = "불량-" + drCounter["cntNg"].ToString();
                            dr[0] = cell.Value;
                            dgvr.Cells.Add(cell);
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = index.ToString();
                            dr[1] = cell.Value;
                            dgvr.Cells.Add(cell);
                            cell = new DataGridViewTextBoxCell();
                            StageNgCnt[localStageIndex - 1]++;
                            if (StageNgCnt[localStageIndex - 1] > 1)
                            {
                                cell.Value = localStageIndex.ToString() + "(" + StageNgCnt[localStageIndex - 1] + ")";
                            }
                            else
                            {
                                cell.Value = localStageIndex.ToString();
                            }
                            dr[2] = cell.Value;
                            dgvr.Cells.Add(cell);
                            cell = new DataGridViewTextBoxCell();
                            string ngMessage = "";
                            if (prevNG[1] == 0)
                            {
                                ngMessage += "양품 ";
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if (prevNG[1] == Constants.NG_NO_LENZ)
                            {
                                ngMessage += "없음 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.NONE);
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if ((prevNG[1] & Constants.NG_BUBBLE) != 0)
                            {
                                ngMessage += "기포 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.BUBBLE);
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if ((prevNG[1] & Constants.NG_DEFEAT) != 0)
                            {
                                ngMessage += "파손 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.DEFEACT);
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if ((prevNG[1] & Constants.NG_EDGE) != 0)
                            {
                                ngMessage += "에지 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.DEFEACT);
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if ((prevNG[1] & Constants.NG_PAINT) != 0)
                            {
                                ngMessage += "인쇄";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.DEFEACT);
                            }
                            else
                            {
                                ngMessage += "    ";
                            }
                            ngMessage += "/ ";

                            if (curNG[1] == 0)
                            {
                                ngMessage += "양품 ";
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if (curNG[1] == Constants.NG_NO_LENZ)
                            {
                                ngMessage += "없음 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.NONE);
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if ((curNG[1] & Constants.NG_DEFEAT) != 0)
                            {
                                ngMessage += "파손 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.DEFEACT);
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if ((curNG[1] & Constants.NG_SIL) != 0)
                            {
                                ngMessage += "실 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.SIL);
                            }
                            else
                            {
                                ngMessage += "   ";
                            }
                            if ((curNG[1] & Constants.NG_DK) != 0)
                            {
                                ngMessage += "뜯김 ";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.DK);
                            }
                            else
                            {
                                ngMessage += "     ";
                            }
                            if ((curNG[1] & Constants.NG_TOP) != 0)
                            {
                                ngMessage += "탑";
                                SetDisplayFlag(ref DisplayFlag, DISPLAY_FLAG.DEFEACT);
                            }
                            else
                            {
                                ngMessage += "  ";
                            }
                            cell.Value = ngMessage;
                            dr[3] = cell.Value;
                            dgvr.Cells.Add(cell);
                            dataSetOkNg.Tables["ng"].Rows.Add(dr);
                            dataGridViewNG.InvokeIfNeeded(() => dataGridViewNG.Rows.Insert(0, dgvr));
                            //pictureBoxPalette.InsertNg(index);
                            switch (DisplayFlag)
                            {
                                case DISPLAY_FLAG.NONE:
                                    drCounter["DisplayNG_None"] = (int)drCounter["DisplayNG_None"] + 1;
                                    break;
                                case DISPLAY_FLAG.DEFEACT:
                                    drCounter["DisplayNG_Defeat"] = (int)drCounter["DisplayNG_Defeat"] + 1;
                                    break;
                                case DISPLAY_FLAG.BUBBLE:
                                    drCounter["DisplayNG_Bubble"] = (int)drCounter["DisplayNG_Bubble"] + 1;
                                    break;
                                case DISPLAY_FLAG.DK:
                                    drCounter["DisplayNG_DK"] = (int)drCounter["DisplayNG_DK"] + 1;
                                    break;
                                case DISPLAY_FLAG.SIL:
                                    drCounter["DisplayNG_Sil"] = (int)drCounter["DisplayNG_Sil"] + 1;
                                    break;
                                    //case DISPLAY_FLAG.CT:
                                    //    cntDisplayNG_CT++;
                                    //    ngCode = "CT";
                                    //break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name + "(1)", ex.ToString());
                }
                prevNG[1] = prevNG[0];
                prevNG[0] = curNG[0];
                string resultText = MakeResultText();
                wSendCount = new Thread(new ThreadStart(wtSendNgInfo));
                wSendCount.Start();

                groupBoxResultList.InvokeIfNeeded(() => groupBoxResultList.Text = resultText);
                //pictureBoxPalette.InvokeIfNeeded(() => pictureBoxPalette.Refresh());
                userControlStatus1.SaveData(m_tempDir + @"\temp.xml");
                textBoxPaletteIndex.InvokeIfNeeded(() => textBoxPaletteIndex.Text = m_palIndex.ToString());
                textBoxOkIndex.InvokeIfNeeded(() => textBoxOkIndex.Text = m_okIndex.ToString());
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name + "(2)", ex.ToString());
            }
        }
        private void SetDisplayFlag(ref DISPLAY_FLAG DisplayFlag, DISPLAY_FLAG value)
        {
            if (DisplayFlag < value) DisplayFlag = value;
        }
        private string MakeResultText()
        {
            int divide = ((int)drCounter["count"] == (int)drCounter["NoLenz1"]) ? 1 : (int)drCounter["count"] - (int)drCounter["NoLenz1"];

            string resultText = "[Total:" + ((int)drCounter["count"] - 2).ToString()
                       + ", OK:" + drCounter["cntOk"]
                        + ", NG:" + drCounter["cntNg"]
                        + " // "
                    + "미분리:" + drCounter["DisplayNG_None"]
                    + ", 기포:" + drCounter["DisplayNG_Bubble"]
                    + ", 파손:" + drCounter["DisplayNG_Defeat"]
                    + ", 뜯김:" + drCounter["DisplayNG_DK"]
                    + ", 실:" + drCounter["DisplayNG_Sil"]
                    + ", CT:" + drCounter["DisplayNG_Ct"]
                    + "]\n"
                        + "[A:" + ((int)drCounter["count"] - (int)drCounter["NoLenz1"]).ToString()
                        + ", OK:" + drCounter["OK1"] + "(" + ((int)drCounter["OK1"] * 100 / divide).ToString() + "%)"
                         + ", 파손:" + drCounter["Defeat1"]
                    + ", 기포:" + drCounter["Bubble"]
                    + ", 인쇄:" + drCounter["Paint"]
                    + ", 에지:" + drCounter["Edge"]
                     + ", CT:" + drCounter["Ct"]
                        + "]";
            if ((int)drCounter["count"] > 2)
            {
                int sub2 = (((int)drCounter["count"] - 2) == (int)drCounter["NoLenz2"]) ? 1 : ((int)drCounter["count"] - 2) - (int)drCounter["NoLenz2"];
                resultText += "  [B:" + (((int)drCounter["count"] - 2) - (int)drCounter["NoLenz2"]).ToString()
                    + ", OK:" + drCounter["OK2"].ToString() + "(" + ((int)drCounter["OK2"] * 100 / (sub2)).ToString() + "%)"
                    + ", 파손:" + drCounter["Defeat2"]
                    + ", 실:" + drCounter["Sil"]
                + ", 뜯김:" + drCounter["DK"]
                    + ", 탑:" + drCounter["TOP"]
                + "]";
            }

            return resultText;
        }
        Thread wSendCount = null;
        private void wtSendNgInfo()
        {
#if false
            int mcId = (int)dataSetBackup.Tables["fixedBackup"].Rows[0]["machineCode"];
            string mccd = "B" + mcId.ToString("000");
            int divide = cntAll * 5 - cntNoLenz1;
            if (divide == 0) divide = 1;
            string cntStr = "|TO=" + ((cntAll - 1) * 5).ToString()
                    + "|OK=" + cntOk.ToString()
                    + "|NG=" + cntNg.ToString()
                    + "|NO=" + cntDisplayNG_None.ToString()
                    + "|BU=" + cntDisplayNG_Bubble.ToString()
                    + "|DE=" + cntDisplayNG_Defeat.ToString()
                    + "|DK=" + cntDisplayNG_DK.ToString()
                    + "|SI=" + cntDisplayNG_Sil.ToString();
            TCPClient client = new TCPClient();
            client.SendMessage += new TCPClient.SendMessageNotify(OnRecieveMessage);
            client.Send("?set_ng||mc_cd=" + mccd + cntStr);
#endif
        }
        private void OnRecieveMessage(object sender, SendMessageArgs e)
        {
        }
        public void wtUpload()
        {
            try
            {
                while (true)
                {
                    if (upLoadQueue.Count > 0)
                    {
                        Dictionary<string, object> dicParam = upLoadQueue.Dequeue();
                        int index = (int)dicParam["index"];
                        int stageId = (int)dicParam["stageId"];
                        string desOrgName1 = dicParam["desOrgName1"].ToString();
                        string netOrgName1 = dicParam["netOrgName1"].ToString();
                        string desResName1 = dicParam["desResName1"].ToString();
                        string netResName1 = dicParam["netResName1"].ToString();
                        string desOrgName2 = dicParam["desOrgName2"].ToString();
                        string netOrgName2 = dicParam["netOrgName2"].ToString();
                        string desResName2 = dicParam["desResName2"].ToString();
                        string netResName2 = dicParam["netResName2"].ToString();
                        string desTxtName = dicParam["desTxtName"].ToString();
                        string netTxtName = dicParam["netTxtName"].ToString();
                        while (true)
                        {
                            try
                            {
                                File.Copy(desOrgName1, netOrgName1, true);
                                File.Copy(desResName1, netResName1, true);
                                File.Copy(desOrgName2, netOrgName2, true);
                                File.Copy(desResName2, netResName2, true);
                            }
                            catch
                            {
                                Thread.Sleep(1000);
                                continue;
                            }
                            break;
                        }
                        drCounter["totalUpCount"] = (int)drCounter["totalUpCount"] + 1;
                        printRemainedUpload();
                    }
                    Thread.Sleep(10);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        #endregion "ThreadCode"

        #region "functions"
        private void logPrint(object sender, LogArgs e)
        {
            logPrint(e.Where, ((e.Ex != null) ? e.Ex.ToString() : e.Message));
        }
        private void logPrint(string where, string msg)
        {
            lock (lockObject)
            {
                string path = m_settingDir + @"\log-" + DateTime.Now.ToString("yyyy-MM");
                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                }
                FileStream fs = File.Open(path + @"\log-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log", FileMode.Append);
                StreamWriter w = new StreamWriter(fs, Encoding.UTF8);
                w.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + where + "] " + msg);
                w.Close();
            }
        }
        private void PutText(ref IplImage img, string text, bool bTimecode, int x, int y)
        {
            x /= 2;
            y /= 2;
            string timecode = "";
            if (bTimecode) timecode = DateTime.Now.ToString(" (yyyy/MM/dd HH:mm:ss)");
            img.PutText(text + timecode, new CvPoint(x, y - 1), font, blackColor);
            img.PutText(text + timecode, new CvPoint(x, y + 1), font, blackColor);
            img.PutText(text + timecode, new CvPoint(x - 1, y), font, blackColor);
            img.PutText(text + timecode, new CvPoint(x + 1, y), font, blackColor);
            img.PutText(text + timecode, new CvPoint(x, y), font, whiteColor);
        }
        private void UpdateUvTh()
        {
#if false
            int[] uvDiff1 = new int[5];
            int[] uvDiff2 = new int[5];
            int okCnt = 0;
            int th1 = 0;
            int th2 = 0;
            for (int i = 0; i < 5; i++)
            {
                if (centerTest[i].UvDiff1 > 0) okCnt++;
                uvDiff1[i] = centerTest[i].UvDiff1;
                uvDiff2[i] = centerTest[i].UvDiff2;
            }
            if (okCnt > 3)
            {
                th1 = GetMedian(uvDiff1) + 10;
                th2 = GetMedian(uvDiff2) + 10;
                trackBarUVDiffInner.InvokeIfNeeded(() => trackBarUVDiffInner.Value = th1);
                trackBarUVDiffOuter.InvokeIfNeeded(() => trackBarUVDiffOuter.Value = th2);
            }
#endif
        }
        private int GetMedian(int[] uvDiff)
        {
            int cnt = 0;
            int sum = 0;
            for (int i = 0; i < 5; i++)
            {
                if (uvDiff[i] > 0 && uvDiff[i] < 60)
                {
                    cnt++;
                    sum += uvDiff[i];
                }
            }
            if (cnt == 0) return 10;
            double mean = (double)sum / cnt;
            int max = 10;
            for (int i = 0; i < 5; i++)
            {
                if (uvDiff[i] > 0 && uvDiff[i] < (mean + 10))
                {
                    if (uvDiff[i] > max) max = uvDiff[i];
                }
            }
            return max;
        }
        private void okProcess(int index, int palIndex, int okIndex, int stageId)
        {
            try
            {
                if (index >= 0)
                {
                    Dictionary<string, object> dicParam = new Dictionary<string, object>();
                    string srcOrgFilename1 = "Org1-" + index.ToString("000000") + ".jpg";
                    string desOrgFilename1 = "Pal" + palIndex.ToString("000") + "Index" + okIndex.ToString("00") + ".jpg";
                    string srcResFilename1 = "Res1-" + index.ToString("000000") + ".jpg";
                    string desResFilename1 = "Res" + palIndex.ToString("000") + "Index" + okIndex.ToString("00") + ".jpg";
                    string srcOrgFilename2 = "Org2-" + index.ToString("000000") + ".jpg";
                    string desOrgFilename2 = "Bau" + palIndex.ToString("000") + "Index" + okIndex.ToString("00") + ".jpg";
                    string srcResFilename2 = "Res2-" + index.ToString("000000") + ".jpg";
                    string desResFilename2 = "Rba" + palIndex.ToString("000") + "Index" + okIndex.ToString("00") + ".jpg";

                    string srcOrgName1 = m_tempDir + @"\" + srcOrgFilename1;
                    string srcResName1 = m_tempDir + @"\" + srcResFilename1;
                    string srcOrgName2 = m_tempDir + @"\" + srcOrgFilename2;
                    string srcResName2 = m_tempDir + @"\" + srcResFilename2;
                    string srcTxtName = m_tempDir + @"\" + textBoxLotNumber.Text + ".txt";

                    string desOrgName1 = m_resultDir + @"\" + textBoxLotNumber.Text + @"\" + desOrgFilename1;
                    string desResName1 = m_resultDir + @"\" + textBoxLotNumber.Text + @"\" + desResFilename1;
                    string desOrgName2 = m_resultDir + @"\" + textBoxLotNumber.Text + @"\" + desOrgFilename2;
                    string desResName2 = m_resultDir + @"\" + textBoxLotNumber.Text + @"\" + desResFilename2;
                    string desTxtName = m_resultDir + @"\" + textBoxLotNumber.Text + @"\" + textBoxLotNumber.Text + ".txt";

                    string netOrgName1 = m_uploadDir + @"\images\" + textBoxLotNumber.Text + @"\" + desOrgFilename1;
                    string netResName1 = m_uploadDir + @"\images\" + textBoxLotNumber.Text + @"\" + desResFilename1;
                    string netOrgName2 = m_uploadDir + @"\images\" + textBoxLotNumber.Text + @"\" + desOrgFilename2;
                    string netResName2 = m_uploadDir + @"\images\" + textBoxLotNumber.Text + @"\" + desResFilename2;
                    string netTxtName = m_uploadDir + @"\resultList\" + textBoxLotNumber.Text + @".txt";

                    dicParam["index"] = index;
                    dicParam["stageId"] = stageId;
                    dicParam["desOrgName1"] = desOrgName1;
                    dicParam["netOrgName1"] = netOrgName1;
                    dicParam["desResName1"] = desResName1;
                    dicParam["netResName1"] = netResName1;
                    dicParam["desOrgName2"] = desOrgName2;
                    dicParam["netOrgName2"] = netOrgName2;
                    dicParam["desResName2"] = desResName2;
                    dicParam["netResName2"] = netResName2;
                    dicParam["desTxtName"] = desTxtName;
                    dicParam["netTxtName"] = netTxtName;

                    string strTest = palIndex.ToString() + " " + okIndex.ToString() + " " + stageId.ToString();
                    StreamWriter SWrite = new StreamWriter(srcTxtName, true, System.Text.Encoding.ASCII);
                    SWrite.WriteLine(strTest);
                    SWrite.Close();
                    File.Copy(srcOrgName1, desOrgName1, true);
                    File.Copy(srcResName1, desResName1, true);
                    File.Copy(srcOrgName2, desOrgName2, true);
                    File.Copy(srcResName2, desResName2, true);
                    File.Copy(srcTxtName, desTxtName, true);
                    if (bUpload)
                    {
                        upLoadQueue.Enqueue(dicParam);
                        printRemainedUpload();
                    }

                }
                else
                {
                    bFinishSignal = true;
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        void wLotFinish()
        {
            try
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "wLotFinish (시작)"));
                Thread.Sleep(3000);
                string srcTxtName = m_tempDir + @"\" + drSystem["lotNumber"].ToString() + ".txt";
                string netCompleteName = m_uploadDir + @"\completed\" + drSystem["lotNumber"].ToString() + @".txt";
                string netListName = m_uploadDir + @"\resultList\" + drSystem["lotNumber"].ToString() + @".txt";
                if ((bool)drCounter["bUpload"])
                {
                    if (File.Exists(srcTxtName))
                    {
                        try
                        {
                            File.Copy(srcTxtName, netListName, true);
                            File.Copy(srcTxtName, netCompleteName, true);
                        }
                        catch (Exception ex)
                        {
                            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex));
                        }
                    }
                    else
                    {
                        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, srcTxtName + "파일을 찾을 수 없습니다."));
                    }
                }
                bInFinish = true;

                SaveLog();
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "SaveLog()"));
                UpdateErp();
                SendLogToEES();
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "SendLogToEES()"));

                //objectAssign(this, new ObjectAssignArgs(Color.LightBlue, ObjectAssignArgs.ObjectType.TextBox_BackColor, textBoxLotNumber));
                //objectAssign(this, new ObjectAssignArgs(Color.LightBlue, ObjectAssignArgs.ObjectType.Panel_BackColor, panelResultBase));
                textBoxLotNumber.InvokeIfNeeded(() => textBoxLotNumber.BackColor = Color.LightBlue);
                panelResultBase.InvokeIfNeeded(() => panelResultBase.BackColor = Color.LightBlue);
                tabControlView.InvokeIfNeeded(() => tabControlView.SelectedIndex = 2);

                // TODO 여기다 pop에 종료 신호 보내자
                if ((bool)drCounter["bUpload"])
                {

                    //TCPClient client = new TCPClient();
                    //client.IpAddress = popIpAddress;
                    //client.SendMessage += new TCPClient.SendMessageNotify(OnRecieveMessage4);
                    //client.Send("?get_data_ngcode||");
                    //client.Send("?set_end||mc_cd=" + mccd);
                }
                Thread.Sleep(5000);
                //buttonEnterNext.InvokeIfNeeded(() => buttonEnterNext.Enabled = true);
                //added by dkpark 2021-09-13
                //buttonLotChange.InvokeIfNeeded(() => buttonLotChange.Enabled = true);
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "wLotFinish (끝)"));
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex));
            }
        }
        private void CopyTable(string lottStartTime, string lotFinishTime)
        {
            //Bitmap b = new Bitmap(dataGridViewFinish.Width, dataGridViewFinish.Height);
            //dataGridViewFinish.DrawToBitmap(b, new Rectangle(0, 0, b.Width, b.Height));
            //pictureBoxPrevLog.InvokeIfNeeded(() => pictureBoxPrevLog.Image = b);
            try
            {
                dataSetFinish.Tables[0].Rows[0]["etc"] = "작업시간 : " + lottStartTime + " ~ " + lotFinishTime;
            }
            catch { }
            dataSetFinish.Tables[0].WriteXml(m_settingDir + @"\copyFinishLog.xml", XmlWriteMode.WriteSchema);
            LoadBackFinish();
        }
        private void SaveLog()
        {
            DateTime now = DateTime.Now;
            string lotStartTime = dataSetBackup.Tables["fixedBackup"].Rows[0]["lotStartTime"].ToString();
            string lotFinishTime = now.ToString("yyyy-MM-dd HH:mm");
            CopyTable(lotStartTime, lotFinishTime);
            string path = m_settingDir + @"\logFinish\log-" + now.ToString("yyyy-MM");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filename = path + @"\finish-" + now.ToString("yyyy-MM-dd") + ".csv";
            bool bExistFile = File.Exists(filename);
            using (FileStream fs = File.Open(filename, FileMode.Append))
            {
                using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                {
                    DataColumnCollection dcc = dataSetFinish.Tables[0].Columns;
                    if (bExistFile == false)
                    {
                        string header = "시작,마감";
                        foreach (DataColumn v in dcc)
                        {
                            header += ("," + v.Caption);
                        }
                        w.WriteLine(header);
                    }
                    {
                        string data = lotStartTime + "," + lotFinishTime;
                        DataRow dr = dataSetFinish.Tables[0].Rows[0];
                        for (int i = 0; i < dcc.Count; i++)
                        {
                            string value = dr[i].ToString();
                            if (value.Contains(","))
                            {
                                value = "\"" + value + "\"";
                            }
                            data += ("," + value);
                        }
                        w.WriteLine(data);
                    }
                }
            }
        }
        private void printRemainedUpload()
        {
            try
            {
                labelNet.InvokeIfNeeded(() => labelNet.Text = "남은 업로드 : (" + upLoadQueue.Count.ToString() + "/" + ((int)drCounter["totalUpCount"]).ToString() + ")");
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        int NumCameras;
        private bool CameraConnect()
        {
            int[] CameraId = new int[2];

            if (!Connected)
            {
                try
                {
                    // Enumerate cameras
                    LucamCamera = new LUCAMAPICOMLib.lucamCOMClass();
                    NumCameras = LucamCamera.NumCameras();
                    if (NumCameras < 1)
                    {
                        MessageBox.Show("카메라 2대가 있어야 합니다.");
                        //return false;
                    }
                    for (int id = 0; id < NumCameras; id++)
                    {
                        hCamera[id] = LucamCamera.CameraOpen(id + 1);
                        LUCAMAPICOMLib.LUCAM_VERSION_COM Version;
                        LucamCamera.QueryVersion(hCamera[id], out Version);
                        if (Version.serialnumber == (int)dataSetBackup.Tables["fixedBackup"].Rows[0]["ID1"])
                            CameraId[0] = id + 1;
                        if (Version.serialnumber == (int)dataSetBackup.Tables["fixedBackup"].Rows[0]["ID2"])
                            CameraId[1] = id + 1;
                        LucamCamera.CameraClose(hCamera[id]);
                    }
                    FrameFormat = new LUCAM_FRAME_FORMAT_COM[NumCameras];
                    for (int id = 0; id < NumCameras; id++)
                    {
                        // Connect to camera
                        hCamera[id] = LucamCamera.CameraOpen(CameraId[id]);
                        if (false == LucamCamera.GetFormat(hCamera[id], out FrameFormat[id], out FrameRate[id]))
                        {
                            MessageBox.Show("Unable to get current frame format");
                        }
                        if (FrameBuffer[id] != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(FrameBuffer[id]);
                            FrameBuffer[id] = IntPtr.Zero;
                        }
                        int Size = FrameFormat[id].width * FrameFormat[id].height;
                        if (FrameFormat[id].pixelFormat == LUCAMAPICOMLib.LUCAM_PIXEL_FORMAT.PF_16)
                            Size *= 2;
                        FrameBuffer[id] = Marshal.AllocHGlobal(Size);
                    }
                    {
                        DataRow dr = dataSetBackup.Tables["fixedBackup"].Rows[0];
                        Exposure[0] = (float)(double)dr["Exposure1"];
                        Exposure[1] = (float)(double)dr["Exposure2"];
                        Gain[0] = (float)(double)dr["Gain1"];
                        Gain[1] = (float)(double)dr["Gain2"];
                        Brightness[0] = (float)(double)dr["Brightness1"];
                        Brightness[1] = (float)(double)dr["Brightness2"];
                        Contrast[0] = (float)(double)dr["Contrast1"];
                        Contrast[1] = (float)(double)dr["Contrast2"];
                        Gamma[0] = (float)(double)dr["Gamma1"];
                        Gamma[1] = (float)(double)dr["Gamma2"];
                    }
                    NewMethod();
                    int context = 0;
                    for (int id = 0; id < NumCameras; id++)
                    {
                        if (cvBufferCam[id] != null) cvBufferCam[id].Dispose();
                        switch (id)
                        {
                            case 0:
                                snapshotCallback0 = new LuCamSnapshotCallback0(this);
                                iSnapshotCallbackId[id] = LucamCamera.AddSnapshotCallback(hCamera[id], (IlucamCOMSnapshotCallback)snapshotCallback0, ref context);
                                break;
                            case 1:
                                snapshotCallback1 = new LuCamSnapshotCallback1(this);
                                iSnapshotCallbackId[id] = LucamCamera.AddSnapshotCallback(hCamera[id], (IlucamCOMSnapshotCallback)snapshotCallback1, ref context);
                                break;
                        }
                        LucamSnapshot[id].bufferLastFrame = 0;
                        LucamSnapshot[id].exposure = Exposure[id];
                        LucamSnapshot[id].exposureDelay = 0.0F;
                        LucamSnapshot[id].flReserved1 = 0.0F;
                        LucamSnapshot[id].flReserved2 = 0.0F;
                        LucamSnapshot[id].format = FrameFormat[id];
                        LucamSnapshot[id].globalGain = Gain[id];
                        LucamSnapshot[id].shutterType = LUCAMAPICOMLib.LUCAM_SHUTTER_TYPE.ROLLING;
                        LucamSnapshot[id].strobeDelay = 0.0F;
                        LucamSnapshot[id].timeout = 3000;
                        LucamSnapshot[id].ulReserved1 = 0;
                        LucamSnapshot[id].ulReserved2 = 0;
                        LucamSnapshot[id].useHwTrigger = 0;
                        LucamSnapshot[id].useStrobe_strobeFlags = 0;
                        LucamSnapshot[id].gainBlue_Cyan = 1.0F;
                        LucamSnapshot[id].gainGrn1_Yel1 = 1.0F;
                        LucamSnapshot[id].gainGrn2_Yel2 = 1.0F;
                        LucamSnapshot[id].gainRed_Mag = 1.0F;
                        if (false == LucamCamera.EnableFastFrames(hCamera[id], ref LucamSnapshot[id]))
                        {
                            int iReturn = LucamCamera.GetLastError();
                            MessageBox.Show("Unable to enable snapshot mode");
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Cannot open camera", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (hCamera[0] == 0)
                {
                    MessageBox.Show("Cannot open camera 1.");
                    return false;
                }
                if (hCamera[1] == 0)
                {
                    MessageBox.Show("Cannot open camera 2.");
                    return false;
                }
                // enable everything
                Connected = true;
            }
            return true;
        }

        private void NewMethod(bool bForce = false)
        {
            for (int id = 0; id < NumCameras; id++)
            {
                if (hCamera[id] > 0)
                {
                    int Flags;
                    if (Exposure[id] <= 0.0f || bForce)
                    {
                        if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.exposure, out Exposure[id], out Flags))
                        {
                            MessageBox.Show("Unable to get current exposure.");
                        }
                    }
                    if (Gain[id] <= 0.0f || bForce)
                    {
                        if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.GAIN, out Gain[id], out Flags))
                        {
                            MessageBox.Show("Unable to get current exposure.");
                        }
                    }
                    if (Brightness[id] <= 0.0f || bForce)
                    {
                        if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.BRIGHTNESS, out Brightness[id], out Flags))
                        {
                            MessageBox.Show("Unable to get current exposure.");
                        }
                    }
                    if (Contrast[id] <= 0.0f || bForce)
                    {
                        if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.CONTRAST, out Contrast[id], out Flags))
                        {
                            MessageBox.Show("Unable to get current exposure.");
                        }
                    }
                    if (Gamma[id] <= 0.0f || bForce)
                    {
                        if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.GAMMA, out Gamma[id], out Flags))
                        {
                            MessageBox.Show("Unable to get current exposure.");
                        }
                    }
                    if (false == LucamCamera.SetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.exposure, Exposure[id], (LUCAM_PROP_FLAG)0))
                    {
                        MessageBox.Show("Unable to get current exposure.");
                    }
                    // get gain value
                    if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.GAIN, out Gain[id], out Flags))
                    {
                        MessageBox.Show("Unable to get current Gain.");
                    }

                    // get brightness value
                    if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.BRIGHTNESS, out Brightness[id], out Flags))
                    {
                        MessageBox.Show("Unable to get current Brightness.");
                    }

                    // get Contrast value
                    if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.CONTRAST, out Contrast[id], out Flags))
                    {
                        MessageBox.Show("Unable to get current Contrast.");
                    }

                    // get Gamma value
                    if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.GAMMA, out Gamma[id], out Flags))
                    {
                        MessageBox.Show("Unable to get current Gamma.");
                    }
                }
            }
            {
                DataRow dr = dataSetBackup.Tables["fixedBackup"].Rows[0];
                dr["Exposure1"] = Exposure[0];
                dr["Exposure2"] = Exposure[1];
                dr["Gain1"] = Gain[0];
                dr["Gain2"] = Gain[1];
                dr["Brightness1"] = Brightness[0];
                dr["Brightness2"] = Brightness[1];
                dr["Contrast1"] = Contrast[0];
                dr["Contrast2"] = Contrast[1];
                dr["Gamma1"] = Gamma[0];
                dr["Gamma2"] = Gamma[1];
            }
        }

        LuCamSnapshotCallback0 snapshotCallback0;
        LuCamSnapshotCallback1 snapshotCallback1;
        private void CameraDisconnect()
        {
            if (Connected)
            {
                for (int i = 0; i < 2; i++)
                {
                    try
                    {
                        LucamCamera.CameraClose(hCamera[i]);
                    }
                    catch
                    {
                        MessageBox.Show("Unable to disconnect from camera.");
                    }

                    // disable everything
                    Connected = false;
                    if (FrameBuffer[i] != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(FrameBuffer[i]);
                        FrameBuffer[i] = IntPtr.Zero;
                    }
                }
            }
        }
        private void loadCenterParameter(ref DataRow dr)
        {
            if (dr["colorInner"] == DBNull.Value) dr["colorInner"] = 50.0;
            if (dr["colorOuter"] == DBNull.Value) dr["colorOuter"] = 1.5;
            if (dr["colorUnderprint"] == DBNull.Value) dr["colorUnderprint"] = 10;
            if (dr["bEdgeCheckPass"] == DBNull.Value) dr["bEdgeCheckPass"] = false;
            if (dr["bPass1"] == DBNull.Value) dr["bPass1"] = false;
            if (dr["bPass2"] == DBNull.Value) dr["bPass2"] = false;
            if (dr["bColor"] == DBNull.Value) dr["bColor"] = false;
            if (dr["bToric"] == DBNull.Value) dr["bToric"] = false;
            if (dr["shadowWidth"] == DBNull.Value) dr["shadowWidth"] = 10;
            if (dr["bSmoothing"] == DBNull.Value) dr["bSmoothing"] = true;
            if (dr["cannyParameter1"] == DBNull.Value) dr["cannyParameter1"] = 5;
            if (dr["cannyParameter2"] == DBNull.Value) dr["cannyParameter2"] = 30;
            if (dr["bPassCrack"] == DBNull.Value) dr["bPassCrack"] = false;
            if (dr["uvLumLowTh"] == DBNull.Value) dr["uvLumLowTh"] = 0;
            if (dr["uvLumHighTh"] == DBNull.Value) dr["uvLumHighTh"] = 0;
            if (dr["bUVLowHigh"] == DBNull.Value) dr["bUVLowHigh"] = false;

            checkBoxForce1Edge.Checked = (bool)dr["bEdgeCheckPass"];
            checkBoxForce1.Checked = (bool)dr["bPass1"];
            checkBoxForce2.Checked = (bool)dr["bPass2"];
            checkBoxColor.Checked = (bool)dr["bColor"];
            checkBoxBanung.Checked = (bool)dr["bToric"];
            // TODO : 강제 off
            checkBoxPassCrack.Checked = false;// (bool)dr["bPassCrack"];
            //checkBoxPassCrack.Checked = (bool)dr["bPassCrack"];
            trackBarCenterDiffTh.SetRange(0, 50);
            trackBarCenterShadowWidth.SetRange(0, 50);
            trackBarUVDiffOuter.SetRange(0, 100);
            trackBarUVDiffInner.SetRange(0, 100);
            trackBarCenterInner.SetRange(30 * 2, 100 * 2);
            trackBarCenterOuter.SetRange(0 * 10, 20 * 10);
            trackBarCenterUnderprint.SetRange(1, 50);
            trackBarCenterDiffTh.Value = (int)dr["diffTh"];
            trackBarCenterShadowWidth.Value = (int)dr["shadowWidth"];
            trackBarUVDiffOuter.Value = (int)((double)dr["devTh"]);
            trackBarUVDiffInner.Value = (int)dr["lumDiffTh"];
            trackBarCenterInner.Value = (int)((double)dr["colorInner"] * 2 + 0.5);
            trackBarCenterOuter.Value = (int)((double)dr["colorOuter"] * 10 + 0.5);
            trackBarCenterUnderprint.Value = (int)dr["colorUnderprint"];
            textBoxCenterS1Start.Text = dr["s1Start"].ToString();
            textBoxCenterS2Start.Text = dr["s2Start"].ToString();
            textBoxCenterS3Start.Text = dr["s3Start"].ToString();
            textBoxCenterS1Count.Text = dr["s1Count"].ToString();
            textBoxCenterS2Count.Text = dr["s2Count"].ToString();
            textBoxCenterS3Count.Text = dr["s3Count"].ToString();
            checkBoxUVLumTh.Checked = (bool)dr["bUVLowHigh"];
            trackBarUVLumLowTh.Value = (int)dr["uvLumLowTh"];
            trackBarUVLumHighTh.Value = (int)dr["uvLumHighTh"];
            checkBoxBubbleTest.Checked = (bool)dr["bBubbleTest"];
        }
        private void loadBaundaryParameter(ref DataRow dr)
        {
            if (dr["cannyParameter1"] == DBNull.Value) dr["cannyParameter1"] = 2;
            if (dr["cannyParameter2"] == DBNull.Value) dr["cannyParameter2"] = 64;
            if (dr["outerTh"] == DBNull.Value) dr["outerTh"] = 5;
            if (dr["diffTh"] == DBNull.Value) dr["diffTh"] = 5;
            if (dr["top"] == DBNull.Value) dr["top"] = 25;
            if (dr["topTh"] == DBNull.Value) dr["topTh"] = 25;
            if (dr["topAngle"] == DBNull.Value) dr["topAngle"] = 15;
            if (dr["topStart"] == DBNull.Value) dr["topStart"] = 5;
            if (dr["topWidth"] == DBNull.Value) dr["topWidth"] = 10;
            if (dr["boundaryDiff"] == DBNull.Value) dr["boundaryDiff"] = 10;
            if (dr["baOffset"] == DBNull.Value) dr["baOffset"] = 4;
            if (dr["baWidth"] == DBNull.Value) dr["baWidth"] = 20;
            if (dr["traceDiffTh"] == DBNull.Value) dr["traceDiffTh"] = 10;
            if (dr["cntDkFlag"] == DBNull.Value) dr["cntDkFlag"] = 1;
            trackBarBoundaryInnerTh.SetRange(0, 100);
            trackBarBoundaryOuterTh.SetRange(0, 100);
            trackBarBoundaryTop.SetRange(0, 100);
            trackBarBoundaryTopTh.SetRange(0, 100);
            trackBarBoundaryTopAngle.SetRange(0, 90);
            trackBarBoundaryTopStart.SetRange(0, 50);
            trackBarBoundaryTopWidth.SetRange(0, 50);
            trackBarBoundaryDiff.SetRange(0, 100);
            trackBarBoundaryBaOffset.SetRange(0, 20);
            trackBarBoundaryBaWidth.SetRange(1, 50);
            trackBarBoundaryInnerTh.Value = (int)((double)dr["devTh"] * 10);
            trackBarBoundaryOuterTh.Value = (int)((double)dr["outerTh"] * 10);
            trackBarBoundaryTop.Value = (int)dr["top"];
            trackBarBoundaryTopTh.Value = (int)dr["topTh"];
            trackBarBoundaryTopAngle.Value = (int)dr["topAngle"];
            trackBarBoundaryTopStart.Value = (int)dr["topStart"];
            trackBarBoundaryTopWidth.Value = (int)dr["topWidth"];
            trackBarBoundaryDiff.Value = (int)dr["boundaryDiff"];
            trackBarBoundaryBaOffset.Value = (int)dr["baOffset"];
            trackBarBoundaryBaWidth.Value = (int)dr["baWidth"];
            textBoxBoundaryS1Start.Text = dr["s1Start"].ToString();
            textBoxBoundaryS2Start.Text = dr["s2Start"].ToString();
            textBoxBoundaryS3Start.Text = dr["s3Start"].ToString();
            textBoxBoundaryS1Count.Text = dr["s1Count"].ToString();
            textBoxBoundaryS2Count.Text = dr["s2Count"].ToString();
            textBoxBoundaryS3Count.Text = dr["s3Count"].ToString();
        }
        private void SetAdminPanel(bool bSet)
        {
            panel3.Enabled = bSet;
            panelAdmin1.Enabled = bSet;
            panelAdmin2.Enabled = bSet;
            panelAdmin3.Enabled = bSet;
            panelAdmin4.Enabled = bSet;
            pictureBoxCam1.Enabled = bSet;
            pictureBoxCam2.Enabled = bSet;
            timerAdmin.Enabled = bSet;
            if (bSet)
            {
                buttonAdmin.ForeColor = Color.Black;
                buttonAdmin.BackColor = Color.Red;
            }
            else
            {
                buttonAdmin.ForeColor = Color.Red;
                buttonAdmin.BackColor = Color.Transparent;
            }
        }
        private void capture()
        {
            try
            {
                if (StageIndexState == 1)
                {
                    StageIndexState = 2;
                    StageIndex = 0;
                }
                if (StageIndex < 100)
                {
                    StageIndex++;
                }
                if (bLive)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "Live 중 측정 무시");
                    labelMessage.Text = "Live 중 측정 무시";
                    MessageBox.Show(labelMessage.Text, "경고", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                for (int i = 0; i < 2; i++)
                {
                    eStatusCam[i] = eResultStatus.READY;
                }
                labelResult1.Text = "< 측정중 >";
                labelResult2.Text = "< 측정중 >";
                labelResult1.BackColor = Color.Yellow;
                labelResult2.BackColor = Color.Yellow;
                pictureBoxResult1.Image = null;
                pictureBoxResult2.Image = null;
                curNG[0] = Constants.NG_NO_LENZ;
                curNG[1] = Constants.NG_NO_LENZ;
                m_startTick = DateTime.Now.Ticks;
                statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[측정 시작]");
                drCounter["count"] = ((int)drCounter["count"]) + 1;
                buttonCapture.Enabled = false;
                //if (serialPortIptPLC1.IsOpen)
                //    serialPortIptPLC1.Write("@T71\r");
                //cntPreview = 0;
                labelResult1.Text = "";
                labelResult2.Text = "";

                buttonCapture.Text = "[측정 시작[" + StageIndex + "] (" + drCounter["count"].ToString() + ")]";
                wCheckResult = new Thread(new ThreadStart(wtCheckResult));
                wCheckResult.Start();
                string filename = "";
                for (int id = 0; id < 2; id++)
                {
                    if (hCamera[id] == 0)
                    {
                        filename = m_settingDir + @"\cam" + (id + 1).ToString() + ".png";
                        if (cvBufferCam[id] != null) cvBufferCam[id].Dispose();
                        try
                        {
                            cvBufferCam[id] = new IplImage(filename, LoadMode.GrayScale);
                        }
                        catch
                        {
                            cvBufferCam[id] = new IplImage(2592, 1944, BitDepth.U8, 1);
                        }
                    }
                    switch (id)
                    {
                        case 0:
                            wCheckCamera[id] = new Thread(new ThreadStart(wtCheckCamera1));
                            break;
                        case 1:
                            wCheckCamera[id] = new Thread(new ThreadStart(wtCheckCamera2));
                            break;
                    }
                    wCheckCamera[id].Priority = ThreadPriority.Highest;
                    wCheckCamera[id].Start();
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        bool bLive = false;
        private void Live(int id)
        {
            FormLive dlg = new FormLive(hCamera[id], LucamCamera, drcRoi[id]);
            bLive = true;
            dlg.ShowDialog();
            bLive = false;
            NewMethod(true);
        }
        private void CamSetting(int id)
        {
            if (((0x8000 & GetAsyncKeyState(0x11)) != 0) && hCamera[id] > 0)
            {
#if false
                LucamCamera.DisplayPropertyPage(hCamera[id], 0);
#else
                int Flags;
                if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.exposure, out Exposure[id], out Flags))
                {
                    MessageBox.Show("Unable to get current exposure.");
                }

                // get gain value
                if (false == LucamCamera.GetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.GAIN, out Gain[id], out Flags))
                {
                    MessageBox.Show("Unable to get current Gain.");
                }
                FormCamsetup dlg = new FormCamsetup();
                dlg.Exposure = Exposure[id];
                dlg.Gain = Gain[id];
                dlg.ShowDialog();
                Exposure[id] = dlg.Exposure;
                Gain[id] = dlg.Gain;
                if (false == LucamCamera.SetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.exposure, Exposure[id], LUCAM_PROP_FLAG.USE))
                {
                    MessageBox.Show("Unable to set current exposure.");
                }

                // get gain value
                if (false == LucamCamera.SetProperty(hCamera[id], LUCAMAPICOMLib.LUCAM_PROPERTY.GAIN, Gain[id], LUCAM_PROP_FLAG.USE))
                {
                    MessageBox.Show("Unable to set current Gain.");
                }
#endif
            }
        }
        //private void buttonResult(int index)
        //{
        //    string filename = "";
        //    string filename2 = "";
        //    int viewIndex = 0;

        //    if (index <= 1)
        //    {
        //        viewIndex = cntAll;
        //        filename = m_tempDir + @"\Org1-" + viewIndex.ToString("000000") + ".jpg";
        //        filename2 = m_tempDir + @"\Res1-" + viewIndex.ToString("000000") + ".jpg";
        //    }
        //    else
        //    {
        //        viewIndex = cntAll-2;
        //        filename = m_tempDir + @"\Org2-" + viewIndex.ToString("000000") + ".jpg";
        //        filename2 = m_tempDir + @"\Res2-" + viewIndex.ToString("000000") + ".jpg";
        //    }
        //    PreViewOpen(filename, filename2, "결과 (" + index.ToString() + ":" + viewIndex.ToString() + ")");
        //}
        private void PreViewOpen(string filename, string filename2, string title, int id)
        {
            try
            {
                IplImage cvImage = new IplImage(filename, LoadMode.GrayScale);
                IplImage cvImage2 = new IplImage(filename2);
                CvRect rcRoi = new CvRect((int)drcRoi[id]["roiLeft"], (int)drcRoi[id]["roiTop"], (int)drcRoi[id]["roiWidth"], (int)drcRoi[id]["roiHeight"]);
                FormPreview form = new FormPreview(cvImage, cvImage2, title, rcRoi);
                openedPreview = form;
                form.Show();
                cvImage.Dispose();
                cvImage2.Dispose();
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void saveParameter(string filename, string tableName)
        {
            try
            {
                dataSetBackup.Tables[tableName].WriteXml(filename, XmlWriteMode.WriteSchema);
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void loadParameter(string filename, string tableName)
        {
            try
            {
                dataSetBackup.Tables[tableName].Clear();
                dataSetBackup.Tables[tableName].ReadXml(filename);
                DataRow dr = dataSetBackup.Tables[tableName].Rows[0];
                if (tableName.Equals("centerParameter"))
                {
                    loadCenterParameter(ref dr);

                }
                else
                {
                    loadBaundaryParameter(ref dr);
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private bool CheckLotFormat(string name)
        {
            try
            {
                int length = name.Length;
                if (length == 12)
                {
                    for (int i = 0; i < name.Length; i++)
                    {
                        if (name[i] == ' ') return false;
                        if (name[i] == '/') return false;
                        if (name[i] == '*') return false;
                        if (name[i] == '?') return false;
                        if (name[i] == '.') return false;
                        if (name[i] == '+') return false;
                        if (i != 8 && name[i] == '-') return false;
                    }
                    int year = int.Parse(name.Substring(0, 4));
                    if (year < 2012 || year > 2099) return false;
                    int month = int.Parse(name.Substring(4, 2));
                    if (month < 1 || month > 12) return false;
                    int day = int.Parse(name.Substring(6, 2));
                    if (day < 1 || day > 31) return false;
                    if (name[8] != '-') return false;
                    int item = int.Parse(name.Substring(9, 3));
                    if (item < 0 || item > 999) return false;
                    return true;
                }
                else if (length == 13)
                {
                    for (int i = 0; i < name.Length; i++)
                    {
                        if (name[i] == ' ') return false;
                        if (name[i] == '/') return false;
                        if (name[i] == '*') return false;
                        if (name[i] == '?') return false;
                        if (name[i] == '.') return false;
                        if (name[i] == '+') return false;
                        if (i != 9 && name[i] == '-') return false;
                    }
                    //if (name[0] != 'B' && name[0] != 'b') return false;
                    int year = int.Parse(name.Substring(1, 4));
                    if (year < 2012 || year > 2099) return false;
                    int month = int.Parse(name.Substring(5, 2));
                    if (month < 1 || month > 12) return false;
                    int day = int.Parse(name.Substring(7, 2));
                    if (day < 1 || day > 31) return false;
                    if (name[9] != '-') return false;
                    int item = int.Parse(name.Substring(10, 3));
                    if (item < 0 || item > 999) return false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        object objectLockCounter = new object();
        private void deleteResult()
        {
            try
            {
                StageIndex = 0;
                StageIndexState = 0;

                lock (objectLockCounter)
                {
                    drCounter["count"] = 0;
                }
                drCounter["cntOk"] = 0;
                drCounter["cntNg"] = 0;
                drCounter["totalUpCount"] = 0;
                drCounter["NoLenz1"] = 0;
                drCounter["NoLenz2"] = 0;
                drCounter["OK1"] = 0;
                drCounter["OK2"] = 0;
                drCounter["Bubble"] = 0;
                drCounter["Defeat1"] = 0;
                drCounter["Defeat2"] = 0;
                drCounter["Paint"] = 0;
                drCounter["Edge"] = 0;
                drCounter["Ct"] = 0;
                drCounter["Sil"] = 0;
                drCounter["DK"] = 0;
                drCounter["TOP"] = 0;

                drCounter["DisplayNG_Bubble"] = 0;
                drCounter["DisplayNG_Defeat"] = 0;
                drCounter["DisplayNG_CT"] = 0;
                drCounter["DisplayNG_DK"] = 0;
                drCounter["DisplayNG_Sil"] = 0;
                drCounter["DisplayNG_None"] = 0;
                drCounter["DisplayNG_Empty1"] = 0;
                drCounter["DisplayNG_Empty2"] = 0;

                upLoadQueue.Clear();
                groupBoxResultList.Text = "결과 리스트";

                try
                {
                    System.IO.Directory.Delete(m_tempDir, true);
                }
                catch
                {
                    MessageBox.Show("Temp 폴더 삭제 실패");
                }
                try
                {
                    System.IO.Directory.CreateDirectory(m_tempDir);
                }
                catch
                {
                    MessageBox.Show("Temp 폴더 생성 실패");
                }
                dataGridViewOK.Rows.Clear();
                dataGridViewNG.Rows.Clear();
                dataSetOkNg.Tables["ok"].Rows.Clear();
                dataSetOkNg.Tables["ng"].Rows.Clear();
                centerTest[0].Clear();
                boundaryTest[0].Clear();
                for (int i = 0; i < 2; i++)
                {
                    cnt[i] = 0;
                    curNG[i] = Constants.NG_NO_LENZ;
                    prevNG[i] = Constants.NG_NO_LENZ;
                    eStatusCam[i] = eResultStatus.READY;
                    eStatusPrevMaster[i] = eResultStatus.READY;
                }
                labelResult1.Text = "-";
                labelResult2.Text = "-";
                labelCount1.Text = "0";
                labelCount2.Text = "0";
                buttonCapture.Text = "[측정시작]";
                pictureBoxCam1.Image = null;
                pictureBoxCam2.Image = null;
                pictureBoxResult1.Image = null;
                pictureBoxResult2.Image = null;
                textBoxLotNumber.Text = " ";
                buttonLotChange_Click(null, null);
                //pictureBoxPalette.Reset();
                m_palIndex = 0;
                m_okIndex = 0;
                userControlStatus1.Init();
                //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        volatile int StageIndex = 0;
        volatile int StageIndexState = 0;
        private void SaveErrorImage(int id)
        {
            try
            {
                {
                    {
                        string srcName = m_tempDir + @"\Org1-" + id.ToString("000000") + ".jpg";
                        string desName = m_errorDir + @"\Org1-" + id.ToString("000000") + ".jpg";
                        File.Copy(srcName, desName, true);
                    }
                    {
                        string srcName = m_tempDir + @"\Res1-" + id.ToString("000000") + ".jpg";
                        string desName = m_errorDir + @"\Res1-" + id.ToString("000000") + ".jpg";
                        File.Copy(srcName, desName, true);
                    }
                    dataSetBackup.WriteXml(m_errorDir + @"\Error-" + DateTime.Now.ToString("MM-dd-HH-mm") + "-Org1-" + id.ToString("000000") + ".err", XmlWriteMode.WriteSchema);
                }
                {
                    {
                        string srcName = m_tempDir + @"\Org2-" + id.ToString("000000") + ".jpg";
                        string desName = m_errorDir + @"\Org2-" + id.ToString("000000") + ".jpg";
                        File.Copy(srcName, desName, true);
                    }
                    {
                        string srcName = m_tempDir + @"\Res2-" + id.ToString("000000") + ".jpg";
                        string desName = m_errorDir + @"\Res2-" + id.ToString("000000") + ".jpg";
                        File.Copy(srcName, desName, true);
                    }
                }
                MessageBox.Show("오류영상(" + id.ToString() + ")을 저장하였습니다.");
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void instantDiCtrl1_Interrupt(object sender, Automation.BDaq.DiSnapEventArgs e)
        {
            if (bAdvIO)
            {
                try
                {
                    Invoke(new UpdateListview(UpdateListviewMethod), new object[] { e.SrcNum, e.PortData });
                }
                catch (System.Exception) { }
            }
        }
        protected delegate void UpdateListview(int ch, byte[] portData);
        DateTime prevShotSignal = DateTime.Now;
        private void UpdateListviewMethod(int ch, byte[] portData)
        {
            if (bAdvIO)
            {
                switch (ch)
                {
                    case 0:
                        statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "[측정 수신]");
                        TimeSpan ts = DateTime.Now - prevShotSignal;
                        if (ts.TotalSeconds > 0 && ts.TotalSeconds < 1.0)
                        {
                            string msg = "[연속수신 무시(" + ts.TotalSeconds.ToString("0.000") + ")]";
                            statusStrip1.InvokeIfNeeded(() => labelMessage.Text = msg);
                            logPrint(MethodBase.GetCurrentMethod().Name, msg);
                            break;
                        }
                        prevShotSignal = DateTime.Now;
                        if (StageIndexState > 1)
                        {
                            StageIndexState++;
                        }
                        if (StageIndexState > 10)
                        {
                            StageIndexState = 0;
                        }
                        capture();
                        break;
                    case 1:
                        //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.PUT_PALETTE);
                        //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                        //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();
                        statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[팔래트 이송 수신]");
                        logPrint(MethodBase.GetCurrentMethod().Name, @"[1.팔래트 이송 수신]");
                        userControlStatus1.RcvPalResetSignal();
                        break;
                    case 2:
                        //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.MOVE_PALETTE);
                        //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                        //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();
                        statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[스테이지초기화 수신]");
                        if (StageIndexState == 0)
                        {
                            StageIndexState = 1;
                        }
                        logPrint(MethodBase.GetCurrentMethod().Name, @"[6.스테이지초기화 수신]");
                        break;
                    case 3:
                        //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.LOT_OUT);
                        //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                        //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();
                        statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[잔량 배출 수신]");
                        logPrint(MethodBase.GetCurrentMethod().Name, @"[7.잔량 배출 수신]");
                        //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.PUT_PALETTE);
                        if (upLoadQueue.Count > 0)
                        {
                            MessageBox.Show("업로드 되지않은 데이터가 있습니다. 확인 바랍니다.");
                        }
                        //if (MessageBox.Show("잔량 배출 수신 처리 하겠습니까?", "잔량 배출", MessageBoxButtons.YesNo) == DialogResult.No)
                        //{
                        //    return;
                        //}
                        okProcess(-1, -1, -1, -1);
                        wLotFinish();
                        break;
                }
            }
        }
        int[] StageNgCnt = new int[12];
        private void ReadSerialDataFromIptPLC1(object s, EventArgs e)
        {
            try
            {
                if (bAdvIO == false)
                {
                    int bytesToRead = 0;
                    try
                    {
                        bytesToRead = serialPortIptPLC1.BytesToRead;
                    }
                    catch (System.IO.IOException)
                    {
                        return;
                    }
                    char[] buffer = new char[bytesToRead];
                    serialPortIptPLC1.Read(buffer, 0, bytesToRead);
                    for (int i = 0; i < bytesToRead; i++)
                    {
                        switch (buffer[i])
                        {
                            case '0':
                                statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "[측정 수신]");
                                TimeSpan ts = DateTime.Now - prevShotSignal;
                                if (ts.TotalSeconds > 0 && ts.TotalSeconds < 1.0)
                                {
                                    string msg = "[연속수신 무시(" + ts.TotalSeconds.ToString("0.000") + ")]";
                                    statusStrip1.InvokeIfNeeded(() => labelMessage.Text = msg);
                                    logPrint(MethodBase.GetCurrentMethod().Name, msg);
                                    break;
                                }
                                prevShotSignal = DateTime.Now;
                                if (StageIndexState > 1)
                                {
                                    StageIndexState++;
                                }
                                if (StageIndexState > 10)
                                {
                                    StageIndexState = 0;
                                }
                                capture();
                                break;
                            case '1':
                                //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.PUT_PALETTE);
                                //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                                //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();
                                statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[팔래트 이송 수신]");
                                logPrint(MethodBase.GetCurrentMethod().Name, @"[1.팔래트 이송 수신]");
                                break;
                            case '6':
                                //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.MOVE_PALETTE);
                                //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                                //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();
                                statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[스테이지초기화 수신]");
                                if (StageIndexState == 0)
                                {
                                    StageIndexState = 1;
                                }
                                logPrint(MethodBase.GetCurrentMethod().Name, @"[6.스테이지초기화 수신]");
                                break;
                            case '7':
                                //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.LOT_OUT);
                                //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                                //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();
                                statusStrip1.InvokeIfNeeded(() => labelMessage.Text += "[잔량 배출 수신]");
                                logPrint(MethodBase.GetCurrentMethod().Name, @"[7.잔량 배출 수신]");
                                //pictureBoxPalette.SetSignal(MyPaletteBox.signalCode.PUT_PALETTE);
                                //if (upLoadQueue.Count > 0)
                                //{
                                //    MessageBox.Show("업로드 되지않은 데이터가 있습니다. 확인 바랍니다.");
                                //}
                                //if (MessageBox.Show("잔량 배출 수신 처리 하겠습니까?", "잔량 배출", MessageBoxButtons.YesNo) == DialogResult.No)
                                //{
                                //    return;
                                //}
                                okProcess(-1, -1, -1, -1);
                                wLotFinish();
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void testCapture()
        {
            labelCount1.Text = "<Start>";
            labelCount2.Text = "<Start>";
            for (int i = 0; i < 2; i++)
            {
                eStatusCam[i] = eResultStatus.READY;
            }
            buttonTestCapture.Enabled = false;
            labelMessage.Text = "[테스트 카메라 시작]";
            GC.Collect();
            wCheckResult = new Thread(new ThreadStart(wtCheckResult));
            wCheckResult.Start();
            for (int id = 0; id < 2; id++)
            {
                switch (id)
                {
                    case 0:
                        wCheckCamera[id] = new Thread(new ThreadStart(wtCheckCamera1));
                        break;
                    case 1:
                        wCheckCamera[id] = new Thread(new ThreadStart(wtCheckCamera2));
                        break;
                }
                wCheckCamera[id].Priority = ThreadPriority.Highest;
                wCheckCamera[id].Start();
            }
        }
        #endregion "functions"

        #region "UI function"
        public FormMain()
        {
            InitializeComponent();
            labelMessage.Text = "*";
            m_settingDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\setting_IptVisionLucam";
            m_resultDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\result_IptVisionLucam";
            m_errorDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\오류영상";
            m_tempDir = m_settingDir + @"\temp";
            m_uploadDir = @"y:";
            System.IO.Directory.CreateDirectory(m_settingDir);
            System.IO.Directory.CreateDirectory(m_errorDir);
            try
            {
                System.IO.Directory.CreateDirectory(m_tempDir);
            }
            catch
            {
                MessageBox.Show("temp 디렉토리를 생성할 수 없습니다.");
            }
        }
        private IntPtr mainHandle = IntPtr.Zero;
        private void FormMain_Load(object sender, EventArgs e)
        {
            mainHandle = this.Handle;
            StageNgCnt.Initialize();
            DataRow dr = null;
            iu = new ImageUtil(this);
            centerTest = new ClassCenter[1] { new ClassCenter(0, this) };
            boundaryTest = new ClassBoundary[1] { new ClassBoundary(0, this) };
            this.Text += (" " + Properties.Resources.VERSION + " / " + iu.Version);
            try
            {
                wUpload = new Thread(new ThreadStart(wtUpload));
                wUpload.Start();

                if (File.Exists(m_settingDir + @"\backup.xml"))
                {
                    try
                    {
                        dataSetBackup.ReadXml(m_settingDir + @"\backup.xml");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error in backup.xml");
                        logPrint(MethodBase.GetCurrentMethod().Name, ex.Message);
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Create system.xml");
                }
                foreach (DataTable dt in dataSetBackup.Tables)
                {
                    if (dt.TableName == "roiCam")
                    {
                        for (int i = dt.Rows.Count; i < 10; i++)
                        {
                            dt.Rows.Add(dt.NewRow());
                        }

                    }
                    else
                    {
                        for (int i = dt.Rows.Count; i < 1; i++)
                        {
                            dt.Rows.Add(dt.NewRow());
                        }
                    }
                }
                IptUtilDataSet.CheckNullItem(dataSetBackup);
                try
                {
                    dr = dataSetBackup.Tables["fixedBackup"].Rows[0];
                    drcRoi = dataSetBackup.Tables["roiCam"].Rows;
                    drSystem = dataSetBackup.Tables["fixedBackup"].Rows[0];
                    bDebug = (bool)dr["debug"];
                    bNoCamera = (100 == (int)dr["machineCode"]) ? true : false;
                    if (!dr["resultDrive"].ToString().Equals("c"))
                    {
                        m_resultDir = dr["resultDrive"].ToString() + @":\result_IptVisionLucam";
                    }
                    System.IO.Directory.CreateDirectory(m_resultDir);
                    bAdvIO = (bool)dr["bAdvIO"];
                    bResizing = (bool)dr["bResizing"];
                    checkBoxErpOff.Checked = true;// (bool)drSystem["bErpOff"];
                    checkBoxEesOff.Checked = (bool)drSystem["bEesOff"];
                    checkBoxErp1110_Off.Checked = (bool)drSystem["bErp1110_Off"];
                    checkBoxImageServerOff.Checked = (bool)drSystem["bImageServer_Off"];
                    bUseNetwork = (bool)drSystem["bUseNetwork"];
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                logPrint(MethodBase.GetCurrentMethod().Name, "==== <Start " + Properties.Resources.VERSION + "> ====");
                {
                    if (File.Exists(m_tempDir + @"\tempData.xml"))
                    {
                        //if (MessageBox.Show("저장된 임시 데이터를 사용 합니까?", "임시 데이터 사용 확인", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        //{
                        //    File.Delete(m_tempDir + @"\" + textBoxLotNumber.Text + ".txt");
                        //    File.Delete(m_tempDir + @"\tempData.xml");
                        //    buttonDeleteResult_Click(null, null);
                        //}
                        //else
                        {
                            dataSetTempData.ReadXml(m_tempDir + @"\tempData.xml", XmlReadMode.ReadSchema);
                            DataRow drTemp = dataSetTempData.Tables["tempBackup"].Rows[0];
                            if (drTemp["totalUpCount"] == DBNull.Value) drTemp["totalUpCount"] = 0;
                            if (drTemp["NoLenz1"] == DBNull.Value) drTemp["NoLenz1"] = 0;
                            if (drTemp["OK1"] == DBNull.Value) drTemp["OK1"] = 0;
                            if (drTemp["Bubble"] == DBNull.Value) drTemp["Bubble"] = 0;
                            if (drTemp["Defeat1"] == DBNull.Value) drTemp["Defeat1"] = 0;
                            if (drTemp["Paint"] == DBNull.Value) drTemp["Paint"] = 0;
                            if (drTemp["NoLenz2"] == DBNull.Value) drTemp["NoLenz2"] = 0;
                            if (drTemp["OK2"] == DBNull.Value) drTemp["OK2"] = 0;
                            if (drTemp["Sil"] == DBNull.Value) drTemp["Sil"] = 0;
                            if (drTemp["Defeat2"] == DBNull.Value) drTemp["Defeat2"] = 0;
                            if (drTemp["DK"] == DBNull.Value) drTemp["DK"] = 0;
                            if (drTemp["TOP"] == DBNull.Value) drTemp["TOP"] = 0;
                            if (drTemp["Edge"] == DBNull.Value) drTemp["Edge"] = 0;

                            if (drTemp["DisplayNG_Bubble"] == DBNull.Value) drTemp["DisplayNG_Bubble"] = 0;
                            if (drTemp["DisplayNG_Defeat"] == DBNull.Value) drTemp["DisplayNG_Defeat"] = 0;
                            if (drTemp["DisplayNG_DK"] == DBNull.Value) drTemp["DisplayNG_DK"] = 0;
                            if (drTemp["DisplayNG_Sil"] == DBNull.Value) drTemp["DisplayNG_Sil"] = 0;
                            if (drTemp["DisplayNG_None"] == DBNull.Value) drTemp["DisplayNG_None"] = 0;

                            eStatusPrevMaster[0] = (eResultStatus)drTemp["eStatusPrevMaster0"];
                            eStatusPrevMaster[1] = (eResultStatus)drTemp["eStatusPrevMaster1"];
                            //cntAll = (int)drTemp["count"];
                            //cntOk = (int)drTemp["cntOk"];
                            //cntNg = (int)drTemp["cntNg"];
                            //bUpload = (bool)drTemp["bUpload"];
                            //drCounter["totalUpCount"] = (int)drTemp["totalUpCount"];
                            //cntNoLenz1 = (int)drTemp["NoLenz1"];
                            //cntOK1 = (int)drTemp["OK1"];
                            //cntBubble = (int)drTemp["Bubble"];
                            //cntDefeat1 = (int)drTemp["Defeat1"];
                            //cntPaint = (int)drTemp["Paint"];
                            //cntNoLenz2 = (int)drTemp["NoLenz2"];
                            //cntOK2 = (int)drTemp["OK2"];
                            //drCounter["Sil"] = (int)drTemp["Sil"];
                            //cntDefeat2 = (int)drTemp["Defeat2"];
                            //cntDK = (int)drTemp["DK"];
                            //drCounter["TOP"] = (int)drTemp["TOP"];
                            //cntEdge = (int)drTemp["Edge"];

                            //cntDisplayNG_Bubble = (int)drTemp["DisplayNG_Bubble"];
                            //cntDisplayNG_Defeat = (int)drTemp["DisplayNG_Defeat"];
                            //cntDisplayNG_DK = (int)drTemp["DisplayNG_DK"];
                            //cntDisplayNG_Sil = (int)drTemp["DisplayNG_Sil"];
                            //cntDisplayNG_None = (int)drTemp["DisplayNG_None"];

                            //pictureBoxPalette.LoadBackup(m_tempDir + @"\paletteBackup.xml");
                            dataSetOkNg.Clear();
                            dataSetOkNg.ReadXml(m_tempDir + @"\OkNgList.xml", XmlReadMode.ReadSchema);
                            foreach (DataRow dr2 in dataSetOkNg.Tables["ok"].Rows)
                            {
                                DataGridViewRow dgvr = new DataGridViewRow();
                                DataGridViewCell cell;
                                for (int i = 0; i < 4; i++)
                                {
                                    cell = new DataGridViewTextBoxCell();
                                    cell.Value = dr2[i].ToString();
                                    dgvr.Cells.Add(cell);
                                }
                                dataGridViewOK.Rows.Insert(0, dgvr);
                            }
                            foreach (DataRow dr2 in dataSetOkNg.Tables["ng"].Rows)
                            {
                                DataGridViewRow dgvr = new DataGridViewRow();
                                DataGridViewCell cell;
                                for (int i = 0; i < 4; i++)
                                {
                                    cell = new DataGridViewTextBoxCell();
                                    cell.Value = dr2[i].ToString();
                                    dgvr.Cells.Add(cell);
                                }
                                dataGridViewNG.Rows.Insert(0, dgvr);
                            }
                        }
                    }
                    else
                    {
                        File.Delete(m_tempDir + @"\" + textBoxLotNumber.Text + ".txt");
                    }
                }
                foreach (DataTable dt in dataSetBackup.Tables)
                {

                    if (dt.Rows.Count == 0)
                    {
                        dt.Rows.Add(dt.NewRow());
                    }
                }

                DataTable dtBackup = dataSetTempData.Tables["tempBackup"];
                for (int i = dtBackup.Rows.Count; i < 1; i++)
                {
                    dtBackup.Rows.Add(dtBackup.NewRow());
                }
                IptUtilDataSet.CheckNullItem(dataSetTempData);
                drCounter = dataSetTempData.Tables["tempBackup"].Rows[0];
            }
            catch
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "초기 변수 로딩 실패");
                labelMessage.Text += "[초기 변수 로딩 실패]";
                foreach (DataTable dataTable in dataSetBackup.Tables)
                {
                    dataTable.Clear();
                    dataTable.Rows.Add(dataTable.NewRow());
                }
            }
            try
            {
                dr = dataSetBackup.Tables["fixedBackup"].Rows[0];
                textBoxLotNumber.Text = dr["lotNumber"].ToString();
                previousLotNumber = textBoxLotNumber.Text;
                labelVersion.Text = Properties.Resources.VERSION + " - " + dr["machineCode"].ToString() + "호기";
                if (bNoCamera)
                {
                    labelMessage.Text += "[디버그 모드 (" + Thread.CurrentThread.ManagedThreadId.ToString() + ")]";
                }
                else
                {
                    if (bAdvIO)
                    {
                        try
                        {
                            instantDiCtrl1 = new Automation.BDaq.InstantDiCtrl();// (this.components);
                                                                                 //this.instantDiCtrl1._StateStream = ((Automation.BDaq.DeviceStateStreamer)(resources.GetObject("instantDiCtrl1._StateStream")));
                            instantDiCtrl1.Interrupt += new System.EventHandler<Automation.BDaq.DiSnapEventArgs>(this.instantDiCtrl1_Interrupt);

                            instantDoCtrl1 = new Automation.BDaq.InstantDoCtrl();// (this.components);
                                                                                 //this.instantDoCtrl1._StateStream = ((Automation.BDaq.DeviceStateStreamer)(resources.GetObject("instantDoCtrl1._StateStream")));
                            instantDiCtrl1.SelectedDevice = new Automation.BDaq.DeviceInformation(0);
                            instantDoCtrl1.SelectedDevice = new Automation.BDaq.DeviceInformation(0);
                            if (instantDiCtrl1.Device != null)
                            {
                                labelMessage.Text = "[" + instantDoCtrl1.Device.Description + " 초기화 완료]";
                                instantDiCtrl1.DiintChannels[0].Enabled = true;
                                instantDiCtrl1.DiintChannels[1].Enabled = true;
                                instantDiCtrl1.DiintChannels[2].Enabled = true;
                                instantDiCtrl1.DiintChannels[3].Enabled = true;
                            }
                            if (instantDoCtrl1.Initialized) instantDoCtrl1.Write(0, 0);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Adventech IO");
                        }
                    }
                    else
                    {
                        try
                        {
                            string portName = drSystem["serialPort"].ToString();
                            serialPortIptPLC1.PortName = portName;
                            serialPortIptPLC1.Open();
                            labelMessage.Text += "[" + portName + " 초기화 완료]";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
                dr = dataSetBackup.Tables["centerParameter"].Rows[0];
                loadCenterParameter(ref dr);
                dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
                loadBaundaryParameter(ref dr);

                buttonCapture.Text = "[측정 시작(" + drCounter["count"].ToString() + ")]";

                //textBoxOkIndex.Text = pictureBoxPalette.GetOkIndex().ToString();
                //textBoxPaletteIndex.Text = pictureBoxPalette.GetPaletteIndex().ToString();

            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name + "변수 설정 적용부1", ex.ToString());
                labelMessage.Text += "[변수 설정 적용부1 실패]";
                labelMessage.BackColor = Color.Red;
            }

            try
            {
                checkBoxAutoUV.Checked = true;
                if (bUpload)
                {
                    if (Directory.Exists(m_uploadDir))
                    {
                        labelNet.Text = "로트번호 변경 버튼을 눌러주세요.";
                        labelNet.BackColor = Color.Orange;
                    }
                    else
                    {
                        labelNet.Text = "네트웤 연결 이상 발생";
                        labelNet.BackColor = Color.Red;
                    }
                }
                else
                {
                    labelNet.Text = "네트웤 저장하지 않음";
                    labelNet.BackColor = Color.Yellow;
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name + "변수 설정 적용부3", ex.ToString());
                labelMessage.Text += "[변수 설정 적용부3 실패]";
                labelMessage.BackColor = Color.Red;
            }
            try
            {
                dataSetFinish.ReadXml(m_settingDir + @"\finish.xml", XmlReadMode.ReadSchema);
            }
            catch
            {
                dataSetFinish.Clear();
                dataSetFinish.Tables[0].Rows.Add();
            }
            dataGridViewFinish.DataSource = dataSetFinish.Tables[0];
            foreach (DataGridViewColumn v in dataGridViewFinish.Columns)
            {
                v.HeaderText = dataSetFinish.Tables[0].Columns[v.Name].Caption;
                switch (v.Name)
                {
                    case "COUNT_ERROR":
                        v.ReadOnly = false;
                        break;
                    default:
                        v.ReadOnly = true;
                        break;
                }
            }
            //if (serialPortIptPLC.IsOpen)
            //{
            //    serialPortIptPLC.Write("@D40\r");
            //}
            //this.SetBounds(0, 0, 1280, 980);
            tabControlView.Dock = DockStyle.Bottom;
            if (labelMessage.BackColor.Equals(Color.Red))
            {
                MessageBox.Show("초기화 과정에 문제가 있습니다. 재시작 바랍니다.\n계속 문제가 발생시 문제 해결 후 재시작 바랍니다.");
                bForceExit = true;
                Close();
            }
            CameraConnect();
            userControlStatus1.LoadData(m_tempDir + @"\temp.xml");
            m_okIndex = userControlStatus1.OkIndex;
            m_palIndex = userControlStatus1.PalIndex;
            if (bAdvIO)
            {
                if (instantDiCtrl1.Initialized) instantDiCtrl1.SnapStart();
            }
            pictureBoxCam1.AllowDrop = true;
            pictureBoxCam2.AllowDrop = true;
            pictureBoxResult1.AllowDrop = true;
            pictureBoxResult2.AllowDrop = true;
            #region MES
            if (bUseNetwork)
            {
                connections();
            }
            //added by dkpark 2021-09-13
            tCheckFinish = new Thread(new ThreadStart(wCheckFinish));
            tCheckFinish.Start();
            LoadBackFinish();
            #endregion
        }
        Thread tCheckFinish = null;
        private void wCheckFinish()
        {
            try
            {
                try
                {
#if !DEBUG
                    while (!bClosing)
                    {
                        Thread.Sleep(100);
                        //if (bInFinish == false && clientUDP[1] != null)
                        if (bInFinish == false)
                        {
                            //string[] address = new string[3] { drSystem["addrPlcQty"].ToString(), drSystem["addrPlcOk"].ToString(), drSystem["addrPlcNg"].ToString() };
                            //int[] D = new int[address.Length];
                            //byte[] sendData = MitubishPLC.RandomReadWordTypeQnA3E(address);
                            try
                            {
                                //clientUDP[1].Send(sendData, sendData.Length, plcEndPoint[1]);
                                //byte[] buffer = clientUDP[1].Receive(ref plcEndPoint[1]);
                                //if (buffer[0] == 0xd0)
                                {
                                    //int headPos = 11;
                                    //for (int i = 0; i < address.Length; i++)
                                    //{
                                    //    int value1 = buffer[headPos];
                                    //    headPos++;
                                    //    int value2 = buffer[headPos];
                                    //    headPos++;
                                    //    D[i] = (Int16)(value1 + (value2 << 8));
                                    //}
                                    //countPlcQty = D[0];
                                    //countPlcOk = D[1];
                                    //countplcNg = D[2];
                                    SafeNativeMethods.PostMessage(mainHandle, (uint)UWM.UPDATE_DATASET_FINISH, IntPtr.Zero, IntPtr.Zero);
                                    //DataRow dr = dataSetFinish.Tables[0].Rows[0];
                                    //int countError = (int)dr["PR_QTY"] - countPlcQty;
                                    //int countNg = countPlcQty - countPlcOk + countError;
                                    ////dr["COUNT_OK"] = countPlcOk;
                                    //dr["COUNT_OK"] = (int)drCounter["cntOk"];
                                    //dr["COUNT_NONE"] = (int)drCounter["DisplayNG_None"];
                                    //dr["COUNT_EDGE_BUBBLE"] = (int)drCounter["DisplayNG_Bubble"];
                                    //dr["COUNT_DEFECT"] = (int)drCounter["DisplayNG_Defeat"];
                                    //dr["COUNT_DK"] = (int)drCounter["DisplayNG_DK"];
                                    //dr["COUNT_SIL"] = (int)drCounter["DisplayNG_Sil"];
                                    //dr["COUNT_CT"] = (int)drCounter["DisplayNG_CT"];
                                    //dr["COUNT_EMPTY1"] = (int)drCounter["DisplayNG_Empty1"];
                                    //dr["COUNT_EMPTY2"] = (int)drCounter["DisplayNG_Empty2"];
                                    //dr["COUNT_ERROR"] = countError;
                                    //dr["COUNT_NG"] = countNg;
                                    //dr["plcQty"] = countPlcQty;
                                    //dr["plcOk"] = countPlcOk;
                                    //dr["plcNg"] = countplcNg;
                                }
                                //if (toolStripStatusLabelPlc2.BackColor != Color.Green)
                                //{
                                //    statusStrip1.InvokeIfNeeded(() => toolStripStatusLabelPlc2.BackColor = Color.Green);
                                //}
                                //if (bFinishSignal)
                                //{
                                //    if (upLoadQueue.Count > 0)
                                //    {
                                //        continue;
                                //    }
                                //    bFinishSignal = false;
                                //    Thread t = new Thread(new ThreadStart(wLotFinish));
                                //    t.Start();
                                //}
                            }
                            catch// (Exception ex)
                            {
                                //if (toolStripStatusLabelPlc2.BackColor != Color.Red)
                                //{
                                //    statusStrip1.InvokeIfNeeded(() => toolStripStatusLabelPlc2.BackColor = Color.Red);
                                //}
                            }
                        }
                    }
#endif
                }
                catch (Exception ex)
                {
                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", ex));
                }
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(1)", ex));
            }
        }
        private void LoadBackFinish()
        {
            try
            {
                DataTable dt = new DataTable();
                dt.ReadXml(m_settingDir + @"\copyFinishLog.xml");
                dataGridViewFinishCopy.DataSource = dt;
                foreach (DataGridViewColumn v in dataGridViewFinishCopy.Columns)
                {
                    v.HeaderText = dt.Columns[v.Name].Caption;
                    switch (v.Name)
                    {
                        case "COUNT_ERROR":
                            v.ReadOnly = false;
                            break;
                        default:
                            v.ReadOnly = true;
                            break;
                    }
                }
                labelWorkingTime.InvokeIfNeeded(() => labelWorkingTime.Text = dt.Rows[0]["etc"].ToString());
            }
            catch { }
        }
       
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bAdvIO)
            {
                if (instantDoCtrl1.Initialized) instantDoCtrl1.Write(0, 0);
            }
            else
            {
                if (serialPortIptPLC1.IsOpen)
                {
                    serialPortIptPLC1.Write("@D00\r");
                }
            }
            bClosing = true;
            if (bForceExit)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "======== <Forece Exit> ========");
                logPrint("", "");
            }
            else
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "남은 업로드 : " + upLoadQueue.Count.ToString());
                logPrint(MethodBase.GetCurrentMethod().Name, "======== <Finish> ========");
                logPrint("", "");
                //dataSetBackup.Tables["fixedBackup"].Rows[0]["debug"] = bDebug;
                //dataSetBackup.Tables["fixedBackup"].Rows[0]["mode"] = "Master";
                if ((int)drCounter["count"] == 0)
                {
                    File.Delete(m_tempDir + @"\tempData.xml");
                }
                //else if (MessageBox.Show("임시 데이터를 저장 합니까?", "임시 데이터 저장 확인", MessageBoxButtons.YesNo) != DialogResult.Yes)
                //{
                //    File.Delete(m_tempDir + @"\tempData.xml");
                //    buttonDeleteResult_Click(null, null);
                //}
                else
                {
                    //pictureBoxPalette.SaveBackup(m_tempDir + @"\paletteBackup.xml");
                    dataSetOkNg.WriteXml(m_tempDir + @"\OkNgList.xml", XmlWriteMode.WriteSchema);
                    //dataSetFinish.WriteXml(m_settingDir + @"\finish.xml", XmlWriteMode.WriteSchema);
                    //dataSetTempData.Tables["tempBackup"].Rows.Clear();
                    //DataRow drTemp = dataSetTempData.Tables["tempBackup"].NewRow();
                    //drTemp["eStatusPrevMaster0"] = eStatusPrevMaster[0];
                    //drTemp["eStatusPrevMaster1"] = eStatusPrevMaster[1];
                    //drTemp["eStatusPrevMaster2"] = 0;
                    //drTemp["eStatusPrevMaster3"] = 0;
                    //drTemp["eStatusPrevMaster4"] = 0;
                    //drTemp["count"] = cntAll;
                    //drTemp["cntOk"] = cntOk;
                    //drTemp["cntNg"] = cntNg;
                    //drTemp["bUpload"] = bUpload;
                    //drTemp["totalUpCount"] = (int)drCounter["totalUpCount"];
                    //drTemp["NoLenz1"] = cntNoLenz1;
                    //drTemp["OK1"] = cntOK1;
                    //drTemp["Bubble"] = cntBubble;
                    //drTemp["Defeat1"] = cntDefeat1;
                    //drTemp["Edge"] = cntEdge;
                    //drTemp["Paint"] = cntPaint;
                    //drTemp["NoLenz2"] = cntNoLenz2;
                    //drTemp["OK2"] = cntOK2;
                    //drTemp["Sil"] = drCounter["Sil"];
                    //drTemp["Defeat2"] = cntDefeat2;
                    //drTemp["DK"] = cntDK;
                    //drTemp["TOP"] = drCounter["TOP"];
                    //drTemp["DisplayNG_Bubble"] = cntDisplayNG_Bubble;
                    //drTemp["DisplayNG_Defeat"] = cntDisplayNG_Defeat;
                    //drTemp["DisplayNG_DK"] = cntDisplayNG_DK;
                    //drTemp["DisplayNG_Sil"] = cntDisplayNG_Sil;
                    //drTemp["DisplayNG_None"] = cntDisplayNG_None;
                    SaveTempData();
                }
                dataSetBackup.WriteXml(m_settingDir + @"\backup.xml", XmlWriteMode.WriteSchema);
            }
            if (bAdvIO)
            {
                if (instantDiCtrl1.Initialized) instantDiCtrl1.SnapStop();
            }
            else
            {
                if (serialPortIptPLC1.IsOpen)
                {
                    serialPortIptPLC1.DiscardInBuffer();
                    serialPortIptPLC1.DiscardOutBuffer();
                    Thread.Sleep(100);
                    serialPortIptPLC1.Close();
                }
            }
            for (int i = 0; i < 2; i++)
            {
                if (wCheckCamera[i] != null)
                {
                    wCheckCamera[i].Interrupt();
                    wCheckCamera[i].Join();
                    wCheckCamera[i] = null;
                }
            }
            if (wCheckResult != null)
            {
                wCheckResult.Interrupt();
                wCheckResult.Join();
            }
            if (wResultList != null)
            {
                wResultList.Interrupt();
                wResultList.Join();
            }
            if (wUpload != null)
            {
                wUpload.Interrupt();
                wUpload.Join();
            }
            if (wUploader != null)
            {
                if (wUploader.IsAlive)
                {
                    wUploader.Interrupt();
                    wUploader.Join();
                }
            }
            #region MES
            if (bUseNetwork)
            {
                closeConnection();
            }
            #endregion
            for (int i = 0; i < 2; i++)
            {
                if (iSnapshotCallbackId[i] >= 0)
                {
                    if (!LucamCamera.RemoveSnapshotCallback(hCamera[i], iSnapshotCallbackId[i]))
                    {
                        MessageBox.Show("Unable to remove snapshot callback");
                    }
                }
            }
            CameraDisconnect();

        }
        void SaveTempData()
        {
            dataSetTempData.WriteXml(m_tempDir + @"\tempData.xml", XmlWriteMode.WriteSchema);
        }
        private void buttonTestCapture_Click(object sender, EventArgs e)
        {
            try
            {
                if ((0x8000 & GetAsyncKeyState(0x11)) != 0)
                {
                    testCapture();
                }
            }
            catch { }
        }
        private void labelVersion_Click(object sender, EventArgs e)
        {
            if ((0x8000 & GetAsyncKeyState(0x11)) != 0)
            {
                FormTableView form = new FormTableView(ref dataSetBackup);
                form.ShowDialog();
            }
        }
        private void buttonExit_Click(object sender, EventArgs e)
        {
            logPrint(MethodBase.GetCurrentMethod().Name, "CLICK");
            Close();
        }
        private void checkBoxForce1Edge_CheckedChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bEdgeCheckPass"] = checkBoxForce1Edge.Checked;
        }
        private void checkBoxForce1_CheckedChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bPass1"] = checkBoxForce1.Checked;
            if (checkBoxForce1.Checked)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=1");
            }
            else
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=0");
            }
        }
        private void checkBoxForce2_CheckedChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bPass2"] = checkBoxForce2.Checked;
            if (checkBoxForce2.Checked)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=1");
            }
            else
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=0");
            }
        }
        private void checkBoxBanung_CheckedChanged(object sender, EventArgs e)
        {
            panelBanung.Visible = checkBoxBanung.Checked;
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bToric"] = checkBoxBanung.Checked;
            if (checkBoxBanung.Checked)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=1");
            }
            else
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=0");
            }
        }
        private void checkBoxColor_CheckedChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bColor"] = checkBoxColor.Checked;
            panelColorParameter.Visible = checkBoxColor.Checked;
            if (checkBoxColor.Checked)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=1");
            }
            else
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "Check=0");
            }
        }
        private void buttonAdmin_Click(object sender, EventArgs e)
        {
            if ((0x8000 & GetAsyncKeyState(0x11)) != 0)
            {
                if (buttonAdmin.ForeColor.Equals(Color.Red))
                {
                    FormPassword dlg = new FormPassword(dataSetBackup.Tables["fixedBackup"].Rows[0]["password"].ToString());
                    dlg.ShowDialog();
                    if (dlg.Pass)
                    {
                        SetAdminPanel(true);
                    }
                }
                else
                {
                    SetAdminPanel(false);
                }
            }
        }
        private void buttonCapture_Click(object sender, EventArgs e)
        {
            try
            {
                if (bNoCamera || ((0x8000 & GetAsyncKeyState(0x11)) != 0))
                {
                    capture();
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void buttonCam1Setting_Click(object sender, EventArgs e)
        {
            CamSetting(0);
        }
        private void buttonCam2Setting_Click(object sender, EventArgs e)
        {
            CamSetting(1);
        }
        private void buttonCamResult1_Click(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    int index = int.Parse(pictureBoxResult1.Tag.ToString());
                    buttonResult(index);
                }
                catch
                {
                    string[] seperators = new string[1] { "|" };
                    string[] s = pictureBoxResult1.Tag.ToString().Split(seperators, StringSplitOptions.None);
                    buttonResultString(s[1], s[0], 0);
                }
            }
            catch { }
            //buttonResult(1);
        }
        private void buttonCamResult2_Click(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    int index = int.Parse(pictureBoxResult2.Tag.ToString());
                    buttonResult2(index);
                }
                catch
                {
                    string[] seperators = new string[1] { "|" };
                    string[] s = pictureBoxResult2.Tag.ToString().Split(seperators, StringSplitOptions.None);
                    buttonResultString(s[1], s[0], 1);
                }
            }
            catch { }
            //buttonResult(2);
        }
        private void buttonResultString(string filename2, string filename1, int id)
        {
            //if ((0x8000 & SafeNativeMethods.GetAsyncKeyState(0x11)) != 0)
            {
                PreViewOpen(filename2, filename1, "결과 (" + filename1 + ")", id);
            }
        }

        private void buttonResult(int viewIndex)
        {
            //if ((0x8000 & SafeNativeMethods.GetAsyncKeyState(0x11)) != 0)
            {
                string filename = "";
                string filename2 = "";
                {
                    filename2 = m_tempDir + @"\Org1-" + viewIndex.ToString("000000") + ".jpg";
                    filename = m_tempDir + @"\Res1-" + viewIndex.ToString("000000") + ".jpg";
                }
                PreViewOpen(filename2, filename, "결과 (" + index.ToString() + ":" + viewIndex.ToString() + ")", 0);
            }

            //string filename = "";
            //string filename2 = "";
            //int viewIndex = 0;

            //if (index <= 5)
            //{
            //    viewIndex = (cntAll - 1) * 5 + index;
            //    filename = m_tempDir + @"\Org1-" + viewIndex.ToString("000000") + ".jpg";
            //    filename2 = m_tempDir + @"\Res1-" + viewIndex.ToString("000000") + ".jpg";
            //}
            //else
            //{
            //    viewIndex = (cntAll - 2) * 5 + index - 5;
            //    filename = m_tempDir + @"\Org2-" + viewIndex.ToString("000000") + ".jpg";
            //    filename2 = m_tempDir + @"\Res2-" + viewIndex.ToString("000000") + ".jpg";
            //}
            //PreViewOpen(filename, filename2, "결과 (" + index.ToString() + ":" + viewIndex.ToString() + ")");
        }
        private void buttonResult2(int viewIndex)
        {
            //if ((0x8000 & SafeNativeMethods.GetAsyncKeyState(0x11)) != 0)
            {
                string filename = "";
                string filename2 = "";
                {
                    filename2 = m_tempDir + @"\Org2-" + viewIndex.ToString("000000") + ".jpg";
                    filename = m_tempDir + @"\Res2-" + viewIndex.ToString("000000") + ".jpg";
                }
                PreViewOpen(filename2, filename, "결과 (" + index.ToString() + ":" + viewIndex.ToString() + ")", 1);
            }

            //string filename = "";
            //string filename2 = "";
            //int viewIndex = 0;

            //if (index <= 5)
            //{
            //    viewIndex = (cntAll - 1) * 5 + index;
            //    filename = m_tempDir + @"\Org1-" + viewIndex.ToString("000000") + ".jpg";
            //    filename2 = m_tempDir + @"\Res1-" + viewIndex.ToString("000000") + ".jpg";
            //}
            //else
            //{
            //    viewIndex = (cntAll - 2) * 5 + index - 5;
            //    filename = m_tempDir + @"\Org2-" + viewIndex.ToString("000000") + ".jpg";
            //    filename2 = m_tempDir + @"\Res2-" + viewIndex.ToString("000000") + ".jpg";
            //}
            //PreViewOpen(filename, filename2, "결과 (" + index.ToString() + ":" + viewIndex.ToString() + ")");
        }
        bool bValueChanged = false;
        private void trackBarCenterDiffTh_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["diffTh"] = trackBarCenterDiffTh.Value;
            labelCenterDiffTh.Text = "차이 임계값=" + trackBarCenterDiffTh.Value.ToString();
            bValueChanged = true;
        }
        private void trackBarCenterShadowWidth_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["shadowWidth"] = trackBarCenterShadowWidth.Value;
            labelCenterShadowWidth.Text = "테두리 그림자 두께=" + trackBarCenterShadowWidth.Value.ToString();
            bValueChanged = true;
        }
        private void trackBarUVDiffOuter_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["devTh"] = trackBarUVDiffOuter.Value / 1.0; //자외선 차이(외측)
            dr["lumDiffTh"] = trackBarUVDiffInner.Value; //자외선 차이(내측)
            labelUVDiff.Text = "자외선 차이(내/외)=" + trackBarUVDiffInner.Value.ToString() + "/" + (trackBarUVDiffOuter.Value / 1.0).ToString("0");
            //DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            //dr["devTh"] = trackBarUVDiffOuter.Value / 1.0;
            //labelCenterDevTh.Text = "자외선 차이(외측)=" + (trackBarUVDiffOuter.Value / 1.0).ToString("0");
            if (!checkBoxAutoUV.Checked) bValueChanged = true;
        }
        private void trackBarUVDiffInner_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["devTh"] = trackBarUVDiffOuter.Value / 1.0; //자외선 차이(외측)
            dr["lumDiffTh"] = trackBarUVDiffInner.Value; //자외선 차이(내측)
            labelUVDiff.Text = "자외선 차이(내/외)=" + trackBarUVDiffInner.Value.ToString() + "/" + (trackBarUVDiffOuter.Value / 1.0).ToString("0");
            //DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            //dr["lumDiffTh"] = trackBarCenterLumDiffTh.Value;
            //labelUVDiff.Text = "자외선 차이(내측)=" + trackBarCenterLumDiffTh.Value.ToString();
            if (!checkBoxAutoUV.Checked) bValueChanged = true;
        }
        private void ChangeSave()
        {
            if (bValueChanged)
            {
                bValueChanged = false;
                dataSetBackup.WriteXml(m_settingDir + @"\backup.xml", XmlWriteMode.WriteSchema);
            }
        }
        private void buttonCenterApply_Click(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            try
            {
                dr["s1Start"] = int.Parse(textBoxCenterS1Start.Text);
                dr["s2Start"] = int.Parse(textBoxCenterS2Start.Text);
                dr["s3Start"] = int.Parse(textBoxCenterS3Start.Text);
                dr["s1Count"] = int.Parse(textBoxCenterS1Count.Text);
                dr["s2Count"] = int.Parse(textBoxCenterS2Count.Text);
                dr["s3Count"] = int.Parse(textBoxCenterS3Count.Text);
                dataSetBackup.WriteXml(m_settingDir + @"\backup.xml", XmlWriteMode.WriteSchema);
                MessageBox.Show("적용 완료");
            }
            catch
            {
                textBoxCenterS1Start.Text = dr["s1Start"].ToString();
                textBoxCenterS2Start.Text = dr["s2Start"].ToString();
                textBoxCenterS3Start.Text = dr["s3Start"].ToString();
                textBoxCenterS1Count.Text = dr["s1Count"].ToString();
                textBoxCenterS2Count.Text = dr["s2Count"].ToString();
                textBoxCenterS3Count.Text = dr["s3Count"].ToString();
            }
            bValueChanged = true;
        }
        private void buttonBoundaryApply_Click(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            try
            {
                dr["s1Start"] = int.Parse(textBoxBoundaryS1Start.Text);
                dr["s2Start"] = int.Parse(textBoxBoundaryS2Start.Text);
                dr["s3Start"] = int.Parse(textBoxBoundaryS3Start.Text);
                dr["s1Count"] = int.Parse(textBoxBoundaryS1Count.Text);
                dr["s2Count"] = int.Parse(textBoxBoundaryS2Count.Text);
                dr["s3Count"] = int.Parse(textBoxBoundaryS3Count.Text);
                dataSetBackup.WriteXml(m_settingDir + @"\backup.xml", XmlWriteMode.WriteSchema);
                MessageBox.Show("적용 완료");
            }
            catch
            {
                textBoxBoundaryS1Start.Text = dr["s1Start"].ToString();
                textBoxBoundaryS2Start.Text = dr["s2Start"].ToString();
                textBoxBoundaryS3Start.Text = dr["s3Start"].ToString();
                textBoxBoundaryS1Count.Text = dr["s1Count"].ToString();
                textBoxBoundaryS2Count.Text = dr["s2Count"].ToString();
                textBoxBoundaryS3Count.Text = dr["s3Count"].ToString();
            }
            bValueChanged = true;
        }
        private void textBoxLotNumber_TextChanged(object sender, EventArgs e)
        {
            textBoxLotNumber.BackColor = Color.Red;
        }
        private void textBoxLotNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                buttonLotChange.PerformClick();
            }
        }
        private void trackBarCenterInner_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["colorInner"] = trackBarCenterInner.Value / 2.0;
            labelCenterInner.Text = "내측(%)=" + dr["colorInner"].ToString();
            bValueChanged = true;
        }
        private void trackBarCenterOuter_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["colorOuter"] = trackBarCenterOuter.Value / 10.0;
            labelCenterOuter.Text = "외측(%)=" + dr["colorOuter"].ToString();
            bValueChanged = true;
        }
        private void trackBarCenterUnderprint_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["colorUnderprint"] = trackBarCenterUnderprint.Value;
            labelCenterUnderprint.Text = "인쇄과부족 민감도=" + dr["colorUnderprint"].ToString();
            bValueChanged = true;
        }
        private void trackBarBoundaryOuterTh_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["outerTh"] = trackBarBoundaryOuterTh.Value / 10.0;
            labelBoundaryOuterTh.Text = "실 거리=" + (trackBarBoundaryOuterTh.Value / 10.0).ToString("0.##");
            bValueChanged = true;
        }
        private void checkBoxAutoUV_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAutoUV.Checked)
            {
                trackBarUVDiffInner.Value = 40;
                trackBarUVDiffOuter.Value = 40;
            }
            bValueChanged = true;
        }

        private void buttonSaveParameter_Click(object sender, EventArgs e)
        {
            try
            {
                saveFileDialog1.InitialDirectory = m_settingDir;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    saveParameter(saveFileDialog1.FileName, "centerParameter");
                    saveParameter(saveFileDialog1.FileName + "_b", "boundaryParameter");
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void buttonLoadParameter_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.InitialDirectory = m_settingDir;
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    loadParameter(openFileDialog1.FileName, "centerParameter");
                    loadParameter(openFileDialog1.FileName + "_b", "boundaryParameter");
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void trackBarBoundaryInnerTh_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["devTh"] = trackBarBoundaryInnerTh.Value / 10.0;
            labelBoundaryDevTh.Text = "뜯김 거리=" + (trackBarBoundaryInnerTh.Value / 10.0).ToString("0.##");
        }

        private void trackBarBoundaryBaOffset_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["baOffset"] = trackBarBoundaryBaOffset.Value;
            labelBoundaryBa.Text = "테두리 시작/두께=" + trackBarBoundaryBaOffset.Value.ToString() + "/" + trackBarBoundaryBaWidth.Value.ToString();
        }
        private void trackBarBoundaryBaWidth_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["baWidth"] = trackBarBoundaryBaWidth.Value;
            labelBoundaryBa.Text = "테두리 시작/두께=" + trackBarBoundaryBaOffset.Value.ToString() + "/" + trackBarBoundaryBaWidth.Value.ToString();
        }
        private void trackBarBoundaryDiff_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["boundaryDiff"] = trackBarBoundaryDiff.Value;
            labelBoundaryDiff.Text = "테두리 명암차이=" + trackBarBoundaryDiff.Value.ToString();
        }
        private void trackBarBoundaryTopTh_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["topTh"] = trackBarBoundaryTopTh.Value;
            labelBoundayTopTh.Text = "탑 결정 밝기차=" + trackBarBoundaryTopTh.Value.ToString();
        }
        private void trackBarBoundaryTop_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["top"] = trackBarBoundaryTop.Value;
            labelBoundayTop.Text = "탑 최대 크기=" + trackBarBoundaryTop.Value.ToString();
        }
        private void trackBarBoundaryTopAngle_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["topAngle"] = trackBarBoundaryTopAngle.Value;
            labelBoundaryTopAngle.Text = "탑 허용 각도=" + trackBarBoundaryTopAngle.Value.ToString();
        }
        private void trackBarBoundaryTopStart_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["topStart"] = trackBarBoundaryTopStart.Value;
            labelBoundayTopRange.Text = "탑 허용 범위=" + trackBarBoundaryTopStart.Value.ToString() + "%/" + (trackBarBoundaryTopStart.Value + trackBarBoundaryTopWidth.Value).ToString() + "%";
        }
        private void trackBarBoundaryTopWidth_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            dr["topWidth"] = trackBarBoundaryTopWidth.Value;
            labelBoundayTopRange.Text = "탑 허용 범위=" + trackBarBoundaryTopStart.Value.ToString() + "%/" + (trackBarBoundaryTopStart.Value + trackBarBoundaryTopWidth.Value).ToString() + "%";
        }

        private int group = 0;
        private string psCd = "";
        private void buttonLotChange_Click(object sender, EventArgs e)
        {
            if (previousLotNumber.Equals(textBoxLotNumber.Text) == false && (int)drCounter["count"] != 0)
            {
                if (MessageBox.Show("결과 삭제가 되지않은 상태로 로트번호 입력이 되었습니다.\n 계속 진행 합니까?", "경고", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }
            bool bCheckOkLotnumber = false;
            if (textBoxLotNumber.Text.Length == 0 || textBoxLotNumber.Text.Equals(" "))
            {
                textBoxLotNumber.Text = "undefined";
            }
            else
            {
                bCheckOkLotnumber = CheckLotFormat(textBoxLotNumber.Text);
                if (!bCheckOkLotnumber)
                {
                    if (MessageBox.Show("로트번호 규칙이 틀렸습니다.\n 계속 진행 합니까?", "경고", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }
            }
            if (bCheckOkLotnumber)
            {
                {
                    DataRow drFinish = dataSetFinish.Tables[0].Rows[0];
                    FormCheckLotNo dlg = new FormCheckLotNo();
                    dlg.StringConnectERP = strConnectionERP;
                    dlg.StringConnectEES = strConnectionEES;
                    dlg.LotNumber = textBoxLotNumber.Text;
                    dlg.PsCd = drFinish["PS_CD"].ToString();
                    dlg.ShowDialog();
                    if (dlg.OK == false)
                    {
                        drCounter["bUpload"] = false;
                        labelNet.Text = "작업자 확인 실패";
                        logPrint(MethodBase.GetCurrentMethod().Name + "(2)", labelNet.Text);
                        labelNet.BackColor = Color.Red;
                        panelResultBase.BackColor = Color.Red;
                        SetReady(false);
                        MessageBox.Show(labelNet.Text);
                        return;
                    }
                    drFinish["PS_CD"] = dlg.PsCd;
                }
                if ((int)drCounter["count"] == 0)
                {
                    if (GetSachulSaengSanSilJuk(textBoxLotNumber.Text) == false)
                    {
                        SetReady(false);
                        //MessageBox.Show("사출생산실적 없음");
                        return;
                    }
                }
                else
                {
                    logPrint(MethodBase.GetCurrentMethod().Name + "(2)", "생산중인 제품이므로 분리지시 확인을 하지 않았습니다.");
                    //MessageBox.Show("생산중인 제품이므로 분리지시 확인을 하지 않았습니다.");
                }
                if (GetUseUV(textBoxLotNumber.Text, out bool bUV) == false)
                {
                    SetReady(false);
                    MessageBox.Show("UV 정보를 읽을 수 없습니다.");
                    return;
                }
                GetNM_PW(textBoxLotNumber.Text, out string NM, out string PW);
                double pw = 0;
                try
                {
                    pw = double.Parse(PW);
                }
                catch
                {
                    try
                    {
                        string[] _pw = PW.Split(',');
                        pw = double.Parse(_pw[0]); // 다촛점 렌즈의 경우 첫번째 파워값을 획득
                    }
                    catch
                    {
                        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(POWER)", PW));
                        pw = 0;
                    }
                }
                string name_power = "";
                if (pw > -1.0)
                {
                    name_power = "0.00~0.75";
                }
                else if (pw > -2.0)
                {
                    name_power = "1.00~1.75";
                }
                else if (pw > -3.0)
                {
                    name_power = "2.00~2.75";
                }
                else if (pw > -4.0)
                {
                    name_power = "3.00~3.75";
                }
                else if (pw > -5.0)
                {
                    name_power = "4.00~4.75";
                }
                else
                {
                    name_power = "5.00~";
                }
                string[] parFiles = Directory.GetFiles(m_settingDir, "*.par");
                string fineName = "";
                foreach (var v in parFiles)
                {
                    if (v.Substring(v.Length - 2) == "_b") continue;
                    string filename = Path.GetFileNameWithoutExtension(v);
                    if (NM.Contains(filename))
                    {
                        fineName = v;
                        break;
                    }
                }
                if (fineName.Length > 0)
                {
                    loadParameter(fineName, "centerParameter");
                    loadParameter(fineName + "_b", "boundaryParameter");
                    string recipeName = Path.GetFileNameWithoutExtension(fineName);
                    labelRecipe.Text = recipeName;
                    CurrentFileName = fineName;
                    //TODO PW레시피 읽어오기 코드 넣기.
                    try
                    {
                        string filename = m_settingDir + @"\" + name_power + ".pow";
                        dataSetPowerRecipe.Clear();
                        dataSetPowerRecipe.ReadXml(filename);
                        PowerToRecipe();
                        labelRecipePower.Text = Path.GetFileNameWithoutExtension(filename);
                        bJobChange = true;
                        bWorkDone = false;
                    }
                    catch
                    {
                        labelRecipePower.Text = "Power Recipe 로드 되지 않음";
                    }
                    checkBoxUVLumTh.Checked = bUV;
                }
                else
                {
                    labelRecipe.Text = "Recipe 로드 되지 않음";
                }
            }
            if (bJobChange == false)
            {
                SetReady(false);
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", "레시피가 로드되지 않았습니다. 작업파일을 로드해주세요."));
                MessageBox.Show("레시피가 로드되지 않았습니다. 작업파일을 로드해주세요.");
                return;
            }
            if (bWorkDone == true)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", "이전 작업이 완료되었습니다. 레시피를 변경하시겠습니까?"));
                if (MessageBox.Show("이전 작업이 완료되었습니다. \n 레시피를 변경하시겠습니까? ", "알림", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    SetReady(false);
                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", "해당 제품 레시피를 불러오세요."));
                    MessageBox.Show("해당 제품 레시피를 불러오세요.");
                    bWorkDone = true;
                    return;
                }
                else
                {
                    bWorkDone = false;
                }

            }
            if (previousLotNumber.Equals(textBoxLotNumber.Text) == false && (int)drCounter["count"] != 0)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", "결과 삭제가 되지않은 상태로 로트번호 입력이 되었습니다. 계속 진행 합니까?"));
                if (MessageBox.Show("결과 삭제가 되지않은 상태로 로트번호 입력이 되었습니다.\n 계속 진행 합니까?", "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                {
                    SetReady(false);
                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", "아니오"));
                    return;
                }
            }
            if (bCheckOkLotnumber && (int)drCounter["count"] == 0)
            {
                drSystem["lotStartTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                dataSetFinish.Tables[0].WriteXml(m_settingDir + @"\finish.xml", XmlWriteMode.WriteSchema);
            }
            //if (bCheckOkLotnumber && cntAll == 0)
            //{
            //    FormCheckLotNo dlg = new FormCheckLotNo();
            //    DataRow dr = dataSetBackup.Tables["fixedBackup"].Rows[0];
            //    dlg.LotNumber = textBoxLotNumber.Text;
            //    dlg.McCd = "B" + ((int)dr["machineCode"]).ToString("000");
            //    dlg.PsCd = psCd;
            //    dlg.Group = group;
            //    dlg.ShowDialog();
            //    bCheckOkLotnumber = dlg.OK;
            //    psCd = dlg.PsCd;
            //    group = dlg.Group;
            //}
            trackBarCenterShadowWidth.Value = 3;
            previousLotNumber = textBoxLotNumber.Text;
            logPrint(MethodBase.GetCurrentMethod().Name, "LotNumber=" + textBoxLotNumber.Text);
            dataSetBackup.Tables["fixedBackup"].Rows[0]["lotNumber"] = textBoxLotNumber.Text;
            System.IO.Directory.CreateDirectory(m_resultDir + @"\" + textBoxLotNumber.Text);
            bInFinish = false;
            if (CheckLotFormat(textBoxLotNumber.Text))
            {
                try
                {
                    if (checkBoxImageServerOff.Checked == false)
                    {
                        System.IO.Directory.CreateDirectory(m_uploadDir + @"\images\" + textBoxLotNumber.Text);
                    }
                    if (!checkBoxImageServerOff.Checked && Directory.Exists(m_uploadDir + @"\images\" + textBoxLotNumber.Text) == false)
                    {
                        bUpload = false;
                        labelNet.Text = "네트웤 이상";
                        labelNet.BackColor = Color.Red;
                        panelResultBase.BackColor = Color.Red;
                        SetReady(false);
                    }
                    else
                    {
                        string strTest = dataSetBackup.Tables["fixedBackup"].Rows[0]["machineCode"].ToString();
                        string desFilename = m_resultDir + @"\" + textBoxLotNumber.Text + @"\ID.txt";
                        string netFilename = m_uploadDir + @"\images\" + textBoxLotNumber.Text + @"\ID.txt";
                        if (checkBoxImageServerOff.Checked == false)
                        {
                            StreamWriter SWrite = new StreamWriter(desFilename, false, System.Text.Encoding.ASCII);
                            SWrite.WriteLine(strTest);
                            SWrite.WriteLine("1");
                            SWrite.Close();
                        }
                        try
                        {
#region MES
                            if ((int)drCounter["count"] == 0)
                            {
                                string lastChecksheetNumber = drSystem["lastChecksheetNumber"].ToString();
                                if (textBoxLotNumber.Text == lastChecksheetNumber)
                                {
                                    SetReady(false);
                                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "이전에 작업한 채크시트 입니다. 작업을 진행할 수 없습니다. = " + lastChecksheetNumber));
                                    MessageBox.Show("이전에 작업한 채크시트 입니다. 작업을 진행할 수 없습니다.", lastChecksheetNumber);
                                    return;
                                }
                                else
                                {
                                    if (bUseNetwork)
                                    {
#if !DEBUG
                                        if(GetProjectNo(out string PR_NO))
                                        {
                                            dataSetFinish.Tables[0].Rows[0]["PR_NO"] = PR_NO;
                                            if (InsertPRTR1120() == false)
                                            {
                                                drCounter["bUpload"] = false;
                                                labelNet.Text = "EES 이상";
                                                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(EES)", labelNet.Text));
                                                labelNet.BackColor = Color.Red;
                                                panelResultBase.BackColor = Color.Red;
                                                return;
                                            }
                                            dataSetFinish.Tables[0].WriteXml(m_settingDir + @"\finish.xml", XmlWriteMode.WriteSchema);
                                            SendCheckSheetNo();
                                            SendLotStart();
                                            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", "생산 실적 등록 성공"));
                                        }
                                        else
                                        {
                                            drCounter["bUpload"] = false;
                                            labelNet.Text = "PR_NO 획득 실패";
                                            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", labelNet.Text));
                                            labelNet.BackColor = Color.Red;
                                            panelResultBase.BackColor = Color.Red;
                                            SetReady(false);
                                        }
#endif
                                    }
                                }
                            }
                            #endregion
                            if (checkBoxImageServerOff.Checked == false)
                            {
                                System.IO.File.Copy(desFilename, netFilename, true);
                                bUpload = true;
                                labelNet.Text = "네트웤 준비 완료";
                                labelNet.BackColor = Color.Transparent;
                            }
                            else
                            {
                                bUpload = false;
                                labelNet.Text = "네트웤 저장 않음";
                                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name + "(2)", labelNet.Text));
                                labelNet.BackColor = Color.Yellow;
                            }
                            panelResultBase.BackColor = Color.LimeGreen;
                            SetReady(true);
                        }
                        catch { }
                    }
                }
                catch
                {
                    bUpload = false;
                    labelNet.Text = "네트웤 이상";
                    labelNet.BackColor = Color.Red;
                    panelResultBase.BackColor = Color.Red;
                    SetReady(false);
                }
            }
            else
            {
                bUpload = false;
                labelNet.Text = "네트웤 저장하지 않음";
                labelNet.BackColor = Color.Yellow;
                panelResultBase.BackColor = Color.Yellow;
                SetReady(true);
            }
            textBoxLotNumber.BackColor = Color.FromKnownColor(KnownColor.Window);
            textBoxLotNumber.Enabled = false;
        }
        private void PowerToRecipe()
        {
            DataRow drSrc = dataSetPowerRecipe.Tables[0].Rows[0];
            DataRow drDes = dataSetBackup.Tables["centerParameter"].Rows[0];
            //drDes["bBubbleTest"] = drSrc["bBubbleTest"];
            //drDes["bubbleTestValue"] = drSrc["bubbleTestValue"];
            drDes["devTh"] = drSrc["devTh"];
            drDes["lumDiffTh"] = drSrc["lumDiffTh"];
            drDes["bUVLowHigh"] = drSrc["bUVLowHigh"];
            drDes["uvLumLowTh"] = drSrc["uvLumLowTh"];
            drDes["uvLumHighTh"] = drSrc["uvLumHighTh"];
            loadCenterParameter(ref drDes);
        }
        private void RecipeToPower()
        {
            DataRow drSrc = dataSetBackup.Tables["centerParameter"].Rows[0];
            DataRow drDes = dataSetPowerRecipe.Tables[0].Rows[0];
            //drDes["bBubbleTest"] = drSrc["bBubbleTest"];
            //drDes["bubbleTestValue"] = drSrc["bubbleTestValue"];
            drDes["devTh"] = drSrc["devTh"];
            drDes["lumDiffTh"] = drSrc["lumDiffTh"];
            drDes["bUVLowHigh"] = drSrc["bUVLowHigh"];
            drDes["uvLumLowTh"] = drSrc["uvLumLowTh"];
            drDes["uvLumHighTh"] = drSrc["uvLumHighTh"];
        }
        void SetReady(bool bOn)
        {
            if (bOn)
            {
                if (bAdvIO)
                {
                    if (instantDoCtrl1.Initialized) instantDoCtrl1.Write(0, 0x10);
                }
                else
                {
                    if (serialPortIptPLC1.IsOpen)
                    {
                        serialPortIptPLC1.Write("@D40\r");
                    }
                }
            }
            else
            {
                if (bAdvIO)
                {
                    if (instantDoCtrl1.Initialized) instantDoCtrl1.Write(0, 0);
                }
                else
                {
                    if (serialPortIptPLC1.IsOpen)
                    {
                        serialPortIptPLC1.Write("@D00\r");
                    }
                }
            }
        }
        private void buttonForceUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if ((0x8000 & GetAsyncKeyState(0x11)) != 0)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "CLICK");
                    bFinishSignal = true;
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void buttonDeleteResult_Click(object sender, EventArgs e)
        {
            try
            {
                if ((0x8000 & GetAsyncKeyState(0x11)) != 0)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "CLICK");
                    deleteResult();
                    textBoxLotNumber.Enabled = true;
                    bWorkDone = true;
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void buttonErrorSave_Click(object sender, EventArgs e)
        {
            try
            {
                if ((0x8000 & GetAsyncKeyState(0x11)) != 0)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "CLICK");
                    if (selectViewResultIndex > 0)
                        SaveErrorImage(selectViewResultIndex);
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void dataGridViewOK_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow dgvr in dataGridViewOK.SelectedRows)
            {
                selectViewResult = 1;
                selectViewResultIndex = int.Parse(dgvr.Cells[1].Value.ToString());
                string filename = m_tempDir + @"\Org1-" + selectViewResultIndex.ToString("000000") + ".jpg";
                try
                {
                    IplImage cvImage = new IplImage(filename, LoadMode.GrayScale);
                    pictureBoxResultPreview1.Image = iu.resizeImage(cvImage, pictureBoxResultPreview1.Size);
                }
                catch
                {
                    pictureBoxResultPreview1.Image = null;
                }
                filename = m_tempDir + @"\Org2-" + selectViewResultIndex.ToString("000000") + ".jpg";
                try
                {
                    IplImage cvImage = new IplImage(filename, LoadMode.GrayScale);
                    pictureBoxResultPreview2.Image = iu.resizeImage(cvImage, pictureBoxResultPreview2.Size);
                }
                catch
                {
                    pictureBoxResultPreview2.Image = null;
                }
            }
        }
        private void dataGridViewNG_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow dgvr in dataGridViewNG.SelectedRows)
            {
                selectViewResult = 2;
                selectViewResultIndex = int.Parse(dgvr.Cells[1].Value.ToString());
                string filename = m_tempDir + @"\Org1-" + selectViewResultIndex.ToString("000000") + ".jpg";
                try
                {
                    IplImage cvImage = new IplImage(filename, LoadMode.GrayScale);
                    pictureBoxResultPreview1.Image = iu.resizeImage(cvImage, pictureBoxResultPreview1.Size);
                }
                catch
                {
                    pictureBoxResultPreview1.Image = null;
                }
                filename = m_tempDir + @"\Org2-" + selectViewResultIndex.ToString("000000") + ".jpg";
                try
                {
                    IplImage cvImage = new IplImage(filename, LoadMode.GrayScale);
                    pictureBoxResultPreview2.Image = iu.resizeImage(cvImage, pictureBoxResultPreview2.Size);
                }
                catch
                {
                    pictureBoxResultPreview2.Image = null;
                }
            }
        }
        private void buttonFineView1_Click(object sender, EventArgs e)
        {
            if (selectViewResult == 0) return;
            string filename = "";
            string filename2 = "";

            filename = m_tempDir + @"\Org1-" + selectViewResultIndex.ToString("000000") + ".jpg";
            filename2 = m_tempDir + @"\Res1-" + selectViewResultIndex.ToString("000000") + ".jpg";
            PreViewOpen(filename, filename2, "내부 측정 결과", 0);
        }
        private void buttonFineView2_Click(object sender, EventArgs e)
        {
            if (selectViewResult == 0) return;
            string filename = "";
            string filename2 = "";

            filename = m_tempDir + @"\Org2-" + selectViewResultIndex.ToString("000000") + ".jpg";
            filename2 = m_tempDir + @"\Res2-" + selectViewResultIndex.ToString("000000") + ".jpg";
            PreViewOpen(filename, filename2, "테두리 측정 결과", 1);
        }
        private void timerFinishCheck_Tick(object sender, EventArgs e)
        {
            try
            {
                if (bFinishSignal)
                {
                    if (upLoadQueue.Count > 0)
                    {
                        return;
                    }
                    bFinishSignal = false;
                    Thread.Sleep(3000);
                    string srcTxtName = m_tempDir + @"\" + textBoxLotNumber.Text + ".txt";
                    string netCompleteName = m_uploadDir + @"\completed\" + textBoxLotNumber.Text + @".txt";
                    string netListName = m_uploadDir + @"\resultList\" + textBoxLotNumber.Text + @".txt";
                    if (bUpload)
                    {
                        File.Copy(srcTxtName, netListName, true);
                        File.Copy(srcTxtName, netCompleteName, true);
                    }
                    textBoxLotNumber.InvokeIfNeeded(() => textBoxLotNumber.BackColor = Color.LightBlue);
                    panelResultBase.InvokeIfNeeded(() => panelResultBase.BackColor = Color.LightBlue);

                    // TODO 여기다 pop에 종료 신호 보내자
#if false
                    DataRow dr = dataSetBackup.Tables["fixedBackup"].Rows[0];
                    string mccd = "B" + ((int)dr["machineCode"]).ToString("000");
                    TCPClient client = new TCPClient();
                    client.SendMessage += new TCPClient.SendMessageNotify(OnRecieveMessage);
                    client.Send("?set_end||mc_cd=" + mccd);
#endif
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        private void timerAdmin_Tick(object sender, EventArgs e)
        {
            timerAdmin.Enabled = false;
            SetAdminPanel(false);
        }
        private void serialPortIptPLC1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                this.Invoke(new EventHandler(ReadSerialDataFromIptPLC1));
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, ex.ToString());
            }
        }
        #endregion "UI function"

        #region "uploader code"
        private void buttonUpload_Click(object sender, EventArgs e)
        {
            buttonUpload.Enabled = false;
            progressBar1.Value = 0;
            wUploader = new Thread(new ThreadStart(wtUploader));
            wUploader.Start();
        }
        Thread wUploader = null;
        void wtUploader()
        {
            try
            {
                statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "업로드 시작");
                buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.Green);
                string basePath = m_resultDir + @"\";
                string lotPath = basePath + textBoxUploadLotNumber.Text;
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(lotPath);
                }
                catch (DirectoryNotFoundException)
                {
                    statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "LOT 결과가 없습니다.");
                    buttonUpload.InvokeIfNeeded(() => buttonUpload.Enabled = true);
                    buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.Red);
                    return;
                }
                if (files.Length < 6)
                {
                    statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "LOT 결과가 없습니다.");
                    buttonUpload.InvokeIfNeeded(() => buttonUpload.Enabled = true);
                    buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.Red);
                    return;
                }
                else
                {
                    string destPath = @"y:\images\" + textBoxUploadLotNumber.Text;
                    try
                    {
                        Directory.CreateDirectory(destPath);
                        if (Directory.Exists(destPath) == false)
                        {
                            statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "서버에 [" + destPath + "]폴더를 생성할 수 없습니다. (1)");
                            buttonUpload.InvokeIfNeeded(() => buttonUpload.Enabled = true);
                            buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.Red);
                            return;
                        }
                    }
                    catch
                    {
                        statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "서버에 [" + destPath + "]폴더를 생성할 수 없습니다. (2)");
                        buttonUpload.InvokeIfNeeded(() => buttonUpload.Enabled = true);
                        buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.Red);
                        return;
                    }
                    int cnt = 0;
                    foreach (string file in files)
                    {
                        cnt++;
                        string destFileName = destPath + @"\" + Path.GetFileName(file);
                        progressBar1.InvokeIfNeeded(() => progressBar1.Value = cnt * progressBar1.Maximum / files.Length);
                        File.Copy(file, destFileName, true);
                        Thread.Sleep(0);
                    }
                }
                try
                {
                    string sourceFileName = lotPath + @"\" + textBoxUploadLotNumber.Text + @".txt";
                    string destFileName = @"y:\resultList\" + textBoxUploadLotNumber.Text + @".txt";
                    File.Copy(sourceFileName, destFileName, true);
                }
                catch
                {
                    try
                    {
                        string sourceFileName = lotPath + @"\resultList.txt";
                        string destFileName = @"y:\resultList\" + textBoxUploadLotNumber.Text + @".txt";
                        File.Copy(sourceFileName, destFileName, true);
                    }
                    catch
                    {
                        statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "서버에 resultList를 저장할 수 없습니다.");
                        buttonUpload.InvokeIfNeeded(() => buttonUpload.Enabled = true);
                        buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.Red);
                        return;
                    }
                }
                try
                {
                    string sourceFileName = lotPath + @"\" + textBoxUploadLotNumber.Text + @".txt";
                    string destFileName = @"y:\completed\" + textBoxUploadLotNumber.Text + @".txt";
                    File.Copy(sourceFileName, destFileName, true);
                }
                catch
                {
                    try
                    {
                        string sourceFileName = lotPath + @"\resultList.txt";
                        string destFileName = @"y:\completed\" + textBoxUploadLotNumber.Text + @".txt";
                        File.Copy(sourceFileName, destFileName, true);
                    }
                    catch
                    {
                        statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "서버에 completed 정보를 저장할 수 없습니다.");
                        buttonUpload.InvokeIfNeeded(() => buttonUpload.Enabled = true);
                        buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.Red);
                        return;
                    }
                }
                statusStrip1.InvokeIfNeeded(() => labelMessage.Text = "업로드 완료");
                buttonUpload.InvokeIfNeeded(() => buttonUpload.Enabled = true);
                buttonUpload.InvokeIfNeeded(() => buttonUpload.BackColor = Color.SkyBlue);
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
        }
        #endregion "uploader code"

        private void checkBoxBubbleTest_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxAutoUV.Visible = !checkBoxBubbleTest.Checked;
            trackBarUVDiffInner.Visible = !checkBoxBubbleTest.Checked;
            trackBarUVDiffOuter.Visible = !checkBoxBubbleTest.Checked;
            bValueChanged = true;
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bBubbleTest"] = checkBoxBubbleTest.Checked;
        }

        private void checkBoxUVLumTh_CheckedChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bUVLowHigh"] = checkBoxUVLumTh.Checked;
            trackBarUVLumLowTh.Visible = checkBoxUVLumTh.Checked;
            trackBarUVLumHighTh.Visible = checkBoxUVLumTh.Checked;
            labelUVLumHighLow.Visible = checkBoxUVLumTh.Checked;
            bValueChanged = true;
        }
        private void trackBarUVLumLowTh_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["uvLumLowTh"] = trackBarUVLumLowTh.Value;
            labelUVLumHighLow.Text = "자외선 (하한/상한)=" + trackBarUVLumLowTh.Value.ToString() + "/" + trackBarUVLumHighTh.Value.ToString();
            bValueChanged = true;
        }
        private void trackBarUVLumHighTh_ValueChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["uvLumHighTh"] = trackBarUVLumHighTh.Value;
            labelUVLumHighLow.Text = "자외선 (하한/상한)=" + trackBarUVLumLowTh.Value.ToString() + "/" + trackBarUVLumHighTh.Value.ToString();
            bValueChanged = true;
        }

        private void checkBoxPassCrack_CheckedChanged(object sender, EventArgs e)
        {
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            dr["bPassCrack"] = checkBoxPassCrack.Checked;
            bValueChanged = true;
        }

        private void buttonPutPal_Click(object sender, EventArgs e)
        {

        }

        private void buttonMovePal_Click(object sender, EventArgs e)
        {

        }

        private void buttonFinish_Click(object sender, EventArgs e)
        {

        }
        private enum UWM
        {
            WM_USER = 0x400, PictureBoxIpl1_Image = WM_USER, PictureBoxIpl2_Image, UserControlBar_Value, CheckCamera1_finish, CheckCamera2_finish, serialPortPLC, FINISH1, FINISH2, UPDATE_DATASET_FINISH
        };
        protected override void WndProc(ref Message msg)
        {
            int id = (int)msg.WParam;
            int value = (int)msg.LParam;
            switch (msg.Msg)
            {
                case (int)UWM.UPDATE_DATASET_FINISH:
                    UdpateDataSetFinish();
                    break;
                case (int)UWM.serialPortPLC:
                    if (bAdvIO)
                    {
                        if (instantDoCtrl1.Initialized)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (code1[i] == '1')
                                {
                                    instantDoCtrl1.WriteBit(0, i, 1);
                                }
                                else
                                {
                                    instantDoCtrl1.WriteBit(0, i, 0);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (serialPortIptPLC1.IsOpen)
                        {
                            serialPortIptPLC1.Write("@B" + code1 + "xxxx\r");
                        }
                    }
                    timer500msOneShot.Start();
                    break;
            }
            // Call the base WndProc method
            // to process any messages not handled.
            base.WndProc(ref msg);
        }

        private void UdpateDataSetFinish()
        {
            try
            {
                DataRow drFinish = dataSetFinish.Tables[0].Rows[0];
                drFinish["COUNT_OK"] = (int)drCounter["cntOk"];
                drFinish["COUNT_NONE"] = (int)drCounter["DisplayNG_None"];//미분리
                drFinish["COUNT_EDGE_BUBBLE"] = (int)drCounter["DisplayNG_Bubble"];//에지기포
                drFinish["COUNT_DEFECT"] = (int)drCounter["DisplayNG_Defeat"];//파손
                drFinish["COUNT_DK"] = (int)drCounter["DisplayNG_DK"];//뜯김
                drFinish["COUNT_SIL"] = (int)drCounter["DisplayNG_Sil"];//실
                drFinish["COUNT_PW"] = 0;// (int)drCounter["DisplayNG_PW"];//파워 (이 장비에는 없음)
                drFinish["COUNT_CT"] = (int)drCounter["DisplayNG_CT"];//CT
                drFinish["COUNT_EMPTY1"] = (int)drCounter["DisplayNG_Empty1"];//유실
                drFinish["COUNT_EMPTY2"] = (int)drCounter["DisplayNG_Empty2"];//유실
                int countNg = (int)drFinish["COUNT_NONE"] + (int)drFinish["COUNT_EDGE_BUBBLE"] + (int)drFinish["COUNT_DEFECT"]
                    + (int)drFinish["COUNT_DK"] + (int)drFinish["COUNT_SIL"] + (int)drFinish["COUNT_PW"] + (int)drFinish["COUNT_CT"]
                    + (int)drFinish["COUNT_EMPTY1"] + (int)drFinish["COUNT_EMPTY2"];
                int countError = (int)drFinish["PR_QTY"] - (int)drFinish["COUNT_OK"] - countNg;
                drFinish["COUNT_ERROR"] = countError; //전공정오차
                drFinish["COUNT_NG"] = countNg + countError;
                drFinish["plcQty"] = 0;// countPlcQty;
                drFinish["plcOk"] = 0;// countPlcOk;
                drFinish["plcNg"] = 0;// countplcNg;
                dataSetFinish.Tables[0].WriteXml(m_settingDir + @"\finish.xml", XmlWriteMode.WriteSchema);
            }
            catch { }
        }

        private void timer500msOneShot_Tick(object sender, EventArgs e)
        {
            timer500msOneShot.Enabled = false;
            if (bAdvIO)
            {
                if (instantDoCtrl1.Initialized)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        instantDoCtrl1.WriteBit(0, i, 0);
                    }
                }
            }
            else
            {
                if (serialPortIptPLC1.IsOpen)
                {
                    serialPortIptPLC1.Write("@B0000xxxx\r");
                }
            }
        }

        private void buttonLive1_Click(object sender, EventArgs e)
        {
            Live(0);
        }

        private void buttonLive2_Click(object sender, EventArgs e)
        {
            Live(1);
        }

        private void pictureBoxCam1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }
        bool bDemo = false;
        private void pictureBoxCam1_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (s == null) return;
            string filename = s[0];

            if (filename.Contains("Org1"))
            {
                demoFilename1 = filename;
                demoFilename2 = filename.Replace("Org1", "Org2");
            }
            else if (filename.Contains("Org2"))
            {
                demoFilename2 = filename;
                demoFilename1 = filename.Replace("Org2", "Org1");
            }
            else if (filename.Contains("Pal"))
            {
                demoFilename1 = filename;
                demoFilename2 = filename.Replace("Pal", "Bau");
            }
            else if (filename.Contains("Bau"))
            {
                demoFilename2 = filename;
                demoFilename1 = filename.Replace("Bau", "Pal");
            }
            else
            {
                MessageBox.Show("영상을 찾지 못합니다");
                return;
            }
            cvBufferCam[0] = new IplImage(filename, LoadMode.GrayScale);
            bDemo = true;
            nDemo = 1;
            wtCheckCamera1();
            wtCheckResult();
            bDemo = false;
            pictureBoxCam1.Image = iu.resizeImage(cvBufferCam[0].ToBitmap(), pictureBoxCam1.Size);
            pictureBoxResult1.Image = centerTest[0].ResultBitmapThumb;
            pictureBoxCam1.Refresh();
            pictureBoxResult1.Refresh();
        }

        private void pictureBoxCam2_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (s == null) return;
            string filename = s[0];

            if (filename.Contains("Org1"))
            {
                demoFilename1 = filename;
                demoFilename2 = filename.Replace("Org1", "Org2");
            }
            else if (filename.Contains("Org2"))
            {
                demoFilename2 = filename;
                demoFilename1 = filename.Replace("Org2", "Org1");
            }
            else if (filename.Contains("Pal"))
            {
                demoFilename1 = filename;
                demoFilename2 = filename.Replace("Pal", "Bau");
            }
            else if (filename.Contains("Bau"))
            {
                demoFilename2 = filename;
                demoFilename1 = filename.Replace("Bau", "Pal");
            }
            else
            {
                MessageBox.Show("영상을 찾지 못합니다");
                return;
            }
            cvBufferCam[1] = new IplImage(filename, LoadMode.GrayScale);
            bDemo = true;
            nDemo = 2;
            wtCheckCamera2();
            wtCheckResult();
            bDemo = false;
            pictureBoxCam2.Image = iu.resizeImage(cvBufferCam[1].ToBitmap(), pictureBoxCam2.Size);
            pictureBoxResult2.Image = boundaryTest[0].ResultBitmapThumb;
            pictureBoxCam2.Refresh();
            pictureBoxResult2.Refresh();
        }
        int nDemo = 0;
        private void pictureBoxCam2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void dataGridViewFinish_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (bInFinish == true)
            {
                if (dataGridViewFinish.Columns[e.ColumnIndex].Name == "COUNT_ERROR")
                {
                    DataRow drFinish = dataSetFinish.Tables[0].Rows[0];
                    int countNg = (int)drFinish["COUNT_EMPTY1"] + (int)drFinish["COUNT_EMPTY2"] + (int)drFinish["COUNT_EDGE_BUBBLE"] + (int)drFinish["COUNT_DEFECT"] + (int)drFinish["COUNT_DK"] + (int)drFinish["COUNT_SIL"] + (int)drFinish["COUNT_CT"] + (int)drFinish["COUNT_PW"] + (int)drFinish["COUNT_UPDOWN"] + (int)drFinish["COUNT_ERROR"];
                    drFinish["COUNT_NG"] = countNg;
                }
            }
        }
        #region EES_MODULE
        //private bool GetSmInspectDefect(string lotNumber, out int PR_QTY, out int NG_BUBBLE_QTY, out int NG_REACTION_QTY, out int NG_PRINT_QTY, out int NG_SEPARATION_QTY)
        //{
        //    PR_QTY = 0;
        //    NG_BUBBLE_QTY = 0;
        //    NG_REACTION_QTY = 0;
        //    NG_PRINT_QTY = 0;
        //    NG_SEPARATION_QTY = 0;
        //    try
        //    {
        //        ///////////////////////////////////////////////////////////////////////////////////////////////
        //        string sql = dataSetBackup.Tables["sql"].Rows[0]["SmInspectDefect"].ToString() + " '" + lotNumber + "'";
        //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + sql));
        //        Class_ERPDB db = new Class_ERPDB();
        //        db.ConnectDB(strConnectionEES);
        //        DataTable dt = db.GetDBtable(sql);
        //        db.CloseDB();
        //        dt.TableName = "SmInspectDefect";
        //        ///////////////////////////////////////////////////////////////////////////////////////////////
        //        dt.WriteXml(m_settingDir + @"\SmInspectDefect.xml", XmlWriteMode.WriteSchema);
        //        DataRow drErp = dt.Rows[0];
        //        PR_QTY = (int)(double.Parse(drErp["PR_QTY"].ToString()) + 0.5);
        //        NG_BUBBLE_QTY = (int)(double.Parse(drErp["NG_BUBBLE_QTY"].ToString()) + 0.5);
        //        NG_REACTION_QTY = (int)(double.Parse(drErp["NG_REACTION_QTY"].ToString()) + 0.5);
        //        NG_PRINT_QTY = (int)(double.Parse(drErp["NG_PRINT_QTY"].ToString()) + 0.5);
        //        NG_SEPARATION_QTY = (int)(double.Parse(drErp["NG_SEPARATION_QTY"].ToString()) + 0.5);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex.Message));
        //        MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
        //        return false;
        //    }
        //}
        private bool GetUseUV(string lotNumber, out bool bUV)
        {
            bUV = true;
#if DEBUG
            return true;
#else
            try
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                string sql = "SELECT GD_NM, POP_MATE_NO FROM PRTR1120 WHERE LOT_NO LIKE '" + lotNumber + "' AND GONG_CD ='10'";
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + sql));
                Class_ERPDB db = new Class_ERPDB();
                db.ConnectDB(strConnectionERP);
                DataTable dt = db.GetDBtable(sql);
                db.CloseDB();
                dt.TableName = "test";
                ///////////////////////////////////////////////////////////////////////////////////////////////
                dt.WriteXml(m_settingDir + @"\GetUV.xml", XmlWriteMode.WriteSchema);
                DataRow drErp = dt.Rows[0];
                string pop_mate_no = drErp["POP_MATE_NO"].ToString();
                if (pop_mate_no.Contains("BMF")) bUV = false;
                if (pop_mate_no.Contains("SHD")) bUV = false;
                if (pop_mate_no.Contains("MTCF")) bUV = false;
                if (pop_mate_no.Contains("PTCF")) bUV = false;
                return true;
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex.Message));
                MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
                return false;
            }
#endif
        }
        private bool GetSachulSaengSanSilJuk(string lotNumber, bool bFirst = true)
        {
#if DEBUG
            return true;
#else
            try
            {
                DataRow drFinish = dataSetFinish.Tables[0].Rows[0];
                // 1
                ///////////////////////////////////////////////////////////////////////////////////////////////
                string sql = "SELECT A.JOB_NO,A.JOB_DT,A.GD_CD,B.GD_NM,B.SPEC,B.UNIT_CD,C.JOB_SEQ,C.GONG_CD,C.WA_CD,C.WA_GU,C.MC_CD,C.JOB_QTY,A.LOT_NO " +
                    "FROM PRTR1110 A JOIN PRTR1111 C ON A.JOB_NO = C.JOB_NO LEFT JOIN COMT1200 B ON A.GD_CD = B.GD_CD " +
                    "WHERE 1 = 1 AND ISNULL(C.CLOSE_YN,'0') <> '1' AND NOT EXISTS(SELECT 'NOT FOUND' FROM PRTR1120 WHERE STTS <> 'D' AND C.JOB_NO = JOB_NO AND C.JOB_SEQ = JOB_SEQ) AND A.JOB_GU = '03' AND A.JOB_NO LIKE '%' + '' + '%' " +
                    "AND(A.GD_CD LIKE '%' + '' + '%' OR B.GD_NM LIKE '%' + '' + '%') AND A.STTS = 'C' AND C.MC_CD LIKE '' + '%' AND A.LOT_NO = '" + lotNumber + "' ORDER BY C.JOB_NO,C.JOB_SEQ";
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + sql));
                Class_ERPDB db = new Class_ERPDB();
                db.ConnectDB(strConnectionERP);
                DataTable dt = db.GetDBtable(sql);
                db.CloseDB();
                dt.TableName = "test";
                ///////////////////////////////////////////////////////////////////////////////////////////////
                dt.WriteXml(m_settingDir + @"\GetSachulSaengSanSilJuk.xml", XmlWriteMode.WriteSchema);
                string MC_CD = "B" + ((int)drSystem["machineCode"]).ToString("000");
                try
                {
                    DataRow drErp = dt.Rows[0];
                    drFinish["mcName"] = MC_CD;
                    drFinish["checksheetNumber"] = lotNumber;
                    drFinish["GDCD"] = drErp["GD_CD"].ToString();
                    drFinish["GDNM"] = drErp["GD_NM"].ToString();
                    drFinish["POWER"] = drErp["SPEC"].ToString();
                    drFinish["PR_QTY"] = (int)(double.Parse(drErp["JOB_QTY"].ToString()) + 0.5);
                    drFinish["JOB_NO"] = drErp["JOB_NO"].ToString();
                    dataSetFinish.Tables[0].WriteXml(m_settingDir + @"\finish.xml", XmlWriteMode.WriteSchema);
                    return true;
                }
                catch (Exception ex)
                {
                    if (bFirst == false)
                    {
                        string msgError = "생산지시를 만들었으나 읽어오지 못했습니다.";
                        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, msgError));
                        MessageBox.Show(msgError, MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP 생산지시 읽기 실패 [첫번째 조건 (지시 정보 확인)] => " + ex.Message));
                    if (CheckSachulSiljuk(lotNumber, out string NEW_GDCD, out string PR_QTY)) // 2
                    {
                        if (checkBoxErp1110_Off.Checked == false)
                        {
                            if (GetProjectNo(out string JOB_NO)) // 3
                            {
                                if (MakeQCode(NEW_GDCD, out string Q_CODE)) // 4
                                {
                                    if (MakeJiSiMaster(JOB_NO, drFinish["PS_CD"].ToString(), Q_CODE, PR_QTY, lotNumber)) // 5
                                    {
                                        if (MakeJiSiSangSe(JOB_NO, Q_CODE, PR_QTY, MC_CD, lotNumber)) // 6
                                        {
                                            return GetSachulSaengSanSilJuk(lotNumber, false);
                                        }
                                        else
                                        {
                                            string msgError = "지시 상세 테이블 정보 입력 실패";
                                            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, msgError));
                                            MessageBox.Show(msgError, MethodBase.GetCurrentMethod().Name);
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        string msgError = "지시 마스터 테이블에 정보 입력 실패";
                                        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, msgError));
                                        MessageBox.Show(msgError, MethodBase.GetCurrentMethod().Name);
                                        return false;
                                    }
                                }
                                else
                                {
                                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "Q CODE 발행"));
                                    return false;
                                }
                            }
                            else
                            {
                                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "지시 번호 수령 실패"));
                                return false;
                            }
                        }
                        else
                        {
                            string msgError = "분리 지시가 조회되지 않습니다. 분리 지시를 확인하세요.";
                            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, msgError));
                            MessageBox.Show(msgError, MethodBase.GetCurrentMethod().Name);
                            return false;
                        }
                    }
                    else
                    {
                        string msgError = "사출 실적이 조회되지 않습니다. 사출 실적을 확인하세요.";
                        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, msgError));
                        MessageBox.Show(msgError, MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex.Message));
                MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
                return false;
            }
#endif
        }
        bool MakeJiSiMaster(string JOB_NO, string PS_CD, string Q_CODE, string PR_QTY, string LOT_NO)
        {
            try
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                string DATETIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string sql = "INSERT INTO PRTR1110 (JOB_NO, JOB_DT, FROM_DT, TO_DT, PS_CD, REMK,  GD_CD, JOB_QTY, IN_DT, AC_DT, CL_DT, STTS, JOB_GU, LOT_NO, SA_CD, JOB_TYPE, DAYTIME_GU) " +
                    "VALUES('" + JOB_NO + "', '" + DATETIME + "', '" + DATETIME + "', '" + DATETIME + "', '" + PS_CD + "', '', '" + Q_CODE + "', '" + PR_QTY + "', '" + DATETIME + "',  '" + DATETIME + "',  '" + DATETIME + "', 'C', '03', '" + LOT_NO + "', '01', 'PR0011', 'PR0061')";
                //                    +                    "FROM PRTR1120 WHERE GONG_CD = '10' AND STTS = 'C' AND LOT_NO ='"+ LOT_NO + "'";
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + sql));
                return WriteDB(strConnectionERP, sql, true, out string msg);
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex.Message));
                return false;
            }
        }
        bool MakeJiSiSangSe(string JOB_NO, string Q_CODE, string PR_QTY, string MC_CD, string lotNumber)
        {
            try
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                string DATETIME = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string sql = "INSERT INTO PRTR1111 (JOB_NO, JOB_SEQ, GD_CD, GONG_CD, FROM_DT, TO_DT, JOB_QTY, WA_CD, MC_CD, WA_GU) " +
                    "VALUES('" + JOB_NO + "', 1, '" + Q_CODE + "', 20, '" + DATETIME + "', '" + DATETIME + "', '" + PR_QTY + "', '10', '" + MC_CD + "', 'I')";
                //+ "FROM PRTR1120 WHERE GONG_CD='10' AND STTS='C' AND LOT_NO ='" + lotNumber +"'";
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + sql));
                return WriteDB(strConnectionERP, sql, true, out string msg);
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex.Message));
                return false;
            }
        }
        bool MakeQCode(string NEW_GDCD, out string QCode)
        {
            QCode = "";
            try
            {
                QCode = "Q" + NEW_GDCD.Substring(1);
                return true;
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex.Message));
                return false;
            }
        }
        bool CheckSachulSiljuk(string lotNumber, out string NEW_GDCD, out string PR_QTY)
        {
            NEW_GDCD = "";
            PR_QTY = "";
            try
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                string sql = "SELECT * FROM PRTR1120 WHERE GONG_CD = '10' AND STTS ='C' AND LOT_NO='" + lotNumber + "'";
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + sql));
                Class_ERPDB db = new Class_ERPDB();
                db.ConnectDB(strConnectionERP);
                DataTable dt = db.GetDBtable(sql);
                db.CloseDB();
                dt.TableName = "test";
                ///////////////////////////////////////////////////////////////////////////////////////////////
                dt.WriteXml(m_settingDir + @"\CheckSachulSiljuk.xml", XmlWriteMode.WriteSchema);
                if (dt.Rows.Count == 0)
                {
                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "사출실적 없음"));
                    return false;
                }
                NEW_GDCD = dt.Rows[0]["NEW_GDCD"].ToString();
                PR_QTY = dt.Rows[0]["PR_QTY"].ToString();
                return true;
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex.Message));
                return false;
            }
        }
        private void GetNM_PW(string lotNumber, out string NM, out string PW)
        {
            NM = "";
            PW = "";
#if DEBUG
            NM = "PIA_MS1 Dazzle Beige_2T";
            PW = "0.00";
#else
            try
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                string sql = "exec wi_pr1122_print '" + lotNumber + "' ";
                Class_ERPDB db = new Class_ERPDB();
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + sql));
                db.ConnectDB(strConnectionERP);
                DataTable dt = db.GetDBtable(sql);
                db.CloseDB();
                ///////////////////////////////////////////////////////////////////////////////////////////////
                try
                {
                    NM = dt.Rows[0]["gdnm"].ToString();
                    PW = dt.Rows[0]["SP"].ToString();
                }
                catch
                {
                    NM = "none";
                    PW = "none";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
            }
#endif
            labelNM_PW.Text = NM + " / " + PW;
        }
        private bool GetProjectNo(out string PR_NO)
        {
            PR_NO = "";
#if DEBUG
            PR_NO = "testPR_NO";
            return true;
#else
            try
            {
                //string sql = "SELECT DBO.fnCodeNo('ERP_TB_PRTR1123', getdate())";
                string sql = dataSetBackup.Tables["sql"].Rows[0]["GetProjectNo1"].ToString();
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + sql));
                Class_ERPDB db = new Class_ERPDB();
                //db.ConnectDB(strConnectionEES);
                db.ConnectDB(strConnectionERP);
                DataTable dt = db.GetDBtable(sql);
                db.CloseDB();
                ///////////////////////////////////////////////////////////////////////////////////////////////
                try
                {
                    PR_NO = dt.Rows[0][0].ToString();
                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "PR_NO = " + PR_NO));
                    return true;
                }
                catch
                {
                    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ESS PR_NO 읽기 실패"));
                    MessageBox.Show("ESS PR_NO 읽기 실패", MethodBase.GetCurrentMethod().Name);
                    PR_NO = "none";
                    return false;
                }
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ESS PR_NO 읽기 실패"));
                MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
                return false;
            }
#endif
        }
        private bool InsertPRTR1120()
        {
            try
            {
                DataRow drFinish = dataSetFinish.Tables[0].Rows[0];
                DateTime now = DateTime.Now;
                if (checkBoxErpOff.Checked)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "checkBoxErpOff.Checked = True");
                }
                else
                {  // 실적 생성하기 (PRTR1120)
                    string query = "insert into PRTR1120 (PR_NO, LOT_NO, PR_DT, IN_DT, FROM_DT, FROM_TIME, MC_CD, GONG_CD, WA_CD, JOB_QTY, PS_CD, NEW_GDCD, PR_JOBNO, JOB_NO, JOB_SEQ, SA_CD, FWH_CD, TWH_CD) values("
                        + "'" + drFinish["PR_NO"] + "'" // PR_NO
                        + ",'" + drFinish["checksheetNumber"] + "'" // LOT_NO
                        + ",'" + now.ToString("yyyy-MM-dd 00:00:00") + "'" // PR_DT
                        + ",'" + now.ToString("yyyy-MM-dd HH:mm:ss") + "'" // IN_DT
                        + ",'" + now.ToString("yyyy-MM-dd HH:mm:ss") + "'" // FROM_DT
                        + ",'" + now.ToString("HH:mm") + "'" // FROM_TIME
                        + ",'" + drFinish["mcName"] + "'" // MC_CD
                        + ",'" + "20" + "'" // GONG_CD
                        + ",'" + "10" + "'" // WA_CD
                        + ",'" + drFinish["PR_QTY"] + "'" // JOB_QTY
                        + ",'" + drFinish["PS_CD"] + "'" // PS_CD
                        + ",'" + drFinish["GDCD"] + "'" // NEW_GDCD
                        + ",'" + drFinish["JOB_NO"] + "'" // PR_JOBNO
                        + ",'" + drFinish["JOB_NO"] + "'" //JOB_NO
                        + ",'1','01','Q001','R001')"; //JOB_SEQ, SA_CD, FWH_CD, TWH_CD
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query);
                    if (WriteDB(strConnectionERP, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "ERP PRTR1120 업로드 실패");
                        MessageBox.Show("ERP PRTR1120 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                    //return true; 
                }
                if (checkBoxEesOff.Checked)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "checkBoxEesOff.Checked = True");
                    return true;
                }
                else
                {  // 실적 생성하기 (PRTR1120)
                    string query = "insert into ERP_TB_PRTR1120 (PR_NO, LOT_NO, PR_DT, IN_DT, FROM_DT, FROM_TIME, MC_CD, GONG_CD, WA_CD, JOB_QTY, PS_CD, NEW_GDCD, PR_JOBNO, JOB_NO, GD_CD, STTS, JOB_SEQ, SA_CD, FWH_CD, TWH_CD) values("
                        + "'" + drFinish["PR_NO"] + "'" // PR_NO
                        + ",'" + drFinish["checksheetNumber"] + "'" // LOT_NO
                        + ",'" + now.ToString("yyyy-MM-dd HH:mm:ss") + "'" // PR_DT
                        + ",'" + now.ToString("yyyy-MM-dd HH:mm:ss") + "'" // IN_DT
                        + ",'" + now.ToString("yyyy-MM-dd HH:mm:ss") + "'" // FROM_DT
                        + ",'" + now.ToString("HH:mm") + "'" // FROM_TIME
                        + ",'" + drFinish["mcName"] + "'" // MC_CD
                        + ",'" + "20" + "'" // GONG_CD
                        + ",'" + "10" + "'" // WA_CD
                        + ",'" + drFinish["PR_QTY"] + "'" // JOB_QTY
                        + ",'" + drFinish["PS_CD"] + "'" // PS_CD
                        + ",'" + drFinish["GDCD"] + "'" // NEW_GDCD
                        + ",'" + drFinish["JOB_NO"] + "'" // PR_JOBNO
                        + ",'" + drFinish["JOB_NO"] + "'" //JOB_NO
                        + ",'" + drFinish["GDCD"] + "'" // GDCD
                        + ",'" + "S" + "'" //JOB_NO
                        + ",'1','01','Q001','R001')"; //JOB_SEQ, SA_CD, FWH_CD, TWH_CD
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1120 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1120 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
                return false;
            }
        }
        private void UpdateErp()
        {
            //DateTime now = DateTime.Now;
            //DataRow drFinish = dataSetFinish.Tables[0].Rows[0];
            //{
            //    string query = "EXEC WI_PR1123 '" + drFinish["PR_NO"] + "', 'C', '11217'";
            //    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //    if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //    {
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP EXEC WI_PR1123 실패"));
            //        MessageBox.Show("ERP EXEC WI_PR1123 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //    }
            //}
            //if (checkBoxErpOff.Checked)
            //{
            //    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "checkBoxErpOff.Checked = True"));
            //    return;
            //}
            //{ // Update PRTR1120
            //    string query = "UPDATE PRTR1120 SET "
            //        //+ "PR_QTY='" + drFinish["PR_QTY"].ToString() + "'," //양품수량
            //        + "PR_QTY='" + drFinish["COUNT_OK"].ToString() + "'," //양품수량
            //        + "NG_QTY='" + drFinish["COUNT_NG"].ToString() + "'," //불량총수량
            //        + "TO_DT='" + now.ToString("yyyy-MM-dd HH:mm:ss") + "'," //종료일 및 시간
            //        + "TO_TIME='" + now.ToString("HH:mm") + "'," //종료시분
            //        + "GD_CD='" + drFinish["GDCD"].ToString() + "',"
            //        + "STTS='" + "S" + "'"
            //        + " WHERE PR_NO='" + drFinish["PR_NO"].ToString() + "'";
            //    logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //    if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //    {
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1120 업데이트 실패"));
            //        MessageBox.Show("ERP PRTR1120 업데이트 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        return;
            //    }
            //    else
            //    {
            //        drSystem["lastChecksheetNumber"] = drFinish["checksheetNumber"].ToString();
            //    }
            //}

            //{ // Insert PRTR1121
            //    int ErrorIndex = 26;
            //    if ((int)drFinish["COUNT_PW"] > 0)//파워
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E002" + "'" // NG_CD
            //        + ",'" + drFinish["COUNT_PW"].ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    if ((int)drFinish["COUNT_CT"] > 0)//CT
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E003" + "'" // NG_CD
            //        + ",'" + drFinish["COUNT_CT"].ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    if ((int)drFinish["COUNT_NONE"] > 0)//미분리
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E007" + "'" // NG_CD
            //        //+ ",'" + drFinish["COUNT_NONE"].ToString() + "'" // NG_QTY
            //        + ",'" + drFinish["COUNT_EMPTY1"].ToString() + "'" // COUNT_EMPTY1
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    if ((int)drFinish["COUNT_EDGE_BUBBLE"] > 0)//에지기포
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E008" + "'" // NG_CD
            //        + ",'" + drFinish["COUNT_EDGE_BUBBLE"].ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    if ((int)drFinish["COUNT_DEFECT"] > 0)//파손
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E009" + "'" // NG_CD
            //        + ",'" + drFinish["COUNT_DEFECT"].ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    if ((int)drFinish["COUNT_DK"] > 0)//뜯김
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E010" + "'" // NG_CD
            //        + ",'" + drFinish["COUNT_DK"].ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    if ((int)drFinish["COUNT_SIL"] > 0)//실
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E011" + "'" // NG_CD
            //        + ",'" + drFinish["COUNT_SIL"].ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    if ((int)drFinish["COUNT_UPDOWN"] > 0)//상하분리
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E012" + "'" // NG_CD
            //        + ",'" + drFinish["COUNT_UPDOWN"].ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    //int yousilCount = (int)drFinish["COUNT_EMPTY1"] + (int)drFinish["COUNT_EMPTY2"];// + (int)drFinish["COUNT_ERROR"];
            //    int yousilCount = (int)drFinish["COUNT_EMPTY2"];// + (int)drFinish["COUNT_ERROR"];
            //    if (yousilCount > 0)//유실
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E013" + "'" // NG_CD
            //        + ",'" + yousilCount.ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //    int errorCount = (int)drFinish["COUNT_ERROR"];
            //    if (errorCount > 0)//전공정 오차
            //    {
            //        string query = "insert into PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
            //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
            //        + ",'" + ErrorIndex + "'" // PR_SEQ
            //        + ",'" + "E021" + "'" // NG_CD
            //        + ",'" + errorCount.ToString() + "'" // NG_QTY
            //        + ",'" + "" + "')"; //REMK
            //        logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "strConnectionERP => " + query));
            //        if (WriteDB(strConnectionERP, query, true, out string msg) == false)
            //        {
            //            logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, "ERP PRTR1121 업로드 실패"));
            //            MessageBox.Show("ERP PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
            //        }
            //        ErrorIndex++;
            //    }
            //}
        }
        private void SendLogToEES()
        {
            if (checkBoxEesOff.Checked)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "checkBoxEesOff.Checked = True");
                return;
            }
            DateTime now = DateTime.Now;
            DataRow drFinish = dataSetFinish.Tables[0].Rows[0];
            { // Update PRTR1120
                string query = "UPDATE ERP_TB_PRTR1120 SET "
                    //+ "PR_QTY='" + drFinish["PR_QTY"].ToString() + "'," //양품수량
                    + "PR_QTY='" + drFinish["COUNT_OK"].ToString() + "'," //양품수량
                    + "NG_QTY='" + drFinish["COUNT_NG"].ToString() + "'," //불량총수량
                    + "TO_DT='" + now.ToString("yyyy-MM-dd HH:mm:ss") + "'," //종료일 및 시간
                    + "TO_TIME='" + now.ToString("HH:mm") + "'" //종료시분
                                                                //+ "GD_CD='" + drFinish["GDCD"].ToString() + "',"
                                                                //+ "STTS='" + "S" + "'"
                    + " WHERE PR_NO='" + drFinish["PR_NO"].ToString() + "'";
                logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1120 업데이트 실패");
                    MessageBox.Show("EES ERP_TB_PRTR1120 업데이트 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    return;
                }
                else
                {
                    drSystem["lastChecksheetNumber"] = drFinish["checksheetNumber"].ToString();
                }
            }

            { // Insert PRTR1121
                int ErrorIndex = 26;
                if ((int)drFinish["COUNT_PW"] != 0)//파워
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E002" + "'" // NG_CD
                    + ",'" + drFinish["COUNT_PW"].ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                if ((int)drFinish["COUNT_CT"] != 0)//CT
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E003" + "'" // NG_CD
                    + ",'" + drFinish["COUNT_CT"].ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                if ((int)drFinish["COUNT_EMPTY1"] != 0)//미분리
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E007" + "'" // NG_CD
                    //+ ",'" + drFinish["COUNT_NONE"].ToString() + "'" // NG_QTY
                    + ",'" + drFinish["COUNT_EMPTY1"].ToString() + "'" // COUNT_EMPTY1
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                if ((int)drFinish["COUNT_EDGE_BUBBLE"] != 0)//에지기포
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E008" + "'" // NG_CD
                    + ",'" + drFinish["COUNT_EDGE_BUBBLE"].ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                if ((int)drFinish["COUNT_DEFECT"] != 0)//파손
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E009" + "'" // NG_CD
                    + ",'" + drFinish["COUNT_DEFECT"].ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                if ((int)drFinish["COUNT_DK"] != 0)//뜯김
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E010" + "'" // NG_CD
                    + ",'" + drFinish["COUNT_DK"].ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                if ((int)drFinish["COUNT_SIL"] != 0)//실
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E011" + "'" // NG_CD
                    + ",'" + drFinish["COUNT_SIL"].ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                if ((int)drFinish["COUNT_UPDOWN"] != 0)//상하분리
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E012" + "'" // NG_CD
                    + ",'" + drFinish["COUNT_UPDOWN"].ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                //int yousilCount = (int)drFinish["COUNT_EMPTY1"] + (int)drFinish["COUNT_EMPTY2"];// + (int)drFinish["COUNT_ERROR"];
                int yousilCount = (int)drFinish["COUNT_EMPTY2"];// + (int)drFinish["COUNT_ERROR"];
                if (yousilCount != 0)//유실
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E013" + "'" // NG_CD
                    + ",'" + yousilCount.ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                int errorCount = (int)drFinish["COUNT_ERROR"];
                if (errorCount != 0)//전공정 오차
                {
                    string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                    + "'" + drFinish["PR_NO"] + "'" // PR_NO
                    + ",'" + ErrorIndex + "'" // PR_SEQ
                    + ",'" + "E021" + "'" // NG_CD
                    + ",'" + errorCount.ToString() + "'" // NG_QTY
                    + ",'" + "" + "')"; //REMK
                    logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                    if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                    {
                        logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                        MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                    }
                    ErrorIndex++;
                }
                //if (!checkBoxSmInspectDefectOff.Checked)
                //{
                //    int NG_BUBBLE_QTY = (int)drFinish["NG_BUBBLE_QTY"];
                //    if (NG_BUBBLE_QTY != 0)//NG_BUBBLE_QTY
                //    {
                //        string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
                //        + ",'" + ErrorIndex + "'" // PR_SEQ
                //        + ",'" + "E025" + "'" // NG_BUBBLE_QTY
                //        + ",'" + NG_BUBBLE_QTY.ToString() + "'" // NG_QTY
                //        + ",'" + "" + "')"; //REMK
                //        logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                //        if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                //        {
                //            logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                //            MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                //        }
                //        ErrorIndex++;
                //    }
                //    int NG_REACTION_QTY = (int)drFinish["NG_REACTION_QTY"];
                //    if (NG_REACTION_QTY != 0)//NG_REACTION_QTY
                //    {
                //        string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
                //        + ",'" + ErrorIndex + "'" // PR_SEQ
                //        + ",'" + "E026" + "'" // NG_REACTION_QTY
                //        + ",'" + NG_REACTION_QTY.ToString() + "'" // NG_QTY
                //        + ",'" + "" + "')"; //REMK
                //        logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                //        if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                //        {
                //            logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                //            MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                //        }
                //        ErrorIndex++;
                //    }
                //    int NG_PRINT_QTY = (int)drFinish["NG_PRINT_QTY"];
                //    if (NG_PRINT_QTY != 0)//NG_PRINT_QTY
                //    {
                //        string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
                //        + ",'" + ErrorIndex + "'" // PR_SEQ
                //        + ",'" + "E027" + "'" // NG_PRINT_QTY
                //        + ",'" + NG_PRINT_QTY.ToString() + "'" // NG_QTY
                //        + ",'" + "" + "')"; //REMK
                //        logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                //        if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                //        {
                //            logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                //            MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                //        }
                //        ErrorIndex++;
                //    }
                //    int NG_SEPARATION_QTY = (int)drFinish["NG_SEPARATION_QTY"];
                //    if (NG_SEPARATION_QTY != 0)//NG_SEPARATION_QTY
                //    {
                //        string query = "insert into ERP_TB_PRTR1121 (PR_NO, PR_SEQ, NG_CD, NG_QTY, REMK) values("
                //        + "'" + drFinish["PR_NO"] + "'" // PR_NO
                //        + ",'" + ErrorIndex + "'" // PR_SEQ
                //        + ",'" + "E028" + "'" // NG_SEPARATION_QTY
                //        + ",'" + NG_SEPARATION_QTY.ToString() + "'" // NG_QTY
                //        + ",'" + "" + "')"; //REMK
                //        logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + query);
                //        if (WriteDB(strConnectionEES, query, true, out string msg) == false)
                //        {
                //            logPrint(MethodBase.GetCurrentMethod().Name, "EES ERP_TB_PRTR1121 업로드 실패");
                //            MessageBox.Show("EES ERP_TB_PRTR1121 업로드 실패\n\n" + msg, MethodBase.GetCurrentMethod().Name);
                //        }
                //        ErrorIndex++;
                //    }
                //}
            }
        }
        private void GetAccessLevel(string PS_CD, out string PS_NM, out string PS_LEVEL)
        {
            PS_NM = "";
            PS_LEVEL = "";
#if DEBUG
            PS_NM = "TEST";
            PS_LEVEL = "ADMIN";
#else
            try
            {
                string sql = "SELECT A.USER_NAME, A.USER_LEVEL FROM COM_TB_MACHINE_ACCESS_USER A WHERE USER_ID ='" + PS_CD + "'";
                logPrint(MethodBase.GetCurrentMethod().Name, "strConnectionEES => " + sql);
                Class_ERPDB db = new Class_ERPDB();
                db.ConnectDB(strConnectionEES);
                DataTable dt = db.GetDBtable(sql);
                db.CloseDB();
                ///////////////////////////////////////////////////////////////////////////////////////////////
                try
                {
                    PS_NM = dt.Rows[0]["USER_NAME"].ToString();
                    PS_LEVEL = dt.Rows[0]["USER_LEVEL"].ToString();
                    logPrint(MethodBase.GetCurrentMethod().Name, "USER_NAME = " + PS_NM + ", USER_LEVEL = " + PS_LEVEL);
                }
                catch
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, "ESS USER_LEVEL 읽기 실패");
                    MessageBox.Show("ESS USER_LEVEL 읽기 실패", MethodBase.GetCurrentMethod().Name);
                    PS_NM = "Not Found";
                    PS_LEVEL = "Not Found";
                }
            }
            catch (Exception ex)
            {
                logPrint(MethodBase.GetCurrentMethod().Name, "ESS USER_LEVEL 읽기 실패");
                MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
                PS_NM = "Not Found";
                PS_LEVEL = "Not Found";
            }
#endif
        }
        string currentAccessLevel = "NONE";
        string currentAccessName = "NONE";
        private void logPrintAccess(string where, string msg1, string msg2 = "")
        {
            DateTime now = DateTime.Now;
            string path = m_settingDir + @"\logAccess\" + now.ToString("yyyy-MM");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            using (FileStream fs = File.Open(path + @"\logAccess-" + now.ToString("yyyy-MM-dd") + ".log", FileMode.Append))
            {
                using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                {
                    if (msg2 == "")
                    {
                        w.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss") + ", " + msg1.Replace("\r", " ").Replace("\n", " ") + ", " + currentAccessName + ", " + currentAccessLevel + ", " + where);
                    }
                    else
                    {
                        w.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss") + ", " + msg1.Replace("\r", " ").Replace("\n", " ") + "=" + msg2 + ", " + currentAccessName + ", " + currentAccessLevel + ", " + where);
                    }
                }
            }
        }
        private bool WriteDB(string strConnection, string query, bool bReadUncommitted, out string msg)
        {
            msg = "";
            for (int i = 0; i < 10; i++)
            {
                if (WriteDB_Tranjaction(strConnection, query, bReadUncommitted, out msg))
                {
                    logPrint(MethodBase.GetCurrentMethod().Name, msg + " => " + query);
                    return true;
                }
                Thread.Sleep(100);
            }
            logPrint(MethodBase.GetCurrentMethod().Name, msg + " => " + query);
            return false;
        }
        private bool WriteDB_Tranjaction(string strConnection, string query, bool bReadUncommitted, out string msg)
        {
            bool ret = false;
            msg = "";
            using (SqlConnection connection = new SqlConnection(strConnection))
            {
                try
                {
                    connection.Open();
                    //lock (erpLock)
                    {
                        SqlTransaction sqlTran = null;
                        if (bReadUncommitted)
                            sqlTran = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                        else
                            sqlTran = connection.BeginTransaction();
                        SqlCommand command = connection.CreateCommand();
                        command.Transaction = sqlTran;
                        command.CommandText = query;
                        command.ExecuteNonQuery();
                        try
                        {
                            sqlTran.Commit();
                            ret = true;
                        }
                        catch (Exception ex)
                        {
                            msg = ex.Message;
                            try
                            {
                                sqlTran.Rollback();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                }
                finally
                {
                    connection.Close();
                }
            }
            return ret;
        }
        #endregion EES_MODULE

        private void buttonNewRecipe_Click(object sender, EventArgs e)
        {
            try
            {
                logPrintAccess(MethodBase.GetCurrentMethod().Name, ((Control)sender).Text, "Click");
                newFileDialog1.InitialDirectory = m_settingDir;
                if (newFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    CurrentFileName = newFileDialog1.FileName;

                    saveParameter(CurrentFileName, "centerParameter");
                    saveParameter(CurrentFileName + "_b", "boundaryParameter");
                    labelRecipe.Text = Path.GetFileNameWithoutExtension(CurrentFileName);
                    bJobChange = true;
                    bWorkDone = false;
                }
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex));
            }
        }

        private void buttonLoadRecipe_Click(object sender, EventArgs e)
        {
            try
            {
                logPrintAccess(MethodBase.GetCurrentMethod().Name, ((Control)sender).Text, "Click");
                openFileDialog1.InitialDirectory = m_settingDir;
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    //내부 파라미터 읽기
                    CurrentFileName = openFileDialog1.FileName;
                    loadParameter(openFileDialog1.FileName, "centerParameter");
                    //테두리 파라미터 읽기
                    loadParameter(openFileDialog1.FileName + "_b", "boundaryParameter");
                    labelRecipe.Text = Path.GetFileNameWithoutExtension(CurrentFileName);
                    try
                    {
                        openPowerRecipeFileDialog.InitialDirectory = m_settingDir;
                        if (openPowerRecipeFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            string filename = openPowerRecipeFileDialog.FileName;
                            dataSetPowerRecipe.Clear();
                            dataSetPowerRecipe.ReadXml(filename);
                            PowerToRecipe();
                            labelRecipePower.Text = Path.GetFileNameWithoutExtension(filename);
                            bJobChange = true;
                            bWorkDone = false;
                        }
                        else
                        {
                            labelRecipePower.Text = "Power Recipe 로드 되지 않음";
                        }
                    }
                    catch
                    {
                        labelRecipePower.Text = "Power Recipe 로드 되지 않음";
                    }
                }
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex));
            }
        }

        private void buttonSaveRecipe_Click(object sender, EventArgs e)
        {
            logPrintAccess(MethodBase.GetCurrentMethod().Name, ((Control)sender).Text, "Click");
            if (bJobChange == false)
            {
                MessageBox.Show("레시피 파일이 로드되지 않았습니다. \n 파일을 새로 생성하거나 불러오기를 하세요.");
                return;
            }
            //내부 파라미터 Apply 및 저장 기능 동시에
            DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
            try
            {
                dr["s1Start"] = int.Parse(textBoxCenterS1Start.Text);
                dr["s2Start"] = int.Parse(textBoxCenterS2Start.Text);
                dr["s3Start"] = int.Parse(textBoxCenterS3Start.Text);
                dr["s1Count"] = int.Parse(textBoxCenterS1Count.Text);
                dr["s2Count"] = int.Parse(textBoxCenterS2Count.Text);
                dr["s3Count"] = int.Parse(textBoxCenterS3Count.Text);

                //dr["s1Start2"] = int.Parse(textBoxCenter2S1Start.Text);
                //dr["s1Count2"] = int.Parse(textBoxCenter2S1Count.Text);
                //dr["s2Start2"] = int.Parse(textBoxCenter2S2Start.Text);
                //dr["s2Count2"] = int.Parse(textBoxCenter2S2Count.Text);
                //dr["s3Start2"] = int.Parse(textBoxCenter2S3Start.Text);
                //dr["s3Count2"] = int.Parse(textBoxCenter2S3Count.Text);
                //dataSetBackup.WriteXml(m_settingDir + @"\backup.xml", XmlWriteMode.WriteSchema);
            }
            catch
            {
                textBoxCenterS1Start.Text = dr["s1Start"].ToString();
                textBoxCenterS2Start.Text = dr["s2Start"].ToString();
                textBoxCenterS3Start.Text = dr["s3Start"].ToString();
                textBoxCenterS1Count.Text = dr["s1Count"].ToString();
                textBoxCenterS2Count.Text = dr["s2Count"].ToString();
                textBoxCenterS3Count.Text = dr["s3Count"].ToString();

                //textBoxCenter2S1Start.Text = dr["s1Start2"].ToString();
                //textBoxCenter2S1Count.Text = dr["s1Count2"].ToString();
                //textBoxCenter2S2Start.Text = dr["s2Start2"].ToString();
                //textBoxCenter2S2Count.Text = dr["s2Count2"].ToString();
                //textBoxCenter2S3Start.Text = dr["s3Start2"].ToString();
                //textBoxCenter2S3Count.Text = dr["s3Count2"].ToString();
            }

            //테두리 파라미터 Apply 및 저장 기능 동시에

            dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
            try
            {
                dr["s1Start"] = int.Parse(textBoxBoundaryS1Start.Text);
                dr["s2Start"] = int.Parse(textBoxBoundaryS2Start.Text);
                dr["s3Start"] = int.Parse(textBoxBoundaryS3Start.Text);
                dr["s1Count"] = int.Parse(textBoxBoundaryS1Count.Text);
                dr["s2Count"] = int.Parse(textBoxBoundaryS2Count.Text);
                dr["s3Count"] = int.Parse(textBoxBoundaryS3Count.Text);

                //dr["s1Start2"] = int.Parse(textBoxBoundary2S1Start.Text);
                //dr["s2Start2"] = int.Parse(textBoxBoundary2S2Start.Text);
                //dr["s3Start2"] = int.Parse(textBoxBoundary2S3Start.Text);
                //dr["s1Count2"] = int.Parse(textBoxBoundary2S1Count.Text);
                //dr["s2Count2"] = int.Parse(textBoxBoundary2S2Count.Text);
                //dr["s3Count2"] = int.Parse(textBoxBoundary2S3Count.Text);
                dataSetBackup.WriteXml(m_settingDir + @"\backup.xml", XmlWriteMode.WriteSchema);
                MessageBox.Show("레시피 저장 완료");
            }
            catch
            {
                textBoxBoundaryS1Start.Text = dr["s1Start"].ToString();
                textBoxBoundaryS2Start.Text = dr["s2Start"].ToString();
                textBoxBoundaryS3Start.Text = dr["s3Start"].ToString();
                textBoxBoundaryS1Count.Text = dr["s1Count"].ToString();
                textBoxBoundaryS2Count.Text = dr["s2Count"].ToString();
                textBoxBoundaryS3Count.Text = dr["s3Count"].ToString();

                //textBoxBoundary2S1Start.Text = dr["s1Start2"].ToString();
                //textBoxBoundary2S2Start.Text = dr["s2Start2"].ToString();
                //textBoxBoundary2S3Start.Text = dr["s3Start2"].ToString();
                //textBoxBoundary2S1Count.Text = dr["s1Count2"].ToString();
                //textBoxBoundary2S2Count.Text = dr["s2Count2"].ToString();
                //textBoxBoundary2S3Count.Text = dr["s3Count2"].ToString();
            }

            bValueChanged = true;
            ChangeSave();
            try
            {
                newFileDialog1.InitialDirectory = m_settingDir;

                saveParameter(CurrentFileName, "centerParameter");
                saveParameter(CurrentFileName + "_b", "boundaryParameter");
                labelRecipe.Text = Path.GetFileNameWithoutExtension(CurrentFileName);
                //    saveFileDialog1.InitialDirectory = m_settingDir;
                //
                //    saveParameter(saveFileDialog1.FileName, "centerParameter");
                //    saveParameter(saveFileDialog1.FileName + "_b", "boundaryParameter");
                //    labelItemName.Text = Path.GetFileNameWithoutExtension(saveFileDialog1.FileName);
                //    LoadFileNametxt.Text = Path.GetFileNameWithoutExtension(saveFileDialog1.FileName);
            }
            catch (Exception ex)
            {
                logPrint(this, new LogArgs(MethodBase.GetCurrentMethod().Name, ex));
            }
        }

        private void buttonSavePowerRecipe_Click(object sender, EventArgs e)
        {
            logPrintAccess(MethodBase.GetCurrentMethod().Name, ((Control)sender).Text, "Click");
            savePowerFileDialog1.InitialDirectory = m_settingDir;
            if (savePowerFileDialog1.ShowDialog() == DialogResult.OK)
            {
                dataSetPowerRecipe.Clear();
                dataSetPowerRecipe.Tables[0].Rows.Add();
                RecipeToPower();
                dataSetPowerRecipe.WriteXml(savePowerFileDialog1.FileName, XmlWriteMode.WriteSchema);
            }
        }

        private void checkBoxErp1110_Off_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) ((CheckBox)sender).BackColor = Color.Red;
            else ((CheckBox)sender).BackColor = Color.Transparent;
            drSystem["bErp1110_Off"] = checkBoxErp1110_Off.Checked;
            bValueChanged = true;
            ChangeSave();
        }

        private void checkBoxErpOff_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) ((CheckBox)sender).BackColor = Color.Red;
            else ((CheckBox)sender).BackColor = Color.Transparent;
            drSystem["bErpOff"] = checkBoxErpOff.Checked;
            bValueChanged = true;
            ChangeSave();
        }

        private void checkBoxEesOff_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) ((CheckBox)sender).BackColor = Color.Red;
            else ((CheckBox)sender).BackColor = Color.Transparent;
            drSystem["bEesOff"] = checkBoxEesOff.Checked;
            bValueChanged = true;
            ChangeSave();
        }

        private void checkBoxImageServerOff_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked) ((CheckBox)sender).BackColor = Color.Red;
            else ((CheckBox)sender).BackColor = Color.Transparent;
            drSystem["bImageServer_Off"] = checkBoxImageServerOff.Checked;
            bValueChanged = true;
            ChangeSave();
        }
        private void buttonFinshLog_Click(object sender, EventArgs e)
        {
            try
            {
                logPrintAccess(MethodBase.GetCurrentMethod().Name, ((Control)sender).Text, "Click");

                //SaveLog();
                //SendLogToEES();
                //deleteResult(false);
                deleteResult();
                textBoxLotNumber.Enabled = true;
                bWorkDone = true;
                //tabControlCamParameter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, MethodBase.GetCurrentMethod().Name);
            }
        }

        private void buttonCopyTable_Click(object sender, EventArgs e)
        {
              logPrintAccess(MethodBase.GetCurrentMethod().Name, ((Control)sender).Text, "Click");
            string lottStartTime = dataSetBackup.Tables["fixedBackup"].Rows[0]["lotStartTime"].ToString();
            string lotFinishTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            CopyTable(lottStartTime, lotFinishTime);
        }

    }

    public class LuCamSnapshotCallback0 : IlucamCOMSnapshotCallback
    {
        public LuCamSnapshotCallback0(object parent)
        {
            m_parent = parent;
        }
        public void SnapshotCallback(int context, int data, uint len)
        {
            long lum = 0;

            unsafe
            {
                byte* pData = (byte*)(((IntPtr)data).ToPointer());
                for (int i = 0; i < len; i++)
                {
                    lum += pData[i];
                }

            }
            ((FormMain)m_parent).SnapshotLum = (long)(lum / len);
        }
        object m_parent;
    }
    public class LuCamSnapshotCallback1 : IlucamCOMSnapshotCallback
    {
        public LuCamSnapshotCallback1(object parent)
        {
            m_parent = parent;
        }
        public void SnapshotCallback(int context, int data, uint len)
        {
            long lum = 0;

            unsafe
            {
                byte* pData = (byte*)(((IntPtr)data).ToPointer());
                for (int i = 0; i < len; i++)
                {
                    lum += pData[i];
                }

            }
            ((FormMain)m_parent).SnapshotLum = (long)(lum / len);
        }
        object m_parent;
    }
    #region "importDll"
    internal static class SafeNativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(Int32 vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
    #endregion "importDll"
}
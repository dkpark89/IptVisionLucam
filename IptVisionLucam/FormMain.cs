using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
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
        private int cntAll = 0;
        private int cntOk = 0;
        private int cntNg = 0;
        private int cntNoLenz1 = 0;
        private int cntNoLenz2 = 0;
        private int cntOK1 = 0;
        private int cntOK2 = 0;
        private int cntBubble = 0;
        private int cntDefeat1 = 0;
        private int cntDefeat2 = 0;
        private int cntPaint = 0;
        private int cntEdge = 0;
        private int cntSil = 0;
        private int cntDK = 0;
        private int cntTop = 0;
        private int[] curNG = new int[2] { Constants.NG_NO_LENZ, Constants.NG_NO_LENZ };
        private int[] prevNG = new int[2] { Constants.NG_NO_LENZ, Constants.NG_NO_LENZ };
        private int cntDisplayNG_Bubble = 0;
        private int cntDisplayNG_Defeat = 0;
        private int cntDisplayNG_DK = 0;
        private int cntDisplayNG_Sil = 0;
        private int cntDisplayNG_None = 0;

        private int cntTotalUpload = 0;
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
        #endregion "variables"

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
                                cntOK1++;
                            }
                            else if (curNG[id] == Constants.NG_NO_LENZ)
                            {
                                cntNoLenz1++;
                            }
                            else
                            {
                                if ((curNG[id] & Constants.NG_BUBBLE) != 0) cntBubble++;
                                if ((curNG[id] & Constants.NG_DEFEAT) != 0) cntDefeat1++;
                                if ((curNG[id] & Constants.NG_EDGE) != 0) cntEdge++;
                                if ((curNG[id] & Constants.NG_PAINT) != 0) cntPaint++;
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
                    PutText(ref cvBufferCam[id], "M#=" + dataSetBackup.Tables["fixedBackup"].Rows[0]["machineCode"].ToString() + ", id=" + (id + 1).ToString() + ", cnt=" + cntAll.ToString() + ", status=" + eStatusCam[id].ToString(), true, 30, 30);
                    DataRow dr = dataSetBackup.Tables["centerParameter"].Rows[0];
                    PutText(ref cvBufferCam[id], "diffTh=" + dr["diffTh"].ToString() + "/colorIn=" + dr["colorInner"].ToString() + "/colorOut" + dr["colorOuter"].ToString(), false, 30, 60);
                    PutText(ref cvBufferCam[id], dr["colorUnderprint"].ToString() + "/" + dr["lumDiffTh"].ToString() + "/" + dr["devTh"].ToString(), false, 30, 90);
                    PutText(ref cvBufferCam[id], dr["s1Start"].ToString() + "/" + dr["s1Count"].ToString() + "/" + dr["s2Start"].ToString() + "/" + dr["s2Count"].ToString() + "/" + dr["s3Start"].ToString() + "/" + dr["s3Count"].ToString(), false, 30, 120);
                    PutText(ref cvBufferCam[id], "UV=" + centerTest[id].UvDiff1.ToString() + "/" + centerTest[id].UvDiff2.ToString(), false, 30, 150);
                    PutText(ref cvBufferCam[id], centerTest[id].C1.ToString() + "/" + centerTest[id].C2.ToString() + "/" + centerTest[id].C3.ToString(), false, 30, 180);
                    PutText(ref cvBufferCam[id], Properties.Resources.VERSION, false, 30, 210);
                    if (!bDemo)
                    {
                        cvBufferCam[id].SaveImage(m_tempDir + @"\Org1-" + (cntAll).ToString("000000") + ".jpg");
                        try
                        {
                            centerTest[id].ResultBitmap.Save(m_tempDir + @"\Res1-" + (cntAll).ToString("000000") + ".jpg");
                        }
                        catch (InvalidOperationException)
                        {
                            Thread.Sleep(100);
                            centerTest[id].ResultBitmap.Save(m_tempDir + @"\Res1-" + (cntAll).ToString("000000") + ".jpg");
                        }
                        pictureBoxCam1.InvokeIfNeeded(() => pictureBoxCam1.Image = iu.resizeImage(cvBufferCam[id], pictureBoxCam1.Size));
                        pictureBoxResult1.Tag = (int)(cntAll);
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
                    if (cntAll > 2 || bDemo)
                    {
                        curNG[id] = checkCameraBoundary(ref boundaryTest[id - 1], cvBufferCam[id], ref dataSetBackup, ref eStatusCam[id]);
                        if (!bDemo)
                        {
                            cnt[id]++;
                            lock (lockObject)
                            {
                                if (curNG[id] == 0)
                                {
                                    cntOK2++;
                                }
                                else if (curNG[id] == Constants.NG_NO_LENZ)
                                {
                                    cntNoLenz2++;
                                }
                                else
                                {
                                    if ((curNG[id] & Constants.NG_DEFEAT) != 0) cntDefeat2++;
                                    if ((curNG[id] & Constants.NG_SIL) != 0) cntSil++;
                                    if ((curNG[id] & Constants.NG_DK) != 0) cntDK++;
                                    if ((curNG[id] & Constants.NG_TOP) != 0) cntTop++;
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
                        PutText(ref cvBufferCam[id], "M#=" + dataSetBackup.Tables["fixedBackup"].Rows[0]["machineCode"].ToString() + ", id=" + (id + 1).ToString() + ", cnt=" + cntAll.ToString() + ", status=" + eStatusCam[id].ToString(), true, 30, 30);

                        DataRow dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
                        PutText(ref cvBufferCam[id], dr["cannyParameter1"].ToString() + "/" + dr["cannyParameter2"].ToString() + "/" + dr["diffTh"].ToString(), false, 30, 60);
                        PutText(ref cvBufferCam[id], dr["s1Start"].ToString() + "/" + dr["s1Count"].ToString() + "/" + dr["s2Start"].ToString() + "/" + dr["s2Count"].ToString() + "/" + dr["s3Start"].ToString() + "/" + dr["s3Count"].ToString(), false, 30, 90);
                        PutText(ref cvBufferCam[id], boundaryTest[id - 1].C1.ToString() + "/" + boundaryTest[id - 1].C2.ToString() + "/" + boundaryTest[id - 1].C3.ToString(), false, 30, 120);
                        PutText(ref cvBufferCam[id], Properties.Resources.VERSION, false, 30, 150);
                        if (!bDemo)
                        {
                            cvBufferCam[id].SaveImage(m_tempDir + @"\Org2-" + (cntAll - 2).ToString("000000") + ".jpg");
                            try
                            {
                                boundaryTest[id - 1].ResultBitmap.Save(m_tempDir + @"\Res2-" + (cntAll - 2).ToString("000000") + ".jpg");
                            }
                            catch (InvalidOperationException)
                            {
                                Thread.Sleep(100);
                                boundaryTest[id - 1].ResultBitmap.Save(m_tempDir + @"\Res2-" + (cntAll - 2).ToString("000000") + ".jpg");
                            }
                            pictureBoxResult2.Tag = (int)(cntAll - 2);
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
                int cntAllIndex = cntAll;
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
                            cntOk++;
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = "양품-" + cntOk.ToString();
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
                            cntNg++;
                            DISPLAY_FLAG DisplayFlag = DISPLAY_FLAG.INIT;
                            DataRow dr = dataSetOkNg.Tables["ng"].NewRow();
                            cell = new DataGridViewTextBoxCell();
                            cell.Value = "불량-" + cntNg.ToString();
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
                                    cntDisplayNG_None++;
                                    break;
                                case DISPLAY_FLAG.DEFEACT:
                                    cntDisplayNG_Defeat++;
                                    break;
                                case DISPLAY_FLAG.BUBBLE:
                                    cntDisplayNG_Bubble++;
                                    break;
                                case DISPLAY_FLAG.DK:
                                    cntDisplayNG_DK++;
                                    break;
                                case DISPLAY_FLAG.SIL:
                                    cntDisplayNG_Sil++;
                                    break;
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
            int divide = (cntAll == cntNoLenz1) ? 1 : cntAll - cntNoLenz1;

            string resultText = "[Total:" + (cntAll - 2).ToString()
                       + ", OK:" + cntOk.ToString()
                        + ", NG:" + cntNg.ToString()
                        + " // "
                    + "미분리:" + cntDisplayNG_None.ToString()
                    + ", 기포:" + cntDisplayNG_Bubble.ToString()
                    + ", 파손:" + cntDisplayNG_Defeat.ToString()
                    + ", 뜯김:" + cntDisplayNG_DK.ToString()
                    + ", 실:" + cntDisplayNG_Sil.ToString()
                    + "]\n"
                        + "[A:" + (cntAll - cntNoLenz1).ToString()
                        + ", OK:" + cntOK1.ToString() + "(" + (cntOK1 * 100 / (divide)).ToString() + "%)"
                        + ", 파손:" + cntDefeat1.ToString()
                        + ", 기포:" + cntBubble.ToString()
                        + ", 인쇄:" + cntPaint.ToString()
                        + ", 에지:" + cntEdge.ToString()
                        + "]";
            if (cntAll > 2)
            {
                int sub2 = ((cntAll - 2) == cntNoLenz2) ? 1 : (cntAll - 2) - cntNoLenz2;
                resultText += "  [B:" + ((cntAll - 2) - cntNoLenz2).ToString()
                    + ", OK:" + cntOK2.ToString() + "(" + (cntOK2 * 100 / (sub2)).ToString() + "%)"
                    + ", 파손:" + cntDefeat2.ToString()
                    + ", 실:" + cntSil.ToString()
                    + ", 뜯김:" + cntDK.ToString()
                    + ", 탑:" + cntTop.ToString()
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
                        cntTotalUpload++;
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
        private void logPrint(string where, string msg)
        {
            lock (lockObject)
            {
                string path = m_resultDir + @"\log-" + DateTime.Now.ToString("yyyy-MM");
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
        private void printRemainedUpload()
        {
            try
            {
                labelNet.InvokeIfNeeded(() => labelNet.Text = "남은 업로드 : (" + upLoadQueue.Count.ToString() + "/" + cntTotalUpload.ToString() + ")");
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
                cntAll++;
                buttonCapture.Enabled = false;
                //if (serialPortIptPLC1.IsOpen)
                //    serialPortIptPLC1.Write("@T71\r");
                //cntPreview = 0;
                labelResult1.Text = "";
                labelResult2.Text = "";

                buttonCapture.Text = "[측정 시작[" + StageIndex + "] (" + cntAll.ToString() + ")]";
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
                    if (name[0] != 'B' && name[0] != 'b') return false;
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
        private void deleteResult()
        {
            try
            {
                StageIndex = 0;
                StageIndexState = 0;

                cntAll = 0;
                cntOk = 0;
                cntNg = 0;
                cntTotalUpload = 0;
                cntNoLenz1 = 0;
                cntNoLenz2 = 0;
                cntOK1 = 0;
                cntOK2 = 0;
                cntBubble = 0;
                cntDefeat1 = 0;
                cntDefeat2 = 0;
                cntPaint = 0;
                cntEdge = 0;
                cntSil = 0;
                cntDK = 0;
                cntTop = 0;
                cntDisplayNG_Bubble = 0;
                cntDisplayNG_Defeat = 0;
                cntDisplayNG_DK = 0;
                cntDisplayNG_Sil = 0;
                cntDisplayNG_None = 0;

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
                        if (MessageBox.Show("잔량 배출 수신 처리 하겠습니까?", "잔량 배출", MessageBoxButtons.YesNo) == DialogResult.No)
                        {
                            return;
                        }
                        okProcess(-1, -1, -1, -1);
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
                                if (upLoadQueue.Count > 0)
                                {
                                    MessageBox.Show("업로드 되지않은 데이터가 있습니다. 확인 바랍니다.");
                                }
                                if (MessageBox.Show("잔량 배출 수신 처리 하겠습니까?", "잔량 배출", MessageBoxButtons.YesNo) == DialogResult.No)
                                {
                                    return;
                                }
                                okProcess(-1, -1, -1, -1);
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
                    bDebug = (bool)dr["debug"];
                    bNoCamera = (100 == (int)dr["machineCode"]) ? true : false;
                    if (!dr["resultDrive"].ToString().Equals("c"))
                    {
                        m_resultDir = dr["resultDrive"].ToString() + @":\result_IptVisionLucam";
                    }
                    System.IO.Directory.CreateDirectory(m_resultDir);
                    bAdvIO = (bool)dr["bAdvIO"];
                    bResizing = (bool)dr["bResizing"];
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
                            cntAll = (int)drTemp["count"];
                            cntOk = (int)drTemp["cntOk"];
                            cntNg = (int)drTemp["cntNg"];
                            bUpload = (bool)drTemp["bUpload"];
                            cntTotalUpload = (int)drTemp["totalUpCount"];
                            cntNoLenz1 = (int)drTemp["NoLenz1"];
                            cntOK1 = (int)drTemp["OK1"];
                            cntBubble = (int)drTemp["Bubble"];
                            cntDefeat1 = (int)drTemp["Defeat1"];
                            cntPaint = (int)drTemp["Paint"];
                            cntNoLenz2 = (int)drTemp["NoLenz2"];
                            cntOK2 = (int)drTemp["OK2"];
                            cntSil = (int)drTemp["Sil"];
                            cntDefeat2 = (int)drTemp["Defeat2"];
                            cntDK = (int)drTemp["DK"];
                            cntTop = (int)drTemp["TOP"];
                            cntEdge = (int)drTemp["Edge"];

                            cntDisplayNG_Bubble = (int)drTemp["DisplayNG_Bubble"];
                            cntDisplayNG_Defeat = (int)drTemp["DisplayNG_Defeat"];
                            cntDisplayNG_DK = (int)drTemp["DisplayNG_DK"];
                            cntDisplayNG_Sil = (int)drTemp["DisplayNG_Sil"];
                            cntDisplayNG_None = (int)drTemp["DisplayNG_None"];

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
                            serialPortIptPLC1.Open();
                            labelMessage.Text += "[COM1 초기화 완료]";
                        }
                        catch
                        {
                            MessageBox.Show("COM1 초기화 실패");
                        }
                    }
                }
                dr = dataSetBackup.Tables["centerParameter"].Rows[0];
                loadCenterParameter(ref dr);
                dr = dataSetBackup.Tables["boundaryParameter"].Rows[0];
                loadBaundaryParameter(ref dr);

                buttonCapture.Text = "[측정 시작(" + cntAll.ToString() + ")]";

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
            //if (serialPortIptPLC.IsOpen)
            //{
            //    serialPortIptPLC.Write("@D40\r");
            //}
            this.SetBounds(0, 0, 1280, 1000);
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
                if (cntAll == 0)
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
                    dataSetTempData.Tables["tempBackup"].Rows.Clear();
                    DataRow drTemp = dataSetTempData.Tables["tempBackup"].NewRow();
                    drTemp["eStatusPrevMaster0"] = eStatusPrevMaster[0];
                    drTemp["eStatusPrevMaster1"] = eStatusPrevMaster[1];
                    drTemp["eStatusPrevMaster2"] = 0;
                    drTemp["eStatusPrevMaster3"] = 0;
                    drTemp["eStatusPrevMaster4"] = 0;
                    drTemp["count"] = cntAll;
                    drTemp["cntOk"] = cntOk;
                    drTemp["cntNg"] = cntNg;
                    drTemp["bUpload"] = bUpload;
                    drTemp["totalUpCount"] = cntTotalUpload;
                    drTemp["NoLenz1"] = cntNoLenz1;
                    drTemp["OK1"] = cntOK1;
                    drTemp["Bubble"] = cntBubble;
                    drTemp["Defeat1"] = cntDefeat1;
                    drTemp["Edge"] = cntEdge;
                    drTemp["Paint"] = cntPaint;
                    drTemp["NoLenz2"] = cntNoLenz2;
                    drTemp["OK2"] = cntOK2;
                    drTemp["Sil"] = cntSil;
                    drTemp["Defeat2"] = cntDefeat2;
                    drTemp["DK"] = cntDK;
                    drTemp["TOP"] = cntTop;
                    drTemp["DisplayNG_Bubble"] = cntDisplayNG_Bubble;
                    drTemp["DisplayNG_Defeat"] = cntDisplayNG_Defeat;
                    drTemp["DisplayNG_DK"] = cntDisplayNG_DK;
                    drTemp["DisplayNG_Sil"] = cntDisplayNG_Sil;
                    drTemp["DisplayNG_None"] = cntDisplayNG_None;

                    dataSetTempData.Tables["tempBackup"].Rows.Add(drTemp);
                    dataSetTempData.WriteXml(m_tempDir + @"\tempData.xml", XmlWriteMode.WriteSchema);
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
            if (previousLotNumber.Equals(textBoxLotNumber.Text) == false && cntAll != 0)
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
            if (bCheckOkLotnumber && cntAll == 0)
            {
                FormCheckLotNo dlg = new FormCheckLotNo();
                DataRow dr = dataSetBackup.Tables["fixedBackup"].Rows[0];
                dlg.LotNumber = textBoxLotNumber.Text;
                dlg.McCd = "B" + ((int)dr["machineCode"]).ToString("000");
                dlg.PsCd = psCd;
                dlg.Group = group;
                dlg.ShowDialog();
                bCheckOkLotnumber = dlg.OK;
                psCd = dlg.PsCd;
                group = dlg.Group;
            }
            trackBarCenterShadowWidth.Value = 3;
            previousLotNumber = textBoxLotNumber.Text;
            logPrint(MethodBase.GetCurrentMethod().Name, "LotNumber=" + textBoxLotNumber.Text);
            dataSetBackup.Tables["fixedBackup"].Rows[0]["lotNumber"] = textBoxLotNumber.Text;
            System.IO.Directory.CreateDirectory(m_resultDir + @"\" + textBoxLotNumber.Text);
            if (CheckLotFormat(textBoxLotNumber.Text))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(m_uploadDir + @"\images\" + textBoxLotNumber.Text);
                    if (Directory.Exists(m_uploadDir + @"\images\" + textBoxLotNumber.Text) == false)
                    {
                        bUpload = false;
                        labelNet.Text = "네트웤 이상";
                        labelNet.BackColor = Color.Red;
                        panelResultBase.BackColor = Color.Red;
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
                    else
                    {
                        string strTest = dataSetBackup.Tables["fixedBackup"].Rows[0]["machineCode"].ToString();
                        string desFilename = m_resultDir + @"\" + textBoxLotNumber.Text + @"\ID.txt";
                        string netFilename = m_uploadDir + @"\images\" + textBoxLotNumber.Text + @"\ID.txt";
                        StreamWriter SWrite = new StreamWriter(desFilename, false, System.Text.Encoding.ASCII);
                        SWrite.WriteLine(strTest);
                        SWrite.WriteLine("1");
                        SWrite.Close();
                        try
                        {
                            System.IO.File.Copy(desFilename, netFilename, true);
                            bUpload = true;
                            labelNet.Text = "네트웤 준비 완료";
                            labelNet.BackColor = Color.Transparent;
                            panelResultBase.BackColor = Color.LimeGreen;
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
                        catch { }
                    }
                }
                catch
                {
                    bUpload = false;
                    labelNet.Text = "네트웤 이상";
                    labelNet.BackColor = Color.Red;
                    panelResultBase.BackColor = Color.Red;
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
            else
            {
                bUpload = false;
                labelNet.Text = "네트웤 저장하지 않음";
                labelNet.BackColor = Color.Yellow;
                panelResultBase.BackColor = Color.Yellow;
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
            textBoxLotNumber.BackColor = Color.FromKnownColor(KnownColor.Window);
            textBoxLotNumber.Enabled = false;
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
            WM_USER = 0x400, PictureBoxIpl1_Image = WM_USER, PictureBoxIpl2_Image, UserControlBar_Value, CheckCamera1_finish, CheckCamera2_finish, serialPortPLC, FINISH1, FINISH2
        };
        protected override void WndProc(ref Message msg)
        {
            int id = (int)msg.WParam;
            int value = (int)msg.LParam;
            switch (msg.Msg)
            {
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
    }
    #endregion "importDll"
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using Ipt;
using LUCAMAPICOMLib;
using OpenCvSharp;

namespace IptVisionLucam
{
    public partial class FormLive : Form
    {
        private long snapshotLum;
        public long SnapshotLum
        {
            set { snapshotLum = value; }
        }
        lucamCOMClass LucamCamera;
        private LuCamRGBPreviewCallback rgbPreviewCallback;
        private LuCamStreamingCallback streamingCallback;
        int hCamera = 0;
        DataRow drRoi = null;
        public FormLive(int hCamera, lucamCOMClass com, DataRow drRoi)
        {
            this.drRoi = drRoi;
            LucamCamera = com;
            this.hCamera = hCamera;
            InitializeComponent();
        }
        int iRGBPreviewCallbackId = 0;
        private int iStreamingCallbackId;


        private void FormLive_Load(object sender, EventArgs e)
        {
            LucamCamera.DisableFastFrames(hCamera);
            //rgbPreviewCallback = new LuCamRGBPreviewCallback();
            streamingCallback = new LuCamStreamingCallback(this);
            if (false == LucamCamera.GetFormat(hCamera, out FrameFormat, out FrameRate))
            {
                MessageBox.Show("Unable to get current frame format");
            }
            pictureBoxCam.Width = FrameFormat.width;
            pictureBoxCam.Height = FrameFormat.height;
            rgbPreviewCallback = new LuCamRGBPreviewCallback(FrameFormat.width, FrameFormat.height, drRoi);
            pictureBox1.Left = 0;
            pictureBox1.Width = pictureBoxCam.Width;
            pictureBox1.Top = pictureBoxCam.Height / 2;
            pictureBox1.Height = 1;
            pictureBox2.Left = pictureBoxCam.Width / 2;
            pictureBox2.Width = 1;
            pictureBox2.Top = 0;
            pictureBox2.Height = pictureBoxCam.Height;

            int context = 0;
            iRGBPreviewCallbackId = LucamCamera.AddRgbPreviewCallback(hCamera, (IlucamCOMRGBPreviewCallback)rgbPreviewCallback, ref context, 2);
            iStreamingCallbackId = LucamCamera.AddStreamingCallback(hCamera, (IlucamCOMStreamingCallback)streamingCallback, ref context);

            // Start preview
            if (false == LucamCamera.StreamVideoControl(hCamera, LUCAMAPICOMLib.LUCAM_STREAMING_MODE.START_DISPLAY, pictureBoxCam.Handle.ToInt32()))
            {
                MessageBox.Show("Unable to start preview.");
            }
            labelROI.Left = (int)drRoi["roiLeft"];
            labelROI.Top = (int)drRoi["roiTop"];
            labelROI.Width = (int)drRoi["roiWidth"];
            labelROI.Height = (int)drRoi["roiHeight"];
            timer1.Start();
        }
        LUCAMAPICOMLib.LUCAM_FRAME_FORMAT_COM FrameFormat;
        float FrameRate;

        private void FormLive_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (false == LucamCamera.StreamVideoControl(hCamera, LUCAMAPICOMLib.LUCAM_STREAMING_MODE.STOP_STREAMING, pictureBoxCam.Handle.ToInt32()))
            {
                MessageBox.Show("Unable to stop preview.");
            }
            else
            {
                if (false == LucamCamera.DestroyDisplayWindow(hCamera))
                    MessageBox.Show("Unable to destroy preview window");
            }
            if (!LucamCamera.RemoveRgbPreviewCallback(hCamera, iRGBPreviewCallbackId))
            {
                MessageBox.Show("Unable to remove RGB preview callback");
            }
            if (!LucamCamera.RemoveStreamingCallback(hCamera, iStreamingCallbackId))
            {
                MessageBox.Show("Unable to remove streaming callback");
            }
            float Exposure, Gain;
            int Flags;
           
            if (false == LucamCamera.GetProperty(hCamera, LUCAMAPICOMLib.LUCAM_PROPERTY.exposure, out Exposure, out Flags))
            {
                MessageBox.Show("Unable to get current exposure.");
            }
            
            if (false == LucamCamera.GetProperty(hCamera, LUCAMAPICOMLib.LUCAM_PROPERTY.GAIN, out Gain, out Flags))
            {
                MessageBox.Show("Unable to get current Gain.");
            }
            LucamSnapshot.bufferLastFrame = 0;
            LucamSnapshot.exposure = Exposure;
            LucamSnapshot.exposureDelay = 0.0F;
            LucamSnapshot.flReserved1 = 0.0F;
            LucamSnapshot.flReserved2 = 0.0F;
            LucamSnapshot.format = FrameFormat;
            LucamSnapshot.globalGain = Gain;
            LucamSnapshot.shutterType = LUCAMAPICOMLib.LUCAM_SHUTTER_TYPE.ROLLING;
            LucamSnapshot.strobeDelay = 0.0F;
            LucamSnapshot.timeout = 3000;
            LucamSnapshot.ulReserved1 = 0;
            LucamSnapshot.ulReserved2 = 0;
            LucamSnapshot.useHwTrigger = 0;
            LucamSnapshot.useStrobe_strobeFlags = 0;
            LucamSnapshot.gainBlue_Cyan = 1.0F;
            LucamSnapshot.gainGrn1_Yel1 = 1.0F;
            LucamSnapshot.gainGrn2_Yel2 = 1.0F;
            LucamSnapshot.gainRed_Mag = 1.0F;
            LucamCamera.EnableFastFrames(hCamera, LucamSnapshot);
        }
        
        private LUCAMAPICOMLib.LUCAM_SNAPSHOT_COM LucamSnapshot;

        private void button1_Click(object sender, EventArgs e)
        {
            LucamCamera.DisplayPropertyPage(hCamera, Handle.ToInt32());
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                pictureBoxCam.Size = panel1.Size;
                pictureBoxCam.SizeMode = PictureBoxSizeMode.Zoom;
            }
            else
            {
                pictureBoxCam.Width = FrameFormat.width;
                pictureBoxCam.Height = FrameFormat.height;
                pictureBoxCam.SizeMode = PictureBoxSizeMode.Normal;
            }
        }

        private void checkBoxViewROI_CheckedChanged(object sender, EventArgs e)
        {
            labelROI.Visible = checkBoxViewROI.Checked;
        }
        System.Drawing.Point prevLocation;
        bool bMoving = false;
        private void labelROI_MouseDown(object sender, MouseEventArgs e)
        {
            prevLocation = e.Location;
            bMoving = true;
        }
        private void labelROI_MouseUp(object sender, MouseEventArgs e)
        {
            ((Control)sender).Cursor = Cursors.Default;
            if (bMoving)
            {
                System.Drawing.Point p = panel1.AutoScrollPosition;
                bMoving = false;
                drRoi["roiTop"] = ((Control)sender).Top - p.Y;
                drRoi["roiLeft"] = ((Control)sender).Left - p.X;
                drRoi["roiWidth"] = ((Control)sender).Width;
                drRoi["roiHeight"] = ((Control)sender).Height;
            }
        }
        private void labelROI_MouseMove(object sender, MouseEventArgs e)
        {
            IptDefines.MoveControl(sender, e.Location, prevLocation, 5, bMoving);
            if (bMoving)
            {
                System.Drawing.Point p = panel1.AutoScrollPosition;
                drRoi["roiTop"] = ((Control)sender).Top - p.Y;
                drRoi["roiLeft"] = ((Control)sender).Left - p.X;
                drRoi["roiWidth"] = ((Control)sender).Width;
                drRoi["roiHeight"] = ((Control)sender).Height;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            labelROI.Text = rgbPreviewCallback.avg.ToString("0");
        }
    }
    public class LuCamRGBPreviewCallback : IlucamCOMRGBPreviewCallback
    {
        int w, h;
        public double avg = 0;
        DataRow drRoi;
        public LuCamRGBPreviewCallback(int w, int h, DataRow _drRoi)
        {
            this.w = w;
            this.h = h;
            m_count = 0;
            this.drRoi = _drRoi;
        }
        public void RGBPreviewCallback(int context, int data, uint len, uint unused)
        {
            CvRect rc = new CvRect();
            rc.X = (int)drRoi["roiLeft"];
            rc.Y = (int)drRoi["roiTop"];
            rc.Width = (int)drRoi["roiWidth"];
            rc.Height = (int)drRoi["roiHeight"];

            m_count++;
            if (m_count >= len) return;
            bool bColor = len > w * h;
            IplImage image = new IplImage(w, h, BitDepth.U8, 1);
            unsafe
            {
                byte* pData = (byte*)(((IntPtr)data).ToPointer());
                byte* p = image.ImageDataPtr;
                //for (int j = (int)rc.Top; j < rc.Bottom; j++) for (int i = (int)rc.Left; i < rc.Right; i++)
                for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
                    {
                        int index1 = i + j * w;
                        int index = i + (h - j -1) * w;
                        if (bColor) index = index * 3;
                        p[index1] = pData[index];
                    }
                //for (int i = 0; i < m_count; i++)
                //{
                //    pData[i] = (byte)255;
                //    i = i + 1;
                //}

            }
            image.SetROI(rc);
            avg = image.Avg();
            image.ResetROI();
            image.Dispose();
        }
        int m_count;
    }
    public class LuCamStreamingCallback : IlucamCOMStreamingCallback
    {
        public LuCamStreamingCallback(object parent)
        {
            m_parent = parent;
        }

        public void StreamingCallback(int context, int data, uint len)
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

           // ((FormLive)m_parent).StreamingLum = (long)(lum / len);
        }
        object m_parent;

    }

}

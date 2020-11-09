using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using OpenCvSharp;
using Ipt;

namespace Ipt
{
	public partial class FormPreview : Form
	{
        
        private Point startPos = new Point();
        private int startValue = 0;
        private IplImage orgBitmap = null;
        private IplImage resultBitmap = null;
        private Queue<Point> queue = new Queue<Point>();
        CvRect rcRoi;
        public FormPreview(IplImage orgBitmap, IplImage resultBitmap, string title, CvRect rcRoi)
		{
            this.rcRoi = rcRoi;
            if (orgBitmap!=null)
				this.orgBitmap = orgBitmap.Clone();
			if(resultBitmap!=null)
                this.resultBitmap = resultBitmap.Clone();
			InitializeComponent();
			pictureBoxIpl1.ImageIpl = this.resultBitmap;
			this.Text = "영상보기 [" + title + "]  (Ctrl:크기계산, Alt:길이계산, Shift:밝기 계산)";
            labelROI.Top = rcRoi.Top;
            labelROI.Left = rcRoi.Left;
            labelROI.Width = rcRoi.Width;
            labelROI.Height = rcRoi.Height;
        }
		private void FormPreview_Load(object sender, EventArgs e)
		{
            pictureBoxIpl1.SizeMode = PictureBoxSizeMode.AutoSize;
			if (resultBitmap != null && orgBitmap != null)
			{
                panel1.VerticalScroll.Maximum = pictureBoxIpl1.Image.Height + this.ClientSize.Height;
                panel1.HorizontalScroll.Maximum = pictureBoxIpl1.Image.Width + this.ClientSize.Width;
			}
		}

        private void DrawLine(MouseEventArgs e)
        {
            if (pictureBoxIpl1.Image == null) return;
            Point endPos = e.Location;
            pictureBoxIpl1.Refresh();
            System.Drawing.Graphics graphicsObj;
            graphicsObj = pictureBoxIpl1.CreateGraphics();
            Pen myPen = new Pen(System.Drawing.Color.Red, 1);
            graphicsObj.DrawLine(myPen, startPos, endPos);
            SolidBrush redBrush = new SolidBrush(Color.Red);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            double size = Math.Sqrt(Math.Pow(startPos.X - endPos.X, 2) + Math.Pow(startPos.Y - endPos.Y, 2));
            graphicsObj.DrawString("길이=" + size.ToString("0.00"), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(endPos.X - 1, endPos.Y - 20));
            graphicsObj.DrawString("길이=" + size.ToString("0.00"), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(endPos.X + 1, endPos.Y - 20));
            graphicsObj.DrawString("길이=" + size.ToString("0.00"), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(endPos.X, endPos.Y - 21));
            graphicsObj.DrawString("길이=" + size.ToString("0.00"), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(endPos.X, endPos.Y - 19));
            graphicsObj.DrawString("길이=" + size.ToString("0.00"), new Font("Arial", 11, System.Drawing.FontStyle.Regular), redBrush, new PointF(endPos.X, endPos.Y - 20));
        }
        private void putQue(ref Bitmap bitmap, Color color, Point p, ref Point LeftTop, ref Point RightBottom)
        {
            bitmap.SetPixel(p.X, p.Y, Color.Pink);
            queue.Enqueue(p);
            if (p.Y < LeftTop.Y) LeftTop.Y = p.Y;
            if (p.Y > RightBottom.Y) RightBottom.Y = p.Y;
            if (p.X < LeftTop.X) LeftTop.X = p.X;
            if (p.X > RightBottom.X) RightBottom.X = p.X;
        }
        private bool getQue(ref Point p)
        {
            if (queue.Count > 0)
            {
                Point pt = new Point();
                pt = queue.Dequeue();
                p.X = pt.X;
                p.Y = pt.Y;
                return true;
            }
            return false;
        }
        private void DrawBox(object sender, Point p)
        {
            if (pictureBoxIpl1.Image == null) return;
            pictureBoxIpl1.Refresh();
            Bitmap bitmap = (Bitmap)(((PictureBox)sender).Image);
            Color color = bitmap.GetPixel(p.X, p.Y);
            if (color.ToArgb() == Color.Black.ToArgb()) return;
            if (color.ToArgb() == Color.Pink.ToArgb()) return;
            if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb) return;

            Point LeftTop = new Point(p.X, p.Y);
            Point RightBottom = new Point(p.X, p.Y);
            queue.Clear();
            putQue(ref bitmap, color, p, ref LeftTop, ref RightBottom);
            while (getQue(ref p))
            {
                if (p.X > 0 && bitmap.GetPixel(p.X - 1, p.Y).Equals(color)) // 좌
                {
                    putQue(ref bitmap, color, new Point(p.X - 1, p.Y), ref LeftTop, ref RightBottom);
                }
                if (p.X < bitmap.Width - 1 && bitmap.GetPixel(p.X + 1, p.Y).Equals(color)) // 우
                {
                    putQue(ref bitmap, color, new Point(p.X + 1, p.Y), ref LeftTop, ref RightBottom);
                }
                if (p.Y > 0 && bitmap.GetPixel(p.X, p.Y - 1).Equals(color))  // 상
                {
                    putQue(ref bitmap, color, new Point(p.X, p.Y - 1), ref LeftTop, ref RightBottom);
                }
                if (p.Y < bitmap.Height - 1 && bitmap.GetPixel(p.X, p.Y + 1).Equals(color)) // 하
                {
                    putQue(ref bitmap, color, new Point(p.X, p.Y + 1), ref LeftTop, ref RightBottom);
                }
                if (p.X > 0 && p.Y > 0 && bitmap.GetPixel(p.X - 1, p.Y - 1).Equals(color)) // 좌상
                {
                    putQue(ref bitmap, color, new Point(p.X - 1, p.Y - 1), ref LeftTop, ref RightBottom);
                }
                if (p.X < bitmap.Width - 1 && p.Y > 0 && bitmap.GetPixel(p.X + 1, p.Y - 1).Equals(color)) // 우상
                {
                    putQue(ref bitmap, color, new Point(p.X + 1, p.Y - 1), ref LeftTop, ref RightBottom);
                }
                if (p.X > 0 && p.Y < bitmap.Height - 1 && bitmap.GetPixel(p.X - 1, p.Y + 1).Equals(color))  // 좌하
                {
                    putQue(ref bitmap, color, new Point(p.X - 1, p.Y + 1), ref LeftTop, ref RightBottom);
                }
                if (p.X < bitmap.Width - 1 && p.Y < bitmap.Height - 1 && bitmap.GetPixel(p.X + 1, p.Y + 1).Equals(color)) // 우하
                {
                    putQue(ref bitmap, color, new Point(p.X + 1, p.Y + 1), ref LeftTop, ref RightBottom);
                }
            }
            Rectangle rect = new Rectangle(LeftTop.X, LeftTop.Y, RightBottom.X - LeftTop.X, RightBottom.Y - LeftTop.Y);
            System.Drawing.Graphics graphicsObj;
            graphicsObj = pictureBoxIpl1.CreateGraphics();
            Pen myPen = new Pen(System.Drawing.Color.Red, 1);
            SolidBrush redBrush = new SolidBrush(Color.Red);
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            if (rect.Width == 0 || rect.Height == 0)
            {
                graphicsObj.DrawLine(myPen, rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
            else
            {
                graphicsObj.DrawRectangle(myPen, rect);
            }
            int size = Math.Max(rect.Width, rect.Height) + 1;
            graphicsObj.DrawString("크기=" + size.ToString(), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(rect.Left - 1, rect.Top - 20));
            graphicsObj.DrawString("크기=" + size.ToString(), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(rect.Left + 1, rect.Top - 20));
            graphicsObj.DrawString("크기=" + size.ToString(), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(rect.Left, rect.Top - 21));
            graphicsObj.DrawString("크기=" + size.ToString(), new Font("Arial", 11, System.Drawing.FontStyle.Regular), blackBrush, new PointF(rect.Left, rect.Top - 19));
            graphicsObj.DrawString("크기=" + size.ToString(), new Font("Arial", 11, System.Drawing.FontStyle.Regular), redBrush, new PointF(rect.Left, rect.Top - 20));
        }
 

		
		public void mouseRClick()
		{
            if (pictureBoxIpl1.ImageIpl == resultBitmap)
			{
                pictureBoxIpl1.ImageIpl = orgBitmap;
			}
			else
			{
                pictureBoxIpl1.ImageIpl = resultBitmap;
			}
		}

        private void pictureBoxIpl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                mouseRClick();
            }
        }

        private void pictureBoxIpl1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if ((0x8000 & SafeNativeMethods.GetAsyncKeyState(0x11)) != 0)
                {
                    ((PictureBox)sender).Cursor = Cursors.UpArrow;
                    startPos = e.Location;
                }
                else if ((0x8000 & SafeNativeMethods.GetAsyncKeyState(0x12)) != 0)
                {
                    ((PictureBox)sender).Cursor = Cursors.Cross;
                    startPos = e.Location;
                }
                else
                {
                    ((PictureBox)sender).Cursor = Cursors.Hand;
                    startPos = e.Location;
                    startValue = VerticalScroll.Value;
                }
            }
            catch { }
        }

        //ToolTip tt = new ToolTip();
        private void pictureBoxIpl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (((PictureBox)sender).Cursor == Cursors.Cross)
            {
                DrawLine(e);
            }
            else
            {
                if ((0x8000 & SafeNativeMethods.GetAsyncKeyState(0xa0)) != 0)
                {
                    Point p = e.Location;
                    var value = pictureBoxIpl1.ImageIpl.Get2D(p.Y, p.X);
                    this.Text = "(Ctrl:크기계산, Alt:길이계산, Shift:밝기 계산) 밝기값=" + value.Val1;
                    //IWin32Window win = this; TODO 나중에 이쁘게 보이게 하자
                    //tt.Show(value.Val1.ToString(), win, e.Location);
                }
            }
        }

        private void pictureBoxIpl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (((PictureBox)sender).Cursor == Cursors.Hand)
            {
                int value = panel1.VerticalScroll.Value - (e.Location.Y - startPos.Y);
                if (value > panel1.VerticalScroll.Maximum) value = panel1.VerticalScroll.Maximum;
                if (value < 0) value = 0;
                panel1.VerticalScroll.Value = value;

                value = panel1.HorizontalScroll.Value - (e.Location.X - startPos.X);
                if (value > panel1.HorizontalScroll.Maximum) value = panel1.HorizontalScroll.Maximum;
                if (value < 0) value = 0;
                panel1.HorizontalScroll.Value = value;
            }
            else if (((PictureBox)sender).Cursor == Cursors.Cross)
            {
                DrawLine(e);
            }
            else if (((PictureBox)sender).Cursor == Cursors.UpArrow)
            {
                Point endPos = e.Location;
                DrawBox(sender, endPos);
            }
            ((PictureBox)sender).Cursor = Cursors.Default;
        }

        private void checkBoxViewROI_CheckedChanged(object sender, EventArgs e)
        {
            labelROI.Visible = checkBoxViewROI.Checked;
            UpdateValue();
        }
        Point prevLocation;
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
                rcRoi.Top = ((Control)sender).Top - p.Y;
                rcRoi.Left = ((Control)sender).Left - p.X;
                rcRoi.Width = ((Control)sender).Width;
                rcRoi.Height = ((Control)sender).Height;
                UpdateValue();
            }
        }

        private void labelROI_MouseMove(object sender, MouseEventArgs e)
        {
            IptDefines.MoveControl(sender, e.Location, prevLocation, 5, bMoving);
            if (bMoving)
            {
                System.Drawing.Point p = panel1.AutoScrollPosition;
                rcRoi.Top = ((Control)sender).Top - p.Y;
                rcRoi.Left = ((Control)sender).Left - p.X;
                rcRoi.Width = ((Control)sender).Width;
                rcRoi.Height = ((Control)sender).Height;
                UpdateValue();
            }
        }
        void UpdateValue()
        {
            try
            {
                orgBitmap.SetROI(rcRoi);
                double avg = orgBitmap.Avg();
                orgBitmap.ResetROI();
                labelROI.Text = avg.ToString("0");
            }
            catch { }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace IptVisionLucam
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool createdNew;
            Mutex gM1 = new Mutex(true, "IptVisionLucam", out createdNew);
            if (createdNew)
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
                try
                {
                    Application.Run(new FormMain());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "프로그램 비정상종료, 화면을 캡춰해주세요.");
                }
                gM1.ReleaseMutex();
            }
            else
                MessageBox.Show("이미 실행되어 있습니다.");
        }
    }
}

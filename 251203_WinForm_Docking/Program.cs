using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using _251203_WinForm_Docking.Util;

namespace _251203_WinForm_Docking
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            //#14_LOGFORM#1 log4net 설정 파일을 읽어들임
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
            SLogger.Write("Logger initialized!", SLogger.LogType.Info);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }
    }
}

using PureGate.Core;
using PureGate.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PureGate
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1) 로그인
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK)
                    return;
            }

            // 2) 로딩폼 표시
            var loading = new LoadingForm();
            loading.Show();
            loading.Refresh();
            Application.DoEvents();

            loading.SetSteps(
                "Initializing core services",
                "Loading inspection model",
                "Finalizing UI"
            );
            loading.SetProgress(null);

            // 3) 메인폼 먼저 생성 (네 구조상 필요)
            var mainForm = new MainForm(); // 생성만

            // 4) Core 초기화
            loading.SetActiveStep(0);
            loading.SetStatus("Initializing core services...");
            Application.DoEvents();
            Global.Inst.Initialize();

            // 5) 최근 모델 로드
            loading.SetActiveStep(1);
            loading.SetStatus("Loading inspection model...");
            Application.DoEvents();
            Global.Inst.InspStage.LastestModelOpen(loading);

            // 6) 마무리
            loading.SetActiveStep(2);
            loading.SetStatus("Starting runtime services...");
            loading.Refresh();
            Application.DoEvents();

            // (있으면) 실제 UI 반영 작업
            // mainForm.ApplyLoadedModel();
            // Application.DoEvents();

            // 최소 표시시간 200ms 보장
            var until = Environment.TickCount + 600;
            while (Environment.TickCount < until)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            }

            // 7) 로딩폼 닫고 메인폼 실행
            loading.Close();
            loading.Dispose();

            Application.Run(mainForm);
        }
    }
}
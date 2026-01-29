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
                "Initializing Core Services",
                "Loading Inspection Model",
                "Finalizing UI"
            );
            loading.SetProgress(0);

            // 3) 메인폼 먼저 생성 (네 구조상 필요)
            var mainForm = new MainForm(); // 생성만

            // 4) Core 초기화
            loading.SetActiveStep(0);
            loading.SetStatus("Initializing Core Services...");
            loading.SetProgress(1);
            loading.Refresh();
            Application.DoEvents();

            Global.Inst.Initialize((p, s) =>
            {
                if (!string.IsNullOrWhiteSpace(s))
                    loading.SetStatus(s);

                loading.SetProgress(p);
                loading.Refresh();
                Application.DoEvents();
            });

            // 5) 최근 모델 로드 (✅ 여기서만!)
            loading.SetActiveStep(1);
            loading.SetStatus("Loading Inspection Model...");
            loading.SetProgress(56);
            loading.Refresh();
            Application.DoEvents();

            Global.Inst.InspStage.LastestModelOpenWithProgress(loading, 55, 90);

            // 6) 마무리
            loading.SetActiveStep(2);
            loading.SetStatus("Finalizing UI...");
            loading.SetProgress(92);
            loading.Refresh();
            Application.DoEvents();

            loading.SetProgress(100);
            loading.SetStatus("Ready");
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
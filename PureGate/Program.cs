using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureGate.Core;
using PureGate.UIControl;
using PureGate.Util;

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

            // 3) 메인폼 먼저 생성
            var mainForm = new MainForm();

            bool modelLoaded = false;

            try
            {
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

                // 5) 최근 모델 로드
                loading.SetActiveStep(1);
                loading.SetStatus("Loading Inspection Model...");
                loading.SetProgress(56);
                loading.Refresh();
                Application.DoEvents();

                // 여기서 USB/라이선스 문제로 예외가 나면 catch로 떨어짐
                Global.Inst.InspStage.LastestModelOpenWithProgress(loading, 55, 90);
                modelLoaded = true;

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

                // 최소 표시시간
                var until = Environment.TickCount + 600;
                while (Environment.TickCount < until)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                // 로딩폼 닫기 전에 안내 먼저 띄워도 되고(취향), 닫고 띄워도 됨.
                // 여기서는 사용자 안내가 우선.

                MsgBox.Show(
                    "최근 모델 로딩에 실패했습니다.\n\n" +
                    "SageVision USB(라이선스 키)가 연결되어 있는지 확인하세요.\n",
                    
                    "모델 로딩 실패",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // 여기서 return 하지 않음: 메인폼은 실행해서 사용자가 나중에 다시 오픈 가능하게
            }
            finally
            {
                try { loading.Close(); } catch { }
                try { loading.Dispose(); } catch { }
            }

            // 메인폼 실행 (modelLoaded가 false여도 실행)
            Application.Run(mainForm);
        }
    }
}
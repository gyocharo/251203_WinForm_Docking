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

            // 3) 메인폼 먼저 생성 (UI 골격만)
            var mainForm = new MainForm(); // 생성만! Show 하지 말 것

            // 4) Core 초기화
            loading.SetStatus("Initializing...");
            Application.DoEvents();
            Global.Inst.Initialize();

            // 5) 최근 모델 로드
            loading.SetStatus("Loading model...");
            Application.DoEvents();

            Global.Inst.InspStage.LastestModelOpen(loading);

            // 6) 모델 반영(타이틀/이미지) — Shown에 맡기지 말고 지금 한 번 호출
            loading.SetStatus("Ready...");
            Application.DoEvents();

            // 7) 로딩폼 닫고 메인폼 실행
            loading.Close();
            loading.Dispose();

            Application.Run(mainForm);
        }
    }
}
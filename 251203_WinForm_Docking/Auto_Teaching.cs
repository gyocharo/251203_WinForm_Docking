using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using _251203_WinForm_Docking.Algorithm;
using _251203_WinForm_Docking.Core;
using _251203_WinForm_Docking.Teach;
using OpenCvSharp;

namespace _251203_WinForm_Docking
{
    public partial class Auto_Teaching : Form
    {
        public Auto_Teaching()
        {
            InitializeComponent();
        }

        private void btn_Apply_Click(object sender, EventArgs e)
        {
            int matchScore = (int)Num_Score.Value;
            int matchCount = (int)Num_Entity.Value;

            // 2️⃣ 선택된 InspWindow 가져오기
            InspWindow inspWindow = Global.Inst.InspStage.PreView?.GetInspWindow();
            if (inspWindow == null)
            {
                MessageBox.Show("선택된 ROI가 없습니다.");
                return;
            }

            // 3️⃣ MatchAlgorithm 찾기
            MatchAlgorithm algo =
                inspWindow.FindInspAlgorithm(InspectType.InspMatch) as MatchAlgorithm;

            if (algo == null)
            {
                MessageBox.Show("Match 알고리즘이 없습니다.");
                return;
            }

            // 4️⃣ 파라미터 반영 (Apply 전용)
            algo.MatchScore = matchScore;
            algo.MatchCount = matchCount;

            // 5️⃣ 이미지 준비
            Mat src = Global.Inst.InspStage.GetMat(0, algo.ImageChannel);
            Rect roi = algo.InspRect;
            Mat target = src[roi];

            // 6️⃣ Auto Teaching 실행
            var results = algo.AutoTeaching(
                target,
                roi.TopLeft,
                matchScore,
                matchCount
            );

            // 7️⃣ 화면 표시용 결과만 갱신
            algo.OutPoints.Clear();
            foreach (var r in results)
                algo.OutPoints.Add(r.Location);

            // 8️⃣ 화면 갱신
            Global.Inst.InspStage.RedrawMainView();
        }
    }
}

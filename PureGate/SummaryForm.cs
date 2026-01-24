using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace PureGate
{
    public partial class SummaryForm : DockContent
    {
        private string _project;
        private DateTime _from;
        private DateTime _to;
        private Label lblInfo;

        // 생성자 수정: 프로젝트명 + 날짜 범위
        public SummaryForm(string project, DateTime from, DateTime to)
        {
            _project = project;
            _from = from;
            _to = to;

            InitializeComponent();
            InitializeUI();
            LoadSummaryData();
        }

        private void InitializeUI()
        {
            lblInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            this.Controls.Add(lblInfo);
        }

        private void LoadSummaryData()
        {
            lblInfo.Text = $"프로젝트: {_project} / 조회 기간: {_from.ToShortDateString()} ~ {_to.ToShortDateString()}";

            // TODO: 데이터 차트, 표 등 추가
        }
    }
}

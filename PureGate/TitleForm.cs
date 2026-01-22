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
    public partial class TitleForm : DockContent
    {
        public TitleForm()
        {
            InitializeComponent();
        }

        private void ShowResult(DateTime start, DateTime end)
        {
            string project = "PureGate"; 
            SummaryForm resultForm = new SummaryForm(project, start, end);
            resultForm.Show();
        }

        private void lblToday_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today, DateTime.Today);
        }

        private void lbl1Week_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today.AddDays(-6), DateTime.Today);
        }

        private void lbl1Month_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today.AddMonths(-1).AddDays(1), DateTime.Today);
        }

        private void lbl1Year_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ShowResult(DateTime.Today.AddYears(-1).AddDays(1), DateTime.Today);
        }

        private void btnShowRange_Click(object sender, EventArgs e)
        {
            // 1. 시작/끝 날짜 가져오기
            DateTime start = dtpStart.Value.Date;
            DateTime end = dtpEnd.Value.Date;

            // 2. 날짜 범위 유효성 체크 (끝날짜가 시작날짜보다 빠르면 경고)
            if (end < start)
            {
                MessageBox.Show("끝 날짜는 시작 날짜보다 빠를 수 없습니다.", "날짜 범위 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. 프로젝트 선택값 가져오기 (ComboBox가 있다면)
            string project = "PureGate";

            // 4. SummaryForm 생성 후 보여주기
            SummaryForm summaryForm = new SummaryForm(project, start, end);
            summaryForm.Show();
        }
    }
}

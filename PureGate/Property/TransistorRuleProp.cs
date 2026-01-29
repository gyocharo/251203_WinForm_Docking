using PureGate.Algorithm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using PureGate.Core;

namespace PureGate.Property
{
    public partial class TransistorRuleProp : UserControl
    {
        private TransistorRuleAlgorithm _algo;
        private bool _updating = false;

        // 외부에서 "값 바뀌었으니 프리뷰/재검사 갱신" 등에 쓰라고 이벤트 제공
        public event EventHandler PropertyChanged;

        private CheckBox chkUse;

        // 기존(공통/기본)
        private NumericUpDown nudBodyDarkPct;
        private NumericUpDown nudLeadBrightPct;
        private NumericUpDown nudExpectedLead;

        // 바디 검출 관련(기존)
        private NumericUpDown nudMinBodyWidthRatio;
        private NumericUpDown nudMinBodyHeightRatio;

        // 리드 검출 관련(기존)
        private NumericUpDown nudMinLeadArea;
        private NumericUpDown nudMinLeadAspectRatio;
        private NumericUpDown nudMaxLeadWidthRatioToBody;
        private NumericUpDown nudLeadAttachGap;

        // Sub(위치불량) 관련(신규)
        private NumericUpDown nudSubMinArea;
        private NumericUpDown nudSubAngleTol;
        private NumericUpDown nudSubOffsetX;
        private NumericUpDown nudSubOffsetY;
        private NumericUpDown nudSubOpenK;
        private NumericUpDown nudSubCloseK;

        private Button btnApply;
        private Button btnResetDefault;

        private GroupBox grpBasic;
        private GroupBox grpBody;
        private GroupBox grpLead;
        private GroupBox grpPose;

        private TableLayoutPanel root;

        public TransistorRuleProp()
        {
            InitializeComponent();
            WireEvents();
        }

        public void SetAlgorithm(TransistorRuleAlgorithm algo)
        {
            _algo = algo;
            SetProperty();
        }

        public void SetProperty()
        {
            if (_algo == null) return;

            _updating = true;
            try
            {
                chkUse.Checked = _algo.IsUse;

                // ---------- 기존 파라미터 ----------
                nudBodyDarkPct.Value = ClampToRange(_algo.BodyDarkThresholdPercentile, (int)nudBodyDarkPct.Minimum, (int)nudBodyDarkPct.Maximum);
                nudLeadBrightPct.Value = ClampToRange(_algo.LeadBrightThresholdPercentile, (int)nudLeadBrightPct.Minimum, (int)nudLeadBrightPct.Maximum);
                nudExpectedLead.Value = ClampToRange(_algo.ExpectedLeadCount, (int)nudExpectedLead.Minimum, (int)nudExpectedLead.Maximum);

                nudMinBodyWidthRatio.Value = ToDecimalClamped(_algo.MinBodyWidthRatio, nudMinBodyWidthRatio.Minimum, nudMinBodyWidthRatio.Maximum);
                nudMinBodyHeightRatio.Value = ToDecimalClamped(_algo.MinBodyHeightRatio, nudMinBodyHeightRatio.Minimum, nudMinBodyHeightRatio.Maximum);

                nudMinLeadArea.Value = ClampToRange(_algo.MinLeadArea, (int)nudMinLeadArea.Minimum, (int)nudMinLeadArea.Maximum);
                nudMinLeadAspectRatio.Value = ToDecimalClamped(_algo.MinLeadAspectRatio, nudMinLeadAspectRatio.Minimum, nudMinLeadAspectRatio.Maximum);
                nudMaxLeadWidthRatioToBody.Value = ToDecimalClamped(_algo.MaxLeadWidthRatioToBody, nudMaxLeadWidthRatioToBody.Minimum, nudMaxLeadWidthRatioToBody.Maximum);
                nudLeadAttachGap.Value = ClampToRange(_algo.LeadAttachMaxGapPx, (int)nudLeadAttachGap.Minimum, (int)nudLeadAttachGap.Maximum);

                // ---------- Sub(위치불량) 신규 파라미터 ----------
                nudSubMinArea.Value = ClampToRange(_algo.SubBodyMinAreaPx, (int)nudSubMinArea.Minimum, (int)nudSubMinArea.Maximum);
                nudSubAngleTol.Value = ToDecimalClamped(_algo.SubAngleTolDeg, nudSubAngleTol.Minimum, nudSubAngleTol.Maximum);
                nudSubOffsetX.Value = ToDecimalClamped(_algo.SubOffsetXTolPx, nudSubOffsetX.Minimum, nudSubOffsetX.Maximum);
                nudSubOffsetY.Value = ToDecimalClamped(_algo.SubOffsetYTolPx, nudSubOffsetY.Minimum, nudSubOffsetY.Maximum);
                nudSubOpenK.Value = ClampToRange(_algo.SubOpenK, (int)nudSubOpenK.Minimum, (int)nudSubOpenK.Maximum);
                nudSubCloseK.Value = ClampToRange(_algo.SubCloseK, (int)nudSubCloseK.Minimum, (int)nudSubCloseK.Maximum);

                ToggleEnabledByUseAndRole();
            }
            finally
            {
                _updating = false;
            }
        }

        public void GetProperty()
        {
            if (_algo == null) return;

            _algo.IsUse = chkUse.Checked;

            // ---------- 기존 파라미터 ----------
            _algo.BodyDarkThresholdPercentile = (int)nudBodyDarkPct.Value;
            _algo.LeadBrightThresholdPercentile = (int)nudLeadBrightPct.Value;
            _algo.ExpectedLeadCount = (int)nudExpectedLead.Value;

            _algo.MinBodyWidthRatio = (double)nudMinBodyWidthRatio.Value;
            _algo.MinBodyHeightRatio = (double)nudMinBodyHeightRatio.Value;

            _algo.MinLeadArea = (int)nudMinLeadArea.Value;
            _algo.MinLeadAspectRatio = (double)nudMinLeadAspectRatio.Value;
            _algo.MaxLeadWidthRatioToBody = (double)nudMaxLeadWidthRatioToBody.Value;
            _algo.LeadAttachMaxGapPx = (int)nudLeadAttachGap.Value;

            // ---------- Sub(위치불량) 신규 파라미터 ----------
            _algo.SubBodyMinAreaPx = (int)nudSubMinArea.Value;
            _algo.SubAngleTolDeg = (double)nudSubAngleTol.Value;
            _algo.SubOffsetXTolPx = (double)nudSubOffsetX.Value;
            _algo.SubOffsetYTolPx = (double)nudSubOffsetY.Value;
            _algo.SubOpenK = (int)nudSubOpenK.Value;
            _algo.SubCloseK = (int)nudSubCloseK.Value;
        }

        private void WireEvents()
        {
            chkUse.CheckedChanged += (s, e) =>
            {
                if (_updating) return;
                ToggleEnabledByUseAndRole();
                ApplyAndNotify();
            };

            // 공통/기본
            nudBodyDarkPct.ValueChanged += (s, e) => OnParamValueChanged();
            nudLeadBrightPct.ValueChanged += (s, e) => OnParamValueChanged();
            nudExpectedLead.ValueChanged += (s, e) => OnParamValueChanged();

            // 바디
            nudMinBodyWidthRatio.ValueChanged += (s, e) => OnParamValueChanged();
            nudMinBodyHeightRatio.ValueChanged += (s, e) => OnParamValueChanged();

            // 리드
            nudMinLeadArea.ValueChanged += (s, e) => OnParamValueChanged();
            nudMinLeadAspectRatio.ValueChanged += (s, e) => OnParamValueChanged();
            nudMaxLeadWidthRatioToBody.ValueChanged += (s, e) => OnParamValueChanged();
            nudLeadAttachGap.ValueChanged += (s, e) => OnParamValueChanged();

            // Sub(위치불량)
            nudSubMinArea.ValueChanged += (s, e) => OnParamValueChanged();
            nudSubAngleTol.ValueChanged += (s, e) => OnParamValueChanged();
            nudSubOffsetX.ValueChanged += (s, e) => OnParamValueChanged();
            nudSubOffsetY.ValueChanged += (s, e) => OnParamValueChanged();
            nudSubOpenK.ValueChanged += (s, e) => OnParamValueChanged();
            nudSubCloseK.ValueChanged += (s, e) => OnParamValueChanged();

            btnApply.Click += (s, e) => ApplyAndNotify();

            btnResetDefault.Click += (s, e) =>
            {
                if (_algo == null) return;
                SetDefaultsToUI();
                ApplyAndNotify();
            };
        }

        private void OnParamValueChanged()
        {
            if (_updating) return;
            ApplyAndNotify();
        }

        private void ApplyAndNotify()
        {
            if (_algo == null) return;

            GetProperty();

            // 알고리즘 쪽 이벤트(있으면)도 함께 올림
            _algo.NotifyParamsChanged();

            // Prop 자체 이벤트도 올림
            if (PropertyChanged != null)
                PropertyChanged(this, EventArgs.Empty);
        }

        private void ToggleEnabledByUseAndRole()
        {
            bool enabled = chkUse.Checked;

            // 사용 여부에 따른 Enabled
            grpBasic.Enabled = enabled;
            grpBody.Enabled = enabled;
            grpLead.Enabled = enabled;
            grpPose.Enabled = enabled;
            chkUse.Enabled = true;

            if (_algo == null) return;

            // 역할별 Visible
            // Basic은 공통으로 항상 보이게
            grpBasic.Visible = true;

            if (_algo.TargetRole == TransistorRoiRole.Base)
            {
                grpBody.Visible = true;
                grpLead.Visible = false;
                grpPose.Visible = false;
            }
            else if (_algo.TargetRole == TransistorRoiRole.Body)
            {
                grpBody.Visible = true;  // 바디를 찾아야 리드 영역을 잡는 구조면 필요
                grpLead.Visible = true;
                grpPose.Visible = false;
            }
            else // Sub
            {
                grpBody.Visible = false;
                grpLead.Visible = false;
                grpPose.Visible = true;
            }
        }

        private void SetDefaultsToUI()
        {
            _updating = true;
            try
            {
                // ---------- 기존 기본값 ----------
                nudBodyDarkPct.Value = 30;
                nudLeadBrightPct.Value = 80;
                nudExpectedLead.Value = 3;

                nudMinBodyWidthRatio.Value = 0.25m;
                nudMinBodyHeightRatio.Value = 0.20m;

                nudMinLeadArea.Value = 250;
                nudMinLeadAspectRatio.Value = 2.0m;
                nudMaxLeadWidthRatioToBody.Value = 0.35m;
                nudLeadAttachGap.Value = 35;

                // ---------- Sub 기본값 ----------
                nudSubMinArea.Value = 2000;
                nudSubAngleTol.Value = 8.0m;
                nudSubOffsetX.Value = 25.0m;
                nudSubOffsetY.Value = 25.0m;
                nudSubOpenK.Value = 3;
                nudSubCloseK.Value = 15;
            }
            finally
            {
                _updating = false;
            }
        }

        // ---------------- UI building (designer-less) ----------------

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;

            chkUse = new CheckBox
            {
                Text = "사용",
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            grpBasic = new GroupBox { Text = "기본", Dock = DockStyle.Fill };
            grpBody = new GroupBox { Text = "바디(검정) 검출", Dock = DockStyle.Fill };
            grpLead = new GroupBox { Text = "리드(다리) 검출", Dock = DockStyle.Fill };
            grpPose = new GroupBox { Text = "위치 불량(Sub)", Dock = DockStyle.Fill };

            // --- numeric controls ---
            nudBodyDarkPct = NewNudInt(0, 100, 1);
            nudLeadBrightPct = NewNudInt(0, 100, 1);
            nudExpectedLead = NewNudInt(1, 10, 1);

            nudMinBodyWidthRatio = NewNudDec(0.05m, 1.00m, 0.01m, 2);
            nudMinBodyHeightRatio = NewNudDec(0.05m, 1.00m, 0.01m, 2);

            nudMinLeadArea = NewNudInt(0, 100000, 10);
            nudMinLeadAspectRatio = NewNudDec(0.5m, 50.0m, 0.1m, 1);
            nudMaxLeadWidthRatioToBody = NewNudDec(0.05m, 2.00m, 0.01m, 2);
            nudLeadAttachGap = NewNudInt(0, 300, 1);

            // Sub(위치불량)
            nudSubMinArea = NewNudInt(0, 500000, 100);
            nudSubAngleTol = NewNudDec(0.0m, 90.0m, 0.1m, 1);
            nudSubOffsetX = NewNudDec(0.0m, 2000.0m, 1.0m, 1);
            nudSubOffsetY = NewNudDec(0.0m, 2000.0m, 1.0m, 1);
            nudSubOpenK = NewNudInt(1, 99, 1);
            nudSubCloseK = NewNudInt(1, 199, 1);

            btnApply = new Button { Text = "적용", Dock = DockStyle.Right, Width = 80 };
            btnResetDefault = new Button { Text = "기본값", Dock = DockStyle.Right, Width = 80 };

            // Root layout
            root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = false
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            // Top bar: chkUse + buttons
            TableLayoutPanel topBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
            };
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            topBar.Controls.Add(chkUse, 0, 0);
            topBar.Controls.Add(btnResetDefault, 1, 0);
            topBar.Controls.Add(btnApply, 2, 0);

            // Basic group content
            grpBasic.Controls.Add(BuildFormTable(new (string, Control)[]
            {
                ("밝기 퍼센타일(바디)", nudBodyDarkPct),
                ("밝기 퍼센타일(리드)", nudLeadBrightPct),
                ("리드 기대 개수", nudExpectedLead),
            }));

            // Body group content
            grpBody.Controls.Add(BuildFormTable(new (string, Control)[]
            {
                ("최소 바디 폭 비율", nudMinBodyWidthRatio),
                ("최소 바디 높이 비율", nudMinBodyHeightRatio),
            }));

            // Lead group content
            grpLead.Controls.Add(BuildFormTable(new (string, Control)[]
            {
                ("최소 리드 면적", nudMinLeadArea),
                ("최소 종횡비(높이/폭)", nudMinLeadAspectRatio),
                ("최대 리드 폭/바디폭", nudMaxLeadWidthRatioToBody),
                ("바디 하단 연결 허용(px)", nudLeadAttachGap),
            }));

            // Pose group content
            grpPose.Controls.Add(BuildFormTable(new (string, Control)[]
            {
                ("최소 바디 면적(px)", nudSubMinArea),
                ("허용 각도(deg)", nudSubAngleTol),
                ("허용 X 이탈(px)", nudSubOffsetX),
                ("허용 Y 이탈(px)", nudSubOffsetY),
                ("열림 커널 크기", nudSubOpenK),
                ("닫힘 커널 크기", nudSubCloseK),
            }));

            root.Controls.Add(topBar, 0, 0);
            root.Controls.Add(grpBasic, 0, 1);
            root.Controls.Add(grpBody, 0, 2);
            root.Controls.Add(grpLead, 0, 3);
            root.Controls.Add(grpPose, 0, 4);

            this.Controls.Add(root);
        }

        private static TableLayoutPanel BuildFormTable((string label, Control control)[] rows)
        {
            TableLayoutPanel t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = rows.Length,
                Padding = new Padding(8),
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

            for (int i = 0; i < rows.Length; i++)
            {
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

                Label lab = new Label
                {
                    Text = rows[i].label,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                };

                rows[i].control.Dock = DockStyle.Fill;

                t.Controls.Add(lab, 0, i);
                t.Controls.Add(rows[i].control, 1, i);
            }
            return t;
        }

        private static NumericUpDown NewNudInt(int min, int max, int inc)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = inc,
                DecimalPlaces = 0
            };
        }

        private static NumericUpDown NewNudDec(decimal min, decimal max, decimal inc, int decimalPlaces)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = inc,
                DecimalPlaces = decimalPlaces
            };
        }

        private static int ClampToRange(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static decimal ToDecimalClamped(double v, decimal min, decimal max)
        {
            decimal d;
            try
            {
                d = Convert.ToDecimal(v, CultureInfo.InvariantCulture);
            }
            catch
            {
                d = min;
            }

            if (d < min) return min;
            if (d > max) return max;
            return d;
        }
    }
}

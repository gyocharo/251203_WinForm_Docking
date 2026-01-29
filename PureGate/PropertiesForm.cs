using PureGate.Algorithm;
using PureGate.Core;
using PureGate.Property;
using PureGate.Teach;
using PureGate.UIControl;
using OpenCvSharp;
using OpenCvSharp.Extensions;
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
    public partial class PropertiesForm : DockContent
    {

        Dictionary<string, TabPage> _allTabs = new Dictionary<string, TabPage>();

        public PropertiesForm()
        {
            InitializeComponent();
            LoadOptionControl(InspectType.InspAIModule);

            this.Text = "";        // 캡션 텍스트 제거
            this.TabText = "";

            ShowAIModuleOnly();
        }

        // AIModuleProp만 출력하기 위함
        public void ShowAIModuleOnly()
        {
            ResetProperty();
            LoadOptionControl(InspectType.InspAIModule);
        }

        private void LoadOptionControl(InspectType inspType)
        {
            string tabName = inspType.ToString();

            // 이미 있는 TabPage인지 확인
            foreach (TabPage tabPage in tabPropControl1.TabPages)
            {
                if (tabPage.Text == tabName)
                    return;
            }

            // 딕셔너리에 있으면 추가
            if (_allTabs.TryGetValue(tabName, out TabPage page))
            {
                tabPropControl1.TabPages.Add(page);
                return;
            }

            // 새로운 UserControl 생성
            UserControl _inspProp = CreateUserControl(inspType);
            if (_inspProp == null)
                return;

            // 새 탭 추가
            TabPage newTab = new TabPage(tabName)
            {
                Dock = DockStyle.Fill
            };
            _inspProp.Dock = DockStyle.Fill;
            newTab.Controls.Add(_inspProp);
            tabPropControl1.TabPages.Add(newTab);
            tabPropControl1.SelectedTab = newTab; // 새 탭 선택

            _allTabs[tabName] = newTab;
        }

        private UserControl CreateUserControl(InspectType inspPropType, InspAlgorithm algo = null)
        {
            UserControl curProp = null;
            switch (inspPropType)
            {
                //#11_MATCHING#5 패턴매칭 속성창 추가
                case InspectType.InspMatch:
                    MatchInspProp matchProp = new MatchInspProp();
                    matchProp.PropertyChanged += PropertyChanged;
                    curProp = matchProp;
                    break;
                case InspectType.InspAIModule:
                    AIModuleProp aiModuleProp = new AIModuleProp();
                    curProp = aiModuleProp;
                    break;
                case InspectType.InspTransistorRule:
                    var trProp = new PureGate.Property.TransistorRuleProp();
                    trProp.PropertyChanged += PropertyChanged; // RedrawMainView 호출되는 기존 핸들러
                    curProp = trProp;
                    break;
                default:
                    // MsgBox.Show("유효하지 않은 옵션입니다.");
                    return null; // 처리 안 하는 InspectType은 탭 생성 스킵
            }
            return curProp;
        }

        public void ShowProperty(InspWindow window)
        {
            LoadOptionControl(InspectType.InspAIModule);

            if (window == null)
                return;

            foreach (InspAlgorithm algo in window.AlgorithmList)
            {
                LoadOptionControl(algo.InspectType);
            }
        }

        public void ResetProperty()
        {
            tabPropControl1.TabPages.Clear();
            LoadOptionControl(InspectType.InspAIModule);
        }

        public void UpdateProperty(InspWindow window)
        {
            if (window is null)
                return;

            foreach (TabPage tabPage in tabPropControl1.TabPages)
            {
                if (tabPage.Controls.Count > 0)
                {
                    UserControl uc = tabPage.Controls[0] as UserControl;

                   if (uc is MatchInspProp matchProp)
                    {
                        MatchAlgorithm matchAlgo = (MatchAlgorithm)window.FindInspAlgorithm(InspectType.InspMatch);
                        if (matchAlgo is null)
                            continue;

                        window.PatternLearn();

                        matchProp.SetAlgorithm(matchAlgo);
                    }
                    else if (uc is PureGate.Property.TransistorRuleProp trProp)
                    {
                        var algo = (TransistorRuleAlgorithm)window.FindInspAlgorithm(InspectType.InspTransistorRule);
                        if (algo != null)
                            trProp.SetAlgorithm(algo);
                    }
                }
            }
        }

        private void PropertyChanged(object sender, EventArgs e)
        {
            Global.Inst.InspStage.RedrawMainView();
        }

    }
}

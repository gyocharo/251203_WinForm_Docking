using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PureGate.Core;
using PureGate.Setting;

namespace PureGate.UIControl
{
    public enum ToolbarButton
    {
        ShowROI,
        SetROI,
        ChannelColor,
        ChannelGray,
        ChannelRed,
        ChannelBlue,
        ChannelGreen
    }

    public partial class MainViewToolbar : UserControl
    {
        private ToolStripDropDownButton _dropDownButton;
        private ToolStripButton _showROIButton;
        private ToolStripButton _setROIButton;

        #region Events
        public event EventHandler<ToolbarEventArgs> ButtonChanged;
        #endregion

        public MainViewToolbar()
        {
            InitializeComponent(); // 디자이너 지원
            BuildToolbar();
        }

        private void BuildToolbar()
        {
            var bar = new ToolStrip
            {
                Dock = DockStyle.Fill,
                GripStyle = ToolStripGripStyle.Hidden,
                LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow,
                AutoSize = false,
                Width = 32,
                Padding = new Padding(2),
                ImageList = imageListToolbar
            };

            ToolStripButton IconButton(string key, string tip, EventHandler onClick = null, bool toggle = false)
            {
                var b = new ToolStripButton
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    ImageKey = key,
                    ImageScaling = ToolStripItemImageScaling.None,
                    AutoSize = true,
                    Width = 32,
                    Height = 32,
                    CheckOnClick = toggle,
                    ToolTipText = tip
                };
                if (onClick != null) b.Click += onClick;
                return b;
            }

            _showROIButton = IconButton("ShowROI", "ROI보기", (s, e) => OnShowROI(), toggle: true);

            // ✅ SetROI는 토글 의미보다 "ROI 생성" 트리거로 쓰는 게 목적이라
            //    메뉴 띄운 뒤 체크 상태는 원래대로 유지(또는 필요시 외부에서 제어)
            _setROIButton = IconButton("SetROI", "ROI 설정", (s, e) => OnSetROI_ShowRoleMenu(), toggle: true);
            _setROIButton.Image = PureGate.Properties.Resources.SetROI;
            _setROIButton.DisplayStyle = ToolStripItemDisplayStyle.Image;

            // ───────────────── Channel DropDown ─────────────────
            _dropDownButton = new ToolStripDropDownButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = imageListToolbar.Images["Color"],
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "Channel"
            };

            void AddChannel(string name)
            {
                var item = new ToolStripMenuItem(name, imageListToolbar.Images[name], (s, e) =>
                {
                    ToolbarButton toolbarButton = ToolbarButton.ChannelGray;
                    if ("Color" == name) toolbarButton = ToolbarButton.ChannelColor;
                    else if ("Red" == name) toolbarButton = ToolbarButton.ChannelRed;
                    else if ("Green" == name) toolbarButton = ToolbarButton.ChannelGreen;
                    else if ("Blue" == name) toolbarButton = ToolbarButton.ChannelBlue;

                    OnSelectChannel(toolbarButton);
                    _dropDownButton.Image = imageListToolbar.Images[name];
                })
                {
                    ImageScaling = ToolStripItemImageScaling.None
                };

                _dropDownButton.DropDownItems.Add(item);
            }

            AddChannel("Color");
            AddChannel("Gray");
            AddChannel("Red");
            AddChannel("Blue");
            AddChannel("Green");

            bar.Items.AddRange(new ToolStripItem[]
            {
                _showROIButton,
                _setROIButton,
                new ToolStripSeparator(),
                _dropDownButton
            });

            Controls.Add(bar);
        }

        #region ROI / Channel Handlers

        private void OnShowROI()
        {
            ButtonChanged?.Invoke(this, new ToolbarEventArgs(ToolbarButton.ShowROI, _showROIButton.Checked));
        }

        /// <summary>
        /// ✅ SetROI 버튼 클릭 시 ROI 역할 선택 메뉴 표시
        /// </summary>
        private void OnSetROI_ShowRoleMenu()
        {
            // 메뉴를 띄우는 순간, CheckOnClick 토글이 바뀌었을 수 있음
            // "ROI 생성 모드"로 토글 유지할지, 클릭 트리거로만 쓸지는 프로젝트 정책에 따라 결정.
            // 여기서는 기존 토글 동작을 유지하고, 역할은 메뉴에서 선택하도록 구현.
            var menu = new ContextMenuStrip();

            menu.Items.Add("Base", null, (_, __) => RaiseSetRoiRole(TransistorRoiRole.Base));
            menu.Items.Add("Body", null, (_, __) => RaiseSetRoiRole(TransistorRoiRole.Body));
            menu.Items.Add("Sub", null, (_, __) => RaiseSetRoiRole(TransistorRoiRole.Sub));

            // 필요하면 활성화
            // menu.Items.Add("Lead", null, (_, __) => RaiseSetRoiRole(TransistorRoiRole.Lead));

            // 버튼 아래에 뜨게
            var pt = _setROIButton.Owner.PointToScreen(new Point(_setROIButton.Bounds.Left, _setROIButton.Bounds.Bottom));
            menu.Show(pt);
        }

        private void RaiseSetRoiRole(TransistorRoiRole role)
        {
            ButtonChanged?.Invoke(this, new ToolbarEventArgs(ToolbarButton.SetROI, _setROIButton.Checked, role));
        }

        private void OnSelectChannel(ToolbarButton buttonType)
        {
            ButtonChanged?.Invoke(this, new ToolbarEventArgs(buttonType, false));
        }

        #endregion

        public void SetSelectButton(eImageChannel channel)
        {
            SelectChannel(channel.ToString());
        }

        private void SelectChannel(string name)
        {
            if (_dropDownButton is null) return;

            var menuItem = _dropDownButton.DropDownItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Text == name);

            if (menuItem == null) return;

            _dropDownButton.Image = menuItem.Image;

            ToolbarButton mappedButton = ToolbarButton.ChannelGray;
            if (Enum.TryParse("Channel" + name, out ToolbarButton result))
                mappedButton = result;

            OnSelectChannel(mappedButton);
        }

        public void SetSetRoiChecked(bool isChecked)
        {
            if (_setROIButton != null)
                _setROIButton.Checked = isChecked;
        }

        public void SetShowRoiChecked(bool isChecked)
        {
            if (_showROIButton != null)
                _showROIButton.Checked = isChecked;
        }
    }

    /// <summary>
    /// ✅ 기존 이벤트 구조를 최대한 유지하면서 RoiRole만 확장
    /// </summary>
    public class ToolbarEventArgs : EventArgs
    {
        public ToolbarButton Button { get; }
        public bool IsChecked { get; }

        // ✅ SetROI일 때만 값이 들어감
        public TransistorRoiRole? RoiRole { get; }

        public ToolbarEventArgs(ToolbarButton button, bool isChecked, TransistorRoiRole? roiRole = null)
        {
            Button = button;
            IsChecked = isChecked;
            RoiRole = roiRole;
        }
    }
}

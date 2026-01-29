using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureGate.Core;
using System.Drawing.Imaging;
using PureGate.Util;
using System.IO;
using PureGate.Setting;
using System.Runtime.Remoting.Channels;

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
        private ToolStripDropDownButton _setROIButton;  // ✅ ToolStripButton → ToolStripDropDownButton 변경

        #region Events

        public event EventHandler<ToolbarEventArgs> ButtonChanged;
        
        // ✅ 추가: ROI 타입 선택 이벤트
        public event EventHandler<RoiTypeSelectedEventArgs> RoiTypeSelected;

        #endregion
        
        public MainViewToolbar()
        {
            InitializeComponent();
            BuildToolbar();
        }

        private void BuildToolbar()
        {
            // ───────────────── ToolStrip ─────────────────
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

            // ───────────────── Helper ─────────────────
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

            // ───────────────── Buttons ─────────────────
            _showROIButton = IconButton("ShowROI", "ROI보기", (s, e) => OnShowROI(), toggle: true);
            
            // ✅ SetROI를 DropDown으로 변경
            _setROIButton = new ToolStripDropDownButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = PureGate.Properties.Resources.SetROI,
                ImageScaling = ToolStripItemImageScaling.None,
                ToolTipText = "ROI 타입 선택",
                AutoSize = true,
                Width = 32,
                Height = 32
            };

            // ✅ InspWindowType enum의 모든 값을 메뉴로 추가
            var windowTypes = Enum.GetValues(typeof(InspWindowType))
                .Cast<InspWindowType>()
                .Where(t => t != InspWindowType.None)  // None은 제외
                .ToList();

            foreach (InspWindowType windowType in windowTypes)
            {
                var menuItem = new ToolStripMenuItem(windowType.ToString(), null, OnRoiTypeClick)
                {
                    Tag = windowType,  // WindowType 저장
                    ImageScaling = ToolStripItemImageScaling.None
                };
                _setROIButton.DropDownItems.Add(menuItem);
            }

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
                    if ("Color" == name)
                        toolbarButton = ToolbarButton.ChannelColor;
                    else if ("Red" == name)
                        toolbarButton = ToolbarButton.ChannelRed;
                    else if ("Green" == name)
                        toolbarButton = ToolbarButton.ChannelGreen;
                    else if ("Blue" == name)
                        toolbarButton = ToolbarButton.ChannelBlue;

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

            // ───────────────── Assemble ─────────────────
            bar.Items.AddRange(new ToolStripItem[]
            {
                _showROIButton,
                _setROIButton,
                new ToolStripSeparator(),
                _dropDownButton
            });

            Controls.Add(bar);
        }

        #region Event Handlers
        
        private void OnShowROI()
        {
            ButtonChanged?.Invoke(this, new ToolbarEventArgs(ToolbarButton.ShowROI, _showROIButton.Checked));
        }

        // ✅ ROI 타입 선택 핸들러
        private void OnRoiTypeClick(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is InspWindowType windowType)
            {
                // ✅ ROI 타입 선택 이벤트 발생
                RoiTypeSelected?.Invoke(this, new RoiTypeSelectedEventArgs(windowType));
                
                // SetROI 버튼 체크 상태로 변경 (ROI 그리기 모드 활성화)
                ButtonChanged?.Invoke(this, new ToolbarEventArgs(ToolbarButton.SetROI, true));
            }
        }
        
        private void OnSelectChannel(ToolbarButton buttonType)
        {
            ButtonChanged?.Invoke(this, new ToolbarEventArgs(buttonType, false));
        }
        
        #endregion

        public void SetSelectButton(eImageChannel channel)
        {
            string name = channel.ToString();
            SelectChannel(name);
        }

        private void SelectChannel(string name)
        {
            if (_dropDownButton is null)
                return;

            var menuItem = _dropDownButton.DropDownItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Text == name);

            if (menuItem == null)
                return;

            _dropDownButton.Image = menuItem.Image;

            ToolbarButton mappedButton = ToolbarButton.ChannelGray;
            if (Enum.TryParse("Channel" + name, out ToolbarButton result))
                mappedButton = result;

            OnSelectChannel(mappedButton);
        }

        public void SetSetRoiChecked(bool isChecked)
        {
            // DropDown 버튼은 Checked 속성이 없으므로 제거 또는 주석 처리
            // 필요시 다른 방식으로 상태 표시 (예: 배경색 변경)
        }

        public void SetShowRoiChecked(bool isChecked)
        {
            if (_showROIButton != null)
                _showROIButton.Checked = isChecked;
        }
    }

    public class ToolbarEventArgs : EventArgs
    {
        public ToolbarButton Button { get; }
        public bool IsChecked { get; }

        public ToolbarEventArgs(ToolbarButton button, bool isChecked)
        {
            Button = button;
            IsChecked = isChecked;
        }
    }

    // ✅ ROI 타입 선택 이벤트 Args
    public class RoiTypeSelectedEventArgs : EventArgs
    {
        public InspWindowType WindowType { get; }

        public RoiTypeSelectedEventArgs(InspWindowType windowType)
        {
            WindowType = windowType;
        }
    }
}

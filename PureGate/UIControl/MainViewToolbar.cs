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

    /*
    #18_IMAGE_CHANNEL# - <<<이미지 채널 설정 기능>>> 
    검사에서 이미지 채널을 사용할 수 있도록 설정 기능 추가
    1) UIControl / MainViewToolbar 유저컨트롤 생성
    2) CameraFrom에 MainViewToolbar 컨트롤 추가
    3) #18_IMAGE_CHANNEL#1 ~ 14
    */

    public enum ToolbarButton
    {
        ChannelColor,
        ChannelGray,
        ChannelRed,
        ChannelBlue,
        ChannelGreen
    }

    public partial class MainViewToolbar : UserControl
    {
        private ToolStripDropDownButton _dropDownButton;

        #region Events

        public event EventHandler<ToolbarEventArgs> ButtonChanged;

        #endregion
        public MainViewToolbar()
        {
            InitializeComponent();                 // 디자이너 지원 (Optional)
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

                    if (name == "Color") toolbarButton = ToolbarButton.ChannelColor;
                    else if (name == "Red") toolbarButton = ToolbarButton.ChannelRed;
                    else if (name == "Green") toolbarButton = ToolbarButton.ChannelGreen;
                    else if (name == "Blue") toolbarButton = ToolbarButton.ChannelBlue;

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

            bar.Items.Add(_dropDownButton);
            Controls.Add(bar);
        }

        #region Sample Handlers        
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

            // 메뉴 항목에서 이름이 일치하는 항목 찾기
            var menuItem = _dropDownButton.DropDownItems
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(i => i.Text == name);

            if (menuItem == null)
                return;

            // 버튼 이미지도 선택된 것으로 변경
            _dropDownButton.Image = menuItem.Image;

            // 버튼 타입 매핑해서 이벤트 발생
            ToolbarButton mappedButton = ToolbarButton.ChannelGray; // 기본값
            if (Enum.TryParse("Channel" + name, out ToolbarButton result))
                mappedButton = result;

            OnSelectChannel(mappedButton);
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
}

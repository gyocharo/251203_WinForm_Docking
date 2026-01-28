using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PureGate.UIControl;

namespace PureGate.UIControl
{
    public static class MsgBox
    {
        private static CustomMessageBox.MsgKind ToKind(MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
                return CustomMessageBox.MsgKind.Confirm;

            if (icon == MessageBoxIcon.Error || icon == MessageBoxIcon.Hand || icon == MessageBoxIcon.Stop)
                return CustomMessageBox.MsgKind.Error;

            if (icon == MessageBoxIcon.Warning || icon == MessageBoxIcon.Exclamation)
                return CustomMessageBox.MsgKind.Warning;

            if (icon == MessageBoxIcon.Information || icon == MessageBoxIcon.Asterisk)
                return CustomMessageBox.MsgKind.Info;

            return CustomMessageBox.MsgKind.Notice;
        }

        // owner가 null이거나, 보이지 않거나, Dispose된 폼이면
        // 현재 화면에 떠 있는 최상단 폼(대개 LoadingForm)을 owner로 자동 선택
        private static IWin32Window ResolveOwner(IWin32Window owner)
        {
            if (owner is Form f)
            {
                if (!f.Visible || f.IsDisposed)
                    owner = null;
            }

            if (owner == null)
            {
                for (int i = Application.OpenForms.Count - 1; i >= 0; i--)
                {
                    var top = Application.OpenForms[i];
                    if (top != null && !top.IsDisposed && top.Visible)
                        return top;
                }
            }

            return owner;
        }

        // ---- No-owner overloads ----
        public static DialogResult Show(string text)
            => CustomMessageBox.Show(ResolveOwner(null), CustomMessageBox.MsgKind.Notice, text);

        public static DialogResult Show(string text, string caption)
            => CustomMessageBox.Show(ResolveOwner(null), CustomMessageBox.MsgKind.Notice, text, title: caption);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
            => Show(null, text, caption, buttons, MessageBoxIcon.None);

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
            => Show(null, text, caption, buttons, icon);

        // ---- Owner overloads ----
        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons)
            => Show(owner, text, caption, buttons, MessageBoxIcon.None);

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            owner = ResolveOwner(owner);

            var kind = ToKind(buttons, icon);

            if (kind == CustomMessageBox.MsgKind.Confirm)
                return CustomMessageBox.Show(owner, CustomMessageBox.MsgKind.Confirm, text, title: caption);

            return CustomMessageBox.Show(owner, kind, text, title: caption);
        }
    }
}
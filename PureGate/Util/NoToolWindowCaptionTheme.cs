using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

public class NoToolWindowCaptionTheme : WeifenLuo.WinFormsUI.Docking.VS2015BlueTheme
{
    public NoToolWindowCaptionTheme() : base()
    {
        Extender.DockPaneCaptionFactory = new NoToolWindowCaptionFactory();
    }

    private class NoToolWindowCaptionFactory
        : DockPanelExtender.IDockPaneCaptionFactory
    {
        public DockPaneCaptionBase CreateDockPaneCaption(DockPane pane)
        {
            return new NoToolWindowCaption(pane);
        }
    }

    private class NoToolWindowCaption : DockPaneCaptionBase
    {
        public NoToolWindowCaption(DockPane pane) : base(pane)
        {
            Visible = false;
            Height = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 아무 것도 그리지 않음
        }

        protected override int MeasureHeight()
        {
            return 0;
        }
    }
}
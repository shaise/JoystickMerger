using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SuncomControllerMerge
{
    public partial class MainForm : Form
    {
        Timer pollTimer;
        GameDevPoller gamePoller;
        bool joyReady;
        public MainForm()
        {
            InitializeComponent();
            gamePoller = new GameDevPoller();
            joyReady = false;
        }

        public Label LblVjoyStat
        {
            get { return lblVjoyStat; }
        }
        public Label LblJoystickStat
        {
            get { return lblJoystickStat; }
        }
        public Label LblThrottleStat
        {
            get { return lblThrottleStat; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            gamePoller.Init(this);
            pollTimer = new Timer();
            pollTimer.Tick += new EventHandler(pollTimer_Tick);
            pollTimer.Interval = 500;
            pollTimer.Start();
        }

        void pollTimer_Tick(object sender, EventArgs e)
        {
            if (joyReady)
            {
                joyReady = gamePoller.Poll();
                if (!joyReady)
                {
                    pollTimer.Stop();
                    if (WindowState == FormWindowState.Minimized)
                        notifyIcon1.ShowBalloonTip(3000, "SuncomControllerMerge", "One of the Suncom controllers was disconnected", ToolTipIcon.None);
                    pollTimer.Interval = 500;
                    pollTimer.Start();
                }
            }
            else if (gamePoller.ValidateDeviceExistance())
            {
                pollTimer.Stop();
                pollTimer.Interval = 10;
                joyReady = true;
                if (WindowState != FormWindowState.Minimized)
                    WindowState = FormWindowState.Minimized;
                else
                    notifyIcon1.ShowBalloonTip(3000, "SuncomControllerMerge", "SuncomControllerMerge is back on-line", ToolTipIcon.None);
                pollTimer.Start();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(1000);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowInTaskbar = true;
            notifyIcon1.Visible = false;
            WindowState = FormWindowState.Normal;
        }

    }
}

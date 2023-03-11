using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using KLC;

namespace KLC_Finch
{
    public class Connection
    {

        public ConnectionGroupItem Control { get; private set; }
        public string Label { get; set; }
        public string GroupName { get; set; }
        public bool IsReal { get; private set; }
        public int Attempt { get; private set; }
        public LiveConnectSession LCSession;
        private WindowAlternative WinAlternative;
        private IRemoteControl rc;
        public IRemoteControl RC {
            get
            {
                if (IsReal)
                    return LCSession.ModuleRemoteControl;
                else
                    return rc;
            }
        }

        public Connection(RemoteControlTest test, string label="Test")
        {
            IsReal = false;
            LCSession = null;
            rc = test;
            Label = label;
            GroupName = "Test";

            Control = new ConnectionGroupItem(this);
        }

        public Connection(LiveConnectSession session, string label, WindowAlternative winAlt=null)
        {
            IsReal = true;
            LCSession = session;
            Label = label;
            GroupName = session.agent.MachineGroupReverse;

            //if (winAlt == null)
                //WinAlternative = new WindowAlternative(session);
            //else
            WinAlternative = winAlt;
            Control = new ConnectionGroupItem(this);
        }

        public void ShowAlternativeWindow(System.Windows.Controls.Control origin, int x = 0)
        {
            Window win = (App.winCharm != null ? App.winCharm : App.winStandaloneViewer);
            if (win == null || WinAlternative == null)
                return;

            if (WinAlternative.WindowState == WindowState.Minimized)
                WinAlternative.WindowState = WindowState.Normal;
            WinAlternative.Visibility = Visibility.Visible;
            WinAlternative.Show(); //Necessary if hadn't been drawn yet
            WinAlternative.Focus(); //Necessary to bring on top

            Point point = origin.TransformToAncestor(win).Transform(new Point(x, 0));

            if (win.WindowState == WindowState.Maximized)
            {
                IntPtr handle = new System.Windows.Interop.WindowInteropHelper(win).Handle;
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(handle);

                WinAlternative.Left = screen.WorkingArea.Left + point.X + 2;
                WinAlternative.Top = screen.WorkingArea.Top + SystemParameters.CaptionHeight + point.Y + 10;
            }
            else
            {
                WinAlternative.Left = win.Left + point.X + 8;
                WinAlternative.Top = win.Top + SystemParameters.CaptionHeight + point.Y + 15;
            }
        }

        public void Disconnect(int reason)
        {
            if(IsReal)
                WinAlternative.Disconnect(LCSession.RandSessionGuid, reason);
        }
    }
}

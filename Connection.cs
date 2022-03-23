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

            if (winAlt == null)
                WinAlternative = new WindowAlternative(session);
            else
                WinAlternative = winAlt;
            Control = new ConnectionGroupItem(this);
        }

        public void ShowAlternativeWindow()
        {
            if (WinAlternative == null)
                return;

            if (WinAlternative.WindowState == System.Windows.WindowState.Minimized)
                WinAlternative.WindowState = System.Windows.WindowState.Normal;
            WinAlternative.Visibility = Visibility.Visible;
            WinAlternative.Focus();

            if (App.winCharm != null)
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle((int)App.winCharm.Left, (int)App.winCharm.Top, (int)App.winCharm.Width, (int)App.winCharm.Height);
                foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
                {
                    if (screen.Bounds.IntersectsWith(rect))
                    {
                        //Placed in center of the screen that has RemoteControl or Charm window.
                        WinAlternative.Left = screen.Bounds.X + ((screen.Bounds.Width - WinAlternative.Width) / 2);
                        WinAlternative.Top = screen.Bounds.Y + ((screen.Bounds.Height - WinAlternative.Height) / 2);
                        break;
                    }
                }
            }
        }

        public void Disconnect(int reason)
        {
            if(IsReal)
                WinAlternative.Disconnect(LCSession.RandSessionGuid, reason);
        }
    }
}

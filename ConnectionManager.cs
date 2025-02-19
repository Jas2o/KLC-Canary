using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KLC;

namespace KLC_Finch {
    public static class ConnectionManager
    {
        private static List<ConnectionGroup> listGroup = new List<ConnectionGroup>();
        public static List<Connection> listConnection = new List<Connection>();
        public static Connection Active;
        public static ControlViewer Viewer;
        public static bool Alive = true;
        private static int TestNum = 1;

        /*
        public static Connection AddTest(string screenLayout, bool isMac, WindowAlternative.HasConnected callback)
        {
            Connection session = new Connection(new RemoteControlTest(screenLayout, isMac), "Test " + (TestNum++));
            listConnection.Add(session);
            Active = session;

            Viewer.rcSwitch = Active.RC;

            callback?.Invoke();

            return session;
        }
        */

        public static Connection AddTest(string screenLayout, bool isMac, WindowAlternative.StatusCallback callback)
        {
            FakeLiveConnectSession lc = new FakeLiveConnectSession();

            Connection session = new Connection(lc, new RemoteControlTest(screenLayout, isMac), "Test " + (TestNum++));
            listConnection.Add(session);
            Active = session;

            Viewer.rcSwitch = Active.RC;

            return session;
        }

        public static Connection AddReal(string agentID, string vsa, string shortToken, WindowAlternative.StatusCallback callbackS, WindowAlternative.ErrorCallback callbackE, WindowAlternative winAlt=null)
        {
            if (agentID == null || shortToken == null)
                return null;

            LiveConnectSession lc = new LiveConnectSession(vsa, shortToken, agentID, callbackS, callbackE);
            if (lc.Eirc == null)
            {
                lc = null;
                return null;
            }

            Connection session = new Connection(lc, lc.agent.AgentNameOnly, winAlt);
            listConnection.Add(session);
            Active = session;

            return session;
        }

        public static Connection AddSimulated(WindowAlternative.StatusCallback callbackS, WindowAlternative.ErrorCallback callbackE, WindowAlternative winAlt = null) {
            string screenLayoutExample3 = @"{""default_screen"":131073,""screens"":[{""screen_height"":900,""screen_id"":131073,""screen_name"":""\\\\.\\DISPLAY1"",""screen_width"":1600,""screen_x"":0,""screen_y"":0},{""screen_height"":1080,""screen_id"":1245327,""screen_name"":""\\\\.\\DISPLAY2"",""screen_width"":1920,""screen_x"":1615,""screen_y"":-741},{""screen_height"":1080,""screen_id"":196759,""screen_name"":""\\\\.\\DISPLAY3"",""screen_width"":1920,""screen_x"":-305,""screen_y"":-1080}]}";

            FakeLiveConnectSession lc = new FakeLiveConnectSession(callbackS, callbackE);

            Connection session = new Connection(lc, new RemoteControlTest(screenLayoutExample3, false), "Lanner");
            listConnection.Add(session);
            Active = session;

            //Viewer.rcSwitch = Active.RC;

            return session;
        }

        public static void Switch(Connection conn)
        {
            Active = conn;
            if (Active.RC != null)
                Viewer.rcSwitch = Active.RC;
        }

        public static void Disconnect(string sessionGuid, int reason)
        {
            //Reasons: 0 is WebSocketA, 1 is WebSocketB, 2 is user

            for(int i = 0; i < listConnection.Count; i++)
            {
                if(listConnection[i].IsReal)
                {
                    if (listConnection[i].LCSession.RandSessionGuid == sessionGuid)
                    {
                        listConnection[i].Disconnect(reason);
                    }
                }
            }
        }

        public static void DisconnectRC()
        {
            Connection conn = Active;
            if (conn == null || conn.RC == null)
                return;
            IRemoteControl rc = conn.RC;

            if (conn.IsReal)
            {
                conn.RC.Disconnect();
                //conn.Disconnect(ConnectionManager.Active.LCSession.RandSessionGuid, 2);
            }
            else
            {
                rc.Disconnect();
            }

            if (rc.IsPrivate) conn.Control.btnRCPrivate.Visibility = Visibility.Collapsed;
            else conn.Control.btnRCShared.Visibility = Visibility.Collapsed;
        }

        public static void DisconnectRC(IRemoteControl rc)
        {
            Connection conn = listConnection.Find(x => x.RC == rc);
            if (conn == null)
                return;

            if (conn.IsReal)
                conn.RC.Disconnect();
            else
                rc.Disconnect();

            //if (rc.IsPrivate) conn.Control.btnRCPrivate.Visibility = Visibility.Collapsed;
            //else conn.Control.btnRCShared.Visibility = Visibility.Collapsed;
        }

        public static ConnectionGroup GetConnectionGroup(string name)
        {
            return listGroup.FirstOrDefault(x => x.Name == name);
        }

        public static ConnectionGroup NewConnectionGroup(string name)
        {
            ConnectionGroup group = new ConnectionGroup(name);
            listGroup.Add(group);
            return group;
        }

        public static void DeleteGroup(ConnectionGroup group)
        {
            listGroup.Remove(group);
        }
    }
}

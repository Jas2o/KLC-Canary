using LibKaseya;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static LibKaseya.Enums;

namespace KLC_Finch
{
    /// <summary>
    /// Interaction logic for WindowCharm.xaml
    /// </summary>
    public partial class WindowCharm : Window
    {
        //private IRemoteControl rc;
        //private RCv rcv;
        //private RCstate state;

        public WindowCharm()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            ConnectionManager.Viewer = controlViewer;
            btnAlt.IsEnabled = btnRC.IsEnabled = btnRCClose.IsEnabled = false;

            WindowUtilities.ActivateWindow(this);
        }

        public delegate void HasConnected();
        public void ConnectDirect()
        {
            //session.ModuleRemoteControl = new RemoteControl(session, directToMode);
            Application.Current.Dispatcher.Invoke((Action)delegate {
                ConnectUpdateUI();
                //session.ModuleRemoteControl.Connect();

                this.Visibility = Visibility.Collapsed;
            });
        }
        public void ConnectNotDirect()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                ConnectUpdateUI();
            });
        }
        private void ConnectUpdateUI()
        {
            if (ConnectionManager.Active.IsReal)
            {
                btnRC.Content = ConnectionManager.Active.LCSession.agent.Name;
                btnAlt.IsEnabled = true;
            }
            else
            {
                btnRC.Content = ConnectionManager.Active.Label;
                btnAlt.IsEnabled = false;
            }

            btnRC.IsEnabled = true;
            btnRCClose.IsEnabled = (ConnectionManager.Active.RC != null && ConnectionManager.Active.RC.state.connectionStatus == ConnectionStatus.Connected);
        }

        private void btnAlt_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionManager.Active == null || !ConnectionManager.Active.IsReal)
                return;

            ConnectionManager.Active.ShowAlternativeWindow();
        }

        private void btnRC_Click(object sender, RoutedEventArgs e)
        {
            if(ConnectionManager.Active == null)
                return;

            tabRC.IsSelected = true; //OpenGL gets mad if it's not visible

            if (ConnectionManager.Active.IsReal)
            {
                if (ConnectionManager.Active.LCSession.WebsocketB == null || !ConnectionManager.Active.LCSession.WebsocketB.ControlAgentIsReady())
                    return;

                ConnectionManager.Active.LCSession.ModuleRemoteControl = new RemoteControl(ConnectionManager.Active.LCSession, RC.Shared);
                ConnectionManager.Active.LCSession.ModuleRemoteControl.ConnectV2(this);
            } else
            {
                RemoteControlTest rcTest = (RemoteControlTest)ConnectionManager.Active.RC;
                if(!rcTest.LoopIsRunning())
                    rcTest.LoopStart(controlViewer);
                //controlViewer.Start(ConnectionManager.Active.RC.state, LibKaseya.Agent.OSProfile.Other);
            }

            btnRCClose.IsEnabled = true;
        }

        private void btnRCClose_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionManager.Active == null)
                return;

            btnRCClose.IsEnabled = false;

            if (ConnectionManager.Active.IsReal) {

                ConnectionManager.Active.Disconnect(ConnectionManager.Active.LCSession.RandSessionGuid, 2);
            } else {
                ((RemoteControlTest)ConnectionManager.Active.RC).Disconnect("Test");
            }
        }

        private void btnSessionClose_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionManager.Active == null)
                return;

            if(ConnectionManager.Active.IsReal)
            {
                ConnectionManager.Active.LCSession.Close();
            } else {
                ((RemoteControlTest)ConnectionManager.Active.RC).LoopStop();
            }
        }

        private void btnSideCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (gridSide.Width > 50)
                gridSide.Width = 30;
            else
                gridSide.Width = 150;
        }

        private void tabRC_GotFocus(object sender, RoutedEventArgs e)
        {
            //Required for OpenGLWPF otherwise the keyboard doesn't work.
            controlViewer.Focus();
        }

        private void btnExampleTest_Click(object sender, RoutedEventArgs e)
        {
            WindowRCTest2 winTest = new WindowRCTest2()
            {
                Owner = this
            };
            winTest.ShowDialog();
            if (winTest.ReturnValue != null)
            {
                btnAlt.IsEnabled = btnRC.IsEnabled = false;

                bool directToRemoteControl = false;
                HasConnected callback = (directToRemoteControl ? new HasConnected(ConnectDirect) : new HasConnected(ConnectNotDirect));

                Connection conn = ConnectionManager.AddTest(winTest.ReturnValue, winTest.ReturnMac, callback);
                SetAltTabIfNoRC(conn);

                Button button = new Button()
                {
                    Content = conn.Label,
                    Tag = conn
                };
                button.Click += BtnActiveConnection_Click;
                stackActiveConnections.Children.Add(button);
            }
        }

        private void btnExampleThis_Click(object sender, RoutedEventArgs e)
        {
            string savedAuthToken = KaseyaAuth.GetStoredAuth();
            string val = "";

            Connection conn = null;
            try
            {
                using (RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    RegistryKey subkey = view32.OpenSubKey(@"SOFTWARE\Kaseya\Agent\AGENT11111111111111"); //Actually in WOW6432Node
                    if (subkey != null)
                        val = subkey.GetValue("AgentGUID").ToString();
                    subkey.Close();
                }

                if (val.Length > 0)
                {
                    btnAlt.IsEnabled = btnRC.IsEnabled = false;

                    bool directToRemoteControl = false;
                    HasConnected callback = (directToRemoteControl ? new HasConnected(ConnectDirect) : new HasConnected(ConnectNotDirect));

                    conn = ConnectionManager.AddReal(val, savedAuthToken, /*this,*/ callback);
                    SetAltTabIfNoRC(conn);
                }
            }
            catch (Exception)
            {
            }

            if(conn != null)
            {
                Button button = new Button()
                {
                    Content = conn.Label,
                    Tag = conn
                };
                button.Click += BtnActiveConnection_Click;
                stackActiveConnections.Children.Add(button);
            }
        }

        private void BtnActiveConnection_Click(object sender, RoutedEventArgs e)
        {
            Connection conn = (Connection)((Button)sender).Tag;
            ConnectionManager.Switch(conn);
            ConnectUpdateUI();

            SetAltTabIfNoRC(conn);
        }

        private void SetAltTabIfNoRC(Connection conn)
        {
            if (conn == null)
                return;

            if (conn.IsReal)
            {
                if (conn.RC == null || conn.RC.state.connectionStatus != ConnectionStatus.Connected)
                    tabAlt.IsSelected = true;
            }
            else
            {
                if (!((RemoteControlTest)conn.RC).LoopIsRunning())
                    tabAlt.IsSelected = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConnectionManager.Alive = false;
            Environment.Exit(0);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //e.Handled = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;
        }
    }
}

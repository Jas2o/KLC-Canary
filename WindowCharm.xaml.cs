using LibKaseya;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
        private readonly NamedPipeListener pipeListener;

        public WindowCharm()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            ConnectionManager.Viewer = controlViewer;
            btnAlt.IsEnabled = btnRCAdd.IsEnabled = btnRCClose.IsEnabled = false;

            DpiScale dpiScale = System.Windows.Media.VisualTreeHelper.GetDpi(this);
            this.Width = App.Settings.RemoteControlWidth / dpiScale.PixelsPerDip;
            this.Height = App.Settings.RemoteControlHeight / dpiScale.PixelsPerDip;

            //For future improvement: https://www.blakepell.com/blog/wpf-dpi-aware-centering-of-window
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            if (this.Width * dpiScale.PixelsPerDip > screen.WorkingArea.Width)
                this.Width = screen.WorkingArea.Width / dpiScale.PixelsPerDip;
            if (this.Height * dpiScale.PixelsPerDip > screen.WorkingArea.Height)
                this.Height = screen.WorkingArea.Height / dpiScale.PixelsPerDip;

            bool createdNew = true;
            if (App.mutex == null)
                App.mutex = new Mutex(true, App.appName, out createdNew);
            if (createdNew)
            {
                try
                {
                    pipeListener = new NamedPipeListener(App.appName, true);
                    pipeListener.MessageReceived += (sender, e) =>
                    {
                        //System.Windows.MessageBox.Show(e.Message);

                        if (e.Message == "focus")
                        {
                            Dispatcher.Invoke((Action)delegate {
                                Show();
                                this.Activate();
                                this.Focus();
                            });
                        }
                        else if (e.Message.Contains("liveconnect:///"))
                            LaunchFromArgument(e.Message.Replace("liveconnect:///", ""));
                        //else
                        //AddAgentToList(e.Message);
                    };
                    pipeListener.Error += (sender, e) => System.Windows.MessageBox.Show(string.Format("Error ({0}): {1}", e.ErrorType, e.Exception.ToString()));
                    pipeListener.Start();
                }
                catch (Exception ex)
                {
                    new WindowException(ex, "Named Pipe Setup").ShowDialog();
                }
            } else
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i].StartsWith("liveconnect:///"))
                    {
                        NamedPipeListener.SendMessage(App.appName, true, args[i]);
                        Environment.Exit(0);
                    }
                }

                NamedPipeListener.SendMessage(App.appName, true, "focus");
                Environment.Exit(0);
            }

            WindowUtilities.ActivateWindow(this);
        }

        private void LaunchFromArgument(string base64)
        {
            KLCCommand command = KLCCommand.NewFromBase64(base64);
            Kaseya.LoadToken(command.payload.auth.Token);
            Kaseya.LaunchNotify(command.launchNotifyUrl);

            Dispatcher.Invoke((Action)delegate {
                WindowUtilities.ActivateWindow(this);
                AddReal(command.payload.agentId);
            });
        }

        /*
        public delegate void StatusCallback(EPStatus status);
        public void StatusUpdate(EPStatus status)
        {
            Console.WriteLine(status.ToString());
        }
        */

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

        private void SelectionUpdateUI()
        {
            foreach (Connection conn in ConnectionManager.listConnection)
            {
                if (ConnectionManager.Active == conn)
                    conn.Control.btnAgent.FontWeight = conn.Control.btnRCShared.FontWeight = conn.Control.btnRCPrivate.FontWeight = FontWeights.Bold;
                else
                    conn.Control.btnAgent.FontWeight = conn.Control.btnRCShared.FontWeight = conn.Control.btnRCPrivate.FontWeight = FontWeights.Thin;
            }
        }

        private void ConnectUpdateUI()
        {
            if (ConnectionManager.Active == null)
                return;

            if (ConnectionManager.Active.IsReal)
            {
                btnRCAdd.Content = ConnectionManager.Active.LCSession.agent.Name;
                btnAlt.IsEnabled = true;

                //if(ConnectionManager.Active.RC != null)
                //this.Title = "KLC-Finch+ " + ConnectionManager.Active.RC.state.BaseTitle;
                //else
                this.Title = "KLC-Finch+ " + ConnectionManager.Active.LCSession.agent.Name;
            }
            else
            {
                btnRCAdd.Content = ConnectionManager.Active.Label;
                btnAlt.IsEnabled = false;

                this.Title = "KLC-Finch+ " + ConnectionManager.Active.Label;
            }

            btnRCAdd.IsEnabled = true;
            btnRCClose.IsEnabled = (ConnectionManager.Active.RC != null && ConnectionManager.Active.RC.state.connectionStatus == ConnectionStatus.Connected);
            btnSessionClose.IsEnabled = true;
        }

        private void btnAlt_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionManager.Active == null || !ConnectionManager.Active.IsReal)
                return;

            ConnectionManager.Active.ShowAlternativeWindow();
        }

        private void btnRCAdd_Click(object sender, RoutedEventArgs e)
        {
            if(ConnectionManager.Active == null)
                return;

            ConnectionManager.Active.Control.btnRCShared.Visibility = Visibility.Visible;
            tabRC.IsSelected = true; //OpenGL gets mad if it's not visible

            if (ConnectionManager.Active.IsReal)
            {
                if (ConnectionManager.Active.LCSession.WebsocketB == null || !ConnectionManager.Active.LCSession.WebsocketB.ControlAgentIsReady())
                    return;
                if (ConnectionManager.Active.RC != null && ConnectionManager.Active.RC.state.connectionStatus != ConnectionStatus.Disconnected)
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
            btnRCClose.IsEnabled = false;
            ConnectionManager.DisconnectRC();
        }

        public void DisconnectAndRemoveActiveSession()
        {
            Connection conn = ConnectionManager.Active;
            if (conn == null)
                return;

            conn.Disconnect(2);

            if (conn.IsReal)
            {
                conn.LCSession.Close();
            }
            else
            {
                RemoteControlTest rc = (RemoteControlTest)conn.RC;
                rc.Disconnect();
                rc.LoopStop();
            }

            btnAlt.IsEnabled = false;
            btnRCAdd.IsEnabled = false;
            btnRCClose.IsEnabled = false;
            btnSessionClose.IsEnabled = false;

            //conn.Control.btnAgent.Style = itemsActiveConnections.FindResource("btnAgentDC") as Style;

            ConnectionGroup group = ConnectionManager.GetConnectionGroup(conn.GroupName);
            if (group == null)
            {
                //This can happen from trying to close window, selecting disconnect, switching tab and pressing close session.
                return;
            }
            group.GroupStack.Children.Remove(conn.Control.btnAgent);
            group.GroupStack.Children.Remove(conn.Control.btnRCShared);
            group.GroupStack.Children.Remove(conn.Control.btnRCPrivate);
            ConnectionManager.listConnection.Remove(conn);

            if (group.GroupStack.Children.Count == 0)
            {
                stackActiveConnections.Children.Remove(group.Label);
                stackActiveConnections.Children.Remove(group.Border);
                ConnectionManager.DeleteGroup(group);
            }
        }

        private void btnSessionClose_Click(object sender, RoutedEventArgs e)
        {
            DisconnectAndRemoveActiveSession();
        }

        private void btnSideCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (gridSide.Width > 70)
                gridSide.Width = 70;
            else
                gridSide.Width = 150;
        }

        private void tabRC_GotFocus(object sender, RoutedEventArgs e)
        {
            //Required for OpenGLWPF otherwise the keyboard doesn't work.
            controlViewer.Focus();

            if (ConnectionManager.Active == null)
                return;
            if (ConnectionManager.Active.IsReal)
            {
                if (ConnectionManager.Active.RC == null || ConnectionManager.Active.RC.state == null)
                    this.Title = "KLC-Finch+ " + ConnectionManager.Active.Label + "::RC";
                else
                    this.Title = "KLC-Finch+ " + ConnectionManager.Active.RC.state.BaseTitle;
            } else
            {
                this.Title = "KLC-Finch+ " + ConnectionManager.Active.Label + "::RC";
            }
        }

        private void AddConnectionToUI(Connection conn, string groupName)
        {
            StackPanel stack = null;
            ConnectionGroup group = ConnectionManager.GetConnectionGroup(groupName);
            if (group == null)
            {
                group = ConnectionManager.NewConnectionGroup(groupName);
                group.Label.Style = itemsActiveConnections.FindResource("lblGroup") as Style;

                stackActiveConnections.Children.Add(group.Label);
                stackActiveConnections.Children.Add(group.Border);
            }
            stack = group.GroupStack;

            conn.Control.btnAgent.Style = itemsActiveConnections.FindResource("btnAgent") as Style;
            conn.Control.btnAgent.Click += BtnMachine_Click;
            conn.Control.btnRCShared.Style = itemsActiveConnections.FindResource("btnRC") as Style;
            conn.Control.btnRCShared.Click += btnRC_Click;
            stack.Children.Add(conn.Control.btnAgent);
            stack.Children.Add(conn.Control.btnRCShared);

            SetAltTabIfNoRC(conn);
            SelectionUpdateUI();
        }

        private void btnAddExampleTest_Click(object sender, RoutedEventArgs e)
        {
            WindowRCTest2 winTest = new WindowRCTest2()
            {
                Owner = this
            };
            winTest.ShowDialog();
            if (winTest.ReturnValue != null)
            {
                btnAlt.IsEnabled = btnRCAdd.IsEnabled = false;

                //bool directToRemoteControl = false;
                //HasConnected callback = (directToRemoteControl ? new HasConnected(ConnectDirect) : new HasConnected(ConnectNotDirect));

                //WindowAlternative.StatusCallback callback = new WindowAlternative.StatusCallback(StatusUpdate);
                Connection conn = ConnectionManager.AddTest(winTest.ReturnValue, winTest.ReturnMac, null);
                AddConnectionToUI(conn, "Test");
            }
        }

        private void btnAddByID_Click(object sender, RoutedEventArgs e)
        {
            WindowInputStringConfirm wrk = new WindowInputStringConfirm("Add agent by ID", "", "")
            {
                Owner = this
            };
            bool accept = (bool)wrk.ShowDialog();
            if (accept)
            {
                AddReal(wrk.ReturnName);
            }
        }

        private void btnAddExampleThis_Click(object sender, RoutedEventArgs e)
        {
            string val = "";

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
                    AddReal(val);
            }
            catch (Exception)
            {
            }
        }

        private void btnAddExampleTeamV_Click(object sender, RoutedEventArgs e)
        {
            AddReal("111111111111111");
        }

        private void btnAddExampleMac_Click(object sender, RoutedEventArgs e)
        {
            AddReal("396130197451961");
        }

        private void btnAddJasonMac_Click(object sender, RoutedEventArgs e)
        {
            AddReal("181240160300176");
        }

        private void AddReal(string val)
        {
            try
            {
                if (val.Length > 0)
                {
                    btnAlt.IsEnabled = btnRCAdd.IsEnabled = false;

                    //WindowAlternative.StatusCallback callback = new WindowAlternative.StatusCallback(StatusUpdate);
                    //WindowAlternative.StatusCallback callback = null;
                    //conn = ConnectionManager.AddReal(val, Kaseya.Token, /*this,*/ callback);

                    WindowAlternative winAlt = new WindowAlternative(val, Kaseya.Token);
                    if (winAlt.conn == null)
                        return;
                    AddConnectionToUI(winAlt.conn, winAlt.conn.LCSession.agent.MachineGroupReverse);
                }
            }
            catch (Exception)
            {
            }
        }

        private void BtnMachine_Click(object sender, RoutedEventArgs e)
        {
            Connection conn = (Connection)((Button)sender).Tag;
            ConnectionManager.Switch(conn);
            tabAlt.Focus();
            //SetAltTabIfNoRC(conn);

            SelectionUpdateUI();
        }

        private void btnRC_Click(object sender, RoutedEventArgs e)
        {
            Connection conn = (Connection)((Button)sender).Tag;
            ConnectionManager.Switch(conn);
            tabRC.Focus();
            //SetAltTabIfNoRC(conn);

            SelectionUpdateUI();
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
            if (ConnectionManager.listConnection.Count == 0)
            {
                ConnectionManager.Alive = false;
                Environment.Exit(0);
            }

            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.WindowTitle = "KLC-Finch+";
                dialog.MainInstruction = "Disconnect/Exit";
                dialog.MainIcon = TaskDialogIcon.Information;
                dialog.CenterParent = true;

                if(ConnectionManager.listConnection.Count == 1)
                    dialog.Content = "You have one active session...";
                else
                    dialog.Content = "You have " + ConnectionManager.listConnection.Count + " active sessions...";
                //dialog.VerificationText = "Close all sessions";
                //dialog.IsVerificationChecked = true;

                TaskDialogButton tdbCancel = new TaskDialogButton("Cancel");
                TaskDialogButton tdbDisconnect = new TaskDialogButton("Disconnect from " + ConnectionManager.Active.Label);
                TaskDialogButton tdbExit = new TaskDialogButton("Completely exit");

                dialog.Buttons.Add(tdbCancel);
                dialog.Buttons.Add(tdbDisconnect);
                dialog.Buttons.Add(tdbExit);

                TaskDialogButton button = dialog.ShowDialog(this);
                //dialog.IsVerificationChecked;
                if (button == tdbCancel)
                {
                    e.Cancel = true;
                }
                if (button == tdbDisconnect)
                {
                    DisconnectAndRemoveActiveSession();
                    e.Cancel = true;
                } else if (button == tdbExit)
                {
                    ConnectionManager.Alive = false;
                    Environment.Exit(0);
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //e.Handled = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = false;
        }

        private void tabAlt_GotFocus(object sender, RoutedEventArgs e)
        {
            ConnectUpdateUI();
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtVersion.Text = App.Version;

            Kaseya.LoadToken(KaseyaAuth.GetStoredAuth());
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("liveconnect:///"))
                {
                    LaunchFromArgument(args[i]);
                }
            }
            if (Kaseya.Token != null)
                btnLoadToken.Visibility = Visibility.Collapsed;

            foreach (Bookmark bm in Bookmarks.List)
            {
                if (bm.Type == "Agent")
                    cmbBookmarks.Items.Add(bm);
            }
        }

        private void btnTestThing_Click(object sender, RoutedEventArgs e)
        {
            SelectionUpdateUI();
        }

        private void cmbBookmarks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Bookmark selected = (Bookmark)cmbBookmarks.SelectedItem;
            if (selected == null || selected.Type != "Agent")
                return;

            AddReal(selected.Value);
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions winOptions = new WindowOptions(ref App.Settings, true)
            {
                Owner = this
            };
            winOptions.ShowDialog();
        }

        private void btnLoadToken_Click(object sender, RoutedEventArgs e)
        {
            Kaseya.LoadToken(WindowAuthToken.GetInput(Kaseya.Token, this));
            if (Kaseya.Token != null)
                btnLoadToken.Visibility = Visibility.Collapsed;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            controlViewer.WinActivated(sender, e);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            controlViewer.WinDeactivated(sender, e);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            controlViewer.WinStateChanged(sender, e, WindowState);
        }
    }
}

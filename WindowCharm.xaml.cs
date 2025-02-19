using LibKaseya;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static LibKaseya.Enums;
using static OpenTK.Windowing.GraphicsLibraryFramework.GLFWCallbacks;

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
        private string authToken;
        private string vsa;

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
            App.mutex ??= new Mutex(true, App.appName, out createdNew);
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
            Kaseya.LoadToken(command.VSA, command.payload.auth.Token);
            Kaseya.LaunchNotify(command.VSA, command.launchNotifyUrl);

            Dispatcher.Invoke((Action)delegate {
                WindowUtilities.ActivateWindow(this);
                AddReal(command.VSA, command.payload.auth.Token, command.payload.agentId);
            });
        }

        public delegate void StatusCallback();

        public void StatusUpdate()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                if (ConnectionManager.Active.LCSession == null)
                {
                    txtStatus.Text = "NULL";
                    borderStatus.Background = new SolidColorBrush(Colors.DimGray);
                    borderStatus.Visibility = Visibility.Visible;
                }
                else
                {
                    txtStatus.Text = ConnectionManager.Active.LCSession.GetStatusText();
                    borderStatus.Background = ConnectionManager.Active.LCSession.GetStatusColor();
                    borderStatus.Visibility = ConnectionManager.Active.LCSession.GetStatusVisibility();
                }
            });
        }

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

            UpdateLessDashboard();
        }

        private void ConnectUpdateUI()
        {
            if (ConnectionManager.Active == null)
                return;

            if (ConnectionManager.Active.IsReal)
            {
                btnAlt.IsEnabled = true;

                //if(ConnectionManager.Active.RC != null)
                //this.Title = "KLC-Finch+ " + ConnectionManager.Active.RC.state.BaseTitle;
                //else
                this.Title = "KLC-Finch+ " + ConnectionManager.Active.LCSession.agent.Name;
            }
            else
            {
                btnAlt.IsEnabled = false;

                this.Title = "KLC-Finch+ " + ConnectionManager.Active.Label;
            }

            UpdateLessDashboard();

            btnRCAdd.IsEnabled = true;
            btnRCClose.IsEnabled = (ConnectionManager.Active.RC != null && ConnectionManager.Active.RC.state.connectionStatus == ConnectionStatus.Connected);
            btnSessionClose.IsEnabled = true;
        }

        private void BtnAlt_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionManager.Active == null || !ConnectionManager.Active.IsReal)
                return;

            ConnectionManager.Active.ShowAlternativeWindow(btnAlt, 100);
        }

        private void BtnRCAdd_Click(object sender, RoutedEventArgs e)
        {
            if(ConnectionManager.Active == null)
                return;

            if (ConnectionManager.Active.IsReal)
            {
                if (ConnectionManager.Active.LCSession.WebsocketB == null || !ConnectionManager.Active.LCSession.WebsocketB.ControlAgentIsReady())
                    return;
                if (ConnectionManager.Active.RC != null)
                {
                    switch(ConnectionManager.Active.RC.state.connectionStatus)
                    {
                        case ConnectionStatus.FirstConnectionAttempt:
                        case ConnectionStatus.Disconnected:
                            break;

                        default:
                            return;
                    }
                }
                tabRC.IsSelected = true;
                ConnectionManager.Active.LCSession.ModuleRemoteControl = new RemoteControl(ConnectionManager.Active.LCSession, RC.Shared);
                ConnectionManager.Active.LCSession.ModuleRemoteControl.ConnectV2(this);
            } else
            {
                tabRC.IsSelected = true;
                RemoteControlTest rcTest = (RemoteControlTest)ConnectionManager.Active.RC;
                if(!rcTest.LoopIsRunning())
                    rcTest.LoopStart(controlViewer);
                //controlViewer.Start(ConnectionManager.Active.RC.state, LibKaseya.Agent.OSProfile.Other);
            }

            ConnectionManager.Active.Control.btnRCShared.Visibility = Visibility.Visible;
            btnRCClose.IsEnabled = true;
        }

        private void BtnRCClose_Click(object sender, RoutedEventArgs e)
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

        private void BtnSessionClose_Click(object sender, RoutedEventArgs e)
        {
            DisconnectAndRemoveActiveSession();
        }

        private void BtnSideCollapse_Click(object sender, RoutedEventArgs e)
        {
            if (gridSide.Width > 70)
                gridSide.Width = 70;
            else
                gridSide.Width = 150;
        }

        private void TabRC_GotFocus(object sender, RoutedEventArgs e)
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
            conn.Control.btnRCShared.Click += BtnRC_Click;
            stack.Children.Add(conn.Control.btnAgent);
            stack.Children.Add(conn.Control.btnRCShared);

            SetAltTabIfNoRC(conn);
            SelectionUpdateUI();
        }

        private void BtnAddExampleTest_Click(object sender, RoutedEventArgs e)
        {
            WindowRCTest2 winTest = new()
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

        private void BtnAddByID_Click(object sender, RoutedEventArgs e)
        {
            WindowInputStringConfirm wrk = new("Add agent by ID", "", "")
            {
                Owner = this
            };
            bool accept = (bool)wrk.ShowDialog();
            if (accept)
            {
                AddReal(vsa, authToken, wrk.ReturnName);
            }
        }

        private void BtnAddExampleThis_Click(object sender, RoutedEventArgs e)
        {
            string val = "";

            //first agent ID in registry that matches current vsa
            try {
                using (RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) {
                    RegistryKey subkey = view32.OpenSubKey(@"SOFTWARE\Kaseya\Agent"); //Actually in WOW6432Node
                    if (subkey != null) {
                        string[] agents = subkey.GetSubKeyNames();
                        foreach (string agent in agents) {
                            RegistryKey agentkey = subkey.OpenSubKey(agent);
                            if (agentkey != null) {
                                string valAddress = (string)agentkey.GetValue("lastKnownConnAddr");
                                string valGUID = (string)agentkey.GetValue("AgentGUID");
                                if (valAddress != null && valGUID != null && valAddress == vsa) {
                                    val = valGUID;
                                    agentkey.Close();
                                    break;
                                }
                                agentkey.Close();
                            }
                        }
                        subkey.Close();
                    }
                }

                if (val.Length > 0)
                    AddReal(vsa, authToken, val);
            } catch (Exception) {
            }
        }

        private void BtnAddSimulated_Click(object sender, RoutedEventArgs e) {
            //btnAlt.IsEnabled = btnRCAdd.IsEnabled = false;

            //WindowAlternative.StatusCallback callback = new WindowAlternative.StatusCallback(StatusUpdate);
            //Connection conn = ConnectionManager.AddSimulated(charmCallback);

            StatusCallback charmCallback = new StatusCallback(StatusUpdate);
            WindowAlternative winAlt = new(Agent.VsaSim, Agent.VsaSim, Agent.VsaSim, OnConnect.AlsoRC, RC.Shared, charmCallback);
            if (winAlt.conn == null)
                return;
            AddConnectionToUI(winAlt.conn, "Lanner");
        }

        public void AddReal(string vsa, string shortToken, string agentID)
        {
            try
            {
                if (agentID.Length > 0)
                {
                    btnAlt.IsEnabled = btnRCAdd.IsEnabled = false;

                    ////WindowAlternative.StatusCallback callback = new WindowAlternative.StatusCallback(StatusUpdate);
                    ////WindowAlternative.StatusCallback callback = null;
                    ////conn = ConnectionManager.AddReal(val, Kaseya.Token, /*this,*/ callback);

                    StatusCallback charmCallback = new StatusCallback(StatusUpdate);
                    WindowAlternative winAlt = new(agentID, vsa, shortToken, OnConnect.NoAction, RC.Shared, charmCallback);
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

        private void BtnRC_Click(object sender, RoutedEventArgs e)
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

            using (TaskDialog dialog = new())
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

                TaskDialogButton tdbCancel = new("Cancel");
                TaskDialogButton tdbDisconnect = new("Disconnect from " + ConnectionManager.Active.Label);
                TaskDialogButton tdbExit = new("Completely exit");

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

        private void TabAlt_GotFocus(object sender, RoutedEventArgs e)
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

            foreach (KeyValuePair<string, KaseyaVSA> v in Kaseya.VSA)
            {
                if (v.Value.Token != null)
                {
                    vsa = v.Key;
                    authToken = v.Value.Token;
                    btnLoadToken.Visibility = Visibility.Collapsed;
                    break;
                }
            }

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("liveconnect:///"))
                {
                    LaunchFromArgument(args[i]);
                }
            }

            foreach (Bookmark bm in App.Shared.Bookmarks)
            {
                cmbBookmarks.Items.Add(bm);
            }

            UpdateLessDashboard();
        }

        private void BtnTestThing_Click(object sender, RoutedEventArgs e)
        {
            SelectionUpdateUI();
        }

        private void CmbBookmarks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Bookmark selected = (Bookmark)cmbBookmarks.SelectedItem;
            if (selected == null)
                return;

            AddReal(selected.VSA, authToken, selected.AgentGUID);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions winOptions = new(ref App.Settings, true)
            {
                Owner = this
            };
            winOptions.ShowDialog();
        }

        private void BtnLoadToken_Click(object sender, RoutedEventArgs e)
        {
            WindowAuthToken entry = new()
            {
                Owner = this
            };
            if (entry.ShowDialog() == true)
            {
                btnLoadToken.Visibility = Visibility.Collapsed;
            }
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

        private void UpdateLessDashboard()
        {
            if (ConnectionManager.Active == null)
            {
                txtMachineName.Text = "No machine selected.";
                txtRebootLast.Text = "Use the controls under Bookmarks to start a session.";
                txtRebootLast.ToolTip = null;
                txtRCNotify.Text = LibKaseyaLiveConnect.Text.RCNotify(NotifyApproval.None);
                DisplayMachineNote(0, "");

                return;
            }

            if (ConnectionManager.Active.IsReal)
            {
                if (ConnectionManager.Active.LCSession == null)
                    return;

                txtMachineName.Text = ConnectionManager.Active.LCSession.agent.Name;

                //txtUtilisationRAM.Text = "RAM: " + ConnectionManager.Active.LCSession.agent.RAMinGB + " GB";
                txtRCNotify.Text = LibKaseyaLiveConnect.Text.RCNotify(ConnectionManager.Active.LCSession.RCNotify);
                //DisplayMachineNote(ConnectionManager.Active.LCSession.agent.MachineShowToolTip, ConnectionManager.Active.LCSession.agent.MachineNote, ConnectionManager.Active.LCSession.agent.MachineNoteLink);
            } else {
                txtMachineName.Text = ConnectionManager.Active.Label;
                //txtRebootLast.Text = "N/A";
                //txtRebootLast.ToolTip = null;
                txtRCNotify.Text = LibKaseyaLiveConnect.Text.RCNotify(NotifyApproval.None);
                //DisplayMachineNote(1, "Test Note");
            }


            if (ConnectionManager.Active.LCSession.agent.RebootLast == default(DateTime)) {
                txtRebootLast.Text = "Last reboot unknown";
                txtRebootLast.ToolTip = null;
            } else {
                txtRebootLast.Text = "Last rebooted ~" + KLC.Util.FuzzyTimeAgo(ConnectionManager.Active.LCSession.agent.RebootLast);
                txtRebootLast.ToolTip = ConnectionManager.Active.LCSession.agent.RebootLast.ToString();
            }

            DisplayMachineNote(ConnectionManager.Active.LCSession.agent.MachineShowToolTip, ConnectionManager.Active.LCSession.agent.MachineNote, ConnectionManager.Active.LCSession.agent.MachineNoteLink);

            StatusUpdate();
        }

        public void DisplayMachineNote(int machineShowToolTip, string machineNote, string machineNoteLink = null)
        {
            //Copied from controlDashboard

            if (machineNote == null)
                return;

            if (machineShowToolTip == 0 && machineNote.Length == 0)
            {
                txtSpecialInstructions.Visibility = txtMachineNote.Visibility = Visibility.Collapsed;
                return;
            }
            txtSpecialInstructions.Visibility = txtMachineNote.Visibility = Visibility.Visible;

            if (machineShowToolTip > 0)
            {
                if (Enum.IsDefined(typeof(Badge), machineShowToolTip))
                    txtSpecialInstructions.Text = "Special Instructions for this Machine (" + Enum.GetName(typeof(Badge), machineShowToolTip) + ")";
                else
                    txtSpecialInstructions.Text = "Special Instructions for this Machine (" + machineShowToolTip + ")";
            }

            if (machineNoteLink != null)
            {
                txtMachineNoteLink.NavigateUri = new Uri(machineNoteLink);
                txtMachineNoteLinkText.Text = machineNoteLink;
                txtMachineNoteText.Text = machineNote;
            }
            else
            {
                txtMachineNoteLinkText.Text = string.Empty;
                txtMachineNoteText.Text = machineNote;
            }

        }

        private void txtMachineNoteLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        }

    }
}

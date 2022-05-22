using LibKaseya;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace KLC_Finch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private string thisAgentID;
        //private WindowRCTest winRCTest;

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            txtVersion.Text = "Build date: " + App.Version;

            string savedAuthToken = KaseyaAuth.GetStoredAuth();
            if (savedAuthToken != null)
                txtAuthToken.Password = savedAuthToken;

            if (!File.Exists(@"C:\Program Files\Kaseya Live Connect-MITM\KaseyaLiveConnect.exe") && !File.Exists(Environment.ExpandEnvironmentVariables(@"%localappdata%\Apps\Kaseya Live Connect-MITM\KaseyaLiveConnect.exe")))
                chkUseMITM.Visibility = Visibility.Collapsed;

            foreach (Bookmark bm in Bookmarks.List)
            {
                if (bm.Type == "Agent")
                    cmbBookmarks.Items.Add(bm);
            }

            #region This Agent ID
            try
            {
                using (RegistryKey view32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    RegistryKey subkey = view32.OpenSubKey(@"SOFTWARE\Kaseya\Agent\AGENT11111111111111"); //Actually in WOW6432Node
                    if (subkey != null)
                        thisAgentID = subkey.GetValue("AgentGUID").ToString();
                    subkey.Close();
                }
            }
            catch (Exception)
            {
            }
            #endregion
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Environment.Exit(0);
        }

        private void BtnAgentGuidConnect_Click(object sender, RoutedEventArgs e) {
            if (txtAgentGuid.Text.Trim().Length > 0) {
                App.winStandalone = new WindowAlternative(txtAgentGuid.Text.Trim(), txtAuthToken.Password);
                App.winStandalone.Show();
                /*
                Connection conn = ConnectionManager.AddReal(txtAgentGuid.Text.Trim(), txtAuthToken.Password, null);
                if (conn == null)
                    return;
                conn.ShowAlternativeWindow();
                */
                btnLaunchCharm.IsEnabled = false;
            }
        }

        private void BtnLaunchThisComputer_Click(object sender, RoutedEventArgs e)
        {
            if (thisAgentID != null)
            {
                App.winStandalone = new WindowAlternative(thisAgentID, txtAuthToken.Password);
                App.winStandalone.Show();
                btnLaunchCharm.IsEnabled = false;
            }
        }

        private void BtnLaunchThisComputerShared_Click(object sender, RoutedEventArgs e)
        {
            if (thisAgentID != null)
            {
                App.winStandalone = new WindowAlternative(thisAgentID, txtAuthToken.Password, Enums.OnConnect.OnlyRC, Enums.RC.Shared);
                App.winStandalone.Show();
                btnLaunchCharm.IsEnabled = false;
            }
        }

        private void BtnLaunchRCTest_Click(object sender, RoutedEventArgs e) {
            if(App.winStandaloneViewer != null && App.winStandaloneViewer.IsVisible)
            {
                DialogResult result = System.Windows.Forms.MessageBox.Show("Disconnect any existing Remote Control window?", "KLC-Finch", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if(result != System.Windows.Forms.DialogResult.Yes)
                    return;

                App.winStandaloneViewer.Close();
            }

            WindowRCTest2 winTest = new WindowRCTest2()
            {
                Owner = this
            };
            winTest.ShowDialog();
            if (winTest.ReturnValue != null)
            {
                App.winStandaloneViewer = new WindowViewerV4();
                App.winStandaloneViewer.Show();
                ConnectionManager.Viewer = App.winStandaloneViewer.controlViewer;

                Connection conn = ConnectionManager.AddTest(winTest.ReturnValue, winTest.ReturnMac, null);
                RemoteControlTest rcTest = (RemoteControlTest)conn.RC;
                if (!rcTest.LoopIsRunning())
                    rcTest.LoopStart(ConnectionManager.Viewer);
            }
        }

        private void BtnLaunchNull_Click(object sender, RoutedEventArgs e) {
            App.winStandalone = new WindowAlternative();
            App.winStandalone.Show();
            btnLaunchCharm.IsEnabled = false;
        }

        private void ChkUseMITM_Change(object sender, RoutedEventArgs e) {
            KLC.WsA.useInternalMITM = (bool)chkUseMITM.IsChecked;
        }

        private void BtnRCSettings_Click(object sender, RoutedEventArgs e) {
            WindowOptions winOptions = new WindowOptions(ref App.Settings, true) {
                Owner = this
            };
            winOptions.ShowDialog();
        }

        private void cmbBookmarks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Bookmark selected = (Bookmark)cmbBookmarks.SelectedItem;
            if (selected == null || selected.Type != "Agent")
                return;

            if (Keyboard.IsKeyDown(Key.LeftShift))
                App.winStandalone = new WindowAlternative(selected.Value, txtAuthToken.Password, Enums.OnConnect.OnlyRC, Enums.RC.Shared);
            else
                App.winStandalone = new WindowAlternative(selected.Value, txtAuthToken.Password);
            App.winStandalone.Show();
            btnLaunchCharm.IsEnabled = false;
        }

        private void btnLaunchCharm_Click(object sender, RoutedEventArgs e)
        {
            App.winCharm = new WindowCharm();
            App.winCharm.Show();
            this.Hide();
        }
    }
}

﻿using LibKaseya;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

namespace KLC_Finch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string thisAgentID;
        private string thisAgentVSA;
        //private WindowRCTest winRCTest;

        public MainWindow()
        {
            InitializeComponent();

            foreach (KeyValuePair<string, KaseyaVSA> vsa in Kaseya.VSA)
            {
                cmbAddress.Items.Add(vsa.Key);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtVersion.Text = "Build date: " + App.Version;

            if (cmbAddress.Items.Count > 0)
            {
                cmbAddress.SelectedIndex = 0;
                cmbAddress_LostFocus(sender, e);
            }

            if (!File.Exists(@"C:\Program Files\Kaseya Live Connect-MITM\KaseyaLiveConnect.exe") && !File.Exists(Environment.ExpandEnvironmentVariables(@"%localappdata%\Apps\Kaseya Live Connect-MITM\KaseyaLiveConnect.exe")))
                chkUseMITM.Visibility = Visibility.Collapsed;

            foreach (Bookmark bm in App.Shared.Bookmarks)
            {
                cmbBookmarks.Items.Add(bm);
            }

            #region This Agent ID (first in registry)
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
                                if (valAddress != null && valGUID != null) {
                                    thisAgentVSA = valAddress;
                                    thisAgentID = valGUID;
                                    agentkey.Close();
                                    break;
                                }
                                agentkey.Close();
                            }
                        }
                        subkey.Close();
                    }
                }
            } catch (Exception) {
            }
            #endregion
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void BtnAgentGuidConnect_Click(object sender, RoutedEventArgs e)
        {
            if (txtAgentGuid.Text.Trim().Length > 0)
            {
                App.winStandalone = new WindowAlternative(txtAgentGuid.Text.Trim(), cmbAddress.Text, txtAuthToken.Password);
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
                App.winStandalone = new WindowAlternative(thisAgentID, thisAgentVSA, txtAuthToken.Password);
                App.winStandalone.Show();
                btnLaunchCharm.IsEnabled = false;
            }
        }

        private void BtnLaunchThisComputerShared_Click(object sender, RoutedEventArgs e)
        {
            if (thisAgentID != null)
            {
                App.winStandalone = new WindowAlternative(thisAgentID, thisAgentVSA, txtAuthToken.Password, Enums.OnConnect.OnlyRC, Enums.RC.Shared);
                App.winStandalone.Show();
                btnLaunchCharm.IsEnabled = false;
            }
        }

        private void BtnLaunchRCTest_Click(object sender, RoutedEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftShift)) {
                App.winStandalone = new WindowAlternative(Agent.VsaSim, Agent.VsaSim, Agent.VsaSim, Enums.OnConnect.NoAction, Enums.RC.Shared, null);
                App.winStandalone.Show();

                /*
                //App.winStandaloneViewer = new WindowViewerV4();
                //App.winStandaloneViewer.Show();

                //ConnectionManager.Viewer = App.winStandaloneViewer.controlViewer;

                Connection conn = ConnectionManager.AddSimulated(App.winStandalone.StatusUpdate);
                RemoteControlTest rcTest = (RemoteControlTest)conn.RC;
                if (!rcTest.LoopIsRunning())
                    rcTest.LoopStart(ConnectionManager.Viewer);
                */
                return;
            }

            //--

            if (App.winStandaloneViewer != null && App.winStandaloneViewer.IsVisible)
            {
                DialogResult result = System.Windows.Forms.MessageBox.Show("Disconnect any existing Remote Control window?", "KLC-Finch", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != System.Windows.Forms.DialogResult.Yes)
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

        private void BtnLaunchNull_Click(object sender, RoutedEventArgs e)
        {
            App.winStandalone = new WindowAlternative(null, null, null);
            App.winStandalone.Show();
            btnLaunchCharm.IsEnabled = false;
        }

        private void ChkUseMITM_Change(object sender, RoutedEventArgs e)
        {
            KLC.WsA.useInternalMITM = (bool)chkUseMITM.IsChecked;
        }

        private void BtnRCSettings_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions winOptions = new WindowOptions(ref App.Settings, true)
            {
                Owner = this
            };
            winOptions.ShowDialog();
        }

        private void cmbBookmarks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Bookmark selected = (Bookmark)cmbBookmarks.SelectedItem;
            if (selected == null)
                return;

            if (Keyboard.IsKeyDown(Key.LeftShift))
                App.winStandalone = new WindowAlternative(selected.AgentGUID, selected.VSA, txtAuthToken.Password, Enums.OnConnect.OnlyRC, Enums.RC.Shared);
            else
                App.winStandalone = new WindowAlternative(selected.AgentGUID, selected.VSA, txtAuthToken.Password);
            App.winStandalone.Show();
            btnLaunchCharm.IsEnabled = false;
        }

        private void cmbAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            if (cmbAddress.SelectedItem == null)
            {
                txtAuthToken.Password = "";
                return;
            }

            string selected = cmbAddress.SelectedItem.ToString();
            foreach (KeyValuePair<string, KaseyaVSA> vsa in Kaseya.VSA)
            {
                if (vsa.Key == selected)
                {
                    txtAuthToken.Password = vsa.Value.Token;
                    break;
                }
            }
        }

        private void btnLaunchCharm_Click(object sender, RoutedEventArgs e)
        {
            App.winCharm = new WindowCharm();
            App.winCharm.Show();
            this.Hide();
        }
    }
}

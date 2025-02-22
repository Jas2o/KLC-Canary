﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace KLC_Finch {
    /// <summary>
    /// Interaction logic for WindowCharm.xaml
    /// </summary>
    public partial class WindowViewerV4 : Window
    {
        //private IRemoteControl rc;
        //private RCv rcv;
        //private RCstate state;

        public WindowViewerV4()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            ConnectionManager.Viewer = controlViewer;

            DpiScale dpiScale = System.Windows.Media.VisualTreeHelper.GetDpi(this);
            this.Width = App.Settings.RemoteControlWidth / dpiScale.PixelsPerDip;
            this.Height = App.Settings.RemoteControlHeight / dpiScale.PixelsPerDip;

            //For future improvement: https://www.blakepell.com/blog/wpf-dpi-aware-centering-of-window
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            if (this.Width * dpiScale.PixelsPerDip > screen.WorkingArea.Width)
                this.Width = screen.WorkingArea.Width / dpiScale.PixelsPerDip;
            if (this.Height * dpiScale.PixelsPerDip > screen.WorkingArea.Height)
                this.Height = screen.WorkingArea.Height / dpiScale.PixelsPerDip;

            WindowUtilities.ActivateWindow(this);
        }

        public delegate void HasConnected();
        public void ConnectDirect()
        {
            //session.ModuleRemoteControl = new RemoteControl(session, directToMode);
            Application.Current.Dispatcher.Invoke((Action)delegate {
                //ConnectUpdateUI();

                this.Visibility = Visibility.Collapsed;
            });
        }
        public void ConnectNotDirect()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                //ConnectUpdateUI();
            });
        }

        /*
        private void ConnectUpdateUI()
        {
            if (ConnectionManager.Active.IsReal)
            {
                btnRCAdd.Content = ConnectionManager.Active.LCSession.agent.Name;
                btnAlt.IsEnabled = true;
            }
            else
            {
                btnRCAdd.Content = ConnectionManager.Active.Label;
                btnAlt.IsEnabled = false;
            }

            btnRCAdd.IsEnabled = true;
            btnRCClose.IsEnabled = (ConnectionManager.Active.RC != null && ConnectionManager.Active.RC.state.connectionStatus == ConnectionStatus.Connected);
            btnSessionClose.IsEnabled = true;
        }

        private void btnRCClose_Click(object sender, RoutedEventArgs e)
        {
            ConnectionManager.DisconnectRC();
        }
        */

        private void btnSessionClose_Click(object sender, RoutedEventArgs e)
        {
            Connection conn = ConnectionManager.Active;
            if (conn == null)
                return;

            conn.Disconnect(2);

            if (conn.IsReal)
            {
                conn.LCSession.Close();
            } else {
                RemoteControlTest rc = (RemoteControlTest)conn.RC;
                rc.Disconnect();
                rc.LoopStop();
            }

            ConnectionManager.listConnection.Remove(conn);
        }

        private void tabRC_GotFocus(object sender, RoutedEventArgs e)
        {
            //Required for OpenGLWPF otherwise the keyboard doesn't work.
            controlViewer.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controlViewer.Window_Closing();

            ConnectionManager.Alive = false;
            ConnectionManager.DisconnectRC();

            if (App.winStandalone != null && App.winStandalone.IsVisible)
            {
                this.Visibility = Visibility.Collapsed;
                e.Cancel = true;
                return;
            }

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

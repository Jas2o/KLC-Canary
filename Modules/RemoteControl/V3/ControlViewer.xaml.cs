using KLC;
using LibKaseya;
using NTR;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static LibKaseya.Enums;

namespace KLC_Finch
{
    /// <summary>
    /// Interaction logic for ControlViewer.xaml
    /// </summary>
    public partial class ControlViewer : UserControl
    {
        public Settings Settings;

        private ClipBoardMonitor clipboardMon;
        //private FPSCounter fpsCounter;
        private readonly KeycodeV3 keyctrl = KeycodeV3.Dictionary[System.Windows.Forms.Keys.ControlKey];
        private KeyboardHook keyHook;
        private readonly KeycodeV3 keyshift = KeycodeV3.Dictionary[System.Windows.Forms.Keys.ShiftKey];
        private readonly KeycodeV3 keywin = KeycodeV3.Dictionary[System.Windows.Forms.Keys.LWin];
        private readonly List<KeycodeV3> listHeldKeysMod = new List<KeycodeV3>();
        private readonly List<KeycodeV3> listHeldKeysOther = new List<KeycodeV3>();
        //private readonly List<TSSession> listTSSession = new List<TSSession>();
        //private TSSession currentTSSession = null;
        private Timer timerClipboard;
        private Timer timerHealth;
        private WindowScreens winScreens;
        private string[] arrAdmins = new string[] { "administrator", "brandadmin", "adminc", "company" };
        private bool autotypeAlwaysConfirmed;
        private string clipboard = "";
        //private double fpsLast;
        private bool keyDownWin;

        private IRemoteControl rc; //Active
        private IRemoteControl _rcSwitch;
        public IRemoteControl rcSwitch
        {
            get
            {
                return _rcSwitch;
            }

            set
            {
                if (rc == null)
                    rc = value;
                else
                    _rcSwitch = value;
            }
        }
        /*
        private IRemoteControl rc {
            get {
                if (ConnectionManager.Active == null)
                    return null;
                return ConnectionManager.Active.RC;
            }
        }
        */

        //private IRemoteControl rc;
        private RCv rcv;
        private bool requiredApproval;
        //private ScreenStatus screenStatus;
        private bool ssKeyHookAllow;

        private WinRCFileTransfer winRCFileTransfer;

        public ControlViewer()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            toolVersion.Header = "Build date: " + App.Version;

            Settings = App.Settings;
            int renderer = App.Settings.Renderer;
            //if (!App.SupportsOpenGL) renderer = 2;

            switch (renderer)
            {
                case 0:
                    rcv = new RCvOpenGL();
                    break;

                case 1:
                    rcv = new RCvOpenGLWPF();
                    break;

                case 2:
                    rcv = new RCvCanvas();
                    toolShowMouse.Visibility = Visibility.Collapsed;
                    break;
            }
            placeholder.Child = rcv;

            winScreens = new WindowScreens();
            keyHook = new KeyboardHook();
            keyHook.KeyDown += KeyHook_KeyDown;
            keyHook.KeyUp += KeyHook_KeyUp;

            clipboardMon = new ClipBoardMonitor();
            clipboardMon.OnUpdate += SyncClipboard;

            //if (rcSessionId != null) {
            timerHealth = new System.Timers.Timer(1000);
            timerHealth.Elapsed += CheckHealth;
            timerHealth.Start();
            //}
            timerClipboard = new System.Timers.Timer(500); //Time changes
            timerClipboard.Elapsed += TimerClipboard_Elapsed;
            //fpsCounter = new FPSCounter();

            //WindowUtilities.ActivateWindow(this);

            rcv.ControlLoaded();
        }

        /*
        public void Start(RCstate state, Agent.OSProfile endpointOS = Agent.OSProfile.Other, string endpointLastUser = "")
        {
            this.DataContext = state;

            this.endpointOS = endpointOS;
            this.endpointLastUser = endpointLastUser;

            //DpiScale dpiScale = System.Windows.Media.VisualTreeHelper.GetDpi(this);
            //this.Width = Settings.RemoteControlWidth / dpiScale.PixelsPerDip;
            //this.Height = Settings.RemoteControlHeight / dpiScale.PixelsPerDip;
            LoadSettings(true);

            if (endpointOS == Agent.OSProfile.Mac)
            {
                toolBlockMouseKB.Visibility = Visibility.Collapsed;
                if (Settings.MacSwapCtrlWin)
                    toolKeyWin.Visibility = Visibility.Collapsed;
            }
            toolBlockScreen.Visibility = Visibility.Collapsed;

            SetControlEnabled(false, true); //Just for the visual
        }
        */

        /*
        public void AddTSSession(string session_id, string session_name)
        {
            TSSession newTSSession = new TSSession(session_id, session_name);
            listTSSession.Add(newTSSession);

            Dispatcher.Invoke((Action)delegate {
                MenuItem item = new MenuItem
                {
                    Header = session_name
                };
                item.Click += new RoutedEventHandler(ToolTSSession_ItemClicked);

                toolTSSession.ContextMenu.Items.Add(item);
                toolTSSession.Visibility = Visibility.Visible;

                if (currentTSSession == null)
                {
                    currentTSSession = newTSSession;
                    toolTSSession.Content = currentTSSession.session_name;
                }
            });
        }
        */

        public void ClearApproval()
        {
            rcv.DisplayApproval(false);
        }

        /*
        public void ClearTSSessions()
        {
            listTSSession.Clear();
            currentTSSession = null;

            Dispatcher.Invoke((Action)delegate {
                toolTSSession.ContextMenu.Items.Clear();
            });
        }
        */

        public void DebugKeyboard()
        {
            string strKeyboard = "";

            if (Settings.DisplayOverlayKeyboardMod)
            {
                string[] keysMod = new string[listHeldKeysMod.Count];
                for (int i = 0; i < keysMod.Length; i++)
                {
                    keysMod[i] = listHeldKeysMod[i].Display;
                }

                strKeyboard += String.Join(", ", keysMod);
            }

            if (Settings.DisplayOverlayKeyboardOther)
            {
                string[] keysOther = new string[listHeldKeysOther.Count];
                for (int i = 0; i < keysOther.Length; i++)
                {
                    keysOther[i] = listHeldKeysOther[i].Display;
                }

                if (strKeyboard.Length > 0 && keysOther.Length > 0)
                    strKeyboard += " | ";
                strKeyboard += String.Join(", ", keysOther);

                if (rc.state.mouseHeldRight)
                    strKeyboard = "MouseRight" + (strKeyboard == "" ? "" : " | " + strKeyboard);
                if (rc.state.mouseHeldLeft)
                    strKeyboard = "MouseLeft" + (strKeyboard == "" ? "" : " | " + strKeyboard);
            }

            if (Settings.DisplayOverlayKeyboardMod || Settings.DisplayOverlayKeyboardOther)
            {
                rcv.DisplayDebugKeyboard(strKeyboard);
            }
        }

        public void UpdateScreenLayoutReflow()
        {
            if (rc == null)
                return;

            int lowestX = rc.state.ListScreen.Min(x => x.rect.X);
            int lowestY = rc.state.ListScreen.Min(x => x.rect.Y);
            int highestX = rc.state.ListScreen.Max(x => x.rect.Right);
            int highestY = rc.state.ListScreen.Max(x => x.rect.Bottom);
            rcv.SetCanvas(lowestX, lowestY, highestX, highestY);
            rcv.UpdateScreenLayout(lowestX, lowestY, highestX, highestY);
            winScreens.SetCanvas(lowestX, lowestY, highestX, highestY);
        }

        public void DebugMouseEvent(int X, int Y)
        {
            if (Settings.DisplayOverlayMouse)
                rcv.DisplayDebugMouseEvent(X, Y);
        }

        public void FromGlChangeScreen(RCScreen screen, bool moveCamera = true)
        {
            rc.state.previousScreen = rc.state.CurrentScreen;
            rc.state.CurrentScreen = screen;
            ChangeScreen(rc.state.CurrentScreen.screen_id);

            rcv.CameraFromClickedScreen(screen, moveCamera);

            Dispatcher.Invoke((Action)delegate {
                lblScreen.Content = screen.screen_name;
                toolScreen.ToolTip = screen.StringResPos();

                foreach (MenuItem item in toolScreen.ContextMenu.Items)
                {
                    item.IsChecked = (item.Header.ToString() == screen.ToString());
                }
            });
        }

        public RCScreen GetCurrentScreen()
        {
            if (rc == null)
                return null;

            return rc.state.CurrentScreen;
        }

        public List<RCScreen> GetListScreen()
        {
            if (rc == null)
                return new List<RCScreen>();

            return rc.state.ListScreen;
        }

        /*
        public bool GetStatePowerSaving()
        {
            return rc.state.powerSaving;
        }
        */

        public void NotifyScreenUpdate()
        {
            if (rc == null)
                return;

            //rc.state.socketAlive = true;
            //rc.state.screenStatus = ScreenStatus.Stable;

            rcv.Refresh();
        }

        public void LoadTexture(int width, int height, Bitmap decomp)
        {
            //For Canvas only

            if (rc.state.screenStatus == ScreenStatus.Preparing)
                return;

            if (rc.state.UseMultiScreen)
            {
                if (rc.state.CurrentScreen == null)
                {
                    //Console.WriteLine("[LoadTexture] No matching RCScreen for screen ID: " + screenID);
                    //listScreen might be empty
                    return;
                }

                Dispatcher.Invoke((Action)delegate {
                    if (rc.state.CurrentScreen.CanvasImage == null)
                        rc.state.CurrentScreen.CanvasImage = new System.Windows.Controls.Image();
                    //rc.state.CurrentScreen.CanvasImage.Width = rc.state.CurrentScreen.rect.Width;
                    //rc.state.CurrentScreen.CanvasImage.Height = rc.state.CurrentScreen.rect.Height;

                    rc.state.CurrentScreen.SetCanvasImage(decomp);
                });
            }
            else
            {
                //Legacy
                if (rc.state.CurrentScreen == null)
                    return;

                if (rc.state.legacyVirtualWidth != width || rc.state.legacyVirtualHeight != height)
                {
                    //Console.WriteLine("[LoadTexture:Legacy] Virtual resolution did not match texture received.");
                    SetVirtual(0, 0, width, height);
                }

                rc.state.legacyScreen.rect = new Rectangle(0, 0, width, height);

                Dispatcher.Invoke((Action)delegate {
                    if (rc.state.legacyScreen.CanvasImage == null)
                        rc.state.legacyScreen.CanvasImage = new System.Windows.Controls.Image();

                    /*
                    if (rc.state.CurrentScreen.CanvasImage != null)
                    {
                        rc.state.CurrentScreen.CanvasImage.Width = rc.state.CurrentScreen.rect.Width;
                        rc.state.CurrentScreen.CanvasImage.Height = rc.state.CurrentScreen.rect.Height;
                    }
                    */

                    rc.state.legacyScreen.SetCanvasImage(decomp);
                });
            }

            //rc.state.socketAlive = true;
            //fpsLast = fpsCounter.GetFPS();
            //rc.state.screenStatus = ScreenStatus.Stable;

            rcv.Refresh();
        }

        public void RefreshTSSessions()
        {
            Dispatcher.Invoke((Action)delegate {
                toolTSSession.ContextMenu.Items.Clear();

                foreach(TSSession session in rc.state.listTSSession)
                {
                    MenuItem item = new MenuItem
                    {
                        Header = session.session_name
                    };
                    item.Click += new RoutedEventHandler(ToolTSSession_ItemClicked);

                    toolTSSession.ContextMenu.Items.Add(item);
                }

                if (rc.state.listTSSession.Count > 0)
                {
                    lblTSSession.Content = rc.state.currentTSSession.session_name;
                    toolTSSession.Visibility = Visibility.Visible;
                } else
                {
                    lblTSSession.Content = "(Sessions)";
                    toolTSSession.Visibility = Visibility.Collapsed;
                }
            });
        }

        public void LoadTextureRaw(byte[] buffer, int width, int height, int stride)
        {
            //For Canvas only

            if (rc.state.screenStatus == ScreenStatus.Preparing || width * height <= 0)
                return;

            if (rc.state.UseMultiScreen)
            {
                if (rc.state.CurrentScreen == null)
                {
                    //Console.WriteLine("[LoadTexture] No matching RCScreen for screen ID: " + screenID);
                    //listScreen might be empty
                    return;
                }

                if (rc.state.CurrentScreen.Texture != null)
                {
                    rc.state.CurrentScreen.Texture.LoadRaw(rc.state.CurrentScreen.rect, width, height, stride, buffer);
                }
                else
                {
                    //Canvas
                    Dispatcher.Invoke((Action)delegate {
                        if (rc.state.CurrentScreen.CanvasImage == null)
                            rc.state.CurrentScreen.CanvasImage = new System.Windows.Controls.Image();
                        //rc.state.CurrentScreen.CanvasImage.Width = width;
                        //rc.state.CurrentScreen.CanvasImage.Height = height;

                        rc.state.CurrentScreen.SetCanvasImageBW(width, height, stride, buffer);
                    });
                }
            }
            else
            {
                //Legacy
                if (rc.state.CurrentScreen == null)
                    return;

                if (rc.state.legacyVirtualWidth != width || rc.state.legacyVirtualHeight != height)
                {
                    //Console.WriteLine("[LoadTexture:Legacy] Virtual resolution did not match texture received.");
                    SetVirtual(0, 0, width, height);

                    /*
                    try {
                        rc.state.CurrentScreen.rect.Width = rc.state.CurrentScreen.rectFixed.Width = width;
                        rc.state.CurrentScreen.rect.Height = rc.state.CurrentScreen.rectFixed.Height = height;
                        ////This is a sad attempt a fixing a problem when changing left monitor's size.
                        ////However if changing a middle monitor, the right monitor will break.
                        ////The reconnect button can otherwise be used, or perhaps a multimonitor/scan feature can be added to automatically detect and repair the list of screens.
                        //if (currentScreen.rect.X < 0)
                        //currentScreen.rect.X = width * -1;
                    } catch (Exception ex) {
                        Console.WriteLine("[LoadTexture:Legacy] " + ex.ToString());
                    }
                    */
                }

                rc.state.textureLegacy.LoadRaw(new Rectangle(0, 0, rc.state.CurrentScreen.rect.Width, rc.state.CurrentScreen.rect.Height), width, height, stride, buffer);
            }

            /*
            rc.state.UseMultiScreenFixAvailable = (rc.state.CurrentScreen.rect.Width != width);
            if (rc.state.UseMultiScreenFixAvailable)
            {
                rc.state.legacyVirtualWidth = width;
                rc.state.legacyVirtualHeight = height;

                if (rc.state.previousScreen == null)
                    ToolScreenFix_Click(null, null);
            }
            */

            //rc.state.socketAlive = true;
            //fpsLast = fpsCounter.GetFPS();
            //rc.state.screenStatus = ScreenStatus.Stable;

            rcv.Refresh();
        }

        /// <summary>
        /// Now uses Kaseya's Paste Clipboard, unless the Autotype toolbar button is used.
        /// </summary>
        public void PerformAutotype(bool fromToolbar = false)
        {
            string text = Clipboard.GetText().Trim();

            if (!text.Contains('\n') && !text.Contains('\r'))
            {
                //Console.WriteLine("Attempt autotype of " + text, "autotype");

                bool confirmed;
                if (text.Length < 51 || autotypeAlwaysConfirmed)
                {
                    confirmed = true;
                }
                else
                {
                    WindowConfirmation winConfirm = new WindowConfirmation("You really want to autotype this?", text);
                    confirmed = (bool)winConfirm.ShowDialog();
                    if (confirmed && (bool)winConfirm.chkDoNotAsk.IsChecked)
                        autotypeAlwaysConfirmed = true;
                }
                if (confirmed)
                {
                    foreach (KeycodeV3 k in listHeldKeysMod)
                        rc.SendKeyUp(k.JavascriptKeyCode, k.USBKeyCode);
                    listHeldKeysMod.Clear();

                    //if(fromToolbar)
                    rc.SendAutotype(text);
                    //else
                    //rc.SendPasteClipboard(text);
                }
            }
            else
            {
                //Console.WriteLine("Autotype blocked: too long or had a new line character");
            }
        }

        public void ReceiveClipboard(string content)
        {
            if (clipboard == content)
            {
                Dispatcher.Invoke((Action)delegate {
                    if (toolClipboardGet.Background == System.Windows.Media.Brushes.Transparent)
                    {
                        toolClipboardGet.Background = System.Windows.Media.Brushes.PaleGreen;
                        timerClipboard.Start();
                    }
                });
                return;
            }

            clipboard = content;
            Dispatcher.Invoke((Action)delegate {
                toolClipboardGetText.Text = clipboard.Truncate(50);
                if (clipboard.Length > 0)
                    Clipboard.SetDataObject(clipboard);
                else
                    Clipboard.Clear();

                toolClipboardGet.Background = System.Windows.Media.Brushes.GreenYellow;
                timerClipboard.Start();
            });
        }

        public void SetApprovalAndSpecialNote(Enums.NotifyApproval rcNotify, int machineShowToolTip, string machineNote, string machineNoteLink)
        {
            switch (rcNotify)
            {
                case Enums.NotifyApproval.ApproveAllowIfNoUser:
                case Enums.NotifyApproval.ApproveDenyIfNoUser:
                    requiredApproval = true;
                    rcv.DisplayApproval(true);
                    toolReconnect.Header = "Reconnect (reapproval required)";
                    break;

                default:
                    rcv.DisplayApproval(false);
                    break;
            }

            if (machineShowToolTip > 0 || machineNote.Length > 0 || machineNoteLink != null)
                toolMachineNote.Visibility = Visibility.Visible;
            else
                toolMachineNote.Visibility = Visibility.Collapsed;

            if (machineShowToolTip > 0 && Enum.IsDefined(typeof(Badge), machineShowToolTip))
                lblMachineNote.Content = Enum.GetName(typeof(Badge), machineShowToolTip);

            toolMachineNoteText.Header = machineNote;
            toolMachineNoteText.Visibility = (machineNote.Length == 0 ? Visibility.Collapsed : Visibility.Visible);

            toolMachineNoteLink.Header = machineNoteLink;
            toolMachineNoteLink.Visibility = (machineNoteLink == null ? Visibility.Collapsed : Visibility.Visible);
        }

        public void SetControlEnabled(RCstate state, bool value, bool isStart = false)
        {
            if (rc == null)
                return;

            if (isStart)
            {
                state.ControlEnabled = Settings.StartControlEnabled;
                if (Settings.StartMultiScreen && state.ControlEnabled)
                    rcv.CameraToCurrentScreen();
                else
                    state.UseMultiScreenOverview = true;
            }
            else
                state.ControlEnabled = value;

            Dispatcher.Invoke((Action)delegate {
                if (state.connectionStatus != ConnectionStatus.Disconnected)
                {
                    rcv.DisplayControl(value);
                }

                KeyHookSet(true);
            });
        }

        public void SetScreen(string id)
        {
            rc.state.previousScreen = rc.state.CurrentScreen;
            rc.state.CurrentScreen = rc.state.ListScreen.First(x => x.screen_id == id);
            ChangeScreen(rc.state.CurrentScreen.screen_id);

            if (rc.state.UseMultiScreen)
                rcv.CameraToCurrentScreen();

            Dispatcher.Invoke((Action)delegate {
                lblScreen.Content = rc.state.CurrentScreen.screen_name;
                toolScreen.ToolTip = rc.state.CurrentScreen.StringResPos();

                foreach (MenuItem item in toolScreen.ContextMenu.Items)
                {
                    item.IsChecked = (item.Header.ToString() == rc.state.CurrentScreen.ToString());
                }
            });
        }

        public void SetVirtual(int virtualX, int virtualY, int virtualWidth, int virtualHeight)
        {
            if (rc.state.UseMultiScreen)
            {
                rc.state.virtualViewWant = new Rectangle(virtualX, virtualY, virtualWidth, virtualHeight);
            }
            else
            {
                rc.state.legacyVirtualWidth = virtualWidth;
                rc.state.legacyVirtualHeight = virtualHeight;
                rc.state.virtualCanvas = rc.state.virtualViewWant = new Rectangle(0, 0, virtualWidth, virtualHeight);
            }

            rc.state.virtualRequireViewportUpdate = true;
        }

        /*
        public void UpdateLatency(long ms)
        {
            if (rc == null)
                return;

            rc.state.lastLatency = ms;

            //Dipatcher.Invoke((Action)delegate {
            //toolLatency.Content = string.Format("{0} ms", ms);
            //});
        }
        */

        public void UpdateScreenLayout(string jsonstr = "")
        {
            if (rc == null)
                return;

            Dispatcher.Invoke((Action)delegate {
                toolScreen.ContextMenu.Items.Clear();

                foreach (RCScreen screen in rc.state.ListScreen)
                {
                    //Add to toolbar menu
                    MenuItem item = new MenuItem
                    {
                        Header = screen.ToString()// screen_name + ": (" + screen_width + " x " + screen_height + " at " + screen_x + ", " + screen_y + ")";
                    };
                    item.Click += new RoutedEventHandler(ToolScreen_ItemClicked);

                    toolScreen.ContextMenu.Items.Add(item);

                    //Private and Mac seem to bug out if you change screens, cause there's only one screen
                    toolScreen.Opacity = (rc.state.ListScreen.Count > 1 ? 1.0 : 0.6);

                    if (screen == rc.state.CurrentScreen)
                    {
                        lblScreen.Content = rc.state.CurrentScreen.screen_name;
                        toolScreen.ToolTip = rc.state.CurrentScreen.StringResPos();
                    }
                }
            });

            if (rc.state.ListScreen.Count == 0)
                return;

            int lowestX = rc.state.ListScreen.Min(x => x.rect.X);
            int lowestY = rc.state.ListScreen.Min(x => x.rect.Y);
            int highestX = rc.state.ListScreen.Max(x => x.rect.Right);
            int highestY = rc.state.ListScreen.Max(x => x.rect.Bottom);
            rcv.SetCanvas(lowestX, lowestY, highestX, highestY);
            rcv.ResetCamera();

            rcv.UpdateScreenLayout(lowestX, lowestY, highestX, highestY);

            rc.UpdateScreens(jsonstr);
            winScreens.UpdateStartScreens(jsonstr);
            winScreens.SetCanvas(lowestX, lowestY, highestX, highestY);

            rc.state.screenStatus = ScreenStatus.LayoutReady;
            rcv.Refresh();
        }

        public void UpdateScreenLayoutHack()
        {
            if (rc == null)
                return;

            if (rc.state.endpointOS == Agent.OSProfile.Mac)
            {
                return;
                //This seems okayish sometimes for scale change, but not layout changes.
                //rc.UpdateScreensHack();
            }
            else
            {
                if (rc.state.currentTSSession == null)
                    rc.ChangeTSSession("0");
                else
                    rc.ChangeTSSession(rc.state.currentTSSession.session_id);
            }
        }

        private void CheckHealth(object sender, ElapsedEventArgs e)
        {
            if(rcSwitch != null)
            {
                rc = rcSwitch;
                rcSwitch = null;

                if (rc != null && rc.state != null)
                {
                    /*
                    Dispatcher.Invoke((Action)delegate
                    {
                        this.DataContext = rc.state;
                    });
                    */
                    rc.state.virtualRequireViewportUpdate = true;
                    //rc.state.ForceRefresh();
                    UpdateScreenLayout();
                }
            }

            if (rc == null || rc.state == null || rc.state.powerSaving)
                return;

            Dispatcher.Invoke((Action)delegate
            {
                if (this.DataContext != rc.state)
                {
                    this.DataContext = rc.state;
                    rcv.DisplayControl(rc.state.ControlEnabled);
                }

                //if (keyHook.IsActive && !IsActive) { MessageBox.Show("[RC:CheckHealth] Keyhook active but not RC window."); } //For testing
                rcv.CheckHealth();

                //txtDebugLeft.Visibility = (Settings.DisplayOverlayKeyboardMod || Settings.DisplayOverlayKeyboardOther ? Visibility.Visible : Visibility.Collapsed);
                //txtDebugRight.Visibility = (Settings.DisplayOverlayMouse ? Visibility.Visible : Visibility.Collapsed);

                switch (rc.state.connectionStatus)
                {
                    case ConnectionStatus.FirstConnectionAttempt:
                        //txtRcFrozen.Visibility = Visibility.Collapsed;
                        //txtRcConnecting.Visibility = Visibility.Visible;
                        break;

                    case ConnectionStatus.Connected:
                        //txtRcConnecting.Visibility = Visibility.Collapsed;

                        if (rc.state.fpsCounter.SeemsAlive(5000))
                        {
                            toolLatency.FontWeight = FontWeights.Normal;
                            //txtRcFrozen.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            rc.state.fpsLast = 0;
                            toolLatency.Content = string.Format("Frozen? | {0} ms", rc.state.lastLatency);
                            toolLatency.FontWeight = FontWeights.Bold;
                            //txtRcFrozen.Visibility = Visibility.Visible;
                        }
                        toolLatency.Content = string.Format("FPS: {0} | {1} ms", rc.state.fpsLast, rc.state.lastLatency);
                        break;

                    case ConnectionStatus.Disconnected:
                        toolLatency.Content = "N/C";
                        //if (App.alternative == null || !App.alternative.socketActive)
                        //toolReconnect.Header = "Hard Reconnect Required";

                        //Not relevant to Charm
                        ////if (keyHook.IsActive)
                            ////keyHook.Uninstall();
                        ////timerHealth.Stop();
                        break;
                }
            });
        }

        private void KeyHook_KeyDown(KeyboardHook.VKeys key)
        {
            Window_PreviewKeyDown2(new System.Windows.Forms.KeyEventArgs((System.Windows.Forms.Keys)key));
        }

        private void KeyHook_KeyUp(KeyboardHook.VKeys key)
        {
            Window_KeyUp2(new System.Windows.Forms.KeyEventArgs((System.Windows.Forms.Keys)key));
        }

        private void KeyHookSet(bool canCheckKeyboard = false)
        {
            if (rc == null)
                return;

            if (canCheckKeyboard)
            {
                ssKeyHookAllow = Keyboard.IsKeyToggled(Key.Scroll) || Settings.KeyboardHook;

                bool winIsActive = (App.winStandaloneViewer != null ? App.winStandaloneViewer.IsActive : App.winCharm.IsActive);
                if (!winIsActive)
                    ssKeyHookAllow = false;
            }

            if (!rc.state.ControlEnabled || !ssKeyHookAllow)
            {
                if (keyHook.IsActive)
                    keyHook.Uninstall();
            }
            else
            {
                if (!keyHook.IsActive)
                    keyHook.Install();
            }

            if (canCheckKeyboard) //More like canUpdateUI
                rcv.DisplayKeyHook(keyHook.IsActive && Settings.DisplayOverlayKeyboardHook);
        }

        private void KeyWinSet(bool set)
        {
            if (rc == null || rc.state == null || !rc.state.ControlEnabled || rc.state.endpointOS == Agent.OSProfile.Mac || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            keyDownWin = set;

            if (keyDownWin)
            {
                if (!listHeldKeysMod.Contains(keywin))
                {
                    listHeldKeysMod.Add(keywin);
                    rc.SendKeyDown(keywin.JavascriptKeyCode, keywin.USBKeyCode);
                }
            }
            else
            {
                if (listHeldKeysMod.Contains(keywin))
                {
                    listHeldKeysMod.Remove(keywin);
                    rc.SendKeyUp(keywin.JavascriptKeyCode, keywin.USBKeyCode);
                }
            }

            toolKeyWin.FontWeight = (keyDownWin ? FontWeights.Bold : FontWeights.Normal);
        }

        /*
        private void LoadSettings(RCstate state, bool isStart = false)
        {
            if (state == null)
                return;

            if (isStart)
            {
                if (rcv.SupportsLegacy)
                {
                    if (endpointOS == Agent.OSProfile.Mac && Settings.StartMultiScreenExceptMac)
                        state.UseMultiScreen = false;
                    else
                        state.UseMultiScreen = Settings.StartMultiScreen;
                }
                else
                {
                    state.UseMultiScreen = true;
                }

                //SetControlEnabled(Settings.StartControlEnabled, true);
            }

            autotypeAlwaysConfirmed = Settings.AutotypeSkipLengthCheck;
            ssKeyHookAllow = Settings.KeyboardHook;
            KeyHookSet(false);

            if (Settings.ClipboardSync == 2 && (endpointOS == Agent.OSProfile.Server || (endpointLastUser != null && arrAdmins.Contains(endpointLastUser.ToLower()))))
            {
                //Server/Admin only
                state.SsClipboardSync = true;
            }
            else
            {
                state.SsClipboardSync = (Settings.ClipboardSync == 1);
            }
        }
        */

        /*
        private void ProgressDialog_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (progressValue < 100)
            {
                progressDialog.ReportProgress(progressValue);
                System.Threading.Thread.Sleep(100);

                if (progressDialog.CancellationPending)
                {
                    rc.FileTransferDownloadCancel();
                    break;
                }
            }
        }
        */

        private bool SwitchToLegacyRendering()
        {
            if (rcv.SwitchToLegacy())
            {
                return true;
            }

            return false;
        }

        private bool SwitchToMultiScreenRendering()
        {
            if (rcv.SwitchToMultiScreen())
            {
                rc.state.UseMultiScreenOverview = false;
                rcv.CameraToCurrentScreen();

                return true;
            }

            return false;
        }

        private void SyncClipboard(object sender, EventArgs e)
        {
            if (rc == null)
                return;

            try
            {
                if (rc.state.SsClipboardSync && rc.state.ControlEnabled)
                {
                    string temp = clipboard;
                    this.ToolClipboardSend_Click(sender, e);
                    if (clipboard != temp)
                    {
                        toolClipboardSend.Background = System.Windows.Media.Brushes.Orange;
                        timerClipboard.Start();
                    }
                    //Console.WriteLine("[Clipboard sync] Success?");
                }
            }
            catch (Exception)
            {
                //Console.WriteLine("[Clipboard sync] Fail");
            }
        }

        private void TimerClipboard_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke((Action)delegate {
                toolClipboardSend.Background = System.Windows.Media.Brushes.Transparent;
                toolClipboardGet.Background = System.Windows.Media.Brushes.Transparent;
            });
            timerClipboard.Stop();
        }

        private void ToolBlockMouseKB_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null)
                return;

            toolBlockMouseKB.IsChecked = !toolBlockMouseKB.IsChecked;
            rc.SendBlackScreenBlockInput(toolBlockScreen.IsChecked, toolBlockMouseKB.IsChecked);
        }

        private void ToolBlockScreen_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null)
                return;

            toolBlockScreen.IsChecked = !toolBlockScreen.IsChecked;
            rc.SendBlackScreenBlockInput(toolBlockScreen.IsChecked, toolBlockMouseKB.IsChecked);
        }

        private void ToolClipboardAutotype_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null || !rc.state.ControlEnabled || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            PerformAutotype(true);
        }

        private void ToolClipboardGet_Click(object sender, RoutedEventArgs e)
        {
            if (clipboard.Length > 0)
                Clipboard.SetDataObject(clipboard);
        }

        private void ToolClipboardPaste_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null || !rc.state.ControlEnabled || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            string text = Clipboard.GetText().Trim();
            rc.SendPasteClipboard(text);
        }

        private void ToolClipboardSend_Click(object sender, EventArgs e)
        {
            if (rc == null || rc.state == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            clipboard = Clipboard.GetText();
            rc.SendClipboard(clipboard);
            toolClipboardSendText.Text = clipboard.Truncate(50);

            //if (Settings.ClipboardSync > 0) toolClipboardGetText.Text = clipboard.Truncate(50);
        }

        private void ToolClipboardSend_MouseEnter(object sender, MouseEventArgs e)
        {
            clipboard = Clipboard.GetText();
            toolClipboardSendText.Text = clipboard.Truncate(50);
        }

        private void ToolClipboardSync_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null)
                return;

            rc.state.SsClipboardSync = !rc.state.SsClipboardSync;
        }

        private void ToolDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null)
                return;

            //if (sessionId == null)
            //NotifySocketClosed(null);
            //else
            //rc.Disconnect();

            ConnectionManager.DisconnectRC(rc);
        }

        private void ToolKeyWin_Click(object sender, RoutedEventArgs e)
        {
            KeyWinSet(!keyDownWin);
        }

        private void ToolMachineNoteLink_Click(object sender, RoutedEventArgs e)
        {
            string link = toolMachineNoteLink.Header.ToString();
            if (link.Contains("http"))
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

        private void toolOpenGLInfo_Click(object sender, RoutedEventArgs e)
        {
            if (App.SupportsOpenGL)
            {
                MessageBoxResult result = MessageBoxResult.OK;
                if (rcv is RCvOpenGLWPF)
                    result = MessageBox.Show("Because you are using renderer GLWpfControl, you will need to manually reconnect Remote Control after this test. Would you like to proceed?", "KLC-Finch: OpenGL Info", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    OpenGLSoftwareTest glSoftwareTest = new OpenGLSoftwareTest(50, 50, "OpenGL Test");
                    MessageBox.Show("Render capability: 0x" + System.Windows.Media.RenderCapability.Tier.ToString("X") + "\r\n\r\nOpenGL Version: " + glSoftwareTest.Version, "KLC-Finch: OpenGL Info");
                }
            } else
            {
                MessageBox.Show("Render capability: 0x" + System.Windows.Media.RenderCapability.Tier.ToString("X"), "KLC-Finch: Non-OpenGL Info");
            }
        }

        private void ToolOptions_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions winOptions = new WindowOptions(ref Settings, true)
            {
                Owner = Window.GetWindow(this)
            };
            winOptions.ShowDialog();

            if(rc != null)
            {
                rc.state.Start(Settings, rc.state.endpointOS);
                if (!rc.state.UseMultiScreenOverview) //Not in Overview?
                    rcv.CameraToCurrentScreen(); //Multi-Screen Alt Fit
            }
        }

        private void ToolPanicRelease_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null || !rc.state.ControlEnabled || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            rc.SendPanicKeyRelease();
            listHeldKeysMod.Clear();
            KeyWinSet(false);

            DebugKeyboard();
        }

        private void ToolReconnect_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null)
                return;

            //if (App.alternative != null && (socketAlive || App.alternative.socketActive))
            if (rc.state.socketAlive && !requiredApproval)
                rc.Reconnect();
            else
                ToolShowAlternative_Click(sender, e);
        }

        private void ToolScreen_Click(object sender, RoutedEventArgs e)
        {
            if (winScreens == null)
                return;

            TimeSpan span = DateTime.Now - winScreens.TimeDeactivated;
            if (Settings.ScreenSelectNew)
            {
                if (span.TotalMilliseconds < 500)
                    winScreens.Hide(); //Doesn't really do anything, will act same as Legacy
                else
                    winScreens.Show();
            }

            if (!winScreens.IsVisible)
            {
                //Otherwise the old menu is shown
                (sender as Button).ContextMenu.IsEnabled = true;
                (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
                (sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                (sender as Button).ContextMenu.IsOpen = true;
            }
        }

        private void ToolScreen_ItemClicked(object sender, RoutedEventArgs e)
        {
            MenuItem source = (MenuItem)e.Source;
            string[] screen_selected = source.Header.ToString().Split(':');

            rc.state.previousScreen = rc.state.CurrentScreen;
            rc.state.CurrentScreen = rc.state.ListScreen.First(x => x.screen_name == screen_selected[0]);
            ChangeScreen(rc.state.CurrentScreen.screen_id);

            if (rc.state.UseMultiScreen)
                rcv.CameraToCurrentScreen();

            lblScreen.Content = rc.state.CurrentScreen.screen_name;
            toolScreen.ToolTip = rc.state.CurrentScreen.StringResPos();
            foreach (MenuItem item in toolScreen.ContextMenu.Items)
            {
                item.IsChecked = (item == source);
            }
        }

        private void ToolScreenMode_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null)
                return;
            else if (rc.state.UseMultiScreen)
            {
                SwitchToLegacyRendering();
            }
            else
            {
                SwitchToMultiScreenRendering();
            }
        }

        private void ToolScreenOverview_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null)
                return;

            rc.state.UseMultiScreenOverview = !rc.state.UseMultiScreenOverview;
            if (rc.state.UseMultiScreenOverview)
            {
                SetControlEnabled(rc.state, false);
                rcv.CameraToOverview();
            }
            else
            {
                rcv.CameraToCurrentScreen();
            }
        }

        private void ToolScreenshotToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            rc.CaptureNextScreen();
        }

        private void ToolSendCtrlAltDel_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null || !rc.state.ControlEnabled || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            rc.SendSecureAttentionSequence();
        }

        private void ToolShowAlternative_Click(object sender, RoutedEventArgs e)
        {
            if(ConnectionManager.Active != null)
                ConnectionManager.Active.ShowAlternativeWindow(rcv);
        }

        private void ToolShowMouse_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null)
                return;

            if(toolShowMouse.Opacity == 0.5)
            {
                toolShowMouse.Opacity = 1;
                rc.ShowCursor(true);
            } else
            {
                toolShowMouse.Opacity = 0.5;
                rc.ShowCursor(false);
            }
        }

        private void ToolToggleControl_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null)
                return;

            SetControlEnabled(rc.state, !rc.state.ControlEnabled);
        }

        private void ToolTSSession_ItemClicked(object sender, RoutedEventArgs e)
        {
            MenuItem source = (MenuItem)e.Source;
            source.IsChecked = true;
            string session_selected = source.Header.ToString();

            TSSession selectedTSSession = rc.state.listTSSession.First(x => x.session_name == session_selected);
            if (rc.state.currentTSSession == selectedTSSession)
                return; //There's a bug with being in legacy, selecting the same session, it changes back to multi-screen but the mouse is wrong.

            rc.state.currentTSSession = selectedTSSession;
            rc.ChangeTSSession(rc.state.currentTSSession.session_id);

            rc.state.UseMultiScreen = Settings.StartMultiScreen;

            toolTSSession.Content = rc.state.currentTSSession.session_name;

            foreach (MenuItem item in toolTSSession.ContextMenu.Items)
            {
                item.IsChecked = (item == source);
            }
        }

        private void ToolUpdateScreenLayout_Click(object sender, RoutedEventArgs e)
        {
            UpdateScreenLayoutHack();
        }

        private void toolViewRCLogs_Click(object sender, RoutedEventArgs e)
        {
            if (ConnectionManager.Active == null || ConnectionManager.Active.LCSession == null)
                return;

            string logs = ConnectionManager.Active.LCSession.agent.GetAgentRemoteControlLogs();
            MessageBox.Show(logs, "KLC-Finch: Remote Control Logs");
        }

        private void toolPanZoom_Click(object sender, RoutedEventArgs e)
        {
            if (rcv == null || rc == null || rc.state == null)
                return;

            rcv.TogglePanZoom();
            if(rc.state.UseMultiScreenPanZoom)
                SetControlEnabled(rc.state, false);
        }

        /*
        private void ToolZoomIn_Click(object sender, RoutedEventArgs e) {
            rcv.ZoomIn();
            //DebugKeyboard();
            rcv.Refresh();
        }

        private void ToolZoomOut_Click(object sender, RoutedEventArgs e) {
            rcv.ZoomOut();
            //DebugKeyboard();
            rcv.Refresh();
        }
        */

        public void WinActivated(object sender, EventArgs e)
        {
            if (rc == null || !rc.state.ControlEnabled || rc.state.CurrentScreen == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            KeyHookSet(true);
            rc.state.windowActivatedMouseMove = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            timerClipboard.Stop();
            timerHealth.Stop();
            rcv.ControlUnload();

            //if (App.alternative != null && App.alternative.Visibility != Visibility.Visible)
                //Environment.Exit(0);
        }

        public void Window_Closing(/*object sender, System.ComponentModel.CancelEventArgs e*/)
        {
            if(winRCFileTransfer != null && winRCFileTransfer.IsVisible)
                winRCFileTransfer.Close();

            timerHealth.Elapsed -= CheckHealth;

            if (keyHook.IsActive)
                keyHook.Uninstall();

            //NotifySocketClosed(sessionId);
            //rc.Disconnect();

            clipboardMon.OnUpdate -= SyncClipboard;
        }

        public void WinDeactivated(object sender, EventArgs e)
        {
            if (rc == null)
                return;

            KeyHookSet(true);
            if (!rc.state.ControlEnabled || rc.state.CurrentScreen == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            //Release modifier keys because the remote control window lost focus
            if (listHeldKeysMod.Count > 0)
            {
                foreach (KeycodeV3 k in listHeldKeysMod)
                    rc.SendKeyUp(k.JavascriptKeyCode, k.USBKeyCode);
                listHeldKeysMod.Clear();

                KeyWinSet(false);

                DebugKeyboard();
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (rc == null)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //if (progressDialog != null && progressDialog.IsBusy)
                //return;

                if (winRCFileTransfer != null)
                    winRCFileTransfer.Topmost = false;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                bool doUpload = false;
                //bool showExplorer = false;
                using (TaskDialog dialog = new TaskDialog())
                {
                    if (files.Length > 1)
                    {
                        dialog.WindowTitle = "KLC-Finch: Upload Multiple Files";
                        dialog.MainInstruction = "Upload " + files.Length + " dropped files to KRCTransferFiles?";
                    }
                    else
                    {
                        dialog.WindowTitle = "KLC-Finch: Upload File";
                        dialog.MainInstruction = "Upload dropped file to KRCTransferFiles?";
                    }
                    dialog.MainIcon = TaskDialogIcon.Information;
                    dialog.CenterParent = true;

                    long totalSize = 0;
                    StringBuilder sb = new StringBuilder();
                    foreach (string file in files)
                    {
                        sb.AppendLine(System.IO.Path.GetFileName(file));
                        FileInfo fi = new FileInfo(file);
                        totalSize += fi.Length;
                    }
                    StringBuilder totalSb = new StringBuilder(32);
                    FormatKbSizeConverter.StrFormatByteSizeW(totalSize, totalSb, totalSb.Capacity);
                    dialog.Content = totalSb.ToString() + "\r\n\r\n" + sb.ToString();

                    //dialog.VerificationText = "Open file explorer when complete";
                    //dialog.IsVerificationChecked = true;

                    TaskDialogButton tdbYes = new TaskDialogButton(ButtonType.Yes);
                    TaskDialogButton tdbCancel = new TaskDialogButton(ButtonType.Cancel);
                    dialog.Buttons.Add(tdbYes);
                    dialog.Buttons.Add(tdbCancel);

                    Window parent = App.winCharm != null ? (Window)App.winCharm : (Window)App.winStandaloneViewer;
                    TaskDialogButton button = dialog.ShowDialog(parent);
                    doUpload = (button == tdbYes);
                    //showExplorer = dialog.IsVerificationChecked;
                }

                if (winRCFileTransfer != null)
                    winRCFileTransfer.Topmost = true;

                if (doUpload)
                {
                    ShowFileTransfer();
                    //winRCFileTransfer.GoToTabUpload();
                    rc.FileTransferUpload(files);
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            System.Windows.Forms.KeyEventArgs e2 = e.ToWinforms();
            Window_KeyUp2(e2);
            e.Handled = true;
        }

        private void Window_KeyUp2(System.Windows.Forms.KeyEventArgs e2)
        {
            if (rc == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            if (e2.KeyCode == System.Windows.Forms.Keys.Scroll)
            {
                KeyHookSet(true);
                return;
            }
            else if (e2.KeyCode == System.Windows.Forms.Keys.PrintScreen)
            {
                rc.CaptureNextScreen();
            }
            else if (!rc.state.ControlEnabled)
            {
                if (e2.KeyCode == System.Windows.Forms.Keys.F2)
                    SetControlEnabled(rc.state, true);
                return;
            }

            if (e2.KeyCode == System.Windows.Forms.Keys.Pause)
            {
                rc.SendPanicKeyRelease();
                listHeldKeysMod.Clear();
                KeyWinSet(false);
            }
            else
            {
                if (KeycodeV3.Unhandled.ContainsKey(e2.KeyCode))
                    return;

                try
                {
                    KeycodeV3 keykaseya = KeycodeV3.Dictionary[e2.KeyCode];

                    if (rc.state.endpointOS == Agent.OSProfile.Mac)
                    {
                        if (Settings.MacSafeKeys && !keykaseya.IsMacSafe) return;

                        if (Settings.MacSwapCtrlWin)
                        {
                            if (KeycodeV3.ModifiersControl.Contains(e2.KeyCode))
                                keykaseya = keywin;
                            else if (e2.KeyCode == System.Windows.Forms.Keys.LWin)
                                keykaseya = keyctrl;
                        }
                    }

                    if (keykaseya == null)
                        throw new KeyNotFoundException(e2.KeyCode.ToString());

                    bool removed = (keykaseya.IsMod ? listHeldKeysMod.Remove(keykaseya) : listHeldKeysOther.Remove(keykaseya));

                    rc.SendKeyUp(keykaseya.JavascriptKeyCode, keykaseya.USBKeyCode);

                    if (keyHook.IsActive)
                    {
                        if (e2.KeyCode == System.Windows.Forms.Keys.LWin || e2.KeyCode == System.Windows.Forms.Keys.RWin)
                            KeyWinSet(false);
                    }
                    else
                    {
                        if (keyDownWin && rc.state.endpointOS != Agent.OSProfile.Mac)
                        {
                            foreach (KeycodeV3 k in listHeldKeysOther)
                                rc.SendKeyUp(k.JavascriptKeyCode, k.USBKeyCode);
                            listHeldKeysOther.Clear();
                            foreach (KeycodeV3 k in listHeldKeysMod)
                                rc.SendKeyUp(k.JavascriptKeyCode, k.USBKeyCode);
                            listHeldKeysMod.Clear();

                            KeyWinSet(false);
                        }
                    }
                }
                catch
                {
                    //Console.WriteLine("Up scan: " + e2.KeyCode + " / " + e2.KeyData + " / " + e2.KeyValue);
                }
            }

            DebugKeyboard();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            winScreens.Owner = Window.GetWindow(this);
            KeyHookSet(true);

            //rcv.ControlLoaded(rc);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //Apparently preview is used because arrow keys?

            System.Windows.Forms.KeyEventArgs e2 = e.ToWinforms();
            Window_PreviewKeyDown2(e2);
            e.Handled = true;
        }

        private bool Window_PreviewKeyDown2(System.Windows.Forms.KeyEventArgs e2)
        {
            if (rc == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return false;

            if (e2.KeyCode == System.Windows.Forms.Keys.F1)
            {
                SetControlEnabled(rc.state, false);
                if (!rc.state.UseMultiScreenPanZoom)
                    rcv.CameraToOverview();
            }

            if (!rc.state.ControlEnabled)
            {
                if (rc.state.UseMultiScreenPanZoom)
                {
                    if (Keyboard.IsKeyDown(Key.W))
                        rcv.MoveUp();
                    else if (Keyboard.IsKeyDown(Key.S))
                        rcv.MoveDown();
                    if (Keyboard.IsKeyDown(Key.A))
                        rcv.MoveLeft();
                    else if (Keyboard.IsKeyDown(Key.D))
                        rcv.MoveRight();
                }
                return false;
            }

            if (e2.KeyCode == System.Windows.Forms.Keys.Pause || e2.KeyCode == System.Windows.Forms.Keys.Scroll)
            {
                //Done on release
                return true;
            }
            else if (e2.KeyCode == System.Windows.Forms.Keys.Oemtilde && e2.Control)
            {
                PerformAutotype(false);
            }
            else if (e2.KeyCode == System.Windows.Forms.Keys.V && e2.Control && e2.Shift)
            {
                PerformAutotype(false);
            }
            else
            {
                if (KeycodeV3.Unhandled.ContainsKey(e2.KeyCode))
                    return false;

                try
                {
                    KeycodeV3 keykaseya = KeycodeV3.Dictionary[e2.KeyCode];

                    if (keykaseya == null)
                        throw new KeyNotFoundException(e2.KeyCode.ToString());

                    if (rc.state.endpointOS == Agent.OSProfile.Mac)
                    {
                        if (Settings.MacSafeKeys && !keykaseya.IsMacSafe) return false;

                        if (Settings.MacSwapCtrlWin)
                        {
                            if (KeycodeV3.ModifiersControl.Contains(e2.KeyCode))
                                keykaseya = keywin;
                            else if (e2.KeyCode == System.Windows.Forms.Keys.LWin)
                                keykaseya = keyctrl;
                        }
                    }

                    if (e2.KeyCode == System.Windows.Forms.Keys.LWin || e2.KeyCode == System.Windows.Forms.Keys.RWin)
                        KeyWinSet(true);

                    if (keykaseya.IsMod)
                    {
                        if (!listHeldKeysMod.Contains(keykaseya))
                            listHeldKeysMod.Add(keykaseya);
                    }
                    else
                    {
                        if (!listHeldKeysOther.Contains(keykaseya))
                            listHeldKeysOther.Add(keykaseya);
                    }

                    //Still allow holding it down
                    rc.SendKeyDown(keykaseya.JavascriptKeyCode, keykaseya.USBKeyCode);
                }
                catch
                {
                    //Console.WriteLine("DOWN scan: " + e2.KeyCode + " / " + e2.KeyData + " / " + e2.KeyValue);
                }

                DebugKeyboard();
            }

            return true;
        }

        public void WinStateChanged(object sender, EventArgs e, WindowState ws)
        {
            if (ws == System.Windows.WindowState.Minimized)
            {
                if (Settings.PowerSaveOnMinimize)
                    rcv.ParentStateChange(false);
            }
            else
            {
                rcv.ParentStateChange(true);
            }
        }

        private void ShowFileTransfer()
        {
            if (winRCFileTransfer == null || !winRCFileTransfer.IsVisible)
            {
                winRCFileTransfer = new(rc)
                {
                    Owner = Window.GetWindow(this)
                };
                winRCFileTransfer.Show();
            }

            Window win = (App.winCharm != null ? App.winCharm : App.winStandaloneViewer);
            System.Windows.Point point = rcv.TransformToAncestor(win).Transform(new System.Windows.Point(0, 0));

            if (win.WindowState == WindowState.Maximized)
            {
                IntPtr handle = new System.Windows.Interop.WindowInteropHelper(win).Handle;
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(handle);

                point.X += (screen.WorkingArea.Width / 2);
                point.X -= (winRCFileTransfer.Width / 2);

                winRCFileTransfer.Left = screen.WorkingArea.Left + point.X + 2;
                winRCFileTransfer.Top = screen.WorkingArea.Top + SystemParameters.CaptionHeight + point.Y + 10;
            }
            else
            {
                point.X += (win.Width / 2);
                point.X -= (winRCFileTransfer.Width / 2);

                winRCFileTransfer.Left = win.Left + point.X + 8;
                winRCFileTransfer.Top = win.Top + SystemParameters.CaptionHeight + point.Y + 15;
            }
        }

        private void ToolFileTransfer_Click(object sender, RoutedEventArgs e)
        {
            ShowFileTransfer();
        }

        private void toolButtonContext(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsEnabled = true;
            (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
            (sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            (sender as Button).ContextMenu.IsOpen = true;
        }

        private void ChangeScreen(string screen_id)
        {
            if (rc == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            if (rc.state.QualityWidth == 0 || rc.state.QualityHeight == 0)
            {
                rc.ChangeScreen(screen_id, (int)rcv.ActualWidth, (int)rcv.ActualHeight, rc.state.QualityDownscale);
            }
            else
            {
                rc.ChangeScreen(screen_id, rc.state.QualityWidth, rc.state.QualityHeight, rc.state.QualityDownscale);
            }
        }

        private void ToolStreamQuality_Click(object sender, RoutedEventArgs e)
        {
            QualityCallback callback = new QualityCallback(QualityUpdate);
            WindowStreamQuality winStreamQuality = new WindowStreamQuality(callback, rc.state.QualityDownscale, rc.state.QualityWidth, rc.state.QualityHeight)
            {
                Owner = Window.GetWindow(this)
            };
            winStreamQuality.ShowDialog();
        }

        public delegate void QualityCallback(int downscale, int width, int height);
        public void QualityUpdate(int downscale, int width, int height)
        {
            rc.state.QualityDownscale = downscale;
            rc.state.QualityWidth = width;
            rc.state.QualityHeight = height;
            ChangeScreen(rc.state.CurrentScreen.screen_id);
        }

        /*
        private void ToolScreenFix_Click(object sender, RoutedEventArgs e)
        {
            if (rc == null || rc.state == null)
                return;

            if (rc.state.CurrentScreen.rect.Width != rc.state.legacyVirtualWidth)
            {
                //rc.state.CurrentScreen.rect.Width = rc.state.legacyVirtualWidth;
                //rc.state.CurrentScreen.rect.Height = rc.state.legacyVirtualHeight;

                if (rc.state.UseMultiScreen)
                {
                    //Retina hack
                    if (rc.state.UseMultiScreenOverview)
                        rcv.CameraToOverview();
                    else
                        rcv.CameraToCurrentScreen();
                }
            }

            UpdateScreenLayoutReflow();
            rc.state.UseMultiScreenFixAvailable = false;
        }
        */

    }
}

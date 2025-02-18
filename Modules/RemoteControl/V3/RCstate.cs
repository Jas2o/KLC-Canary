using LibKaseya;
using NTR;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using static LibKaseya.Enums;

namespace KLC_Finch
{

    public enum ConnectionStatus
    {
        FirstConnectionAttempt,
        Connected,
        Disconnected
    }

    public enum ScreenStatus
    {
        Preparing,
        LayoutReady,
        Stable
    }

    public class RCstate : INotifyPropertyChanged
    {
        private static string[] arrAdmins = new string[] { "administrator" };

        public Agent.OSProfile endpointOS;
        public string endpointLastUser;

        public bool autotypeAlwaysConfirmed = false;
        public string BaseTitle;
        public ConnectionStatus connectionStatus = ConnectionStatus.FirstConnectionAttempt;
        public RCScreen CurrentScreen;
        public long lastLatency;
        public RCScreen legacyScreen;
        public int legacyVirtualWidth, legacyVirtualHeight;
        public List<RCScreen> ListScreen;
        public bool mouseHeldLeft = false;
        public bool mouseHeldRight = false;
        public bool powerSaving;
        public RCScreen previousScreen;
        public bool requiredApproval = false;
        public ScreenStatus screenStatus = ScreenStatus.Preparing;
        public string sessionId;
        public bool socketAlive = false;
        public TextureCursor textureCursor = null;
        public TextureScreen textureLegacy;
        public Rectangle virtualCanvas, virtualViewWant, virtualViewNeed;
        public bool virtualRequireViewportUpdate = false;
        public int virtualWidth, virtualHeight;
        public bool windowActivatedMouseMove;
        public readonly List<TSSession> listTSSession = new List<TSSession>();
        public TSSession currentTSSession = null;
        public readonly FPSCounter fpsCounter;
        public double fpsLast;

        public int QualityDownscale = 1;
        public int QualityWidth = 0;
        public int QualityHeight = 0;

        public RCstate()
        {
            ListScreen = new List<RCScreen>();

            DependencyObject dep = new DependencyObject();
            if (DesignerProperties.GetIsInDesignMode(dep))
            {
                useMultiScreen = true;
            }

            legacyScreen = new RCScreen("Legacy", "Legacy", 800, 600, 0, 0);
            fpsCounter = new FPSCounter();
        }

        /*
        public RCstate(WindowCharm window)
        {
            Console.WriteLine("! RCstate with WindowCharm is not yet supported !");
            //Window = null;
            ListScreen = new List<RCScreen>();

            DependencyObject dep = new DependencyObject();
            if (DesignerProperties.GetIsInDesignMode(dep))
            {
                useMultiScreen = true;
            }
        }
        */

        public void Start(Settings settings, Agent.OSProfile endpointOS = Agent.OSProfile.Other, string endpointLastUser = "") //bool isStart = false
        {
            if (this.connectionStatus == ConnectionStatus.Disconnected)
                this.connectionStatus = ConnectionStatus.FirstConnectionAttempt;
            this.endpointOS = endpointOS;
            this.endpointLastUser = endpointLastUser;

            //if (isStart)
            //{
            /*
            if (rcv.SupportsLegacy)
            {
            */
            if (endpointOS == Agent.OSProfile.Mac && settings.StartMultiScreenExceptMac)
                UseMultiScreen = false;
            else
                UseMultiScreen = settings.StartMultiScreen;
            /*
            }
            else
            {
                UseMultiScreen = true;
            }
            */
            //}

            autotypeAlwaysConfirmed = settings.AutotypeSkipLengthCheck;
            /*
            ssKeyHookAllow = settings.KeyboardHook;
            KeyHookSet(false);
            */

            if (settings.ClipboardSync == 2 && (endpointOS == Agent.OSProfile.Server || (endpointLastUser != null && arrAdmins.Contains(endpointLastUser.ToLower()))))
            {
                //Server/Admin only
                SsClipboardSync = true;
            }
            else
            {
                ssClipboardSync = (settings.ClipboardSync == 1);
            }
        }

        public void SetSessionID(string sessionId)
        {
            this.sessionId = sessionId;

            if (sessionId != null)
            {
                socketAlive = true;
                //connectionStatus = ConnectionStatus.Connected;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool controlEnabled;
        private bool useMultiScreen;
        private bool useMultiScreenOverview;
        private bool useMultiScreenPanZoom;
        private bool useMultiScreenFixAvailable;
        private bool ssClipboardSync;
        private bool hasFileTransferWaiting;

        public void SetTitle(string title, RC mode)
        {
            switch (mode)
            {
                case RC.Shared:
                    BaseTitle = title + "::Shared";
                    break;
                case RC.Private:
                    BaseTitle = title + "::Private";
                    //toolReconnect.Header = "Reconnect (lose private session)";
                    break;
                case RC.OneClick:
                    BaseTitle = title + "::1-Click";
                    //toolReconnect.Header = "Reconnect (lose 1-Click session)";
                    break;
                default:
                    BaseTitle = title + "::UNSUPPORTED";
                    break;
            }
        }

        public void ForceRefresh()
        {
            //This is intended to be used when switching active RC/State.
            NotifyPropertyChanged("ControlEnabled");
            NotifyPropertyChanged("ControlEnabledText");
            NotifyPropertyChanged("ControlEnabledTextWeight");

            NotifyPropertyChanged("UseMultiScreen");
            NotifyPropertyChanged("ScreenModeText");

            NotifyPropertyChanged("UseMultiScreenOverview");
            NotifyPropertyChanged("UseMultiScreenOverviewTextWeight");

            NotifyPropertyChanged("UseMultiScreenPanZoom");

            NotifyPropertyChanged("UseMultiScreenFixAvailable");

            NotifyPropertyChanged("SsClipboardSync");
            NotifyPropertyChanged("SsClipboardSyncReceiveOnly");

            NotifyPropertyChanged("HasFileTransferWaiting");
        }

        public bool ControlEnabled
        {
            get { return controlEnabled; }
            set
            {
                controlEnabled = value;
                NotifyPropertyChanged("ControlEnabled");
                NotifyPropertyChanged("ControlEnabledText");
                NotifyPropertyChanged("ControlEnabledTextWeight");
            }
        }
        public string ControlEnabledText
        {
            get
            {
                if (controlEnabled)
                    return "Control Enabled";
                else
                    return "Control Disabled";
            }
        }
        public FontWeight ControlEnabledTextWeight
        {
            get
            {
                if (controlEnabled)
                    return FontWeights.Normal;
                else
                    return FontWeights.Bold;
            }
        }
        public bool UseMultiScreen
        {
            get { return useMultiScreen; }
            set
            {
                useMultiScreen = value;
                NotifyPropertyChanged("UseMultiScreen");
                NotifyPropertyChanged("ScreenModeText");
            }
        }
        public string ScreenModeText
        {
            get
            {
                if (useMultiScreen)
                    return "Multi";
                else
                    return "Legacy";
            }
        }
        public bool UseMultiScreenOverview
        {
            get { return useMultiScreenOverview; }
            set
            {
                useMultiScreenOverview = value;
                UseMultiScreenFixAvailable = false;
                NotifyPropertyChanged("UseMultiScreenOverview");
                NotifyPropertyChanged("UseMultiScreenOverviewTextWeight");
            }
        }
        public FontWeight UseMultiScreenOverviewTextWeight
        {
            get
            {
                if (useMultiScreenOverview)
                    return FontWeights.Bold;
                else
                    return FontWeights.Normal;
            }
        }
        public bool UseMultiScreenPanZoom
        {
            get { return useMultiScreenPanZoom; }
            set
            {
                useMultiScreenPanZoom = value;
                NotifyPropertyChanged("UseMultiScreenPanZoom");
            }
        }
        public bool UseMultiScreenFixAvailable
        {
            get { return useMultiScreenFixAvailable; }
            set
            {
                useMultiScreenFixAvailable = useMultiScreen ? value : false;
                NotifyPropertyChanged("UseMultiScreenFixAvailable");
            }
        }
        public bool SsClipboardSync
        {
            get { return ssClipboardSync; }
            set
            {
                ssClipboardSync = value;
                NotifyPropertyChanged("SsClipboardSync");
                NotifyPropertyChanged("SsClipboardSyncReceiveOnly");
            }
        }
        public bool SsClipboardSyncReceiveOnly
        {
            get { return !ssClipboardSync; }
        }

        public bool HasFileTransferWaiting
        {
            get { return hasFileTransferWaiting; }
            set
            {
                hasFileTransferWaiting = value;
                NotifyPropertyChanged("HasFileTransferWaiting");
            }
        }

        public void UpdateScreenLayout(dynamic json)
        {
            screenStatus = ScreenStatus.Preparing;

            ListScreen.Clear();
            previousScreen = CurrentScreen = null;

            if (json == null)
                return;

            string default_screen = json["default_screen"].ToString();
            connectionStatus = ConnectionStatus.Connected;

            foreach (dynamic screen in json["screens"])
            {
                string screen_id = screen["screen_id"].ToString(); //int or BigInteger
                string screen_name = (string)screen["screen_name"];
                int screen_height = (int)screen["screen_height"];
                int screen_width = (int)screen["screen_width"];
                int screen_x = (int)screen["screen_x"];
                int screen_y = (int)screen["screen_y"];

                //Add Screen
                RCScreen newScreen = new RCScreen(screen_id, screen_name, screen_height, screen_width, screen_x, screen_y);
                ListScreen.Add(newScreen);

                if (screen_id == default_screen)
                {
                    CurrentScreen = newScreen;
                    //Legacy
                    legacyVirtualHeight = CurrentScreen.rect.Height;
                    legacyVirtualWidth = CurrentScreen.rect.Width;
                    virtualRequireViewportUpdate = true;
                }
            }

            int lowestX = ListScreen.Min(x => x.rect.X);
            int lowestY = ListScreen.Min(x => x.rect.Y);
            int highestX = ListScreen.Max(x => x.rect.Right);
            int highestY = ListScreen.Max(x => x.rect.Bottom);
            virtualCanvas = new Rectangle(lowestX, lowestY, highestX - lowestX, highestY - lowestY);

            /*
            rcv.SetCanvas(lowestX, lowestY, highestX, highestY);

            rcv.UpdateScreenLayout(lowestX, lowestY, highestX, highestY);

            rc.UpdateScreens(jsonstr);
            winScreens.UpdateStartScreens(jsonstr);
            winScreens.SetCanvas(lowestX, lowestY, highestX, highestY);
            */

            screenStatus = ScreenStatus.LayoutReady;
        }

        public RCScreen GetScreenUsingMouse(int x, int y)
        {
            if (CurrentScreen == null)
                return null;
            if (CurrentScreen.rect.Contains(x, y))
                return CurrentScreen;

            //This doesn't yet work in Canvas
            foreach (RCScreen screen in ListScreen)
            {
                if (screen == CurrentScreen)
                    continue;

                if (screen.rect.Contains(x, y))
                    return screen;
            }
            return null;
        }

        public RCScreen GetClosestScreenUsingMouse(int x, int y)
        {
            if (CurrentScreen == null)
                return null;
            if (CurrentScreen.rectEdge.Contains(x, y))
                return CurrentScreen;

            //This doesn't yet work in Canvas
            foreach (RCScreen screen in ListScreen)
            {
                if (screen == CurrentScreen)
                    continue;

                if (screen.rectEdge.Contains(x, y))
                    return screen;
            }
            return null;
        }

        public void SetVirtual(int virtualX, int virtualY, int virtualWidth, int virtualHeight)
        {
            if (UseMultiScreen)
            {
                virtualViewWant = new Rectangle(virtualX, virtualY, virtualWidth, virtualHeight);
            }
            else
            {
                this.legacyVirtualWidth = virtualWidth;
                this.legacyVirtualHeight = virtualHeight;
                virtualCanvas = virtualViewWant = new Rectangle(0, 0, virtualWidth, virtualHeight);
            }

            virtualRequireViewportUpdate = true;
        }

        public bool WindowIsActive()
        {
            return true;
            //return Window.IsActive;
        }

        public void ZoomIn()
        {
            if (!UseMultiScreen)
                return;
            if (virtualViewWant.Width - 200 < 0 || virtualViewWant.Height - 200 < 0)
                return;

            virtualViewWant = new Rectangle(virtualViewWant.X + 100, virtualViewWant.Y + 100, virtualViewWant.Width - 200, virtualViewWant.Height - 200);
            virtualRequireViewportUpdate = true;
        }

        public void ZoomOut()
        {
            if (!UseMultiScreen)
                return;

            virtualViewWant = new Rectangle(virtualViewWant.X - 100, virtualViewWant.Y - 100, virtualViewWant.Width + 200, virtualViewWant.Height + 200);
            virtualRequireViewportUpdate = true;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadCursor(int cursorX, int cursorY, int cursorWidth, int cursorHeight, int cursorHotspotX, int cursorHotspotY, byte[] remaining)
        {
            if (textureCursor != null)
                textureCursor.Load(new Rectangle(cursorX - cursorHotspotX, cursorY - cursorHotspotY, cursorWidth, cursorHeight), remaining);
        }

        public void LoadTexture(int width, int height, Bitmap decomp)
        {
            if (screenStatus == ScreenStatus.Preparing)
                return;

            if (UseMultiScreen)
            {
                if (CurrentScreen == null)
                {
                    //Console.WriteLine("[LoadTexture] No matching RCScreen for screen ID: " + screenID);
                    //listScreen might be empty
                    return;
                }

                if (CurrentScreen.Texture != null)
                    CurrentScreen.Texture.Load(CurrentScreen.rect, decomp);
                else
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate {
                        if (CurrentScreen.CanvasImage == null)
                            CurrentScreen.CanvasImage = new System.Windows.Controls.Image();
                        CurrentScreen.CanvasImage.Width = CurrentScreen.rect.Width;
                        CurrentScreen.CanvasImage.Height = CurrentScreen.rect.Height;

                        CurrentScreen.SetCanvasImage(decomp);
                    });
                }
            }
            else
            {
                //Legacy
                if (CurrentScreen == null)
                    return;

                if (legacyVirtualWidth != CurrentScreen.rect.Width || legacyVirtualHeight != CurrentScreen.rect.Height)
                {
                    //Console.WriteLine("[LoadTexture:Legacy] Virtual resolution did not match texture received.");
                    SetVirtual(0, 0, CurrentScreen.rect.Width, CurrentScreen.rect.Height);
                }

                if (textureLegacy != null)
                    textureLegacy.Load(new Rectangle(0, 0, width, height), decomp);
                else
                {
                    legacyScreen.rect = new Rectangle(0, 0, width, height);

                    Application.Current.Dispatcher.Invoke((Action)delegate {
                        if (legacyScreen.CanvasImage == null)
                            legacyScreen.CanvasImage = new System.Windows.Controls.Image();
                        CurrentScreen.CanvasImage.Width = CurrentScreen.rect.Width;
                        CurrentScreen.CanvasImage.Height = CurrentScreen.rect.Height;

                        legacyScreen.SetCanvasImage(decomp);
                    });
                }

                /*
                textureLegacyWidth = width;
                textureLegacyHeight = height;

                BitmapData data = decomp.LockBits(new System.Drawing.Rectangle(0, 0, decomp.Width, decomp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                if (textureLegacyData != null && textureLegacyData.Length != Math.Abs(data.Stride * data.Height)) {
                    virtualRequireViewportUpdate = true;
                    Console.WriteLine("[LoadTexture:Legacy] Array needs to be resized");
                }

                if (textureLegacyData == null || virtualRequireViewportUpdate) {
                    Console.WriteLine("Rebuilding Legacy Texture Buffer");
                    textureLegacyData = new byte[Math.Abs(data.Stride * data.Height)];
                }
                Marshal.Copy(data.Scan0, textureLegacyData, 0, textureLegacyData.Length); //This can fail with re-taking over private remote control
                decomp.UnlockBits(data);

                textureLegacyNew = true;
                */
            }

            /*
            state.UseMultiScreenFixAvailable = (state.CurrentScreen.rect.Width != width);
            if (state.UseMultiScreenFixAvailable) {
                state.legacyVirtualWidth = width;
                state.legacyVirtualHeight = height;

                if (state.previousScreen == null)
                    ToolScreenFix_Click(null, null);
            }
            */
            socketAlive = true;

            fpsLast = fpsCounter.GetFPS();
            screenStatus = ScreenStatus.Stable;
        }

        public void LoadTextureRaw(byte[] buffer, int width, int height, int stride)
        {
            if (screenStatus == ScreenStatus.Preparing || width * height <= 0)
                return;

            if (UseMultiScreen)
            {
                if (CurrentScreen == null)
                {
                    //Console.WriteLine("[LoadTexture] No matching RCScreen for screen ID: " + screenID);
                    //listScreen might be empty
                    return;
                }

                if (CurrentScreen.Texture != null)
                {
                    CurrentScreen.Texture.LoadRaw(CurrentScreen.rect, width, height, stride, buffer);
                }
                else
                {
                    //Canvas
                    Application.Current.Dispatcher.Invoke((Action)delegate {
                        if (CurrentScreen.CanvasImage == null)
                            CurrentScreen.CanvasImage = new System.Windows.Controls.Image();
                        CurrentScreen.CanvasImage.Width = CurrentScreen.rect.Width;
                        CurrentScreen.CanvasImage.Height = CurrentScreen.rect.Height;

                        CurrentScreen.SetCanvasImageBW(width, height, stride, buffer);
                    });
                }
            }
            else
            {
                //Legacy
                if (CurrentScreen == null)
                    return;

                if (legacyVirtualWidth != CurrentScreen.rect.Width || legacyVirtualHeight != CurrentScreen.rect.Height)
                {
                    //Console.WriteLine("[LoadTexture:Legacy] Virtual resolution did not match texture received.");
                    SetVirtual(0, 0, CurrentScreen.rect.Width, CurrentScreen.rect.Height);
                }

                textureLegacy.LoadRaw(new Rectangle(0, 0, CurrentScreen.rect.Width, CurrentScreen.rect.Height), width, height, stride, buffer);
            }

            /*
            state.UseMultiScreenFixAvailable = (state.CurrentScreen.rect.Width != width);
            if (state.UseMultiScreenFixAvailable) {
                state.legacyVirtualWidth = width;
                state.legacyVirtualHeight = height;

                if (state.previousScreen == null)
                    ToolScreenFix_Click(null, null);
            }
            */
            socketAlive = true;

            fpsLast = fpsCounter.GetFPS();
            screenStatus = ScreenStatus.Stable;
        }
    }

    /*
    public enum GraphicsMode {
        OpenGL_YUV = 0,
        OpenGL_RGB = 1,
        OpenGL_WPF_YUV = 10,
        OpenGL_WPF_RGB = 11,
        Canvas_RGB = 20,
        Canvas_Y = 21
    }
    */
}
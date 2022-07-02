using NTR;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KLC_Finch {

    /// <summary>
    /// Interaction logic for RCvCanvas.xaml
    /// </summary>
    public partial class RCvCanvas : RCv {
        private List<System.Windows.Shapes.Rectangle> canvasListRectangle;
        private int canvasOffsetX, canvasOffsetY;

        private bool tempPanning;
        private System.Windows.Point tempPanningPoint;

        public RCvCanvas(/*IRemoteControl rc, RCstate state*/) : base(/*rc, state*/) {
            InitializeComponent();
            txtDebugLeft.Text = "";
            txtDebugRight.Text = "";
            //txtRcConnecting.Visibility = Visibility.Visible; //Default
            txtRcFrozen.Visibility = Visibility.Collapsed;
            txtRcDisconnected.Visibility = Visibility.Collapsed;
            txtZoom.Visibility = Visibility.Collapsed;

            ZoomSlider.Visibility = Visibility.Collapsed;
            rcScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            rcScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        }

        public override bool SupportsLegacy { get { return false; } }
        public override void CameraFromClickedScreen(RCScreen screen, bool moveCamera = true) {
            if (rc.state.UseMultiScreen && moveCamera)
                CameraToCurrentScreen();
        }

        public override void CameraToCurrentScreen() {
            if (!rc.state.UseMultiScreen || rc.state.CurrentScreen == null)
                return;

            rc.state.UseMultiScreenOverview = false;
            rc.state.UseMultiScreenPanZoom = false;
            //ZoomSlider.Visibility = Visibility.Collapsed;
            //rcScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            //rcScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

            if (App.Settings.MultiAltFit) {
                bool adjustLeft = false;
                bool adjustUp = false;
                bool adjustRight = false;
                bool adjustDown = false;

                foreach (RCScreen screen in rc.state.ListScreen) {
                    if (screen == rc.state.CurrentScreen)
                        continue;

                    if (screen.rect.Right <= rc.state.CurrentScreen.rect.Left)
                        adjustLeft = true;
                    if (screen.rect.Bottom <= rc.state.CurrentScreen.rect.Top)
                        adjustUp = true;
                    if (screen.rect.Left >= rc.state.CurrentScreen.rect.Right)
                        adjustRight = true;
                    if (screen.rect.Top >= rc.state.CurrentScreen.rect.Bottom)
                        adjustDown = true;
                }

                int virtualX = rc.state.CurrentScreen.rect.X - (adjustLeft ? 80 : 0);
                int virtualY = rc.state.CurrentScreen.rect.Y - (adjustUp ? 80 : 0);

                //SetCanvas(Math.Max(canvasOffsetX, virtualX), Math.Max(canvasOffsetY, virtualY),
                //state.CurrentScreen.rectFixed.Width + (adjustLeft ? 80 : 0) + (adjustRight ? 80 : 0),
                //state.CurrentScreen.rectFixed.Height + (adjustUp ? 80 : 0) + (adjustDown ? 80 : 0));

                SetCanvas(virtualX, virtualY,
                    rc.state.CurrentScreen.rect.Width + (adjustLeft ? 80 : 0) + (adjustRight ? 80 : 0),
                    rc.state.CurrentScreen.rect.Height + (adjustUp ? 80 : 0) + (adjustDown ? 80 : 0));
            } else
                SetCanvas(rc.state.CurrentScreen.rect.X, rc.state.CurrentScreen.rect.Y, rc.state.CurrentScreen.rect.Width, rc.state.CurrentScreen.rect.Height);

            //SetCanvas(state.CurrentScreen.rect.X, state.CurrentScreen.rect.Y, state.CurrentScreen.rect.Width, state.CurrentScreen.rect.Height);
        }

        public override void CameraToOverview() {
            if (!rc.state.UseMultiScreen)
                return;

            rc.state.UseMultiScreenOverview = true;
            rc.state.UseMultiScreenPanZoom = false;
            /*
            Dispatcher.Invoke((Action)delegate {
                ZoomSlider.Visibility = Visibility.Collapsed;
                rcScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                rcScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            });
            */

            int lowestX = 0;
            int lowestY = 0;
            int highestX = 0;
            int highestY = 0;
            foreach (RCScreen screen in rc.state.ListScreen) {
                lowestX = Math.Min(screen.rect.X, lowestX);
                lowestY = Math.Min(screen.rect.Y, lowestY);
                highestX = Math.Max(screen.rect.X + screen.rect.Width, highestX);
                highestY = Math.Max(screen.rect.Y + screen.rect.Height, highestY);
            }

            SetCanvas(lowestX, lowestY, Math.Abs(lowestX) + highestX, Math.Abs(lowestY) + highestY);
        }

        public override void TogglePanZoom() {
            rc.state.UseMultiScreenPanZoom = !rc.state.UseMultiScreenPanZoom;

            if(rc.state.UseMultiScreenPanZoom && App.Settings.DisplayOverlayPanZoom) {
                ZoomSlider.Visibility = Visibility.Visible;
                rcScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                rcScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            } else {
                ZoomSlider.Visibility = Visibility.Collapsed;
                rcScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                rcScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }

        public override void MoveDown() {
            rcScrollViewer.ScrollToVerticalOffset(rcScrollViewer.VerticalOffset + (rcScrollViewer.ActualHeight / 100));
        }

        public override void MoveLeft() {
            rcScrollViewer.ScrollToHorizontalOffset(rcScrollViewer.HorizontalOffset - (rcScrollViewer.ActualWidth / 100));
        }

        public override void MoveRight() {
            rcScrollViewer.ScrollToHorizontalOffset(rcScrollViewer.HorizontalOffset + (rcScrollViewer.ActualWidth / 100));
        }

        public override void MoveUp() {
            rcScrollViewer.ScrollToVerticalOffset(rcScrollViewer.VerticalOffset - (rcScrollViewer.ActualHeight / 100));
        }

        /*
        public override void ZoomIn() {
            state.UseMultiScreenPanZoom = true;

            ZoomSlider.Visibility = Visibility.Visible;
            rcScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            rcScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;

            ZoomSlider.Value += 0.05;
        }

        public override void ZoomOut() {
            state.UseMultiScreenPanZoom = true;

            ZoomSlider.Visibility = Visibility.Visible;
            rcScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            rcScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;

            ZoomSlider.Value -= 0.05;
        }
        */

        public override void CheckHealth() {
            if (rc == null)
                return;

            txtDebugLeft.Visibility = (App.Settings.DisplayOverlayKeyboardMod || App.Settings.DisplayOverlayKeyboardOther ? Visibility.Visible : Visibility.Collapsed);
            txtDebugRight.Visibility = (App.Settings.DisplayOverlayMouse ? Visibility.Visible : Visibility.Collapsed);
            txtZoom.Visibility = (App.Settings.DisplayOverlayPanZoom ? Visibility.Visible : Visibility.Collapsed);

            txtRcDisconnected.Visibility = (rc.state.connectionStatus == ConnectionStatus.Disconnected ? Visibility.Visible : Visibility.Collapsed);

            switch (rc.state.connectionStatus) {
                case ConnectionStatus.FirstConnectionAttempt:
                    txtRcFrozen.Visibility = Visibility.Collapsed;
                    txtRcConnecting.Visibility = Visibility.Visible;
                    break;

                case ConnectionStatus.Connected:
                    txtRcConnecting.Visibility = Visibility.Collapsed;
                    /*
                    if (state.fpsCounter.SeemsAlive(5000)) {
                        txtRcFrozen.Visibility = Visibility.Collapsed;
                    } else {
                        txtRcFrozen.Visibility = Visibility.Visible;
                    }
                    */
                    break;

                case ConnectionStatus.Disconnected:
                    txtRcControlOff1.Visibility = txtRcControlOff2.Visibility = txtRcNotify.Visibility = Visibility.Collapsed;
                    txtRcFrozen.Visibility = Visibility.Collapsed;
                    //txtRcDisconnected.Visibility = Visibility.Visible;
                    rcBorderBG.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Maroon);

                    /*
                    if (state.keyHook.IsActive) {
                        keyHook.Uninstall();
                        txtRcHookOn.Visibility = Visibility.Collapsed;
                    }
                    */
                    break;
            }
        }

        public override void ControlLoaded() {
            ////Legacy
            //if (!rc.state.UseMultiScreen)
                //rc.state.SetVirtual(0, 0, rc.state.virtualWidth, rc.state.virtualHeight);

            //rcCanvas.Children.Clear();
            rcRectangleExample.Visibility = Visibility.Hidden;
            canvasListRectangle = new List<System.Windows.Shapes.Rectangle>();

            /*
            if (App.Settings.RendererAlt) {
                rc.DecodeMode = DecodeMode.RawY;
                //ConnectionManager.Viewer.Title = state.BaseTitle + " (Canvas Y) Alpha";
            } else {
                rc.DecodeMode = DecodeMode.BitmapRGB;
                //ConnectionManager.Viewer.Title = state.BaseTitle + " (Canvas RGB) Alpha";
            }
            */

            rcBorderBG.MouseMove += HandleCanvasMouseMove;
            rcCanvas.MouseMove += HandleCanvasMouseMove;
            rcCanvas.MouseDown += HandleCanvasMouseDown;
            rcCanvas.MouseUp += HandleCanvasMouseUp;
            rcCanvas.MouseLeave += HandleCanvasMouseLeave;
            rcCanvas.MouseWheel += HandleCanvasMouseWheel;
        }

        public override void ControlUnload()
        {
        }

        private void HandleCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            tempPanning = false;
        }

        public override void DisplayApproval(bool visible) {
            Dispatcher.Invoke((Action)delegate {
                txtRcNotify.Visibility = (visible ? Visibility.Visible : Visibility.Collapsed);
            });
        }

        public override void DisplayControl(bool controlEnabled) {
            if (rc.state.ControlEnabled)
                rcBorderBG.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 20, 20, 20));
            else
                rcBorderBG.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.MidnightBlue);

            txtRcControlOff1.Visibility = txtRcControlOff2.Visibility = (rc.state.ControlEnabled ? Visibility.Hidden : Visibility.Visible);
        }

        public override void DisplayDebugKeyboard(string strKeyboard) {
            Dispatcher.Invoke((Action)delegate {
                txtDebugLeft.Text = strKeyboard;
            });
        }

        public override void DisplayDebugMouseEvent(int X, int Y) {
            string strMousePos = string.Format("X: {0}, Y: {1}", X, Y);
            Dispatcher.Invoke((Action)delegate {
                txtDebugRight.Text = strMousePos;
            });
        }

        public override void DisplayKeyHook(bool enabled) {
            txtRcHookOn.Visibility = (enabled ? Visibility.Visible : Visibility.Hidden);
        }

        public override void ParentStateChange(bool visible) {
        }

        public override void Refresh() {
            if (rc == null || rc.state.UseMultiScreen)
                return;

            /*
            Dispatcher.Invoke((Action)delegate {
                //Canvas.SetLeft(state.legacyScreen.CanvasImage, 0);
                //Canvas.SetTop(state.legacyScreen.CanvasImage, 0);

                legacyCanvas.Width = legacyViewbox.ActualWidth;
                legacyCanvas.Height = legacyViewbox.ActualHeight;
            });
            */
        }

        public override void SetCanvas(int virtualX, int virtualY, int virtualWidth, int virtualHeight) { //More like lowX, lowY, highX, highY
            if (rcScrollViewer.ActualHeight == 0 || rcScrollViewer.ViewportHeight == 0)
                return;

            //float currentAspectRatio = (float)rcScrollViewer.ViewportWidth / (float)rcScrollViewer.ViewportHeight;
            float currentAspectRatio = (float)rcScrollViewer.ActualWidth / (float)rcScrollViewer.ActualHeight;
            float targetAspectRatio = (float)virtualWidth / (float)virtualHeight;
            int width = virtualWidth;
            int height = virtualHeight;

            if (currentAspectRatio > targetAspectRatio) {
                //Pillarbox
                width = (int)((float)height * currentAspectRatio);
                //vpX = (width - state.virtualViewWant.Width) / 2;
            } else {
                //Letterbox
                height = (int)((float)width / currentAspectRatio);
                //vpY = (height - state.virtualViewWant.Height) / 2;
            }

            //double scaleX = (double)rcScrollViewer.ViewportWidth / (double)width;
            //double scaleY = (double)rcScrollViewer.ViewportHeight / (double)height;
            double scaleX = (double)rcScrollViewer.ActualWidth / (double)width;
            double scaleY = (double)rcScrollViewer.ActualHeight / (double)height;

            Dispatcher.Invoke((Action)delegate {
                if (!Double.IsInfinity(scaleY)) {
                    ZoomSlider.Value = scaleY;

                    rcScrollViewer.ScrollToHorizontalOffset((virtualX - canvasOffsetX) * ZoomSlider.Value);
                    rcScrollViewer.ScrollToVerticalOffset((virtualY - canvasOffsetY) * ZoomSlider.Value);
                }
            });
        }
        public override bool SwitchToLegacy() {
            rc.state.UseMultiScreen = true;

            return false;
        }

        public override bool SwitchToMultiScreen() {
            rc.state.UseMultiScreen = true;

            //dockCanvas.Visibility = Visibility.Visible;
            //legacyBorderBG.Visibility = Visibility.Hidden;

            return true;
        }

        public override void UpdateScreenLayout(int lowestX, int lowestY, int highestX, int highestY) {
            canvasOffsetX = lowestX;
            canvasOffsetY = lowestY;

            Dispatcher.Invoke((Action)delegate {
                rcCanvas.Children.Clear();

                rcCanvas.Width = Math.Abs(lowestX) + highestX;
                rcCanvas.Height = Math.Abs(lowestY) + highestY;

                foreach (RCScreen screen in rc.state.ListScreen) {
                    screen.CanvasImage = new System.Windows.Controls.Image {
                        Height = screen.rect.Height,
                        Width = screen.rect.Width
                    };

                    rcCanvas.Children.Add(screen.CanvasImage);
                    Canvas.SetLeft(screen.CanvasImage, screen.rect.Left - canvasOffsetX); //X
                    Canvas.SetRight(screen.CanvasImage, screen.rect.Right - canvasOffsetX);
                    Canvas.SetTop(screen.CanvasImage, screen.rect.Top - canvasOffsetY); //Y
                    Canvas.SetBottom(screen.CanvasImage, screen.rect.Bottom - canvasOffsetY);

                    screen.SetCanvasFilled();
                }

                //ZoomSlider.Minimum = rcScrollViewer.ScrollableHeight / virtualHeight;

                //--

                rc.state.legacyScreen.CanvasImage = new System.Windows.Controls.Image();
                rc.state.legacyScreen.SetCanvasFilled();

                //legacyCanvas.Children.Clear();
                //legacyCanvas.Children.Add(state.legacyScreen.CanvasImage);
            });

            if (rc.state.UseMultiScreen) {
                if (!App.Settings.StartControlEnabled)
                    CameraToOverview();
                else
                    CameraToCurrentScreen();
            }
        }

        private void HandleCanvasMouseDown(object sender, MouseButtonEventArgs e) {
            if (rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            System.Windows.Point point = e.GetPosition(rcCanvas);
            if (rc.state.UseMultiScreen) {
                if (!rc.state.ControlEnabled && e.ChangedButton == MouseButton.Right)
                {
                    if (rc.state.UseMultiScreenPanZoom)
                    {
                        tempPanningPoint = Mouse.GetPosition(this);
                        tempPanning = true;
                    }
                    return;
                }

                RCScreen screenPointingTo = rc.state.GetScreenUsingMouse(canvasOffsetX + (int)point.X, canvasOffsetY + (int)point.Y);
                if (screenPointingTo == null)
                    return;

                if (rc.state.ControlEnabled) {
                    if (!rc.state.UseMultiScreenPanZoom && screenPointingTo != rc.state.CurrentScreen) {
                        ConnectionManager.Viewer.FromGlChangeScreen(screenPointingTo, true);
                        return;
                    }
                } else {
                    if (e.ClickCount == 2) {
                        ConnectionManager.Viewer.SetControlEnabled(rc.state, true);
                    } else if (e.ChangedButton == MouseButton.Left) {
                        if (!rc.state.UseMultiScreenPanZoom) {
                            if (rc.state.CurrentScreen != screenPointingTo) //Multi-Screen (Focused), Control Disabled, Change Screen
                                ConnectionManager.Viewer.FromGlChangeScreen(screenPointingTo, false);
                            //Else
                            //We already changed the active screen by moving the mouse
                            CameraToCurrentScreen();
                        }
                    }
                    return;
                }
            } else {
                //Use legacy behavior

                if (!rc.state.ControlEnabled) {
                    if (e.ClickCount == 2)
                        ConnectionManager.Viewer.SetControlEnabled(rc.state, true);

                    return;
                }
            }

            if (e.ChangedButton == MouseButton.Middle) {
                if (e.ClickCount == 1) //Logitech bug
                    ConnectionManager.Viewer.PerformAutotype(false);
            } else {
                if (rc.state.windowActivatedMouseMove)
                    HandleCanvasMouseMove(sender, e);

                rc.SendMouseDown(e.ChangedButton);

                if (e.ChangedButton == MouseButton.Left)
                    rc.state.mouseHeldLeft = true;
                if (e.ChangedButton == MouseButton.Right)
                    rc.state.mouseHeldRight = true;

                ConnectionManager.Viewer.DebugKeyboard();
            }
        }

        private void HandleCanvasMouseMove(object sender, MouseEventArgs e) {
            if (rc == null || rc.state.CurrentScreen == null || rc.state.connectionStatus != ConnectionStatus.Connected || !rc.state.WindowIsActive())
                return;

            rc.state.windowActivatedMouseMove = false;
            System.Windows.Point point = e.GetPosition(rcCanvas);

            if (rc.state.UseMultiScreen) {
                if (!rc.state.ControlEnabled)
                {
                    if (tempPanning)
                    {
                        Point pointMouse = Mouse.GetPosition(this);
                        Point diff = (Point)(tempPanningPoint - pointMouse);
                        tempPanningPoint = pointMouse;
                        if(diff.X != 0)
                            rcScrollViewer.ScrollToHorizontalOffset(rcScrollViewer.HorizontalOffset + diff.X);
                        if (diff.Y != 0)
                            rcScrollViewer.ScrollToVerticalOffset(rcScrollViewer.VerticalOffset + diff.Y);
                        return;
                    }
                }

                point.X += canvasOffsetX;
                point.Y += canvasOffsetY;

                RCScreen screenPointingTo = rc.state.GetScreenUsingMouse((int)point.X, (int)point.Y);
                if (screenPointingTo == null)
                    return;

                /*
                if (screenPointingTo.MouseScale > 1.0)
                {
                    //Not great, but progress for Macs?
                    point.X -= screenPointingTo.screen_x;

                    point.X = point.X * screenPointingTo.MouseScale;
                    point.Y = point.Y * screenPointingTo.MouseScale;

                    point.X += screenPointingTo.screen_x;
                }
                */
                ConnectionManager.Viewer.DebugMouseEvent((int)point.X, (int)point.Y);

                //Console.WriteLine(string.Format("{0},{1} of {2},{3}", point.X, point.Y, screenPointingTo.rectFixed.X, screenPointingTo.rectFixed.Y));
                //Console.WriteLine(state.CurrentScreen.screen_id + " != " + screenPointingTo.screen_id);

                if ((rc.state.UseMultiScreenOverview || rc.state.UseMultiScreenPanZoom) && rc.state.CurrentScreen.screen_id != screenPointingTo.screen_id) {
                    //We are in overview, change which screen gets texture updates
                    ConnectionManager.Viewer.FromGlChangeScreen(screenPointingTo, false);
                }

                if (!rc.state.ControlEnabled)
                    return;

                rc.SendMousePosition((int)point.X, (int)point.Y);
            } else {
                //Legacy behavior
                if (!rc.state.ControlEnabled)
                    return;

                //throw new NotImplementedException();
                /*
                System.Drawing.Point legacyPoint = new System.Drawing.Point(e.X - vpX, e.Y - vpY);
                if (legacyPoint.X < 0 || legacyPoint.Y < 0)
                    if (legacyPoint.X < 0 || legacyPoint.Y < 0)
                        return;

                if (vpX > 0) {
                    legacyPoint.X = (int)(legacyPoint.X / scaleY);
                    legacyPoint.Y = (int)(legacyPoint.Y / scaleY);
                } else {
                    legacyPoint.X = (int)(legacyPoint.X / scaleX);
                    legacyPoint.Y = (int)(legacyPoint.Y / scaleX);
                }

                if (legacyPoint.X > legacyVirtualWidth || legacyPoint.Y > legacyVirtualHeight)
                    return;

                legacyPoint.X = legacyPoint.X + currentScreen.rect.X;
                legacyPoint.Y = legacyPoint.Y + currentScreen.rect.Y;

                DebugMouseEvent(legacyPoint.X, legacyPoint.Y);

                rc.SendMousePosition(legacyPoint.X, legacyPoint.Y);
                */
            }

            /*
            if (!controlEnabled || connectionStatus != ConnectionStatus.Connected)
                return;

            System.Windows.Point point = e.GetPosition((Canvas)sender);
            DebugMouseEvent((int)point.X, (int)point.Y);

            rc.SendMousePosition(canvasOffsetX + (int)point.X, canvasOffsetY + (int)point.Y);
            */
        }

        private void HandleCanvasMouseUp(object sender, MouseButtonEventArgs e) {
            if (rc == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            if(!rc.state.ControlEnabled)
            {
                if (e.ChangedButton == MouseButton.Right)
                    tempPanning = false;
                return;
            }

            //if (rcCanvas.Contains(e.GetPosition((Canvas)sender))) {
            rc.SendMouseUp(e.ChangedButton);

            if (e.ChangedButton == MouseButton.Left)
                rc.state.mouseHeldLeft = false;
            if (e.ChangedButton == MouseButton.Right)
                rc.state.mouseHeldRight = false;

            ConnectionManager.Viewer.DebugKeyboard();
            //}
        }

        private void HandleCanvasMouseWheel(object sender, MouseWheelEventArgs e) {
            if (rc == null || rc.state.connectionStatus != ConnectionStatus.Connected)
                return;

            if (rc.state.ControlEnabled)
                rc.SendMouseWheel(e.Delta);
            else if (rc.state.UseMultiScreenPanZoom) {
                if (e.Delta > 0) {
                    ZoomSlider.Value += 0.01;
                } else {
                    ZoomSlider.Value -= 0.01;
                }
            }
        }
        private void rcScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            e.Handled = true;

            HandleCanvasMouseWheel(sender, e);
        }

        private void rcScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) {
            if(rc != null && rc.state.UseMultiScreen && !rc.state.UseMultiScreenPanZoom) {
                if(rc.state.UseMultiScreenOverview)
                    CameraToOverview();
                else
                    CameraToCurrentScreen();
            }
        }

        public override void ResetCamera()
        {
        }
    }
}
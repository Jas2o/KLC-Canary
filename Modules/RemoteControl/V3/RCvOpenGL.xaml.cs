using NTR;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;

namespace KLC_Finch {
    /// <summary>
    /// Interaction logic for RCvOpenGL.xaml
    /// </summary>
    public partial class RCvOpenGL : RCv {

        private const int overlayHeight = 100;
        private const int overlayWidth = 400;
        private static Font arial = new Font("Arial", 32);
        private readonly int[] m_shader_sampler = new int[3];
        private readonly Camera MainCamera;
        private int fragment_shader_object = 0;
        private string glVersion;
        private int m_shader_multiplyColor = 0;
        private Bitmap overlay2dControlOff;
        private Bitmap overlay2dDisconnected;
        private Bitmap overlay2dKeyboard;
        private Bitmap overlay2dMouse;
        private double scaleX, scaleY;
        private int shader_program = 0;
        private int textureOverlay2dControlOff;
        private int textureOverlay2dDisconnected;
        private int textureOverlay2dKeyboard;
        private int textureOverlay2dMouse;
        private byte[] textureOverlayDataControlOff;
        private byte[] textureOverlayDataDisconnected;
        //private byte[] textureOverlayDataKeyboard;
        //private byte[] textureOverlayDataMouse;
        private int VBOmouse, VBOkeyboard, VBOtop, VBOcenter;
        private int VBOScreen;
        private Vector2[] vertBufferMouse, vertBufferKeyboard, vertBufferTop, vertBufferCenter;
        private Vector2[] vertBufferScreen;
        private int vertex_shader_object = 0;
        private int vpX, vpY;

        private bool tempPanning;
        private System.Drawing.Point tempPanningPoint;
        
        public RCvOpenGL() : base() {
            InitializeComponent();
            txtRcNotify.Visibility = Visibility.Collapsed;

            MainCamera = new Camera(Vector2.Zero); //for Multi-screen
            glControl.APIVersion = new Version(3, 2, 0, 0);
            glControl.Profile = OpenTK.Windowing.Common.ContextProfile.Compatability;
            glControl.Load += glControl_Load;
        }

        public bool powerSaving { get; protected set; }
        public override bool SupportsLegacy { get { return true; } }
        public override void CameraFromClickedScreen(RCScreen screen, bool moveCamera = true) {
            if (rc.state.UseMultiScreen && moveCamera)
                CameraToCurrentScreen();
        }

        public override void CameraToCurrentScreen() {
            if (rc == null || !rc.state.UseMultiScreen || rc.state.CurrentScreen == null)
                return;

            rc.state.UseMultiScreenOverview = false;
            rc.state.UseMultiScreenPanZoom = false;

            ResetCamera();
            //DebugKeyboard();

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

                rc.state.SetVirtual(rc.state.CurrentScreen.rect.X - (adjustLeft ? 80 : 0),
                    rc.state.CurrentScreen.rect.Y - (adjustUp ? 80 : 0),
                    rc.state.CurrentScreen.rect.Width + (adjustLeft ? 80 : 0) + (adjustRight ? 80 : 0),
                    rc.state.CurrentScreen.rect.Height + (adjustUp ? 80 : 0) + (adjustDown ? 80 : 0));
            } else
                rc.state.SetVirtual(rc.state.CurrentScreen.rect.X, rc.state.CurrentScreen.rect.Y, rc.state.CurrentScreen.rect.Width, rc.state.CurrentScreen.rect.Height);
            
            glControl.Invalidate();
        }

        public override void CameraToOverview() {
            if (!rc.state.UseMultiScreen)
                return;

            rc.state.UseMultiScreenOverview = true;
            rc.state.UseMultiScreenPanZoom = false;

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

            SetCanvas(lowestX, lowestY, highestX, highestY);

            //--

            ResetCamera();
            //DebugKeyboard();

            rc.state.virtualViewWant = rc.state.virtualCanvas;
            rc.state.virtualRequireViewportUpdate = true;
        }

        public override void CheckHealth() {
            if (rc == null)
                return;

            if (rc.state.connectionStatus == ConnectionStatus.Disconnected)
                glControl.Invalidate();
        }

        public override void ControlLoaded() {
            //For OpenGL, this has aleady happened.
            //OpenGLWPF and Canvas still use this.

            /*
            if (App.Settings.RendererAlt) {
                rc.DecodeMode = DecodeMode.BitmapRGB;
                //rc.state.Window.Title = rc.state.BaseTitle + " (RGB)";
            }
            else {
                rc.DecodeMode = DecodeMode.RawYUV;
                //rc.state.Window.Title = rc.state.BaseTitle + " (YUV)";
            }

            glVersion = GL.GetString(StringName.Version);

            CreateShaders(Shaders.yuvtorgb_vertex, Shaders.yuvtorgb_fragment, out vertex_shader_object, out fragment_shader_object, out shader_program);
            m_shader_sampler[0] = GL.GetUniformLocation(shader_program, "y_sampler");
            m_shader_sampler[1] = GL.GetUniformLocation(shader_program, "u_sampler");
            m_shader_sampler[2] = GL.GetUniformLocation(shader_program, "v_sampler");
            m_shader_multiplyColor = GL.GetUniformLocation(shader_program, "multiplyColor");
            */
        }

        public override void ControlUnload() {
            if (shader_program != 0)
                GL.DeleteProgram(shader_program);
            if (fragment_shader_object != 0)
                GL.DeleteShader(fragment_shader_object);
            if (vertex_shader_object != 0)
                GL.DeleteShader(vertex_shader_object);
        }

        public override void DisplayApproval(bool visible) {
            Dispatcher.Invoke((Action)delegate {
                txtRcNotify.Visibility = (visible ? Visibility.Visible : Visibility.Collapsed);
            });
        }

        public override void DisplayControl(bool enabled) {

        }

        public override void DisplayDebugKeyboard(string strKeyboard) {
        }

        public override void DisplayDebugMouseEvent(int X, int Y) {
        }

        public override void DisplayKeyHook(bool enabled) {
            //txtRcHookOn.Visibility = (enabled ? Visibility.Visible : Visibility.Hidden);
        }

        public override void MoveDown() {
            MainCamera.Move(new Vector2(0f, rc.state.virtualViewWant.Height / 100));
            glControl.Invalidate();
        }

        public override void MoveLeft() {
            
            MainCamera.Move(new Vector2(-(rc.state.virtualViewWant.Width / 100), 0f));
            glControl.Invalidate();
        }

        public override void MoveRight() {
            MainCamera.Move(new Vector2(rc.state.virtualViewWant.Width / 100, 0f));
            glControl.Invalidate();
        }

        public override void MoveUp() {
            MainCamera.Move(new Vector2(0f, -(rc.state.virtualViewWant.Height / 100)));
            glControl.Invalidate();
        }

        public override void ParentStateChange(bool visible) {
            if (visible && powerSaving) {
                powerSaving = false;
                //glControl.Render += GlControl_Render;
            } else {
                powerSaving = true;
                //glControl.Render -= GlControl_Render;
            }
        }

        public override void Refresh() {
            glControl.Invalidate();
        }
        public override void SetCanvas(int virtualX, int virtualY, int virtualWidth, int virtualHeight) { //More like lowX, lowY, highX, highY
            if (rc == null)
                return;

            if (rc.state.UseMultiScreen) {
                rc.state.virtualCanvas = new Rectangle(virtualX, virtualY, Math.Abs(virtualX) + virtualWidth, Math.Abs(virtualY) + virtualHeight);
                rc.state.SetVirtual(virtualX, virtualY, rc.state.virtualCanvas.Width, rc.state.virtualCanvas.Height);
            } else {
                rc.state.virtualCanvas = new Rectangle(0, 0, virtualWidth, virtualHeight);
                rc.state.SetVirtual(0, 0, virtualWidth, virtualHeight);
            }

            rc.state.virtualRequireViewportUpdate = true;
        }

        public override bool SwitchToLegacy() {
            rc.state.UseMultiScreen = false;
            rc.state.virtualRequireViewportUpdate = true;

            return true;
        }

        public override bool SwitchToMultiScreen() {
            rc.state.UseMultiScreen = true;
            rc.state.virtualRequireViewportUpdate = true;

            return true;
        }

        public override void UpdateScreenLayout(int lowestX, int lowestY, int highestX, int highestY) {
            //Empty
        }

        public override void TogglePanZoom() {
            if (rc == null)
                return;

            rc.state.UseMultiScreenPanZoom = !rc.state.UseMultiScreenPanZoom;
        }

        /*
        public override void ZoomIn() {
            rc.state.UseMultiScreenPanZoom = true;

            rc.state.ZoomIn();
            glControl.Invalidate();
        }

        public override void ZoomOut() {
            rc.state.UseMultiScreenPanZoom = true;

            rc.state.ZoomOut();
            glControl.Invalidate();
        }
        */

        private void CreateShaders(string vs, string fs, out int vertexObject, out int fragmentObject, out int program) {
            vertexObject = GL.CreateShader(ShaderType.VertexShader);
            fragmentObject = GL.CreateShader(ShaderType.FragmentShader);

            // Compile vertex shader
            GL.ShaderSource(vertexObject, vs);
            GL.CompileShader(vertexObject);
            GL.GetShaderInfoLog(vertexObject, out string info);
            GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out int status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // Compile vertex shader
            GL.ShaderSource(fragmentObject, fs);
            GL.CompileShader(fragmentObject);
            GL.GetShaderInfoLog(fragmentObject, out info);
            GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            program = GL.CreateProgram();
            GL.AttachShader(program, fragmentObject);
            GL.AttachShader(program, vertexObject);

            GL.LinkProgram(program);
            GL.UseProgram(program);
        }

        private void glControl_Load(object sender, EventArgs e) {
            //if (rc == null)
                //return;

            glVersion = GL.GetString(StringName.Version);

            CreateShaders(Shaders.yuvtorgb_vertex, Shaders.yuvtorgb_fragment, out vertex_shader_object, out fragment_shader_object, out shader_program);
            m_shader_sampler[0] = GL.GetUniformLocation(shader_program, "y_sampler");
            m_shader_sampler[1] = GL.GetUniformLocation(shader_program, "u_sampler");
            m_shader_sampler[2] = GL.GetUniformLocation(shader_program, "v_sampler");
            m_shader_multiplyColor = GL.GetUniformLocation(shader_program, "multiplyColor");

            InitTextures();
            //RefreshVirtual();

            glControl.Paint += glControl_Paint;
            glControl.Resize += glControl_Resize;
            glControl.MouseMove += HandleMouseMove;
            glControl.MouseDown += HandleMouseDown;
            glControl.MouseUp += HandleMouseUp;
            glControl.MouseWheel += HandleMouseWheel;
        }

        private void glControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e) {
            //This happens when initialized and when resized
            Render();
        }

        private void glControl_Resize(object sender, EventArgs e) {
            if (rc == null)
                return;

            RefreshVirtual();
            rc.state.virtualRequireViewportUpdate = true;
        }

        private void HandleMouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
            if (rc == null) //rc.state.connectionStatus != ConnectionStatus.Connected
                return;

            if (rc.state.UseMultiScreen) {
                if (!rc.state.ControlEnabled && e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    if (rc.state.UseMultiScreenPanZoom)
                    {
                        tempPanningPoint = e.Location;
                        tempPanning = true;
                    }
                    return;
                }

                Vector2 point = MainCamera.ScreenToWorldCoordinates(new Vector2((float)(e.X / scaleX), (float)(e.Y / scaleY)), rc.state.virtualViewNeed.X, rc.state.virtualViewNeed.Y);
                RCScreen screenPointingTo = rc.state.GetScreenUsingMouse((int)point.X, (int)point.Y);
                if (screenPointingTo == null)
                    return;

                if (rc.state.ControlEnabled) {
                    if (!rc.state.UseMultiScreenPanZoom && screenPointingTo != rc.state.CurrentScreen) {
                        ConnectionManager.Viewer.FromGlChangeScreen(screenPointingTo, true);
                        return;
                    }
                } else {
                    if (e.Clicks == 2) {
                        ConnectionManager.Viewer.SetControlEnabled(rc.state, true);
                    } else if (e.Button == System.Windows.Forms.MouseButtons.Left) {
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
                    if (e.Clicks == 2)
                        ConnectionManager.Viewer.SetControlEnabled(rc.state, true);

                    return;
                }
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Middle) {
                if (e.Clicks == 1) //Logitech bug
                    ConnectionManager.Viewer.PerformAutotype(false);
            } else {
                if (rc.state.windowActivatedMouseMove)
                    HandleMouseMove(sender, e);

                rc.SendMouseDown(e.Button);

                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    rc.state.mouseHeldLeft = true;
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    rc.state.mouseHeldRight = true;

                ConnectionManager.Viewer.DebugKeyboard();
            }
        }

        private void HandleMouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
            if (rc == null || rc.state.CurrentScreen == null || rc == null || !rc.state.WindowIsActive())
                return;

            rc.state.windowActivatedMouseMove = false;

            if (rc.state.UseMultiScreen) {
                if (!rc.state.ControlEnabled)
                {
                    if (tempPanning)
                    {
                        Vector2 diff = new Vector2(tempPanningPoint.X - e.Location.X, tempPanningPoint.Y - e.Location.Y);
                        tempPanningPoint = e.Location;
                        MainCamera.Move(diff);
                        glControl.Invalidate();
                        return;
                    }
                }

                Vector2 point = MainCamera.ScreenToWorldCoordinates(new Vector2((float)(e.X / scaleX), (float)(e.Y / scaleY)), rc.state.virtualViewNeed.X, rc.state.virtualViewNeed.Y);

                RCScreen screenPointingTo = rc.state.GetScreenUsingMouse((int)point.X, (int)point.Y);
                if (screenPointingTo == null)
                    return;

                /*
                if (screenPointingTo.MouseScale > 1.0)
                {
                    //Not great, but progress for Macs?
                    point.X -= screenPointingTo.screen_x;

                    point.X = (float)(point.X * screenPointingTo.MouseScale);
                    point.Y = (float)(point.Y * screenPointingTo.MouseScale);

                    point.X += screenPointingTo.screen_x;
                }
                */
                ConnectionManager.Viewer.DebugMouseEvent((int)point.X, (int)point.Y);

                if ((rc.state.UseMultiScreenOverview || rc.state.UseMultiScreenPanZoom) && rc.state.CurrentScreen.screen_id != screenPointingTo.screen_id) {
                    //We are in overview, change which screen gets texture updates
                    ConnectionManager.Viewer.FromGlChangeScreen(screenPointingTo, false);
                    glControl.Invalidate();
                }

                if (!rc.state.ControlEnabled)
                    return;

                if (screenPointingTo == rc.state.CurrentScreen)
                {
                    //Macs have an interesting quirk, they base mouse movement off the current screen.
                    //This is how the resolution can overlap the screen positions and still "work"
                    //(despite the whole busted 2nd screen thing)

                    rc.SendMousePosition((int)point.X, (int)point.Y);
                }
            } else {
                //Legacy behavior
                if (!rc.state.ControlEnabled)
                    return;

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

                if (legacyPoint.X > rc.state.legacyVirtualWidth || legacyPoint.Y > rc.state.legacyVirtualHeight)
                    return;

                legacyPoint.X += rc.state.CurrentScreen.rect.X;
                legacyPoint.Y += rc.state.CurrentScreen.rect.Y;

                ConnectionManager.Viewer.DebugMouseEvent(legacyPoint.X, legacyPoint.Y);

                if(rc.state.connectionStatus == ConnectionStatus.Connected)
                    rc.SendMousePosition(legacyPoint.X, legacyPoint.Y);
            }
        }

        private void HandleMouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
            if (rc == null)
                return;

            if (!rc.state.ControlEnabled)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    tempPanning = false;
                return;
            }

            if (glControl.ClientRectangle.Contains(e.Location)) {
                if (rc.state.connectionStatus != ConnectionStatus.Connected)
                    return;

                rc.SendMouseUp(e.Button);

                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    rc.state.mouseHeldLeft = false;
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    rc.state.mouseHeldRight = false;

                ConnectionManager.Viewer.DebugKeyboard();
            }
        }

        private void HandleMouseWheel(object sender, System.Windows.Forms.MouseEventArgs e) {
            if (rc == null)
                return;

            if (rc.state.ControlEnabled)
            {
                if(rc.state.connectionStatus == ConnectionStatus.Connected)
                    rc.SendMouseWheel(e.Delta);
            }
            else if (rc.state.UseMultiScreenPanZoom)
            {
                //Console.WriteLine(e.Delta + " " + MainCamera.Scale);

                //Console.WriteLine(vpX + " x:y " + vpY + " // " + scaleX + " // " + MainCamera.Scale);

                if (e.Delta > 0)
                {
                    //MainCamera.Move(new Vector2(500f, 0f));
                    MainCamera.Scale = Vector2.Add(MainCamera.Scale, new Vector2(0.1f, 0.1f));
                    if (MainCamera.Scale.X > 4.0 || MainCamera.Scale.Y > 4.0)
                        MainCamera.Scale = new Vector2(4.0f, 4.0f);
                }
                else
                {
                    //MainCamera.Move(new Vector2(-(500f), 0f));
                    MainCamera.Scale = Vector2.Subtract(MainCamera.Scale, new Vector2(0.1f, 0.1f));
                    if (MainCamera.Scale.X < 0.1 || MainCamera.Scale.Y < 0.1)
                        MainCamera.Scale = new Vector2(0.1f, 0.1f);
                }
                glControl.Invalidate();
            }
        }

        private void InitOverlayTexture(ref Bitmap overlay2d, ref int textureOverlay2d, int overlayW = overlayWidth, int overlayH = overlayHeight) {
            overlay2d = new Bitmap(overlayW, overlayH);
            textureOverlay2d = GL.GenTexture();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureOverlay2d);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, overlay2d.Width, overlay2d.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero); // just allocate memory, so we can update efficiently using TexSubImage2D
        }

        private void InitTextures() {
            //FPS/Mouse/Keyboard
            InitOverlayTexture(ref overlay2dMouse, ref textureOverlay2dMouse);
            InitOverlayTexture(ref overlay2dKeyboard, ref textureOverlay2dKeyboard);
            InitOverlayTexture(ref overlay2dDisconnected, ref textureOverlay2dDisconnected, 400);
            InitOverlayTexture(ref overlay2dControlOff, ref textureOverlay2dControlOff, 400);

            #region Texture - Disconnected

            using (Graphics gfx = Graphics.FromImage(overlay2dDisconnected)) {
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                //gfx.Clear(System.Drawing.Color.Transparent);
                gfx.Clear(System.Drawing.Color.FromArgb(128, 0, 0, 0));

                using (GraphicsPath gp = new GraphicsPath())
                using (System.Drawing.Pen outline = new System.Drawing.Pen(System.Drawing.Color.Black, 3) { LineJoin = LineJoin.Round }) //outline width=1
                using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (System.Drawing.Brush foreBrush = new SolidBrush(System.Drawing.Color.Lime)) {

                    

                    gp.AddString("Disconnected", arial.FontFamily, (int)arial.Style, arial.Size, gfx.VisibleClipBounds, sf);
                    gfx.DrawPath(outline, gp);
                    gfx.FillPath(foreBrush, gp);
                }

                BitmapData data2 = overlay2dDisconnected.LockBits(new System.Drawing.Rectangle(0, 0, overlay2dDisconnected.Width, overlay2dDisconnected.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                if (textureOverlayDataDisconnected == null)
                    textureOverlayDataDisconnected = new byte[Math.Abs(data2.Stride * data2.Height)];
                Marshal.Copy(data2.Scan0, textureOverlayDataDisconnected, 0, textureOverlayDataDisconnected.Length);

                overlay2dDisconnected.UnlockBits(data2);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dDisconnected);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0, //Level
                PixelInternalFormat.Rgba,
                overlay2dDisconnected.Width,
                overlay2dDisconnected.Height,
                0, //Border
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte,
                textureOverlayDataDisconnected);

            #endregion Texture - Disconnected

            #region Texture - Control Off

            using (Graphics gfx = Graphics.FromImage(overlay2dControlOff)) {
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.Clear(System.Drawing.Color.Transparent);

                using (GraphicsPath gp = new GraphicsPath())
                using (System.Drawing.Pen outline = new System.Drawing.Pen(Color.FromArgb(128, Color.Black), 3) { LineJoin = LineJoin.Round }) //outline width=1
                using (StringFormat sf = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (System.Drawing.Brush foreBrush = new SolidBrush(Color.FromArgb(128, Color.White))) {
                    gp.AddString("F2 or double-click to enable control", arial.FontFamily, (int)arial.Style, arial.Size, gfx.VisibleClipBounds, sf);
                    gfx.DrawPath(outline, gp);
                    gfx.FillPath(foreBrush, gp);
                }

                BitmapData data2 = overlay2dControlOff.LockBits(new System.Drawing.Rectangle(0, 0, overlay2dControlOff.Width, overlay2dControlOff.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                if (textureOverlayDataControlOff == null)
                    textureOverlayDataControlOff = new byte[Math.Abs(data2.Stride * data2.Height)];
                Marshal.Copy(data2.Scan0, textureOverlayDataControlOff, 0, textureOverlayDataControlOff.Length);

                overlay2dControlOff.UnlockBits(data2);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dControlOff);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0, //Level
                PixelInternalFormat.Rgba,
                overlay2dControlOff.Width,
                overlay2dControlOff.Height,
                0, //Border
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte,
                textureOverlayDataControlOff);

            #endregion Texture - Control Off

            //rc.state.textureLegacy = new TextureScreen(rc.DecodeMode);
            //InitLegacyScreenTexture();

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.Enable(EnableCap.Texture2D);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        private void RefreshVirtual() {
            if (rc == null)
                return;

            vertBufferScreen = new Vector2[8] {
                new Vector2(rc.state.virtualCanvas.Left, rc.state.virtualCanvas.Top), new Vector2(0, 1),
                new Vector2(rc.state.virtualCanvas.Right, rc.state.virtualCanvas.Top), new Vector2(1, 1),
                new Vector2(rc.state.virtualCanvas.Right, rc.state.virtualCanvas.Bottom), new Vector2(1, 0),
                new Vector2(rc.state.virtualCanvas.Left, rc.state.virtualCanvas.Bottom), new Vector2(0, 0)
            };

            VBOScreen = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOScreen);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(Vector2.SizeInBytes * vertBufferScreen.Length), vertBufferScreen, BufferUsageHint.StaticDraw);

            vertBufferMouse = new Vector2[8] {
                new Vector2(glControl.Width - overlayWidth,overlayHeight), new Vector2(0, 1),
                new Vector2(glControl.Width,overlayHeight), new Vector2(1, 1),
                new Vector2(glControl.Width,0), new Vector2(1, 0),
                new Vector2(glControl.Width - overlayWidth,0), new Vector2(0, 0)
            };

            VBOmouse = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOmouse);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(Vector2.SizeInBytes * vertBufferMouse.Length), vertBufferMouse, BufferUsageHint.StaticDraw);

            vertBufferKeyboard = new Vector2[8] {
                new Vector2(0,overlayHeight), new Vector2(0, 1),
                new Vector2(overlayWidth,overlayHeight), new Vector2(1, 1),
                new Vector2(overlayWidth,0), new Vector2(1, 0),
                new Vector2(0,0), new Vector2(0, 0)
            };

            VBOkeyboard = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOkeyboard);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(Vector2.SizeInBytes * vertBufferKeyboard.Length), vertBufferKeyboard, BufferUsageHint.StaticDraw);

            int leftCenter = (glControl.Width - 400) / 2;
            int topCenter = (glControl.Height - overlayHeight) / 2;
            vertBufferCenter = new Vector2[8] {
                new Vector2(leftCenter, topCenter + overlayHeight), new Vector2(0, 1),
                new Vector2(leftCenter + 400, topCenter + overlayHeight), new Vector2(1, 1),
                new Vector2(leftCenter + 400, topCenter), new Vector2(1, 0),
                new Vector2(leftCenter, topCenter), new Vector2(0, 0)
            };

            VBOcenter = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOcenter);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(Vector2.SizeInBytes * vertBufferCenter.Length), vertBufferCenter, BufferUsageHint.StaticDraw);

            vertBufferTop = new Vector2[8] {
                new Vector2(leftCenter, overlayHeight), new Vector2(0, 1),
                new Vector2(leftCenter + 400, overlayHeight), new Vector2(1, 1),
                new Vector2(leftCenter + 400, 0), new Vector2(1, 0),
                new Vector2(leftCenter, 0), new Vector2(0, 0)
            };

            VBOtop = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOtop);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(Vector2.SizeInBytes * vertBufferTop.Length), vertBufferTop, BufferUsageHint.StaticDraw);
        }

        private Color ConnectionStatusToColor()
        {
            switch (rc.state.connectionStatus)
            {
                case ConnectionStatus.FirstConnectionAttempt:
                    return Color.SlateGray;

                case ConnectionStatus.Connected:
                    if (rc.state.ControlEnabled)
                        return Color.FromArgb(255, 20, 20, 20);
                    else
                        return Color.MidnightBlue;

                case ConnectionStatus.Disconnected:
                    return Color.Maroon;
            }

            return Color.BlueViolet;
        }

        private void Render() {
            glControl.MakeCurrent();
#if DEBUG
            ErrorCode errorCode = GL.GetError();
#endif

            if (rc == null)
            {
                GL.ClearColor(System.Drawing.Color.SlateGray);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                glControl.SwapBuffers();
                return;
            } else if (vertBufferScreen == null)
            {
                RefreshVirtual();
            }

            if (rc.state.textureLegacy == null)
                rc.state.textureLegacy = new TextureScreen();//rc.DecodeMode

            if (rc.state.UseMultiScreen)
                RenderStartMulti();
            else
                RenderStartLegacy();

            //GL.UseProgram(0); //No shader for RGB

            // Setup new textures, not actually render
            //lock (lockFrameBuf) {
            foreach (RCScreen screen in rc.state.ListScreen) {
                if (screen.Texture == null)
                    screen.Texture = new TextureScreen();//rc.DecodeMode
                else
                    screen.Texture.RenderNew(m_shader_sampler);
            }
            if (rc.state.UseMultiScreen) {
                if (rc.state.textureCursor == null) {
                    rc.state.textureCursor = new TextureCursor();
                } else
                    rc.state.textureCursor.RenderNew();
            }
            if (!rc.state.UseMultiScreen) {
                if (rc.state.textureLegacy != null)
                    rc.state.textureLegacy.RenderNew(m_shader_sampler);
            }
            //}

            Color bgColor = ConnectionStatusToColor();
            GL.ClearColor(bgColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (rc.state.UseMultiScreen) {
                if (!rc.state.UseMultiScreenPanZoom)
                {
                    GL.Disable(EnableCap.Texture2D);
                    GL.Begin(PrimitiveType.Polygon);
                    GL.Color3(bgColor);
                    //GL.Vertex2(rc.state.virtualViewNeed.Left - 50, rc.state.virtualViewNeed.Top - 50);
                    //GL.Vertex2(rc.state.virtualViewNeed.Right + 50, rc.state.virtualViewNeed.Top - 50);
                    GL.Vertex2(rc.state.virtualViewNeed.Left - 50, rc.state.virtualViewNeed.Bottom - (rc.state.virtualViewNeed.Height /2) - 50);
                    GL.Vertex2(rc.state.virtualViewNeed.Right + 50, rc.state.virtualViewNeed.Bottom - (rc.state.virtualViewNeed.Height / 2) - 50);
                    GL.Color3(Color.Black);
                    GL.Vertex2(rc.state.virtualViewNeed.Right + 50, rc.state.virtualViewNeed.Bottom + 50);
                    GL.Vertex2(rc.state.virtualViewNeed.Left - 50, rc.state.virtualViewNeed.Bottom + 50);
                    GL.End();
                }

                foreach (RCScreen screen in rc.state.ListScreen) {
                    if (screen.Texture != null) {
                        Color multiplyColor;
                        if (screen == rc.state.CurrentScreen)
                            multiplyColor = Color.White;
                        else if (rc.state.ControlEnabled || rc.state.UseMultiScreenOverview || rc.state.UseMultiScreenPanZoom)
                            multiplyColor = Color.Gray;
                        else
                            multiplyColor = Color.Cyan;

                        GL.Disable(EnableCap.Texture2D);
                        //GL.UseProgram(0);
                        GL.Color3(Color.DimGray);

                        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                        GL.Begin(PrimitiveType.Polygon);
                        //GL.PointSize(5f); //Line only
                        //GL.LineWidth(5f); //Line only

                        GL.Vertex2(screen.rect.Left, screen.rect.Bottom);
                        GL.Vertex2(screen.rect.Left, screen.rect.Top);
                        GL.Vertex2(screen.rect.Right, screen.rect.Top);
                        GL.Vertex2(screen.rect.Right, screen.rect.Bottom);

                        //GL.Vertex2(vertBufferScreen[0].X, vertBufferScreen[0].Y);

                        GL.End();

                        if (!screen.Texture.Render(shader_program, m_shader_sampler, m_shader_multiplyColor, multiplyColor)) {
                            GL.Disable(EnableCap.Texture2D);
                            GL.UseProgram(0);
                            GL.Color3(Color.DimGray);

                            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                            GL.Begin(PrimitiveType.Polygon);
                            //GL.PointSize(5f); //Line only
                            //GL.LineWidth(5f); //Line only

                            GL.Vertex2(screen.rect.Left, screen.rect.Bottom);
                            GL.Vertex2(screen.rect.Left, screen.rect.Top);
                            GL.Vertex2(screen.rect.Right, screen.rect.Top);
                            GL.Vertex2(screen.rect.Right, screen.rect.Bottom);

                            //GL.Vertex2(vertBufferScreen[0].X, vertBufferScreen[0].Y);

                            GL.End();
                        }
                    }
                }

                if (App.Settings.MultiShowCursor) {
                    if (rc.state.textureCursor != null) {
                        GL.Color3(Color.White);
                        rc.state.textureCursor.Render();
                    }

                    /*
                    if(rectCursor != null) {
                        GL.Disable(EnableCap.Texture2D);
                        GL.Color3(Color.Yellow);

                        //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                        GL.Begin(PrimitiveType.Polygon);
                        GL.PointSize(5f);
                        GL.LineWidth(5f);

                        GL.Vertex2(rectCursor.Left, rectCursor.Bottom);
                        GL.Vertex2(rectCursor.Left, rectCursor.Top);
                        GL.Vertex2(rectCursor.Right, rectCursor.Top);
                        GL.Vertex2(rectCursor.Right, rectCursor.Bottom);

                        //GL.Vertex2(vertBufferScreen[0].X, vertBufferScreen[0].Y);

                        GL.End();
                    }
                    */
                }
            } else {
                //Legacy behavior
                if (!rc.state.textureLegacy.Render(shader_program, m_shader_sampler, m_shader_multiplyColor, Color.White)) {
                    GL.Disable(EnableCap.Texture2D);
                    GL.UseProgram(0);
                    GL.Color3(Color.DimGray);

                    //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.Begin(PrimitiveType.Polygon);
                    GL.PointSize(5f);
                    GL.LineWidth(5f);

                    GL.Vertex2(0, rc.state.legacyVirtualHeight);
                    GL.Vertex2(0, 0);
                    GL.Vertex2(rc.state.legacyVirtualWidth, 0);
                    GL.Vertex2(rc.state.legacyVirtualWidth, rc.state.legacyVirtualHeight);

                    //GL.Vertex2(vertBufferScreen[0].X, vertBufferScreen[0].Y);

                    GL.End();
                }
            }

            //--

            GL.Enable(EnableCap.Texture2D);

            #region Overlay

            if (!rc.state.UseMultiScreen)
                GL.Viewport(0, 0, glControl.Width, glControl.Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, glControl.Width, glControl.Height, 0, MainCamera.ZNear, MainCamera.ZFar); //Test

            /*
            if (overlayNewMouse) {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dMouse);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0, //Level
                    PixelInternalFormat.Rgba,
                    overlay2dMouse.Width,
                    overlay2dMouse.Height,
                    0, //Border
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    textureOverlayDataMouse);

                overlayNewMouse = false;
            }

            if (overlayNewKeyboard) {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dKeyboard);

                GL.TexImage2D(
                    TextureTarget.Texture2D,
                    0, //Level
                    PixelInternalFormat.Rgba,
                    overlay2dKeyboard.Width,
                    overlay2dKeyboard.Height,
                    0, //Border
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    textureOverlayDataKeyboard);

                overlayNewKeyboard = false;
            }
            */

            GL.Color3(Color.White);
            GL.UseProgram(0);
            if (App.Settings.DisplayOverlayMouse) {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dMouse);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOmouse);
                GL.VertexPointer(2, VertexPointerType.Float, Vector2.SizeInBytes * 2, 0);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, Vector2.SizeInBytes * 2, Vector2.SizeInBytes);
                GL.DrawArrays(PrimitiveType.Quads, 0, vertBufferMouse.Length / 2);
            }

            if (App.Settings.DisplayOverlayKeyboardOther || App.Settings.DisplayOverlayKeyboardMod) {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dKeyboard);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOkeyboard);
                GL.VertexPointer(2, VertexPointerType.Float, Vector2.SizeInBytes * 2, 0);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, Vector2.SizeInBytes * 2, Vector2.SizeInBytes);
                GL.DrawArrays(PrimitiveType.Quads, 0, vertBufferKeyboard.Length / 2);
            }

            if (rc.state.connectionStatus == ConnectionStatus.Disconnected) {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dDisconnected);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOcenter);
                GL.VertexPointer(2, VertexPointerType.Float, Vector2.SizeInBytes * 2, 0);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, Vector2.SizeInBytes * 2, Vector2.SizeInBytes);
                GL.DrawArrays(PrimitiveType.Quads, 0, vertBufferCenter.Length / 2);
            } else if (rc.state.connectionStatus == ConnectionStatus.Connected && !rc.state.ControlEnabled) {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, textureOverlay2dControlOff);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOtop);
                GL.VertexPointer(2, VertexPointerType.Float, Vector2.SizeInBytes * 2, 0);
                GL.TexCoordPointer(2, TexCoordPointerType.Float, Vector2.SizeInBytes * 2, Vector2.SizeInBytes);
                GL.DrawArrays(PrimitiveType.Quads, 0, vertBufferTop.Length / 2);
            }

            #endregion Overlay

            //--

#if DEBUG
            errorCode = GL.GetError();
            if (errorCode != ErrorCode.NoError)
                Console.WriteLine("OpenGL error: " + errorCode.ToString());
#endif

            glControl.SwapBuffers();
        }

        private void RenderStartLegacy() {
            if (rc.state.virtualRequireViewportUpdate) {
                RefreshVirtual();
                rc.state.virtualRequireViewportUpdate = false;
            }

            int screenWidth = glControl.Width;
            int screenHeight = glControl.Height;

            float targetAspectRatio = (float)rc.state.legacyVirtualWidth / (float)rc.state.legacyVirtualHeight;

            int width = screenWidth;
            int height = (int)((float)width / targetAspectRatio/* + 0.5f*/);

            if (height > screenHeight) {
                //Pillarbox
                height = screenHeight;
                width = (int)((float)height * targetAspectRatio/* + 0.5f*/);
            }

            vpX = (screenWidth / 2) - (width / 2);
            vpY = (screenHeight / 2) - (height / 2);

            GL.Viewport(vpX, vpY, width, height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, rc.state.legacyVirtualWidth, rc.state.legacyVirtualHeight, 0, -1, 1);//Upsidedown
            //GL.Ortho(0, legacyVirtualWidth, 0, legacyVirtualHeight, -1, 1);

            GL.MatrixMode(MatrixMode.Modelview);
            //GL.PushMatrix();

            //Now to calculate the scale considering the screen size and virtual size
            scaleX = (double)screenWidth / (double)rc.state.legacyVirtualWidth;
            scaleY = (double)screenHeight / (double)rc.state.legacyVirtualHeight;
            GL.Scale(scaleX, scaleY, 1.0f);

            GL.LoadIdentity();

            GL.Disable(EnableCap.DepthTest);
        }

        private void RenderStartMulti() {
            if (rc.state.virtualRequireViewportUpdate) {
                float currentAspectRatio = (float)glControl.Width / (float)glControl.Height;
                float targetAspectRatio = (float)rc.state.virtualViewWant.Width / (float)rc.state.virtualViewWant.Height;
                int width = rc.state.virtualViewWant.Width;
                int height = rc.state.virtualViewWant.Height;
                vpX = 0;
                vpY = 0;

                if (currentAspectRatio > targetAspectRatio) {
                    //Pillarbox
                    width = (int)((float)height * currentAspectRatio);
                    vpX = (width - rc.state.virtualViewWant.Width) / 2;
                } else {
                    //Letterbox
                    height = (int)((float)width / currentAspectRatio);
                    vpY = (height - rc.state.virtualViewWant.Height) / 2;
                }

                scaleX = (double)glControl.Width / (double)width;
                scaleY = (double)glControl.Height / (double)height;

                rc.state.virtualViewNeed = new Rectangle(rc.state.virtualViewWant.X - vpX, rc.state.virtualViewWant.Y - vpY, width, height);

                rc.state.virtualRequireViewportUpdate = false;
            }

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(rc.state.virtualViewNeed.Left, rc.state.virtualViewNeed.Right, rc.state.virtualViewNeed.Bottom, rc.state.virtualViewNeed.Top, MainCamera.ZNear, MainCamera.ZFar);
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            MainCamera.ApplyTransform();
            GL.MatrixMode(MatrixMode.Modelview);
        }

        public override void ResetCamera()
        {
            //MainCamera.Rotation = 0f;
            MainCamera.Position = Vector2.Zero;
            MainCamera.Scale = new Vector2(1f, 1f);
        }
    }
}

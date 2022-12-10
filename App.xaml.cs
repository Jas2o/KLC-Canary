using LibKaseya;
using nucs.JsonSettings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KLC_Finch {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        public const string appName = "KLCFC";
        public static Mutex mutex = null;

        public static string Version;
        public static WindowAlternative winStandalone;
        public static WindowViewerV4 winStandaloneViewer;
        public static WindowCharm winCharm;
        public static Settings Settings;
        public static KLCShared Shared;
        public static DecodeMode TexDecodeMode { get; private set; }
        public static bool SupportsOpenGL;

        public App() : base() {
            if (!Debugger.IsAttached) {
                //Setup exception handling rather than closing rudely
                AppDomain.CurrentDomain.UnhandledException += (sender, args) => ShowUnhandledException(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
                TaskScheduler.UnobservedTaskException += (sender, args) => {
                    if (Settings.AltShowWarnings)
                        ShowUnhandledExceptionFromSrc(args.Exception, "TaskScheduler.UnobservedTaskException");
                    args.SetObserved();
                };

                Dispatcher.UnhandledException += (sender, args) => {
                    args.Handled = true;
                    if (Settings.AltShowWarnings)
                        ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException");
                };
            }

            Version = KLC_Finch.Properties.Resources.BuildDate.Trim();

            //--

            string pathSettings = Path.GetDirectoryName(Environment.ProcessPath) + "\\KLC-Finch-config.json";
            if (File.Exists(pathSettings))
                Settings = JsonSettings.Load<Settings>(pathSettings);
            else
                Settings = JsonSettings.Construct<Settings>(pathSettings);

            string pathShared = Path.GetDirectoryName(Environment.ProcessPath) + @"\KLC-Shared.json";
            if (File.Exists(pathShared))
                Shared = JsonSettings.Load<KLCShared>(pathShared);
            else
                Shared = JsonSettings.Construct<KLCShared>(pathShared);

            //--

            SupportsOpenGL = !SystemParameters.IsRemoteSession; 
            //SupportsOpenGL = System.Windows.Media.RenderCapability.IsPixelShaderVersionSupported(1, 0) || System.Windows.Media.RenderCapability.IsPixelShaderVersionSupportedInSoftware(1, 0);

            //--

            if (Settings.Renderer == 2 || !SupportsOpenGL) {
                //Canvas
                if (App.Settings.RendererAlt)
                    TexDecodeMode = DecodeMode.RawY;
                else
                    TexDecodeMode = DecodeMode.BitmapRGB;
            } else
            {
                if (App.Settings.RendererAlt)
                    TexDecodeMode = DecodeMode.BitmapRGB;
                else
                    TexDecodeMode = DecodeMode.RawYUV;
            }
        }

        public static void ShowUnhandledExceptionFromSrc(Exception e, string source) {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                new WindowException(e, source).Show();
            });
        }

        public static void ShowUnhandledExceptionFromSrc(string error, string source) {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                new WindowException(error, source).Show();
            });
        }

        [STAThread]
        private static void ShowUnhandledException(Exception e, string unhandledExceptionType) {
            new WindowException(e, unhandledExceptionType).Show(); //Removed: , Debugger.IsAttached
        }

        private void Application_Startup(object sender, StartupEventArgs e) {
            foreach (string vsa in App.Shared.VSA)
            {
                Kaseya.Start(vsa, KaseyaAuth.GetStoredAuth(vsa));
            }

            string[] args = Environment.GetCommandLineArgs();
            bool useCharm = args.Contains("-charm");
            bool createdNew = true;
            if (useCharm)
            {
                mutex = new Mutex(true, appName, out createdNew);
                if (!createdNew)
                    mutex.Close();
            }
            KLCCommand command = null;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("liveconnect:///")) {
                    if(useCharm && !createdNew)
                    {
                        NamedPipeListener.SendMessage(appName, true, args[i]);
                        Environment.Exit(0);
                    } else
                        command = KLCCommand.NewFromBase64(args[i].Replace("liveconnect:///", ""));
                }
            }

            //var pair = Kaseya.VSA.First();
            //command = KLCCommand.Example(pair.Key, "694656559250358", pair.Value.Token);
            //command.SetForLiveConnect();

            if (useCharm)
            {
                if(!createdNew) {
                    NamedPipeListener.SendMessage(App.appName, true, "focus");
                    Environment.Exit(0);
                }
                winCharm = new WindowCharm();
                winCharm.Show();
            } else
            {
                if (command != null)
                {
                    if (command.payload.navId == "remotecontrol/shared")
                        winStandalone = new WindowAlternative(command.payload.agentId, command.VSA, command.payload.auth.Token, Enums.OnConnect.OnlyRC, Enums.RC.Shared);
                    else if (command.payload.navId == "remotecontrol/private")
                        winStandalone = new WindowAlternative(command.payload.agentId, command.VSA, command.payload.auth.Token, Enums.OnConnect.OnlyRC, Enums.RC.Private);
                    else if (command.payload.navId.StartsWith("remotecontrol/private/#"))
                        winStandalone = new WindowAlternative(command.payload.agentId, command.VSA, command.payload.auth.Token, Enums.OnConnect.AlsoRC, Enums.RC.NativeRDP);
                    else if (command.payload.navId == "remotecontrol/1-click")
                        winStandalone = new WindowAlternative(command.payload.agentId, command.VSA, command.payload.auth.Token, Enums.OnConnect.OnlyRC, Enums.RC.OneClick);
                    else
                        winStandalone = new WindowAlternative(command.payload.agentId, command.VSA, command.payload.auth.Token);

                    //Console.WriteLine(command.payload.navId + ": " + command.payload.agentId);
                    winStandalone.Show();
                }
                else
                {
                    new MainWindow().Show();
                }
            }
		}

	}
}

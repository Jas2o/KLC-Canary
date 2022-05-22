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
        public static DecodeMode TexDecodeMode { get; private set; }

        public App() : base()
        {
            if (!Debugger.IsAttached)
            {
                //Setup exception handling rather than closing rudely (this doesn't really work well).
                AppDomain.CurrentDomain.UnhandledException += (sender, args) => ShowUnhandledException(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
                TaskScheduler.UnobservedTaskException += (sender, args) =>
                {
                    ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException");
                    args.SetObserved();
                };

                Dispatcher.UnhandledException += (sender, args) =>
                {
                    args.Handled = true;
                    ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException");
                };
            }

            Version = KLC_Finch.Properties.Resources.BuildDate.Trim();

            string pathSettings = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\KLC-Finch-config.json";
            if (File.Exists(pathSettings))
                Settings = JsonSettings.Load<Settings>(pathSettings);
            else
                Settings = JsonSettings.Construct<Settings>(pathSettings);
            Bookmarks.Load();

            if (Settings.Renderer == 2) {
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
        private void ShowUnhandledException(Exception e, string unhandledExceptionType) {
            new WindowException(e, unhandledExceptionType).Show(); //Removed: , Debugger.IsAttached
        }

        private void Application_Startup(object sender, StartupEventArgs e) {
			Kaseya.Start();

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
                if(args[i].StartsWith("liveconnect:///")) {
                    if(useCharm && !createdNew)
                    {
                        NamedPipeListener<string>.SendMessage(appName, true, args[i]);
                        Environment.Exit(0);
                    } else
                        command = KLCCommand.NewFromBase64(args[i].Replace("liveconnect:///", ""));
                }
            }

            //command = KLCCommand.Example("guidhere", KaseyaAuth.GetStoredAuth());
            //command.SetForRemoteControl(false, true);

            if (useCharm)
            {
                if(!createdNew) {
                    NamedPipeListener<string>.SendMessage(App.appName, true, "focus");
                    Environment.Exit(0);
                }
                winCharm = new WindowCharm();
                winCharm.Show();
            } else
            {
                if (command != null)
                {
                    if (command.payload.navId == "remotecontrol/shared")
                        winStandalone = new WindowAlternative(command.payload.agentId, command.payload.auth.Token, Enums.OnConnect.OnlyRC, Enums.RC.Shared);
                    else if (command.payload.navId == "remotecontrol/private")
                        winStandalone = new WindowAlternative(command.payload.agentId, command.payload.auth.Token, Enums.OnConnect.OnlyRC, Enums.RC.Private);
                    else if (command.payload.navId == "remotecontrol/1-click")
                        winStandalone = new WindowAlternative(command.payload.agentId, command.payload.auth.Token, Enums.OnConnect.OnlyRC, Enums.RC.OneClick);
                    else
                        winStandalone = new WindowAlternative(command.payload.agentId, command.payload.auth.Token);

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

using KLC_Finch;
using LibKaseya;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using static LibKaseya.Enums;

namespace KLC {
    public class FakeLiveConnectSession : ILiveConnectSession {

        public WsM WebsocketM { get; }
        public WsA WebsocketA { get; }
        public WsB WebsocketB { get; }

        public string agentGuid { get; private set; }
        public Agent agent { get; private set; }
        public KaseyaAuth Auth { get; private set; }

        public Structure.EAL Eal { get; private set; }
        public Structure.EIRC Eirc { get; private set; }
        public string RandSessionGuid { get; private set; }
        public Enums.NotifyApproval RCNotify { get; private set; }

        public Dashboard ModuleDashboard { get; set; }
        public StaticImage ModuleStaticImage { get; set; } //Sub-module of Dashboard
        public RemoteControl ModuleRemoteControl { get; set; }
        public CommandTerminal ModuleCommandTerminal { get; set; }
        public CommandPowershell ModuleCommandPowershell { get; set; }
        public FileExplorer ModuleFileExplorer { get; set; }
        public RegistryEditor ModuleRegistryEditor { get; set; }
        public KLC_Finch.Modules.Events ModuleEvents { get; set; }
        public KLC_Finch.Modules.Services ModuleServices { get; set; }
        public KLC_Finch.Modules.Processes ModuleProcesses { get; set; }
        public Toolbox ModuleToolbox { get; set; }
        public Forwarding ModuleForwarding { get; set; }

        public WindowAlternative.StatusCallback CallbackS { get; set; }
        public WindowAlternative.ErrorCallback CallbackE { get; set; }
        public Enums.EPStatus Status { get; set; }
        public int StatusConnectionAttempt { get; set; }

        public FakeLiveConnectSession(WindowAlternative.StatusCallback callbackS = null, WindowAlternative.ErrorCallback callbackE = null) {
            int PortB = LiveConnectSession.GetNewPort();

            CallbackS = callbackS;
            CallbackE = callbackE;
            agent = new Agent(Agent.VsaSim, PortB.ToString());

            RCNotify = NotifyApproval.None;

            Auth = new KaseyaAuth();
            Eal = new Structure.EAL();
            Eirc = new Structure.EIRC();

            //--

            WebsocketB = new WsB(this, PortB);

            int PortA = LiveConnectSession.GetNewPort();
            WebsocketA = new WsA(this, PortA, PortB);

            NamedPipeListener.SendMessage("KLCMITM", true, PortA.ToString());
        }

        public void Close() {
            if (WebsocketB != null)
                WebsocketB.Close();
            //if (WebsocketA != null)
                //WebsocketA.Close();
        }

        public string GetWiresharkFilter() {
            string filter = string.Format("(tcp.srcport == {0}) || (tcp.dstport == {0})", WebsocketB.PortB);

            //string filter = string.Format("(tcp.srcport == {0}) || (tcp.dstport == {0})", WebsocketA.PortA);
            //filter += string.Format(" || (tcp.srcport == {0}) || (tcp.dstport == {0})", WebsocketB.PortB);

            return filter;
        }

        public string GetStatusText(OnConnect directAction = OnConnect.NoAction) {
            switch (Status) {
                case EPStatus.AttemptingToConnect:
                case EPStatus.PeerOffline:
                case EPStatus.PeerToPeerFailure:
                    if (directAction != OnConnect.NoAction)
                        return "Attempt " + StatusConnectionAttempt + " to connect and open Remote Control... ";
                    else
                        return "Attempt " + StatusConnectionAttempt + " to connect...";

                case EPStatus.Connected:
                    return "Connected";

                case EPStatus.UnavailableWsA:
                    return "Endpoint Unavailable (Web Socket A)";

                case EPStatus.DisconnectedWsB:
                    return "Endpoint Disconnected (Web Socket B)";

                case EPStatus.DisconnectedManual:
                    return "Manual Disconnection";

                case EPStatus.UnableToStartSession:
                    return "Unable to start session with endpoint.";

                case EPStatus.AuthFailed:
                    return "Authentication failure or cannot communicate with VSA.";

                case EPStatus.NativeRDPStarting:
                    return "Native RDP - Starting TCP Tunneling";

                case EPStatus.NativeRDPActive:
                    return "Native RDP - TCP Tunneling Active";

                case EPStatus.NativeRDPEnded:
                    return "Native RDP - Ended";

                default:
                    return "Status unknown: " + Status;
            }
        }

        public Visibility GetStatusVisibility() {
            switch (Status) {
                case EPStatus.Connected:
                case EPStatus.NativeRDPEnded:
                    return Visibility.Collapsed;

                /*
                case EPStatus.AttemptingToConnect:
                case EPStatus.PeerOffline:
                case EPStatus.PeerToPeerFailure:
                case EPStatus.UnavailableWsA:
                case EPStatus.DisconnectedWsB:
                case EPStatus.DisconnectedManual:
                case EPStatus.UnableToStartSession:
                case EPStatus.AuthFailed:
                case EPStatus.NativeRDPStarting:
                case EPStatus.NativeRDPActive:
                */
                default:
                    return Visibility.Visible;
            }
        }

        private static SolidColorBrush brushBlue1 = new SolidColorBrush(Colors.DeepSkyBlue);
        private static SolidColorBrush brushBlue2 = new SolidColorBrush(Colors.DodgerBlue);
        private static SolidColorBrush brushGreen = new SolidColorBrush(Colors.Green);
        private static SolidColorBrush brushOrange = new SolidColorBrush(Colors.DarkOrange);
        private static SolidColorBrush brushMaroon = new SolidColorBrush(Colors.Maroon);
        private static SolidColorBrush brushDimGray = new SolidColorBrush(Colors.DimGray);
        private static SolidColorBrush brushMagenta = new SolidColorBrush(Colors.Magenta);

        public SolidColorBrush GetStatusColor() {
            switch (Status) {
                case EPStatus.AttemptingToConnect:
                case EPStatus.PeerOffline:
                case EPStatus.PeerToPeerFailure:
                    if (StatusConnectionAttempt % 2 == 0)
                        return brushBlue1;
                    else
                        return brushBlue2;

                case EPStatus.Connected:
                case EPStatus.NativeRDPStarting:
                case EPStatus.NativeRDPActive:
                case EPStatus.NativeRDPEnded:
                    return brushGreen;

                case EPStatus.UnavailableWsA:
                    return brushOrange;

                case EPStatus.DisconnectedWsB:
                    return brushMaroon;

                case EPStatus.DisconnectedManual:
                case EPStatus.UnableToStartSession:
                case EPStatus.AuthFailed:
                    return brushDimGray;

                default:
                    return brushMagenta;
            }
        }

    }
}

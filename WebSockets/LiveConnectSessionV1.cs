using KLC_Finch;
using LibKaseya;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media;
using static LibKaseya.Enums;

namespace KLC {

    public class LiveConnectSession : ILiveConnectSession {
        
        public WsM WebsocketM { get; private set; }
        public WsA WebsocketA { get; private set; }
        public WsB WebsocketB { get; private set; }

        public string shorttoken; //KLC would normally change tokens during initial connection
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

        public LiveConnectSession(string vsa, string shortToken, string agentID, WindowAlternative.StatusCallback callbackS = null, WindowAlternative.ErrorCallback callbackE = null) {
            agentGuid = agentID;
            shorttoken = shortToken;
            CallbackS = callbackS;
            CallbackE = callbackE;
            Kaseya.LoadToken(vsa, shortToken);
            agent = new Agent(vsa, agentGuid);

            Auth = KaseyaAuth.ApiAuthX(shorttoken, vsa);
            if (Auth == null) {
                CallbackS?.Invoke(Enums.EPStatus.AuthFailed);
                return;
            }
            shortToken = Auth.Token; //Works fine without this line, but it's something KLC does.
            Eal = Api15.EndpointsAdminLogin(vsa, shorttoken);

            JObject rcNotifyPolicy = agent.GetRemoteControlNotifyPolicyFromAPI();
            if (rcNotifyPolicy["Result"] != null) {
                if (rcNotifyPolicy["Result"]["RemoteControlNotify"] != null)
                    RCNotify = (Enums.NotifyApproval)(int)rcNotifyPolicy["Result"]["RemoteControlNotify"];
            } else
                return; //AgentID doesn't exist?

            ////dynamic agentSettings = agent.GetAgentSettingsInfoFromAPI(shorttoken);
            ////dynamic auditSummary = agent.GetAgentAuditSummaryFromAPI(shorttoken);

            try {
                Eirc = Api15.EndpointsInitiateRemoteControl(vsa, shorttoken, agentGuid);
            } catch (Exception) {
                CallbackS?.Invoke(Enums.EPStatus.UnableToStartSession);
                return;
            }
            RandSessionGuid = Guid.NewGuid().ToString();

            //jsonAgentSettings = Api10.AssetmgmtAgentSettings(shorttoken, agentGuid);
            //jsonAgentSummary = Api10.AssetmgmtAuditSummary(shorttoken, agentGuid);
            //jsonRemoteControlNotifyPolicy = Api10.RemoteControlNotifyPolicy(shorttoken, agentGuid);

            int PortB = GetNewPort();

            //if(WsM.CertificateExists()) WebsocketM = new WsM(this, GetNewPort());

            WebsocketB = new WsB(this, PortB);
            WebsocketA = new WsA(this, GetNewPort(), PortB);
        }

        public static int GetNewPort() {
            TcpListener tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 0);
            tcpListener.Start();
            int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();

            return port;
        }

        public void Close() {
            //if (ModuleRemoteControl != null)
                //ModuleRemoteControl.Disconnect();
            if (WebsocketB != null)
                WebsocketB.Close();
            if (WebsocketA != null)
                WebsocketA.Close();
        }

        public string GetWiresharkFilter() {
            string filter = string.Format("(tcp.srcport == {0}) || (tcp.dstport == {0})", WebsocketA.PortA);
            filter += string.Format(" || (tcp.srcport == {0}) || (tcp.dstport == {0})", WebsocketB.PortB);

            return filter;
        }

        public string GetStatusText(OnConnect directAction = OnConnect.NoAction)
        {
            switch (Status)
            {
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

        public Visibility GetStatusVisibility()
        {
            switch (Status)
            {
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

        public SolidColorBrush GetStatusColor()
        {
            switch (Status)
            {
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
using KLC_Finch;
using LibKaseya;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static LibKaseya.Enums;

namespace KLC {
    public interface ILiveConnectSession {

        //public static List<ILiveConnectSession> listSession = new List<ILiveConnectSession>();

        public WsM WebsocketM { get; }
        public WsA WebsocketA { get; }
        public WsB WebsocketB { get; }

        public string agentGuid { get; }
        public Agent agent { get; }
        public KaseyaAuth Auth { get; }

        public Structure.EAL Eal { get; }
        public Structure.EIRC Eirc { get; }
        public string RandSessionGuid { get; }
        public Enums.NotifyApproval RCNotify { get; }

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

        void Close();

        string GetWiresharkFilter();

        string GetStatusText(OnConnect directAction = OnConnect.NoAction);
        SolidColorBrush GetStatusColor();
        Visibility GetStatusVisibility();

    }
}

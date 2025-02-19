using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using static LibKaseya.Enums;

namespace KLC_Finch {
    /// <summary>
    /// Interaction logic for controlDashboard.xaml
    /// </summary>
    public partial class controlDashboard : UserControl {

        private Dashboard moduleDashboard;
        private StaticImage moduleStaticImage;

        public controlDashboard() {
            InitializeComponent();
        }

        public void UpdateDisplayData(KLC.ILiveConnectSession session) {
            //KLC.LiveConnectSession session = ((WindowAlternative)Window.GetWindow(this)).session;

            txtUtilisationRAM.Text = "RAM: " + session.agent.RAMinGB + " GB";
            DisplayRCNotify(session.RCNotify);
            DisplayMachineNote(session.agent.MachineShowToolTip, session.agent.MachineNote, session.agent.MachineNoteLink);
        }

        public void btnDashboardStartData_Click(object sender, RoutedEventArgs e) {
            KLC.ILiveConnectSession session = ((WindowAlternative)Window.GetWindow(this)).session;
            if (session != null) { //Intentionally different
                btnDashboardStartData.IsEnabled = false;

                moduleDashboard = new Dashboard(session, txtDashboard, stackDisks, txtUtilisationRAM, txtUtilisationCPU, progressCPU, progressRAM);
                session.ModuleDashboard = moduleDashboard;
            }
        }

        private void btnDashboardGetCpuRam_Click(object sender, RoutedEventArgs e) {
            if (moduleDashboard != null) {
                moduleDashboard.GetCpuRam();
                //moduleDashboard.GetTopEvents();
                //moduleDashboard.GetTopProcesses();
            }
        }

        private void btnDashboardGetVolumes_Click(object sender, RoutedEventArgs e) {
            if (moduleDashboard != null)
                moduleDashboard.GetVolumes();
        }

        public void btnStaticImageStart_Click(object sender, RoutedEventArgs e) {
            KLC.ILiveConnectSession session = ((WindowAlternative)Window.GetWindow(this)).session;
            if (session != null) { //Intentionally different
                btnStaticImageStart.IsEnabled = false;
                lblStaticImage.Visibility = Visibility.Collapsed;

                moduleStaticImage = new StaticImage(session, imgScreenPreview);
                session.ModuleStaticImage = moduleStaticImage;
            }
        }

        private void btnStaticImageRefresh_Click(object sender, RoutedEventArgs e) {
            if (moduleStaticImage == null)
                return;

            moduleStaticImage.RequestRefresh();
        }

        private void btnStaticImageRefreshFull_Click(object sender, RoutedEventArgs e) {
            if (moduleStaticImage == null)
                return;

            moduleStaticImage.RequestRefreshFull();
        }

        public void DisplayRCNotify(LibKaseya.Enums.NotifyApproval policy) {
            txtRCNotify.Text = LibKaseyaLiveConnect.Text.RCNotify(policy);
        }

        //session.agent.MachineShowToolTip, session.agent.MachineNote, session.agent.MachineNoteLink
        public void DisplayMachineNote(int machineShowToolTip, string machineNote, string machineNoteLink=null) {
            if (machineNote == null)
                return;

            if (machineShowToolTip == 0 && machineNote.Length == 0) {
                txtSpecialInstructions.Visibility = txtMachineNote.Visibility = Visibility.Collapsed;
                return;
            }

            if (machineShowToolTip > 0) {
                if (Enum.IsDefined(typeof(Badge), machineShowToolTip))
                    txtSpecialInstructions.Text = "Special Instructions for this Machine (" + Enum.GetName(typeof(Badge), machineShowToolTip) + ")";
                else
                    txtSpecialInstructions.Text = "Special Instructions for this Machine (" + machineShowToolTip + ")";
            }

            if (machineNoteLink != null) {
                txtMachineNoteLink.NavigateUri = new Uri(machineNoteLink);
                txtMachineNoteLinkText.Text = machineNoteLink;
                txtMachineNoteText.Text = machineNote;
            } else {
                txtMachineNoteLinkText.Text = string.Empty;
                txtMachineNoteText.Text = machineNote;
            }

        }

        private void txtMachineNoteLink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        }

        private void btnStaticImageDumpLayout_Click(object sender, RoutedEventArgs e)
        {
            string info = moduleStaticImage.DumpScreens();
            ClipboardHelper.SetText(info);
            //Clipboard.SetDataObject(info); //Has issues
        }

        public void UpdateTimer()
        {
            if (moduleDashboard != null)
                moduleDashboard.UpdateTimer();
        }

    }
}

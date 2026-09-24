using System.Windows.Controls;

namespace CustomInstallerUI.Dialogs
{
    /// <summary>
    /// Equivalent of the MSI ProgressDlg: shown while msiexec performs the
    /// actual install / uninstall.
    /// </summary>
    public partial class ProgressDlg : UserControl
    {
        public ProgressDlg() => InitializeComponent();

        public void ReportStatus(string status) => StatusText.Text = status;
    }
}

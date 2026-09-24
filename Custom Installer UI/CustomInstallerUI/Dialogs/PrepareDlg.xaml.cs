using System.Windows.Controls;

namespace CustomInstallerUI.Dialogs
{
    /// <summary>
    /// Equivalent of the MSI PrepareDlg: shown while the wizard reads the
    /// package information and queries the installation state.
    /// </summary>
    public partial class PrepareDlg : UserControl
    {
        public PrepareDlg() => InitializeComponent();
    }
}

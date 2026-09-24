using System.Windows.Controls;

namespace CustomInstallerUI.Dialogs
{
    /// <summary>
    /// Equivalent of the MSI VerifyReadyDlg (or VerifyRemoveDlg in uninstall
    /// mode): last confirmation before the operation starts.
    /// </summary>
    public partial class VerifyReadyDlg : UserControl
    {
        public VerifyReadyDlg() => InitializeComponent();

        public void Configure(MsiPackageInfo package, bool uninstallMode, string? installFolder)
        {
            if (uninstallMode)
            {
                ReadyTitle.Text = "Ready to Remove";
                SummaryText.Text =
                    $"Product:  {package.ProductName}\n" +
                    $"Version:  {package.ProductVersion}";
                ReadyBody.Text =
                    $"Click Remove to remove {package.ProductName} from your computer, " +
                    "or click Back to review your settings. Click Cancel to exit the wizard.";
            }
            else
            {
                ReadyTitle.Text = "Ready to Install";
                SummaryText.Text =
                    $"Product:  {package.ProductName}\n" +
                    $"Version:  {package.ProductVersion}\n" +
                    $"Destination folder:  {installFolder}";
                ReadyBody.Text =
                    "Click Install to begin the installation, or click Back if you want " +
                    "to review or change any of your settings. Click Cancel to exit the wizard.";
            }
        }
    }
}

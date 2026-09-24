using System.Windows.Controls;

namespace CustomInstallerUI.Dialogs
{
    /// <summary>
    /// Equivalent of the MSI WelcomeDlg (or the maintenance welcome when the
    /// product is already installed and the wizard runs in uninstall mode).
    /// </summary>
    public partial class WelcomeDlg : UserControl
    {
        public WelcomeDlg() => InitializeComponent();

        public void Configure(MsiPackageInfo package, bool uninstallMode)
        {
            if (uninstallMode)
            {
                WelcomeTitle.Text = $"Welcome to the {package.ProductName} Uninstall Wizard";
                WelcomeBody.Text =
                    $"{package.ProductName} {package.ProductVersion} is installed on this " +
                    "computer. The wizard will remove it. Click Next to continue, " +
                    "or Cancel to exit the wizard.";
            }
            else
            {
                WelcomeTitle.Text = $"Welcome to the {package.ProductName} Setup Wizard";
                WelcomeBody.Text =
                    $"The Setup Wizard will install {package.ProductName} " +
                    $"{package.ProductVersion} on your computer. Click Next to continue, " +
                    "or Cancel to exit the wizard.";
            }
        }
    }
}

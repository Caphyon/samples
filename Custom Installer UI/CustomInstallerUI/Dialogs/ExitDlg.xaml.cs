using System.Windows;
using System.Windows.Controls;

namespace CustomInstallerUI.Dialogs
{
    /// <summary>
    /// Equivalent of the MSI ExitDlg: final page, with the optional
    /// "launch application" checkbox after a successful install.
    /// </summary>
    public partial class ExitDlg : UserControl
    {
        public ExitDlg() => InitializeComponent();

        public bool LaunchRequested =>
            LaunchCheckBox.Visibility == Visibility.Visible &&
            LaunchCheckBox.IsChecked == true;

        public void Configure(string productName) =>
            LaunchCheckBox.Content = $"Launch {productName} now";

        public void SetCompleted(string productName, bool installMode, bool rebootRequired)
        {
            CompletedText.Text = installMode
                ? $"{productName} was installed successfully." +
                  (rebootRequired ? " A restart may be required to complete the installation." : "")
                : $"{productName} was removed from your computer.";
        }

        public void ShowLaunchOption(bool visible) =>
            LaunchCheckBox.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }
}

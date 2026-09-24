using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace CustomInstallerUI.Dialogs
{
    /// <summary>
    /// Equivalent of the MSI FolderDlg: lets the user pick the destination
    /// folder. The value is passed to msiexec as the APPDIR property.
    /// </summary>
    public partial class FolderDlg : UserControl
    {
        private string _productName = "DemoApp";

        public FolderDlg() => InitializeComponent();

        public void Configure(string productName) => _productName = productName;

        public string SelectedPath
        {
            get => PathTextBox.Text.Trim();
            set => PathTextBox.Text = value;
        }

        public bool Validate(out string error)
        {
            if (SelectedPath.Length == 0 || !Path.IsPathRooted(SelectedPath))
            {
                error = "Please enter a full destination path (e.g. C:\\Program Files (x86)\\My Company\\DemoApp).";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // OpenFolderDialog is built into WPF starting with .NET 8.
            var dialog = new OpenFolderDialog { Title = "Choose the installation folder" };

            if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            {
                // Install into a product sub-folder, so APPDIR never points at
                // a folder that may contain unrelated files.
                string folder = dialog.FolderName;
                if (!string.Equals(
                        Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)),
                        _productName, StringComparison.OrdinalIgnoreCase))
                {
                    folder = Path.Combine(folder, _productName);
                }
                PathTextBox.Text = folder;
            }
        }
    }
}

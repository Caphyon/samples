using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using CustomInstallerUI.Dialogs;

namespace CustomInstallerUI
{
    
    public partial class MainWindow : Window
    {
        private enum WizardPage { Prepare, Welcome, Folder, VerifyReady, Progress, Exit }
        private enum SetupMode { Install, Uninstall }

        private readonly PrepareDlg     _prepareDlg     = new();
        private readonly WelcomeDlg     _welcomeDlg     = new();
        private readonly FolderDlg      _folderDlg      = new();
        private readonly VerifyReadyDlg _verifyReadyDlg = new();
        private readonly ProgressDlg    _progressDlg    = new();
        private readonly ExitDlg        _exitDlg        = new();

        private WizardPage _page;
        private SetupMode  _mode;
        private MsiPackageInfo? _package;

        public MainWindow()
        {
            InitializeComponent();
            ShowPage(WizardPage.Prepare);
            Loaded += async (_, _) => await PrepareAsync();
        }

       
        private async Task PrepareAsync()
        {
            try
            {
                var (package, installed) = await Task.Run(() =>
                {
                    MsiPackageInfo pkg = MsiService.LoadPackage();
                    return (pkg, MsiService.IsInstalled(pkg.ProductCode));
                });

                
                await Task.Delay(800);

                _package = package;
                _mode = installed ? SetupMode.Uninstall : SetupMode.Install;

                Title            = $"{package.ProductName} Setup";
                HeaderTitle.Text = package.ProductName;

                _welcomeDlg.Configure(package, _mode == SetupMode.Uninstall);
                _folderDlg.Configure(package.ProductName);
                _folderDlg.SelectedPath = MsiService.GetDefaultInstallDirectory(package);
                _exitDlg.Configure(package.ProductName);

                ShowPage(WizardPage.Welcome);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Setup",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

       
        private void ShowPage(WizardPage page)
        {
            _page = page;

            switch (page)
            {
                case WizardPage.Prepare:
                    DialogHost.Content = _prepareDlg;
                    HeaderSubtitle.Text = "Preparing";
                    SetButtons(backEnabled: false, nextText: "Next >", nextEnabled: false, cancelEnabled: true);
                    break;

                case WizardPage.Welcome:
                    DialogHost.Content = _welcomeDlg;
                    HeaderSubtitle.Text = _mode == SetupMode.Install ? "Setup Wizard" : "Uninstall Wizard";
                    SetButtons(backEnabled: false, nextText: "Next >", nextEnabled: true, cancelEnabled: true);
                    break;

                case WizardPage.Folder:
                    DialogHost.Content = _folderDlg;
                    HeaderSubtitle.Text = "Destination Folder";
                    SetButtons(backEnabled: true, nextText: "Next >", nextEnabled: true, cancelEnabled: true);
                    break;

                case WizardPage.VerifyReady:
                    _verifyReadyDlg.Configure(_package!, _mode == SetupMode.Uninstall,
                        _mode == SetupMode.Install ? _folderDlg.SelectedPath : null);
                    DialogHost.Content = _verifyReadyDlg;
                    HeaderSubtitle.Text = _mode == SetupMode.Install ? "Ready to Install" : "Ready to Remove";
                    SetButtons(backEnabled: true,
                               nextText: _mode == SetupMode.Install ? "Install" : "Remove",
                               nextEnabled: true, cancelEnabled: true);
                    break;

                case WizardPage.Progress:
                    DialogHost.Content = _progressDlg;
                    HeaderSubtitle.Text = _mode == SetupMode.Install ? "Installing" : "Removing";
                    SetButtons(backEnabled: false, nextText: "Next >", nextEnabled: false, cancelEnabled: false);
                    break;

                case WizardPage.Exit:
                    DialogHost.Content = _exitDlg;
                    HeaderSubtitle.Text = "Completed";
                    SetButtons(backEnabled: false, nextText: "Finish", nextEnabled: true, cancelEnabled: false);
                    break;
            }
        }

        private void SetButtons(bool backEnabled, string nextText, bool nextEnabled, bool cancelEnabled)
        {
            BackButton.IsEnabled   = backEnabled;
            NextButton.Content     = nextText;
            NextButton.IsEnabled   = nextEnabled;
            CancelButton.IsEnabled = cancelEnabled;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_page)
            {
                case WizardPage.Folder:
                    ShowPage(WizardPage.Welcome);
                    break;

                case WizardPage.VerifyReady:
                    ShowPage(_mode == SetupMode.Install ? WizardPage.Folder : WizardPage.Welcome);
                    break;
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_page)
            {
                case WizardPage.Welcome:
                    ShowPage(_mode == SetupMode.Install ? WizardPage.Folder : WizardPage.VerifyReady);
                    break;

                case WizardPage.Folder:
                    if (!_folderDlg.Validate(out string error))
                    {
                        MessageBox.Show(this, error, Title,
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    ShowPage(WizardPage.VerifyReady);
                    break;

                case WizardPage.VerifyReady:
                    await RunOperationAsync();
                    break;

                case WizardPage.Exit:
                    if (_mode == SetupMode.Install && _exitDlg.LaunchRequested)
                        MsiService.LaunchInstalledApp(_package!.ProductCode);
                    Close();
                    break;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show(this,
                "Are you sure you want to cancel the setup?",
                Title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
                Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
           
            if (_page == WizardPage.Progress)
                e.Cancel = true;

            base.OnClosing(e);
        }

        
        private async Task RunOperationAsync()
        {
            ShowPage(WizardPage.Progress);
            var status = new Progress<string>(s => _progressDlg.ReportStatus(s));

            try
            {
                if (_mode == SetupMode.Install)
                {
                    
                    string installFolder = _folderDlg.SelectedPath;

                    int exitCode = await Task.Run(() =>
                        MsiService.Install(_package!, installFolder, status));

                    _exitDlg.SetCompleted(_package!.ProductName, installMode: true,
                                          rebootRequired: exitCode == 3010);
                    _exitDlg.ShowLaunchOption(
                        MsiService.TryGetInstalledExePath(_package.ProductCode) != null);
                }
                else
                {
                    await Task.Run(() => MsiService.Uninstall(_package!, status));

                    _exitDlg.SetCompleted(_package!.ProductName, installMode: false,
                                          rebootRequired: false);
                    _exitDlg.ShowLaunchOption(false);
                }

                ShowPage(WizardPage.Exit);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Title,
                                MessageBoxButton.OK, MessageBoxImage.Error);
                ShowPage(WizardPage.VerifyReady);
            }
        }
    }
}

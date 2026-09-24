using System.Windows;

namespace CustomInstallerUI
{
  
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
          

            MsiService.CompletePendingSelfDelete();
            base.OnExit(e);
        }
    }
}

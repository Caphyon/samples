using System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MyDemoService
{
    public partial class Service1 : ServiceBase
    {
        private static readonly string LogDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Caphyon",
            "ServiceSample");

        private static readonly string LogFilePath = Path.Combine(LogDirectoryPath, "ServiceHeartbeat.log");

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteHeartbeat(string.Format("Service started under {0}", GetAccountDescription()));
        }

        protected override void OnStop()
        {
            WriteHeartbeat(string.Format("Service stopped under {0}", GetAccountDescription()));
        }

        private static void WriteHeartbeat(string message)
        {
            Directory.CreateDirectory(LogDirectoryPath);

            File.AppendAllText(
                LogFilePath,
                string.Format("{0:yyyy-MM-dd HH:mm:ss} - {1}{2}", DateTime.Now, message, Environment.NewLine));
        }

        private static string GetAccountDescription()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                if (identity.User != null)
                {
                    if (identity.User.IsWellKnown(WellKnownSidType.LocalSystemSid))
                    {
                        return "Local System account";
                    }

                    if (identity.User.IsWellKnown(WellKnownSidType.LocalServiceSid))
                    {
                        return "Local Service account";
                    }

                    if (identity.User.IsWellKnown(WellKnownSidType.NetworkServiceSid))
                    {
                        return "Network Service account";
                    }
                }

                return string.Format("'{0}' account", identity.Name);
            }
        }
    }
}

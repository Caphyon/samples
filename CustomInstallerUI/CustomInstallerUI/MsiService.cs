using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace CustomInstallerUI
{
   
    public sealed record MsiPackageInfo(
        string MsiPath, string ProductCode, string ProductName,
        string ProductVersion, string Manufacturer);

    
    public static class MsiService
    {
       
        public const string MainExecutable = "DemoApp.exe";

        
        private const string BootstrapperUninstallKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DemoApp";

        private const int InstallStateDefault = 5;   
        private const uint ErrorMoreData      = 234; 
        private const int MsiExitSuccess        = 0;
        private const int MsiExitRebootRequired = 3010;

        

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        private static extern int MsiQueryProductState(string szProduct);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        private static extern uint MsiOpenDatabase(string szDatabasePath, IntPtr szPersist,
                                                   out IntPtr phDatabase);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        private static extern uint MsiDatabaseOpenView(IntPtr hDatabase, string szQuery,
                                                       out IntPtr phView);

        [DllImport("msi.dll")]
        private static extern uint MsiViewExecute(IntPtr hView, IntPtr hRecord);

        [DllImport("msi.dll")]
        private static extern uint MsiViewFetch(IntPtr hView, out IntPtr phRecord);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        private static extern uint MsiRecordGetString(IntPtr hRecord, uint iField,
                                                      StringBuilder szValueBuf, ref uint pcchValueBuf);

        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        private static extern uint MsiGetProductInfo(string szProduct, string szAttribute,
                                                     StringBuilder lpValueBuf, ref uint pcchValueBuf);

        [DllImport("msi.dll")]
        private static extern uint MsiCloseHandle(IntPtr hAny);

       
        public static string ExtractedMsiDirectory =>
            Path.Combine(Path.GetTempPath(), "DemoAppSetup");

        
        public static string BootstrapperHomeDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                         "DemoApp", "Setup");

        public static string InstallLogPath   => Path.Combine(Path.GetTempPath(), "DemoApp_install.log");
        public static string UninstallLogPath => Path.Combine(Path.GetTempPath(), "DemoApp_uninstall.log");

        
        public static MsiPackageInfo LoadPackage()
        {
            string msiPath = ExtractEmbeddedMsi();

            return new MsiPackageInfo(
                msiPath,
                ReadMsiProperty(msiPath, "ProductCode"),
                ReadMsiProperty(msiPath, "ProductName"),
                ReadMsiProperty(msiPath, "ProductVersion"),
                ReadMsiProperty(msiPath, "Manufacturer"));
        }

       
        private static string ExtractEmbeddedMsi()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
                throw new InvalidOperationException(
                    "No .msi package is embedded in this executable.\n" +
                    "Put one in the project's Payload folder and rebuild.");

            // "CustomInstallerUI.Payload.DemoApp_V1_0_0.msi" -> "DemoApp_V1_0_0.msi"
            string fileName = resourceName;
            int marker = fileName.IndexOf("Payload.", StringComparison.OrdinalIgnoreCase);
            if (marker >= 0)
                fileName = fileName[(marker + "Payload.".Length)..];

            Directory.CreateDirectory(ExtractedMsiDirectory);
            string msiPath = Path.Combine(ExtractedMsiDirectory, fileName);

            using Stream resource = assembly.GetManifestResourceStream(resourceName)!;
            using FileStream file = File.Create(msiPath);
            resource.CopyTo(file);

            return msiPath;
        }

       
        public static bool IsInstalled(string productCode) =>
            MsiQueryProductState(productCode) == InstallStateDefault;

       
        public static string GetDefaultInstallDirectory(MsiPackageInfo package)
        {
            string programFiles =
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            return string.IsNullOrWhiteSpace(package.Manufacturer)
                ? Path.Combine(programFiles, package.ProductName)
                : Path.Combine(programFiles, package.Manufacturer, package.ProductName);
        }

       
        private static string ReadMsiProperty(string msiPath, string property)
        {
            // MSIDBOPEN_READONLY == (LPCTSTR)0
            uint result = MsiOpenDatabase(msiPath, IntPtr.Zero, out IntPtr database);
            if (result != 0)
                throw new InvalidOperationException($"Cannot open MSI database (error {result}).");

            try
            {
                result = MsiDatabaseOpenView(database,
                    $"SELECT `Value` FROM `Property` WHERE `Property`='{property}'",
                    out IntPtr view);
                if (result != 0)
                    throw new InvalidOperationException($"Cannot query MSI database (error {result}).");

                try
                {
                    MsiViewExecute(view, IntPtr.Zero);

                    result = MsiViewFetch(view, out IntPtr record);
                    if (result != 0)
                        return string.Empty; // property not present in this package

                    try
                    {
                        var buffer = new StringBuilder(512);
                        uint length = (uint)buffer.Capacity;
                        result = MsiRecordGetString(record, 1, buffer, ref length);
                        if (result == ErrorMoreData)
                        {
                            buffer.Capacity = (int)++length;
                            MsiRecordGetString(record, 1, buffer, ref length);
                        }
                        return buffer.ToString();
                    }
                    finally { MsiCloseHandle(record); }
                }
                finally { MsiCloseHandle(view); }
            }
            finally { MsiCloseHandle(database); }
        }

       
        public static int Install(MsiPackageInfo package, string? customInstallDir,
                                  IProgress<string> status)
        {
            status.Report($"Installing {package.ProductName} {package.ProductVersion}... " +
                          "this can take a minute.");

            // /qn  = no MSI UI (ours is the only UI)
            // ARPSYSTEMCOMPONENT=1 = hide the MSI's own Programs and Features
            //                        entry; we register our own instead.
            string arguments =
                $"/i \"{package.MsiPath}\" /qn /norestart ARPSYSTEMCOMPONENT=1 " +
                $"/l*v \"{InstallLogPath}\"";

            // Advanced Installer packages use APPDIR as the install folder property.
            if (!string.IsNullOrWhiteSpace(customInstallDir))
                arguments += $" APPDIR=\"{customInstallDir.Trim()}\"";

            int exitCode = RunMsiexec(arguments);
            ThrowIfMsiFailed(exitCode, "installation", InstallLogPath);

            status.Report("Registering the uninstaller...");
            CopyBootstrapperHome();
            RegisterUninstallEntry(package);

            return exitCode;
        }

        private static void CopyBootstrapperHome()
        {
            string exePath   = Environment.ProcessPath!;
            string sourceDir = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(exePath)!);
            string targetDir = Path.TrimEndingDirectorySeparator(BootstrapperHomeDirectory);

            // Already running from the home directory (e.g. reinstall) - nothing to copy.
            if (sourceDir.Equals(targetDir, StringComparison.OrdinalIgnoreCase))
                return;

            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
        }

        
        private static void RegisterUninstallEntry(MsiPackageInfo package)
        {
            string uninstallerExe = Path.Combine(
                BootstrapperHomeDirectory, Path.GetFileName(Environment.ProcessPath!));

            string displayIcon = TryGetInstalledExePath(package.ProductCode) ?? uninstallerExe;

            using RegistryKey key = Registry.LocalMachine.CreateSubKey(BootstrapperUninstallKey);
            key.SetValue("DisplayName",     package.ProductName);
            key.SetValue("DisplayVersion",  package.ProductVersion);
            key.SetValue("Publisher",       "My Company");
            key.SetValue("DisplayIcon",     displayIcon);
            key.SetValue("UninstallString", $"\"{uninstallerExe}\"");
            key.SetValue("InstallDate",     DateTime.Now.ToString("yyyyMMdd"));
            key.SetValue("NoModify",        1, RegistryValueKind.DWord);
            key.SetValue("NoRepair",        1, RegistryValueKind.DWord);
        }

      

        private static string? _pendingDeleteDirectory;

        
        public static int Uninstall(MsiPackageInfo package, IProgress<string> status)
        {
            status.Report($"Removing {package.ProductName}...");

            int exitCode = RunMsiexec(
                $"/x {package.ProductCode} /qn /norestart /l*v \"{UninstallLogPath}\"");
            ThrowIfMsiFailed(exitCode, "uninstall", UninstallLogPath);

            status.Report("Cleaning up...");
            Registry.LocalMachine.DeleteSubKeyTree(BootstrapperUninstallKey,
                                                   throwOnMissingSubKey: false);

            // Remove the bootstrapper home folder (%ProgramData%\DemoApp).
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DemoApp");

            if (Directory.Exists(root))
            {
                string self = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
                string rootFull = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));

                bool runningFromRoot =
                    self.Equals(rootFull, StringComparison.OrdinalIgnoreCase) ||
                    self.StartsWith(rootFull + Path.DirectorySeparatorChar,
                                    StringComparison.OrdinalIgnoreCase);

                if (runningFromRoot)
                {
                    
                    _pendingDeleteDirectory = root;
                }
                else
                {
                    Directory.Delete(root, recursive: true);
                }
            }

            return exitCode;
        }

       
        public static void CompletePendingSelfDelete()
        {
            if (_pendingDeleteDirectory == null)
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName        = "cmd.exe",
                Arguments       = $"/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"{_pendingDeleteDirectory}\"",
                CreateNoWindow  = true,
                UseShellExecute = false,
                WindowStyle     = ProcessWindowStyle.Hidden
            });

            _pendingDeleteDirectory = null;
        }

       

        private static int RunMsiexec(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName        = "msiexec.exe",
                Arguments       = arguments,
                UseShellExecute = false,
                CreateNoWindow  = true
            };

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start msiexec.exe.");
            process.WaitForExit();
            return process.ExitCode;
        }

        private static void ThrowIfMsiFailed(int exitCode, string operation, string logPath)
        {
            if (exitCode is MsiExitSuccess or MsiExitRebootRequired)
                return;

            string reason = exitCode switch
            {
                1602 => "the operation was cancelled",
                1603 => "a fatal error occurred during the operation",
                1618 => "another installation is already in progress",
                1619 => "the installation package could not be opened",
                _    => $"msiexec exited with code {exitCode}"
            };

            throw new InvalidOperationException(
                $"The {operation} failed: {reason}.\n\nDetails: {logPath}");
        }

        
        public static string? TryGetInstalledExePath(string productCode)
        {
            var buffer = new StringBuilder(1024);
            uint length = (uint)buffer.Capacity;
            if (MsiGetProductInfo(productCode, "InstallLocation", buffer, ref length) == 0 &&
                buffer.Length > 0)
            {
                string candidate = Path.Combine(buffer.ToString(), MainExecutable);
                if (File.Exists(candidate))
                    return candidate;
            }

            foreach (string basePath in new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\"
            })
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(basePath + productCode);
                if (key?.GetValue("DisplayIcon") is string icon)
                {
                    string path = icon.Split(',')[0].Trim().Trim('"');
                    if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(path))
                        return path;
                }
            }

            return null;
        }

        public static void LaunchInstalledApp(string productCode)
        {
            string? exe = TryGetInstalledExePath(productCode);
            if (exe == null)
                return;

            Process.Start(new ProcessStartInfo(exe)
            {
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute  = true
            });
        }
    }
}

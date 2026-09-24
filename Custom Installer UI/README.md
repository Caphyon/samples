# DemoApp – Custom Installer UI

This project is a setup wizard, developed in WPF, for an MSI package. The actual installation is performed by the MSI package, while the user interacts only with the setup dialogs, whose structure and appearance can be customized as needed, directly in the WPF project.

The same interface is also used for uninstallation, including when it is started from Control Panel. The application can be distributed as a single executable file that includes the MSI package.
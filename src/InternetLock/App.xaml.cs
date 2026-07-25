using System.Windows;
using InternetLock.Helpers;
using InternetLock.Services;
using InternetLock.ViewModels;
using InternetLock.Views;

namespace InternetLock
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppPaths.EnsureDirectoriesExist();

            // Dependency Injection Setup
            ILoggerService loggerService = new FileLoggerService();
            IStateStorageService stateStorageService = new StateStorageService(loggerService);
            IPasswordService passwordService = new PasswordService(loggerService);
            INetworkAdapterService adapterService = new NetworkAdapterService(loggerService);

            var mainViewModel = new MainViewModel(
                adapterService,
                passwordService,
                stateStorageService,
                loggerService);

            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}

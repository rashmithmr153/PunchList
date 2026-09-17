using System;
using System.Linq;
using System.Windows;
using PunchList.Data;
using PunchList.Services;
using PunchList.ViewModels;

namespace PunchList
{
    public partial class App : Application
    {
        private TaskTimerService? _timerService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Initialize SQLite database & tables
            var dbInit = new DatabaseInitializer();
            dbInit.InitializeDatabase();

            // 2. Instantiate core services
            var repository = new TaskRepository(dbInit);
            var stateMachine = new TaskStateMachine();
            _timerService = new TaskTimerService(stateMachine, repository);
            var notificationService = new NotificationService(repository, _timerService, stateMachine);
            var startupService = new StartupService();

            // 3. Start background polling loop
            _timerService.Start();

            // 4. Create ViewModel and MainWindow
            var viewModel = new MainViewModel(repository, stateMachine, _timerService, notificationService, startupService);
            var mainWindow = new MainWindow(viewModel);
            MainWindow = mainWindow;

            // 5. If started with --minimized (e.g. from Windows Startup), stay in system tray
            bool startMinimized = e.Args.Any(arg => arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase));
            if (!startMinimized)
            {
                mainWindow.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _timerService?.Stop();
            base.OnExit(e);
        }
    }
}

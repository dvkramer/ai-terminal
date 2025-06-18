using System;
using Prism.Ioc;
using AICommandPrompt.Views;
using System.Windows;
using System.Windows.Threading; // Required for DispatcherUnhandledExceptionEventArgs

namespace AICommandPrompt
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register services for DI here if needed in the future
            // containerRegistry.RegisterSingleton<Services.ISettingsService, Services.SettingsService>();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}"); // Log to debug output

            // Log to a file (example - consider a more robust logging solution for production)
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AICommandPrompt",
                    "error_log.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now}: Unhandled Exception: {e.Exception}\n\n");
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {logEx}");
            }

            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nThe application may become unstable. It's recommended to save your work if possible and restart.",
                            "Unhandled Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

            // Setting e.Handled = true prevents the application from crashing immediately.
            // For many unhandled exceptions, especially those from the UI thread, the application might be unstable.
            // Depending on the severity/type of common exceptions, you might decide to let some crash
            // or attempt more specific recovery. For now, we'll mark it as handled.
            e.Handled = true;
        }
    }
}

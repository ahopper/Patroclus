using System;
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using Patroclus.Avalonia.ViewModels;
using Patroclus.Avalonia.Views;

namespace Patroclus.Avalonia
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp().Start<MainWindow>(() => new MainWindowViewModel());
            }
            catch(Exception e)
            {
                Errorlog.logException(e, "Main");
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
    }
}

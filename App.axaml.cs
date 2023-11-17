using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SmartTrainApplication.Views;
using Splat;

namespace SmartTrainApplication
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            Locator.CurrentMutable.RegisterLazySingleton<MainWindowViewModel>(() => new MainWindowViewModel());
            Locator.CurrentMutable.RegisterLazySingleton<SideBarViewModel>(() => new SideBarViewModel());
            Locator.CurrentMutable.RegisterLazySingleton<SideBar2ViewModel>(() => new SideBar2ViewModel());
            Locator.CurrentMutable.RegisterLazySingleton<SideBar3ViewModel>(() => new SideBar3ViewModel());
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
#region

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SmartTrainApplication.Views;
using Splat;

#endregion

namespace SmartTrainApplication;

public partial class App : Application {
  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);
    Locator.CurrentMutable.RegisterLazySingleton<MainWindowViewModel>(() => new MainWindowViewModel());
    Locator.CurrentMutable.RegisterLazySingleton<TrackEditorViewModel>(() => new TrackEditorViewModel());
    Locator.CurrentMutable.RegisterLazySingleton<TrainEditorViewModel>(() => new TrainEditorViewModel());
    Locator.CurrentMutable.RegisterLazySingleton<SimulationViewModel>(() => new SimulationViewModel());
  }

  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.MainWindow = new MainWindow();

    base.OnFrameworkInitializationCompleted();
  }
}
#region

using Avalonia.Controls;

#endregion

namespace SmartTrainApplication.Views;

public partial class MainWindow : Window {
  public static TopLevel TopLevel { get; set; }

  public MainWindow() {
    InitializeComponent();
    DataContext = new MainWindowViewModel();
    TopLevel? topLevel = GetTopLevel(this);
    TopLevel = topLevel;
  }
}
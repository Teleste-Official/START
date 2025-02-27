#region

using System.ComponentModel;

#endregion

namespace SmartTrainApplication.Views;

public class ViewModelBase : INotifyPropertyChanged {
  public event PropertyChangedEventHandler? PropertyChanged;

  protected virtual void RaisePropertyChanged(string propertyName) {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
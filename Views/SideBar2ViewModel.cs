using System;


namespace SmartTrainApplication.Views
{
    internal class SideBar2ViewModel : ViewModelBase
    {

        public Sidebar2 SidebarView { get; }

        public SideBar2ViewModel()
        {
            SidebarView = new Sidebar2();
        }
    }
}

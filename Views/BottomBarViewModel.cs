using SmartTrainApplication.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartTrainApplication.Views
{
    public class BottomBarViewModel : ViewModelBase
    {

        public BottomBarViewModel()
        {
            
        }

        public void ImportButton()
        {
            LayerManager.ImportNewRoute(MainWindow.TopLevel);
            return;
        }
    }
}

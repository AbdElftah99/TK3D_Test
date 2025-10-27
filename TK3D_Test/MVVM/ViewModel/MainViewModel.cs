using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TK3D_Test.Revit;

namespace TK3D_Test.MVVM.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        #region Properties
        private Document _doc;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DoneCommand))]
        private string ceilingHeight;

        [ObservableProperty]
        private ObservableCollection<string> wallFinishTypes;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DoneCommand))]
        private string selectedWallFinishType;
        private int integerValue;
        #endregion

        #region Constructor
        public MainViewModel(Document doc)
        {
            _doc = doc;
            DataCache dataCache = new();
            WallFinishTypes     = new ObservableCollection<string>(dataCache.GetWallTypes(_doc).Select(w => w.Name));
        }
        #endregion

        #region Commands
        [RelayCommand(CanExecute = nameof(CanDone))]
        public void Done(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }
        public bool CanDone() => !string.IsNullOrEmpty(SelectedWallFinishType) 
            && (!string.IsNullOrWhiteSpace(CeilingHeight) && int.TryParse(CeilingHeight, out integerValue) && integerValue is >= 0);
        #endregion
    }
}

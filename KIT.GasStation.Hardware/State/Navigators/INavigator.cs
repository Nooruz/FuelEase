using KIT.GasStation.Hardware.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.State.Navigators
{
    /// <summary>
    /// Тип ViewModel
    /// </summary>
    public enum ViewType
    {
        MainWindow,
        Main,
        Lanfeng,
        EKassa,
        NewCas,
        PKElectronics,
        KITView,
        Gilbarco,
        Emulator
    }
    public interface INavigator
    {
        BaseViewModel CurrentViewModel { get; set; }
        event Action StateChanged;

        Task PreloadViewModelsAsync(IEnumerable<ViewType> viewTypes);
        Task<BaseViewModel> GetViewModelAsync(ViewType viewType);
        BaseViewModel GetViewModel(ViewType viewType);
        Task Renavigate(ViewType viewType);
    }
}

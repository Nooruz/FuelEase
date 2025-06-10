using FuelEase.Hardware.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FuelEase.Hardware.State.Navigators
{
    public enum ViewType
    {
        MainWindow,
        Main,
        Lanfeng,
        EKassa,
        PKElectronics
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

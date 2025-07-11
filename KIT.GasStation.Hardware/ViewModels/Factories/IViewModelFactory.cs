using KIT.GasStation.Hardware.State.Navigators;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.ViewModels.Factories
{
    public interface IViewModelFactory
    {
        Task<BaseViewModel> CreateViewModelAsync(ViewType viewType);
        BaseViewModel CreateViewModel(ViewType viewType);
    }
}

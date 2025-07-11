using KIT.GasStation.State.Navigators;
using KIT.GasStation.ViewModels.Base;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Factories
{
    public interface IViewModelFactory
    {
        Task<BaseViewModel> CreateViewModelAsync(ViewType viewType);
        BaseViewModel CreateViewModel(ViewType viewType);
    }
}

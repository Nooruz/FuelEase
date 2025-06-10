using FuelEase.State.Navigators;
using FuelEase.ViewModels.Base;
using System.Threading.Tasks;

namespace FuelEase.ViewModels.Factories
{
    public interface IViewModelFactory
    {
        Task<BaseViewModel> CreateViewModelAsync(ViewType viewType);
        BaseViewModel CreateViewModel(ViewType viewType);
    }
}

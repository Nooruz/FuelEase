using FuelEase.Hardware.State.Navigators;
using System.Threading.Tasks;

namespace FuelEase.Hardware.ViewModels.Factories
{
    public interface IViewModelFactory
    {
        Task<BaseViewModel> CreateViewModelAsync(ViewType viewType);
        BaseViewModel CreateViewModel(ViewType viewType);
    }
}

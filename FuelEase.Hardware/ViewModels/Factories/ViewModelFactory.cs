using FuelEase.Hardware.State.Navigators;
using System;
using System.Threading.Tasks;

namespace FuelEase.Hardware.ViewModels.Factories
{
    public class ViewModelFactory : IViewModelFactory
    {
        #region Private Members

        private readonly CreateViewModel<MainWindowViewModel> _createMainWindowViewModel;
        private readonly CreateViewModel<LanfengViewModel> _createLanfengViewModel;
        private readonly CreateViewModel<PKElectronicsViewModel> _createPKElectronicsViewModel;
        private readonly CreateViewModel<EKassaViewModel> _createEKassaViewModel;

        #endregion

        #region Constructor

        public ViewModelFactory(CreateViewModel<MainWindowViewModel> createMainWindowViewModel,
            CreateViewModel<LanfengViewModel> createLanfengViewModel,
            CreateViewModel<PKElectronicsViewModel> createPKElectronicsViewModel,
            CreateViewModel<EKassaViewModel> createEKassaViewModel)
        {
            _createMainWindowViewModel = createMainWindowViewModel;
            _createLanfengViewModel = createLanfengViewModel;
            _createPKElectronicsViewModel = createPKElectronicsViewModel;
            _createEKassaViewModel = createEKassaViewModel;
        }

        #endregion

        #region Public Voids

        public async Task<BaseViewModel> CreateViewModelAsync(ViewType viewType)
        {
            // Выполнение создания ViewModel в асинхронном методе (например, имитация задержки для асинхронной инициализации)
            return await Task.Run(() =>
            {
                return viewType switch
                {
                    ViewType.MainWindow => (BaseViewModel)_createMainWindowViewModel(),
                    ViewType.Lanfeng => _createLanfengViewModel(),
                    ViewType.PKElectronics => _createPKElectronicsViewModel(),
                    ViewType.EKassa => _createEKassaViewModel(),
                    _ => throw new ArgumentException("The ViewType does not have a ViewModel.", nameof(viewType)),
                };
            });
        }

        public BaseViewModel CreateViewModel(ViewType viewType)
        {
            return viewType switch
            {
                ViewType.MainWindow => _createMainWindowViewModel(),
                ViewType.Lanfeng => _createLanfengViewModel(),
                ViewType.PKElectronics => _createPKElectronicsViewModel(),
                ViewType.EKassa => _createEKassaViewModel(),
                _ => throw new ArgumentException("The ViewType does not have a ViewModel.", nameof(viewType)),
            };
        }

        #endregion
    }
}

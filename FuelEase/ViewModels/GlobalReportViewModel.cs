using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.Domain.Views;
using FuelEase.EntityFramework.Services;
using FuelEase.State.Navigators;
using FuelEase.State.Nozzles;
using FuelEase.State.Shifts;
using FuelEase.State.Users;
using FuelEase.ViewModels.Base;
using FuelEase.ViewModels.Factories;
using FuelEase.ViewModels.GlobalReports;
using FuelEase.Views.GlobalReports;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FuelEase.ViewModels
{
    public class GlobalReportViewModel : BaseViewModel, ISupportServices, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<GlobalReportViewModel> _logger;
        private readonly INavigator _navigator;
        private ModuleViewModel _selectedModuleViewModel;
        private List<ModuleViewModel> _moduleViewModels = new();

        #endregion

        #region Public Properties

        public virtual bool CanShowSplashScreen { get; set; }
        public List<ModuleViewModel> ModuleViewModels
        {
            get => _moduleViewModels;
            set
            {
                _moduleViewModels = value;
                OnPropertyChanged(nameof(ModuleViewModels));
            }
        }
        public ModuleViewModel SelectedModuleViewModel
        {
            get => _selectedModuleViewModel;
            set
            {
                _selectedModuleViewModel = value;
                SelectedModuleViewModel.Show(NavigationService);
                OnPropertyChanged(nameof(SelectedModuleViewModel));
            }
        }

        #endregion

        #region Constructor

        public GlobalReportViewModel(ILogger<GlobalReportViewModel> logger,
            INavigator navigator,
            ILogger<ShiftInfoViewModel> shiftInfoViewModelLogger,
            IShiftService shiftService,
            IUserStore userStore,
            IFuelSaleService fuelSaleService,
            IShiftStore shiftStore,
            IFuelService fuelService,
            INozzleService nozzleService,
            IShiftCounterService nozzleCounterService,
            ITankService tankService,
            IFuelIntakeService fuelIntakeService,
            ITankShiftCounterService tankShiftCounterService,
            INozzleStore nozzleStore,
            IViewService<TankFuelQuantityView> tankFuelQuantityView)
        {
            CanShowSplashScreen = false;
            _logger = logger;
            _navigator = navigator;

            ServiceContainer.RegisterService(shiftInfoViewModelLogger);
            ServiceContainer.RegisterService(shiftService);
            ServiceContainer.RegisterService(userStore);
            ServiceContainer.RegisterService(fuelSaleService);
            ServiceContainer.RegisterService(shiftStore);
            ServiceContainer.RegisterService(fuelService);
            ServiceContainer.RegisterService(nozzleService);
            ServiceContainer.RegisterService(nozzleCounterService);
            ServiceContainer.RegisterService(tankService);
            ServiceContainer.RegisterService(fuelIntakeService);
            ServiceContainer.RegisterService(tankShiftCounterService);
            ServiceContainer.RegisterService(nozzleStore);
            ServiceContainer.RegisterService(tankFuelQuantityView);
        }

        #endregion

        #region Public Voids

        [Command]
        public void OnModulesLoaded()
        {
            if (SelectedModuleViewModel == null)
            {
                SelectedModuleViewModel = ModuleViewModels.First();
                SelectedModuleViewModel.IsSelected = true;
                SelectedModuleViewModel.Show(viewModel: SelectedModuleViewModel);
            }

            CanShowSplashScreen = true;
        }

        [Command]
        public void OnSelectedModuleViewModelChanged()
        {
            SelectedModuleViewModel.IsSelected = true;
            SelectedModuleViewModel.Show(viewModel: SelectedModuleViewModel);
        }

        public async Task StartAsync()
        {
            var shiftInfo = new ShiftInfoViewModel(nameof(ShiftInfoView), this, "Смена");

            await shiftInfo.StartAsync();

            shiftInfo.SetIcon("GridTasks");

            ModuleViewModels.Add(shiftInfo);
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var viewModels in ModuleViewModels)
                {
                    viewModels.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}

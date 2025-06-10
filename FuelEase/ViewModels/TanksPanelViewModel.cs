using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.Domain.Views;
using FuelEase.State.Shifts;
using FuelEase.ViewModels.Base;
using FuelEase.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FuelEase.ViewModels
{
    public class TanksPanelViewModel : PanelViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<TanksPanelViewModel> _logger;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly ITankService _tankService;
        private readonly IFuelService _fuelService;
        private readonly IFuelIntakeService _fuelIntakeService;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly IShiftStore _shiftStore;
        private readonly ITankShiftCounterService _tankShiftCounterService;
        private ObservableCollection<TankFuelQuantityView> _tankFuelQuantityViews = new();

        #endregion

        #region Public Properties

        public ObservableCollection<TankFuelQuantityView> TankFuelQuantityViews
        {
            get => _tankFuelQuantityViews;
            set
            {
                _tankFuelQuantityViews = value;
                OnPropertyChanged(nameof(TankFuelQuantityViews));
            }
        }

        #endregion

        #region Constructor

        public TanksPanelViewModel(ILogger<TanksPanelViewModel> logger,
            IViewService<TankFuelQuantityView> tankFuelQuantityView, 
            ITankService tankService, 
            IFuelService fuelService,
            IFuelIntakeService fuelIntakeService,
            IFuelSaleService fuelSaleService,
            ITankShiftCounterService tankShiftCounterService,
            IShiftStore shiftStore)
        {
            Title = "Панель резервуаров";

            _tankFuelQuantityView = tankFuelQuantityView;
            _tankService = tankService;
            _fuelService = fuelService;
            _fuelIntakeService = fuelIntakeService;
            _fuelSaleService = fuelSaleService;
            _logger = logger;
            _shiftStore = shiftStore;
            _tankShiftCounterService = tankShiftCounterService;

            _tankService.OnCreated += async (f) => await GetData();
            _tankService.OnUpdated += async (f) => await GetData();
            _tankService.OnDeleted += async (f) => await GetData();
            _fuelService.OnCreated += async (f) => await GetData();
            _fuelService.OnUpdated += async (f) => await GetData();
            _fuelService.OnDeleted += async (f) => await GetData();
            _fuelIntakeService.OnCreated += async (f) => await GetData();
            _fuelIntakeService.OnUpdated += async (f) => await GetData();
            _fuelIntakeService.OnDeleted += async (f) => await GetData();
            _fuelSaleService.OnCreated += async (f) => await GetData();
            _fuelSaleService.OnUpdated += async (f) => await GetData();
            _fuelSaleService.OnDeleted += async (f) => await GetData();
            _shiftStore.OnOpened += ShiftStore_OnOpened;
            _shiftStore.OnClosed += ShiftStore_OnClosed;
        }

        #endregion

        #region Public Voids

        public async Task StartAsync()
        {
            await GetData();
        }

        #endregion

        #region Private Voids

        public async Task GetData()
        {
            try
            {
                TankFuelQuantityViews = new(await _tankFuelQuantityView.GetAllAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void ShiftStore_OnClosed(Shift shift)
        {
            _ = Task.Run(async () => 
            {
                if (TankFuelQuantityViews != null && TankFuelQuantityViews.Any())
                {
                    if (_shiftStore.CurrentShift != null && _shiftStore.CurrentShiftState == ShiftState.Closed)
                    {
                        foreach (var item in TankFuelQuantityViews)
                        {
                            var tankShiftCounter = await _tankShiftCounterService.GetAsync(item.Id, _shiftStore.CurrentShift.Id);

                            if (tankShiftCounter != null)
                            {
                                tankShiftCounter.EndCount = item.CurrentFuelQuantity;
                                await _tankShiftCounterService.UpdateAsync(tankShiftCounter.Id, tankShiftCounter);
                            }
                        }
                    }
                }
            });
        }

        private void ShiftStore_OnOpened(Shift shift)
        {
            _ = Task.Run(async () =>
            {
                if (TankFuelQuantityViews != null && TankFuelQuantityViews.Any())
                {
                    if (_shiftStore.CurrentShift != null && _shiftStore.CurrentShiftState == ShiftState.Open)
                    {
                        foreach (var item in TankFuelQuantityViews)
                        {
                            var tankShiftCounter = await _tankShiftCounterService.GetAsync(item.Id, _shiftStore.CurrentShift.Id);

                            if (tankShiftCounter == null)
                            {
                                await _tankShiftCounterService.CreateAsync(new TankShiftCounter
                                {
                                    ShiftId = _shiftStore.CurrentShift.Id,
                                    TankId = item.Id,
                                    BeginCount = item.CurrentFuelQuantity
                                });
                            }
                        }
                    }
                }
            });
        }

        #endregion
    }
}
